namespace rec Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization

#endif

// * CuePlayerYaml

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

type CuePlayerYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Locked: bool
  [<DefaultValue>] val mutable CueList: string
  [<DefaultValue>] val mutable Selected: int
  [<DefaultValue>] val mutable Call: string
  [<DefaultValue>] val mutable Next: string
  [<DefaultValue>] val mutable Previous: string
  [<DefaultValue>] val mutable RemainingWait: int
  [<DefaultValue>] val mutable LastCalled: string
  [<DefaultValue>] val mutable LastCaller: string

  // ** From

  static member From(player: CuePlayer) =
    let yaml = CuePlayerYaml()
    let opt2str opt =
      match opt with
      | Some thing -> string thing
      | None -> null
    yaml.Id <- string player.Id
    yaml.Name <- unwrap player.Name
    yaml.Locked <- player.Locked
    yaml.CueList <- opt2str player.CueList
    yaml.Selected <- int player.Selected
    yaml.Call <- string player.Call
    yaml.Next <- string player.Next
    yaml.Previous <- string player.Previous
    yaml.RemainingWait <- player.RemainingWait
    yaml.LastCaller <- opt2str player.LastCaller
    yaml.LastCalled <- opt2str player.LastCalled
    yaml

  // ** ToPlayer

  member yaml.ToPlayer() =
    either {
      let str2opt str =
        match str with
        | null -> None
        | thing -> Some (Id thing)
      return { Id = Id yaml.Id
               Name = name yaml.Name
               Locked = yaml.Locked
               CueList = str2opt yaml.CueList
               Selected = index yaml.Selected
               Call = Id yaml.Call
               Next = Id yaml.Next
               Previous = Id yaml.Previous
               RemainingWait = yaml.RemainingWait
               LastCaller = str2opt yaml.LastCaller
               LastCalled = str2opt yaml.LastCalled }
    }

#endif

// * CuePlayer

type CuePlayer =
  { Id: Id
    Name: Name
    Locked: bool
    CueList: Id option
    Selected: int<index>
    Call: Id                           // should be Bang pin type
    Next: Id                           // should be Bang pin type
    Previous: Id                       // should be Bang pin type
    RemainingWait: int
    LastCalled: Id option
    LastCaller: Id option }

  // ** ToOffset

  member player.ToOffset(builder: FlatBufferBuilder) =
    let id = player.Id |> string |> builder.CreateString
    let name = player.Name |> unwrap |> Option.mapNull builder.CreateString
    let cuelist = player.CueList |> Option.map (string >> builder.CreateString)
    let call = player.Call |> string |> builder.CreateString
    let next = player.Next |> string |> builder.CreateString
    let previous = player.Previous |> string |> builder.CreateString
    let lastcalled = player.LastCalled |> Option.map (string >> builder.CreateString)
    let lastcaller = player.LastCaller |> Option.map (string >> builder.CreateString)

    CuePlayerFB.StartCuePlayerFB(builder)
    CuePlayerFB.AddId(builder, id)
    Option.iter (fun value -> CuePlayerFB.AddName(builder,value)) name
    CuePlayerFB.AddLocked(builder, player.Locked)
    CuePlayerFB.AddSelected(builder, int player.Selected)
    CuePlayerFB.AddRemainingWait(builder, player.RemainingWait)
    CuePlayerFB.AddCall(builder, call)
    CuePlayerFB.AddNext(builder, next)
    CuePlayerFB.AddPrevious(builder, previous)
    Option.iter (fun cl -> CuePlayerFB.AddCueList(builder, cl)) cuelist
    Option.iter (fun cl -> CuePlayerFB.AddLastCalled(builder, cl)) lastcalled
    Option.iter (fun cl -> CuePlayerFB.AddLastCaller(builder, cl)) lastcaller
    CuePlayerFB.EndCuePlayerFB(builder)

  // ** FromFB

  static member FromFB(fb: CuePlayerFB) =
    either {
      let cuelist =
        if isNull fb.CueList then
          None
         else Some (Id fb.CueList)

      let lastcalled =
        if isNull fb.LastCalled then
          None
        else Some (Id fb.LastCalled)

      let lastcaller =
        if isNull fb.LastCaller then
          None
        else Some (Id fb.LastCaller)

      return { Id = Id fb.Id
               Name = name fb.Name
               Locked = fb.Locked
               CueList = cuelist
               Selected = index fb.Selected
               Call = Id fb.Call
               Next = Id fb.Next
               Previous = Id fb.Previous
               RemainingWait = fb.RemainingWait
               LastCalled = lastcalled
               LastCaller = lastcaller }
    }

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,CuePlayer> =
    Binary.createBuffer bytes
    |> CuePlayerFB.GetRootAsCuePlayerFB
    |> CuePlayer.FromFB

  // ** ToYamlObject

  #if !FABLE_COMPILER && !IRIS_NODES

  member player.ToYamlObject() = CuePlayerYaml.From(player)

  // ** FromYamlObject

  static member FromYamlObject(yaml: CuePlayerYaml) = yaml.ToPlayer()

  // ** FromYaml

  static member FromYaml(raw: string) : Either<IrisError,CuePlayer> =
    let serializer = Serializer()
    serializer.Deserialize<CuePlayerYaml>(raw)
    |> Yaml.fromYaml

  // ** ToYaml

  member player.ToYaml(serializer: Serializer) =
    player
    |> Yaml.toYaml
    |> serializer.Serialize

  #endif

  // ** HasParent

  /// CuePlayers don't live in nested directories, hence false
  member player.HasParent with get () = false

  // ** Load

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load(path: FilePath) : Either<IrisError,CuePlayer> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError,CuePlayer array> =
    basePath </> filepath Constants.CUEPLAYER_DIR
    |> IrisData.loadAll

  // ** Save

  member player.Save (basePath: FilePath) =
    IrisData.save basePath player

  // ** Delete

  member player.Delete (basePath: FilePath) =
    IrisData.delete basePath player

  #endif

  // ** AssetPath

  member player.AssetPath
    with get () =
      CuePlayer.assetPath player

// * CuePlayer module

module CuePlayer =

  open NameUtils

  // ** create

  let create (playerName: Name) (cuelist: Id option) =
    let id = Id.Create()
    { Id            = id
      Name          = playerName
      Locked        = false
      CueList       = cuelist
      Selected      = -1<index>
      Call          = Pin.Player.callId     id
      Next          = Pin.Player.nextId     id
      Previous      = Pin.Player.previousId id
      RemainingWait = -1
      LastCalled    = None
      LastCaller    = None }

  // ** assetPath

  let assetPath (player: CuePlayer) =
    CUEPLAYER_DIR <.> sprintf "%O%s" player.Id ASSET_EXTENSION
