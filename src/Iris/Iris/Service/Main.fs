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
    // Tracing.enable()

    let parsed =
      try
        parser.ParseCommandLine args
      with
        | exn ->
          exn.Message
          |> Error.asOther "Main"
          |> Error.exitWith

    validateOptions parsed

    let getBindIp() =
        match parsed.TryGetResult <@ Bind @> with
        | Some bindIp -> bindIp
        | None ->
          failwith "Please specify a valid IP address to bind Iris services with --bind argument"

    // Init machine config
    parsed.TryGetResult <@ Machine @>
    |> Option.map (filepath >> Path.getFullPath)
    |> MachineConfig.init getBindIp (parsed.TryGetResult <@ Shift_Defaults @>)
    |> Error.orExit ignore

    let result =
      let machine = MachineConfig.get()
      let validation = MachineConfig.validate machine

      if not validation.IsEmpty then
        printfn "Machine configuration file is invalid, please check the following settings:"
        printfn ""
        for KeyValue(name, _) in validation do
          printfn "    %A must not be empty" name
        printfn ""
        exit 1

      let dir =
        parsed.TryGetResult <@ Project @>
        |> Option.map (fun projectName ->
          machine.WorkSpace </> filepath projectName)

      let frontend =
        parsed.TryGetResult <@ Frontend @>
        |> Option.map filepath

      match parsed.GetResult <@ Cmd @>, dir with
      | Create,            _ -> createProject parsed
      | Start,           dir -> startService dir frontend
      | Reset,      Some dir -> resetProject dir
      | Add_User,   Some dir -> addUser dir
      | Add_Member, Some dir -> addMember dir
      | Help,              _ -> help ()
      |  _ ->
        sprintf "Unexpected command line failure: %A" args
        |> Error.asParseError "Main"
        |> Either.fail

    result |> Error.orExit ignore

    Error.exitWith IrisError.OK
