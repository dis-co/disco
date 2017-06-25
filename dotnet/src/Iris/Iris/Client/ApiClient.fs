namespace Iris.Client

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Core
open Iris.Client
open Iris.Net
open Iris.Serialization

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

  // ** TIMEOUT

  let private TIMEOUT = 5000<ms>

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<ClientEvent>

  // ** ClientState

  [<NoComparison;NoEquality>]
  type private ClientState =
    { Status: ServiceStatus
      Client: IrisClient
      Peer: IrisServer
      Socket: IClient
      Store:  Store
      Subscriptions: Subscriptions
      Disposables: IDisposable list
      Stopper: AutoResetEvent }

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        List.iter dispose self.Disposables
        try dispose self.Socket  with | _ -> ()
        try dispose self.Stopper with | _ -> ()

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start
    | Stop
    | Dispose
    | Notify      of ClientEvent
    | SetState    of state:State
    | SetStatus   of status:ServiceStatus
    | Update      of sm:StateMachine
    | Request     of sm:StateMachine
    | SocketEvent of ev:TcpClientEvent

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** handleNotify

  let private handleNotify (state: ClientState) (ev: ClientEvent) =
    Observable.onNext state.Subscriptions ev
    state

  // ** requestRegister

  let private requestRegister (state: ClientState) =
    sprintf "registering with %O:%O" state.Peer.IpAddress state.Peer.Port
    |> Logger.debug (tag "requestRegister")
    state.Client
    |> ApiRequest.Register
    |> Binary.encode
    |> Request.create (Guid.ofId state.Client.Id)
    |> state.Socket.Request

  // ** requestUnRegister

  let private requestUnRegister (state: ClientState) =
    sprintf "unregistering from %O:%O" state.Peer.IpAddress state.Peer.Port
    |> Logger.debug (tag "requestUnRegister")
    state.Client
    |> ApiRequest.UnRegister
    |> Binary.encode
    |> Request.create (Guid.ofId state.Client.Id)
    |> state.Socket.Request

  // ** handleStart

  let private handleStart (state: ClientState) (_: ApiAgent) =
    Tracing.trace (tag "handleStart") <| fun () ->
      requestRegister state
      state

  // ** handleSetStatus

  let private handleSetStatus (state: ClientState) (status: ServiceStatus) (agent: ApiAgent) =
    Tracing.trace (tag "handleSetStatus") <| fun () ->
      status |> ClientEvent.Status |> Msg.Notify |> agent.Post
      { state with Client = { state.Client with Status = status } }


  // ** handleSetState

  let private handleSetState (state: ClientState) (newstate: State) (agent: ApiAgent) =
    ClientEvent.Snapshot |> Msg.Notify |> agent.Post
    { state with Store = new Store(newstate) }

  // ** handleUpdate

  let private handleUpdate (state: ClientState) (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace (tag "handleUpdate") <| fun () ->
      state.Store.Dispatch sm
      sm |> ClientEvent.Update |> Msg.Notify |> agent.Post
      state

  // ** requestUpdate

  let private requestUpdate (socket: IClient) (sm: StateMachine) =
    sm
    |> ApiRequest.Update
    |> Binary.encode
    |> Request.create (Guid.ofId socket.ClientId)
    |> socket.Request

  // ** maybeDispatch

  let private maybeDispatch (data: ClientState) (sm: StateMachine) =
    Tracing.trace (tag "maybeDispatch") <| fun () ->
      match sm with
      | UpdateSlices _ -> data.Store.Dispatch sm
      | _ -> ()

  // ** handleRequest

  let private handleRequest (state: ClientState) (sm: StateMachine) =
    maybeDispatch state sm
    requestUpdate state.Socket sm
    state

  // ** handleServerRequest

  let private handleServerRequest (state: ClientState) (req: Request) (agent: ApiAgent) =
      match req.Body |> Binary.decode with
      | Right (ApiRequest.Snapshot snapshot) ->
        snapshot
        |> Msg.SetState
        |> agent.Post

        ApiResponse.OK
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Socket.Respond

      | Right (ApiRequest.Update sm) ->
        sm
        |> Msg.Update
        |> agent.Post

        ApiResponse.OK
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Socket.Respond

      | Right other ->
        string other
        |> ApiError.UnknownCommand
        |> ApiResponse.NOK
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Socket.Respond

      | Left error ->
        error
        |> string
        |> ApiError.MalformedRequest
        |> ApiResponse.NOK
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Socket.Respond
      state

  // ** handleClientResponse

  let private handleClientResponse (state: ClientState) (req: Response) (agent: ApiAgent) =
    match Binary.decode req.Body with
    //  ____            _     _                    _
    // |  _ \ ___  __ _(_)___| |_ ___ _ __ ___  __| |
    // | |_) / _ \/ _` | / __| __/ _ \ '__/ _ \/ _` |
    // |  _ <  __/ (_| | \__ \ ||  __/ | |  __/ (_| |
    // |_| \_\___|\__, |_|___/\__\___|_|  \___|\__,_|
    //            |___/
    | Right ApiResponse.Registered ->
      Logger.debug (tag "handleClientResponse") "registration successful"
      ClientEvent.Registered |> Msg.Notify |> agent.Post
    //  _   _       ____            _     _                    _
    // | | | |_ __ |  _ \ ___  __ _(_)___| |_ ___ _ __ ___  __| |
    // | | | | '_ \| |_) / _ \/ _` | / __| __/ _ \ '__/ _ \/ _` |
    // | |_| | | | |  _ <  __/ (_| | \__ \ ||  __/ | |  __/ (_| |
    //  \___/|_| |_|_| \_\___|\__, |_|___/\__\___|_|  \___|\__,_|
    //                        |___/
    | Right ApiResponse.Unregistered ->
      Logger.debug (tag "handleClientResponse") "un-registration successful"
      ClientEvent.UnRegistered |> Msg.Notify |> agent.Post
      agent.Post Msg.Dispose
    //   ___  _  __
    //  / _ \| |/ /
    // | | | | ' /
    // | |_| | . \
    //  \___/|_|\_\
    | Right ApiResponse.OK -> ()

    //  _   _  ___  _  __
    // | \ | |/ _ \| |/ /
    // |  \| | | | | ' /
    // | |\  | |_| | . \
    // |_| \_|\___/|_|\_\
    | Right (ApiResponse.NOK error) -> error |> string |> Logger.err (tag "handleClientResponse")
    //  ____                     _        _____
    // |  _ \  ___  ___ ___   __| | ___  | ____|_ __ _ __ ___  _ __
    // | | | |/ _ \/ __/ _ \ / _` |/ _ \ |  _| | '__| '__/ _ \| '__|
    // | |_| |  __/ (_| (_) | (_| |  __/ | |___| |  | | | (_) | |
    // |____/ \___|\___\___/ \__,_|\___| |_____|_|  |_|  \___/|_|
    | Left error -> error |> string |> Logger.err (tag "handleClientResponse")
    state

  // ** handleSocketEvent

  let private handleSocketEvent state (ev: TcpClientEvent) agent =
    match ev with
    | TcpClientEvent.Request  request  -> handleServerRequest  state request  agent
    | TcpClientEvent.Response response -> handleClientResponse state response agent

    | TcpClientEvent.Connected _ ->
      let status =
        match state.Client.Status with
        | ServiceStatus.Running -> state.Client.Status
        | _ ->
          let newstatus = ServiceStatus.Running
          newstatus
          |> ClientEvent.Status
          |> Msg.Notify
          |> agent.Post
          newstatus
      { state with Client = { state.Client with Status = status } }

    | TcpClientEvent.Disconnected _ ->
      let status =
        "Server ping timed out"
        |> Error.asClientError (tag "handleCheckStatus")
        |> ServiceStatus.Failed
      status
      |> ClientEvent.Status
      |> Msg.Notify
      |> agent.Post
      { state with Client = { state.Client with Status = status } }

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
          | Msg.Notify ev          -> handleNotify      state ev
          | Msg.Dispose            -> handleDispose     state
          | Msg.Start              -> handleStart       state inbox
          | Msg.Stop               -> handleStop        state
          | Msg.SetStatus status   -> handleSetStatus   state status   inbox
          | Msg.SetState newstate  -> handleSetState    state newstate inbox
          | Msg.Update sm          -> handleUpdate      state sm       inbox
          | Msg.Request       sm   -> handleRequest     state sm
          | Msg.SocketEvent   ev   -> handleSocketEvent state ev       inbox
        store.Update newstate
        return! act()
      }
    act ()

  // ** ApiClient module

  [<RequireQualifiedAccess>]
  module ApiClient =

    // *** create

    let create (server: IrisServer) (client: IrisClient) =
      let cts = new CancellationTokenSource()
      let subscriptions = new Subscriptions()

      let state =
        { Status = ServiceStatus.Stopped
          Client = client
          Peer = server
          Socket = Unchecked.defaultof<IClient>
          Store = Store(State.Empty)
          Subscriptions = subscriptions
          Stopper = new AutoResetEvent(false)
          Disposables = [] }

      let store:IAgentStore<ClientState> = AgentStore.create()
      store.Update state

      let agent = new ApiAgent(loop store, cts.Token)
      agent.Error.Add(sprintf "unhandled error on loop: %O" >> Logger.err (tag "loop"))

      { new IApiClient with

          // **** Start

          member self.Start () =
            either {
              client.Port
              |> Some
              |> Uri.tcpUri client.IpAddress
              |> sprintf "Connecting to server on %O"
              |> Logger.debug (tag "start")

              server.Port
              |> Some
              |> Uri.tcpUri server.IpAddress
              |> sprintf "Starting server on %O"
              |> Logger.debug (tag "start")

              let socket = TcpClient.create {
                ClientId = client.Id
                PeerAddress = server.IpAddress
                PeerPort = server.Port
                Timeout = int Constants.REQ_TIMEOUT * 1<ms>
              }

              do! socket.Start()

              let clntobs = socket.Subscribe (Msg.SocketEvent >> agent.Post)

              store.Update { store.State with Socket = socket; Disposables = [ clntobs ] }

              agent.Start()
              agent.Post Msg.Start
            }

          // **** State

          member self.State
            with get () = store.State.Store.State // :D

          // **** Status

          member self.Status
            with get () = store.State.Status

          // **** Subscribe

          member self.Subscribe (callback: ClientEvent -> unit) =
            Observable.subscribe callback subscriptions

          // **** Dispose

          //  ____  _
          // |  _ \(_)___ _ __   ___  ___  ___
          // | | | | / __| '_ \ / _ \/ __|/ _ \
          // | |_| | \__ \ |_) | (_) \__ \  __/
          // |____/|_|___/ .__/ \___/|___/\___|
          //             |_|

          member self.Dispose () =
            agent.Post Msg.Stop
            match store.State.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0) with
            | true -> ()
            | false ->
              Logger.debug (tag "Dispose") "attempt to un-register with server failed"
              ServiceStatus.Disposed |> ClientEvent.Status |> Msg.Notify |> agent.Post
              if not (store.State.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
                Logger.debug (tag "Dispose") "timeout: attempt to dispose api client failed"
            dispose cts
            dispose store.State
            store.Update { store.State with Status = ServiceStatus.Disposed }

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

          // **** Append

          member self.Append(cmd: StateMachine) =
            cmd
            |> Msg.Request
            |> agent.Post
        }
