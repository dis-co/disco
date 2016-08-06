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
open Pallet.Core

[<AutoOpen>]
module Main =

  ////////////////////////////////////////
  //  __  __       _                    //
  // |  \/  | __ _(_)_ __               //
  // | |\/| |/ _` | | '_ \              //
  // | |  | | (_| | | | | |             //
  // |_|  |_|\__,_|_|_| |_|             //
  ////////////////////////////////////////
  [<EntryPoint>]
  let main args =
    use kontext = new ZContext()

    // 1. Initialise the application server from the supplied options
    let options = parseOptions args
    use server = new RaftServer(options, kontext)
    server.Start()

    // 6. Start the console input loop.
    printfn "Welcome to the Raft REPL. Type help to see all commands."
    consoleLoop server

    0
