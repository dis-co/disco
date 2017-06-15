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

module ApiServer =
  open ZeroMQ

  //  ____       _            _
  // |  _ \ _ __(_)_   ____ _| |_ ___
  // | |_) | '__| \ \ / / _` | __/ _ \
  // |  __/| |  | |\ V / (_| | ||  __/
  // |_|   |_|  |_| \_/ \__,_|\__\___|

  // ** tag

  let private tag (str: string) = String.Format("ApiServer.{0}",str)

  // ** timeout

  [<Literal>]
  let private timeout = 1000

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<IrisEvent>

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

  // ** ServerState

  [<NoComparison;NoEquality>]
  type private ServerState =
    { Id: Id
      Status: ServiceStatus
      Server: IServer
      Publisher: Pub
      Subscriber: Sub
      Clients: Map<Id,Client>
      Context: ZContext
      Callbacks: IApiServerCallbacks
      Subscriptions: Subscriptions
      Disposables: IDisposable list
      Stopper: AutoResetEvent }

    // *** Dispose

    interface IDisposable with
      member data.Dispose() =
        List.iter dispose data.Disposables
        dispose data.Server
        dispose data.Publisher
        dispose data.Subscriber
        Map.iter (fun _ v -> dispose v) data.Clients
        data.Subscriptions.Clear()

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start
    | Stop
    | SetStatus         of status:ServiceStatus
    | AddClient         of client:IrisClient
    | RemoveClient      of client:IrisClient
    | SetClientStatus   of id:Id * status:ServiceStatus
    | InstallSnapshot   of id:Id
    | Update            of origin:Origin * sm:StateMachine
    | RawServerRequest  of req:RawServerRequest
    | RawClientResponse of req:RawClientResponse

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** requestInstallSnapshot

  let private requestInstallSnapshot (state: ServerState) (client: Client) (agent: ApiAgent) =
    state.Callbacks.PrepareSnapshot()
    |> ClientApiRequest.Snapshot
    |> Binary.encode
    |> fun body -> { RequestId = Guid.NewGuid(); Body = body }
    |> client.Socket.Request
    |> Either.mapError (string >> Logger.err (tag "requestInstallSnapshot"))
    |> ignore

    // match result with
    // | Right ApiResponse.OK -> ()

    // | Right (ApiResponse.NOK error) ->
    //   error
    //   |> string
    //   |> Error.asClientError (tag "requestInstallSnapshot")
    //   |> ServiceStatus.Failed
    //   |> fun reason -> client.Meta.Id, reason
    //   |> Msg.SetClientStatus
    //   |> agent.Post

    // | Right other ->
    //   other
    //   |> sprintf "Unexpected reply from Client %A"
    //   |> Error.asClientError (tag "requestInstallSnapshot")
    //   |> ServiceStatus.Failed
    //   |> fun reason -> client.Meta.Id, reason
    //   |> Msg.SetClientStatus
    //   |> agent.Post

    // | Left error ->
    //   error
    //   |> string
    //   |> Error.asClientError (tag "requestInstallSnapshot")
    //   |> ServiceStatus.Failed
    //   |> fun reason -> client.Meta.Id, reason
    //   |> Msg.SetClientStatus
    //   |> agent.Post

  // ** pingTimer

  let private pingTimer (socket: IClient) (agent: ApiAgent) =
    let cts = new CancellationTokenSource()

    let rec loop () =
      async {
        do! Async.Sleep(timeout)
        ClientApiRequest.Ping
        |> Binary.encode
        |> fun body -> { RequestId = Guid.NewGuid(); Body = body }
        |> socket.Request
        |> Either.mapError (string >> Logger.err (tag "pingTimer"))
        |> ignore
        return! loop ()
      }

    Async.Start(loop (), cts.Token)
    { new IDisposable with
        member self.Dispose () =
          cts.Cancel() }


  // ** processSubscriptionEvent

  let private processSubscriptionEvent (mem: Id) (agent: ApiAgent) (peer:Id, bytes:byte array) =
    match Binary.decode bytes with
    | Right command ->
      match command with
      // Special case for tests:
      //
      // In tests, the Logger singleton won't have the correct Id (because they run in the same
      // process). Hence, we look at the peer Id as supplied from the Sub socket, compare and
      // substitute if necessary. This goes in conjunction with only publishing logs on the Api that
      // are from that service.
      | LogMsg log when log.Tier = Tier.Service && log.Id <> mem ->
        Logger.append { log with Id = peer }

      // Base case for logs:
      //
      // Append logs to the current Logger singleton, to be forwarded to the frontend.
      | LogMsg log -> Logger.append log

      | CallCue _ | UpdateSlices _ ->
        Msg.Update(Origin.Api, command) |> agent.Post
      | _ -> ()
    | _ -> () // not sure if I should log here..

  // ** handleStart

  let private handleStart (state: ServerState) (agent: ApiAgent) =
    state

  // ** handleAddClient

  let private handleAddClient (state: ServerState) (meta: IrisClient) (agent: ApiAgent) =
    Tracing.trace (tag "handleAddClient") <| fun () ->
      // first, dispose of the previous client
      match Map.tryFind meta.Id state.Clients with
      | None -> ()
      | Some client ->
        dispose client
        (Origin.Service, RemoveClient client.Meta)
        |> IrisEvent.Append
        |> Observable.onNext state.Subscriptions

      // construct a new client value
      let addr = Uri.tcpUri meta.IpAddress (Some meta.Port)
      let client = Client.create state.Context {
        PeerId = meta.Id
        Frontend = addr
        Timeout = int Constants.REQ_TIMEOUT * 1<ms>
      }

      match client with
      | Right socket ->
        socket.Subscribe (Msg.RawClientResponse >> agent.Post) |> ignore

        let client =
          { Meta = meta
            Socket = socket
            Timer = pingTimer socket agent }

        meta.Id |> Msg.InstallSnapshot |> agent.Post

        (Origin.Service, AddClient meta)
        |> IrisEvent.Append
        |> Observable.onNext state.Subscriptions

        { state with Clients = Map.add meta.Id client state.Clients }
      | Left error ->
        error
        |> string
        |> Logger.err (tag "handleAddClient")
        state

  // ** handleRemoveClient

  let private handleRemoveClient (state: ServerState) (peer: IrisClient) =
    Tracing.trace (tag "handleRemoveClient") <| fun () ->
      match Map.tryFind peer.Id state.Clients with
      | Some client ->
        dispose client
        (Origin.Service, RemoveClient peer)
        |> IrisEvent.Append
        |> Observable.onNext state.Subscriptions
        { state with Clients = Map.remove peer.Id state.Clients }
      | _ -> state

  // ** updateClient

  let private updateClient (sm: StateMachine) (client: Client) =
    sm
    |> ClientApiRequest.Update
    |> Binary.encode
    |> fun body -> { RequestId = Guid.NewGuid(); Body = body }
    |> client.Socket.Request
    |> Either.mapError (string >> Logger.err (tag "updateClient"))
    |> ignore

  // ** updateAllClients

  let private updateAllClients (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace (tag "updateAllClients") <| fun () ->
      state.Clients
      |> Map.toArray
      |> Array.Parallel.map (snd >> updateClient sm)
      |> ignore

  // ** multicastClients

  let private multicastClients (state: ServerState) except (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace (tag "multicastClients") <| fun () ->
      state.Clients
      |> Map.filter (fun id _ -> except <> id)
      |> Map.toArray
      |> Array.Parallel.map (snd >> updateClient sm)
      |> ignore

  // ** publish

  let private publish (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    sm
    |> Binary.encode
    |> state.Publisher.Publish
    |> Either.mapError (ServiceStatus.Failed >> Msg.SetStatus >> agent.Post)
    |> ignore

  // ** handleSetStatus

  let private handleSetStatus (state: ServerState) (status: ServiceStatus) =
    status
    |> IrisEvent.Status
    |> Observable.onNext state.Subscriptions
    { state with Status = status }

  // ** handleSetClientStatus

  let private handleSetClientStatus (state: ServerState) (id: Id) (status: ServiceStatus) =
    match Map.tryFind id state.Clients with
    | Some client ->
      match client.Meta.Status, status with
      | ServiceStatus.Running, ServiceStatus.Running -> state
      | oldst, newst ->
        if oldst <> newst then
          let updated = { client with Meta = { client.Meta with Status = status } }
          (Origin.Service, UpdateClient updated.Meta)
          |> IrisEvent.Append
          |> Observable.onNext state.Subscriptions
          { state with Clients = Map.add id updated state.Clients }
        else state
    | None -> state

  // ** handleInstallSnapshot

  let private handleInstallSnapshot (state: ServerState) (id: Id) (agent: ApiAgent) =
    match Map.tryFind id state.Clients with
    | Some client -> requestInstallSnapshot state client agent
    | None -> ()
    state

  // ** handleUpdate

  let private handleUpdate (state: ServerState)
                           (origin: Origin)
                           (cmd: StateMachine)
                           (agent: ApiAgent) =
    match origin, cmd with
    | Origin.Api, _ ->
      updateAllClients state cmd agent       // in order to preserve ordering of the messages
      (origin, cmd)
      |> IrisEvent.Append
      |> Observable.onNext state.Subscriptions

    | Origin.Raft, _ ->
      updateAllClients state cmd agent       // in order to preserve ordering of the messages

    | Origin.Client id, LogMsg       _
    | Origin.Client id, CallCue      _
    | Origin.Client id, UpdateSlices _ ->
      publish state cmd agent
      multicastClients state id cmd agent       // in order to preserve ordering of the messages
      (origin, cmd) |> IrisEvent.Append |> Observable.onNext state.Subscriptions

    | Origin.Client id, _ ->
      (origin, cmd) |> IrisEvent.Append |> Observable.onNext state.Subscriptions

    | Origin.Web _, LogMsg       _
    | Origin.Web _, CallCue      _
    | Origin.Web _, UpdateSlices _ ->
      publish state cmd agent
      updateAllClients state cmd agent

    | Origin.Web id, _ ->
      updateAllClients state cmd agent

    | Origin.Service, AddClient    _
    | Origin.Service, UpdateClient _
    | Origin.Service, RemoveClient _ ->
      (origin, cmd)
      |> IrisEvent.Append
      |> Observable.onNext state.Subscriptions

    | Origin.Service, LogMsg _
    | Origin.Service, UpdateSlices _ ->
      publish state cmd agent
      updateAllClients state cmd agent

    | other -> ignore other

    state

  // ** handleServerRequest

  let private handleServerRequest (state: ServerState) (req: RawServerRequest) (agent: ApiAgent) =
    Tracing.trace (tag "handleServerRequest") <| fun () ->
      match req.Body |> Binary.decode with
      | Right (Register client) ->
        client.Id
        |> sprintf "%O requested to be registered"
        |> Logger.debug (tag "handleServerRequest")

        client |> Msg.AddClient |> agent.Post
        Registered |> Binary.encode |> RawServerResponse.fromRequest req |> state.Server.Respond

      | Right (UnRegister client) ->
        client.Id
        |> sprintf "%O requested to be un-registered"
        |> Logger.debug (tag "handleServerRequest")

        client |> Msg.RemoveClient |> agent.Post
        Unregistered |> Binary.encode |> RawServerResponse.fromRequest req |> state.Server.Respond

      | Right (Update sm) ->
        let id = req.From |> string |> Id
        (Origin.Client id, sm)
        |> Msg.Update
        |> agent.Post
        OK |> Binary.encode |> RawServerResponse.fromRequest req |> state.Server.Respond

      | Left error ->
        error
        |> sprintf "error decoding request: %O"
        |> Logger.err (tag "handleServerRequest")

        string error
        |> ApiError.Internal
        |> NOK
        |> Binary.encode
        |> RawServerResponse.fromRequest req
        |> state.Server.Respond
      state

  // ** handleClientResponse

  let private handleClientResponse state (resp: RawClientResponse) (agent: ApiAgent) =
    match Either.bind Binary.decode resp.Body with
    | Right ApiResponse.Pong ->
      (resp.PeerId, ServiceStatus.Running)
      |> Msg.SetClientStatus
      |> agent.Post
    | Right (ApiResponse.NOK error) ->
      error
      |> sprintf "NOK in client request. reason: %O"
      |> Logger.err (tag "handleClientResponse")
      // set the status of this client to error
      let err = error |> string |> Error.asSocketError (tag "handleClientResponse")
      (resp.PeerId, ServiceStatus.Failed err)
      |> Msg.SetClientStatus
      |> agent.Post
    | Right (ApiResponse.OK _)
    | Right (ApiResponse.Registered _)
    | Right (ApiResponse.Unregistered _) -> ()
    | Left error ->
      error
      |> sprintf "error returned in client request. reason: %O"
      |> Logger.err (tag "handleClientResponse")
      // set the status of this client to error
      (resp.PeerId, ServiceStatus.Failed error)
      |> Msg.SetClientStatus
      |> agent.Post
    state

  // ** handleStop

  let private handleStop (state: ServerState) =
    dispose state
    state.Stopper.Set() |> ignore
    { state with Status = ServiceStatus.Stopping }

  // ** loop

  let private loop (store: IAgentStore<ServerState>) (inbox: ApiAgent) =
    let rec act () =
      async {
        try
          let! msg = inbox.Receive()

          Actors.warnQueueLength (tag "loop") inbox

          let state = store.State
          let newstate =
            try
              match msg with
              | Msg.Start                       -> handleStart state inbox
              | Msg.Stop                        -> handleStop state
              | Msg.AddClient(client)           -> handleAddClient state client inbox
              | Msg.RemoveClient(client)        -> handleRemoveClient state client
              | Msg.SetClientStatus(id, status) -> handleSetClientStatus state id status
              | Msg.SetStatus(status)           -> handleSetStatus state status
              | Msg.InstallSnapshot(id)         -> handleInstallSnapshot state id inbox
              | Msg.Update(origin,sm)           -> handleUpdate state origin sm inbox
              | Msg.RawServerRequest(req)       -> handleServerRequest state req inbox
              | Msg.RawClientResponse(resp)     -> handleClientResponse state resp inbox
            with
              | exn ->
                exn.Message + exn.StackTrace
                |> String.format "Error in loop: {0}"
                |> Logger.err (tag "loop")
                state
          if not (Service.isStopping newstate.Status) then
            store.Update newstate
        with
          | exn ->
            exn.Message
            |> Logger.err (tag "loop")
        return! act ()
      }
    act ()

  // ** start

  let private start (ctx: ZContext)
                    (mem: RaftMember)
                    (projectId: Id)
                    (store: IAgentStore<ServerState>)
                    (agent: ApiAgent) =
    either {
      let frontend = Uri.tcpUri mem.IpAddr (Some mem.ApiPort)
      let backend = Uri.inprocUri Constants.API_BACKEND_PREFIX (mem.Id |> string |> Some)

      let pubSubAddr =
        Uri.epgmUri
          mem.IpAddr
          (IPv4Address Constants.MCAST_ADDRESS)
          (port Constants.MCAST_PORT)

      let publisher = new Pub(unwrap pubSubAddr, string projectId, ctx)
      let subscriber = new Sub(unwrap pubSubAddr, string projectId, ctx)

      let result = Server.create ctx {
        Id = mem.Id
        Listen = frontend
      }

      match result  with
      | Right server ->
        match publisher.Start(), subscriber.Start() with
        | Right (), Right () ->
          let srv = server.Subscribe (Msg.RawServerRequest >> agent.Post)
          let sub = subscriber.Subscribe(processSubscriptionEvent mem.Id agent)

          let updated =
            { store.State with
                Status = ServiceStatus.Running
                Publisher = publisher
                Subscriber = subscriber
                Server = server
                Disposables = [ srv; sub ] }

          store.Update updated
          agent.Start()
          agent.Post Msg.Start

        | Left error, _ | _, Left error ->
          dispose server
          dispose publisher
          dispose subscriber
          return! Either.fail error

      | Left error ->
        return! Either.fail error
    }

  // ** create

  let create ctx (mem: RaftMember) (projectId: Id) callbacks =
    either {
      let cts = new CancellationTokenSource()

      let store = AgentStore.create ()

      store.Update {
        Id = mem.Id
        Status = ServiceStatus.Stopped
        Server = Unchecked.defaultof<IServer>
        Publisher = Unchecked.defaultof<Pub>
        Subscriber = Unchecked.defaultof<Sub>
        Clients = Map.empty
        Subscriptions = Subscriptions()
        Context = ctx
        Disposables = []
        Callbacks = callbacks
        Stopper = new AutoResetEvent(false)
      }

      let agent = new ApiAgent(loop store, cts.Token)
      agent.Error.Add(sprintf "unhandled error on actor loop: %O" >> Logger.err (tag "loop"))

      return
        { new IApiServer with

            // *** Publish

            member self.Publish (ev: IrisEvent) =
              match ev with
              | IrisEvent.Append (origin, (LogMsg log as cmd)) when log.Id <> mem.Id -> ()
              | IrisEvent.Append (origin, cmd) ->
                self.Update origin cmd
              | _ -> ()

            // *** Start

            member self.Start () = start ctx mem projectId store agent

            // *** Clients

            member self.Clients
              with get () = store.State.Clients |> Map.map (fun id client -> client.Meta)

            // *** SendSnapshot

            member self.SendSnapshot () =
              Map.iter (fun id _ -> id |> Msg.InstallSnapshot |> agent.Post) store.State.Clients

            // *** Update

            member self.Update (origin: Origin) (sm: StateMachine) =
              (origin, sm) |> Msg.Update |> agent.Post

            // *** Subscribe

            member self.Subscribe (callback: IrisEvent -> unit) =
              Observable.subscribe callback store.State.Subscriptions

            // *** Dispose

            member self.Dispose () =
              agent.Post Msg.Stop
              if not (store.State.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
                Logger.debug (tag "Dispose") "timeout: attempt to dispose api server failed"
              dispose cts
          }
    }
