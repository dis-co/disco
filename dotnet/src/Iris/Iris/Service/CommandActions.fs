module Iris.Service.CommandActions

#if !IRIS_NODES

open System
open Iris.Raft
open Iris.Core
open Iris.Core.Commands
open Iris.Core.FileSystem
open Iris.Service.Interfaces
open Iris.Service.Persistence
open System.Collections.Concurrent

type private Channel = AsyncReplyChannel<Either<IrisError,string>>

let private tag s = "Iris.Service.Commands." + s

let private serializeJson =
    let converter = Fable.JsonConverter()
    fun (o: obj) -> Newtonsoft.Json.JsonConvert.SerializeObject(o, converter)

// Command to test:
// curl -H "Content-Type: application/json" \
//      -XPOST \
//      -d '"GetServiceInfo"' \
//      http://localhost:7000/api/command

let getServiceInfo (iris: IIris): Either<IrisError,string> =
  let notLoaded () = null |> serializeJson |> Either.succeed
  match iris.IrisService with
  | Some service ->
    match Config.findMember service.Config iris.Machine.MachineId with
    | Right mem ->
      { webSocket = sprintf "ws://%O:%i" mem.IpAddr mem.WsPort
        version = Build.VERSION
        buildNumber = Build.BUILD_NUMBER }
      |> serializeJson
      |> Either.succeed
    | Left _ -> notLoaded()
  | None -> notLoaded()

// Command to test:
// curl -H "Content-Type: application/json" \
//      -XPOST \
//      -d '"ListProjects"' \
//      http://localhost:7000/api/comman

let listProjects (cfg: IrisMachine): Either<IrisError,string> =
  cfg.WorkSpace
  |> Directory.getDirectories
  |> Array.choose (fun dir ->
    match IrisProject.Load(dir, cfg) with
    | Right project -> { Name = project.Name; Id = project.Id } |> Some
    | Left error -> printfn "ERROR: %A" error; None)
  |> serializeJson
  |> Either.succeed

/// ## buildProject
///
/// Create a new IrisProject data structure with given parameters.
///
/// ### Signature:
/// - name: Name of the Project
/// - path: destination path of the Project
/// - raftDir: Raft data directory
/// - mem: self Member (built from Member Id env var)
///
/// Returns: IrisProject
let buildProject (machine: IrisMachine)
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

    let! _ = IrisData.saveWithCommit path User.Admin.Signature updated

    printfn "project: %A" project.Name
    printfn "created in: %O" project.Path

    return updated
  }

/// ## initializeRaft
///
/// Given the user (usually the admin user) and Project value, initialize the Raft intermediate
/// state in the data directory and commit the result to git.
///
/// ### Signature:
/// - user: User to commit as
/// - project: IrisProject to initialize
///
/// Returns: unit
let initializeRaft (project: IrisProject) = either {
    let! raft = createRaft project.Config
    let! _ = saveRaft project.Config raft
    return ()
  }

let createProject (machine: IrisMachine) (opts: CreateProjectOptions) = either {
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
          IpAddr  = IpAddress.Parse opts.ipAddr
          GitPort = port opts.gitPort
          WsPort  = port opts.wsPort
          ApiPort = port opts.apiPort
          Port    = port opts.port }

    let! project = buildProject machine opts.name dir raftDir mem

    do! initializeRaft project

    return "ok"
  }

let getProjectSites machine projectName =
  either {
    let! path = Project.checkPath machine projectName
    let! (state: State) = Asset.loadWithMachine path machine
    // TODO: Check username and password?
    return state.Project.Config.Sites |> Array.map (fun x -> x.Name) |> serializeJson
  }

// Command to test:
// curl -H "Content-Type: application/json" \
//      -XPOST \
//      -d '"MachineStatus"' \
//      http://localhost:7000/api/comman

let machineStatus (iris: IIris) =
  match iris.IrisService with
  | Some service -> Busy(service.Project.Id, service.Project.Name)
  | None -> MachineStatus.Idle
  |> serializeJson
  |> Either.succeed

// Command to test:
// curl -H "Content-Type: application/json" \
//      -XPOST \
//      -d '"MachineConfig"' \
//      http://localhost:7000/api/comman

let machineConfig () =
  MachineConfig.get()
  |> serializeJson
  |> Either.succeed

// Command to test:
// curl -H "Content-Type: application/json" \
//      -XPOST \
//      -d '{"CloneProject":["meh","git://192.168.2.106:6000/meh/.git"]}' \
//      http://localhost:7000/api/command

let cloneProject (name: Name) (uri: Url) =
  let machine = MachineConfig.get()
  let target = machine.WorkSpace </> filepath (unwrap name)
  let success = sprintf "Successfully cloned project from: %A" uri
  Git.Repo.clone target (unwrap uri)
  |> Either.map (konst (serializeJson success))

// Command to test:
// curl -H "Content-Type: application/json" \
//      -XPOST \
//      -d '{"PullProject":["dfb6eff5-e4b8-465d-9ad0-ee58bd508cad","meh","git://192.168.2.106:6000/meh/.git"]}' \
//      http://localhost:7000/api/command

let pullProject (id: ProjectId) (name: Name) (uri: Url) = either {
    let machine = MachineConfig.get()
    let target = machine.WorkSpace </> filepath (unwrap name)
    let! repo = Git.Repo.repository target

    let! _ =
      match Git.Config.tryFindRemote repo (string id) with
      | Some remote -> Git.Config.updateRemote repo remote uri
      | None -> Git.Config.addRemote repo (string id) uri

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

let registeredServices = ConcurrentDictionary<string, IDisposable>()

let startAgent (cfg: IrisMachine) (iris: IIris) =
  MailboxProcessor<Command*Channel>.Start(fun agent ->
    let rec loop() = async {
      let! input, replyChannel = agent.Receive()
      let res =
        match input with
        | Shutdown ->
          let msg = "Disposing service..."
          // TODO: Grab a reference of the http server to dispose it too?
          Async.Start <| async {
            do! Async.Sleep 1000
            printfn "%s" msg
            dispose iris
            exit 0
          }
          Right msg
        | Command.UnloadProject -> iris.UnloadProject() |> Either.map (konst "Project unloaded")
        | ListProjects -> listProjects cfg
        | GetServiceInfo -> getServiceInfo iris
        | MachineStatus -> machineStatus iris
        | MachineConfig -> machineConfig ()
        | CreateProject opts -> createProject cfg opts
        | SaveProject ->
          iris.SaveProject()
          |> Either.map (fun _ -> "Successfully saved project")
        | CloneProject (name, gitUri) -> cloneProject name gitUri
        | PullProject (id, name, gitUri) -> pullProject id name gitUri
        | LoadProject(projectName, username, password, site) ->
          iris.LoadProject(projectName, username, password, site)
          |> Either.map (fun _ -> "Loaded project " + unwrap projectName)
        | GetProjectSites(projectName, _, _) -> getProjectSites cfg projectName

      replyChannel.Reply res
      do! loop()
    }
    loop()
  )

let postCommand (agent: (MailboxProcessor<Command*Channel> option) ref) (cmd: Command) =
  let err msg =
    Error.asOther (tag "postCommand") msg |> Either.fail
  match !agent with
  | Some agent ->
    async {
      let! res = agent.PostAndTryAsyncReply((fun ch -> cmd, ch), Constants.COMMAND_TIMEOUT)
      match res with
      | Some res -> return res
      | None -> return err "Request has timeout"
    }
  | None -> err "Command agent hasn't been initialized yet" |> async.Return

#endif
