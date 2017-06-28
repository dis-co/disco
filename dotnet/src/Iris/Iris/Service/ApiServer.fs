namespace Iris.Service

// * Imports

open System
open System.Net
open System.Threading
open System.Collections.Concurrent
open Iris.Raft
open Iris.Core
open Iris.Client
open Iris.Net
open Iris.Service.Interfaces
open Iris.Serialization

// * ApiServer module

module ApiServer =

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

  // ** ServerState

  [<NoComparison;NoEquality>]
  type private ServerState =
    { Id: Id
      Status: ServiceStatus
      Server: IServer
      PubSub: IPubSub
      Clients: Map<Id,IrisClient>
      Callbacks: IApiServerCallbacks
      Subscriptions: Subscriptions
      Disposables: IDisposable list
      Stopper: AutoResetEvent }

    // *** Dispose

    interface IDisposable with
      member data.Dispose() =
        List.iter dispose data.Disposables
        dispose data.Server
        dispose data.PubSub
        data.Subscriptions.Clear()

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start
    | Stop
    | SetStatus       of status:ServiceStatus
    | AddClient       of client:IrisClient
    | RemoveClient    of client:IrisClient
    | SetClientStatus of id:Id * status:ServiceStatus
    | InstallSnapshot of id:Id
    | Update          of origin:Origin * sm:StateMachine
    | ServerEvent     of ev:TcpServerEvent

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** requestInstallSnapshot

  let private requestInstallSnapshot (state: ServerState) (client: Id) =
    state.Callbacks.PrepareSnapshot()
    |> ApiRequest.Snapshot
    |> Binary.encode
    |> Request.create (Guid.ofId state.Server.Id)
    |> state.Server.Request (Guid.ofId client)

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

  // ** processSubscriptionEvent

  let private processSubscriptionEvent (mem: Id) (agent: ApiAgent) = function
    | PubSubEvent.Request(id, bytes) ->
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
          Logger.append { log with Id = id }

        // Base case for logs:
        //
        // Append logs to the current Logger singleton, to be forwarded to the frontend.
        | LogMsg log -> Logger.append log

        | CallCue _ | UpdateSlices _ ->
          Msg.Update(Origin.Api, command) |> agent.Post
        | _ -> ()
      | Left _ -> () // not sure if I should log here..

  // ** handleStart

  let private handleStart (state: ServerState) =
    state

  // ** handleAddClient

  let private handleAddClient (state: ServerState) (client: IrisClient) (agent: ApiAgent) =
    // first, dispose of the previous client
    client.Id |> Msg.InstallSnapshot |> agent.Post
    match Map.tryFind client.Id state.Clients with
    | None   ->
      (Origin.Service, AddClient client)
      |> IrisEvent.Append
      |> Observable.onNext state.Subscriptions
    | Some _ ->
      (Origin.Service, UpdateClient client)
      |> IrisEvent.Append
      |> Observable.onNext state.Subscriptions
    { state with Clients = Map.add client.Id client state.Clients }

  // ** handleRemoveClient

  let private handleRemoveClient (state: ServerState) (peer: IrisClient) =
    Tracing.trace (tag "handleRemoveClient") <| fun () ->
      match Map.tryFind peer.Id state.Clients with
      | Some client ->
        (Origin.Service, RemoveClient peer)
        |> IrisEvent.Append
        |> Observable.onNext state.Subscriptions
        { state with Clients = Map.remove peer.Id state.Clients }
      | _ -> state

  // ** updateClient

  let private updateClient (sm: StateMachine) (server: IServer) (client: IrisClient) =
    sm
    |> ApiRequest.Update
    |> Binary.encode
    |> Request.create (Guid.ofId server.Id)
    |> server.Request (Guid.ofId client.Id)

  // ** updateAllClients

  let private updateAllClients (state: ServerState) (sm: StateMachine) =
    Tracing.trace (tag "updateAllClients") <| fun () ->
      state.Clients
      |> Map.toArray
      |> Array.Parallel.map (snd >> updateClient sm state.Server)
      |> ignore

  // ** multicastClients

  let private multicastClients (state: ServerState) except (sm: StateMachine) =
    Tracing.trace (tag "multicastClients") <| fun () ->
      state.Clients
      |> Map.filter (fun id _ -> except <> id)
      |> Map.toArray
      |> Array.Parallel.map (snd >> updateClient sm state.Server)
      |> ignore

  // ** publish

  let private publish (state: ServerState) (sm: StateMachine) (_: ApiAgent) =
    // sm |> Binary.encode |> state.PubSub.Send
    ()

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
      match client.Status, status with
      | ServiceStatus.Running, ServiceStatus.Running -> state
      | oldst, newst ->
        if oldst <> newst then
          let updated = { client with Status = status }
          (Origin.Service, UpdateClient updated)
          |> IrisEvent.Append
          |> Observable.onNext state.Subscriptions
          { state with Clients = Map.add id updated state.Clients }
        else state
    | None -> state

  // ** handleInstallSnapshot

  let private handleInstallSnapshot (state: ServerState) (id: Id) =
    match Map.tryFind id state.Clients with
    | Some client -> requestInstallSnapshot state client.Id
    | None -> ()
    state

  // ** handleUpdate

  let private handleUpdate (state: ServerState)
                           (origin: Origin)
                           (cmd: StateMachine)
                           (agent: ApiAgent) =
    match origin, cmd with
    | Origin.Api, _ ->
      updateAllClients state cmd        // in order to preserve ordering of the messages
      (origin, cmd)
      |> IrisEvent.Append
      |> Observable.onNext state.Subscriptions

    | Origin.Raft, _ ->
      updateAllClients state cmd        // in order to preserve ordering of the messages

    | Origin.Client id, LogMsg       _
    | Origin.Client id, CallCue      _
    | Origin.Client id, UpdateSlices _ ->
      publish state cmd agent
      multicastClients state id cmd     // in order to preserve ordering of the messages
      (origin, cmd) |> IrisEvent.Append |> Observable.onNext state.Subscriptions

    | Origin.Client _, _ ->
      (origin, cmd) |> IrisEvent.Append |> Observable.onNext state.Subscriptions

    | Origin.Web _, LogMsg       _
    | Origin.Web _, CallCue      _
    | Origin.Web _, UpdateSlices _ ->
      publish state cmd agent
      updateAllClients state cmd

    | Origin.Web _, _ ->
      updateAllClients state cmd

    | Origin.Service, AddClient    _
    | Origin.Service, UpdateClient _
    | Origin.Service, RemoveClient _ ->
      (origin, cmd)
      |> IrisEvent.Append
      |> Observable.onNext state.Subscriptions

    | Origin.Service, LogMsg _
    | Origin.Service, UpdateSlices _ ->
      publish state cmd agent
      updateAllClients state cmd

    | other -> ignore other

    state

  // ** handleServerRequest

  let private handleServerRequest (state: ServerState) (req: Request) (agent: ApiAgent) =
    Tracing.trace (tag "handleServerRequest") <| fun () ->
      match req.Body |> Binary.decode with
      | Right (Register client) ->
        client.Id
        |> sprintf "%O requested to be registered"
        |> Logger.debug (tag "handleServerRequest")

        client
        |> Msg.AddClient
        |> agent.Post

        Registered
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Server.Respond

      | Right (UnRegister client) ->
        client.Id
        |> sprintf "%O requested to be un-registered"
        |> Logger.debug (tag "handleServerRequest")

        client
        |> Msg.RemoveClient
        |> agent.Post

        Unregistered
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Server.Respond

      | Right (Update sm) ->
        let id = req.PeerId |> string |> Id
        (Origin.Client id, sm)
        |> Msg.Update
        |> agent.Post

        OK
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Server.Respond

      | Right other -> ()                // ignore Ping et al

      | Left error ->
        error
        |> sprintf "error decoding request: %O"
        |> Logger.err (tag "handleServerRequest")

        string error
        |> ApiError.Internal
        |> NOK
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Server.Respond
      state

  // ** handleClientResponse

  let private handleClientResponse state (resp: Response) (agent: ApiAgent) =
    match Binary.decode resp.Body with
    //  _   _  ___  _  __
    // | \ | |/ _ \| |/ /
    // |  \| | | | | ' /
    // | |\  | |_| | . \
    // |_| \_|\___/|_|\_\
    | Right (ApiResponse.NOK error) ->
      error
      |> sprintf "NOK in client request. reason: %O"
      |> Logger.err (tag "handleClientResponse")
      // set the status of this client to error
      let err = error |> string |> Error.asSocketError (tag "handleClientResponse")
      (Guid.toId resp.PeerId, ServiceStatus.Failed err)
      |> Msg.SetClientStatus
      |> agent.Post
    //   ___  _  __
    //  / _ \| |/ /
    // | | | | ' /
    // | |_| | . \
    //  \___/|_|\_\
    | Right (ApiResponse.OK _)
    | Right (ApiResponse.Registered _)
    | Right (ApiResponse.Unregistered _) -> ()
    //  ____                     _        _____
    // |  _ \  ___  ___ ___   __| | ___  | ____|_ __ _ __ ___  _ __
    // | | | |/ _ \/ __/ _ \ / _` |/ _ \ |  _| | '__| '__/ _ \| '__|
    // | |_| |  __/ (_| (_) | (_| |  __/ | |___| |  | | | (_) | |
    // |____/ \___|\___\___/ \__,_|\___| |_____|_|  |_|  \___/|_|
    | Left error ->
      error
      |> sprintf "error returned in client request. reason: %O"
      |> Logger.err (tag "handleClientResponse")
      // set the status of this client to error
      (Guid.toId resp.PeerId, ServiceStatus.Failed error)
      |> Msg.SetClientStatus
      |> agent.Post
    state

  // ** handleServerEvent

  let private handleServerEvent state (ev: TcpServerEvent) agent =
    match ev with
    | TcpServerEvent.Request  request  -> handleServerRequest state request agent
    | TcpServerEvent.Response response -> handleClientResponse state response agent

    | TcpServerEvent.Connect(_, ip, port) ->
      sprintf "new connnection from %O:%d" ip port
      |> Logger.debug (tag "handleServerEvent")
      state

    | TcpServerEvent.Disconnect(peer) ->
      sprintf "%O disconnected" peer
      |> Logger.debug (tag "handleServerEvent")
      let id = Guid.toId peer
      match Map.tryFind id state.Clients with
      | None -> state
      | Some client ->
        (Origin.Service, RemoveClient client)
        |> IrisEvent.Append
        |> Observable.onNext state.Subscriptions
        { state with Clients = Map.remove id state.Clients }
        // (Guid.toId resp.PeerId, ServiceStatus.Running)
        // |> Msg.SetClientStatus
        // |> agent.Post

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
              | Msg.Start                       -> handleStart state
              | Msg.Stop                        -> handleStop state
              | Msg.AddClient(client)           -> handleAddClient state client inbox
              | Msg.RemoveClient(client)        -> handleRemoveClient state client
              | Msg.SetClientStatus(id, status) -> handleSetClientStatus state id status
              | Msg.SetStatus(status)           -> handleSetStatus state status
              | Msg.InstallSnapshot(id)         -> handleInstallSnapshot state id
              | Msg.Update(origin,sm)           -> handleUpdate state origin sm inbox
              | Msg.ServerEvent(ev)             -> handleServerEvent state ev inbox
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

  let private start (mem: RaftMember)
                    (projectId: Id)
                    (store: IAgentStore<ServerState>)
                    (agent: ApiAgent) =
    either {
      let pubsub =
        PubSub.create
          mem.Id                        // to deduplicate messages sent from this process
          PubSub.defaultAddress
          (int Constants.MCAST_PORT)

      let server = TcpServer.create {
        ServerId = mem.Id
        Listen = mem.IpAddr
        Port = mem.ApiPort
      }

      match server.Start()  with
      | Right () ->
        match pubsub.Start() with
        | Right () ->
          let srv = server.Subscribe (Msg.ServerEvent >> agent.Post)
          let pbsb = pubsub.Subscribe(processSubscriptionEvent mem.Id agent)

          let updated =
            { store.State with
                Status = ServiceStatus.Running
                PubSub = pubsub
                Server = server
                Disposables = [ srv; pbsb ] }

          store.Update updated
          agent.Start()
          agent.Post Msg.Start

        | Left error ->
          dispose server
          dispose pubsub
          return! Either.fail error

      | Left error ->
        return! Either.fail error
    }

  // ** create

  let create (mem: RaftMember) (projectId: Id) callbacks =
    either {
      let cts = new CancellationTokenSource()

      let store = AgentStore.create ()

      store.Update {
        Id = mem.Id
        Status = ServiceStatus.Stopped
        Server = Unchecked.defaultof<IServer>
        PubSub = Unchecked.defaultof<IPubSub>
        Clients = Map.empty
        Subscriptions = Subscriptions()
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
              if Service.isRunning store.State.Status then
                match ev with
                | IrisEvent.Append (_, LogMsg log) when log.Id <> mem.Id -> ()
                | IrisEvent.Append (_, cmd) ->
                  updateAllClients store.State cmd
                  publish store.State cmd agent
                | _ -> ()

            // *** Start

            member self.Start () = start mem projectId store agent

            // *** Clients

            member self.Clients
              with get () = store.State.Clients |> Map.map (fun _ client -> client)

            // *** SendSnapshot

            member self.SendSnapshot () =
              Map.iter (fun id _ -> id |> Msg.InstallSnapshot |> agent.Post) store.State.Clients

            // *** Update

            member self.Update (origin: Origin) (sm: StateMachine) =
              if Service.isRunning store.State.Status then
                updateAllClients store.State sm
                publish store.State sm agent

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
