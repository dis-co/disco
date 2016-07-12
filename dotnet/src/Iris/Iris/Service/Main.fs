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
open LibGit2Sharp

open FSharpx.Functional
open Argu
open fszmq
open fszmq.Context
open fszmq.Socket
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
    let zmqContext = new fszmq.Context()

    // 1. Initialise the application context from the supplied options
    let options = parseOptions args
    use context = new RaftServer(options, zmqContext)
    context.Start()

    // 6. Start the console input loop.
    printfn "Welcome to the Raft REPL. Type help to see all commands."
    consoleLoop context

    0
