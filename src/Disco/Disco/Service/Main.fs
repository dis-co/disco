namespace Disco.Service

open Argu
open Disco.Core
open System
open System.Threading
open Disco.Raft
open Disco.Client
open Disco.Service
open Disco.Service.Interfaces
open Disco.Service.CommandLine

[<AutoOpen>]
module Main =
  let private prompt () =
    do Console.Write "> "

  let private promptKey () =
    do prompt()
    Console.ReadKey()

  let private promptYesNo() =
    printf "(y/n) "
    let key = Console.ReadKey()
    key.KeyChar = 'y'

  let private promptLine () =
    do prompt ()
    Console.ReadLine()

  let private readBindAddress () =
    let _, interfaces =
      Network.getInterfaces ()
      |> List.collect (fun iface -> List.map (fun addr -> iface.Name, string addr) iface.IpAddresses)
      |> List.fold
        (fun (idx, map) pair ->
          let map = Map.add (string idx) pair map
          idx + 1, map)
        (0,Map.empty)
    let mutable address = None

    printfn "Choose the interface (number) you want to use for Disco:"
    Map.iter (fun key (iface,addr) -> printfn "(%s) %s: %s" key iface addr) interfaces

    while Option.isNone address do
      let choice = promptKey()
      match Map.tryFind (string choice.KeyChar) interfaces with
      | Some (name, addr) -> address <- Some addr
      | None -> ()
    Console.WriteLine()
    let result = (Option.get address)
    printfn "using address: %A" result
    result

  let private readMulticastAddress() =
    printfn "Enter the multicast group address to use:"
    printfn "(Enter) %s" Constants.DEFAULT_MCAST_ADDRESS
    let result = promptLine()
    if String.IsNullOrEmpty result
    then Constants.DEFAULT_MCAST_ADDRESS
    else result

  let private hostName () =
    let host = Network.getHostName()
    printfn "Enter the machine name:"
    printfn "(Enter) %s" host
    let result = promptLine()
    if String.IsNullOrEmpty result
    then host
    else result

  let private workspace () =
    let defaultWorkspace = MachineConfig.defaultWorkspace()
    printfn "Enter the path to the workspace directory:"
    printfn "(Enter) %A" defaultWorkspace
    let result = promptLine()
    if String.IsNullOrEmpty result
    then defaultWorkspace
    else filepath result

  let private assetDirectory () =
    let defaultAssetDirectory = MachineConfig.defaultAssetDirectory()
    printfn "Enter the path to the asset directory directory:"
    printfn "(Enter) %A" defaultAssetDirectory
    let result = promptLine()
    if String.IsNullOrEmpty result
    then defaultAssetDirectory
    else filepath result

  let private logDirectory () =
    let defaultLogDirectory = MachineConfig.defaultLogDirectory()
    printfn "Enter the path to the log directory directory:"
    printfn "(Enter) %A" defaultLogDirectory
    let result = promptLine()
    if String.IsNullOrEmpty result
    then defaultLogDirectory
    else filepath result

  ///  ____       _
  /// / ___|  ___| |_ _   _ _ __
  /// \___ \ / _ \ __| | | | '_ \
  ///  ___) |  __/ |_| |_| | |_) |
  /// |____/ \___|\__|\__,_| .__/
  ///                      |_|

  let private setupPrompt (parsed:ParseResults<CLIArguments>) =
    let target =
      parsed.TryGetResult <@ Machine @>
      |> Option.map (filepath >> Path.getFullPath)

    let address = readBindAddress()
    let mcast = readMulticastAddress()
    let host = hostName()
    let assetDir = assetDirectory()
    let logDir = logDirectory()
    let workspace = workspace()

    let machine =
      let fileName = filepath (Constants.MACHINECONFIG_NAME + Constants.ASSET_EXTENSION)
      match target with
      | Some path when Directory.exists path && Directory.contains (path </> fileName) path ->
        match MachineConfig.load target with
        | Right config -> config
        | _ -> MachineConfig.create address None
      | _ -> MachineConfig.create address None

    let result =
      machine
      |> MachineConfig.setMulticastAddress (IpAddress.Parse mcast)
      |> MachineConfig.setHostName (name host)
      |> MachineConfig.setAssetDirectory assetDir
      |> MachineConfig.setWorkSpace workspace
      |> MachineConfig.setLogDirectory logDir

    printfn "%A" result
    printfn "Looks good?"

    let yes = promptYesNo()

    if yes
    then MachineConfig.save target result
    else Either.nothing

  // ** setupDefaults

  let private setupDefaults (parsed:ParseResults<CLIArguments>) =
    let target =
      parsed.TryGetResult <@ Machine @>
      |> Option.map (filepath >> Path.getFullPath)

    let addrs =
      Network.getInterfaces ()
      |> List.filter Network.isOnline

    if List.length addrs = 0 then
      printfn "error: no public network interfaces found"
      exit 1

    let address =
      let iface = List.head addrs
      let ip = List.head iface.IpAddresses
      string ip

    let machine =
      let fileName = filepath (Constants.MACHINECONFIG_NAME + Constants.ASSET_EXTENSION)
      match target with
      | Some path when Directory.exists path && Directory.contains (path </> fileName) path ->
        match MachineConfig.load target with
        | Right config -> config
        | _ -> MachineConfig.create address None
      | _ -> MachineConfig.create address None

    MachineConfig.save target machine

  // ** setup

  let private setup (parsed:ParseResults<CLIArguments>) =
    match parsed.TryGetResult <@ Yes @> with
    | Some _ -> setupDefaults parsed
    | None -> setupPrompt parsed

  ///  ____  _             _
  /// / ___|| |_ __ _ _ __| |_
  /// \___ \| __/ _` | '__| __|
  ///  ___) | || (_| | |  | |_
  /// |____/ \__\__,_|_|   \__|

  let private start (parsed:ParseResults<CLIArguments>) =
    let machine =
      parsed.TryGetResult <@ Machine @>
      |> Option.map filepath
      |> MachineConfig.load
    match machine with
    | Left error -> Error.exitWith error
    | Right machine ->
      do MachineConfig.set machine
      let validation = MachineConfig.validate machine
      if not validation.IsEmpty then
        printfn "Machine configuration file is invalid, please check the following settings:"
        printfn ""
        for KeyValue(name, _) in validation do
          printfn "    %A must not be empty" name
        printfn ""
        exit 1

      let dir =
        parsed.TryGetResult <@ Project @>
        |> Option.map (fun projectName ->
          machine.WorkSpace </> filepath projectName)

      let frontend =
        parsed.TryGetResult <@ Frontend @>
        |> Option.map filepath

      startService machine dir frontend

  ///  __  __       _
  /// |  \/  | __ _(_)_ __
  /// | |\/| |/ _` | | '_ \
  /// | |  | | (_| | | | | |
  /// |_|  |_|\__,_|_|_| |_|

  [<EntryPoint>]
  let main args =
    let parsed =
      try parser.ParseCommandLine args
      with exn ->
        exn.Message
        |> Error.asOther "Main"
        |> Error.exitWith

    let result =
      match parsed.GetResult <@ Cmd @> with
      | Start   -> start parsed
      | Setup   -> setup parsed
      | Help  _ -> help ()

    result |> Error.orExit ignore

    Error.exitWith DiscoError.OK
