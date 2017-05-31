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

  type private Subscriptions = Subscriptions<IrisEvent>

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
      Store: Store
      Server: IServer
      Publisher: Pub
      Subscriber: Sub
      Clients: Map<Id,Client>
      Context: ZContext
      Subscriptions: Subscriptions
      Disposables: IDisposable list
      Stopper: AutoResetEvent }

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
    | SetState          of state:State
    | InstallSnapshot   of id:Id
    | Update            of origin:Origin * sm:StateMachine
    | RawServerRequest  of req:RawServerRequest
    | RawClientResponse of req:RawClientResponse

  // ** ApiAgent

  type private ApiAgent = MailboxProcessor<Msg>

  // ** requestInstallSnapshot

  let private requestInstallSnapshot (state: ServerState) (client: Client) (agent: ApiAgent) =
    state.Store.State
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

  let private processSubscriptionEvent (agent: ApiAgent) (bytes: byte array) =
    match Binary.decode bytes with
    | Right command ->
      match command with
      | UpdateSlices _ | CallCue _ -> Msg.Update(Origin.Api, command) |> agent.Post
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
        (Origin.Service, RemoveClient client.Meta)
        |> IrisEvent.Append
        |> Observable.notify state.Subscriptions

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
        |> Observable.notify state.Subscriptions

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
        |> Observable.notify state.Subscriptions
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

  // ** updateClients

  let private updateClients (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    Tracing.trace (tag "updateClients") <| fun () ->
      state.Clients
      |> Map.toArray
      |> Array.Parallel.map (snd >> updateClient sm)
      |> ignore

  // ** maybePublish

  let private maybePublish (state: ServerState) (sm: StateMachine) (agent: ApiAgent) =
    sm
    |> Binary.encode
    |> state.Publisher.Publish
    |> Either.mapError (ServiceStatus.Failed >> Msg.SetStatus >> agent.Post)
    |> ignore

  // ** maybeDispatch

  let private maybeDispatch (state: ServerState) (sm: StateMachine) =
    match sm with
    | UpdateSlices _ | CallCue _ -> state.Store.Dispatch sm
    | _ -> ()

  // ** handleSetStatus

  let private handleSetStatus (state: ServerState) (status: ServiceStatus) =
    Tracing.trace (tag "handleSetStatus") <| fun () ->
      status
      |> IrisEvent.Status
      |> Observable.notify state.Subscriptions
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
            (Origin.Service, UpdateClient updated.Meta)
            |> IrisEvent.Append
            |> Observable.notify state.Subscriptions
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

  // ** handleUpdate

  let private handleUpdate (state: ServerState)
                           (origin: Origin)
                           (cmd: StateMachine)
                           (agent: ApiAgent) =
    match origin with
    | Origin.Api ->
      maybeDispatch state cmd             // we need to send these request synchronously
      updateClients state cmd agent       // in order to preserve ordering of the messages
      (origin, cmd)
      |> IrisEvent.Append
      |> Observable.notify state.Subscriptions

    | Origin.Raft ->
      maybeDispatch state cmd             // we need to send these request synchronously
      updateClients state cmd agent       // in order to preserve ordering of the messages

    | Origin.Client id ->
      maybeDispatch state cmd
      maybePublish state cmd agent
      (origin, cmd)
      |> IrisEvent.Append
      |> Observable.notify state.Subscriptions

    | Origin.Web id ->
      maybeDispatch state cmd             // we need to send these request synchronously
      maybePublish state cmd agent
      updateClients state cmd agent       // in order to preserve ordering of the messages

    | Origin.Service _ ->
      (origin, cmd)
      |> IrisEvent.Append
      |> Observable.notify state.Subscriptions
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
        let! msg = inbox.Receive()
        let state = store.State
        let newstate =
          try
            match msg with
            | Msg.Start                       -> handleStart state inbox
            | Msg.Stop                        -> handleStop state
            | Msg.AddClient(client)           -> handleAddClient state client inbox
            | Msg.RemoveClient(client)        -> handleRemoveClient state client
            | Msg.SetClientStatus(id, status) -> handleSetClientStatus state id status
            | Msg.SetState(newstate)          -> handleSetState state newstate inbox
            | Msg.SetStatus(status)           -> handleSetStatus state status
            | Msg.InstallSnapshot(id)         -> handleInstallSnapshot state id inbox
            | Msg.Update(origin,sm)           -> handleUpdate state origin sm inbox
            | Msg.RawServerRequest(req)       -> handleServerRequest state req inbox
            | Msg.RawClientResponse(resp)     -> handleClientResponse state resp inbox
          with
            | exn ->
              exn.Message + exn.StackTrace
              |> sprintf "Error in loop: %O"
              |> Logger.err (tag "loop")
              state
        if not (Service.isStopping newstate.Status) then
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

    // *** create

    let create ctx (mem: RaftMember) (projectId: Id) =
      either {
        let cts = new CancellationTokenSource()

        let state = {
          Id = mem.Id
          Status = ServiceStatus.Stopped
          Store = Store(State.Empty)
          Server = Unchecked.defaultof<IServer>
          Publisher = Unchecked.defaultof<Pub>
          Subscriber = Unchecked.defaultof<Sub>
          Clients = Map.empty
          Subscriptions = Subscriptions()
          Context = ctx
          Disposables = []
          Stopper = new AutoResetEvent(false)
        }

        let store = AgentStore.create ()
        store.Update state

        let agent = new ApiAgent(loop store, cts.Token)
        agent.Error.Add(sprintf "unhandled error on actor loop: %O" >> Logger.err (tag "loop"))

        return
          { new IApiServer with

              // **** Start

              member self.Start () = either {
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

              // **** Clients

              member self.Clients
                with get () = store.State.Clients |> Map.map (fun id client -> client.Meta)

              // **** State

              member self.State
                with get () = store.State.Store.State
                 and set state = state |> Msg.SetState |> agent.Post

              // **** Update

              member self.Update (origin: Origin) (sm: StateMachine) =
                (origin, sm) |> Msg.Update |> agent.Post

              // **** Subscribe

              member self.Subscribe (callback: IrisEvent -> unit) =
                let listener = Observable.createListener store.State.Subscriptions
                { new IObserver<IrisEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              // **** Dispose

              member self.Dispose () =
                agent.Post Msg.Stop
                if not (store.State.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
                  Logger.debug (tag "Dispose") "timeout: attempt to dispose api server failed"
                dispose cts
            }
      }
