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

  type private Subscriptions = ConcurrentDictionary<Guid, IObserver<ClientEvent>>

  // ** ClientStateData

  [<NoComparison;NoEquality>]
  type private ClientStateData =
    { Client: IrisClient
      Elapsed: uint32
      Server: IBroker
      Socket: IClient
      Store:  Store
      Disposables: IDisposable list }

    interface IDisposable with
      member self.Dispose() =
        List.iter dispose self.Disposables
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
    | Start         of chan:ReplyChan
    | GetStatus     of chan:ReplyChan
    | SetStatus     of status:ServiceStatus
    | Dispose       of chan:ReplyChan
    | AsyncDispose
    | GetState      of chan:ReplyChan
    | SetState      of state:State
    | Update        of sm:StateMachine
    | Request       of chan:ReplyChan * sm:StateMachine
    | ServerRequest of req:RawServerRequest

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
    for KeyValue(_,sub) in subs do
      sub.OnNext ev

  // ** requestRegister

  let private requestRegister (data: ClientStateData) =
    data.Client
    |> ServerApiRequest.Register
    |> Binary.encode
    |> fun body -> { Body = body }
    |> data.Socket.Request
    |> Either.mapError (string >> Logger.err "requestRegister")
    |> ignore

    // match response with
    // | Right OK -> Either.succeed ()
    // | Right (NOK error) ->
    //   string error
    //   |> Error.asClientError (tag "requestRegister")
    //   |> Either.fail
    // | Right other ->
    //   sprintf "Unexpected Response from server: %A" other
    //   |> Error.asClientError (tag "requestRegister")
    //   |> Either.fail
    // | Left error ->
    //   error
    //   |> Either.fail

  // ** requestUnRegister

  let private requestUnRegister (data: ClientStateData) =
    data.Client
    |> ServerApiRequest.UnRegister
    |> Binary.encode
    |> fun body -> { Body = body }
    |> data.Socket.Request
    |> Either.mapError (string >> Logger.err "requestUnregister")
    |> ignore

    // match response with
    // | Right OK -> Either.succeed ()
    // | Right (NOK error) ->
    //   string error
    //   |> Error.asClientError (tag "requestUnRegister")
    //   |> Either.fail
    // | Right other ->
    //   sprintf "Unexpected Response from server: %A" other
    //   |> Error.asClientError (tag "requestUnRegister")
    //   |> Either.fail
    // | Left error ->
    //   error
    //   |> Either.fail

  // ** start

  let private start (chan: ReplyChan)
                    (server: IrisServer)
                    (client: IrisClient)
                    (subs: Subscriptions)
                    (agent: ApiAgent) =

    Tracing.trace "ApiClient.start" <| fun () ->
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

      let socket = Client.create {
        PeerId = client.Id
        Frontend = srvAddr
        Timeout = int Constants.REQ_TIMEOUT * 1<ms>
      }

      let result = Broker.create {
        Id = client.Id
        MinWorkers = 5uy
        MaxWorkers = 20uy
        Frontend = clientAddr
        Backend = backendAddr
        RequestTimeout = int Constants.REQ_TIMEOUT * 1<ms>
      }

      match result with
      | Right server ->
        let disposable = server.Subscribe (Msg.ServerRequest >> agent.Post)

        let timer = pingTimer agent

        let data =
          { Elapsed = 0u
            Client = client
            Socket = socket
            Server = server
            Store = new Store(State.Empty)
            Disposables = [ timer; disposable ] }

        sprintf "Connecting to server on %O" srvAddr
        |> Logger.debug (tag "start")

        requestRegister data

        // | Right () ->
        //   srvAddr
        //   |> sprintf "Registration with %O successful"
        //   |> Logger.debug (tag "start")

        //   Reply.Ok
        //   |> Either.succeed
        //   |>  chan.Reply

        //   notify subs ClientEvent.Registered

        // | Left error ->
        //   error
        //   |> sprintf "Registration with %O encountered error: %O" srvAddr
        //   |> Logger.debug (tag "start")

        //   Msg.AsyncDispose
        //   |> agent.Post

        //   error
        //   |> Either.fail
        //   |> chan.Reply

        Loaded data

      | Left error ->
        error
        |> string
        |> sprintf "Error starting sockets: %s"
        |> Logger.debug (tag "start")

        error
        |> Either.fail
        |> chan.Reply

        dispose socket
        Idle

  // ** handleStart

  let private handleStart (chan: ReplyChan)
                          (state: ClientState)
                          (server: IrisServer)
                          (client: IrisClient)
                          (subs: Subscriptions)
                          (agent: ApiAgent) =
    Tracing.trace "ApiClient.handleStart" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ -> dispose data
        start chan server client subs agent
      | Idle ->
        start chan server client subs agent

  // ** handleDispose

  let private handleDispose (chan: ReplyChan) (state: ClientState) =
    Tracing.trace "ApiClient.handleDispose" <| fun () ->
      match state with
      | Loaded data -> requestUnRegister data
      | _ -> ()

      dispose state

      Reply.Ok
      |> Either.succeed
      |> chan.Reply
      Idle

  // ** handleAsyncDispose

  let private handleAsyncDispose (state: ClientState) =
    Tracing.trace "ApiClient.handleAsyncDispose" <| fun () ->
      asynchronously <| fun _ ->
        Tracing.trace "ApiClient.handleAsyncDispose.asynchronously" <| fun () ->
          match state with
          | Loaded data -> requestUnRegister data
          | _ -> ()

          dispose state
    Idle

  // ** handleGetState

  let private handleGetState (chan: ReplyChan) (state: ClientState) =
    Tracing.trace "ApiClient.handleGetState" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ ->
          Tracing.trace "ApiClient.handleGetState.reply" <| fun () ->
            data.Store.State
            |> Reply.State
            |> Either.succeed
            |> chan.Reply
        state
      | Idle ->
        asynchronously <| fun _ ->
          Tracing.trace "ApiClient.handleGetState.error" <| fun () ->
            "Not loaded"
            |> Error.asClientError (tag "handleGetState")
            |> Either.fail
            |> chan.Reply
        Idle

  // ** handleGetStatus

  let private handleGetStatus (chan: ReplyChan) (state: ClientState) =
    Tracing.trace "ApiClient.handleGetStatus" <| fun () ->
      match state with
      | Loaded data ->
        chan.Reply(Right (Reply.Status data.Client.Status))
        state
      | Idle ->
        chan.Reply(Right (Reply.Status ServiceStatus.Stopped))
        state

  // ** handleSetStatus

  let private handleSetStatus (state: ClientState) (subs: Subscriptions) (status: ServiceStatus) =
    Tracing.trace "ApiClient.handleSetStatus" <| fun () ->
      match state with
      | Loaded data ->
        notify subs (ClientEvent.Status status)
        Loaded { data with Client = { data.Client with Status = status } }
      | Idle -> Idle

  // ** handleCheckStatus

  let private handleCheckStatus (state: ClientState) (subs: Subscriptions) =
    Tracing.trace "ApiClient.handleCheckStatus" <| fun () ->
      match state with
      | Loaded data ->
        if not (Service.hasFailed data.Client.Status) then
          match data.Elapsed with
          | x when x > TIMEOUT ->
            let status =
              "Server ping timed out"
              |> Error.asClientError (tag "handleCheckStatus")
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
    Tracing.trace "ApiClient.handlePing" <| fun () ->
      match state with
      | Loaded data -> Loaded { data with Elapsed = 0u }
      | idle -> idle

  // ** handleSetState

  let private handleSetState (state: ClientState) (subs: Subscriptions) (newstate: State) =
    Tracing.trace "ApiClient.handleSetState" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ ->
          notify subs ClientEvent.Snapshot
        Loaded { data with Store = new Store(newstate) }
      | Idle -> state

  // ** handleUpdate

  let private handleUpdate (state: ClientState) (subs: Subscriptions) (sm: StateMachine) =
    Tracing.trace "ApiClient.handleUpdate" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ ->
          data.Store.Dispatch sm
          notify subs (ClientEvent.Update sm)
        state
      | Idle -> state

  // ** requestUpdate

  let private requestUpdate (socket: IClient) (sm: StateMachine) =
    ServerApiRequest.Update sm
    |> Binary.encode
    |> fun body -> { Body = body }
    |> socket.Request
    |> Either.mapError (string >> Logger.err "requestUpdate")
    |> ignore

    // match result with
    // | Right ApiResponse.OK ->
    //   Either.succeed ()
    // | Right other ->
    //   sprintf "Unexpected reply from Server: %A" other
    //   |> Error.asClientError (tag "requestUpdate")
    //   |> Either.fail
    // | Left error ->
    //   error
    //   |> Either.fail

  // ** maybeDispatch

  let private maybeDispatch (data: ClientStateData) (sm: StateMachine) =
    Tracing.trace "ApiClient.maybeDispatch" <| fun () ->
      match sm with
      | UpdateSlices _ -> data.Store.Dispatch sm
      | _ -> ()

  // ** handleRequest

  let private handleRequest (chan: ReplyChan)
                            (state: ClientState)
                            (sm: StateMachine)
                            (agent: ApiAgent) =
    match state with
    | Loaded data ->
      maybeDispatch data sm
      requestUpdate data.Socket sm
      // | Right () ->
      //   Reply.Ok
      //   |> Either.succeed
      //   |> chan.Reply

      // | Left error ->
      //   error
      //   |> ServiceStatus.Failed
      //   |> Msg.SetStatus
      //   |> agent.Post

      //   error
      //   |> Either.fail
      // |> chan.Reply
      state
    | Idle ->
      asynchronously <| fun _ ->
        "Not running"
        |> Error.asClientError (tag "handleRequest")
        |> Either.fail
        |> chan.Reply
      state

  // ** handleServerRequest

  let private handleServerRequest (state: ClientState) (req: RawServerRequest) (agent: ApiAgent) =
      match state with
      | Idle -> state
      | Loaded data ->
        match req.Body |> Binary.decode with
        | Right ClientApiRequest.Ping ->
          Msg.Ping
          |> agent.Post

          ApiResponse.Pong
          |> Binary.encode
          |> RawServerResponse.fromRequest req
          |> data.Server.Respond

        | Right (ClientApiRequest.Snapshot snapshot) ->
          snapshot
          |> Msg.SetState
          |> agent.Post

          ApiResponse.OK
          |> Binary.encode
          |> RawServerResponse.fromRequest req
          |> data.Server.Respond

        | Right (ClientApiRequest.Update sm) ->
          sm
          |> Msg.Update
          |> agent.Post

          ApiResponse.OK
          |> Binary.encode
          |> RawServerResponse.fromRequest req
          |> data.Server.Respond

        | Left error ->
          error
          |> string
          |> ApiError.MalformedRequest
          |> ApiResponse.NOK
          |> Binary.encode
          |> RawServerResponse.fromRequest req
          |> data.Server.Respond

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
          | Msg.AsyncDispose      -> handleAsyncDispose state
          | Msg.Dispose chan      -> handleDispose chan state
          | Msg.GetStatus chan    -> handleGetStatus chan state
          | Msg.SetStatus status  -> handleSetStatus state subs status
          | Msg.CheckStatus       -> handleCheckStatus state subs
          | Msg.Ping              -> handlePing state
          | Msg.Update sm         -> handleUpdate state subs sm
          | Msg.Request(chan, sm) -> handleRequest chan state sm inbox
          | Msg.ServerRequest req -> handleServerRequest state req inbox

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
        agent.Start()

        return
          { new IApiClient with
              member self.Start () =
                Tracing.trace "ApiClient.Start()" <| fun () ->
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
                  Tracing.trace "ApiClient.State" <| fun () ->
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
                  Tracing.trace "ApiClient.Status" <| fun () ->
                    match postCommand agent (fun chan -> Msg.GetStatus chan) with
                    | Right (Reply.Status status) -> status
                    | Right _ -> ServiceStatus.Stopped
                    | Left error -> ServiceStatus.Failed error

              member self.Subscribe (callback: ClientEvent -> unit) =
                let guid = Guid.NewGuid()
                let listener = createListener guid subs
                { new IObserver<ClientEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              //   ____
              //  / ___|   _  ___
              // | |  | | | |/ _ \
              // | |__| |_| |  __/
              //  \____\__,_|\___|

              member self.AddCue (cue: Cue) =
                Tracing.trace "ApiClient.AddCue" <| fun () ->
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
                Tracing.trace "ApiClient.UpdateCue" <| fun () ->
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
                Tracing.trace "ApiClient.RemoveCue" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, RemoveCue cue)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "RemoveCue")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              //  ____       _       _
              // |  _ \ __ _| |_ ___| |__
              // | |_) / _` | __/ __| '_ \
              // |  __/ (_| | || (__| | | |
              // |_|   \__,_|\__\___|_| |_|

              member self.AddPinGroup (group: PinGroup) =
                Tracing.trace "ApiClient.AddPinGroup" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, AddPinGroup group)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "AddPinGroup")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.UpdatePinGroup (group: PinGroup) =
                Tracing.trace "ApiClient.UpdatePinGroup" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, UpdatePinGroup group)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "UpdatePinGroup")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.RemovePinGroup (group: PinGroup) =
                Tracing.trace "ApiClient.RemovePinGroup" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, RemovePinGroup group)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "RemovePinGroup")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              //   ____           _     _     _
              //  / ___|   _  ___| |   (_)___| |_
              // | |  | | | |/ _ \ |   | / __| __|
              // | |__| |_| |  __/ |___| \__ \ |_
              //  \____\__,_|\___|_____|_|___/\__|

              member self.AddCueList (cuelist: CueList) =
                Tracing.trace "ApiClient.AddCueList" <| fun () ->
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
                Tracing.trace "ApiClient.UpdateCueList" <| fun () ->
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
                Tracing.trace "ApiClient.RemoveCueList" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, RemoveCueList cuelist)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "RemoveCueList")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              //  ____  _
              // |  _ \(_)_ __
              // | |_) | | '_ \
              // |  __/| | | | |
              // |_|   |_|_| |_|

              member self.AddPin(pin: Pin) =
                Tracing.trace "ApiClient.AddPin" <| fun () ->
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
                Tracing.trace "ApiClient.UpdatePin" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, UpdatePin pin)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "UpdatePin")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.UpdateSlices(slices: Slices) =
                Tracing.trace "ApiClient.UpdateSlices" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, UpdateSlices slices)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "UpdatePin")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.RemovePin(pin: Pin) =
                Tracing.trace "ApiClient.RemovePin" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, RemovePin pin)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "RemovePin")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.Append(cmd: StateMachine) =
                Tracing.trace "ApiClient.Append" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Request(chan, cmd)) with
                  | Right Reply.Ok -> Either.succeed ()
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "RemovePin")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              //  ____  _
              // |  _ \(_)___ _ __   ___  ___  ___
              // | | | | / __| '_ \ / _ \/ __|/ _ \
              // | |_| | \__ \ |_) | (_) \__ \  __/
              // |____/|_|___/ .__/ \___/|___/\___|
              //             |_|

              member self.Dispose () =
                postCommand agent Msg.Dispose
                |> ignore
                dispose cts
            }
      }
