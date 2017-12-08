namespace Disco.Service

// * Imports

open System
open System.Threading
open Disco.Core
open Disco.Core.Interfaces

// * Resolver

module Resolver =

  // ** tag

  let private tag (str: string) = String.format "Resolver.{0}" str

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<DiscoEvent>

  // ** ResolverAgent

  type private ResolverAgent = MailboxProcessor<StateMachine>

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
    cue.Slices
    |> UpdateSlices.ofArray
    |> DiscoEvent.appendService
    |> Observable.onNext state.Subscriptions

  // ** maybeDispatch

  let private maybeDispatch (current: Frame) (state: ResolverState) =
    let call, keep =
      Map.fold
        (fun (call, keep) desired cue ->
          if desired <= current then
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

  let private handleMessage (msg: StateMachine) (state: ResolverState) =
    match msg with
    | UpdateClock tick -> maybeDispatch (int tick * 1<frame>) state
    | CallCue cue ->
      maybeDispatch state.Current {
        state with Pending = Map.add state.Current cue state.Pending
      }
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

        member resolver.Update (cmd: StateMachine) =
          agent.Post cmd

        member resolver.Subscribe callback =
          Observable.subscribe callback subscriptions

        member resolver.Dispose () =
          try
            cts.Cancel()
            dispose cts
          finally
            Logger.debug (tag "Dispose") "disposed" }
