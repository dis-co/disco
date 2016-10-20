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
          printfn "%s" <| parser.PrintUsage exn.Message
          Error.exitWith CliParseError

    validateOptions parsed

    match parsed.GetResult <@ Cmd @>, parsed.GetResult <@ Dir @> with
    | Create, _   -> createProject parsed |> ignore
    | Start,  dir -> startService  dir    |> ignore
    | Reset,  dir -> resetProject  dir    |> ignore
    | Dump,   dir -> dumpDataDir   dir    |> ignore

    Error.exitWith OK
