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
      Socket: Req
      Timer: IDisposable }

    interface IDisposable with
      member client.Dispose() =
        dispose client.Timer
        dispose client.Socket

  // ** ClientStateData

  [<NoComparison;NoEquality>]
  type private ClientStateData =
    { Store: Store
      Server: Rep
      Publisher: Pub
      Subscriber: Sub
      Clients: Map<Id,Client> }

    interface IDisposable with
      member data.Dispose() =
        dispose data.Publisher
        dispose data.Subscriber
        dispose data.Server
        Map.iter (fun _ v -> dispose v) data.Clients

  // ** ClientState

  [<NoComparison;NoEquality>]
  type private ClientState =
    | Loaded of ClientStateData
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
    | Update          of sm:StateMachine
    | GetClients      of chan:ReplyChan
    | AddClient       of chan:ReplyChan * client:IrisClient
    | RemoveClient    of chan:ReplyChan * client:IrisClient
    | SetStatus       of id:Id          * status:ServiceStatus
    | SetState        of chan:ReplyChan * state:State
    | GetState        of chan:ReplyChan
    | InstallSnapshot of id:Id
    | ClientUpdate    of sm:StateMachine

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

  let private requestInstallSnapshot (data: ClientStateData) (client: Client) (agent: ApiAgent) =
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
      |> Msg.SetStatus
      |> agent.Post
    | Right other ->
      let reason =
        sprintf "Unexpected reply from Client %A" other
        |> Error.asClientError (tag "requestInstallSnapshot")
      (client.Meta.Id, ServiceStatus.Failed reason)
      |> Msg.SetStatus
      |> agent.Post
    | Left error ->
      let reason =
        string error
        |> Error.asClientError (tag "requestInstallSnapshot")
      (client.Meta.Id, ServiceStatus.Failed reason)
      |> Msg.SetStatus
      |> agent.Post

  // ** pingTimer

  let private pingTimer (socket: Req) (agent: ApiAgent) =
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
          string socket.Id
          |> sprintf "ping request to %s successful"
          |> Logger.debug socket.Id (tag "pingTimer")
          (socket.Id, ServiceStatus.Running)
          |> Msg.SetStatus
          |> agent.Post
        | Left error ->
          string error
          |> sprintf "error during ping request to %s: %s" (string socket.Id)
          |> Logger.debug socket.Id (tag "pingTimer")
          (socket.Id, ServiceStatus.Failed error)
          |> Msg.SetStatus
          |> agent.Post
        | _ -> ()

        return! loop ()
      }

    Async.Start(loop (), cts.Token)
    { new IDisposable with
        member self.Dispose () =
          cts.Cancel() }

  // ** requestHandler

  let private requestHandler (agent: ApiAgent) (raw: byte array) =
    match Binary.decode raw with
    | Right (Register client) ->
      match postCommand agent (fun chan -> Msg.AddClient(chan, client)) with
      | Right Reply.Ok -> Binary.encode OK
      | Right _ ->
        "Received wrong Reply type from ApiAgent"
        |> ApiError.Internal
        |> NOK
        |> Binary.encode
      | Left error ->
        string error
        |> ApiError.Internal
        |> NOK
        |> Binary.encode
    | Right (UnRegister client) ->
      match postCommand agent (fun chan -> Msg.RemoveClient(chan, client)) with
      | Right Reply.Ok -> Binary.encode OK
      | Right _ ->
        "Received wrong Reply type from ApiAgent"
        |> ApiError.Internal
        |> NOK
        |> Binary.encode
      | Left error ->
        string error
        |> ApiError.Internal
        |> NOK
        |> Binary.encode
    | Right (Update sm) ->
      agent.Post(Msg.ClientUpdate sm)
      Binary.encode OK
    | Left error ->
      string error
      |> ApiError.Internal
      |> NOK
      |> Binary.encode

  // ** processSubscriptionEvent

  let private processSubscriptionEvent (agent: ApiAgent) (bytes: byte array) =
    printfn "processSubscriptionEvent"

  // ** start

  let private start (chan: ReplyChan) (agent: ApiAgent) (mem: RaftMember) (id: Id) =
    let srvAddr = formatTCPUri mem.IpAddr (int mem.ApiPort)
    let server = new Rep(mem.Id, srvAddr, requestHandler agent)

    let pubSubAddr =
      formatEPGMUri
        mem.IpAddr
        (IPv4Address Constants.MCAST_ADDRESS)
        Constants.MCAST_PORT

    let publisher = new Pub(mem.Id, pubSubAddr, string id)
    let subscriber = new Sub(mem.Id, pubSubAddr, string id)

    subscriber.Subscribe(processSubscriptionEvent agent)
    |> ignore                            // get cleaned up during Dispose

    match server.Start(), publisher.Start(), subscriber.Start() with
    | Right (), Right (), Right () ->
      chan.Reply(Right Reply.Ok)
      Loaded { Store = new Store(State.Empty)
               Publisher = publisher
               Subscriber = subscriber
               Clients = Map.empty
               Server = server }
    | Left error, _, _ | _, Left error, _ | _, _, Left error ->
      chan.Reply(Left error)
      dispose server
      Idle

  // ** handleStart

  let private handleStart (chan: ReplyChan)
                          (state: ClientState)
                          (agent: ApiAgent)
                          (mem: RaftMember)
                          (projectId: Id) =
    match state with
    | Loaded data ->
      dispose data
      start chan agent mem projectId
    | Idle ->
      start chan agent mem projectId

  // ** handleDispose

  let private handleDispose (chan: ReplyChan) (state: ClientState) =
    dispose state
    chan.Reply(Right Reply.Ok)
    Idle

  // ** handleAddClient

  let private handleAddClient (chan: ReplyChan)
                              (state: ClientState)
                              (subs: Subscriptions)
                              (meta: IrisClient)
                              (agent: ApiAgent) =
    match state with
    | Loaded data ->

      // first, dispose of the previous client
      match Map.tryFind meta.Id data.Clients with
      | Some client ->
        dispose client
        notify subs (ApiEvent.UnRegister client.Meta)
      | None -> ()

      // construct a new client value
      let addr = formatTCPUri meta.IpAddress (int meta.Port)
      let socket = new Req(meta.Id, addr, Constants.REQ_TIMEOUT)
      socket.Start()

      let client =
        { Meta = meta
          Socket = socket
          Timer = pingTimer socket agent }

      agent.Post(Msg.InstallSnapshot meta.Id)

      chan.Reply(Right Reply.Ok)
      notify subs (ApiEvent.Register meta)
      Loaded { data with Clients = Map.add meta.Id client data.Clients }
    | Idle ->
      chan.Reply(Right Reply.Ok)
      Idle

  // ** handleRemoveClient

  let private handleRemoveClient (chan: ReplyChan)
                                 (state: ClientState)
                                 (subs: Subscriptions)
                                 (peer: IrisClient) =
    match state with
    | Loaded data ->
      match Map.tryFind peer.Id data.Clients with
      | Some client ->
        dispose client
        chan.Reply(Right Reply.Ok)
        notify subs (ApiEvent.UnRegister peer)
        Loaded { data with Clients = Map.remove peer.Id data.Clients }
      | _ ->
        chan.Reply(Right Reply.Ok)
        state
    | Idle ->
      chan.Reply(Right Reply.Ok)
      state

  // ** updateClient

  let private updateClient (sm: StateMachine) (client: Client) =
    async {
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

  // ** handleUpdate

  let private handleUpdate (state: ClientState)
                           (sm: StateMachine)
                           (agent: ApiAgent) =
    match state with
    | Loaded data ->
      data.Store.Dispatch sm

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
            |> Msg.SetStatus
            |> agent.Post
          | _ -> ())

      state
    | Idle -> state

  // ** handleGetClients

  let private handleGetClients (chan: ReplyChan) (state: ClientState) =
    match state with
    | Loaded data ->
      data.Clients
      |> Map.map (fun _ v -> v.Meta)
      |> Reply.Clients
      |> Either.succeed
      |> chan.Reply
      state
    | Idle ->
      "ClientApi not running"
      |> Error.asClientError (tag "handleGetClients")
      |> Either.fail
      |> chan.Reply
      state

  // ** handleSetStatus

  let private handleSetStatus (id: Id)
                              (status: ServiceStatus)
                              (state: ClientState)
                              (subs: Subscriptions) =
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
            notify subs (ApiEvent.Status updated.Meta)
            Loaded { data with Clients = Map.add id updated data.Clients }
          else
            state
      | None -> state
    | idle -> idle

  // ** handleSetState

  let private handleSetState (chan: ReplyChan)
                             (state: ClientState)
                             (newstate: State)
                             (agent: ApiAgent) =
    match state with
    | Loaded data ->
      Map.iter (fun id _ -> agent.Post(Msg.InstallSnapshot id)) data.Clients
      chan.Reply (Right Reply.Ok)
      Loaded { data with Store = new Store(newstate) }
    | Idle ->
      chan.Reply (Right Reply.Ok)
      state

  // ** handleInstallSnapshot

  let private handleInstallSnapshot (state: ClientState) (id: Id) (agent: ApiAgent) =
    match state with
    | Loaded data ->
      match Map.tryFind id data.Clients with
      | Some client ->
        requestInstallSnapshot data client agent
        state
      | None -> state
    | Idle -> state

  // ** handleGetState

  let private handleGetState (chan: ReplyChan) (state: ClientState) =
    match state with
    | Loaded data ->
      data.Store.State
      |> Reply.State
      |> Either.succeed
      |> chan.Reply
      state
    | _ ->
      "Not Loaded"
      |> Error.asClientError (tag "handleGetState")
      |> Either.fail
      |> chan.Reply
      state

  // ** handleClientUpdate

  let private handleClientUpdate (state: ClientState) (subs: Subscriptions) (sm: StateMachine) =
    match state with
    | Loaded _ ->
      notify subs (ApiEvent.Update sm)
      state
    | Idle ->
      state

  // ** loop

  let private loop (initial: ClientState) (subs: Subscriptions) (inbox: ApiAgent) =
    let rec act (state: ClientState) =
      async {
        let! msg = inbox.Receive()

        let newstate =
          match msg with
          | Msg.Start(chan,mem,projectId) -> handleStart chan state inbox mem projectId
          | Msg.Dispose chan              -> handleDispose chan state
          | Msg.AddClient(chan,client)    -> handleAddClient chan state subs client inbox
          | Msg.RemoveClient(chan,client) -> handleRemoveClient chan state subs client
          | Msg.Update(sm)                -> handleUpdate state sm inbox
          | Msg.GetClients(chan)          -> handleGetClients chan state
          | Msg.SetStatus(id, status)     -> handleSetStatus id status state subs
          | Msg.SetState(chan, newstate)  -> handleSetState chan state newstate inbox
          | Msg.GetState(chan)            -> handleGetState chan state
          | Msg.InstallSnapshot(id)       -> handleInstallSnapshot state id inbox
          | Msg.ClientUpdate(sm)          -> handleClientUpdate state subs sm

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
                agent.Post(Msg.Update sm)

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
