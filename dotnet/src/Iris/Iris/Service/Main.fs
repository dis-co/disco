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
        | exn -> Error.exitWith CliParseError

    validateOptions parsed

    let web = not (parsed.Contains <@ NoHttp @>)
    let interactive = parsed.Contains <@ Interactive @>

    let res =
      match parsed.GetResult <@ Cmd @>, parsed.TryGetResult <@ Dir @> with
      | Create,       _ -> createProject parsed
      | Start, Some dir -> startService web interactive dir
      | Reset, Some dir -> resetProject dir
      | Dump,  Some dir -> dumpDataDir dir
      | User,  Some dir -> addUser dir
      | Setup, Some dir -> setup (Some dir)
      | Setup,        _ -> setup None
      | Help,         _ -> help ()
      |  _ ->
        sprintf "Unexpected command line failure: %A" args
        |> ParseError
        |> Either.fail

    res
    |> Error.orExit id
    |> ignore

    Error.exitWith OK
