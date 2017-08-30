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
  [<DefaultValue>] val mutable Call: PinYaml
  [<DefaultValue>] val mutable Next: PinYaml
  [<DefaultValue>] val mutable Previous: PinYaml
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
    yaml.Call <- Yaml.toYaml player.Call
    yaml.Next <- Yaml.toYaml player.Next
    yaml.Previous <- Yaml.toYaml player.Previous
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

      let! call = Yaml.fromYaml yaml.Call
      let! next = Yaml.fromYaml yaml.Next
      let! previous = Yaml.fromYaml yaml.Previous
      return { Id = Id yaml.Id
               Name = name yaml.Name
               Locked = yaml.Locked
               CueList = str2opt yaml.CueList
               Selected = index yaml.Selected
               Call = call
               Next = next
               Previous = previous
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
    Call: Pin                           // Bang pin type
    Next: Pin                           // Bang pin type
    Previous: Pin                       // Bang pin type
    RemainingWait: int
    LastCalled: Id option
    LastCaller: Id option }

  // ** ToOffset

  member player.ToOffset(builder: FlatBufferBuilder) =
    let id = player.Id |> string |> builder.CreateString
    let name = player.Name |> unwrap |> Option.mapNull builder.CreateString
    let cuelist = player.CueList |> Option.map (string >> builder.CreateString)
    let call = Binary.toOffset builder player.Call
    let next = Binary.toOffset builder player.Next
    let previous = Binary.toOffset builder player.Previous
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

      let! call =
        #if FABLE_COMPILER
        Pin.FromFB fb.Call
        #else
        let callish = fb.Call
        if callish.HasValue then
          callish.Value
          |> Pin.FromFB
        else
          "Could not parse empty Call field"
          |> Error.asParseError "CuePlayer"
          |> Either.fail
        #endif

      let! next =
        #if FABLE_COMPILER
        Pin.FromFB fb.Next
        #else
        let nextish = fb.Next
        if nextish.HasValue then
          nextish.Value
          |> Pin.FromFB
        else
          "Could not parse empty Next field"
          |> Error.asParseError "CuePlayer"
          |> Either.fail
        #endif

      let! previous =
        #if FABLE_COMPILER
        Pin.FromFB fb.Previous
        #else
        let previousish = fb.Previous
        if previousish.HasValue then
          previousish.Value
          |> Pin.FromFB
        else
          "Could not parse empty Previous field"
          |> Error.asParseError "CuePlayer"
          |> Either.fail
        #endif

      return { Id = Id fb.Id
               Name = name fb.Name
               Locked = fb.Locked
               CueList = cuelist
               Selected = index fb.Selected
               Call = call
               Next = next
               Previous = previous
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

  // ** Load

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load(path: FilePath) : Either<IrisError,CuePlayer> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError,CuePlayer array> =
    basePath </> filepath Constants.CUEPLAYER_DIR
    |> IrisData.loadAll

  // ** Save

  member player.Save(basePath: FilePath) =
    IrisData.save basePath player

  #endif

  // ** AssetPath

  member player.AssetPath
    with get () =
      CuePlayer.assetPath player

// * CuePlayer module

module CuePlayer =

  // ** create

  let create (name: Name) (cuelist: Id option) =
    let id = Id.Create()
    { Id = id
      Name = name
      Locked = false
      CueList = cuelist
      Selected = -1<index>
      Call = Pin.Player.call id
      Next = Pin.Player.next id
      Previous = Pin.Player.previous id
      RemainingWait = -1
      LastCalled = None
      LastCaller = None }

  // ** assetPath

  let assetPath (player: CuePlayer) =
    CUEPLAYER_DIR <.> sprintf "%s%s" (string player.Id) ASSET_EXTENSION

  // ** updateSlices

  let updateSlices (player: CuePlayer) (slices: Slices) =
    { player with
        Call = Pin.setSlices slices player.Call
        Next = Pin.setSlices slices player.Next
        Previous = Pin.setSlices slices player.Previous }

  // ** processSlices

  let processSlices (player: CuePlayer) (slices: Map<Id,Slices>) =
    let maybeUpdate (pin: Pin) =
      slices
      |> Map.tryFind pin.Id
      |> Option.map (flip Pin.setSlices pin)
      |> Option.defaultValue pin
    { player with
        Call     = maybeUpdate player.Call
        Next     = maybeUpdate player.Next
        Previous = maybeUpdate player.Previous }
