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
    //  ____        _       ______        _
    // |  _ \ _   _| |__   / / ___| _   _| |__
    // | |_) | | | | '_ \ / /\___ \| | | | '_ \
    // |  __/| |_| | |_) / /  ___) | |_| | |_) |
    // |_|    \__,_|_.__/_/  |____/ \__,_|_.__/

    // use logobs = Logger.subscribe Logger.stdout

    // if args.[0] = "--pub" then
    //   let rand = new Random()

    //   let publisher = new Iris.Zmq.Pub(Id.Create(),sprintf "epgm://%s;224.0.0.1:5555" args.[1], args.[2])
    //   publisher.Start()
    //   |> ignore

    //   let rec pub (p: Iris.Zmq.Pub) =
    //     async {
    //       do! Async.Sleep(1000)
    //       let bytes = Array.zeroCreate 5  // 5 bytes of random stuff
    //       let num = rand.NextBytes bytes
    //       p.Publish bytes |> ignore
    //       printfn "Published new message: %A" bytes
    //       return! pub p
    //     }

    //   Async.Start(pub publisher)

    // elif args.[0] = "--sub" then
    //   let subscriber = new Iris.Zmq.Sub.Sub(Id.Create(),sprintf "epgm://%s;224.0.0.1:5555" args.[1], args.[2])

    //   let obs = subscriber.Subscribe(printfn "received: %A")

    //   subscriber.Start()
    //   |> ignore
    // else
    //   printfn "Bollocks"

    // while true do
    //   Console.ReadLine()
    //   |> ignore

    //     _          _  ____ _ _            _     _____         _
    //    / \   _ __ (_)/ ___| (_) ___ _ __ | |_  |_   _|__  ___| |_
    //   / _ \ | '_ \| | |   | | |/ _ \ '_ \| __|   | |/ _ \/ __| __|
    //  / ___ \| |_) | | |___| | |  __/ | | | |_    | |  __/\__ \ |_
    // /_/   \_\ .__/|_|\____|_|_|\___|_| |_|\__|   |_|\___||___/\__|
    //         |_|

    let result =
      either {
        let logobs = Logger.subscribe (string >> printfn "%s")

        let pid = Id.Create()

        let mem =
          { Member.create (Id.Create()) with
              IpAddr = IPv4Address "192.168.2.108"
              ApiPort = 10000us }

        let! server = ApiServer.create mem pid
        do! server.Start()
        do! server.SetState State.Empty
        let obs = server.Subscribe(printfn "ApiEvent: %A")

        return
          { new IDisposable with
              member self.Dispose() =
                dispose obs
                dispose server
                dispose logobs }
      }

    match result with
    | Right server ->
      let mutable run = true

      while run do
        match Console.ReadLine() with
        | "quit" | "exit" ->
          dispose server
          run <- false
        | _ -> ()

    | Left error ->
      printfn "Error: %A" error

    //  ____                  _
    // |  _ \ ___  __ _ _   _| | __ _ _ __
    // | |_) / _ \/ _` | | | | |/ _` | '__|
    // |  _ <  __/ (_| | |_| | | (_| | |
    // |_| \_\___|\__, |\__,_|_|\__,_|_|
    //            |___/

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
