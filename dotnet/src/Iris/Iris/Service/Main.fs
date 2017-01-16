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
    let config = MachineConfig.create ()
    match DiscoveryService.create (config) with
    | Right srvc ->
      srvc.Start()
      |> printfn "result: %A"

      let run = ref true

      while !run do
        let line = System.Console.ReadLine()
        if line = "quit" then
          run := false
        else
          match srvc.Services with
          | Right (reg, res) ->
            printfn "registered services:"
            Map.iter (fun _ s -> printfn "%s" (s.ToString())) reg
            printfn "resolved services:"
            Map.iter (fun _ s -> printfn "%A" s) res
          | other -> printfn "other: %A" other

      printfn "disposing"
      dispose srvc
    | Left error ->
      printfn "ERROR: %A" error

    // let parsed =
    //   try
    //     parser.ParseCommandLine args
    //   with
    //     | exn ->
    //       exn.Message
    //       |> Error.asOther "Main"
    //       |> Error.exitWith

    // validateOptions parsed

    // let interactive = parsed.Contains <@ Interactive @>
    // let web =
    //   match parsed.TryGetResult <@ Http @> with
    //   | Some basePath -> System.IO.Path.GetFullPath basePath
    //   | None -> Http.getDefaultBasePath()

    // let res =
    //   match parsed.GetResult <@ Cmd @>, parsed.TryGetResult <@ Dir @> with
    //   | Create,            _ -> createProject parsed
    //   | Start,           dir -> startService web interactive dir
    //   | Reset,      Some dir -> resetProject dir
    //   | Dump,       Some dir -> dumpDataDir dir
    //   | Add_User,   Some dir -> addUser dir
    //   | Add_Member, Some dir -> addMember dir
    //   | Setup,      Some dir -> setup (Some dir)
    //   | Setup,             _ -> setup None
    //   | Help,              _ -> help ()
    //   |  _ ->
    //     sprintf "Unexpected command line failure: %A" args
    //     |> Error.asParseError "Main"
    //     |> Either.fail

    // res
    // |> Error.orExit id
    // |> ignore

    Error.exitWith OK
