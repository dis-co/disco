namespace Iris.Service


open Argu
open Iris.Core
open System
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
      either {
        use log = Logger.subscribe Logger.stdout
        let machine = MachineConfig.create ()
        let config = Config.create "hello" machine
        use! srvr = ApiServer.create config
        use obs1 = srvr.Subscribe(printfn "srvr: %A")
        do! srvr.Start()

        let server : IrisServer =
          { Id = Id.Create()
            Name = "cool"
            Port = 9000us
            IpAddress = IPv4Address "127.0.0.1" }

        let client : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            IpAddress = IPv4Address "127.0.0.1"
            Port = 9001us }

        use! clnt = ApiClient.create server client
        use obs2 = clnt.Subscribe(printfn "clnt: %A")

        do! clnt.Start()

        while true do
          Console.ReadLine()
          |> ignore
          let! clients = srvr.Clients
          printfn "%A" clients
      }

    printfn "%A" result

    // let parsed =
    //   try
    //     parser.ParseCommandLine args
    //   with
    //     | exn ->
    //       exn.Message
    //       |> Error.asOther "Main"
    //       |> Error.exitWith

    // validateOptions parsed

    // // Init machine config
    // parsed.TryGetResult <@ Machine @>
    // |> Option.map System.IO.Path.GetFullPath
    // |> MachineConfig.init
    // |> Error.orExit ignore

    // let result =
    //   let machine = MachineConfig.get()
    //   let dir =
    //     parsed.TryGetResult <@ Project @>
    //     |> Option.map (fun projectName ->
    //       machine.WorkSpace </> projectName)

    //   let interactive = parsed.Contains <@ Interactive @>

    //   match parsed.GetResult <@ Cmd @>, dir with
    //   | Create,            _ -> createProject parsed
    //   | Start,           dir -> startService interactive dir
    //   | Reset,      Some dir -> resetProject dir
    //   | Dump,       Some dir -> dumpDataDir dir
    //   | Add_User,   Some dir -> addUser dir
    //   | Add_Member, Some dir -> addMember dir
    //   | Help,              _ -> help ()
    //   |  _ ->
    //     sprintf "Unexpected command line failure: %A" args
    //     |> Error.asParseError "Main"
    //     |> Either.fail

    // result |> Error.orExit ignore

    Error.exitWith IrisError.OK
