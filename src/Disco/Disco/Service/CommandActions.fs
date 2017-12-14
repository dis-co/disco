module Disco.Service.CommandActions

#if !DISCO_NODES

open System
open Disco.Raft
open Disco.Core
open Disco.Core.Commands
open Disco.Core.FileSystem
open Disco.Service.Interfaces
open Disco.Service.Persistence
open LibGit2Sharp
open System.Collections.Concurrent

// * Channel

type private Channel = AsyncReplyChannel<Either<DiscoError,string>>

// * tag

let private tag s = "CommandActions." + s

// * serializeJson

/// Serialize a value to json using Fable.JsonConverter
let private serializeJson =
    let converter = Fable.JsonConverter()
    fun (o: obj) -> Newtonsoft.Json.JsonConvert.SerializeObject(o, converter)

// * getServiceInfo

/// Retrieve the build number, version and current WebSocket port from the currently
/// running service.
///
/// Command to test:
/// curl -H "Content-Type: application/json" \
///      -XPOST \
///      -d '"GetServiceInfo"' \
///      http://localhost:7000/api/command
let getServiceInfo (disco: IDisco): Either<DiscoError,string> =
  let notLoaded () = null |> serializeJson |> Either.succeed
  match disco.DiscoService with
  | Some service ->
    match Config.findMember service.Config disco.Machine.MachineId with
    | Right mem ->
      { webSocket = sprintf "ws://%O:%i" mem.IpAddress mem.WsPort
        version = Build.VERSION
        buildNumber = Build.BUILD_NUMBER }
      |> serializeJson
      |> Either.succeed
    | Left _ -> notLoaded()
  | None -> notLoaded()

// * listProjects

/// Enumerate all projects in the current WorkSpace.
///
/// Command to test:
/// curl -H "Content-Type: application/json" \
///      -XPOST \
///      -d '"ListProjects"' \
///      http://localhost:7000/api/command
///
let listProjects (cfg: DiscoMachine): Either<DiscoError,string> =
  cfg.WorkSpace
  |> Directory.getDirectories
  |> Array.choose (fun dir ->
    match DiscoProject.Load(dir, cfg) with
    | Right project ->
      project.Name
      |> String.format "Found valid project \"{0}\" in current WorkSpace."
      |> Logger.info (tag "listProjects")
      Some { Name = project.Name; Id = project.Id }
    | Left _ ->
      dir
      |> String.format "\"{0}\" does not contain a valid project.yaml"
      |> Logger.info (tag "listProjects")
      None)
  |> serializeJson
  |> Either.succeed

// * buildProject

/// Create a new DiscoProject data structure with given parameters.
let buildProject (machine: DiscoMachine)
                  (name: string)
                  (path: FilePath)
                  (raftDir: FilePath)
                  (mem: RaftMember) =
  either {
    let! project = Project.create (Project.ofFilePath path) name machine

    let site =
        let def = ClusterConfig.Default
        { def with Members = Map.add mem.Id mem def.Members }

    let updated =
      project
      |> Project.updateDataDir raftDir
      |> fun p -> Project.updateConfig (Config.addSiteAndSetActive site p.Config) p

    let! _ = DiscoData.saveWithCommit path User.Admin.Signature updated

    project.Path
    |> sprintf "Created %A in %A" project.Name
    |> Logger.info (tag "buildProject")

    return updated
  }

// * initializeRaft

/// Given the user (usually the admin user) and Project value, initialize the Raft intermediate
/// state in the data directory and commit the result to git.
let initializeRaft (project: DiscoProject) = either {
    let! raft = createRaft project.Config
    let! _ = saveRaft project.Config raft
    return ()
  }

// * createProject

let createProject (machine: DiscoMachine) (opts: CreateProjectOptions) = either {
    let dir = machine.WorkSpace </> filepath opts.name
    let raftDir = dir </> filepath RAFT_DIRECTORY

    // TODO: Throw error instead?
    do!
      if Directory.exists dir
      then rmDir dir
      else Either.nothing

    do! mkDir dir
    do! mkDir raftDir

    let mem =
      { Member.create(machine.MachineId) with
          IpAddress = IpAddress.Parse opts.ipAddr
          GitPort   = port opts.gitPort
          WsPort    = port opts.wsPort
          ApiPort   = port opts.apiPort
          RaftPort  = port opts.port }

    let! project = buildProject machine opts.name dir raftDir mem
    do! initializeRaft project

    return "ok"
  }

// * getProjectSites

let getProjectSites machine projectName =
  either {
    let! path = Project.checkPath machine projectName
    let! (state: State) = Asset.loadWithMachine path machine
    // TODO: Check username and password?
    return
      state.Project.Config.Sites
      |> Array.map (fun x -> { Name = x.Name; Id = x.Id })
      |> serializeJson
  }

// * machineStatus

/// Gets the current machine's status, i.e. whether its Idle or Busy with a loaded project. This
/// is necessary in order to determine whether this machine can be added to a cluster or not.
///
/// Command to test:
/// curl -H "Content-Type: application/json" \
///      -XPOST \
///      -d '"MachineStatus"' \
///      http://localhost:7000/api/comman
let machineStatus (disco: IDisco) =
  match disco.DiscoService with
  | Some service -> Busy(service.Project.Id, service.Project.Name)
  | None -> MachineStatus.Idle
  |> serializeJson
  |> Either.succeed

// * machineConfig

/// Retrieve the machine configuration. This is used when new projects are constructed.
///
/// Command to test:
/// curl -H "Content-Type: application/json" \
///      -XPOST \
///      -d '"MachineConfig"' \
///      http://localhost:7000/api/comman
let machineConfig () =
  MachineConfig.get()
  |> serializeJson
  |> Either.succeed

// * cloneProject

/// Git clone a project from another server in order to start a service. This is used during
/// the process of adding a new machine to a cluster.
///
/// Command to test:
/// curl -H "Content-Type: application/json" \
///      -XPOST \
///      -d '{"CloneProject":["meh","git://192.168.2.106:6000/meh/.git"]}' \
///      http://localhost:7000/api/command
let cloneProject (name: Name) (uri: Url) =
  let machine = MachineConfig.get()
  let target = machine.WorkSpace </> filepath (unwrap name)
  let success = sprintf "Successfully cloned project from: %A" uri
  Git.Repo.clone target (unwrap uri)
  |> Either.map (konst (serializeJson success))

// * pullProject

/// Git pull an already existing project from a remote server. This is used in the process
/// of adding a machine to a cluster that already has the project in question locally.
///
/// Command to test:
/// curl -H "Content-Type: application/json" \
///      -XPOST \
///      -d '{"PullProject":["dfb6eff5-e4b8-465d-9ad0-ee58bd508cad","meh","git://192.168.2.106:6000/meh/.git"]}' \
///      http://localhost:7000/api/command
let pullProject (id: string) (name: Name) (uri: Url) = either {
    let machine = MachineConfig.get()
    let target = machine.WorkSpace </> filepath (unwrap name)
    use! repo = Git.Repo.repository target
    let branch = Git.Branch.current repo

    let! remote =
      match Git.Config.tryFindRemote repo id with
      | Some remote -> Git.Config.updateRemote repo remote uri
      | None -> Git.Config.addRemote repo id uri

    do! Git.Branch.setTracked repo branch remote
    do! Git.Repo.reset ResetMode.Hard repo

    let! result = Git.Repo.pull repo User.Admin.Signature

    match result.Status with
    | LibGit2Sharp.MergeStatus.Conflicts ->
      return!
        "Clonflict while pulling from " + unwrap uri
        |> Error.asGitError "pullProject"
        |> Either.fail
    | _ ->
      return
        sprintf "Successfully pulled changes from: %A" url
        |> serializeJson
  }

// * registeredServices

let registeredServices = ConcurrentDictionary<string, IDisposable>()

// * startAgent

let startAgent (cfg: DiscoMachine) (disco: IDisco) =
  MailboxProcessor<Command*Channel>.Start(fun agent ->
    let rec loop() = async {
      let! input, replyChannel = agent.Receive()
      let res =
        match input with
        | Shutdown ->
          // TODO: Grab a reference of the http server to dispose it too?
          Async.Start <| async {
            do! Async.Sleep 1000
            dispose disco
            exit 0
          }
          Right "Disposing service..."
        | Command.UnloadProject -> disco.UnloadProject() |> Either.map (konst "Project unloaded")
        | ListProjects -> listProjects cfg
        | GetServiceInfo -> getServiceInfo disco
        | MachineStatus -> machineStatus disco
        | MachineConfig -> machineConfig ()
        | CreateProject opts -> createProject cfg opts
        | SaveProject ->
          disco.SaveProject()
          |> Either.map (fun _ -> "Successfully saved project")
        | CloneProject (name, gitUri) -> cloneProject name gitUri
        | PullProject (id, name, gitUri) -> pullProject id name gitUri
        | LoadProject(projectName, Some { Id = siteId; Name = name }) ->
          disco.LoadProject(
            projectName,
            Measure.name Constants.ADMIN_USER_NAME,
            Measure.password Constants.ADMIN_DEFAULT_PASSWORD,
            Some (name, siteId))
          |> Either.map (fun _ -> "Loaded project " + unwrap projectName)
        | LoadProject(projectName, _) ->
          disco.LoadProject(
            projectName,
            Measure.name Constants.ADMIN_USER_NAME,
            Measure.password Constants.ADMIN_DEFAULT_PASSWORD,
            None)
          |> Either.map (fun _ -> "Loaded project " + unwrap projectName)
        | GetProjectSites projectName -> getProjectSites cfg projectName

      replyChannel.Reply res
      do! loop()
    }
    loop()
  )

// * postCommand

let postCommand (agent: (MailboxProcessor<Command*Channel> option) ref) (cmd: Command) =
  let err msg =
    Error.asOther (tag "postCommand") msg |> Either.fail
  match !agent with
  | Some agent ->
    async {
      let! res = agent.PostAndTryAsyncReply((fun ch -> cmd, ch), Constants.COMMAND_TIMEOUT)
      match res with
      | Some res -> return res
      | None -> return err "Request has timed out"
    }
  | None -> err "Command agent hasn't been initialized yet" |> async.Return

#endif
