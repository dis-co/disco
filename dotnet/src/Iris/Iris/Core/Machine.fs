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
    WorkSpace : FilePath }

  // ** Default

  static member Default
    with get () =
      { MachineId = Id "<empty>"
        HostName  = ""
        WorkSpace = "" }

  override self.ToString() =
    sprintf "MachineId: %s" (string self.MachineId)

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.MachineId)
    let hn = builder.CreateString self.HostName
    let wsp = builder.CreateString self.WorkSpace

    MachineConfigFB.StartMachineConfigFB(builder)
    MachineConfigFB.AddMachineId(builder,id)
    MachineConfigFB.AddHostName(builder, hn)
    MachineConfigFB.AddWorkSpace(builder,wsp)
    MachineConfigFB.EndMachineConfigFB(builder)

  static member FromFB(fb: MachineConfigFB) =
    either {
      return
        { MachineId = Id fb.MachineId
          HostName  = fb.HostName
          WorkSpace = fb.WorkSpace }
    }

// * MachineConfig module

[<RequireQualifiedAccess>]
module MachineConfig =

  let private tag (str: string) = sprintf "MachineConfig.%s" str

  // ** MachineConfigYaml (private)

  #if !FABLE_COMPILER

  type MachineConfigYaml () =
    [<DefaultValue>] val mutable MachineId : string
    [<DefaultValue>] val mutable WorkSpace : string

    static member Create (cfg: IrisMachine) =
      let yml = new MachineConfigYaml()
      yml.MachineId <- string cfg.MachineId
      yml.WorkSpace <- cfg.WorkSpace
      yml

  #endif

  // ** parse (private)

  #if !FABLE_COMPILER

  let private parse (yml: MachineConfigYaml) : Either<IrisError,IrisMachine> =
    let hostname = Network.getHostName ()
    { MachineId = Id yml.MachineId
      HostName  = hostname
      WorkSpace = yml.WorkSpace }
    |> Either.succeed

  #endif

  // ** ensureExists (private)

  #if !FABLE_COMPILER

  let private ensureExists (path: FilePath) =
    try
      if not (Directory.Exists path) then
        Directory.CreateDirectory path
        |> ignore
    with
      | _ -> ()

  #endif

  // ** defaultPath

  #if !FABLE_COMPILER

  let defaultPath =
    let dir =
      Assembly.GetExecutingAssembly().Location
      |> Path.GetDirectoryName
    dir </> MACHINECONFIG_DEFAULT_PATH </> MACHINECONFIG_NAME + ASSET_EXTENSION

  #endif

  // ** create

  let create () : IrisMachine =
    let hostname = Network.getHostName()
    let workspace =
      #if FABLE_COMPILER
        ""
      #else
      if Platform.isUnix then
        let home = Environment.GetEnvironmentVariable "HOME"
        home </> "iris"
      else
        @"C:\Iris"
      #endif

    { MachineId = Id.Create()
      HostName  = hostname
      WorkSpace = workspace }

  // ** save

  #if !FABLE_COMPILER

  let save (path: FilePath option) (cfg: IrisMachine) : Either<IrisError,unit> =
    let serializer = new Serializer()

    try
      let location =
        match path with
        | Some location -> location
        | None -> defaultPath

      let payload=
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

  #endif

  // ** load

  #if !FABLE_COMPILER

  let load (path: FilePath option) : Either<IrisError,IrisMachine> =
    let serializer = new Serializer()
    try
      let location =
        match path with
        | Some location -> location
        | None -> defaultPath

      let raw = File.ReadAllText location
      serializer.Deserialize<MachineConfigYaml>(raw)
      |> parse
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "load")
        |> Either.fail

  #endif
