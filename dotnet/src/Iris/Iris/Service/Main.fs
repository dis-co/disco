namespace Iris.Service

open System
open System.Security.Cryptography
open System.IO
open System.Net
open System.Linq
open System.Threading
open System.Diagnostics

open Iris.Core
open Iris.Core.Utils
open Iris.Service.Types
open Iris.Service.Core
open Iris.Service.CommandLine
open Iris.Service.Raft.Server
open FSharpx.Functional
open LibGit2Sharp
open Argu
open ZeroMQ
open Iris.Raft
open Iris.Service.Raft.Db
open Iris.Service.Raft.Utilities

[<AutoOpen>]
module Main =

  //  ____  _             _
  // / ___|| |_ __ _ _ __| |_
  // \___ \| __/ _` | '__| __|
  //  ___) | || (_| | |  | |_
  // |____/ \__\__,_|_|   \__|

  let startRaft (projectdir: FilePath) =
    let projFile = IO.Path.Combine(projectdir, PROJECT_FILENAME)

    if IO.File.Exists projFile |> not then
      printfn "Project file not found. Aborting."
      exitWith ExitCode.ProjectMissing

    match Project.Load(projFile) with
      | Right project ->
        use server = new IrisService(ref project)
        server.Start()

        printfn "Welcome to the Raft REPL. Type help to see all commands."
        consoleLoop server
      | Left error ->
        printfn "Could not load project. %A Aborting." error
        exitWith ExitCode.ProjectMissing

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  let createDataDir (parsed: ParseResults<CLIArguments>) =
    let baseDir = parsed.GetResult <@ Dir @>
    let name = parsed.GetResult <@ Name @>
    let dir = IO.Path.Combine(baseDir, name)
    let raftDir = IO.Path.Combine(IO.Path.GetFullPath(dir), RAFT_DIRECTORY)

    if IO.Directory.Exists dir then
      let empty = IO.Directory.EnumerateFileSystemEntries(dir).Count() = 0
      if  not empty then
        printf "%A not empty. I clean first? y/n" dir
        match Console.ReadLine() with
          | "y" -> rmDir dir
          | _   -> exitWith ExitCode.OK

    mkDir dir
    mkDir raftDir

    let me = new Signature("Operator", "operator@localhost", new DateTimeOffset(DateTime.Now))

    let node =
      getNodeId ()
      |> fun id ->
        { Node.create(id) with
            IpAddr = parsed.GetResult <@ Bind @> |> IpAddress.Parse
            GitPort = parsed.GetResult <@ Git @>
            WsPort = parsed.GetResult <@ Ws @>
            WebPort = parsed.GetResult <@ Web @>
            Port = parsed.GetResult <@ Raft @> }

    let project  =
      let def = Project.Create(name)
      let cfg =
        def.Config
        |> updateEngine
          { def.Config.RaftConfig with DataDir = raftDir }
        |> addNodeConfig node
      { def with
          Path = Some dir
          Config = cfg }

    match createDB raftDir with
      | Some db ->
        createRaft project.Config
        |> flip saveRaft db
        dispose db
        printfn "successfully created database"
      | _      -> printfn "unable to create database"

    match project.Save(me,"project created") with
      | Right(commit, project) ->
        project.Path
        |> Option.get
        |> printfn "project initialized in %A"
      | Left error ->
        failwithf "Unable to create project: %A" error

  //  ____                _
  // |  _ \ ___  ___  ___| |_
  // | |_) / _ \/ __|/ _ \ __|
  // |  _ <  __/\__ \  __/ |_
  // |_| \_\___||___/\___|\__|

  let resetDataDir (datadir: FilePath) =
    match Project.Load(datadir </> PROJECT_FILENAME) with
    | Right project ->
      let raftDir = IO.Path.Combine(datadir, RAFT_DIRECTORY)
      if IO.Directory.Exists raftDir then
        rmDir raftDir

      mkDir raftDir

      match createDB raftDir with
      | Some db ->
        createRaft project.Config
        |> flip saveRaft db
        dispose db
        printfn "successfully reset database"
      | _ -> printfn "unable to reset database"

    | Left error ->
      printfn "Project could not be loaded. %A" error

  //  ____
  // |  _ \ _   _ _ __ ___  _ __
  // | | | | | | | '_ ` _ \| '_ \
  // | |_| | |_| | | | | | | |_) |
  // |____/ \__,_|_| |_| |_| .__/
  //                       |_|

  let dumpDataDir (datadir: FilePath) =
    let raftDir = IO.Path.Combine(datadir, RAFT_DIRECTORY)
    match openDB raftDir with
      | Some db ->
        dumpDB db
        |> indent 4
        |> printfn "Database contents:\n%s"
      | _ ->
        printfn "No Database found at %A." raftDir

  ////////////////////////////////////////
  //  __  __       _                    //
  // |  \/  | __ _(_)_ __               //
  // | |\/| |/ _` | | '_ \              //
  // | |  | | (_| | | | | |             //
  // |_|  |_|\__,_|_|_| |_|             //
  ////////////////////////////////////////

  [<EntryPoint>]
  let main args =
    let parsed =
      try
        parser.ParseCommandLine args
      with
        | exn ->
          printfn "%s" <| parser.PrintUsage exn.Message
          exitWith ExitCode.CliParseError

    validateOptions parsed

    match parsed.GetResult <@ Cmd @> with
    | Create -> createDataDir parsed
    | Start ->
      parsed.TryGetResult <@ Dir @>
      |> Option.map startRaft
      |> ignore
    | Reset ->
      parsed.TryGetResult <@ Dir @>
      |> Option.map resetDataDir
      |> ignore
    | Dump ->
      parsed.TryGetResult <@ Dir @>
      |> Option.map dumpDataDir
      |> ignore

    exitWith ExitCode.OK
