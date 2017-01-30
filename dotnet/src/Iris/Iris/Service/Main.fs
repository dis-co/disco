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
      either {
        let mem =
          { Member.create (Id.Create()) with
              IpAddr = IPv4Address "192.168.2.108"
              ApiPort = 10000us }

        let! server = ApiServer.create mem
        do! server.Start()
        do! server.SetState State.Empty
        let obs = server.Subscribe(printfn "ApiEvent: %A")

        return
          { new IDisposable with
              member self.Dispose() =
                dispose server
                dispose obs }
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
