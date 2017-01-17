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
open Iris.Serialization.Raft
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
    WebPort   : uint16 }

  override self.ToString() =
    sprintf "MachineId: %s" (string self.MachineId)

// * MachineConfig module

[<RequireQualifiedAccess>]
module MachineConfig =

  let private tag (str: string) = sprintf "MachineConfig.%s" str

  let mutable private singleton = Unchecked.defaultof<IrisMachine>

  let get() = singleton

  #if !FABLE_COMPILER

  let getLocation (path: FilePath option) =
    match path with
    | Some location ->
      if location.EndsWith(ASSET_EXTENSION)
      then location
      else location </> MACHINECONFIG_NAME + ASSET_EXTENSION
    | None ->
      let dir =
        Assembly.GetExecutingAssembly().Location
        |> Path.GetDirectoryName
      dir </> MACHINECONFIG_DEFAULT_PATH </> MACHINECONFIG_NAME + ASSET_EXTENSION

  // ** MachineConfigYaml (private)

  type MachineConfigYaml () =
    [<DefaultValue>] val mutable MachineId : string
    [<DefaultValue>] val mutable WorkSpace : string
    [<DefaultValue>] val mutable WebIP     : string
    [<DefaultValue>] val mutable WebPort   : uint16

    static member Create (cfg: IrisMachine) =
      let yml = new MachineConfigYaml()
      yml.MachineId <- string cfg.MachineId
      yml.WorkSpace <- cfg.WorkSpace
      yml.WebIP     <- cfg.WebIP
      yml.WebPort   <- cfg.WebPort
      yml

  // ** parse (private)

  let private parse (yml: MachineConfigYaml) : Either<IrisError,IrisMachine> =
    let hostname = Network.getHostName ()
    { MachineId = Id yml.MachineId
      HostName  = hostname
      WorkSpace = yml.WorkSpace
      WebIP     = yml.WebIP
      WebPort   = yml.WebPort }
    |> Either.succeed

  // ** ensureExists (private)

  let private ensureExists (path: FilePath) =
    try
      if not (Directory.Exists path) then
        Directory.CreateDirectory path
        |> ignore
    with
      | _ -> ()

  // ** create

  let create () : IrisMachine =
    let hostname = Network.getHostName()
    let workspace =
      if Platform.isUnix then
        let home = Environment.GetEnvironmentVariable "HOME"
        home </> MACHINECONFIG_DEFAULT_WORKSPACE_UNIX
      else
        MACHINECONFIG_DEFAULT_WORKSPACE_WINDOWS

    if Directory.Exists workspace |> not then
      Directory.CreateDirectory workspace |> ignore

    { MachineId = Id.Create()
      HostName  = hostname
      WorkSpace = workspace
      WebIP     = Constants.DEFAULT_IP
      WebPort   = Constants.DEFAULT_WEB_PORT }

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
      |> Path.GetDirectoryName
      |> ensureExists

      File.WriteAllText(location, payload)
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
        if File.Exists location
        then
          let raw = File.ReadAllText location
          serializer.Deserialize<MachineConfigYaml>(raw)
          |> parse
        else
          let cfg = create()
          save path cfg
          |> Either.map (fun _ -> cfg)

      match cfg with
      | Left err -> Either.fail err
      | Right cfg ->
        if Path.IsPathRooted cfg.WorkSpace
        then singleton <- cfg
        else singleton <- { cfg with WorkSpace = Path.GetDirectoryName location </> cfg.WorkSpace }
        Either.succeed()
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "load")
        |> Either.fail

  #endif
