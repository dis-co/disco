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

  type private Subscriptions = ConcurrentDictionary<int, IObserver<ApiEvent>>

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
      let reason =
        string error
        |> Error.asClientError (tag "requestInstallSnapshot")
      (client.Meta.Id, ServiceStatus.Failed reason)
      |> Msg.SetClientStatus
      |> agent.Post
    | Right other ->
      let reason =
        sprintf "Unexpected reply from Client %A" other
        |> Error.asClientError (tag "requestInstallSnapshot")
      (client.Meta.Id, ServiceStatus.Failed reason)
      |> Msg.SetClientStatus
      |> agent.Post
    | Left error ->
      let reason =
        string error
        |> Error.asClientError (tag "requestInstallSnapshot")
      (client.Meta.Id, ServiceStatus.Failed reason)
      |> Msg.SetClientStatus
      |> agent.Post

  // ** pingTimer

  let private pingTimer (socket: IClient) (agent: ApiAgent) =
    let cts = new CancellationTokenSource()

    let rec loop () =
      async {
        do! Async.Sleep(timeout)

        if not socket.Running then
          socket.Restart()

        let response : Either<IrisError,ApiResponse> =
          ClientApiRequest.Ping
          |> Binary.encode
          |> socket.Request
          |> Either.bind Binary.decode

        match response with
        | Right Pong ->
          (socket.Id, ServiceStatus.Running)
          |> Msg.SetClientStatus
          |> agent.Post
        | Left error ->
          string error
          |> sprintf "error during
           to %s: %s" (string socket.Id)
          |> Logger.debug socket.Id (tag "pingTimer") //
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

  let private start (chan: ReplyChan) (agent: ApiAgent) (mem: RaftMember) (id: Id) =
    let frontend = formatTCPUri mem.IpAddr (int mem.ApiPort)
    let backend = Constants.API_BACKEND_PREFIX + string mem.Id
    let pubSubAddr = formatEPGMUri mem.IpAddr (IPv4Address Constants.MCAST_ADDRESS) Constants.MCAST_PORT

    let publisher = new Pub(mem.Id, pubSubAddr, string id)
    let subscriber = new Sub(mem.Id, pubSubAddr, string id)

    subscriber.Subscribe(processSubscriptionEvent agent)
    |> ignore                            // gets cleaned up during Dispose

    match Broker.create mem.Id 5 frontend backend with
    | Right server ->
      let disposable = server.Subscribe (Msg.RawRequest >> agent.Post)
      match publisher.Start(), subscriber.Start() with
      | Right (), Right () ->
        chan.Reply(Right Reply.Ok)
        Loaded { Status = ServiceStatus.Running
                 Store = new Store(State.Empty)
                 Publisher = publisher
                 Subscriber = subscriber
                 Clients = Map.empty
                 Server = server
                 Disposables = [ disposable ] }
      | Left error, _ | _, Left error ->
        dispose server
        dispose publisher
        dispose subscriber
        chan.Reply(Left error)
        Idle

    | Left error ->
      chan.Reply(Left error)
      Idle

  // ** handleStart

  let private handleStart (chan: ReplyChan)
                          (state: ServerState)
                          (agent: ApiAgent)
                          (mem: RaftMember)
                          (projectId: Id) =
    match state with
    | Loaded data ->
      job {
        dispose data
      } |> Hopac.start
      start chan agent mem projectId
    | Idle ->
      start chan agent mem projectId

  // ** handleDispose

  let private handleDispose (chan: ReplyChan) (state: ServerState) =
    job {
      dispose state
      chan.Reply(Right Reply.Ok)
    } |> Hopac.start
    Idle

  // ** handleAddClient

  let private handleAddClient (state: ServerState)
                              (subs: Subscriptions)
                              (meta: IrisClient)
                              (agent: ApiAgent) =
    match state with
    | Idle -> state
    | Loaded data ->
      // first, dispose of the previous client
      match Map.tryFind meta.Id data.Clients with
      | Some client ->
        job {
          dispose client
          notify subs (ApiEvent.UnRegister client.Meta)
        } |> Hopac.start
      | None -> ()

      // construct a new client value
      let addr = formatTCPUri meta.IpAddress (int meta.Port)
      let socket = Client.create meta.Id addr

      let client =
        { Meta = meta
          Socket = socket
          Timer = pingTimer socket agent }

      job {
        meta.Id |> Msg.InstallSnapshot |> agent.Post
        notify subs (ApiEvent.Register meta)
      } |> Hopac.start

      Loaded { data with Clients = Map.add meta.Id client data.Clients }

  // ** handleRemoveClient

  let private handleRemoveClient (state: ServerState)
                                 (subs: Subscriptions)
                                 (peer: IrisClient) =
    match state with
    | Idle -> state
    | Loaded data ->
      match Map.tryFind peer.Id data.Clients with
      | Some client ->
        job {
          dispose client
          peer
          |> ApiEvent.UnRegister
          |> notify subs
        } |> Hopac.start
        Loaded { data with Clients = Map.remove peer.Id data.Clients }
      | _ -> state

  // ** updateClient

  let private updateClient (sm: StateMachine) (client: Client) =
    async {
      if not client.Socket.Running then
        client.Socket.Restart()

      let result : Either<IrisError,ApiResponse> =
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
    data.Clients
    |> Map.toArray
    |> Array.map (snd >> updateClient sm)
    |> Async.Parallel
    |> Async.RunSynchronously
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
    match state with
    | Loaded data ->
      job {
        data.Clients
        |> Map.map (fun _ v -> v.Meta)
        |> Reply.Clients
        |> Either.succeed
        |> chan.Reply
      } |> Hopac.start
      state
    | Idle ->
      job {
        "ClientApi not running"
        |> Error.asClientError (tag "handleGetClients")
        |> Either.fail
        |> chan.Reply
      } |> Hopac.start
      state

  // ** handleSetStatus

  let private handleSetStatus (state: ServerState)
                              (subs: Subscriptions)
                              (status: ServiceStatus) =
    match state with
    | Loaded data ->
      job {
        notify subs (ApiEvent.ServiceStatus status)
      } |> Hopac.start
      Loaded { data with Status = status }
    | idle -> idle

  // ** handleSetClientStatus

  let private handleSetClientStatus (state: ServerState)
                                    (subs: Subscriptions)
                                    (id: Id)
                                    (status: ServiceStatus) =
    match state with
    | Loaded data ->
      match Map.tryFind id data.Clients with
      | Some client ->
        match client.Meta.Status, status with
        | ServiceStatus.Running, ServiceStatus.Running ->
          state
        | oldst, newst ->
          if oldst <> newst then
            let updated = { client with Meta = { client.Meta with Status = status } }
            notify subs (ApiEvent.ClientStatus updated.Meta)
            Loaded { data with Clients = Map.add id updated data.Clients }
          else
            state
      | None -> state
    | idle -> idle

  // ** handleSetState

  let private handleSetState (chan: ReplyChan)
                             (state: ServerState)
                             (newstate: State)
                             (agent: ApiAgent) =
    match state with
    | Loaded data ->
      job {
        Map.iter (fun id _ -> id |> Msg.InstallSnapshot |> agent.Post) data.Clients
        chan.Reply (Right Reply.Ok)
      } |> Hopac.start
      Loaded { data with Store = new Store(newstate) }
    | Idle ->
      chan.Reply (Right Reply.Ok)
      state

  // ** handleInstallSnapshot

  let private handleInstallSnapshot (state: ServerState) (id: Id) (agent: ApiAgent) =
    match state with
    | Loaded data ->
      job {
        match Map.tryFind id data.Clients with
        | Some client ->
          requestInstallSnapshot data client agent
        | None -> ()
      } |> Hopac.start
      state
    | Idle -> state

  // ** handleGetState

  let private handleGetState (chan: ReplyChan) (state: ServerState) =
    match state with
    | Loaded data ->
      job {
        data.Store.State
        |> Reply.State
        |> Either.succeed
        |> chan.Reply
      } |> Hopac.start
      state
    | _ ->
      job {
        "Not Loaded"
        |> Error.asClientError (tag "handleGetState")
        |> Either.fail
        |> chan.Reply
      } |> Hopac.start
      state

  // ** handleClientUpdate

  let private handleClientUpdate (state: ServerState)
                                 (subs: Subscriptions)
                                 (sm: StateMachine)
                                 (agent: ApiAgent) =
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
      match req.Body |> Binary.decode with
      | Right (Register client) ->
        job {
          client
          |> Msg.AddClient
          |> agent.Post
          OK
          |> Binary.encode
          |> RawResponse.fromRequest req
          |> data.Server.Respond
        } |> Hopac.start
      | Right (UnRegister client) ->
        job {
          client
          |> Msg.RemoveClient
          |> agent.Post
          OK
          |> Binary.encode
          |> RawResponse.fromRequest req
          |> data.Server.Respond
        } |> Hopac.start
      | Right (Update sm) ->
        job {
          sm
          |> Msg.ClientUpdate
          |> agent.Post
          OK
          |> Binary.encode
          |> RawResponse.fromRequest req
          |> data.Server.Respond
        } |> Hopac.start
      | Left error ->
        job {
          string error
          |> ApiError.Internal
          |> NOK
          |> Binary.encode
          |> RawResponse.fromRequest req
          |> data.Server.Respond
        } |> Hopac.start
      state


  // ** loop

  let private loop (initial: ServerState) (subs: Subscriptions) (inbox: ApiAgent) =
    let rec act (state: ServerState) =
      async {
        let! msg = inbox.Receive()

        let newstate =
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
        let listener = createListener subs
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
