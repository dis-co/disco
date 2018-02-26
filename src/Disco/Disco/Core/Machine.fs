(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System
open System.IO
open FlatBuffers
open Disco.Serialization
open System.Reflection
open System.Runtime.CompilerServices

#endif

// * DiscoMachine

///  ____  _               __  __            _     _
/// |  _ \(_)___  ___ ___ |  \/  | __ _  ___| |__ (_)_ __   ___
/// | | | | / __|/ __/ _ \| |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
/// | |_| | \__ \ (_| (_) | |  | | (_| | (__| | | | | | | |  __/
/// |____/|_|___/\___\___/|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

type DiscoMachine =
  { MachineId:        MachineId
    HostName:         Name
    WorkSpace:        FilePath
    AssetDirectory:   FilePath
    AssetFilter:      string
    LogDirectory:     FilePath
    CollectMetrics:   bool
    MetricsHost:      IpAddress
    MetricsPort:      Port
    MetricsDb:        string
    BindAddress:      IpAddress
    MulticastAddress: IpAddress
    MulticastPort:    Port
    WebPort:          Port
    RaftPort:         Port
    WsPort:           Port
    GitPort:          Port
    ApiPort:          Port
    Version:          Disco.Core.Version }

  // ** optics

  static member MachineId_ =
    (fun (machine:DiscoMachine) -> machine.MachineId),
    (fun id (machine:DiscoMachine) -> { machine with MachineId = id })

  static member HostName_ =
    (fun (machine:DiscoMachine) -> machine.HostName),
    (fun hostName (machine:DiscoMachine) -> { machine with HostName = hostName })

  static member WorkSpace_ =
    (fun (machine:DiscoMachine) -> machine.WorkSpace),
    (fun workSpace (machine:DiscoMachine) -> { machine with WorkSpace = workSpace })

  static member AssetDirectory_ =
    (fun (machine:DiscoMachine) -> machine.AssetDirectory),
    (fun assetDirectory (machine:DiscoMachine) -> { machine with AssetDirectory = assetDirectory })

  static member AssetFilter_ =
    (fun (machine:DiscoMachine) -> machine.AssetFilter),
    (fun assetFilter (machine:DiscoMachine) -> { machine with AssetFilter = assetFilter })

  static member LogDirectory_ =
    (fun (machine:DiscoMachine) -> machine.LogDirectory),
    (fun logDirectory (machine:DiscoMachine) -> { machine with LogDirectory = logDirectory })

  static member MulticastAddress_ =
    (fun (machine:DiscoMachine) -> machine.MulticastAddress),
    (fun multicastAddress (machine:DiscoMachine) -> { machine with MulticastAddress = multicastAddress })

  static member MulticastPort_ =
    (fun (machine:DiscoMachine) -> machine.MulticastPort),
    (fun multicastPort (machine:DiscoMachine) -> { machine with MulticastPort = multicastPort })

  static member BindAddress_ =
    (fun (machine:DiscoMachine) -> machine.BindAddress),
    (fun bindAddress (machine:DiscoMachine) -> { machine with BindAddress = bindAddress })

  static member WebPort_ =
    (fun (machine:DiscoMachine) -> machine.WebPort),
    (fun webPort (machine:DiscoMachine) -> { machine with WebPort = webPort })

  static member RaftPort_ =
    (fun (machine:DiscoMachine) -> machine.RaftPort),
    (fun raftPort (machine:DiscoMachine) -> { machine with RaftPort = raftPort })

  static member WsPort_ =
    (fun (machine:DiscoMachine) -> machine.WsPort),
    (fun wsPort (machine:DiscoMachine) -> { machine with WsPort = wsPort })

  static member GitPort_ =
    (fun (machine:DiscoMachine) -> machine.GitPort),
    (fun gitPort (machine:DiscoMachine) -> { machine with GitPort = gitPort })

  static member ApiPort_ =
    (fun (machine:DiscoMachine) -> machine.ApiPort),
    (fun apiPort (machine:DiscoMachine) -> { machine with ApiPort = apiPort })

  static member Version_ =
    (fun (machine:DiscoMachine) -> machine.Version),
    (fun version (machine:DiscoMachine) -> { machine with Version = version })

  // ** ToString

  override self.ToString() =
    sprintf "MachineId: %s" (string self.MachineId)

  // ** ToOffset

  member machine.ToOffset(builder: FlatBufferBuilder) =
    let mapNull = function
      | null -> None
      | str -> builder.CreateString str |> Some
    let webip = machine.BindAddress |> string |> builder.CreateString
    let mcastip = machine.MulticastAddress |> string |> builder.CreateString
    let workspace = machine.WorkSpace |> unwrap |> mapNull
    let logdir = machine.LogDirectory |> unwrap |> mapNull
    let assetdir = machine.AssetDirectory |> unwrap |> mapNull
    let assetFilter = machine.AssetFilter |> unwrap |> mapNull
    let hostname = machine.HostName |> unwrap |> mapNull
    let machineid = DiscoMachineFB.CreateMachineIdVector(builder, machine.MachineId.ToByteArray())
    let version = machine.Version |> unwrap |> mapNull
    let metricsHost = machine.MetricsHost |> string |> builder.CreateString
    let metricsDb = machine.MetricsDb |> mapNull

    DiscoMachineFB.StartDiscoMachineFB(builder)
    DiscoMachineFB.AddMachineId(builder, machineid)
    Option.iter (fun value -> DiscoMachineFB.AddHostName(builder, value)) hostname
    Option.iter (fun value -> DiscoMachineFB.AddWorkSpace(builder, value)) workspace
    Option.iter (fun value -> DiscoMachineFB.AddLogDirectory(builder, value)) logdir
    Option.iter (fun value -> DiscoMachineFB.AddAssetDirectory(builder, value)) assetdir
    Option.iter (fun value -> DiscoMachineFB.AddAssetFilter(builder, value)) assetFilter
    DiscoMachineFB.AddCollectMetrics(builder, machine.CollectMetrics)
    DiscoMachineFB.AddMetricsHost(builder, metricsHost)
    Option.iter (fun value -> DiscoMachineFB.AddMetricsDb(builder, value)) metricsDb
    DiscoMachineFB.AddMetricsPort(builder, unwrap machine.MetricsPort)
    DiscoMachineFB.AddMulticastAddress(builder, mcastip)
    DiscoMachineFB.AddMulticastPort(builder, unwrap machine.MulticastPort)
    DiscoMachineFB.AddBindAddress(builder, webip)
    DiscoMachineFB.AddWebPort(builder, unwrap machine.WebPort)
    DiscoMachineFB.AddRaftPort(builder, unwrap machine.RaftPort)
    DiscoMachineFB.AddWsPort(builder, unwrap machine.WsPort)
    DiscoMachineFB.AddGitPort(builder, unwrap machine.GitPort)
    DiscoMachineFB.AddApiPort(builder, unwrap machine.ApiPort)
    Option.iter (fun value ->DiscoMachineFB.AddVersion(builder, value)) version
    DiscoMachineFB.EndDiscoMachineFB(builder)

  // ** FromFB

  static member FromFB (fb: DiscoMachineFB) =
    result {
      let! machineId = Id.decodeMachineId fb
      let! ip = IpAddress.TryParse fb.BindAddress
      let! metricsHost = IpAddress.TryParse fb.MetricsHost
      let! mcastip = IpAddress.TryParse fb.MulticastAddress
      return {
        MachineId        = machineId
        WorkSpace        = filepath fb.WorkSpace
        AssetDirectory   = filepath fb.AssetDirectory
        AssetFilter      = fb.AssetFilter
        LogDirectory     = filepath fb.LogDirectory
        CollectMetrics   = fb.CollectMetrics
        MetricsHost      = metricsHost
        MetricsPort      = port fb.MetricsPort
        MetricsDb        = fb.MetricsDb
        HostName         = name fb.HostName
        BindAddress      = ip
        MulticastAddress = mcastip
        MulticastPort    = port fb.MulticastPort
        WebPort          = port fb.WebPort
        RaftPort         = port fb.RaftPort
        WsPort           = port fb.WsPort
        GitPort          = port fb.GitPort
        ApiPort          = port fb.ApiPort
        Version          = version fb.Version
      }
    }


  // *** ToBytes

  member machine.ToBytes() = Binary.buildBuffer machine

  // *** FromBytes

  static member FromBytes(bytes: byte[]) =
    bytes
    |> Binary.createBuffer
    |> DiscoMachineFB.GetRootAsDiscoMachineFB
    |> DiscoMachine.FromFB

  // ** Default

  static member Default
    with get () =
      { MachineId        = DiscoId.Create()
        HostName         = name "<empty>"
        CollectMetrics   = false
        MetricsHost      = IPv4Address Constants.DEFAULT_METRICS_HOST
        MetricsPort      = port Constants.DEFAULT_METRICS_PORT
        MetricsDb        = Constants.DEFAULT_METRICS_DB
        #if FABLE_COMPILER
        WorkSpace        = filepath "/dev/null"
        LogDirectory     = filepath "/dev/null"
        AssetDirectory   = filepath "/dev/null"
        #else
        WorkSpace        = filepath Environment.CurrentDirectory
        AssetDirectory   = filepath Environment.CurrentDirectory
        LogDirectory     = filepath Environment.CurrentDirectory
        #endif
        AssetFilter      = Constants.DEFAULT_ASSET_FILTER
        BindAddress      = IPv4Address "127.0.0.1"
        MulticastAddress = IpAddress.Parse Constants.DEFAULT_MCAST_ADDRESS
        MulticastPort    = port Constants.DEFAULT_MCAST_PORT
        WebPort          = port Constants.DEFAULT_HTTP_PORT
        RaftPort         = port Constants.DEFAULT_RAFT_PORT
        WsPort           = port Constants.DEFAULT_WEB_SOCKET_PORT
        GitPort          = port Constants.DEFAULT_GIT_PORT
        ApiPort          = port Constants.DEFAULT_API_PORT
        Version          = version Build.VERSION }

// * DiscoMachineYaml

type DiscoMachineYaml () =
  [<DefaultValue>] val mutable MachineId:        string
  [<DefaultValue>] val mutable HostName:         string
  [<DefaultValue>] val mutable WorkSpace:        string
  [<DefaultValue>] val mutable AssetDirectory:   string
  [<DefaultValue>] val mutable AssetFilter:      string
  [<DefaultValue>] val mutable LogDirectory:     string
  [<DefaultValue>] val mutable CollectMetrics:   bool

  [<DefaultValue>] val mutable MetricsHost:      string

  [<DefaultValue>] val mutable MetricsPort:      uint16

  [<DefaultValue>] val mutable MetricsDb:        string
  [<DefaultValue>] val mutable BindAddress:      string
  [<DefaultValue>] val mutable MulticastAddress: string
  [<DefaultValue>] val mutable MulticastPort:    uint16
  [<DefaultValue>] val mutable WebPort:          uint16
  [<DefaultValue>] val mutable RaftPort:         uint16
  [<DefaultValue>] val mutable WsPort:           uint16
  [<DefaultValue>] val mutable GitPort:          uint16
  [<DefaultValue>] val mutable ApiPort:          uint16
  [<DefaultValue>] val mutable Version:          string

  static member Create (cfg: DiscoMachine) =
    let yml = DiscoMachineYaml()
    yml.MachineId        <- string cfg.MachineId
    yml.HostName         <- string cfg.HostName
    yml.WorkSpace        <- unwrap cfg.WorkSpace
    yml.AssetDirectory   <- unwrap cfg.AssetDirectory
    yml.AssetFilter      <- cfg.AssetFilter
    yml.LogDirectory     <- unwrap cfg.LogDirectory
    yml.CollectMetrics   <- cfg.CollectMetrics
    yml.MetricsHost      <- string cfg.MetricsHost
    yml.MetricsPort      <- unwrap cfg.MetricsPort
    yml.MetricsDb        <- cfg.MetricsDb
    yml.BindAddress      <- string cfg.BindAddress
    yml.MulticastAddress <- string cfg.MulticastAddress
    yml.MulticastPort    <- unwrap cfg.MulticastPort
    yml.WebPort          <- unwrap cfg.WebPort
    yml.RaftPort         <- unwrap cfg.RaftPort
    yml.WsPort           <- unwrap cfg.WsPort
    yml.GitPort          <- unwrap cfg.GitPort
    yml.ApiPort          <- unwrap cfg.ApiPort
    yml.Version          <- cfg.Version.ToString()
    yml

// * MachineStatus

[<AutoOpen>]
module MachineStatus =

  [<Literal>]
  let IDLE = "idle"

  [<Literal>]
  let BUSY = "busy"

  // ** MachineStatus

  type MachineStatus =
    | Idle
    | Busy of ProjectId:ProjectId * ProjectName:Name

    // *** ToString

    override status.ToString() =
      match status with
      | Idle   -> IDLE
      | Busy _ -> BUSY

    // *** ToOffset

    member status.ToOffset(builder: FlatBufferBuilder) =
      let mapNull (builder: FlatBufferBuilder) = function
        | null -> None
        | other -> builder.CreateString other |> Some
      match status with
      | Idle ->
        MachineStatusFB.StartMachineStatusFB(builder)
        MachineStatusFB.AddStatus(builder, MachineStatusEnumFB.IdleFB)
        MachineStatusFB.EndMachineStatusFB(builder)
      | Busy (id, name) ->
        let idoff = MachineStatusFB.CreateProjectIdVector(builder,id.ToByteArray())
        let nameoff = name |> unwrap |> mapNull builder
        MachineStatusFB.StartMachineStatusFB(builder)
        MachineStatusFB.AddStatus(builder, MachineStatusEnumFB.BusyFB)
        MachineStatusFB.AddProjectId(builder, idoff)
        Option.iter (fun value -> MachineStatusFB.AddProjectName(builder,value)) nameoff
        MachineStatusFB.EndMachineStatusFB(builder)

    // *** FromOffset

    static member FromFB(fb: MachineStatusFB) =
      #if FABLE_COMPILER
      match fb.Status with
      | x when x = MachineStatusEnumFB.IdleFB -> Result.succeed Idle
      | x when x = MachineStatusEnumFB.BusyFB ->
        result {
          let! id = Id.decodeProjectId fb
          return Busy (id, name fb.ProjectName)
        }
      | other ->
        sprintf "Unknown Machine Status: %d" other
        |> Error.asParseError "MachineStatus.FromOffset"
        |> Result.fail
      #else
      match fb.Status with
      | MachineStatusEnumFB.IdleFB -> Result.succeed Idle
      | MachineStatusEnumFB.BusyFB ->
        result {
          let! id = Id.decodeProjectId fb
          return Busy (id, name fb.ProjectName)
        }
      | other ->
        sprintf "Unknown Machine Status: %O" other
        |> Error.asParseError "MachineStatus.FromOffset"
        |> Result.fail
      #endif

    // *** ToBytes

    member status.ToBytes() = Binary.buildBuffer status

    // *** FromBytes

    static member FromBytes(bytes: byte[]) =
      bytes
      |> Binary.createBuffer
      |> MachineStatusFB.GetRootAsMachineStatusFB
      |> MachineStatus.FromFB

// * MachineConfig module

[<RequireQualifiedAccess>]
module MachineConfig =
  open Path
  open Aether

  #if !FABLE_COMPILER && !DISCO_NODES

  open SharpYaml.Serialization

  #endif

  // ** tag

  let private tag (str: string) = sprintf "MachineConfig.%s" str

  // ** getters

  let machineId = Optic.get DiscoMachine.MachineId_
  let hostName = Optic.get DiscoMachine.HostName_
  let workSpace = Optic.get DiscoMachine.WorkSpace_
  let assetDirectory = Optic.get DiscoMachine.AssetDirectory_
  let assetFilter = Optic.get DiscoMachine.AssetFilter_
  let logDirectory = Optic.get DiscoMachine.LogDirectory_
  let bindAddress = Optic.get DiscoMachine.BindAddress_
  let multicastAddress = Optic.get DiscoMachine.MulticastAddress_
  let multicastPort = Optic.get DiscoMachine.MulticastPort_
  let webPort = Optic.get DiscoMachine.WebPort_
  let raftPort = Optic.get DiscoMachine.RaftPort_
  let wsPort = Optic.get DiscoMachine.WsPort_
  let gitPort = Optic.get DiscoMachine.GitPort_
  let apiPort = Optic.get DiscoMachine.ApiPort_
  let version = Optic.get DiscoMachine.Version_

  // ** setters

  let setMachineId = Optic.set DiscoMachine.MachineId_
  let setHostName = Optic.set DiscoMachine.HostName_
  let setWorkSpace = Optic.set DiscoMachine.WorkSpace_
  let setAssetDirectory = Optic.set DiscoMachine.AssetDirectory_
  let setAssetFilter = Optic.set DiscoMachine.AssetFilter_
  let setLogDirectory = Optic.set DiscoMachine.LogDirectory_
  let setBindAddress = Optic.set DiscoMachine.BindAddress_
  let setMulticastAddress = Optic.set DiscoMachine.MulticastAddress_
  let setMulticastPort = Optic.set DiscoMachine.MulticastPort_
  let setWebPort = Optic.set DiscoMachine.WebPort_
  let setRaftPort = Optic.set DiscoMachine.RaftPort_
  let setWsPort = Optic.set DiscoMachine.WsPort_
  let setGitPort = Optic.set DiscoMachine.GitPort_
  let setApiPort = Optic.set DiscoMachine.ApiPort_
  let setVersion = Optic.set DiscoMachine.Version_

  // ** singleton

  let mutable private singleton = Unchecked.defaultof<DiscoMachine>

  // ** get

  let get() = singleton

  // ** set

  let set config = singleton <- config

  #if !FABLE_COMPILER && !DISCO_NODES

  // ** getLocation

  let getLocation = function
    | Some location ->
      if Path.endsWith ASSET_EXTENSION location
      then location
      else location </> filepath (MACHINECONFIG_NAME + ASSET_EXTENSION)
    | None ->
      Assembly.GetExecutingAssembly().Location
      |> Path.GetDirectoryName
      <.> MACHINECONFIG_DEFAULT_PATH
      </> filepath (MACHINECONFIG_NAME + ASSET_EXTENSION)

  // ** parse

  let private parse (yml: DiscoMachineYaml) : DiscoResult<DiscoMachine> =
    result {
      let! ip = IpAddress.TryParse yml.BindAddress
      let! metricsHost = IpAddress.TryParse yml.MetricsHost
      let! id = DiscoId.TryParse yml.MachineId
      let! mcastip = IpAddress.TryParse yml.MulticastAddress
      return {
        MachineId        = id
        HostName         = name yml.HostName
        WorkSpace        = filepath yml.WorkSpace
        AssetDirectory   = filepath yml.AssetDirectory
        AssetFilter      = yml.AssetFilter
        LogDirectory     = filepath yml.LogDirectory
        CollectMetrics   = yml.CollectMetrics
        MetricsHost      = metricsHost
        MetricsPort      = port yml.MetricsPort
        MetricsDb        = yml.MetricsDb
        BindAddress      = ip
        MulticastAddress = mcastip
        MulticastPort    = port yml.MulticastPort
        WebPort          = port yml.WebPort
        RaftPort         = port yml.RaftPort
        WsPort           = port yml.WsPort
        GitPort          = port yml.GitPort
        ApiPort          = port yml.ApiPort
        Version          = Measure.version yml.Version
      }
    }

  // ** ensureExists (private)

  let private ensureExists (path: FilePath) =
    try
      if not (Directory.exists path) then
        Directory.createDirectory path |> ignore
    with
      | _ -> ()


  // ** defaultWorkspace

  let defaultWorkspace () =
    if Platform.isUnix then
      let home = Environment.GetEnvironmentVariable "HOME"
      home <.> MACHINECONFIG_DEFAULT_WORKSPACE_UNIX
    else filepath MACHINECONFIG_DEFAULT_WORKSPACE_WINDOWS

  // ** defaultAssetDirectory

  let defaultAssetDirectory () =
    if Platform.isUnix then
      let home = Environment.GetEnvironmentVariable "HOME"
      home <.> MACHINECONFIG_DEFAULT_ASSET_DIRECTORY_UNIX
    else filepath MACHINECONFIG_DEFAULT_ASSET_DIRECTORY_WINDOWS

  // ** logDir

  let defaultLogDirectory () =
    let workspace = defaultWorkspace()
    workspace </> filepath "log"

  // ** create

  let create (bindIp: string) (shiftDefaults: uint16 option) : DiscoMachine =
    let shiftPort p =
        match shiftDefaults with
        | Some shift -> port (p + shift)
        | None -> port p

    let hostname = Network.getHostName()
    let workspace = defaultWorkspace()
    let assetDir = defaultAssetDirectory()
    let logDir = defaultLogDirectory()

    if Directory.exists workspace |> not then
      Directory.createDirectory workspace |> ignore

    if Directory.exists assetDir |> not then
      Directory.createDirectory assetDir |> ignore

    let version = Assembly.GetExecutingAssembly().GetName().Version |> string |> Measure.version

    { MachineId        = DiscoId.Create()
      HostName         = name hostname
      WorkSpace        = workspace
      AssetDirectory   = assetDir
      AssetFilter      = Constants.DEFAULT_ASSET_FILTER
      LogDirectory     = logDir
      CollectMetrics   = false
      MetricsHost      = IPv4Address Constants.DEFAULT_METRICS_HOST
      MetricsPort      = port Constants.DEFAULT_METRICS_PORT
      MetricsDb        = Constants.DEFAULT_METRICS_DB
      BindAddress      = IpAddress.Parse bindIp
      MulticastAddress = IpAddress.Parse Constants.DEFAULT_MCAST_ADDRESS
      MulticastPort    = port Constants.DEFAULT_MCAST_PORT
      WebPort          = shiftPort Constants.DEFAULT_HTTP_PORT
      RaftPort         = shiftPort Constants.DEFAULT_RAFT_PORT
      WsPort           = shiftPort Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort          = shiftPort Constants.DEFAULT_GIT_PORT
      ApiPort          = shiftPort Constants.DEFAULT_API_PORT
      Version          = version }

  // ** save

  let save (path: FilePath option) (cfg: DiscoMachine) : DiscoResult<unit> =
    let serializer = Serializer()

    try
      let location = getLocation path
      let payload =
        cfg
        |> DiscoMachineYaml.Create
        |> serializer.Serialize

      location
      |> unwrap
      |> Path.GetDirectoryName
      |> filepath
      |> ensureExists

      File.WriteAllText(unwrap location, payload)
      |> Result.succeed
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "save")
        |> Result.fail

  // ** load

  let load path =
    try
      let location = getLocation path
      if File.exists location then
        printfn "loading configuration from: %A" location
        let raw = File.ReadAllText(unwrap location)
        let serializer = Serializer()
        serializer.Deserialize<DiscoMachineYaml>(raw)
        |> parse
      else
        "could not find machine configuration"
        |> Error.asIOError (tag "load")
        |> Result.fail
    with exn ->
      exn.Message
      |> Error.asIOError (tag "load")
      |> Result.fail

  // ** init

  /// Attention: this method must be called only when starting the main process
  let init getBindIp shiftDefaults (path: FilePath option) : DiscoResult<unit> =
    let serializer = Serializer()
    try
      let location = getLocation path
      let cfg =
        if File.exists location
        then
          let raw = File.ReadAllText(unwrap location)
          serializer.Deserialize<DiscoMachineYaml>(raw)
          |> parse
        else
          let bindIp = getBindIp()
          let cfg = create bindIp shiftDefaults
          save path cfg
          |> Result.map (fun _ -> cfg)

      match cfg with
      | Error err -> Result.fail err
      | Ok cfg ->
        if Path.IsPathRooted (unwrap cfg.WorkSpace)
        then singleton <- cfg
        else
          let wp =
            unwrap location
            |> Path.GetDirectoryName
            |> filepath
            </> cfg.WorkSpace
          singleton <- { cfg with WorkSpace = wp }
        Result.succeed()
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "load")
        |> Result.fail

  #endif

  // ** validate

  let validate (config: DiscoMachine) =
    let inline check (o: obj) = o |> isNull |> not
    [ ("LogDirectory",     check config.LogDirectory)
      ("WorkSpace",        check config.WorkSpace)
      ("AssetDirectory",   check config.AssetDirectory)
      ("MachineId",        check config.MachineId)
      ("MulticastAddress", check config.MulticastAddress)
      ("BindAddress",      check config.BindAddress) ]
    |> List.fold
        (fun m (name,result) ->
          if not result then
            Map.add name result m
          else m)
        Map.empty
