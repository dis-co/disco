namespace Iris.Client

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Core
open Iris.Client
open Iris.Zmq
open Iris.Serialization

// * ApiClient module

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

[<AutoOpen>]
module ApiClient =

  //  ____       _            _
  // |  _ \ _ __(_)_   ____ _| |_ ___
  // | |_) | '__| \ \ / / _` | __/ _ \
  // |  __/| |  | |\ V / (_| | ||  __/
  // |_|   |_|  |_| \_/ \__,_|\__\___|

  // ** tag

  let private tag (str: string) = sprintf "ApiClient.%s" str

  // ** FREQ

  [<Literal>]
  let private FREQ = 500u

  // ** TIMEOUT

  [<Literal>]
  let private TIMEOUT = 5000u

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<int, IObserver<ClientEvent>>

  // ** ClientStateData

  [<NoComparison;NoEquality>]
  type private ClientStateData =
    { Client: IrisClient
      Elapsed: uint32
      Server: Rep
      Socket: Req
      Store:  Store
      Timer:  IDisposable }

    interface IDisposable with
      member self.Dispose() =
        dispose self.Timer
        dispose self.Server
        dispose self.Socket

  // ** ClientState

  [<NoComparison;NoEquality>]
  type private ClientState =
    | Loaded of ClientStateData
    | Idle

    interface IDisposable with
      member self.Dispose() =
        match self with
        | Loaded data -> dispose data
        | Idle -> ()

  // ** Reply

  [<RequireQualifiedAccess>]
  type private Reply =
    | State  of State
    | Status of ServiceStatus
    | Ok

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Ping
    | CheckStatus
    | Start     of chan:ReplyChan
    | GetStatus of chan:ReplyChan
    | SetStatus of status:ServiceStatus
    | Dispose   of chan:ReplyChan
    | GetState  of chan:ReplyChan
    | SetState  of state:State
    | Update    of sm:StateMachine
    | Request   of chan:ReplyChan * sm:StateMachine

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** Listener

  type private Listener = IObservable<ClientEvent>

  // ** createListener

  let private createListener (subscriptions: Subscriptions) =
    { new Listener with
        member self.Subscribe(obs) =
          while not (subscriptions.TryAdd(obs.GetHashCode(), obs)) do
            Thread.Sleep(1)

          { new IDisposable with
              member self.Dispose() =
                match subscriptions.TryRemove(obs.GetHashCode()) with
                | true, _  -> ()
                | _ -> subscriptions.TryRemove(obs.GetHashCode())
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
    for KeyValue(_,sub) in subs do
      sub.OnNext ev

  // ** requestHandler

  let private requestHandler (agent: ApiAgent) (raw: byte array) =
    match Binary.decode raw with
    | Right ClientApiRequest.Ping ->
      agent.Post(Msg.Ping)
      ApiResponse.Pong
      |> Binary.encode
    | Right (ClientApiRequest.Snapshot state) ->
      agent.Post(Msg.SetState state)
      ApiResponse.OK
      |> Binary.encode
    | Right (ClientApiRequest.Update sm) ->
      agent.Post(Msg.Update sm)
      ApiResponse.OK
      |> Binary.encode
    | Left error ->
      string error
      |> ApiError.MalformedRequest
      |> ApiResponse.NOK
      |> Binary.encode

  // ** requestRegister

  let private requestRegister (data: ClientStateData) =
    let response =
      data.Client
      |> ServerApiRequest.Register
      |> Binary.encode
      |> data.Socket.Request
      |> Either.bind Binary.decode

    match response with
    | Right OK -> Either.succeed ()
    | Right (NOK error) ->
      string error
      |> Error.asClientError (tag "requestRegister")
      |> Either.fail
    | Right other ->
      sprintf "Unexpected Response from server: %A" other
      |> Error.asClientError (tag "requestRegister")
      |> Either.fail
    | Left error ->
      error
      |> Either.fail

  // ** requestUnRegister

  let private requestUnRegister (data: ClientStateData) =
    let response =
      data.Client
      |> ServerApiRequest.UnRegister
      |> Binary.encode
      |> data.Socket.Request
      |> Either.bind Binary.decode

    match response with
    | Right OK -> Either.succeed ()
    | Right (NOK error) ->
      string error
      |> Error.asClientError (tag "requestUnRegister")
      |> Either.fail
    | Right other ->
      sprintf "Unexpected Response from server: %A" other
      |> Error.asClientError (tag "requestUnRegister")
      |> Either.fail
    | Left error ->
      error
      |> Either.fail

  // ** start

  let private start (chan: ReplyChan)
                    (server: IrisServer)
                    (client: IrisClient)
                    (subs: Subscriptions)
                    (agent: ApiAgent) =
    let clientAddr = formatUri client.IpAddress (int client.Port)
    let srvAddr = formatUri server.IpAddress (int server.Port)

    sprintf "Starting server on %s" clientAddr
    |> Logger.debug client.Id (tag "start")

    sprintf "Connecting to server on %s" srvAddr
    |> Logger.debug client.Id (tag "start")

    let server = new Rep(clientAddr, requestHandler agent)
    let socket = new Req(client.Id, srvAddr, Constants.REQ_TIMEOUT)
    socket.Start()

    match server.Start() with
    | Right () ->
      let data =
        { Client = client
          Timer = pingTimer agent
          Elapsed = 0u
          Store = new Store(State.Empty)
          Socket = socket
          Server = server }

      match requestRegister data with
      | Right () ->
        sprintf "Registration with %s successful" srvAddr
        |> Logger.debug client.Id (tag "start")

        chan.Reply(Right Reply.Ok)
        notify subs ClientEvent.Registered
        Loaded data
      | Left error ->
        sprintf "Registration with %s encountered error: %s" srvAddr (string error)
        |> Logger.debug client.Id (tag "start")

        dispose data
        chan.Reply(Left error)
        Idle

    | Left error ->
      sprintf "Error starting sockets: %s" (string error)
      |> Logger.debug client.Id (tag "start")

      chan.Reply(Left error)
      dispose server
      dispose socket
      Idle

  // ** handleStart

  let private handleStart (chan: ReplyChan)
                          (state: ClientState)
                          (server: IrisServer)
                          (client: IrisClient)
                          (subs: Subscriptions)
                          (agent: ApiAgent) =
    match state with
    | Loaded data ->
      dispose data
      start chan server client subs agent
    | Idle ->
      start chan server client subs agent

  // ** handleDispose

  let private handleDispose (chan: ReplyChan) (state: ClientState) =
    match state with
    | Loaded data ->
      match requestUnRegister data with
      | Left error ->
        string error
        |> Logger.err data.Client.Id (tag "handleDispose")
      | _ -> ()
    | _ -> ()
    dispose state
    chan.Reply(Right Reply.Ok)
    Idle

  // ** handleGetState

  let private handleGetState (chan: ReplyChan) (state: ClientState) =
    match state with
    | Loaded data ->
      chan.Reply(Right (Reply.State data.Store.State))
      state
    | Idle ->
      "Not loaded"
      |> Error.asClientError (tag "handleGetState")
      |> Either.fail
      |> chan.Reply
      Idle

  // ** handleGetStatus

  let private handleGetStatus (chan: ReplyChan) (state: ClientState) =
    match state with
    | Loaded data ->
      chan.Reply(Right (Reply.Status data.Client.Status))
      state
    | Idle ->
      chan.Reply(Right (Reply.Status ServiceStatus.Stopped))
      state

  // ** handleSetStatus

  let private handleSetStatus (state: ClientState) (subs: Subscriptions) (status: ServiceStatus) =
    match state with
    | Loaded data ->
      notify subs (ClientEvent.Status status)
      Loaded { data with Client = { data.Client with Status = status } }
    | Idle -> Idle

  // ** handleCheckStatus

  let private handleCheckStatus (state: ClientState) (subs: Subscriptions) =
    match state with
    | Loaded data ->
      if not (Service.hasFailed data.Client.Status) then
        match data.Elapsed with
        | x when x > TIMEOUT ->
          let status =
            "Server ping timed out"
            |> Error.asClientError (tag "handlePing")
            |> ServiceStatus.Failed
          notify subs (ClientEvent.Status status)
          Loaded { data with
                    Client = { data.Client with Status = status}
                    Elapsed = data.Elapsed + FREQ }
        | _ ->
          let status =
            match data.Client.Status with
            | ServiceStatus.Running -> data.Client.Status
            | _ ->
              let newstatus = ServiceStatus.Running
              notify subs (ClientEvent.Status newstatus)
              newstatus
          Loaded { data with
                    Client = { data.Client with Status = status }
                    Elapsed = data.Elapsed + FREQ }
      else
        state
    | idle -> idle

  // ** handlePing

  let private handlePing (state: ClientState) =
    match state with
    | Loaded data -> Loaded { data with Elapsed = 0u }
    | idle -> idle

  // ** handleSetState

  let private handleSetState (state: ClientState) (subs: Subscriptions) (newstate: State) =
    match state with
    | Loaded data ->
      notify subs ClientEvent.Snapshot
      Loaded { data with Store = new Store(newstate) }
    | Idle -> state

  // ** handleUpdate

  let private handleUpdate (state: ClientState) (subs: Subscriptions) (sm: StateMachine) =
    match state with
    | Loaded data ->
      data.Store.Dispatch sm
      notify subs (ClientEvent.Update sm)
      state
    | Idle -> state

  // ** requestUpdate

  let private requestUpdate (socket: Req) (sm: StateMachine) =
    let result : Either<IrisError,ApiResponse> =
      ServerApiRequest.Update sm
      |> Binary.encode
      |> socket.Request
      |> Either.bind Binary.decode

    match result with
    | Right ApiResponse.OK ->
      Either.succeed ()
    | Right other ->
      sprintf "Unexpected reply from Server: %A" other
      |> Error.asClientError (tag "requestUpdate")
      |> Either.fail
    | Left error ->
      error
      |> Either.fail

  // ** handleRequest

  let private handleRequest (chan: ReplyChan)
                            (state: ClientState)
                            (sm: StateMachine)
                            (agent: ApiAgent) =
    match state with
    | Loaded data ->
      match requestUpdate data.Socket sm with
      | Right () ->
        Reply.Ok
        |> Either.succeed
        |> chan.Reply
      | Left error ->
        ServiceStatus.Failed error
        |> Msg.SetStatus
        |> agent.Post
        error
        |> Either.fail
        |> chan.Reply
      state
    | Idle ->
      "Not running"
      |> Error.asClientError (tag "handleRequest")
      |> Either.fail
      |> chan.Reply
      state

  // ** loop

  let private loop (initial: ClientState)
                   (server: IrisServer)
                   (client: IrisClient)
                   (subs: Subscriptions)
                   (inbox: ApiAgent) =
    let rec act (state: ClientState) =
      async {
        let! msg = inbox.Receive()

        let newstate =
          match msg with
          | Msg.Start chan        -> handleStart chan state server client subs inbox
          | Msg.GetState chan     -> handleGetState chan state
          | Msg.SetState newstate -> handleSetState state subs newstate
          | Msg.Dispose chan      -> handleDispose chan state
          | Msg.GetStatus chan    -> handleGetStatus chan state
          | Msg.SetStatus status  -> handleSetStatus state subs status
          | Msg.CheckStatus       -> handleCheckStatus state subs
          | Msg.Ping              -> handlePing state
          | Msg.Update sm         -> handleUpdate state subs sm
          | Msg.Request(chan, sm) -> handleRequest chan state sm inbox

        return! act newstate
      }
    act initial

  // ** postCommand

  let inline private postCommand (agent: ApiAgent) (cb: ReplyChan -> Msg) =
    async {
      let! result = agent.PostAndTryAsyncReply(cb, Constants.COMMAND_TIMEOUT)
      match result with
      | Some response -> return response
      | None ->
        return
          "Command Timeout"
          |> Error.asOther (tag "postCommand")
          |> Either.fail
    }
    |> Async.RunSynchronously

  // ** ApiClient module

  [<RequireQualifiedAccess>]
  module ApiClient =

    // ** create

    let create (server: IrisServer) (client: IrisClient) =
      either {
        let cts = new CancellationTokenSource()
        let subs = new Subscriptions()
        let agent = new ApiAgent(loop Idle server client subs, cts.Token)
        let listener = createListener subs
        agent.Start()

        return
          { new IApiClient with
              member self.Start () =
                match postCommand agent (fun chan -> Msg.Start chan) with
                | Right (Reply.Ok) -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "Start")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.State
                with get () =
                  match postCommand agent (fun chan -> Msg.GetState(chan)) with
                  | Right (Reply.State state) -> Either.succeed state
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "State")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.Status
                with get () =
                  match postCommand agent (fun chan -> Msg.GetStatus chan) with
                  | Right (Reply.Status status) -> status
                  | Right _ -> ServiceStatus.Stopped
                  | Left error -> ServiceStatus.Failed error

              member self.Subscribe (callback: ClientEvent -> unit) =
                { new IObserver<ClientEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.AddCue (cue: Cue) =
                match postCommand agent (fun chan -> Msg.Request(chan, AddCue cue)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "AddCue")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.UpdateCue (cue: Cue) =
                match postCommand agent (fun chan -> Msg.Request(chan, UpdateCue cue)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "UpdateCue")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.RemoveCue (cue: Cue) =
                match postCommand agent (fun chan -> Msg.Request(chan, RemoveCue cue)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "RemoveCue")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.AddCueList (cuelist: CueList) =
                match postCommand agent (fun chan -> Msg.Request(chan, AddCueList cuelist)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "AddCueList")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.UpdateCueList (cuelist: CueList) =
                match postCommand agent (fun chan -> Msg.Request(chan, UpdateCueList cuelist)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "UpdateCueList")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.RemoveCueList (cuelist: CueList) =
                match postCommand agent (fun chan -> Msg.Request(chan, RemoveCueList cuelist)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "RemoveCueList")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.AddPin(pin: Pin) =
                match postCommand agent (fun chan -> Msg.Request(chan, AddPin pin)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "AddPin")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.UpdatePin(pin: Pin) =
                match postCommand agent (fun chan -> Msg.Request(chan, UpdatePin pin)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "UpdatePin")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.RemovePin(pin: Pin) =
                match postCommand agent (fun chan -> Msg.Request(chan, RemovePin pin)) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "RemovePin")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Dispose () =
                postCommand agent Msg.Dispose
                |> ignore
                dispose cts
            }
      }
