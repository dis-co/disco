module Iris.Service.CommandActions

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Core.Commands
open Iris.Core.FileSystem
open Iris.Service.Interfaces
open Iris.Service.Persistence

type private Channel = AsyncReplyChannel<Either<IrisError,string>>

let private tag s = "Iris.Service.Commands." + s

let getWsport (iris: IIrisServer) =
    either {
      let! cfg = iris.Config
      match Map.tryFind cfg.MachineId cfg.Cluster.Members with
      | Some mem -> return mem.WsPort |> string
      | None -> return "0"
    }

let listProjects (cfg: IrisMachine) =
  Directory.GetDirectories(cfg.WorkSpace)
  |> Array.map Path.GetFileName
  |> String.concat ","
  |> Either.succeed

let loadProject (iris: IIrisServer) name =
  let cfg = MachineConfig.get()
  let projFile = cfg.WorkSpace </> name </> PROJECT_FILENAME + ASSET_EXTENSION
  if File.Exists projFile |> not then
    sprintf "Project Not Found: %s" name
    |> Error.asOther "startService"
    |> Either.fail
  else
    iris.Load projFile
    |> Either.map (fun () -> sprintf "Loaded project %s" name)

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
    let! project = Project.create path name machine

    let updated =
      project
      |> Project.updateDataDir raftDir
      |> Project.addMember mem

    let! _ = Asset.saveWithCommit path User.Admin.Signature updated

    printfn "project: %A" project.Name
    printfn "created in: %s" project.Path

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
    let dir = machine.WorkSpace </> opts.name
    let raftDir = dir </> RAFT_DIRECTORY

    // TODO: Throw error instead?
    do!
      if Directory.Exists dir
      then rmDir dir
      else Either.nothing

    do! mkDir dir
    do! mkDir raftDir

    let mem =
      { Member.create(machine.MachineId) with
          IpAddr  = IpAddress.Parse opts.ipAddress
          GitPort = opts.gitPort
          WsPort  = opts.webSocketPort
          Port    = opts.raftPort }

    let! project = buildProject machine opts.name dir raftDir mem

    do! initializeRaft project

    return "ok"
  }

let startAgent (cfg: IrisMachine) (iris: IIrisServer) = MailboxProcessor<Command*Channel>.Start(fun agent ->
    let rec loop() = async {
      let! input, replyChannel = agent.Receive()
      match input with
      | ListProjects -> listProjects cfg
      | GetWebSocketPort -> getWsport iris
      | LoadProject name -> loadProject iris name
      | CreateProject opts -> createProject cfg opts
      |> replyChannel.Reply
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
