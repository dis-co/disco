namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Raft
open Iris.Core
open Iris.Client
open Iris.Zmq
open Iris.Service.Interfaces
open Iris.Serialization
open Hopac
open Hopac.Infixes

// * ApiServer module

[<AutoOpen>]
module ApiServer =

  //  ____       _            _
  // |  _ \ _ __(_)_   ____ _| |_ ___
  // | |_) | '__| \ \ / / _` | __/ _ \
  // |  __/| |  | |\ V / (_| | ||  __/
  // |_|   |_|  |_| \_/ \__,_|\__\___|

  // ** tag

  let private tag (str: string) = sprintf "IApiServer.%s" str

  // ** timeout

  [<Literal>]
  let private timeout = 1000

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<Guid, IObserver<ApiEvent>>

  // ** Client

  [<NoComparison;NoEquality>]
  type private Client =
    { Meta: IrisClient
      Socket: IClient
      Timer: IDisposable }

    interface IDisposable with
      member client.Dispose() =
        dispose client.Timer
        dispose client.Socket

  // ** ServerStateData

  [<NoComparison;NoEquality>]
  type private ServerStateData =
    { Status: ServiceStatus
      Store: Store
      Server: IBroker
      Publisher: Pub
      Subscriber: Sub
      Clients: Map<Id,Client>
      Disposables: IDisposable list }

    interface IDisposable with
      member data.Dispose() =
        List.iter dispose data.Disposables
        dispose data.Publisher
        dispose data.Subscriber
        dispose data.Server
        Map.iter (fun _ v -> dispose v) data.Clients

  // ** ServerState

  [<NoComparison;NoEquality>]
  type private ServerState =
    | Loaded of ServerStateData
    | Idle

    interface IDisposable with
      member state.Dispose() =
        match state with
        | Loaded data -> dispose data
        | _ -> ()

  // ** Reply

  [<RequireQualifiedAccess>]
  type private Reply =
    | Clients of Map<Id,IrisClient>
    | State of State
    | Ok

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start           of chan:ReplyChan * mem:RaftMember * projectId:Id
    | Dispose         of chan:ReplyChan
    | GetClients      of chan:ReplyChan
    | SetStatus       of status:ServiceStatus
    | AddClient       of client:IrisClient
    | RemoveClient    of client:IrisClient
    | SetClientStatus of id:Id          * status:ServiceStatus
    | SetState        of chan:ReplyChan * state:State
    | GetState        of chan:ReplyChan
    | InstallSnapshot of id:Id
    | LocalUpdate     of sm:StateMachine
    | RemoteUpdate    of sm:StateMachine
    | ClientUpdate    of sm:StateMachine
    | RawRequest      of req:RawRequest

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** Listener

  type private Listener = IObservable<ApiEvent>

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

  // ** notify

  let private notify (subs: Subscriptions) (ev: ApiEvent) =
    for KeyValue(_,sub) in subs do
      sub.OnNext ev

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

  // ** requestInstallSnapshot

  let private requestInstallSnapshot (data: ServerStateData) (client: Client) (agent: ApiAgent) =
    let result : Either<IrisError,ApiResponse> =
      data.Store.State
      |> ClientApiRequest.Snapshot
      |> Binary.encode
      |> client.Socket.Request
      |> Either.bind Binary.decode

    match result with
    | Right ApiResponse.OK -> ()

    | Right (ApiResponse.NOK error) ->
      error
      |> string
      |> Error.asClientError (tag "requestInstallSnapshot")
      |> ServiceStatus.Failed
      |> fun reason -> client.Meta.Id, reason
      |> Msg.SetClientStatus
      |> agent.Post

    | Right other ->
      other
      |> sprintf "Unexpected reply from Client %A"
      |> Error.asClientError (tag "requestInstallSnapshot")
      |> ServiceStatus.Failed
      |> fun reason -> client.Meta.Id, reason
      |> Msg.SetClientStatus
      |> agent.Post

    | Left error ->
      error
      |> string
      |> Error.asClientError (tag "requestInstallSnapshot")
      |> ServiceStatus.Failed
      |> fun reason -> client.Meta.Id, reason
      |> Msg.SetClientStatus
      |> agent.Post

  // ** pingTimer

  let private pingTimer (socket: IClient) (agent: ApiAgent) =
    let cts = new CancellationTokenSource()

    let rec loop () =
      async {
        do! Async.Sleep(timeout)

        Tracing.trace "ApiServer.pingRequest" <| fun () ->
          if not socket.Running then
            socket.Restart()

          let response : Either<IrisError,ApiResponse> =
            ClientApiRequest.Ping
            |> Binary.encode
            |> socket.Request
            |> Either.bind Binary.decode

          match response with
          | Right Pong ->
            // ping request successful
            (socket.Id, ServiceStatus.Running)
            |> Msg.SetClientStatus
            |> agent.Post

          | Left error ->
            // log this error
            string error
            |> sprintf "error during to %s: %s" (string socket.Id)
            |> Logger.err (tag "pingTimer")

            // set the status of this client to error
            (socket.Id, ServiceStatus.Failed error)
            |> Msg.SetClientStatus
            |> agent.Post
          | _ -> ()

        return! loop ()
      }

    Async.Start(loop (), cts.Token)
    { new IDisposable with
        member self.Dispose () =
          cts.Cancel() }


  // ** processSubscriptionEvent

  let private processSubscriptionEvent (agent: ApiAgent) (bytes: byte array) =
    match Binary.decode bytes with
    | Right command ->
      match command with
      | UpdateSlices _ | CallCue _ ->
        command
        |> Msg.RemoteUpdate
        |> agent.Post
      | _ -> ()
    | _ -> ()

  // ** start

  let private start (chan: ReplyChan) (agent: ApiAgent) (mem: RaftMember) (project: Id) =
    let frontend = Uri.tcpUri mem.IpAddr (mem.ApiPort |> port |> Some)
    let backend = Uri.inprocUri Constants.API_BACKEND_PREFIX (mem.Id |> string |> Some)

    let pubSubAddr =
      Uri.epgmUri
        mem.IpAddr
        (IPv4Address Constants.MCAST_ADDRESS)
        (port Constants.MCAST_PORT)

    let publisher = new Pub(pubSubAddr, string project)
    let subscriber = new Sub(pubSubAddr, string project)

    let result = Broker.create {
      Id = mem.Id
      MinWorkers = 5uy
      MaxWorkers = 20uy
      Frontend = frontend
      Backend = backend
      RequestTimeout = uint32 Constants.REQ_TIMEOUT
    }

    match result  with
    | Right server ->
      match publisher.Start(), subscriber.Start() with
      | Right (), Right () ->
        let srv = server.Subscribe (Msg.RawRequest >> agent.Post)
        let sub = subscriber.Subscribe(processSubscriptionEvent agent)

        Reply.Ok
        |> Either.succeed
        |> chan.Reply

        Loaded { Status = ServiceStatus.Running
                 Store = new Store(State.Empty)
                 Publisher = publisher
                 Subscriber = subscriber
                 Clients = Map.empty
                 Server = server
                 Disposables = [ srv; sub ] }

      | Left error, _ | _, Left error ->
        dispose server
        dispose publisher
        dispose subscriber
        chan.Reply(Left error)
        Idle

    | Left error ->
      error
      |> Either.fail
      |> chan.Reply
      Idle

  // ** handleStart

  let private handleStart (chan: ReplyChan)
                          (state: ServerState)
                          (agent: ApiAgent)
                          (mem: RaftMember)
                          (project: Id) =
    Tracing.trace "ApiServer.handleStart" <| fun () ->
      match state with
      | Loaded data ->
        dispose data
        start chan agent mem project
      | Idle ->
        start chan agent mem project

  // ** handleDispose

  let private handleDispose (chan: ReplyChan) (state: ServerState) =
    Tracing.trace "ApiServer.handleDispose" <| fun () ->
      dispose state
      Reply.Ok
      |> Either.succeed
      |> chan.Reply
      Idle

  // ** handleAddClient

  let private handleAddClient (state: ServerState)
                              (subs: Subscriptions)
                              (meta: IrisClient)
                              (agent: ApiAgent) =
    Tracing.trace "ApiServer.handleAddClient" <| fun () ->
      match state with
      | Idle -> state
      | Loaded data ->
        // first, dispose of the previous client
        match Map.tryFind meta.Id data.Clients with
        | None -> ()
        | Some client ->
          dispose client
          notify subs (ApiEvent.UnRegister client.Meta)

        // construct a new client value
        let addr = Uri.tcpUri meta.IpAddress (Some meta.Port)
        let socket = Client.create meta.Id addr Constants.REQ_TIMEOUT

        let client =
          { Meta = meta
            Socket = socket
            Timer = pingTimer socket agent }

        asynchronously <| fun _ ->
          meta.Id
          |> Msg.InstallSnapshot
          |> agent.Post

          meta
          |> ApiEvent.Register
          |> notify subs

        Loaded { data with Clients = Map.add meta.Id client data.Clients }

  // ** handleRemoveClient

  let private handleRemoveClient (state: ServerState)
                                 (subs: Subscriptions)
                                 (peer: IrisClient) =
    Tracing.trace "ApiServer.handleRemoveClient" <| fun () ->
      match state with
      | Idle -> state
      | Loaded data ->
        match Map.tryFind peer.Id data.Clients with
        | Some client ->
          asynchronously <| fun _ ->
            dispose client
            peer
            |> ApiEvent.UnRegister
            |> notify subs
          Loaded { data with Clients = Map.remove peer.Id data.Clients }
        | _ -> state

  // ** updateClient

  let private updateClient (sm: StateMachine) (client: Client) =
    job {
      if not client.Socket.Running then
        client.Socket.Restart()

      let result : Either<IrisError,ApiResponse> =
        Tracing.trace "ApiServer.updateClient" <| fun () ->
          sm
          |> ClientApiRequest.Update
          |> Binary.encode
          |> client.Socket.Request
          |> Either.bind Binary.decode

      match result with
      | Right ApiResponse.OK | Right ApiResponse.Pong ->
        return Either.succeed ()
      | Right (ApiResponse.NOK err) ->
        let error =
          string err
          |> Error.asClientError (tag "updateClient")
        return  Either.fail (client.Meta.Id, error)
      | Left error ->
        return Either.fail (client.Meta.Id, error)
    }

  // ** updateClients

  let private updateClients (data: ServerStateData) (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace "ApiServer.updateClients" <| fun () ->
      data.Clients
      |> Map.toArray
      |> Array.map (snd >> updateClient sm)
      |> Job.conCollect
      |> Hopac.run
      |> fun arr -> arr.ToArray()
      |> Array.iter
        (fun result ->
          match result with
          | Left (id, error) ->
            (id, ServiceStatus.Failed error)
            |> Msg.SetClientStatus
            |> agent.Post
          | _ -> ())

  // ** maybePublish

  let private maybePublish (data: ServerStateData) (sm: StateMachine) (agent: ApiAgent) =
    match sm with
    | UpdateSlices _ | CallCue _ ->
      asynchronously <| fun () ->
        Tracing.trace "ApiServer.maybePublish" <| fun () ->
          sm
          |> Binary.encode
          |> data.Publisher.Publish
          |> Either.mapError (ServiceStatus.Failed >> Msg.SetStatus >> agent.Post)
          |> ignore
    | _ -> ()

  // ** maybeDispatch

  let private maybeDispatch (data: ServerStateData) (sm: StateMachine) =
    match sm with
    | UpdateSlices _ | CallCue _ -> data.Store.Dispatch sm
    | _ -> ()

  // ** handleGetClients

  let private handleGetClients (chan: ReplyChan) (state: ServerState) =
    Tracing.trace "ApiServer.handleGetClients" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ ->
          data.Clients
          |> Map.map (fun _ v -> v.Meta)
          |> Reply.Clients
          |> Either.succeed
          |> chan.Reply
        state
      | Idle ->
        asynchronously <| fun _ ->
          "ClientApi not running"
          |> Error.asClientError (tag "handleGetClients")
          |> Either.fail
          |> chan.Reply
        state

  // ** handleSetStatus

  let private handleSetStatus (state: ServerState)
                              (subs: Subscriptions)
                              (status: ServiceStatus) =
    Tracing.trace "ApiServer.handleSetStatus" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ ->
          notify subs (ApiEvent.ServiceStatus status)
        Loaded { data with Status = status }
      | idle -> idle

  // ** handleSetClientStatus

  let private handleSetClientStatus (state: ServerState)
                                    (subs: Subscriptions)
                                    (id: Id)
                                    (status: ServiceStatus) =
    Tracing.trace "ApiServer.handleSetClientStatus" <| fun () ->
      match state with
      | Loaded data ->
        match Map.tryFind id data.Clients with
        | Some client ->
          match client.Meta.Status, status with
          | ServiceStatus.Running, ServiceStatus.Running -> state
          | oldst, newst ->
            if oldst <> newst then
              let updated = { client with Meta = { client.Meta with Status = status } }
              asynchronously <| fun () ->
                updated.Meta
                |> ApiEvent.ClientStatus
                |> notify subs
              Loaded { data with Clients = Map.add id updated data.Clients }
            else state
        | None -> state
      | idle -> idle

  // ** handleSetState

  let private handleSetState (chan: ReplyChan)
                             (state: ServerState)
                             (newstate: State)
                             (agent: ApiAgent) =

    Tracing.trace "ApiServer.handleSetState" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ ->
          Map.iter (fun id _ -> id |> Msg.InstallSnapshot |> agent.Post) data.Clients
          Reply.Ok
          |> Either.succeed
          |> chan.Reply
        Loaded { data with Store = new Store(newstate) }
      | Idle ->
        chan.Reply (Right Reply.Ok)
        state

  // ** handleInstallSnapshot

  let private handleInstallSnapshot (state: ServerState) (id: Id) (agent: ApiAgent) =
    Tracing.trace "ApiServer.handleInstallSnapshot" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ ->
          match Map.tryFind id data.Clients with
          | Some client ->
            requestInstallSnapshot data client agent
          | None -> ()
        state
      | Idle -> state

  // ** handleGetState

  let private handleGetState (chan: ReplyChan) (state: ServerState) =
    Tracing.trace "ApiServer.handleGetState" <| fun () ->
      match state with
      | Loaded data ->
        asynchronously <| fun _ ->
          data.Store.State
          |> Reply.State
          |> Either.succeed
          |> chan.Reply
        state
      | _ ->
        asynchronously <| fun _ ->
          "Not Loaded"
          |> Error.asClientError (tag "handleGetState")
          |> Either.fail
          |> chan.Reply
        state

  // ** handleClientUpdate

  let private handleClientUpdate (state: ServerState)
                                 (subs: Subscriptions)
                                 (sm: StateMachine)
                                 (agent: ApiAgent) =
    Tracing.trace "ApiServer.handleClientUpdate" <| fun () ->
      match state with
      | Loaded data ->
        maybeDispatch data sm
        maybePublish data sm agent
        notify subs (ApiEvent.Update sm)
        state
      | Idle ->
        state

  // ** handleRemoteUpdate

  let private handleRemoteUpdate (state: ServerState)
                                 (subs: Subscriptions)
                                 (sm: StateMachine)
                                 (agent: ApiAgent) =
    Tracing.trace "ApiServer.handleRemoteUpdate" <| fun () ->
      match state with
      | Loaded data ->
        maybeDispatch data sm             // we need to send these request synchronously
        updateClients data sm agent       // in order to preserve ordering of the messages
        notify subs (ApiEvent.Update sm)
        state
      | Idle ->
        state

  // ** handleLocalUpdate

  let private handleLocalUpdate (state: ServerState)
                                (sm: StateMachine)
                                (agent: ApiAgent) =
    Tracing.trace "ApiServer.handleLocalUpdate" <| fun () ->
      match state with
      | Loaded data ->
        data.Store.Dispatch sm            // we need to send these request synchronously
        maybePublish data sm agent        // in order to preserve ordering of the messages
        updateClients data sm agent
        state
      | Idle -> state

  // ** handleRawRequest

  let private handleRawRequest (state: ServerState) (req: RawRequest) (agent: ApiAgent) =
    match state with
    | Idle -> state
    | Loaded data ->
      asynchronously <| fun _ ->
        Tracing.trace "ApiServer.handleRawRequest" <| fun () ->
          match req.Body |> Binary.decode with
          | Right (Register client) ->
            client
            |> Msg.AddClient
            |> agent.Post

            OK
            |> Binary.encode
            |> RawResponse.fromRequest req
            |> data.Server.Respond

          | Right (UnRegister client) ->
            client
            |> Msg.RemoveClient
            |> agent.Post

            OK
            |> Binary.encode
            |> RawResponse.fromRequest req
            |> data.Server.Respond

          | Right (Update sm) ->
            sm
            |> Msg.ClientUpdate
            |> agent.Post

            OK
            |> Binary.encode
            |> RawResponse.fromRequest req
            |> data.Server.Respond

          | Left error ->
            string error
            |> ApiError.Internal
            |> NOK
            |> Binary.encode
            |> RawResponse.fromRequest req
            |> data.Server.Respond
      state


  // ** loop

  let private loop (initial: ServerState) (subs: Subscriptions) (inbox: ApiAgent) =
    let rec act (state: ServerState) =
      async {
        let! msg = inbox.Receive()

        let newstate =
          try
            match msg with
            | Msg.Start(chan,mem,projectId)   -> handleStart chan state inbox mem projectId
            | Msg.Dispose chan                -> handleDispose chan state
            | Msg.AddClient(client)           -> handleAddClient state subs client inbox
            | Msg.RemoveClient(client)        -> handleRemoveClient state subs client
            | Msg.GetClients(chan)            -> handleGetClients chan state
            | Msg.SetStatus(status)           -> handleSetStatus state subs status
            | Msg.SetClientStatus(id, status) -> handleSetClientStatus state subs id status
            | Msg.SetState(chan, newstate)    -> handleSetState chan state newstate inbox
            | Msg.GetState(chan)              -> handleGetState chan state
            | Msg.InstallSnapshot(id)         -> handleInstallSnapshot state id inbox
            | Msg.LocalUpdate(sm)             -> handleLocalUpdate state sm inbox
            | Msg.ClientUpdate(sm)            -> handleClientUpdate state subs sm inbox
            | Msg.RemoteUpdate(sm)            -> handleRemoteUpdate state subs sm inbox
            | Msg.RawRequest(req)             -> handleRawRequest state req inbox
          with
            | exn ->
              exn.Message + exn.StackTrace
              |> sprintf "Error in loop: %O"
              |> Logger.err "ApiServer"
              state

        return! act newstate
      }
    act initial

  //  ____        _     _ _
  // |  _ \ _   _| |__ | (_) ___
  // | |_) | | | | '_ \| | |/ __|
  // |  __/| |_| | |_) | | | (__
  // |_|    \__,_|_.__/|_|_|\___|

  // ** ApiServer module

  [<RequireQualifiedAccess>]
  module ApiServer =

    let create (mem: RaftMember) (projectId: Id) =
      either {
        let cts = new CancellationTokenSource()
        let subs = new Subscriptions()
        let agent = new ApiAgent(loop Idle subs, cts.Token)
        agent.Start()

        return
          { new IApiServer with
              member self.Start () =
                match postCommand agent (fun chan -> Msg.Start(chan,mem,projectId)) with
                | Right (Reply.Ok) -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "Start")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Clients
                with get () =
                  match postCommand agent (fun chan -> Msg.GetClients(chan)) with
                  | Right (Reply.Clients clients) -> Either.succeed clients
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "Clients")
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

              member self.Update (sm: StateMachine) =
                agent.Post(Msg.LocalUpdate sm)

              member self.SetState (state: State) =
                match postCommand agent (fun chan -> Msg.SetState(chan, state)) with
                | Right (Reply.Ok) -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "SetState")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Subscribe (callback: ApiEvent -> unit) =
                let guid = Guid.NewGuid()
                let listener = createListener guid subs
                { new IObserver<ApiEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Dispose () =
                postCommand agent (fun chan -> Msg.Dispose chan)
                |> ignore
                dispose cts
            }
      }
