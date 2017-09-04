namespace rec Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Iris.Serialization

#endif

open Path

// * PinGroupYaml

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

type PinGroupYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Path: string
  [<DefaultValue>] val mutable Client: string
  [<DefaultValue>] val mutable RefersTo: ReferencedValueYaml
  [<DefaultValue>] val mutable Pins: PinYaml array

  static member From (group: PinGroup) =
    let yml = PinGroupYaml()
    yml.Id <- string group.Id
    yml.Name <- unwrap group.Name
    yml.Client <- string group.Client
    yml.Path <- Option.defaultValue null (Option.map unwrap group.Path)
    yml.Pins <- group.Pins |> Map.toArray |> Array.map (snd >> Yaml.toYaml)
    Option.iter (fun reference -> yml.RefersTo <- Yaml.toYaml reference) group.RefersTo
    yml

  member yml.ToPinGroup() =
    either {
      let! pins =
        Array.fold
          (fun (m: Either<IrisError,Map<Id,Pin>>) pinyml -> either {
            let! pins = m
            let! (pin : Pin) = Yaml.fromYaml pinyml
            return Map.add pin.Id pin pins
          })
          (Right Map.empty)
          yml.Pins

      let path =
        if isNull yml.Path
        then None
        else Some (filepath yml.Path)

      let! refersTo =
        if isNull yml.RefersTo then
          Either.succeed None
        else
          Yaml.fromYaml yml.RefersTo
          |> Either.map Some

      return { Id = Id yml.Id
               Name = name yml.Name
               Path = path
               RefersTo = refersTo
               Client = Id yml.Client
               Pins = pins }
    }

// * ReferencedValueYaml

[<AllowNullLiteral>]
type ReferencedValueYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Type: string

  static member From (value: ReferencedValue) =
    let yml = ReferencedValueYaml()
    match value with
    | ReferencedValue.Player id ->
      yml.Id <- string id
      yml.Type <- "Player"
    | ReferencedValue.Widget id ->
      yml.Id <- string id
      yml.Type <- "Widget"
    yml

  member yml.ToReferencedValue() =
    match yml.Type.ToLowerInvariant() with
    | "player" -> ReferencedValue.Player (Id yml.Id) |> Either.succeed
    | "widget" -> ReferencedValue.Widget (Id yml.Id) |> Either.succeed
    | other ->
      other
      |> String.format "Could not parse ReferencedValue type: {0}"
      |> Error.asParseError "ReferencedValueYaml.ToReferencedValue"
      |> Either.fail

#endif

// * ReferencedValue

[<RequireQualifiedAccess>]
type ReferencedValue =
  | Player of Id
  | Widget of Id

  // ** Id

  member reference.Id
    with get () = match reference with | Player id | Widget id -> id

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member reference.ToYamlObject () = ReferencedValueYaml.From(reference)

  // ** ToYaml

  member reference.ToYaml (serializer: Serializer) =
    reference
    |> Yaml.toYaml
    |> serializer.Serialize

  // ** FromYamlObject

  static member FromYamlObject (yml: ReferencedValueYaml) = yml.ToReferencedValue()

  // ** FromYaml

  static member FromYaml (str: string) : Either<IrisError,ReferencedValue> =
    let serializer = Serializer()
    let yml = serializer.Deserialize<ReferencedValueYaml>(str)
    Yaml.fromYaml yml

  #endif

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: ReferencedValueFB) =
    #if FABLE_COMPILER
    match fb.Type with
    | x when x = ReferencedValueTypeFB.PlayerFB -> fb.Id |> Id |> Player |> Either.succeed
    | x when x = ReferencedValueTypeFB.WidgetFB -> fb.Id |> Id |> Widget |> Either.succeed
    | x ->
      x
      |> String.format "Could not parse unknown ReferencedValueTypeFB {0}"
      |> Error.asParseError "ReferencedValue.FromFB"
      |> Either.fail
    #else
    match fb.Type with
    | ReferencedValueTypeFB.PlayerFB -> fb.Id |> Id |> Player |> Either.succeed
    | ReferencedValueTypeFB.WidgetFB -> fb.Id |> Id |> Widget |> Either.succeed
    | other ->
      other
      |> String.format "Could not parse unknown ReferencedValueTypeFB {0}"
      |> Error.asParseError "ReferencedValue.FromFB"
      |> Either.fail
    #endif

  // ** ToOffset

  member reference.ToOffset(builder: FlatBufferBuilder) : Offset<ReferencedValueFB> =
    let id = reference.Id |> string |> builder.CreateString
    ReferencedValueFB.StartReferencedValueFB(builder)
    ReferencedValueFB.AddId(builder, id)
    match reference with
    | Player _ -> ReferencedValueFB.AddType(builder, ReferencedValueTypeFB.PlayerFB)
    | Widget _ -> ReferencedValueFB.AddType(builder, ReferencedValueTypeFB.WidgetFB)
    ReferencedValueFB.EndReferencedValueFB(builder)

  // ** ToBytes

  member reference.ToBytes() : byte[] = Binary.buildBuffer reference

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,ReferencedValue> =
    Binary.createBuffer bytes
    |> ReferencedValueFB.GetRootAsReferencedValueFB
    |> ReferencedValue.FromFB

// * PinGroup

//  ____  _        ____
// |  _ \(_)_ __  / ___|_ __ ___  _   _ _ __
// | |_) | | '_ \| |  _| '__/ _ \| | | | '_ \
// |  __/| | | | | |_| | | | (_) | |_| | |_) |
// |_|   |_|_| |_|\____|_|  \___/ \__,_| .__/
//                                     |_|

type PinGroup =
  { Id: Id
    Name: Name
    Client: Id
    RefersTo: ReferencedValue option    /// optionally add a reference to a player/widget
    Path: FilePath option               /// optionally the location of this group on disk
    Pins: Map<Id,Pin> }

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member group.ToYamlObject () = PinGroupYaml.From(group)

  // ** ToYaml

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  // ** FromYamlObject

  static member FromYamlObject (yml: PinGroupYaml) = yml.ToPinGroup()

  // ** FromYaml

  static member FromYaml (str: string) : Either<IrisError,PinGroup> =
    let serializer = Serializer()
    let yml = serializer.Deserialize<PinGroupYaml>(str)
    Yaml.fromYaml yml

  #endif

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: PinGroupFB) =
    either {
      let! pins =
        let arr = Array.zeroCreate fb.PinsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id,Pin>>) _ -> either {
              let! (i, pins) = m

              #if FABLE_COMPILER
              let! pin = i |> fb.Pins |> Pin.FromFB
              #else
              let! pin =
                let nullable = fb.Pins(i)
                if nullable.HasValue then
                  nullable.Value
                  |> Pin.FromFB
                else
                  "Could not parse empty PinFB"
                  |> Error.asParseError "PinGroup.FromFB"
                  |> Either.fail
              #endif

              return (i + 1, Map.add pin.Id pin pins)
            })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      let! refersTo =
        #if FABLE_COMPILER
        if isNull fb.RefersTo then
          Either.succeed None
        else
          fb.RefersTo
          |> ReferencedValue.FromFB
          |> Either.map Some
        #else
        let refish = fb.RefersTo
        if refish.HasValue then
          let value = refish.Value
          ReferencedValue.FromFB value
          |> Either.map Some
        else
          Either.succeed None
        #endif

      let path =
        if isNull fb.Path
        then None
        else Some (filepath fb.Path)

      return { Id = Id fb.Id
               Name = name fb.Name
               Path = path
               RefersTo = refersTo
               Client = Id fb.Client
               Pins = pins }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<PinGroupFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let path = self.Path |> Option.map (unwrap >> builder.CreateString)
    let client = self.Client |> string |> builder.CreateString
    let refersTo = self.RefersTo |> Option.map (Binary.toOffset builder)
    let pinoffsets =
      self.Pins
      |> Map.toArray
      |> Array.map (fun (_,pin: Pin) -> pin.ToOffset(builder))

    let pins = PinGroupFB.CreatePinsVector(builder, pinoffsets)
    PinGroupFB.StartPinGroupFB(builder)
    PinGroupFB.AddId(builder, id)
    Option.iter (fun value -> PinGroupFB.AddName(builder,value)) name
    Option.iter (fun value -> PinGroupFB.AddPath(builder,value)) path
    Option.iter (fun value -> PinGroupFB.AddRefersTo(builder,value)) refersTo
    PinGroupFB.AddClient(builder, client)
    PinGroupFB.AddPins(builder, pins)
    PinGroupFB.EndPinGroupFB(builder)

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,PinGroup> =
    Binary.createBuffer bytes
    |> PinGroupFB.GetRootAsPinGroupFB
    |> PinGroup.FromFB

  // ** HasParent

  /// PinGroups do live in nested directories, hence true
  member widget.HasParent with get () = true

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load(path: FilePath) : Either<IrisError, PinGroup> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError, PinGroup array> =
    basePath </> filepath Constants.PINGROUP_DIR
    |> IrisData.loadAll

  // ** Save

  member group.Save (basePath: FilePath) =
    PinGroup.save basePath group

  // ** Delete

  member group.Delete (basePath: FilePath) =
    IrisData.delete basePath group


  // ** Persisted

  member group.Persisted
    with get () = PinGroup.persisted group

  // ** IsSaved

  member group.Exists (basePath: FilePath) =
    basePath </> PinGroup.assetPath group
    |> File.exists

  #endif

  // ** AssetPath

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member pingroup.AssetPath
    with get () = PinGroup.assetPath pingroup

// * PinGroup module

module PinGroup =

  // ** create

  let create (groupName: Name) =
    { Id = Id.Create()
      Name = groupName
      Client = Id.Create()
      RefersTo = None
      Path = None
      Pins = Map.empty }

  // ** persisted

  let persisted (group: PinGroup) =
    group.Pins
    |> Map.filter (fun _ (pin: Pin) -> pin.Persisted)
    |> Map.isEmpty
    |> not

  // ** persistedPins

  let persistedPins (group: PinGroup) =
    Map.filter (fun _ (pin: Pin) -> pin.Persisted) group.Pins

  // ** volatilePins

  let volatilePins (group: PinGroup) =
    Map.filter (fun _ (pin: Pin) -> not pin.Persisted) group.Pins

  // ** removeVolatile

  let removeVolatile (group: PinGroup) =
    { group with Pins = persistedPins group }

  // ** save

  #if !FABLE_COMPILER && !IRIS_NODES

  let save basePath (group: PinGroup) =
    if persisted group then
      group
      |> removeVolatile
      |> IrisData.save basePath
    else Either.succeed ()

  #endif

  // ** assetPath

  let assetPath (group: PinGroup) =
    let fn = (string group.Id |> String.sanitize) + ASSET_EXTENSION
    let path = (string group.Client) <.> fn
    filepath PINGROUP_DIR </> path

  // ** hasPin

  let hasPin (group : PinGroup) (id: Id) : bool =
    Map.containsKey id group.Pins

  // ** findPin

  let findPin (group: PinGroup) (id: Id) =
    Map.find id group.Pins

  // ** tryFindPin

  let tryFindPin (group: PinGroup) (id: Id) =
    Map.tryFind id group.Pins

  // ** addPin

  let addPin (group : PinGroup) (pin : Pin) : PinGroup =
    if hasPin group pin.Id
    then   group
    else { group with Pins = Map.add pin.Id pin group.Pins }

  // ** updatePin

  let updatePin (group : PinGroup) (pin : Pin) : PinGroup =
    if hasPin group pin.Id
    then { group with Pins = Map.add pin.Id pin group.Pins }
    else   group

  // ** updateSlices

  let updateSlices (group : PinGroup) (slices: Slices) : PinGroup =
    match Map.tryFind slices.Id group.Pins with
    | Some pin -> { group with Pins = Map.add pin.Id (Pin.setSlices slices pin) group.Pins }
    | None -> group

  // ** processSlices

  let processSlices (group: PinGroup) (slices: Map<Id,Slices>) : PinGroup =
    let mapper _ (pin: Pin) =
      match Map.tryFind pin.Id slices with
      | Some slices -> Pin.setSlices slices pin
      | None -> pin
    { group with Pins = Map.map mapper group.Pins }

  // ** removePin

  //                                    ____  _
  //  _ __ ___ _ __ ___   _____   _____|  _ \(_)_ __
  // | '__/ _ \ '_ ` _ \ / _ \ \ / / _ \ |_) | | '_ \
  // | | |  __/ | | | | | (_) \ V /  __/  __/| | | | |
  // |_|  \___|_| |_| |_|\___/ \_/ \___|_|   |_|_| |_|

  let removePin (group : PinGroup) (pin : Pin) : PinGroup =
    { group with Pins = Map.remove pin.Id group.Pins }

  // ** setPinsOffline

  let setPinsOffline (group: PinGroup) =
    { group with Pins = Map.map (fun _ pin -> Pin.setOnline false pin) group.Pins }

  // ** ofPlayer

  let ofPlayer (player: CuePlayer) =
    let call = Pin.Player.call player.Id
    let next = Pin.Player.next player.Id
    let prev = Pin.Player.previous player.Id
    { Id = player.Id
      Name = name (unwrap player.Name + " (Cue Player)")
      Client = Id Constants.CUEPLAYER_GROUP_DIR
      Path = None
      RefersTo = Some (ReferencedValue.Player player.Id)
      Pins = Map.ofList
                [ (call.Id, call)
                  (next.Id, next)
                  (prev.Id, prev) ] }

  // ** ofWidget

  let ofWidget (widget: PinWidget) =
    { Id = widget.Id
      Name = name (unwrap widget.Name + " (Widget)")
      Client = Id Constants.PINWIDGET_GROUP_DIR
      RefersTo = Some (ReferencedValue.Widget widget.Id)
      Path = None
      Pins = Map.empty }

  // ** sinks

  let sinks (group: PinGroup) =
    Map.filter (fun _ pin -> Pin.isSink pin) group.Pins

  // ** sources

  let sources (group: PinGroup) =
    Map.filter (fun _ pin -> Pin.isSource pin) group.Pins

  // ** isPlayer

  let isPlayer (group: PinGroup) =
    match group.RefersTo with
    | Some (ReferencedValue.Player _) -> true
    | _ -> false

  // ** isWidget

  let isWidget (group: PinGroup) =
    match group.RefersTo with
    | Some (ReferencedValue.Widget _) -> true
    | _ -> false

// * Map module

module Map =

  // ** tryFindPin

  //  _              _____ _           _ ____  _
  // | |_ _ __ _   _|  ___(_)_ __   __| |  _ \(_)_ __
  // | __| '__| | | | |_  | | '_ \ / _` | |_) | | '_ \
  // | |_| |  | |_| |  _| | | | | | (_| |  __/| | | | |
  //  \__|_|   \__, |_|   |_|_| |_|\__,_|_|   |_|_| |_|
  //           |___/

  let tryFindPin (id : Id) (groups : Map<Id, PinGroup>) : Pin option =
    let folder (m : Pin option) _ (group: PinGroup) =
      match m with
        | Some _ as res -> res
        |      _        -> Map.tryFind id group.Pins
    Map.fold folder None groups

  // ** containsPin

  //                  _        _           ____  _
  //   ___ ___  _ __ | |_ __ _(_)_ __  ___|  _ \(_)_ __
  //  / __/ _ \| '_ \| __/ _` | | '_ \/ __| |_) | | '_ \
  // | (_| (_) | | | | || (_| | | | | \__ \  __/| | | | |
  //  \___\___/|_| |_|\__\__,_|_|_| |_|___/_|   |_|_| |_|

  let containsPin (id: Id) (groups : Map<Id,PinGroup>) : bool =
    let folder m _ group =
      if m then m else PinGroup.hasPin group id || m
    Map.fold folder false groups
