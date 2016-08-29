namespace Iris.Service

open System
open System.Security.Cryptography
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

[<AutoOpen>]
module Main =
  let mkTmpPath snip =
    let basePath =
      match Environment.GetEnvironmentVariable("IN_NIX_SHELL") with
        | "1" -> "/tmp/"
        | _   -> System.IO.Path.GetTempPath()
    basePath </> snip

  let startRaft (datadir: FilePath) =
    use kontext = new ZContext()

    let options = failwith "need to create options"

    // 1. Initialise the application server from the supplied options
    // let options = parseOptions args
    use server = new RaftServer(options, kontext)
    server.Start()

    // 6. Start the console input loop.
    printfn "Welcome to the Raft REPL. Type help to see all commands."
    consoleLoop server

  let createDataDir (parsed: ParseResults<CLIArguments>) =
    let baseDir = parsed.GetResult <@ Project_Dir @>
    let name = parsed.GetResult <@ Project_Name @>
    let dir = IO.Path.Combine(baseDir, name)
    let raftDir = IO.Path.Combine(IO.Path.GetFullPath(dir), ".raft")

    if IO.Directory.Exists dir then
      let empty = IO.Directory.EnumerateFileSystemEntries(dir).Count() = 0
      if  not empty then
        printf "%A not empty. I clean first? y/n" dir
        match Console.ReadLine() with
          | "y" -> rmDir dir
          | _   -> exit 1

    mkDir dir
    mkDir raftDir

    match createDB raftDir with
      | Some _ -> printfn "successfully created database"
      | _      -> printfn "unable to create database"

    let me = new Signature("Operator", "operator@localhost", new DateTimeOffset(DateTime.Now))

    let project  =
      let def = Project.Create(name)
      let cfg =
        def.Config
        |> updateEngine
          { def.Config.RaftConfig with
              DataDir     = raftDir
              BindAddress = parsed.GetResult <@ Bind_Address @> }
        |> updatePorts
          { def.Config.PortConfig with
              WebSocket = parsed.GetResult <@ Ws_Port @>
              Http = parsed.GetResult <@ Web_Port @>
              Raft = parsed.GetResult <@ Raft_Port @> }
      { def with
          Path = Some dir
          Config = cfg }

    match project.Save(me,"project created") with
      | Some(commit, project) ->
        printfn "project initialized in %A" project.Path
      | _ ->
        failwith "unable to create project"


  let resetDataDir (datadir: FilePath) =
    if IO.Directory.Exists datadir then
      rmDir datadir

  let dumpDataDir (datadir: FilePath) =
    failwith "dump data dir contents"

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
        | _ ->
          printfn "%s" <| parser.Usage("Unable to parse command line. Usage: ")
          exit 2

    validateOptions parsed

    if parsed.Contains <@ Create @> then
      createDataDir parsed
    else
      let dir = parsed.GetResult <@ Project_Dir @>
      if parsed.Contains <@ Start @> then
        startRaft dir
      elif parsed.Contains <@ Reset @> then
        resetDataDir dir
      elif parsed.Contains <@ Dump @> then
        dumpDataDir dir

    0
