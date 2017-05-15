namespace Iris.Client

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Core
open Iris.Client
open Iris.Zmq
open Iris.Serialization
open Hopac
open ZeroMQ

// * ApiClient module

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

[<AutoOpen>]
module ApiClient =

  // ** tag

  let private tag (str: string) = String.Format("ApiClient.{0}",str)

  // ** FREQ

  let private FREQ = 500<ms>

  // ** TIMEOUT

  let private TIMEOUT = 5000<ms>

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<Guid, IObserver<ClientEvent>>

  // ** ClientState

  [<NoComparison;NoEquality>]
  type private ClientState =
    { Status: ServiceStatus
      Client: IrisClient
      Peer: IrisServer
      Elapsed: Timeout
      Server: IBroker
      Socket: IClient
      Store:  Store
      Subscriptions: Subscriptions
      Disposables: IDisposable list
      Stopper: AutoResetEvent }

    interface IDisposable with
      member self.Dispose() =
        List.iter dispose self.Disposables
        dispose self.Server
        dispose self.Socket
        dispose self.Stopper

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Ping
    | Start
    | Stop
    | CheckStatus
    | Dispose
    | SetState          of state:State
    | SetStatus         of status:ServiceStatus
    | Update            of sm:StateMachine
    | Request           of sm:StateMachine
    | RawServerRequest  of req:RawServerRequest
    | RawClientResponse of req:RawClientResponse

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** Listener

  type private Listener = IObservable<ClientEvent>

  // ** createListener

  let private createListener (guid: Guid) (subscriptions: Subscriptions) =
    { new Listener with
        member self.Subscribe(obs) =
          while not (subscriptions.TryAdd(guid, obs)) do
            Thread.Sleep(1)

          { new IDisposable with
              member self.Dispose() =
                match subscriptions.TryRemove(guid) with
                | true, _  -> ()
                | _ -> subscriptions.TryRemove(guid)
                      |> ignore } }

  // ** pingTimer

  let private pingTimer (agent: ApiAgent) =
    let cts = new CancellationTokenSource()

    let rec loop () =
      async {
        do! Async.Sleep(int FREQ)
        agent.Post(Msg.CheckStatus)
        return! loop ()
      }

    Async.Start(loop (), cts.Token)
    { new IDisposable with
        member self.Dispose () =
          cts.Cancel() }

  // ** notify

  let private notify (subs: Subscriptions) (ev: ClientEvent) =
    let subscriptions = subs.ToArray()
    for KeyValue(_,sub) in subscriptions do
      try sub.OnNext ev
      with
        | exn ->
          exn.Message
          |> sprintf "error calling on next on listener subscription: %s"
          |> Logger.err (tag "notify")

  // ** requestRegister

  let private requestRegister (state: ClientState) =
    sprintf "registering with %O:%O" state.Peer.IpAddress state.Peer.Port
    |> Logger.debug (tag "requestRegister")
    state.Client
    |> ServerApiRequest.Register
    |> Binary.encode
    |> fun body -> { Body = body }
    |> state.Socket.Request
    |> Either.mapError (string >> Logger.err (tag "requestRegister"))
    |> ignore

  // ** requestUnRegister

  let private requestUnRegister (state: ClientState) =
    sprintf "unregistering from %O:%O" state.Peer.IpAddress state.Peer.Port
    |> Logger.debug (tag "requestUnRegister")
    state.Client
    |> ServerApiRequest.UnRegister
    |> Binary.encode
    |> fun body -> { Body = body }
    |> state.Socket.Request
    |> Either.mapError (string >> Logger.err (tag "requestUnregister"))
    |> ignore

  // ** handleStart

  let private handleStart (state: ClientState) (agent: ApiAgent) =
    Tracing.trace (tag "handleStart") <| fun () ->
      let timer = pingTimer agent
      requestRegister state
      { state with Disposables = timer :: state.Disposables }

  // ** handleSetStatus

  let private handleSetStatus (state: ClientState) (status: ServiceStatus) =
    Tracing.trace (tag "handleSetStatus") <| fun () ->
      notify state.Subscriptions (ClientEvent.Status status)
      { state with Client = { state.Client with Status = status } }

  // ** handleCheckStatus

  let private handleCheckStatus (state: ClientState) =
    Tracing.trace (tag "handleCheckStatus") <| fun () ->
      if not (Service.hasFailed state.Client.Status) then
        match state.Elapsed with
        | x when x > TIMEOUT ->
          let status =
            "Server ping timed out"
            |> Error.asClientError (tag "handleCheckStatus")
            |> ServiceStatus.Failed
          notify state.Subscriptions (ClientEvent.Status status)
          { state with
             Client = { state.Client with Status = status }
             Elapsed = state.Elapsed + FREQ }
        | _ ->
          let status =
            match state.Client.Status with
            | ServiceStatus.Running -> state.Client.Status
            | _ ->
              let newstatus = ServiceStatus.Running
              notify state.Subscriptions (ClientEvent.Status newstatus)
              newstatus
          { state with
             Client = { state.Client with Status = status }
             Elapsed = state.Elapsed + FREQ }
      else
        state

  // ** handlePing

  let private handlePing (state: ClientState) =
    Tracing.trace (tag "handlePing") <| fun () ->
      { state with Elapsed = 0<ms> }

  // ** handleSetState

  let private handleSetState (state: ClientState) (newstate: State) =
    Tracing.trace (tag "handleSetState") <| fun () ->
      notify state.Subscriptions ClientEvent.Snapshot
      { state with Store = new Store(newstate) }

  // ** handleUpdate

  let private handleUpdate (state: ClientState) (sm: StateMachine) =
    Tracing.trace (tag "handleUpdate") <| fun () ->
      state.Store.Dispatch sm
      notify state.Subscriptions (ClientEvent.Update sm)
      state

  // ** requestUpdate

  let private requestUpdate (socket: IClient) (sm: StateMachine) =
    ServerApiRequest.Update sm
    |> Binary.encode
    |> fun body -> { Body = body }
    |> socket.Request
    |> Either.mapError (string >> Logger.err (tag "requestUpdate"))
    |> ignore

  // ** maybeDispatch

  let private maybeDispatch (data: ClientState) (sm: StateMachine) =
    Tracing.trace (tag "maybeDispatch") <| fun () ->
      match sm with
      | UpdateSlices _ -> data.Store.Dispatch sm
      | _ -> ()

  // ** handleRequest

  let private handleRequest (state: ClientState) (sm: StateMachine) (agent: ApiAgent) =
    maybeDispatch state sm
    requestUpdate state.Socket sm
    state

  // ** handleServerRequest

  let private handleServerRequest (state: ClientState) (req: RawServerRequest) (agent: ApiAgent) =
      match req.Body |> Binary.decode with
      | Right ClientApiRequest.Ping ->
        Msg.Ping
        |> agent.Post

        ApiResponse.Pong
        |> Binary.encode
        |> RawServerResponse.fromRequest req
        |> state.Server.Respond

      | Right (ClientApiRequest.Snapshot snapshot) ->
        snapshot
        |> Msg.SetState
        |> agent.Post

        ApiResponse.OK
        |> Binary.encode
        |> RawServerResponse.fromRequest req
        |> state.Server.Respond

      | Right (ClientApiRequest.Update sm) ->
        sm
        |> Msg.Update
        |> agent.Post

        ApiResponse.OK
        |> Binary.encode
        |> RawServerResponse.fromRequest req
        |> state.Server.Respond

      | Left error ->
        error
        |> string
        |> ApiError.MalformedRequest
        |> ApiResponse.NOK
        |> Binary.encode
        |> RawServerResponse.fromRequest req
        |> state.Server.Respond
      state

  // ** handleClientResponse

  let private handleClientResponse (state: ClientState) (req: RawClientResponse) (agent: ApiAgent) =
    match Either.bind Binary.decode req.Body with
    | Right ApiResponse.Registered ->
      Logger.debug (tag "handleClientResponse") "registration successful"
      notify state.Subscriptions ClientEvent.Registered
    | Right ApiResponse.Unregistered ->
      Logger.debug (tag "handleClientResponse") "un-registration successful"
      notify state.Subscriptions ClientEvent.UnRegistered
      agent.Post Msg.Dispose
    | Right ApiResponse.OK
    | Right ApiResponse.Pong -> ()
    | Right (ApiResponse.NOK error) -> error |> string |> Logger.err (tag "handleClientResponse")
    | Left error -> error |> string |> Logger.err (tag "handleClientResponse")
    state

  // ** handleStop

  let private handleStop (state: ClientState) =
    requestUnRegister state
    state

  // ** handleDispose

  let private handleDispose (state: ClientState) =
    List.iter dispose state.Disposables
    state.Stopper.Set() |> ignore
    { state with
        Status = ServiceStatus.Stopping
        Disposables = [] }

  // ** loop

  let private loop (store: IAgentStore<ClientState>) (inbox: ApiAgent) =
    let rec act () =
      async {
        let! msg = inbox.Receive()
        let state = store.State
        let newstate =
          match msg with
          | Msg.Dispose                -> handleDispose        state
          | Msg.Start                  -> handleStart          state inbox
          | Msg.Stop                   -> handleStop           state
          | Msg.SetStatus status       -> handleSetStatus      state status
          | Msg.SetState newstate      -> handleSetState       state newstate
          | Msg.CheckStatus            -> handleCheckStatus    state
          | Msg.Ping                   -> handlePing           state
          | Msg.Update sm              -> handleUpdate         state sm
          | Msg.Request       sm       -> handleRequest        state sm   inbox
          | Msg.RawServerRequest req   -> handleServerRequest  state req  inbox
          | Msg.RawClientResponse resp -> handleClientResponse state resp inbox
        store.Update newstate
        return! act()
      }
    act ()

  // ** ApiClient module

  [<RequireQualifiedAccess>]
  module ApiClient =

    // *** create

    let create ctx (server: IrisServer) (client: IrisClient) =
      either {
        let cts = new CancellationTokenSource()
        let subscriptions = new Subscriptions()

        let state =
          { Status = ServiceStatus.Stopped
            Client = client
            Peer = server
            Server = Unchecked.defaultof<IBroker>
            Socket = Unchecked.defaultof<IClient>
            Store = Store(State.Empty)
            Elapsed = 0<ms>
            Subscriptions = subscriptions
            Stopper = new AutoResetEvent(false)
            Disposables = [] }

        let store:IAgentStore<ClientState> = AgentStore.create()
        store.Update state

        let agent = new ApiAgent(loop store, cts.Token)
        agent.Error.Add(sprintf "unhandled error on loop: %O" >> Logger.err (tag "loop"))

        return
          { new IApiClient with

              // **** Start
              member self.Start () = either {
                  let backendAddr =
                    client.Id
                    |> string
                    |> Some
                    |> Uri.inprocUri Constants.API_CLIENT_PREFIX

                  let clientAddr =
                    client.Port
                    |> Some
                    |> Uri.tcpUri client.IpAddress

                  let srvAddr =
                    server.Port
                    |> Some
                    |> Uri.tcpUri server.IpAddress

                  sprintf "Starting server on %O" clientAddr
                  |> Logger.debug (tag "start")

                  let socket = Client.create ctx {
                    PeerId = client.Id
                    Frontend = srvAddr
                    Timeout = int Constants.REQ_TIMEOUT * 1<ms>
                  }

                  let result = Broker.create ctx {
                    Id = client.Id
                    MinWorkers = 5uy
                    MaxWorkers = 20uy
                    Frontend = clientAddr
                    Backend = backendAddr
                    RequestTimeout = int Constants.REQ_TIMEOUT * 1<ms>
                  }

                  match result with
                  | Right server ->
                    let srvobs = server.Subscribe (Msg.RawServerRequest >> agent.Post)
                    let clntobs = socket.Subscribe (Msg.RawClientResponse >> agent.Post)

                    let updated =
                      { store.State with
                          Socket = socket
                          Server = server
                          Disposables = [ srvobs; clntobs ] }

                    store.Update updated

                    agent.Start()
                    agent.Post Msg.Start
                  | Left error ->
                    Logger.err (tag "Start") (string error)
                    return! Either.fail error
                }

              // **** State

              member self.State
                with get () = store.State.Store.State // :D

              // **** Status

              member self.Status
                with get () = store.State.Status

              // **** Subscribe

              member self.Subscribe (callback: ClientEvent -> unit) =
                let guid = Guid.NewGuid()
                let listener = createListener guid subscriptions
                { new IObserver<ClientEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              // **** Dispose

              //  ____  _
              // |  _ \(_)___ _ __   ___  ___  ___
              // | | | | / __| '_ \ / _ \/ __|/ _ \
              // | |_| | \__ \ |_) | (_) \__ \  __/
              // |____/|_|___/ .__/ \___/|___/\___|
              //             |_|

              member self.Dispose () =
                agent.Post Msg.Stop
                store.State.Stopper.WaitOne() |> ignore
                dispose cts
                dispose store.State

              // **** AddCue

              //   ____
              //  / ___|   _  ___
              // | |  | | | |/ _ \
              // | |__| |_| |  __/
              //  \____\__,_|\___|

              member self.AddCue (cue: Cue) =
                AddCue cue
                |> Msg.Request
                |> agent.Post

              // **** UpdateCue

              member self.UpdateCue (cue: Cue) =
                UpdateCue cue
                |> Msg.Request
                |> agent.Post

              // **** RemoveCue

              member self.RemoveCue (cue: Cue) =
                RemoveCue cue
                |> Msg.Request
                |> agent.Post

              // **** AddPinGroup

              member self.AddPinGroup (group: PinGroup) =
                AddPinGroup group
                |> Msg.Request
                |> agent.Post

              // **** UpdatePinGroup

              member self.UpdatePinGroup (group: PinGroup) =
                UpdatePinGroup group
                |> Msg.Request
                |> agent.Post

              // **** RemovePinGroup

              member self.RemovePinGroup (group: PinGroup) =
                RemovePinGroup group
                |> Msg.Request
                |> agent.Post

              // **** AddCueList

              member self.AddCueList (cuelist: CueList) =
                AddCueList cuelist
                |> Msg.Request
                |> agent.Post

              // **** UpdateCueList

              member self.UpdateCueList (cuelist: CueList) =
                UpdateCueList cuelist
                |> Msg.Request
                |> agent.Post

              // **** RemoveCueList

              member self.RemoveCueList (cuelist: CueList) =
                RemoveCueList cuelist
                |> Msg.Request
                |> agent.Post

              // **** AddPin

              member self.AddPin(pin: Pin) =
                AddPin pin
                |> Msg.Request
                |> agent.Post

              // **** UpdatePin

              member self.UpdatePin(pin: Pin) =
                UpdatePin pin
                |> Msg.Request
                |> agent.Post

              // **** UpdateSlices

              member self.UpdateSlices(slices: Slices) =
                UpdateSlices slices
                |> Msg.Request
                |> agent.Post

              // **** RemovePin

              member self.RemovePin(pin: Pin) =
                RemovePin pin
                |> Msg.Request
                |> agent.Post

              // **** AddPin

              member self.Append(cmd: StateMachine) =
                cmd
                |> Msg.Request
                |> agent.Post
            }
      }
