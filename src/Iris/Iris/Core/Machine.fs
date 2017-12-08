namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.IO
open FlatBuffers
open Iris.Serialization
open System.Reflection
open System.Runtime.CompilerServices

#endif

// * IrisMachine

//  ___      _     __  __            _     _
// |_ _|_ __(_)___|  \/  | __ _  ___| |__ (_)_ __   ___
//  | || '__| / __| |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  | || |  | \__ \ |  | | (_| | (__| | | | | | | |  __/
// |___|_|  |_|___/_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

type IrisMachine =
  { MachineId:        MachineId
    HostName:         Name
    WorkSpace:        FilePath
    AssetDirectory:   FilePath
    AssetFilter:      string
    LogDirectory:     FilePath
    BindAddress:      IpAddress
    MulticastAddress: IpAddress
    MulticastPort:    Port
    WebPort:          Port
    RaftPort:         Port
    WsPort:           Port
    GitPort:          Port
    ApiPort:          Port
    Version:          Iris.Core.Version }

  // ** optics

  static member MachineId_ =
    (fun (machine:IrisMachine) -> machine.MachineId),
    (fun id (machine:IrisMachine) -> { machine with MachineId = id })

  static member HostName_ =
    (fun (machine:IrisMachine) -> machine.HostName),
    (fun hostName (machine:IrisMachine) -> { machine with HostName = hostName })

  static member WorkSpace_ =
    (fun (machine:IrisMachine) -> machine.WorkSpace),
    (fun workSpace (machine:IrisMachine) -> { machine with WorkSpace = workSpace })

  static member AssetDirectory_ =
    (fun (machine:IrisMachine) -> machine.AssetDirectory),
    (fun assetDirectory (machine:IrisMachine) -> { machine with AssetDirectory = assetDirectory })

  static member AssetFilter_ =
    (fun (machine:IrisMachine) -> machine.AssetFilter),
    (fun assetFilter (machine:IrisMachine) -> { machine with AssetFilter = assetFilter })

  static member LogDirectory_ =
    (fun (machine:IrisMachine) -> machine.LogDirectory),
    (fun logDirectory (machine:IrisMachine) -> { machine with LogDirectory = logDirectory })

  static member MulticastAddress_ =
    (fun (machine:IrisMachine) -> machine.MulticastAddress),
    (fun multicastAddress (machine:IrisMachine) -> { machine with MulticastAddress = multicastAddress })

  static member MulticastPort_ =
    (fun (machine:IrisMachine) -> machine.MulticastPort),
    (fun multicastPort (machine:IrisMachine) -> { machine with MulticastPort = multicastPort })

  static member BindAddress_ =
    (fun (machine:IrisMachine) -> machine.BindAddress),
    (fun bindAddress (machine:IrisMachine) -> { machine with BindAddress = bindAddress })

  static member WebPort_ =
    (fun (machine:IrisMachine) -> machine.WebPort),
    (fun webPort (machine:IrisMachine) -> { machine with WebPort = webPort })

  static member RaftPort_ =
    (fun (machine:IrisMachine) -> machine.RaftPort),
    (fun raftPort (machine:IrisMachine) -> { machine with RaftPort = raftPort })

  static member WsPort_ =
    (fun (machine:IrisMachine) -> machine.WsPort),
    (fun wsPort (machine:IrisMachine) -> { machine with WsPort = wsPort })

  static member GitPort_ =
    (fun (machine:IrisMachine) -> machine.GitPort),
    (fun gitPort (machine:IrisMachine) -> { machine with GitPort = gitPort })

  static member ApiPort_ =
    (fun (machine:IrisMachine) -> machine.ApiPort),
    (fun apiPort (machine:IrisMachine) -> { machine with ApiPort = apiPort })

  static member Version_ =
    (fun (machine:IrisMachine) -> machine.Version),
    (fun version (machine:IrisMachine) -> { machine with Version = version })

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
    let machineid = IrisMachineFB.CreateMachineIdVector(builder, machine.MachineId.ToByteArray())
    let version = machine.Version |> unwrap |> mapNull
    IrisMachineFB.StartIrisMachineFB(builder)
    IrisMachineFB.AddMachineId(builder, machineid)
    Option.iter (fun value -> IrisMachineFB.AddHostName(builder, value)) hostname
    Option.iter (fun value -> IrisMachineFB.AddWorkSpace(builder, value)) workspace
    Option.iter (fun value -> IrisMachineFB.AddLogDirectory(builder, value)) logdir
    Option.iter (fun value -> IrisMachineFB.AddAssetDirectory(builder, value)) assetdir
    Option.iter (fun value -> IrisMachineFB.AddAssetFilter(builder, value)) assetFilter
    IrisMachineFB.AddMulticastAddress(builder, mcastip)
    IrisMachineFB.AddMulticastPort(builder, unwrap machine.MulticastPort)
    IrisMachineFB.AddBindAddress(builder, webip)
    IrisMachineFB.AddWebPort(builder, unwrap machine.WebPort)
    IrisMachineFB.AddRaftPort(builder, unwrap machine.RaftPort)
    IrisMachineFB.AddWsPort(builder, unwrap machine.WsPort)
    IrisMachineFB.AddGitPort(builder, unwrap machine.GitPort)
    IrisMachineFB.AddApiPort(builder, unwrap machine.ApiPort)
    Option.iter (fun value ->IrisMachineFB.AddVersion(builder, value)) version
    IrisMachineFB.EndIrisMachineFB(builder)

  // ** FromFB

  static member FromFB (fb: IrisMachineFB) =
    either {
      let! machineId = Id.decodeMachineId fb
      let! ip = IpAddress.TryParse fb.BindAddress
      let! mcastip = IpAddress.TryParse fb.MulticastAddress
      return {
        MachineId        = machineId
        WorkSpace        = filepath fb.WorkSpace
        AssetDirectory   = filepath fb.AssetDirectory
        AssetFilter      = fb.AssetFilter
        LogDirectory     = filepath fb.LogDirectory
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

  // ** Default

  static member Default
    with get () =
      { MachineId        = IrisId.Create()
        HostName         = name "<empty>"
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
      | x when x = MachineStatusEnumFB.IdleFB -> Either.succeed Idle
      | x when x = MachineStatusEnumFB.BusyFB ->
        either {
          let! id = Id.decodeProjectId fb
          return Busy (id, name fb.ProjectName)
        }
      | other ->
        sprintf "Unknown Machine Status: %d" other
        |> Error.asParseError "MachineStatus.FromOffset"
        |> Either.fail
      #else
      match fb.Status with
      | MachineStatusEnumFB.IdleFB -> Either.succeed Idle
      | MachineStatusEnumFB.BusyFB ->
        either {
          let! id = Id.decodeProjectId fb
          return Busy (id, name fb.ProjectName)
        }
      | other ->
        sprintf "Unknown Machine Status: %O" other
        |> Error.asParseError "MachineStatus.FromOffset"
        |> Either.fail
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

  // ** tag

  let private tag (str: string) = sprintf "MachineConfig.%s" str

  // ** getters

  let machineId = Optic.get IrisMachine.MachineId_
  let hostName = Optic.get IrisMachine.HostName_
  let workSpace = Optic.get IrisMachine.WorkSpace_
  let assetDirectory = Optic.get IrisMachine.AssetDirectory_
  let assetFilter = Optic.get IrisMachine.AssetFilter_
  let logDirectory = Optic.get IrisMachine.LogDirectory_
  let bindAddress = Optic.get IrisMachine.BindAddress_
  let multicastAddress = Optic.get IrisMachine.MulticastAddress_
  let multicastPort = Optic.get IrisMachine.MulticastPort_
  let webPort = Optic.get IrisMachine.WebPort_
  let raftPort = Optic.get IrisMachine.RaftPort_
  let wsPort = Optic.get IrisMachine.WsPort_
  let gitPort = Optic.get IrisMachine.GitPort_
  let apiPort = Optic.get IrisMachine.ApiPort_
  let version = Optic.get IrisMachine.Version_

  // ** setters

  let setMachineId = Optic.set IrisMachine.MachineId_
  let setHostName = Optic.set IrisMachine.HostName_
  let setWorkSpace = Optic.set IrisMachine.WorkSpace_
  let setAssetDirectory = Optic.set IrisMachine.AssetDirectory_
  let setAssetFilter = Optic.set IrisMachine.AssetFilter_
  let setLogDirectory = Optic.set IrisMachine.LogDirectory_
  let setBindAddress = Optic.set IrisMachine.BindAddress_
  let setMulticastAddress = Optic.set IrisMachine.MulticastAddress_
  let setMulticastPort = Optic.set IrisMachine.MulticastPort_
  let setWebPort = Optic.set IrisMachine.WebPort_
  let setRaftPort = Optic.set IrisMachine.RaftPort_
  let setWsPort = Optic.set IrisMachine.WsPort_
  let setGitPort = Optic.set IrisMachine.GitPort_
  let setApiPort = Optic.set IrisMachine.ApiPort_
  let setVersion = Optic.set IrisMachine.Version_

  // ** singleton

  let mutable private singleton = Unchecked.defaultof<IrisMachine>

  // ** get

  let get() = singleton

  #if !FABLE_COMPILER && !IRIS_NODES

  // ** getLocation

  let getLocation (path: FilePath option) =
    match path with
    | Some location ->
      if Path.endsWith ASSET_EXTENSION location then
        location
      else
        location </> filepath (MACHINECONFIG_NAME + ASSET_EXTENSION)
    | None ->
      Assembly.GetExecutingAssembly().Location
      |> Path.GetDirectoryName
      <.> MACHINECONFIG_DEFAULT_PATH
      </> filepath (MACHINECONFIG_NAME + ASSET_EXTENSION)

  open SharpYaml.Serialization

  // ** MachineConfigYaml (private)

  type MachineConfigYaml () =
    [<DefaultValue>] val mutable MachineId:        string
    [<DefaultValue>] val mutable WorkSpace:        string
    [<DefaultValue>] val mutable AssetDirectory:   string
    [<DefaultValue>] val mutable AssetFilter:      string
    [<DefaultValue>] val mutable LogDirectory:     string
    [<DefaultValue>] val mutable BindAddress:      string
    [<DefaultValue>] val mutable MulticastAddress: string
    [<DefaultValue>] val mutable MulticastPort:    uint16
    [<DefaultValue>] val mutable WebPort:          uint16
    [<DefaultValue>] val mutable RaftPort:         uint16
    [<DefaultValue>] val mutable WsPort:           uint16
    [<DefaultValue>] val mutable GitPort:          uint16
    [<DefaultValue>] val mutable ApiPort:          uint16
    [<DefaultValue>] val mutable Version:          string

    static member Create (cfg: IrisMachine) =
      let yml = MachineConfigYaml()
      yml.MachineId        <- string cfg.MachineId
      yml.WorkSpace        <- unwrap cfg.WorkSpace
      yml.AssetDirectory   <- unwrap cfg.AssetDirectory
      yml.AssetFilter      <- cfg.AssetFilter
      yml.LogDirectory     <- unwrap cfg.LogDirectory
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

  // ** parse (private)

  let private parse (yml: MachineConfigYaml) : Either<IrisError,IrisMachine> =
    either {
      let hostname = Network.getHostName ()
      let! ip = IpAddress.TryParse yml.BindAddress
      let! id = IrisId.TryParse yml.MachineId
      let! mcastip = IpAddress.TryParse yml.MulticastAddress
      return {
        MachineId        = id
        HostName         = name hostname
        WorkSpace        = filepath yml.WorkSpace
        AssetDirectory   = filepath yml.AssetDirectory
        AssetFilter      = yml.AssetFilter
        LogDirectory     = filepath yml.LogDirectory
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

  // ** create

  let create (bindIp: string) (shiftDefaults: uint16 option) : IrisMachine =
    let shiftPath path =
        match shiftDefaults with
        | Some shift -> path + (string shift)
        | None -> path
    let shiftPort p =
        match shiftDefaults with
        | Some shift -> port (p + shift)
        | None -> port p
    let hostname = Network.getHostName()

    let workspace =
      if Platform.isUnix then
        let home = Environment.GetEnvironmentVariable "HOME"
        home <.> (shiftPath MACHINECONFIG_DEFAULT_WORKSPACE_UNIX)
      else
        filepath (shiftPath MACHINECONFIG_DEFAULT_WORKSPACE_WINDOWS)

    let assetDir =
      if Platform.isUnix then
        let home = Environment.GetEnvironmentVariable "HOME"
        home <.> (shiftPath MACHINECONFIG_DEFAULT_ASSET_DIRECTORY_UNIX)
      else
        filepath (shiftPath MACHINECONFIG_DEFAULT_ASSET_DIRECTORY_WINDOWS)

    if Directory.exists workspace |> not then
      Directory.createDirectory workspace |> ignore

    let version = Assembly.GetExecutingAssembly().GetName().Version |> string |> Measure.version

    { MachineId        = IrisId.Create()
      HostName         = name hostname
      WorkSpace        = workspace
      AssetDirectory   = assetDir
      AssetFilter      = Constants.DEFAULT_ASSET_FILTER
      LogDirectory     = workspace </> filepath "log"
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

  let save (path: FilePath option) (cfg: IrisMachine) : Either<IrisError,unit> =
    let serializer = Serializer()

    try
      let location = getLocation path

      let payload =
        cfg
        |> MachineConfigYaml.Create
        |> serializer.Serialize

      location
      |> unwrap
      |> Path.GetDirectoryName
      |> filepath
      |> ensureExists

      File.WriteAllText(unwrap location, payload)
      |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "save")
        |> Either.fail

  // ** init

  /// Attention: this method must be called only when starting the main process
  let init getBindIp shiftDefaults (path: FilePath option) : Either<IrisError,unit> =
    let serializer = Serializer()
    try
      let location = getLocation path
      let cfg =
        if File.exists location
        then
          let raw = File.ReadAllText(unwrap location)
          serializer.Deserialize<MachineConfigYaml>(raw)
          |> parse
        else
          let bindIp = getBindIp()
          let cfg = create bindIp shiftDefaults
          save path cfg
          |> Either.map (fun _ -> cfg)

      match cfg with
      | Left err -> Either.fail err
      | Right cfg ->
        if Path.IsPathRooted (unwrap cfg.WorkSpace)
        then singleton <- cfg
        else
          let wp =
            unwrap location
            |> Path.GetDirectoryName
            |> filepath
            </> cfg.WorkSpace
          singleton <- { cfg with WorkSpace = wp }
        Either.succeed()
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "load")
        |> Either.fail

  #endif


  // ** validate

  let validate (config: IrisMachine) =
    let inline check (o: obj) = o |> isNull |> not
    [ ("LogDirectory", check config.LogDirectory)
      ("WorkSpace",    check config.WorkSpace)
      ("MachineId",    check config.MachineId)
      ("BindAddress",  check config.BindAddress) ]
    |> List.fold
        (fun m (name,result) ->
          if not result then
            Map.add name result m
          else m)
        Map.empty
