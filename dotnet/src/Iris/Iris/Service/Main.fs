namespace Iris.Service

open Argu
open Iris.Core
open System
open System.Threading
open Iris.Raft
open Iris.Client
open Iris.Service
open Iris.Service.Interfaces
open Iris.Service.CommandLine

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
    let parsed =
      try
        parser.ParseCommandLine args
      with
        | exn ->
          exn.Message
          |> Error.asOther "Main"
          |> Error.exitWith

    validateOptions parsed

    // Init machine config
    parsed.TryGetResult <@ Machine @>
    |> Option.map System.IO.Path.GetFullPath
    |> MachineConfig.init
    |> Error.orExit ignore

    Thread.CurrentThread.GetApartmentState()
    |> printfn "Using Threading Model: %A"

    let threadCount = System.Environment.ProcessorCount * 2
    ThreadPool.SetMinThreads(threadCount,threadCount)
    |> fun result ->
      printfn "Setting Min. Threads in ThreadPool To %d %s"
        threadCount
        (if result then "Successful" else "Unsuccessful")

    let result =
      let machine = MachineConfig.get()

      let dir =
        parsed.TryGetResult <@ Project @>
        |> Option.map (fun projectName ->
          machine.WorkSpace </> projectName)

      let frontend =
        parsed.TryGetResult <@ Frontend @>

      Logger.initialize machine.MachineId

      match parsed.GetResult <@ Cmd @>, dir with
      | Create,            _ -> createProject parsed
      | Start,           dir -> startService dir frontend
      | Reset,      Some dir -> resetProject dir
      | Dump,       Some dir -> dumpDataDir dir
      | Add_User,   Some dir -> addUser dir
      | Add_Member, Some dir -> addMember dir
      | Help,              _ -> help ()
      |  _ ->
        sprintf "Unexpected command line failure: %A" args
        |> Error.asParseError "Main"
        |> Either.fail

    result |> Error.orExit ignore

    Error.exitWith IrisError.OK
