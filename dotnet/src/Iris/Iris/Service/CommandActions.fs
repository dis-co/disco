module Iris.Service.CommandActions

open System
open System.IO
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

let getWsAddress (iris: IIrisServer): Either<IrisError,string> =
    match iris.Config with
    | Left _ -> "0"
    | Right cfg ->
        match Config.findMember cfg cfg.MachineId with
        | Right mem -> sprintf "ws://%O:%i" mem.IpAddr mem.WsPort
        | Left _ -> "0"
    |> Either.succeed

let listProjects (cfg: IrisMachine): Either<IrisError,string> =
  Directory.GetDirectories(cfg.WorkSpace)
  |> Array.map Path.GetFileName
  |> String.concat ","
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
          ApiPort = opts.apiPort
          Port    = opts.raftPort }

    let! project = buildProject machine opts.name dir raftDir mem

    do! initializeRaft project

    return "ok"
  }

let registeredServices = ConcurrentDictionary<string, IDisposable>()

let startAgent (cfg: IrisMachine) (iris: IIrisServer) =
  let fail cmd msg =
    IrisError.Other (tag cmd, msg) |> Either.fail
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
        | UnloadProject ->
          // TODO: Check if a project is actually loaded
          iris.UnloadProject()
          |> Either.map (fun () -> "Project unloaded")
        | ListProjects -> listProjects cfg
        | GetWebSocketAddress -> getWsAddress iris
        | CreateProject opts -> createProject cfg opts
        | LoadProject(projectName, userName, password) ->
          iris.LoadProject(projectName, userName, password)
          |> Either.map (fun _ -> "Loaded project " + projectName)
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
