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
      Clients: Map<Id,Client> }

    interface IDisposable with
      member data.Dispose() =
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
    | Ok

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start           of chan:ReplyChan * mem:RaftMember
    | Dispose         of chan:ReplyChan
    | UpdateClients   of chan:ReplyChan * sm:StateMachine
    | GetClients      of chan:ReplyChan
    | AddClient       of chan:ReplyChan * client:IrisClient
    | RemoveClient    of chan:ReplyChan * client:IrisClient
    | SetStatus       of id:Id          * status:ServiceStatus
    | SetState        of chan:ReplyChan * state:State
    | InstallSnapshot of id:Id

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
        "Unexpected reply from Client"
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
          (socket.Id, ServiceStatus.Running)
          |> Msg.SetStatus
          |> agent.Post
        | Left error ->
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
      match agent.PostAndReply(fun chan -> Msg.AddClient(chan, client)) with
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
      match agent.PostAndReply(fun chan -> Msg.RemoveClient(chan, client)) with
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
    | Left error ->
      string error
      |> ApiError.Internal
      |> NOK
      |> Binary.encode

  // ** start

  let private start (chan: ReplyChan) (agent: ApiAgent) (mem: RaftMember) =
    let addr = formatUri mem.IpAddr (int mem.ApiPort)
    let server = new Rep(addr, requestHandler agent)
    match server.Start() with
    | Right () ->
      chan.Reply(Right Reply.Ok)
      Loaded { Store = new Store(State.Empty)
               Clients = Map.empty
               Server = server }
    | Left error ->
      chan.Reply(Left error)
      dispose server
      Idle

  // ** handleStart

  let private handleStart (chan: ReplyChan)
                          (state: ClientState)
                          (agent: ApiAgent)
                          (mem: RaftMember) =
    match state with
    | Loaded data ->
      dispose data
      start chan agent mem
    | Idle ->
      start chan agent mem

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
      let socket = new Req(meta.Id, formatUri meta.IpAddress (int meta.Port), 50)
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
                                 (client: IrisClient) =
    match state with
    | Loaded data ->
      chan.Reply(Right Reply.Ok)
      notify subs (ApiEvent.UnRegister client)
      Loaded { data with Clients = Map.remove client.Id data.Clients }
    | Idle ->
      chan.Reply(Right Reply.Ok)
      Idle

  // ** updateClient

  let private updateClient (sm: StateMachine) (client: Client) =
    async {
      let request =
        match sm with
        | DataSnapshot state ->
          ClientApiRequest.Snapshot state
        | _ ->
          ClientApiRequest.Ping

      let result : Either<IrisError,ApiResponse> =
        request
        |> Binary.encode
        |> client.Socket.Request
        |> Either.bind Binary.decode

      match result with
      | Right ApiResponse.OK -> printfn "Request OK"
      | Right ApiResponse.Pong -> printfn "PONG"
      | Right (ApiResponse.NOK err) ->
        printfn "Request NOK: %A" err
      | Left error ->
        printfn "Error in request to client: %A" error
    }

  // ** updateClients

  let private updateClients (sm: StateMachine) (clients: Map<Id,Client>) =
    clients
    |> Map.toArray
    |> Array.map (snd >> updateClient sm)
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore

  // ** handleUpdateClients

  let private handleUpdateClients (chan: ReplyChan) (state: ClientState) (sm: StateMachine) =
    match state with
    | Loaded data ->
      updateClients sm data.Clients
      chan.Reply(Right Reply.Ok)
      state
    | Idle ->
      "ClientApi not running"
      |> Error.asClientError (tag "handleUpdateClients")
      |> Either.fail
      |> chan.Reply
      state

  // ** handleGetClients

  let private handleGetClients (chan: ReplyChan) (state: ClientState) =
    match state with
    | Loaded data ->
      data.Clients
      |> Map.map (fun k v -> v.Meta)
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

  // ** loop

  let private loop (initial: ClientState) (subs: Subscriptions) (inbox: ApiAgent) =
    let rec act (state: ClientState) =
      async {
        let! msg = inbox.Receive()

        let newstate =
          match msg with
          | Msg.Start(chan,mem)           -> handleStart chan state inbox mem
          | Msg.Dispose chan              -> handleDispose chan state
          | Msg.AddClient(chan,client)    -> handleAddClient chan state subs client inbox
          | Msg.RemoveClient(chan,client) -> handleRemoveClient chan state subs client
          | Msg.UpdateClients(chan,sm)    -> handleUpdateClients chan state sm
          | Msg.GetClients(chan)          -> handleGetClients chan state
          | Msg.SetStatus(id, status)     -> handleSetStatus id status state subs
          | Msg.SetState(chan, newstate)  -> handleSetState chan state newstate inbox
          | Msg.InstallSnapshot(id)       -> handleInstallSnapshot state id inbox

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

    let create (mem: RaftMember) =
      either {
        let cts = new CancellationTokenSource()
        let subs = new Subscriptions()
        let agent = new ApiAgent(loop Idle subs, cts.Token)
        let listener = createListener subs
        agent.Start()

        return
          { new IApiServer with
              member self.Start () =
                match agent.PostAndReply(fun chan -> Msg.Start(chan,mem)) with
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
                  match agent.PostAndReply(fun chan -> Msg.GetClients(chan)) with
                  | Right (Reply.Clients clients) -> Either.succeed clients
                  | Right other ->
                    sprintf "Unexpected Reply from ApiAgent: %A" other
                    |> Error.asClientError (tag "UpdateClients")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.UpdateClients (sm: StateMachine) =
                match agent.PostAndReply(fun chan -> Msg.UpdateClients(chan, sm)) with
                | Right (Reply.Ok) -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "UpdateClients")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.SetState (state: State) =
                match agent.PostAndReply(fun chan -> Msg.SetState(chan, state)) with
                | Right (Reply.Ok) -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected Reply from ApiAgent: %A" other
                  |> Error.asClientError (tag "UpdateClients")
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
                agent.PostAndReply(fun chan -> Msg.Dispose chan)
                |> ignore
                dispose cts
            }
      }
