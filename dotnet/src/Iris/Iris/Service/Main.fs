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

[<AutoOpen>]
module Main =
  let mkTmpPath snip =
    let basePath =
      match Environment.GetEnvironmentVariable("IN_NIX_SHELL") with
        | "1" -> "/tmp/"
        | _   -> System.IO.Path.GetTempPath()
    basePath </> snip

  let createConfig debug rid idx start lip lpidx  =
    let portbase = 8000
    // { Id             = rid
    // ; Debug            = debug
    // ; IpAddr           = "127.0.0.1"
    // ; WebPort          = (portbase - 1000) + idx
    // ; RaftPort         = portbase + idx
    // ; Start            = start
    // ; LeaderIp         = lip
    // ; LeaderPort       = Option.map (fun n -> uint32 portbase + n) lpidx
    // ; MaxRetries       = 5u
    // ; DataDir          = Id.Create() |> string |> mkTmpPath
    // ; PeriodicInterval = 50UL
    // }

    failwith "config config config"

  let createFollower debug (rid: string) (portidx: int) lid lpidx =
    createConfig debug rid portidx false (Some "127.0.0.1") (Some (uint32 lpidx))

  let createLeader debug (rid: string) (portidx: int) =
    createConfig debug rid portidx true None None

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
    let dir = parsed.GetResult <@ Data_Dir @>
    let empty = IO.Directory.EnumerateFileSystemEntries(dir).Count() = 0

    if IO.Directory.Exists dir && not empty then
      printf "%A not empty. Can I clean first? y/n" dir
      match Console.ReadLine() with
        | "y" -> delete dir
        | _   -> ()

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

    let parsed = parser.ParseCommandLine args

    validateOptions parsed

    if parsed.Contains <@ Create @> then
      createDataDir parsed
    else
      let dir = parsed.GetResult <@ Data_Dir @>
      if parsed.Contains <@ Start @> then
        startRaft dir
      elif parsed.Contains <@ Reset @> then
        resetDataDir dir
      elif parsed.Contains <@ Dump @> then
        dumpDataDir dir

    0
