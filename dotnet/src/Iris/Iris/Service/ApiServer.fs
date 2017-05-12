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

  let private tag (str: string) = String.Format("ApiServer.{0}",str)

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

  // ** ServerState

  [<NoComparison;NoEquality>]
  type private ServerState =
    { Status: ServiceStatus
      Store: Store
      Server: IBroker
      Publisher: Pub
      Subscriber: Sub
      Clients: Map<Id,Client>
      Subscriptions: Subscriptions
      Disposables: IDisposable list }

    interface IDisposable with
      member data.Dispose() =
        data.Subscriptions.Clear()
        List.iter dispose data.Disposables
        dispose data.Publisher
        dispose data.Subscriber
        dispose data.Server
        Map.iter (fun _ v -> dispose v) data.Clients

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start
    | SetStatus         of status:ServiceStatus
    | AddClient         of client:IrisClient
    | RemoveClient      of client:IrisClient
    | SetClientStatus   of id:Id * status:ServiceStatus
    | SetState          of state:State
    | InstallSnapshot   of id:Id
    | LocalUpdate       of sm:StateMachine
    | RemoteUpdate      of sm:StateMachine
    | ClientUpdate      of sm:StateMachine
    | RawServerRequest  of req:RawServerRequest
    | RawClientResponse of req:RawClientResponse

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

  // ** requestInstallSnapshot

  let private requestInstallSnapshot (state: ServerState) (client: Client) (agent: ApiAgent) =
    state.Store.State
    |> ClientApiRequest.Snapshot
    |> Binary.encode
    |> fun body -> { Body = body }
    |> client.Socket.Request
    |> Either.mapError (string >> Logger.err "requestInstallSnapshot")
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

        if not socket.Running then
          socket.Restart()

        ClientApiRequest.Ping
        |> Binary.encode
        |> fun body -> { Body = body }
        |> socket.Request
        |> Either.mapError (string >> Logger.err "pingTimer")
        |> ignore

        // match response with
        // | Right Pong ->
        //   // ping request successful
        //   (socket.Id, ServiceStatus.Running)
        //   |> Msg.SetClientStatus
        //   |> agent.Post

        // | Left error ->
        //   // log this error
        //   string error
        //   |> sprintf "error during to %s: %s" (string socket.Id)
        //   |> Logger.err (tag "pingTimer")

        //   // set the status of this client to error
        //   (socket.Id, ServiceStatus.Failed error)
        //   |> Msg.SetClientStatus
        //   |> agent.Post
        // | _ -> ()

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
        notify state.Subscriptions (ApiEvent.UnRegister client.Meta)

      // construct a new client value
      let addr = Uri.tcpUri meta.IpAddress (Some meta.Port)
      let socket = Client.create {
        PeerId = meta.Id
        Frontend = addr
        Timeout = int Constants.REQ_TIMEOUT * 1<ms>
      }

      let client =
        { Meta = meta
          Socket = socket
          Timer = pingTimer socket agent }

      meta.Id |> Msg.InstallSnapshot |> agent.Post
      meta |> ApiEvent.Register |> notify state.Subscriptions

      { state with Clients = Map.add meta.Id client state.Clients }

  // ** handleRemoveClient

  let private handleRemoveClient (state: ServerState) (peer: IrisClient) =
    Tracing.trace (tag "handleRemoveClient") <| fun () ->
        match Map.tryFind peer.Id state.Clients with
        | Some client ->
          dispose client
          peer |> ApiEvent.UnRegister |> notify state.Subscriptions
          { state with Clients = Map.remove peer.Id state.Clients }
        | _ -> state

  // ** updateClient

  let private updateClient (sm: StateMachine) (client: Client) =
    // if not client.Socket.Running then
    //   client.Socket.Restart()

    sm
    |> ClientApiRequest.Update
    |> Binary.encode
    |> fun body -> { Body = body }
    |> client.Socket.Request
    |> Either.mapError (string >> Logger.err "updateClient")
    |> ignore

    // match result with
    // | Right ApiResponse.OK | Right ApiResponse.Pong ->
    //   return Either.succeed ()
    // | Right (ApiResponse.NOK err) ->
    //   let error =
    //     string err
    //     |> Error.asClientError (tag "updateClient")
    //   return  Either.fail (client.Meta.Id, error)
    // | Left error ->
    //   return Either.fail (client.Meta.Id, error)

  // ** updateClients

  let private updateClients (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace (tag "updateClients") <| fun () ->
      state.Clients
      |> Map.toArray
      |> Array.Parallel.map (snd >> updateClient sm)
      |> ignore

  // ** maybePublish

  let private maybePublish (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    match sm with
    | UpdateSlices _ | CallCue _ ->
      Tracing.trace (tag "maybePublish") <| fun () ->
        sm
        |> Binary.encode
        |> state.Publisher.Publish
        |> Either.mapError (ServiceStatus.Failed >> Msg.SetStatus >> agent.Post)
        |> ignore
    | _ -> ()

  // ** maybeDispatch

  let private maybeDispatch (state: ServerState) (sm: StateMachine) =
    match sm with
    | UpdateSlices _ | CallCue _ -> state.Store.Dispatch sm
    | _ -> ()

  // ** handleSetStatus

  let private handleSetStatus (state: ServerState) (status: ServiceStatus) =
    Tracing.trace (tag "handleSetStatus") <| fun () ->
      notify state.Subscriptions (ApiEvent.ServiceStatus status)
      { state with Status = status }

  // ** handleSetClientStatus

  let private handleSetClientStatus (state: ServerState) (id: Id) (status: ServiceStatus) =
    Tracing.trace (tag "handleSetClientStatus") <| fun () ->
      match Map.tryFind id state.Clients with
      | Some client ->
        match client.Meta.Status, status with
        | ServiceStatus.Running, ServiceStatus.Running -> state
        | oldst, newst ->
          if oldst <> newst then
            let updated = { client with Meta = { client.Meta with Status = status } }
            updated.Meta |> ApiEvent.ClientStatus |> notify state.Subscriptions
            { state with Clients = Map.add id updated state.Clients }
          else state
      | None -> state

  // ** handleSetState

  let private handleSetState (state: ServerState) (newstate: State) (agent: ApiAgent) =
    Tracing.trace (tag "handleSetState") <| fun () ->
      Map.iter (fun id _ -> id |> Msg.InstallSnapshot |> agent.Post) state.Clients
      { state with Store = new Store(newstate) }

  // ** handleInstallSnapshot

  let private handleInstallSnapshot (state: ServerState) (id: Id) (agent: ApiAgent) =
    Tracing.trace (tag "handleInstallSnapshot") <| fun () ->
      match Map.tryFind id state.Clients with
      | Some client -> requestInstallSnapshot state client agent
      | None -> ()
      state

  // ** handleClientUpdate

  let private handleClientUpdate (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace (tag "handleClientUpdate") <| fun () ->
      maybeDispatch state sm
      maybePublish state sm agent
      notify state.Subscriptions (ApiEvent.Update sm)
      state

  // ** handleRemoteUpdate

  let private handleRemoteUpdate (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace (tag "handleRemoteUpdate") <| fun () ->
      maybeDispatch state sm             // we need to send these request synchronously
      updateClients state sm agent       // in order to preserve ordering of the messages
      notify state.Subscriptions (ApiEvent.Update sm)
      state

  // ** handleLocalUpdate

  let private handleLocalUpdate (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace (tag "handleLocalUpdate") <| fun () ->
      state.Store.Dispatch sm            // we need to send these request synchronously
      maybePublish state sm agent        // in order to preserve ordering of the messages
      updateClients state sm agent
      state

  // ** handleServerRequest

  let private handleServerRequest (state: ServerState) (req: RawServerRequest) (agent: ApiAgent) =
    Tracing.trace (tag "handleServerRequest") <| fun () ->
      match req.Body |> Binary.decode with
      | Right (Register client) ->
        client |> Msg.AddClient |> agent.Post
        OK |> Binary.encode |> RawServerResponse.fromRequest req |> state.Server.Respond

      | Right (UnRegister client) ->
        client |> Msg.RemoveClient |> agent.Post
        OK |> Binary.encode |> RawServerResponse.fromRequest req |> state.Server.Respond

      | Right (Update sm) ->
        sm |> Msg.ClientUpdate |> agent.Post
        OK |> Binary.encode |> RawServerResponse.fromRequest req |> state.Server.Respond

      | Left error ->
        string error
        |> ApiError.Internal
        |> NOK
        |> Binary.encode
        |> RawServerResponse.fromRequest req
        |> state.Server.Respond
      state

  // ** handleClientResponse

  let private handleClientResponse (state: ServerState) (resp: RawClientResponse) (agent: ApiAgent) =
    failwith "never"

  // ** loop

  let private loop (store: IAgentStore<ServerState>) (inbox: ApiAgent) =
    let rec act () =
      async {
        let! msg = inbox.Receive()
        let state = store.State
        let newstate =
          try
            match msg with
            | Msg.Start                       -> handleStart state inbox
            | Msg.AddClient(client)           -> handleAddClient state client inbox
            | Msg.RemoveClient(client)        -> handleRemoveClient state client
            | Msg.SetClientStatus(id, status) -> handleSetClientStatus state id status
            | Msg.SetState(newstate)          -> handleSetState state newstate inbox
            | Msg.SetStatus(status)           -> handleSetStatus state status
            | Msg.InstallSnapshot(id)         -> handleInstallSnapshot state id inbox
            | Msg.LocalUpdate(sm)             -> handleLocalUpdate state sm inbox
            | Msg.ClientUpdate(sm)            -> handleClientUpdate state sm inbox
            | Msg.RemoteUpdate(sm)            -> handleRemoteUpdate state sm inbox
            | Msg.RawServerRequest(req)       -> handleServerRequest state req inbox
            | Msg.RawClientResponse(resp)     -> handleClientResponse state resp inbox
          with
            | exn ->
              exn.Message + exn.StackTrace
              |> sprintf "Error in loop: %O"
              |> Logger.err "ApiServer"
              state
        store.Update newstate
        return! act ()
      }
    act ()

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

        let state = {
          Status = ServiceStatus.Stopped
          Store = Store(State.Empty)
          Server = Unchecked.defaultof<IBroker>
          Publisher = Unchecked.defaultof<Pub>
          Subscriber = Unchecked.defaultof<Sub>
          Clients = Map.empty
          Subscriptions = Subscriptions()
          Disposables = []
        }

        let store = AgentStore.create state
        let agent = new ApiAgent(loop store, cts.Token)

        return
          { new IApiServer with
              member self.Start () = either {
                  let frontend = Uri.tcpUri mem.IpAddr (mem.ApiPort |> port |> Some)
                  let backend = Uri.inprocUri Constants.API_BACKEND_PREFIX (mem.Id |> string |> Some)

                  let pubSubAddr =
                    Uri.epgmUri
                      mem.IpAddr
                      (IPv4Address Constants.MCAST_ADDRESS)
                      (port Constants.MCAST_PORT)

                  let publisher = new Pub(unwrap pubSubAddr, string projectId)
                  let subscriber = new Sub(unwrap pubSubAddr, string projectId)

                  let result = Broker.create {
                    Id = mem.Id
                    MinWorkers = 5uy
                    MaxWorkers = 20uy
                    Frontend = frontend
                    Backend = backend
                    RequestTimeout = int Constants.REQ_TIMEOUT * 1<ms>
                  }

                  match result  with
                  | Right server ->
                    match publisher.Start(), subscriber.Start() with
                    | Right (), Right () ->
                      let srv = server.Subscribe (Msg.RawServerRequest >> agent.Post)
                      let sub = subscriber.Subscribe(processSubscriptionEvent agent)

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

              member self.Clients
                with get () = store.State.Clients |> Map.map (fun id client -> client.Meta)

              member self.State
                with get () = store.State.Store.State
                 and set state = state |> Msg.SetState |> agent.Post

              member self.Update (sm: StateMachine) =
                agent.Post(Msg.LocalUpdate sm)

              member self.Subscribe (callback: ApiEvent -> unit) =
                let guid = Guid.NewGuid()
                let listener = createListener guid store.State.Subscriptions
                { new IObserver<ApiEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Dispose () =
                dispose cts
                dispose store.State
            }
      }
