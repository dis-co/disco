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

  let mkclient server client =
    either {
      let! clnt = ApiClient.create server client
      let obs2 = clnt.Subscribe(printfn "clnt: %A")
      do! clnt.Start()
      return { new IDisposable with
                 member self.Dispose() =
                   dispose clnt
                   dispose obs2 }
    }

  [<EntryPoint>]
  let main args =
    let result =
      either {
        // use logger = Logger.subscribe Logger.stdout
        let mem = Member.create (Id.Create())
        use! srvr = ApiServer.create mem
        use obs1 = srvr.Subscribe(printfn "srvr: %A")
        do! srvr.Start()

        let server : IrisServer =
          { Id = Id.Create()
            Name = "cool"
            Port = mem.ApiPort
            IpAddress = mem.IpAddr }

        let client : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            Status = ServiceStatus.Starting
            IpAddress = IPv4Address "127.0.0.1"
            Port = 9001us }

        let aclient = ref (Unchecked.defaultof<IDisposable>)
        let! c = mkclient server client
        aclient := c

        while true do
          match Console.ReadLine() with
          | "stop server" -> dispose srvr
          | "stop client" -> dispose !aclient
          | "start client" ->
            let! c = mkclient server client
            aclient := c
          | _ ->
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
