namespace Iris.Service


open Argu
open Iris.Core
open System
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
    let result =
      let machine = MachineConfig.get()
      let dir =
        parsed.TryGetResult <@ Project @>
        |> Option.map (fun projectName ->
          machine.WorkSpace </> projectName)

      match parsed.GetResult <@ Cmd @>, dir with
      | Create,            _ -> createProject parsed
      | Start,           dir -> startService dir
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
