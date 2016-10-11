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

  let startRaft (projectdir: FilePath) : unit =
    let projFile = projectdir </> PROJECT_FILENAME

    if File.Exists projFile |> not then
      ProjectNotFound projectdir |> Error.exitWith

    match loadProject projFile with
      | Right project ->
        use server = new IrisService(ref project)
        server.Start()

        printfn "Welcome to the Raft REPL. Type help to see all commands."
        consoleLoop server

      | Left error ->
        ProjectNotFound projectdir |> Error.exitWith

  let buildNode (parsed: ParseResults<CLIArguments>) (id: Id) =
    { Node.create(id) with
        IpAddr  = parsed.GetResult <@ Bind @> |> IpAddress.Parse
        GitPort = parsed.GetResult <@ Git  @>
        WsPort  = parsed.GetResult <@ Ws   @>
        WebPort = parsed.GetResult <@ Web  @>
        Port    = parsed.GetResult <@ Raft @> }

  let buildProject (name: string) (path: FilePath) (raftDir: FilePath) (node: RaftNode) =
    createProject name
    |> updatePath path
    |> updateDataDir raftDir
    |> addMember node

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  let createDataDir (parsed: ParseResults<CLIArguments>) =
    let me = User.Admin.Signature
    let baseDir = parsed.GetResult <@ Dir @>
    let name = parsed.GetResult <@ Name @>
    let dir = baseDir </> name
    let raftDir = Path.GetFullPath(dir) </> RAFT_DIRECTORY

    if Directory.Exists dir then
      let empty = Directory.EnumerateFileSystemEntries(dir).Count() = 0
      if  not empty then
        printf "%A not empty. I clean first? y/n" dir
        match Console.ReadLine() with
          | "y" -> rmDir dir
          | _   -> Error.exitWith OK

    mkDir dir
    mkDir raftDir

    getNodeId ()
    |> Either.map (buildNode parsed)
    |> Either.map (buildProject dir name raftDir)
    |> Either.map
        (fun project ->
            match createDB raftDir, createRaft project.Config with
            | Right db, Right raft ->
              try
                saveRaft raft db
                dispose db
                printfn "successfully created database"
              with
                | exn ->
                  DatabaseCreateError exn.Message
                  |> Error.exitWith
            | Left error, _ -> Error.exitWith error
            | _, Left error -> Error.exitWith error

            match saveProject me "project created" project with
            | Right(commit, project) ->
              project.Path
              |> Option.get
              |> printfn "project initialized in %A"
            | Left error ->
              Error.exitWith error)

  //  ____                _
  // |  _ \ ___  ___  ___| |_
  // | |_) / _ \/ __|/ _ \ __|
  // |  _ <  __/\__ \  __/ |_
  // |_| \_\___||___/\___|\__|

  let resetDataDir (datadir: FilePath) =

    let reset project =
      let raftDir = datadir </> RAFT_DIRECTORY
      if Directory.Exists raftDir then
        rmDir raftDir

      mkDir raftDir

      match createDB raftDir, createRaft project.Config with
      | Right db, Right raft ->
        try
          saveRaft raft db
          dispose db
          printfn "successfully reset database"
        with
          | exn ->
            DatabaseCreateError exn.Message
            |> Error.exitWith
      | Left error, _ -> Error.exitWith error
      | _, Left error -> Error.exitWith error

    datadir </> PROJECT_FILENAME
    |> loadProject
    |> Either.orExit reset

  //  ____
  // |  _ \ _   _ _ __ ___  _ __
  // | | | | | | | '_ ` _ \| '_ \
  // | |_| | |_| | | | | | | |_) |
  // |____/ \__,_|_| |_| |_| .__/
  //                       |_|

  let dumpDataDir (datadir: FilePath) =
    let raftDir = datadir </> RAFT_DIRECTORY
    match openDB raftDir with
    | Right db ->
      dumpDB db
      |> indent 4
      |> printfn "Database contents:\n%s"
    | Left error ->
      Error.exitWith error

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
          Error.exitWith CliParseError

    validateOptions parsed

    match parsed.GetResult <@ Cmd @>, parsed.GetResult <@ Dir @> with
    | Create, _   -> createDataDir parsed |> ignore
    | Start,  dir -> startRaft     dir    |> ignore
    | Reset,  dir -> resetDataDir  dir    |> ignore
    | Dump,   dir -> dumpDataDir   dir    |> ignore

    Error.exitWith OK
