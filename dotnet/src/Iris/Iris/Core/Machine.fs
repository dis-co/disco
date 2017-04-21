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
    WebIP     : string
    WebPort   : uint16
    RaftPort  : uint16
    WsPort    : uint16
    GitPort   : uint16
    ApiPort   : uint16
    Version   : Version }

  override self.ToString() =
    sprintf "MachineId: %s" (string self.MachineId)

// * MachineConfig module

[<RequireQualifiedAccess>]
module MachineConfig =
  open Path

  let private tag (str: string) = sprintf "MachineConfig.%s" str

  let mutable private singleton = Unchecked.defaultof<IrisMachine>

  let get() = singleton

  #if !FABLE_COMPILER

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
      Version   = Version.Parse yml.Version }
    |> Either.succeed

  // ** ensureExists (private)

  let private ensureExists (path: FilePath) =
    try
      if not (Directory.directoryExists path) then
        Directory.createDirectory path |> ignore
    with
      | _ -> ()

  // ** create

  let create () : IrisMachine =
    let hostname = Network.getHostName()
    let workspace =
      if Platform.isUnix then
        let home = Environment.GetEnvironmentVariable "HOME"
        home <.> MACHINECONFIG_DEFAULT_WORKSPACE_UNIX
      else
        filepath MACHINECONFIG_DEFAULT_WORKSPACE_WINDOWS

    if Directory.directoryExists workspace |> not then
      Directory.createDirectory workspace |> ignore

    { MachineId = Id.Create()
      HostName  = hostname
      WorkSpace = workspace
      WebIP     = Constants.DEFAULT_IP
      WebPort   = Constants.DEFAULT_WEB_PORT
      RaftPort  = Constants.DEFAULT_RAFT_PORT
      WsPort    = Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort   = Constants.DEFAULT_GIT_PORT
      ApiPort   = Constants.DEFAULT_API_PORT
      Version   = Assembly.GetExecutingAssembly().GetName().Version }

  // ** save

  let save (path: FilePath option) (cfg: IrisMachine) : Either<IrisError,unit> =
    let serializer = new Serializer()

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
  let init (path: FilePath option) : Either<IrisError,unit> =
    let serializer = new Serializer()
    try
      let location = getLocation path
      let cfg =
        if File.fileExists location
        then
          let raw = File.ReadAllText(unwrap location)
          serializer.Deserialize<MachineConfigYaml>(raw)
          |> parse
        else
          let cfg = create()
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
