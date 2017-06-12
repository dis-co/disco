namespace Iris.Service

// * Imports

open System
open System.Threading
open Iris.Core
open Iris.Core.Interfaces

// * Resolver

module Resolver =

  // ** tag

  let private tag (str: string) = String.format "Resolver.{0}" str

  // ** Subscriptions

  type private Subscriptions = Subscriptions<IrisEvent>

  // ** ResolverAgent

  type private ResolverAgent = MailboxProcessor<IrisEvent>

  // ** ResolverState

  [<NoComparison;NoEquality>]
  type private ResolverState =
    { Current: Frame
      Pending: Map<Frame, Cue>
      Subscriptions: Subscriptions }

  // ** ResolverStore

  type private ResolverStore = IAgentStore<ResolverState>

  // ** dispatch

  let private dispatch (state: ResolverState) (cue: Cue) =
    Array.iter
      (fun slices ->
         (Origin.Service, UpdateSlices slices)
         |> IrisEvent.Append
         |> Observable.notify state.Subscriptions)
      cue.Slices

  // ** maybeDispatch

  let private maybeDispatch (current: Frame) (state: ResolverState) =
    let call, keep =
      Map.fold
        (fun (call, keep) desired cue ->
          if desired < current then
            cue :: call, keep
          else
            call, Map.add desired cue keep)
        (List.empty, Map.empty)
        state.Pending
    List.iter (dispatch state) call
    { state with
        Current = current
        Pending = keep }

  // ** handleMessage

  let private handleMessage (msg: IrisEvent) (state: ResolverState) =
    match msg with
    | Append (_, CallCue cue) ->
      { state with Pending = Map.add state.Current cue state.Pending }
      |> maybeDispatch state.Current
    | Append (_, UpdateClock tick) ->
      maybeDispatch (int tick * 1<frame>) state
    | _ -> state

  // ** loop

  let private loop (store: ResolverStore) (inbox: ResolverAgent) =
    let rec impl () =
      async {
        let! msg = inbox.Receive()

        Actors.warnQueueLength (tag "loop") inbox

        store.State
        |> handleMessage msg
        |> store.Update

        return! impl()
      }

    impl ()

  // ** create

  let create () =
    let cts = new CancellationTokenSource()
    let subscriptions = Subscriptions()

    let store = AgentStore.create ()

    store.Update {
        Current = 0<frame>
        Pending = Map.empty
        Subscriptions = subscriptions
      }

    let agent = ResolverAgent.Start(loop store, cts.Token)

    { new IResolver with
        member resolver.Pending
          with get () = store.State.Pending

        member resolver.Update (ev: IrisEvent) =
          agent.Post ev

        member resolver.Subscribe callback =
          let listener = Observable.createListener subscriptions
          { new IObserver<IrisEvent> with
              member self.OnCompleted() = ()
              member self.OnError(error) = ()
              member self.OnNext(value) = callback value }
          |> listener.Subscribe

        member resolver.Dispose () =
          try
            cts.Cancel()
            dispose cts
          finally
            Logger.debug (tag "Dispose") "disposed" }
