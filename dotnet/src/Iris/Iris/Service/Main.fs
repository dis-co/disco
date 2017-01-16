namespace Iris.Service

open Argu
open Iris.Core
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

    let result =
      let machine = MachineConfig.get()
      let dir =
        parsed.TryGetResult <@ Project @>
        |> Option.map (fun projectName ->
          machine.WorkSpace </> projectName)

      let interactive = parsed.Contains <@ Interactive @>

      match parsed.GetResult <@ Cmd @>, dir with
      | Create,            _ -> createProject parsed
      | Start,           dir -> startService interactive dir
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

    Error.exitWith OK
