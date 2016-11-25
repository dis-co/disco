namespace Iris.Service

open Iris.Core
//open Argu
//open Iris.Service.CommandLine

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
    try
      //Logger.subscribe (printfn "%A") |> ignore
      let service = new Iris.Service.MockService()
      service.Start()
      let _ = System.Console.ReadLine()
      //let parsed =
      //  try
      //    parser.ParseCommandLine args
      //  with
      //    | exn ->
      //      printfn "%s" <| parser.PrintUsage exn.Message
      //      Error.exitWith CliParseError
      //
      //validateOptions parsed
      //
      //let web = not (parsed.Contains <@ NoHttp @>)
      //let interactive = parsed.Contains <@ Interactive @>
      //
      //match parsed.GetResult <@ Cmd @>, parsed.GetResult <@ Dir @> with
      //| Create,  _ -> createProject parsed
      //| Start, dir -> startService web interactive dir
      //| Reset, dir -> resetProject dir
      //| Dump,  dir -> dumpDataDir dir
      //|> Error.orExit id
      //|> ignore

      Error.exitWith OK
    with
    | ex -> printfn "%s\n%s" ex.Message ex.StackTrace; 1

