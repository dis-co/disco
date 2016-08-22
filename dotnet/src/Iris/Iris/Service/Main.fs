namespace Iris.Service

open System
open System.Security.Cryptography
open System.Net
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
    { Guid             = rid
    ; Debug            = debug
    ; IpAddr           = "127.0.0.1"
    ; WebPort          = (portbase - 1000) + idx
    ; RaftPort         = portbase + idx
    ; Start            = start
    ; LeaderIp         = lip
    ; LeaderPort       = Option.map (fun n -> uint32 portbase + n) lpidx
    ; MaxRetries       = 5u
    ; DataDir          = Guid.Create() |> string |> mkTmpPath
    ; PeriodicInterval = 50UL
    }

  let createFollower debug (rid: string) (portidx: int) lid lpidx =
    createConfig debug rid portidx false (Some "127.0.0.1") (Some (uint32 lpidx))

  let createLeader debug (rid: string) (portidx: int) =
    createConfig debug rid portidx true None None

  ////////////////////////////////////////
  //  __  __       _                    //
  // |  \/  | __ _(_)_ __               //
  // | |\/| |/ _` | | '_ \              //
  // | |  | | (_| | | | | |             //
  // |_|  |_|\__,_|_|_| |_|             //
  ////////////////////////////////////////
  [<EntryPoint>]
  let main args =

    let debug = Array.contains "--debug" args

    let options =
      if Array.length args = 2 || Array.length args = 3 then
        createLeader debug args.[0] (int args.[1])
      else
        createFollower debug args.[0] (int args.[1]) args.[2] (int args.[3])

    use kontext = new ZContext()

    // 1. Initialise the application server from the supplied options
    // let options = parseOptions args
    use server = new RaftServer(options, kontext)
    server.Start()

    // 6. Start the console input loop.
    printfn "Welcome to the Raft REPL. Type help to see all commands."
    consoleLoop server

    0
