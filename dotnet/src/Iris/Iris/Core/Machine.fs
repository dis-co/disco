namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.IO
open SharpYaml.Serialization
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
  { MachineId : Id
    HostName  : string
    WorkSpace : FilePath
    /// In spite of its name, other services should bind
    /// to this IP too, not only the HTTP server
    WebIP     : string
    WebPort   : uint16
    RaftPort  : uint16
    WsPort    : uint16
    GitPort   : uint16
    ApiPort   : uint16
    Version   : Iris.Core.Version }

  // ** ToString

  override self.ToString() =
    sprintf "MachineId: %s" (string self.MachineId)

  // ** ToOffset

  member machine.ToOffset(builder: FlatBufferBuilder) =
    let webip = machine.WebIP |> string |> builder.CreateString
    let workspace = machine.WorkSpace |> string |> builder.CreateString
    let hostname = machine.HostName |> string |> builder.CreateString
    let machineid = machine.MachineId |> string |> builder.CreateString
    let version = machine.Version |> unwrap |> builder.CreateString
    IrisMachineFB.StartIrisMachineFB(builder)
    IrisMachineFB.AddMachineId(builder, machineid)
    IrisMachineFB.AddHostName(builder, hostname)
    IrisMachineFB.AddWorkSpace(builder, workspace)
    IrisMachineFB.AddWebIP(builder, webip)
    IrisMachineFB.AddWebPort(builder, machine.WebPort)
    IrisMachineFB.AddRaftPort(builder, machine.RaftPort)
    IrisMachineFB.AddWsPort(builder, machine.WsPort)
    IrisMachineFB.AddGitPort(builder, machine.GitPort)
    IrisMachineFB.AddApiPort(builder, machine.ApiPort)
    IrisMachineFB.AddVersion(builder, version)
    IrisMachineFB.EndIrisMachineFB(builder)

  // ** FromFB

  static member FromFB (fb: IrisMachineFB) =
    { MachineId = Id fb.MachineId
      WorkSpace = filepath fb.WorkSpace
      HostName = fb.HostName
      WebIP = fb.WebIP
      WebPort = fb.WebPort
      RaftPort = fb.RaftPort
      WsPort = fb.WsPort
      GitPort = fb.GitPort
      ApiPort = fb.ApiPort
      Version = version fb.Version }
    |> Either.succeed

  // ** Default

  static member Default
    with get () =
      { MachineId = Id "<empty>"
        HostName  = "<empty>"
        WorkSpace = filepath "/dev/null"
        WebIP     = "127.0.0.1"
        WebPort   = Constants.DEFAULT_WEB_PORT
        RaftPort  = Constants.DEFAULT_RAFT_PORT
        WsPort    = Constants.DEFAULT_WEB_SOCKET_PORT
        GitPort   = Constants.DEFAULT_GIT_PORT
        ApiPort   = Constants.DEFAULT_API_PORT
        Version   = version Build.VERSION }

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
    | Busy of ProjectId:Id * ProjectName:Name

    // *** ToString

    override status.ToString() =
      match status with
      | Idle   -> IDLE
      | Busy _ -> BUSY

    // *** ToOffset

    member status.ToOffset(builder: FlatBufferBuilder) =
      match status with
      | Idle ->
        MachineStatusFB.StartMachineStatusFB(builder)
        MachineStatusFB.AddStatus(builder, MachineStatusEnumFB.IdleFB)
        MachineStatusFB.EndMachineStatusFB(builder)
      | Busy (id, name) ->
        let idoff = id |> string |> builder.CreateString
        let nameoff = name |> unwrap |> builder.CreateString
        MachineStatusFB.StartMachineStatusFB(builder)
        MachineStatusFB.AddStatus(builder, MachineStatusEnumFB.BusyFB)
        MachineStatusFB.AddProjectId(builder, idoff)
        MachineStatusFB.AddProjectName(builder, nameoff)
        MachineStatusFB.EndMachineStatusFB(builder)

    // *** FromOffset

    static member FromFB(fb: MachineStatusFB) =
      #if FABLE_COMPILER
      match fb.Status with
      | x when x = MachineStatusEnumFB.IdleFB -> Either.succeed Idle
      | x when x = MachineStatusEnumFB.BusyFB ->
        Busy (Id fb.ProjectId, name fb.ProjectName)
        |> Either.succeed
      | other ->
        sprintf "Unknown Machine Status: %d" other
        |> Error.asParseError "MachineStatus.FromOffset"
        |> Either.fail
      #else
      match fb.Status with
      | MachineStatusEnumFB.IdleFB -> Either.succeed Idle
      | MachineStatusEnumFB.BusyFB ->
        Busy (Id fb.ProjectId, name fb.ProjectName)
        |> Either.succeed
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

  // ** tag

  let private tag (str: string) = sprintf "MachineConfig.%s" str

  // ** singleton

  let mutable private singleton = Unchecked.defaultof<IrisMachine>

  // ** get

  let get() = singleton

  #if !FABLE_COMPILER

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

  // ** MachineConfigYaml (private)

  type MachineConfigYaml () =
    [<DefaultValue>] val mutable MachineId : string
    [<DefaultValue>] val mutable WorkSpace : string
    [<DefaultValue>] val mutable WebIP     : string
    [<DefaultValue>] val mutable WebPort   : uint16
    [<DefaultValue>] val mutable RaftPort  : uint16
    [<DefaultValue>] val mutable WsPort    : uint16
    [<DefaultValue>] val mutable GitPort   : uint16
    [<DefaultValue>] val mutable ApiPort   : uint16
    [<DefaultValue>] val mutable Version   : string

    static member Create (cfg: IrisMachine) =
      let yml = new MachineConfigYaml()
      yml.MachineId <- string cfg.MachineId
      yml.WorkSpace <- unwrap cfg.WorkSpace
      yml.WebIP     <- cfg.WebIP
      yml.WebPort   <- cfg.WebPort
      yml.RaftPort  <- cfg.RaftPort
      yml.WsPort    <- cfg.WsPort
      yml.GitPort   <- cfg.GitPort
      yml.ApiPort   <- cfg.ApiPort
      yml.Version   <- cfg.Version.ToString()
      yml

  // ** parse (private)

  let private parse (yml: MachineConfigYaml) : Either<IrisError,IrisMachine> =
    let hostname = Network.getHostName ()
    { MachineId = Id yml.MachineId
      HostName  = hostname
      WorkSpace = filepath yml.WorkSpace
      WebIP     = yml.WebIP
      WebPort   = yml.WebPort
      RaftPort  = yml.RaftPort
      WsPort    = yml.WsPort
      GitPort   = yml.GitPort
      ApiPort   = yml.ApiPort
      Version   = version yml.Version }
    |> Either.succeed

  // ** ensureExists (private)

  let private ensureExists (path: FilePath) =
    try
      if not (Directory.exists path) then
        Directory.createDirectory path |> ignore
    with
      | _ -> ()

  // ** create

  let create bindIp (shiftDefaults: uint16 option) : IrisMachine =
    let shiftPath path =
        match shiftDefaults with
        | Some shift -> path + (string shift)
        | None -> path
    let shiftPort port =
        match shiftDefaults with
        | Some shift -> port + shift
        | None -> port
    let hostname = Network.getHostName()
    let workspace =
      if Platform.isUnix then
        let home = Environment.GetEnvironmentVariable "HOME"
        home <.> (shiftPath MACHINECONFIG_DEFAULT_WORKSPACE_UNIX)
      else
        filepath (shiftPath MACHINECONFIG_DEFAULT_WORKSPACE_WINDOWS)

    if Directory.exists workspace |> not then
      Directory.createDirectory workspace |> ignore

    let version = Assembly.GetExecutingAssembly().GetName().Version |> string |> version

    { MachineId = Id.Create()
      HostName  = hostname
      WorkSpace = workspace
      WebIP     = bindIp
      WebPort   = shiftPort Constants.DEFAULT_WEB_PORT
      RaftPort  = shiftPort Constants.DEFAULT_RAFT_PORT
      WsPort    = shiftPort Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort   = shiftPort Constants.DEFAULT_GIT_PORT
      ApiPort   = shiftPort Constants.DEFAULT_API_PORT
      Version   = version }

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
