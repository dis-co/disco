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

  // ** FREQ

  let private FREQ = 500<ms>

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
      Elapsed: Timeout
      Server: IServer
      Socket: IClient
      Store:  Store
      Subscriptions: Subscriptions
      Disposables: IDisposable list
      Stopper: AutoResetEvent }

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        List.iter dispose self.Disposables
        try dispose self.Server  with | _ -> ()
        try dispose self.Socket  with | _ -> ()
        try dispose self.Stopper with | _ -> ()

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Ping
    | Start
    | Stop
    | CheckStatus
    | Dispose
    | Notify      of ClientEvent
    | SetState    of state:State
    | SetStatus   of status:ServiceStatus
    | Update      of sm:StateMachine
    | Request     of sm:StateMachine
    | ServerEvent of ev:TcpServerEvent
    | ClientEvent of ev:TcpClientEvent

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

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

  // ** handleNotify

  let private handleNotify (state: ClientState) (ev: ClientEvent) =
    Observable.onNext state.Subscriptions ev
    state

  // ** requestRegister

  let private requestRegister (state: ClientState) =
    sprintf "registering with %O:%O" state.Peer.IpAddress state.Peer.Port
    |> Logger.debug (tag "requestRegister")
    state.Client
    |> ServerApiRequest.Register
    |> Binary.encode
    |> Request.create (Guid.ofId state.Client.Id)
    |> state.Socket.Request

  // ** requestUnRegister

  let private requestUnRegister (state: ClientState) =
    sprintf "unregistering from %O:%O" state.Peer.IpAddress state.Peer.Port
    |> Logger.debug (tag "requestUnRegister")
    state.Client
    |> ServerApiRequest.UnRegister
    |> Binary.encode
    |> Request.create (Guid.ofId state.Client.Id)
    |> state.Socket.Request

  // ** handleStart

  let private handleStart (state: ClientState) (agent: ApiAgent) =
    Tracing.trace (tag "handleStart") <| fun () ->
      let timer = pingTimer agent
      requestRegister state
      { state with Disposables = timer :: state.Disposables }

  // ** handleSetStatus

  let private handleSetStatus (state: ClientState) (status: ServiceStatus) (agent: ApiAgent) =
    Tracing.trace (tag "handleSetStatus") <| fun () ->
      status |> ClientEvent.Status |> Msg.Notify |> agent.Post
      { state with Client = { state.Client with Status = status } }

  // ** handleCheckStatus

  let private handleCheckStatus (state: ClientState) (agent: ApiAgent) =
    Tracing.trace (tag "handleCheckStatus") <| fun () ->
      if not (Service.hasFailed state.Client.Status) then
        match state.Elapsed with
        | x when x > TIMEOUT ->
          let status =
            "Server ping timed out"
            |> Error.asClientError (tag "handleCheckStatus")
            |> ServiceStatus.Failed
          status |> ClientEvent.Status |> Msg.Notify |> agent.Post
          { state with
             Client = { state.Client with Status = status }
             Elapsed = state.Elapsed + FREQ }
        | _ ->
          let status =
            match state.Client.Status with
            | ServiceStatus.Running -> state.Client.Status
            | _ ->
              let newstatus = ServiceStatus.Running
              newstatus |> ClientEvent.Status |> Msg.Notify |> agent.Post
              newstatus
          { state with
             Client = { state.Client with Status = status }
             Elapsed = state.Elapsed + FREQ }
      else
        state

  // ** handlePing

  let private handlePing (state: ClientState) =
    { state with Elapsed = 0<ms> }

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
    ServerApiRequest.Update sm
    |> Binary.encode
    |> Request.create (Guid.ofId socket.PeerId)
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

  let private handleServerRequest (state: ClientState) (req: IncomingRequest) (agent: ApiAgent) =
      match req.Body |> Binary.decode with
      | Right ClientApiRequest.Ping ->
        Msg.Ping
        |> agent.Post

        ApiResponse.Pong
        |> Binary.encode
        |> OutgoingResponse.fromRequest req
        |> state.Server.Respond

      | Right (ClientApiRequest.Snapshot snapshot) ->
        snapshot
        |> Msg.SetState
        |> agent.Post

        ApiResponse.OK
        |> Binary.encode
        |> OutgoingResponse.fromRequest req
        |> state.Server.Respond

      | Right (ClientApiRequest.Update sm) ->
        sm
        |> Msg.Update
        |> agent.Post

        ApiResponse.OK
        |> Binary.encode
        |> OutgoingResponse.fromRequest req
        |> state.Server.Respond

      | Left error ->
        error
        |> string
        |> ApiError.MalformedRequest
        |> ApiResponse.NOK
        |> Binary.encode
        |> OutgoingResponse.fromRequest req
        |> state.Server.Respond
      state

  // ** handleServerEvent

  let private handleServerEvent (state) (ev: TcpServerEvent) agent =
    match ev with
    | TcpServerEvent.Connect(peer, ip, port) ->
      sprintf "Connection from %O:%d" ip port
      |> Logger.debug (tag "handleServerEvent")
      state
    | TcpServerEvent.Disconnect peer ->
      sprintf "Connection from %O" peer
      |> Logger.debug (tag "handleServerEvent")
      state
    | TcpServerEvent.Request request ->
      handleServerRequest state request agent

  // ** handleClientResponse

  let private handleClientResponse (state: ClientState) (req: Response) (agent: ApiAgent) =
    match Binary.decode req.Body with
    | Right ApiResponse.Registered ->
      Logger.debug (tag "handleClientResponse") "registration successful"
      ClientEvent.Registered |> Msg.Notify |> agent.Post
    | Right ApiResponse.Unregistered ->
      Logger.debug (tag "handleClientResponse") "un-registration successful"
      ClientEvent.UnRegistered |> Msg.Notify |> agent.Post
      agent.Post Msg.Dispose
    | Right ApiResponse.OK
    | Right ApiResponse.Pong -> ()
    | Right (ApiResponse.NOK error) -> error |> string |> Logger.err (tag "handleClientResponse")
    | Left error -> error |> string |> Logger.err (tag "handleClientResponse")
    state

  // ** handleClientEvent

  let private handleClientEvent state (ev: TcpClientEvent) agent =
    match ev with
    | TcpClientEvent.Connected _ ->
      "Connected!" |> Logger.debug (tag "handleClientEvent")
      state
    | TcpClientEvent.Disconnected _ ->
      "Disconnected :(" |> Logger.debug (tag "handleClientEvent")
      state
    | TcpClientEvent.Response response ->
      handleClientResponse state response agent

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
          | Msg.CheckStatus        -> handleCheckStatus state          inbox
          | Msg.Ping               -> handlePing        state
          | Msg.Update sm          -> handleUpdate      state sm       inbox
          | Msg.Request       sm   -> handleRequest     state sm
          | Msg.ServerEvent   ev   -> handleServerEvent state ev       inbox
          | Msg.ClientEvent   ev   -> handleClientEvent state ev       inbox
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
          Server = Unchecked.defaultof<IServer>
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
                PeerId = client.Id
                PeerAddress = server.IpAddress
                PeerPort = server.Port
                Timeout = int Constants.REQ_TIMEOUT * 1<ms>
              }

              let server = TcpServer.create {
                Id = client.Id
                Listen = client.IpAddress
                Port = client.Port
              }

              do! socket.Start()
              do! server.Start()

              let srvobs = server.Subscribe (Msg.ServerEvent >> agent.Post)
              let clntobs = socket.Subscribe (Msg.ClientEvent >> agent.Post)

              store.Update { store.State with
                               Socket = socket
                               Server = server
                               Disposables = [ srvobs; clntobs ] }

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
