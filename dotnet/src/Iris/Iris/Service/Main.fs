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

    let interactive = parsed.Contains <@ Interactive @>
    let web =
      match parsed.TryGetResult <@ Http @> with
      | Some basePath ->
        match bool.TryParse basePath with
        | true, false -> None
        | true, true -> Http.getDefaultBasePath() |> Some
        | false, _ -> System.IO.Path.GetFullPath basePath |> Some
      | None -> Http.getDefaultBasePath() |> Some

    #if FRONTEND_DEV
    printfn "Starting service for Frontend development..."
    Option.iter (printfn "HttpServer will serve from %s") web
    #endif

    let res =
      match parsed.GetResult <@ Cmd @>, parsed.GetResult <@ Dir @> with
      | Create,  _ -> createProject parsed
      | Start, dir -> startService web interactive dir
      | Reset, dir -> resetProject dir
      | Dump,  dir -> dumpDataDir dir
    res
    |> Error.orExit id
    |> ignore

//    System.Console.ReadLine() |> ignore
    Error.exitWith OK
