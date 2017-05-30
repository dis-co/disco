namespace Iris.Service

// * Imports

#if !IRIS_NODES

open System
open System.IO
open System.Threading
open System.Collections.Concurrent
open Iris.Raft
open Iris.Zmq
open Iris.Core
open Iris.Core.Utils
open Iris.Core.Commands
open Iris.Service.Interfaces
open Iris.Service.Persistence
open Iris.Service.Git
open Iris.Service.WebSockets
open Iris.Service.Raft
open Iris.Service.Http
open Microsoft.FSharp.Control
open FSharpx.Functional
open LibGit2Sharp
open SharpYaml.Serialization
open ZeroMQ

// * IrisService

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

[<AutoOpen>]
module IrisService =
  // ** tag

  let private tag (str: string) =
    String.Format("IrisService.{0}", str)

  // ** keys

  [<Literal>]
  let private API_SERVER = "api"

  [<Literal>]
  let private GIT_SERVER = "git"

  [<Literal>]
  let private LOG_HANDLER = "log"

  [<Literal>]
  let private RAFT_SERVER = "raft"

  [<Literal>]
  let private WS_SERVER = "ws"

  [<Literal>]
  let private CLOCK_SERVICE = "clock"

  // ** Subscriptions

  /// ## Subscriptions
  ///
  /// Type alias for IObserver subscriptions.
  ///
  type Subscriptions = ConcurrentDictionary<Guid,IObserver<IrisEvent>>

  // ** disposeAll

  /// ## disposeAll
  ///
  /// Dispose all resource in the passed `seq`.
  ///
  /// ### Signature:
  /// - disposables: IDisposable seq
  ///
  /// Returns: unit
  let inline private disposeAll (disposables: Map<_,IDisposable>) =
    Map.iter (konst dispose) disposables

  // ** Leader

  [<NoComparison;NoEquality>]
  type private Leader =
    { Member: RaftMember
      Socket: IClient }

    interface IDisposable with
      member self.Dispose() =
        dispose self.Socket

  // ** IrisState

  /// ## IrisState
  ///
  /// Encapsulate all service-internal state to hydrate an `IrisAgent` with a project loaded.
  /// As the actor receives messages, it uses (and updates) this record and passes it on.
  /// For ease of use it implements the IDisposable interface.
  ///
  /// ### Fields:
  /// - MemberId: ServiceStatus of currently loaded project
  /// - Status: ServiceStatus of currently loaded project
  /// - Store: Store containing all state. This is sent to user via WebSockets on connection.
  /// - Project: IrisProject currently loaded
  /// - GitServer: IGitServer for current project
  /// - RaftServer: IRaftServer for current project
  /// - SocketServer: IWebSocketServer for current project
  /// - Disposables: IDisposable list for Observables and the like
  ///
  [<NoComparison;NoEquality>]
  type private IrisState =
    { Member         : RaftMember
      Machine        : IrisMachine
      Status         : ServiceStatus
      Store          : Store
      Leader         : Leader option
      ApiServer      : IApiServer
      GitServer      : IGitServer
      RaftServer     : IRaftServer
      SocketServer   : IWebSocketServer
      ClockService   : IClock
      Subscriptions  : Subscriptions
      Disposables    : Map<string,IDisposable>
      Context        : ZContext }

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        self.Subscriptions.Clear()
        disposeAll self.Disposables
        Option.iter dispose self.Leader
        dispose self.ApiServer
        dispose self.GitServer
        dispose self.RaftServer
        dispose self.ClockService
        dispose self.SocketServer
        dispose self.Context

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start
    | Stop              of AutoResetEvent
    | Notify            of IrisEvent
    | Event             of IrisEvent
    | SetConfig         of IrisConfig
    | RawClientResponse of RawClientResponse
    | ForceElection
    | Periodic
    // | Join              of IpAddress  * uint16
    // | Leave

  // ** IrisAgent

  /// ## IrisAgent
  ///
  /// Type alias for internal state mutation actor.
  ///
  type private IrisAgent = MailboxProcessor<Msg>

  // ** handleNotify

  let private handleNotify (state: IrisState) (ev: IrisEvent) =
    Observable.notify state.Subscriptions ev
    state

  // ** broadcastMsg

  let private broadcastMsg (state: IrisState) (cmd: StateMachine) =
    cmd
    |> state.SocketServer.Broadcast
    |> ignore

  // ** sendMsg

  let private sendMsg (state: IrisState) (id: Id) (cmd: StateMachine) =
    Tracing.trace (tag "sendMsg") <| fun () ->
      cmd
      |> state.SocketServer.Send id
      |> ignore

  // ** appendCmd

  let private appendCmd (state: IrisState) (cmd: StateMachine) =
    if state.RaftServer.IsLeader then
      match cmd with
      | AddMember mem -> state.RaftServer.AddMember mem
      | RemoveMember mem -> state.RaftServer.RemoveMember mem.Id
      | other -> state.RaftServer.Append other
    else
      "ignoring append request, not leader"
      |> Logger.debug (tag "appendCmd")


  // ** publishCmd

  let private publishCmd (state: IrisState) cmd =
    state.ApiServer.Update cmd
    state.Store.Dispatch cmd
    broadcastMsg state cmd

  // ** ignoreEvent

  let private ignoreEvent (state: IrisState) _ = state

  // ** onConfigured

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  // ** persistLog

  /// ## persistLog
  ///
  /// Register a callback to be run when an appended entry is considered safely appended to a
  /// majority of servers logs. The entry then is regarded as applied.
  ///
  /// In this callback implementation we essentially do 3 things:
  ///
  ///   - the state machine command is applied to the store, potentially altering its state
  ///   - the state machine command is broadcast to all clients
  ///   - the state machine command is persisted to disk (potentially recorded in a git commit)

  let private persistLog (state: IrisState) (sm: StateMachine) =
    if state.RaftServer.IsLeader then
      match persistEntry state.Store.State sm with
      | Right commit ->
        sprintf "Persisted command %s in commit: %s" (string sm) commit.Sha
        |> Logger.debug (tag "persistLog")
        state
      | Left error ->
        sprintf "Error persisting command: %A" error
        |> Logger.err (tag "persistLog")
        state
    else
      let raft = state.RaftServer.Raft
      let mem =
        raft
        |> Raft.currentLeader
        |> Option.bind (flip Raft.getMember raft)

      match mem with
      | Some leader ->
        match updateRepo state.Store.State.Project leader with
        | Right () -> ()
        | Left error ->
          error
          |> string
          |> Logger.err (tag "persistLog")
      | None -> ()
      state

  // ** makeLeader

  let private makeLeader (leader: RaftMember)
                         (construct: ClientConfig -> Either<IrisError,IClient>)
                         (agent: IrisAgent) =
    let result = construct {
      PeerId = leader.Id
      Frontend = Uri.raftUri leader
      Timeout = int Constants.REQ_TIMEOUT * 1<ms>
    }
    match result with
    | Right socket ->
      socket.Subscribe (Msg.RawClientResponse >> agent.Post) |> ignore
      Some { Member = leader; Socket = socket }
    | Left error ->
      error
      |> sprintf "error creating connection for leader: %O"
      |> Logger.err (tag "makeLeader")
      None

  // ** stateChanged

  let private stateChanged (state: IrisState)
                           (oldstate: RaftState)
                           (newstate: RaftState)
                           (agent: IrisAgent) =
    newstate
    |> sprintf "Raft state changed from %A to %A" oldstate
    |> Logger.debug (tag "stateChanged")
    // create redirect socket
    match oldstate, newstate with
    | _, Follower ->
      Option.iter dispose state.Leader
      match state.RaftServer.Leader with
      | Some leader ->
        let makePeerSocket = Client.create state.Context
        { state with Leader = makeLeader leader makePeerSocket agent }
      | None ->
        "Could not start re-direct socket: no leader"
        |> Logger.debug (tag "stateChanged")
        state
    | _, Leader ->
      Option.iter dispose state.Leader
      { state with Leader = None }
    | _ -> state

  // ** retrieveSnapshot

  let private retrieveSnapshot (state: IrisState) =
    let path = Constants.RAFT_DIRECTORY <.>
               Constants.SNAPSHOT_FILENAME +
               Constants.ASSET_EXTENSION
    match Asset.read path with
    | Right str ->
      try
        let serializer = new Serializer()
        let yml = serializer.Deserialize<SnapshotYaml>(str)

        let members =
          match Config.getActiveSite state.Store.State.Project.Config with
          | Some site -> site.Members |> Map.toArray |> Array.map snd
          | _ -> [| |]

        Snapshot(Id yml.Id
                ,yml.Index
                ,yml.Term
                ,yml.LastIndex
                ,yml.LastTerm
                ,members
                ,DataSnapshot state.Store.State)
        |> Some
      with
        | exn ->
          exn.Message
          |> Logger.err (tag "retrieveSnapshot")
          None

    | Left error ->
      error
      |> string
      |> Logger.err (tag "retrieveSnapshot")
      None

  // ** persistSnapshot

  let private persistSnapshot (state: IrisState) (log: RaftLogEntry) =
    match persistSnapshot state.Store.State log with
    | Left error -> Logger.err (tag "persistSnapshot") (string error)
    | _ -> ()
    state

  // ** requestAppend

  let private requestAppend (self: MemberId) (leader: Leader) (sm: StateMachine) =
    AppendEntry sm
    |> Binary.encode
    |> fun body -> { RequestId = Guid.NewGuid(); Body = body }
    |> leader.Socket.Request
    |> Either.mapError (sprintf "request error: %O" >> Logger.err (tag "requestAppend"))
    |> ignore

    //   match result with
    //   | Right (AppendEntryResponse _) ->
    //     Either.succeed leader
    //   | Right (Redirect mem) ->
    //     if count < max then
    //       dispose leader
    //       let newleader = mkLeader self mem
    //       impl newleader (count + 1)
    //     else
    //       max
    //       |> sprintf "Maximum re-direct count reached (%d). Appending failed"
    //       |> Error.asRaftError (tag "requestAppend")
    //       |> Either.fail
    //   | Right other ->
    //     other
    //     |> sprintf "Received unexpected response from server: %A"
    //     |> Error.asRaftError (tag "requestAppend")
    //     |> Either.fail
    //   | Left error ->
    //     Either.fail error

    // Tracing.trace (tag "requestAppend") <| fun () ->
    //   impl leader 0

  // ** forwardCommand

  let private forwardCommand (state: IrisState) (sm: StateMachine) (agent: IrisAgent) =
    Tracing.trace (tag "forwardCommand") <| fun () ->
      match state.Leader with
      | Some leader ->
        requestAppend state.Member.Id leader sm
        state
      | None ->
        match state.RaftServer.Leader with
        | Some mem ->
          let makePeerSocket = Client.create state.Context
          let leader = makeLeader mem makePeerSocket agent
          Option.iter (fun leader -> requestAppend state.Member.Id leader sm) leader
          { state with Leader = leader }
        | None ->
          "Could not start re-direct socket: No Known Leader"
          |> Logger.debug (tag "forwardCommand")
          state

  // ** handleRaftEvent

  let private handleRaftEvent (state: IrisState) (agent: IrisAgent) (ev: RaftEvent) =
    ev |> IrisEvent.Raft |> Msg.Event |> agent.Post

  // ** forwardEvent

  let inline private forwardEvent (constr: ^a -> IrisEvent) (agent: IrisAgent) =
    constr >> Msg.Event >> agent.Post

  // ** restartGitServer

  let private restartGitServer (state: IrisState) (agent: IrisAgent) =
    Tracing.trace (tag "restartGitServer") <| fun () ->
      state.Disposables
      |> Map.tryFind GIT_SERVER
      |> Option.map dispose
      |> ignore

      dispose state.GitServer

      let result =
        either {
          let mem = state.RaftServer.Member
          let gitserver = GitServer.create mem state.Store.State.Project.Path
          let disposable =
            agent
            |> forwardEvent IrisEvent.Git
            |> gitserver.Subscribe
          match gitserver.Start() with
          | Right () ->
            return { state with
                      GitServer = gitserver
                      Disposables = Map.add GIT_SERVER disposable state.Disposables }
          | Left error ->
            dispose disposable
            dispose gitserver
            return! Either.fail error
        }

      match result with
      | Right newdata -> newdata
      | Left error ->
        error
        |> string
        |> Logger.err (tag "restartGitServer")
        state

  //  _
  // | |    ___   __ _
  // | |   / _ \ / _` |
  // | |__| (_) | (_| |
  // |_____\___/ \__, |
  //             |___/

  // ** handleLogEvent

  let private handleLogEvent (state: IrisState) (log: LogEvent) =
    log
    |> LogMsg
    |> broadcastMsg state
    state

  // ** handleSetConfig

  let private handleSetConfig (state: IrisState) (config: IrisConfig) =
    Tracing.trace (tag "handleSetConfig") <| fun () ->
      Project.updateConfig config state.Store.State.Project
      |> UpdateProject
      |> state.Store.Dispatch
      state

  // ** handleForceElection

  let private handleForceElection (state: IrisState) =
    Tracing.trace (tag "handleForceElection") <| fun () ->
      state.RaftServer.ForceElection()
      state

  // ** handlePeriodic

  let private handlePeriodic (state: IrisState) =
    Tracing.trace (tag "handlePeriodic") <| fun () ->
      state.RaftServer.Periodic()
      state

  // ** handleJoin

  // let private handleJoin (state: IrisState) (chan: ReplyChan) (ip: IpAddress) (port: uint16) =
  //   Tracing.trace (tag "handleJoin") <| fun () ->
  //     match state.RaftServer.JoinCluster ip port with
  //     | Right () ->
  //       Reply.Ok
  //       |> Either.succeed
  //       |> chan.Reply
  //     | Left error ->
  //       error
  //       |> Either.fail
  //       |> chan.Reply
  //     state

  // ** handleLeave

  // let private handleLeave (state: IrisState) (chan: ReplyChan) =
  //   withDefaultReply state chan <| fun data ->
  //     asynchronously <| fun _ ->
  //       Tracing.trace (tag "handleLeave") <| fun () ->
  //         match data.RaftServer.LeaveCluster () with
  //         | Right () ->
  //           Reply.Ok
  //           |> Either.succeed
  //           |> chan.Reply
  //         | Left error ->
  //           error
  //           |> Either.fail
  //           |> chan.Reply
  //     state

  // ** handleClientResponse

  let private handleClientResponse (state: IrisState) (response: RawClientResponse) =
    state

  // ** handleStart

  let private handleStart (state: IrisState) (agent: IrisAgent) =
    let status = ServiceStatus.Running
    status |> IrisEvent.Status |> Msg.Notify |> agent.Post
    { state with Status = status }

  // ** handleStop

  let private handleStop (state: IrisState) (agent: IrisAgent) (are: AutoResetEvent) =
    let status = ServiceStatus.Stopping
    status |> IrisEvent.Status |> Msg.Notify |> agent.Post
    are.Set() |> ignore
    { state with Status = status }

  // ** publishEvent

  let private publishEvent (state: IrisState) (agent: IrisAgent) (ev: IrisEvent) =
    match ev with
    //     _          _
    //    / \   _ __ (_)
    //   / _ \ | '_ \| |
    //  / ___ \| |_) | |
    // /_/   \_\ .__/|_|
    //         |_|
    | Api (ApiEvent.Update cmd) ->
      publishCmd state cmd
      state

    //   ____ _ _
    //  / ___(_) |_
    // | |  _| | __|
    // | |_| | | |_
    //  \____|_|\__|

    | Git (GitEvent.Started pid) ->
      sprintf "Git daemon started with PID: %d" pid
      |> Logger.debug (tag "handleGitEvent")
      state

    | Git (GitEvent.Exited _) ->
      "Git daemon exited. Attempting to restart."
      |> Logger.debug (tag "handleGitEvent")
      restartGitServer state agent

    | Git (GitEvent.Failed reason) ->
      reason
      |> sprintf "Git daemon failed. %A Attempting to restart."
      |> Logger.debug (tag "handleGitEvent")
      restartGitServer state agent

    | Git (GitEvent.Pull (_, addr, port)) ->
      sprintf "Client %s:%d pulled updates from me" addr port
      |> Logger.debug (tag "handleGitEvent")
      state

    //  ____             _        _
    // / ___|  ___   ___| | _____| |_
    // \___ \ / _ \ / __| |/ / _ \ __|
    //  ___) | (_) | (__|   <  __/ |_
    // |____/ \___/ \___|_|\_\___|\__|

    | Socket (OnMessage(_, cmd)) ->
      publishCmd state cmd
      state

    //  ____        __ _
    // |  _ \ __ _ / _| |_
    // | |_) / _` | |_| __|
    // |  _ < (_| |  _| |_
    // |_| \_\__,_|_|  \__|

    | Raft (ApplyLog sm) ->
      publishCmd state sm
      persistLog state sm

    | Raft (MemberAdded mem) ->
      mem |> AddMember    |> publishCmd state
      state

    | Raft (MemberRemoved mem) ->
      mem |> RemoveMember |> publishCmd state
      state

    | Raft (MemberUpdated mem) ->
      mem |> UpdateMember |> publishCmd state
      state

    | Raft (Configured mems) ->
      mems
      |> Array.map (Member.getId >> string)
      |> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
      |> Logger.debug (tag "publishEvent")
      state

    | Raft (PersistSnapshot log) ->
      persistSnapshot  state log

    | Raft (StateChanged (ost, nst)) ->
      stateChanged     state ost nst agent

    //   ____ _            _
    //  / ___| | ___   ___| | __
    // | |   | |/ _ \ / __| |/ /
    // | |___| | (_) | (__|   <
    //  \____|_|\___/ \___|_|\_\

    | Clock clock ->
      clock.Frame |> uint32 |> UpdateClock |>  publishCmd state
      state

    | Log log ->                         // ?
      state

    //   ___  _   _
    //  / _ \| |_| |__   ___ _ __
    // | | | | __| '_ \ / _ \ '__|
    // | |_| | |_| | | |  __/ |
    //  \___/ \__|_| |_|\___|_|

    | other ->
      ignore other
      state

  // ** replicateEvent

  let private replicateEvent (state: IrisState) (ev: IrisEvent) =
    match ev with
    //     _          _
    //    / \   _ __ (_)
    //   / _ \ | '_ \| |
    //  / ___ \| |_) | |
    // /_/   \_\ .__/|_|
    //         |_|
    | Api (ApiEvent.Register client)     -> client |> AddClient    |> appendCmd state
    | Api (ApiEvent.ClientStatus client) -> client |> UpdateClient |> appendCmd state
    | Api (ApiEvent.UnRegister client)   -> client |> RemoveClient |> appendCmd state
    | Api (ApiEvent.Update cmd)          -> cmd    |> appendCmd state

    //  ____             _        _
    // / ___|  ___   ___| | _____| |_
    // \___ \ / _ \ / __| |/ / _ \ __|
    //  ___) | (_) | (__|   <  __/ |_
    // |____/ \___/ \___|_|\_\___|\__|

    // first, send a snapshot to the new browser session to bootstrap it
    | Socket (SessionAdded id) ->
      sendMsg state id (DataSnapshot state.Store.State)

    // next, replicate AddSession to other Raft nodes
    | Socket (OnMessage(id, AddSession session)) ->
      session
      |> state.SocketServer.BuildSession id
      |> Either.map AddSession
      |> Either.iter (appendCmd state)

    // replicate a RemoveSession command if the session exists
    | Socket (SessionRemoved id) ->
      state.Store.State.Sessions
      |> Map.tryFind id
      |> Option.iter (RemoveSession >> appendCmd state)

    // log error and replicate RemoveSession if the session exists
    | Socket (OnError(id, exn)) ->
      exn
      |> String.format "Error on WebSocket connection: {0}"
      |> Logger.err (tag "replicateEvent")

      state.Store.State.Sessions
      |> Map.tryFind id
      |> Option.iter (RemoveSession >> appendCmd state)

    // Execute the AddMember request on the RaftServer. This will internally resolve
    | Socket (OnMessage(_, AddMember mem)) ->
      mem |> AddMember |> appendCmd state

    // replicate command via Raft
    | Socket (OnMessage(_, RemoveMember mem)) ->
      mem |> RemoveMember |> appendCmd state

    // replicate command via Raft
    | Socket (OnMessage(_, cmd)) -> appendCmd state cmd

    //     _                               _
    //    / \   _ __  _ __   ___ _ __   __| |
    //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |
    //  / ___ \| |_) | |_) |  __/ | | | (_| |
    // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|
    //         |_|   |_|

    | Append cmd -> appendCmd state cmd

    //   ___  _   _
    //  / _ \| |_| |__   ___ _ __
    // | | | | __| '_ \ / _ \ '__|
    // | |_| | |_| | | |  __/ |
    //  \___/ \__|_| |_|\___|_|

    | other -> ignore other

    state

  // ** dispatchEvent

  let private dispatchEvent (state: IrisState) (agent: IrisAgent) (ev: IrisEvent) =
    ev |> Msg.Notify |> agent.Post
    match ev.DispatchStrategy with
    | Publish   -> publishEvent   state agent ev
    | Replicate -> replicateEvent state       ev
    | Ignore    -> ignoreEvent    state       ev

  // ** loop

  let private loop (store: IAgentStore<IrisState>) (inbox: IrisAgent) =
    let rec act () =
      async {
        try
          let! msg = inbox.Receive()
          let state = store.State
          let newstate =
            match msg with
            | Msg.Start                  -> handleStart          state inbox
            | Msg.Stop               are -> handleStop           state inbox are
            | Msg.Notify              ev -> handleNotify         state       ev
            | Msg.SetConfig          cnf -> handleSetConfig      state       cnf
            | Msg.Event              ev  -> dispatchEvent        state inbox ev
            | Msg.ForceElection          -> handleForceElection  state
            | Msg.Periodic               -> handlePeriodic       state
            | Msg.RawClientResponse resp -> handleClientResponse state       resp
          store.Update newstate
        with
          | exn ->
            let format = "Message: {0}\nStackTrace: {1}"
            String.Format(format, exn.Message, exn.StackTrace)
            |> Logger.err (tag "loop")
        if Service.isStopping store.State.Status then
          return ()
        else
          return! act ()
    }
    act ()

  // ** IrisService

  [<RequireQualifiedAccess>]
  module IrisService =

    // *** isValidPassword

    let private isValidPassword (user: User) (password: Password) =
      let password = Crypto.hashPassword password user.Salt
      password = user.Password

    // *** makeListener

    let private makeListener (subscriptions: Subscriptions) =
      { new IObservable<IrisEvent> with
          member self.Subscribe(obs) =
            let guid = Guid.NewGuid()
            do subscriptions.TryAdd(guid, obs) |> ignore
            { new IDisposable with
                member self.Dispose () =
                  do subscriptions.TryRemove(guid) |> ignore } }

    // *** create

    let create (iris: IrisServiceOptions) =
      let subscriptions = new Subscriptions()
      let cts = new CancellationTokenSource()
      let store = AgentStore.create()
      let agent = new IrisAgent(loop store, cts.Token)

      // set up the error handler so we can address any problems properly
      agent.Error.Add (sprintf "error on agent loop: %O" >> Logger.err (tag "loop"))

      agent.Start()                     // start the agent

      { new IIrisService with
          member self.Start() =
            either {
              let! path = Project.checkPath iris.Machine iris.ProjectName
              let! (state: State) = Asset.loadWithMachine path iris.Machine

              let user =
                state.Users
                |> Map.tryPick (fun _ u -> if u.UserName = iris.UserName then Some u else None)

              match user with
              | Some user when isValidPassword user iris.Password ->
                let state =
                  match iris.SiteId with
                  | Some site ->
                    let site =
                      state.Project.Config.Sites
                      |> Array.tryFind (fun s -> s.Id = site)
                      |> function Some s -> s | None -> ClusterConfig.Default

                    // Add current machine if necessary
                    // taking the default ports from MachineConfig
                    let site =
                      let machineId = iris.Machine.MachineId
                      if Map.containsKey machineId site.Members
                      then site
                      else
                        let selfMember =
                          { Member.create(machineId) with
                              IpAddr  = iris.Machine.BindAddress
                              GitPort = iris.Machine.GitPort
                              WsPort  = iris.Machine.WsPort
                              ApiPort = iris.Machine.ApiPort
                              Port    = iris.Machine.RaftPort }
                        { site with Members = Map.add machineId selfMember site.Members }

                    let cfg = state.Project.Config |> Config.addSiteAndSetActive site
                    { state with Project = { state.Project with Config = cfg }}
                  | None -> state

                // This will fail if there's no ActiveSite set up in state.Project.Config
                // The frontend needs to handle that case
                let! mem = Config.selfMember state.Project.Config

                let context = new ZContext()

                let clockService = Clock.create context mem.IpAddr
                let! raftServer = RaftServer.create context state.Project.Config {
                    new IRaftSnapshotCallbacks with
                      member self.PrepareSnapshot () = Some store.State.Store.State
                      member self.RetrieveSnapshot () = retrieveSnapshot store.State
                  }
                let! socketServer = WebSocketServer.create mem
                let! apiServer = ApiServer.create context mem state.Project.Id
                let gitServer = GitServer.create mem state.Project.Path // IMPORTANT: use the
                                                                        // projects path here, not
                                                                        // the path to project.yml

                // set up event forwarding of various services to the actor
                let disposables =
                  [ (LOG_HANDLER,   forwardEvent IrisEvent.Log    agent |> Logger.subscribe)
                    (RAFT_SERVER,   forwardEvent IrisEvent.Raft   agent |> raftServer.Subscribe)
                    (WS_SERVER,     forwardEvent IrisEvent.Socket agent |> socketServer.Subscribe)
                    (API_SERVER,    forwardEvent IrisEvent.Api    agent |> apiServer.Subscribe)
                    (GIT_SERVER,    forwardEvent IrisEvent.Git    agent |> gitServer.Subscribe)
                    (CLOCK_SERVICE, forwardEvent IrisEvent.Clock  agent |> clockService.Subscribe) ]
                  |> Map.ofList

                // set up the agent state
                { Member         = mem
                  Machine        = iris.Machine
                  Leader         = None
                  Status         = ServiceStatus.Starting
                  Store          = new Store(state)
                  Context        = context
                  ApiServer      = apiServer
                  GitServer      = gitServer
                  RaftServer     = raftServer
                  SocketServer   = socketServer
                  ClockService   = clockService
                  Subscriptions  = subscriptions
                  Disposables    = disposables }
                |> store.Update          // and feed it to the store, before we start the services

                let result =
                  either {
                    do! raftServer.Start()
                    do! apiServer.Start()
                    do! socketServer.Start()
                    do! gitServer.Start()
                  }

                agent.Post Msg.Start    // this service is ready for action

                match result with
                | Right _ -> return ()
                | Left error ->
                  disposeAll disposables
                  dispose socketServer
                  dispose apiServer
                  dispose raftServer
                  dispose gitServer
                  return! Either.fail error
              | _ ->
                return!
                  "Login rejected"
                  |> Error.asProjectError (tag "loadProject")
                  |> Either.fail
            }

          member self.Project
            with get () = store.State.Store.State.Project // :D

          member self.Config
            with get () = store.State.Store.State.Project.Config // :D
            and set config = agent.Post (Msg.SetConfig config)

          member self.Status
            with get () = store.State.Status

          member self.ForceElection () = agent.Post(Msg.ForceElection)

          member self.Periodic () = agent.Post(Msg.Periodic)

          member self.AddMember mem =
            mem |> AddMember |> IrisEvent.Append |> Msg.Event |> agent.Post

          member self.RemoveMember id =
            store.State.RaftServer.Raft.Peers
            |> Map.tryFind id
            |> Option.iter (RemoveMember >> IrisEvent.Append >> Msg.Event >> agent.Post)

          member self.Append cmd =
            cmd |> IrisEvent.Append |> Msg.Event |> agent.Post

          member self.GitServer
            with get () = store.State.GitServer

          member self.RaftServer
            with get () = store.State.RaftServer

          member self.SocketServer
            with get () = store.State.SocketServer

          member self.Subscribe(callback: IrisEvent -> unit) =
            let listener = makeListener subscriptions
            { new IObserver<IrisEvent> with
                member self.OnCompleted() = ()
                member self.OnError(error) = ()
                member self.OnNext(value) = callback value }
            |> listener.Subscribe

          member self.Machine
            with get () = iris.Machine

          member self.Dispose() =
            Tracing.trace (tag "Dispose") <| fun () ->
              match store.State.Status with
              | ServiceStatus.Starting -> dispose agent
              | ServiceStatus.Running ->
                use are = new AutoResetEvent(false)
                are |> Msg.Stop |> agent.Post // signalling stop to the loop
                if not (are.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
                  "timeout: attempt to dispose iris service failed"
                  |> Logger.debug (tag "Dispose")
                cts.Cancel()                // cancel the actor
                dispose cts
                dispose agent
                dispose store.State         // dispose the state
                store.Update { store.State with Status = ServiceStatus.Disposed }
              | _ -> ()

          // member self.LeaveCluster () =
          //   Tracing.trace (tag "LeaveCluster") <| fun () ->
          //     match postCommand agent "LeaveCluster"  Msg.Leave with
          //     | Right Reply.Ok -> Right ()
          //     | Left error -> Left error
          //     | Right other ->
          //       sprintf "Unexpected response from IrisAgent: %A" other
          //       |> Error.asOther (tag "LeaveCluster")
          //       |> Either.fail

          // member self.JoinCluster ip port =
          //   Tracing.trace (tag "JoinCluster") <| fun () ->
          //     match postCommand agent "JoinCluster" (fun chan -> Msg.Join(chan,ip, port)) with
          //     | Right Reply.Ok -> Right ()
          //     | Left error  -> Left error
          //     | Right other ->
          //       sprintf "Unexpected response from IrisAgent: %A" other
          //       |> Error.asOther (tag "JoinCluster")
          //       |> Either.fail

        }

#endif
