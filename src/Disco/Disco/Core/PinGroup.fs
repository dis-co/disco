namespace rec Disco.Core

// * Imports

open Aether
open Aether.Operators

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Disco.Serialization

#endif

open Path

// * PinGroupYaml

#if !FABLE_COMPILER && !DISCO_NODES

open SharpYaml.Serialization

type PinGroupYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Path: string
  [<DefaultValue>] val mutable ClientId: string
  [<DefaultValue>] val mutable RefersTo: ReferencedValueYaml
  [<DefaultValue>] val mutable Pins: PinYaml array

  static member From (group: PinGroup) =
    let yml = PinGroupYaml()
    yml.Id <- string group.Id
    yml.Name <- unwrap group.Name
    yml.ClientId <- string group.ClientId
    yml.Path <- Option.defaultValue null (Option.map unwrap group.Path)
    yml.Pins <- group.Pins |> Map.toArray |> Array.map (snd >> Yaml.toYaml)
    Option.iter (fun reference -> yml.RefersTo <- Yaml.toYaml reference) group.RefersTo
    yml

  member yml.ToPinGroup() =
    either {
      let! id = DiscoId.TryParse yml.Id
      let! client = DiscoId.TryParse yml.ClientId

      let! pins =
        Array.fold
          (fun (m: Either<DiscoError,Map<PinId,Pin>>) pinyml -> either {
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
          yml.RefersTo
          |> Yaml.fromYaml
          |> Either.map Some

      return {
        Id = id
        Name = Measure.name yml.Name
        ClientId = client
        Path = path
        RefersTo = refersTo
        Pins = pins
      }
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
    | "player" -> either {
        let! id = DiscoId.TryParse yml.Id
        return ReferencedValue.Player id
      }
    | "widget" -> either {
        let! id = DiscoId.TryParse yml.Id
        return ReferencedValue.Widget id
      }
    | other ->
      other
      |> String.format "Could not parse ReferencedValue type: {0}"
      |> Error.asParseError "ReferencedValueYaml.ToReferencedValue"
      |> Either.fail

#endif

// * ReferencedValue

[<RequireQualifiedAccess>]
type ReferencedValue =
  | Player of PlayerId
  | Widget of WidgetId

  // ** optics

  static member Player_ =
    (function
      | Player id -> Some id
      | _ -> None),
    (fun id -> function
      | Player _ -> Player id
      | other -> other)

  static member Widget_ =
    (function
      | Widget id -> Some id
      | _ -> None),
    (fun id -> function
      | Widget _ -> Widget id
      | other -> other)

  static member Id_ =
    (function | Player id | Widget id -> id),
    (fun id -> function
      | Player _ -> Player id
      | Widget _ -> Widget id)

  // ** Id

  member reference.Id = Optic.get ReferencedValue.Id_ reference

  // ** ToYaml

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !DISCO_NODES

  member reference.ToYaml () = ReferencedValueYaml.From(reference)

  // ** FromYaml

  static member FromYaml(yml: ReferencedValueYaml) = yml.ToReferencedValue()

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
    | x when x = ReferencedValueTypeFB.PlayerFB -> Id.decodeId fb |> Either.map Player
    | x when x = ReferencedValueTypeFB.WidgetFB -> Id.decodeId fb |> Either.map Widget
    | x ->
      x
      |> String.format "Could not parse unknown ReferencedValueTypeFB {0}"
      |> Error.asParseError "ReferencedValue.FromFB"
      |> Either.fail
    #else
    match fb.Type with
    | ReferencedValueTypeFB.PlayerFB -> Id.decodeId fb |> Either.map Player
    | ReferencedValueTypeFB.WidgetFB -> Id.decodeId fb |> Either.map Widget
    | other ->
      other
      |> String.format "Could not parse unknown ReferencedValueTypeFB {0}"
      |> Error.asParseError "ReferencedValue.FromFB"
      |> Either.fail
    #endif

  // ** ToOffset

  member reference.ToOffset(builder: FlatBufferBuilder) : Offset<ReferencedValueFB> =
    let refid = reference.Id
    let id = ReferencedValueFB.CreateIdVector(builder,refid.ToByteArray())
    ReferencedValueFB.StartReferencedValueFB(builder)
    ReferencedValueFB.AddId(builder, id)
    match reference with
    | Player _ -> ReferencedValueFB.AddType(builder, ReferencedValueTypeFB.PlayerFB)
    | Widget _ -> ReferencedValueFB.AddType(builder, ReferencedValueTypeFB.WidgetFB)
    ReferencedValueFB.EndReferencedValueFB(builder)

  // ** ToBytes

  member reference.ToBytes() : byte[] = Binary.buildBuffer reference

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<DiscoError,ReferencedValue> =
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
  { Id: PinGroupId
    Name: Name
    ClientId: ClientId
    RefersTo: ReferencedValue option    /// optionally add a reference to a player/widget
    Path: FilePath option               /// optionally the location of this group on disk
    Pins: Map<PinId,Pin> }

  // ** optics

  static member Id_ =
    (fun (group:PinGroup) -> group.Id),
    (fun id (group:PinGroup) -> { group with Id = id })

  static member Name_ =
    (fun (group:PinGroup) -> group.Name),
    (fun name (group:PinGroup) -> { group with Name = name })

  static member ClientId_ =
    (fun (group:PinGroup) -> group.ClientId),
    (fun clientId (group:PinGroup) -> { group with ClientId = clientId })

  static member RefersTo_ =
    (fun (group:PinGroup) -> group.RefersTo),
    (fun refersTo (group:PinGroup) -> { group with RefersTo = refersTo })

  static member Path_ =
    (fun (group:PinGroup) -> group.Path),
    (fun path (group:PinGroup) -> { group with Path = path })

  static member Pins_ =
    (fun (group:PinGroup) -> group.Pins),
    (fun pins (group:PinGroup) -> { group with Pins = pins })

  static member Pin_ (id:PinId) =
    PinGroup.Pins_ >-> Map.value_ id

  /// reach into the PinGroup to see if we can find a PlayerId
  static member Player_ =
        PinGroup.RefersTo_
    >-> Option.value_
    >?> ReferencedValue.Player_

  /// reach into the PinGroup to see if we can find a WidgetId
  static member Widget_ =
        PinGroup.RefersTo_
    >-> Option.value_
    >?> ReferencedValue.Widget_

  // ** ToYaml

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !DISCO_NODES

  member group.ToYaml () = PinGroupYaml.From(group)

  // ** FromYaml

  static member FromYaml (yml: PinGroupYaml) = yml.ToPinGroup()

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
          (fun (m: Either<DiscoError,int * Map<PinId,Pin>>) _ -> either {
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

      let! id = Id.decodeId fb
      let! client = Id.decodeClientId fb

      return {
        Id = id
        Name = Measure.name fb.Name
        Path = path
        ClientId = client
        RefersTo = refersTo
        Pins = pins
      }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<PinGroupFB> =
    let id = PinGroupFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let path = self.Path |> Option.map (unwrap >> builder.CreateString)
    let client = PinGroupFB.CreateClientIdVector(builder,self.ClientId.ToByteArray())
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
    PinGroupFB.AddClientId(builder, client)
    PinGroupFB.AddPins(builder, pins)
    PinGroupFB.EndPinGroupFB(builder)

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<DiscoError,PinGroup> =
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

  #if !FABLE_COMPILER && !DISCO_NODES

  static member Load(path: FilePath) : Either<DiscoError, PinGroup> =
    DiscoData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<DiscoError, PinGroup array> =
    basePath </> filepath Constants.PINGROUP_DIR
    |> DiscoData.loadAll

  // ** Save

  member group.Save (basePath: FilePath) =
    PinGroup.save basePath group

  // ** Delete

  member group.Delete (basePath: FilePath) =
    DiscoData.delete basePath group


  // ** Persisted

  member group.Persisted
    with get () = PinGroup.hasPersistedPins group

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

  /// // ** getters

  let id = Optic.get PinGroup.Id_
  let name = Optic.get PinGroup.Name_
  let clientId = Optic.get PinGroup.ClientId_
  let refersTo = Optic.get PinGroup.RefersTo_
  let path = Optic.get PinGroup.Path_
  let pins = Optic.get PinGroup.Pins_
  let pin id = Optic.get (PinGroup.Pin_ id)

  /// // ** setters

  let setId = Optic.set PinGroup.Id_
  let setName = Optic.set PinGroup.Name_
  let setClientId = Optic.set PinGroup.ClientId_
  let setRefersTo = Optic.set PinGroup.RefersTo_
  let setPath = Optic.set PinGroup.Path_
  let setPins = Optic.set PinGroup.Pins_
  let setPin id pin = Optic.set (PinGroup.Pin_ id) (Some pin)

  // ** isEmpty

  let isEmpty (group: PinGroup) =
    group.Pins.IsEmpty

  // ** create

  let create (groupName: Name) =
    { Id = DiscoId.Create()
      Name = groupName
      ClientId = DiscoId.Create()
      RefersTo = None
      Path = None
      Pins = Map.empty }

  // ** contains

  let contains (pin: PinId) (group: PinGroup) =
    Map.containsKey pin group.Pins

  // ** save

  #if !FABLE_COMPILER && !DISCO_NODES

  let save basePath (group: PinGroup) =
    if hasPersistedPins group then
      group
      |> persistedPins
      |> DiscoData.save basePath
    else Either.succeed ()

  #endif

  // ** relativePath

  let relativePath (client: ClientId) (group: PinGroupId) =
    let fn = string group + ASSET_EXTENSION
    let path = (string client) <.> fn
    filepath PINGROUP_DIR </> path

  // ** absolutePath

  let absolutePath (basePath: FilePath) (client: ClientId) (group: PinGroupId) =
    basePath </> relativePath client group

  // ** assetPath

  let assetPath (group: PinGroup) =
    relativePath group.ClientId group.Id

  // ** findPin

  let findPin (id: PinId) (group: PinGroup) =
    Map.find id group.Pins

  // ** tryFindPin

  let tryFindPin (id: PinId) (group: PinGroup) =
    Map.tryFind id group.Pins

  // ** addPin

  let addPin (pin : Pin) (group : PinGroup) : PinGroup =
    if contains pin.Id group
    then   group
    else { group with Pins = Map.add pin.Id pin group.Pins }

  // ** updatePin

  let updatePin (pin : Pin) (group : PinGroup) : PinGroup =
    if contains pin.Id group
    then { group with Pins = Map.add pin.Id pin group.Pins }
    else   group

  // ** updateSlices

  let updateSlices (slices: Slices) (group : PinGroup) : PinGroup =
    match Map.tryFind slices.PinId group.Pins with
    | Some pin -> { group with Pins = Map.add pin.Id (Pin.setSlices slices pin) group.Pins }
    | None -> group

  // ** processSlices

  let processSlices (slices: Map<PinId,Slices>) (group: PinGroup) : PinGroup =
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

  let removePin (pin : Pin) (group : PinGroup) : PinGroup =
    { group with Pins = Map.remove pin.Id group.Pins }

  // ** setPinsOffline

  let setPinsOffline (group: PinGroup) =
    { group with Pins = Map.map (fun _ pin -> Pin.setOnline false pin) group.Pins }

  // ** ofPlayer

  let ofPlayer (player: CuePlayer) =
    let client = DiscoId.Parse Constants.CUEPLAYER_GROUP_ID
    let call = Pin.Player.call     client player.Id player.CallId
    let next = Pin.Player.next     client player.Id player.NextId
    let prev = Pin.Player.previous client player.Id player.PreviousId
    { Id = player.Id
      Name = Measure.name (unwrap player.Name + " (Cue Player)")
      ClientId = client
      Path = None
      RefersTo = Some (ReferencedValue.Player player.Id)
      Pins = Map.ofList
                [ (call.Id, call)
                  (next.Id, next)
                  (prev.Id, prev) ] }

  // ** ofWidget

  let ofWidget (widget: PinWidget) =
    { Id = widget.Id
      Name = Measure.name (unwrap widget.Name + " (Widget)")
      ClientId = DiscoId.Parse Constants.PINWIDGET_GROUP_ID
      RefersTo = Some (ReferencedValue.Widget widget.Id)
      Path = None
      Pins = Map.empty }

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

  // ** filter

  let filter (pred: Pin -> bool) (group: PinGroup): PinGroup =
    group
    |> pins
    |> Map.filter (fun _ pin -> pred pin)
    |> flip setPins group

  // ** exists

  let exists (f: Pin -> bool) (group:PinGroup) =
    group
    |> PinGroup.pins
    |> Seq.exists (function KeyValue(_,pin) -> f pin)

  // ** hasPresetPins

  let hasPresetPins = exists Pin.isPreset

  // ** hasUnifiedPins

  let hasUnifiedPins = exists (Pin.isPreset >> not)

  // ** hasDirtyPins

  let hasDirtyPins = exists Pin.isDirty

  // ** hasPersistedPins

  let hasPersistedPins = exists Pin.isPersisted

  // ** hasUnpersistedPins

  let hasUnpersistedPins =  exists (Pin.isPersisted >> not)

  // ** updatePins

  let updatePins pins (group: PinGroup) = setPins pins group

  // ** unifiedPins

  let unifiedPins = filter (Pin.isPreset >> not)

  // ** presetPins

  let presetPins = filter Pin.isPreset

  // ** persistedPins

  let persistedPins = filter Pin.isPersisted

  // ** unpersistedPins

  let unpersistedPins = filter (Pin.isPersisted >> not)

  // ** volatilePins

  let volatilePins = unpersistedPins

  // ** sinks

  let sinks (group: PinGroup) =
    filter Pin.isSink group

  // ** sources

  let sources (group: PinGroup) =
    filter Pin.isSource group

  // ** dirtyPins

  let dirtyPins (group: PinGroup) =
    filter Pin.isDirty group

  // ** map

  let map (f: Pin -> Pin) (group: PinGroup) =
    { group with Pins = Map.map (fun _ -> f) group.Pins }

  // ** iter

  let iter (f: Pin -> unit) (group: PinGroup) =
    Map.iter (fun _ -> f) group.Pins

  // ** fold

  let fold (f: 's -> Pin -> 's) (state: 's) (group: PinGroup) =
    Map.fold (fun s _ pin -> f s pin) state group.Pins

// * PinGroupMap

type GroupMap = Map<PinGroupId, PinGroup>

type PinGroupMap =
  { Groups: Map<ClientId,GroupMap>
    Players: GroupMap
    Widgets: GroupMap }

  // ** optics

  static member Groups_ =
    (fun (map:PinGroupMap) -> map.Groups),
    (fun groups (pgm:PinGroupMap) -> { pgm with Groups = groups })

  static member Players_ =
    (fun (map:PinGroupMap) -> map.Players),
    (fun players (pgm:PinGroupMap) -> { pgm with Players = players })

  static member Widgets_ =
    (fun (map:PinGroupMap) -> map.Widgets),
    (fun widgets (pgm:PinGroupMap) -> { pgm with Widgets = widgets })

  static member ByClient_ (clientId:ClientId) =
    PinGroupMap.Groups_ >-> Map.value_ clientId >-> Option.value_

  static member Group_ (clientId:ClientId) (groupId:PinGroupId) =
    PinGroupMap.ByClient_ clientId >?> Map.value_ groupId >?> Option.value_

  static member Player_ (playerId:PlayerId) =
    PinGroupMap.Players_ >-> Map.value_ playerId >-> Option.value_

  static member Widget_ (widgetId:WidgetId) =
    PinGroupMap.Widgets_ >-> Map.value_ widgetId >-> Option.value_

  // ** Item

  member map.Item (clientId,groupId) =
    map.Groups.[clientId].[groupId]

  // ** ToOffset

  member map.ToOffset(builder: FlatBufferBuilder) =
    let regular =
      map.Groups
      |> Map.toArray
      |> Array.map (snd >> Map.toArray >> Array.map snd)
      |> Array.concat

    let players = map.Players |> Map.toArray |> Array.map snd
    let widgets = map.Widgets |> Map.toArray |> Array.map snd

    let vector =
      [ regular; players; widgets ]
      |> Array.concat
      |> Array.map (Binary.toOffset builder)
      |> fun arr -> PinGroupMapFB.CreateGroupsVector(builder, arr)

    PinGroupMapFB.StartPinGroupMapFB(builder)
    PinGroupMapFB.AddGroups(builder, vector)
    PinGroupMapFB.EndPinGroupMapFB(builder)

  // ** FromFB

  static member FromFB(fb: PinGroupMapFB) =
    [ 0 .. fb.GroupsLength - 1 ]
    |> List.fold
      (fun (m: Either<DiscoError,PinGroupMap>) idx -> either {
          let! current = m
          let! parsed =
            #if FABLE_COMPILER
            fb.Groups(idx)
            |> PinGroup.FromFB
            #else
            let groupish = fb.Groups(idx)
            if groupish.HasValue then
              let value = groupish.Value
              PinGroup.FromFB value
            else
              "Could not parse empty PinGroup value"
              |> Error.asParseError "PinGroupMap.FromFB"
              |> Either.fail
            #endif
          return PinGroupMap.add parsed current

        })
      (Right PinGroupMap.empty)

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<DiscoError,PinGroupMap> =
    Binary.createBuffer bytes
    |> PinGroupMapFB.GetRootAsPinGroupMapFB
    |> PinGroupMap.FromFB

  // ** Load

  #if !FABLE_COMPILER && !DISCO_NODES

  static member Load(path: FilePath) : Either<DiscoError, PinGroupMap> =
    either {
      let! groups = Asset.loadAll path
      return PinGroupMap.ofArray groups
    }

  // ** Save

  member map.Save (basePath: FilePath) =
    Map.fold
      (fun (m: Either<DiscoError,unit>) _ groups ->
        either {
          let! _ = m
          return! Map.fold (Asset.saveMap basePath) Either.nothing groups
        })
      Either.nothing
      map.Groups

  #endif

// * PinGroupMap module

module PinGroupMap =

  // ** empty

  let empty =
    { Groups = Map.empty
      Players = Map.empty
      Widgets = Map.empty }

  // ** getters

  let groups = Optic.get PinGroupMap.Groups_
  let players = Optic.get PinGroupMap.Players_
  let widgets = Optic.get PinGroupMap.Widgets_

  let byClient id = Optic.get (PinGroupMap.ByClient_ id)

  let group clientId groupId map =
    Optic.get (PinGroupMap.ByClient_ clientId >?> Map.value_ groupId) map

  // ** setters

  let setGroups = Optic.set PinGroupMap.Groups_
  let setPlayers = Optic.set PinGroupMap.Players_
  let setWidgets = Optic.set PinGroupMap.Widgets_

  let setByClient (clientId:ClientId) gm map =
    { map with Groups = Map.add clientId gm map.Groups }

  // ** addGroup

  let addGroup (group:PinGroup) (map:PinGroupMap) =
    match byClient group.ClientId map with
    | Some groupMap ->
      groupMap
      |> Map.add group.Id group
      |> fun gm -> setByClient group.ClientId gm map
    | None ->
      Map.empty
      |> Map.add group.Id group
      |> fun gm -> setByClient group.ClientId gm map

  // ** addPlayer

  let addPlayer (player:CuePlayer) (map:PinGroupMap) =
    let group = PinGroup.ofPlayer player
    players map
    |> Map.add group.Id group
    |> fun players -> setPlayers players map

  // ** removePlayer

  let removePlayer (player:CuePlayer) (map:PinGroupMap) =
    map.Players
    |> Map.filter
      (fun _ -> function
        | { RefersTo = Some reference } when reference.Id = player.Id -> false
        | _ -> true)
    |> fun players -> setPlayers players map

  // ** addWidget

  let addWidget (widget:PinWidget) (map:PinGroupMap) =
    let group = PinGroup.ofWidget widget
    widgets map
    |> Map.add group.Id group
    |> fun widgets -> setWidgets widgets map

  // ** removeWidget

  let removeWidget (widget:PinWidget) (map:PinGroupMap) =
    map.Widgets
    |> Map.filter
      (fun _ -> function
        | { RefersTo = Some reference } when reference.Id = widget.Id -> false
        | _ -> true)
    |> fun widgets -> setWidgets widgets map

  // ** add

  let add (group: PinGroup) (map: PinGroupMap) =
    match group.RefersTo with
    | Some (ReferencedValue.Player _) -> { map with Players = Map.add group.Id group map.Players }
    | Some (ReferencedValue.Widget _) -> { map with Widgets = Map.add group.Id group map.Widgets }
    | None -> addGroup group map

  // ** update

  let update = add

  // ** remove

  let remove (group: PinGroup) (map: PinGroupMap) =
    let current = map.Groups
    match Map.tryFind group.ClientId current with
    | None -> map
    | Some groups ->
      let groups = Map.remove group.Id groups
      if groups.IsEmpty then
        Map.remove group.ClientId current
        |> fun groups -> setGroups groups map
      else
        Map.add group.ClientId groups current
        |> fun groups -> setGroups groups map

  // ** containsGroup

  let containsGroup (client: ClientId) (group: PinGroupId) (map: PinGroupMap)  =
    match Map.tryFind client map.Groups with
    | Some groups -> Map.containsKey group groups
    | None -> false

  // ** containsPin

  let containsPin (client: ClientId) (group: PinGroupId) (pin: PinId) (map: PinGroupMap) =
    match Map.tryFind client map.Groups with
    | None -> false
    | Some groups -> Map.tryFind group groups |> function
      | Some group -> PinGroup.contains pin group
      | None -> false

  // ** modifyGroup

  let modifyGroup (f: PinGroup -> PinGroup) (client: ClientId) (group: PinGroupId) map =
    match map |> groups |> Map.tryFind client with
    | Some groups -> Map.tryFind group groups |> function
      | Some group ->
        let group = f group
        if PinGroup.isEmpty group
        then remove group map
        else update group map
      | None -> map
    | None -> map

  // ** addPin

  let addPin (pin: Pin) (map: PinGroupMap) =
    modifyGroup (PinGroup.addPin pin) pin.ClientId pin.PinGroupId map

  // ** updatePin

  let updatePin (pin: Pin) (map: PinGroupMap) =
    modifyGroup (PinGroup.updatePin pin) pin.ClientId pin.PinGroupId map

  // ** removePin

  let removePin (pin: Pin) (map: PinGroupMap) =
    modifyGroup (PinGroup.removePin pin) pin.ClientId pin.PinGroupId map

  // ** foldGroups

  let foldGroups (f: 'a -> PinGroupId -> PinGroup -> 'a) (state: 'a) map =
    groups map
    |> Map.fold (fun s _ groups -> Map.fold f s groups) state

  // ** iterGroups

  let iterGroups (f: PinGroup -> unit) (map: PinGroupMap) =
    groups map
    |> Map.iter (fun _ map -> Map.iter (fun _ group -> f group) map)


  // ** mapGroups

  let mapGroups (f: PinGroup -> PinGroup) (pgm: PinGroupMap) =
    groups pgm
    |> Map.map (fun _ groups -> Map.map (fun _ group -> f group) groups)
    |> fun groups -> setGroups groups pgm

  // ** mapPins

  let mapPins (f: Pin -> Pin) (pgm: PinGroupMap) =
    mapGroups (PinGroup.map f) pgm

  // ** count

  let count (map: PinGroupMap) =
    foldGroups (fun count _ _ -> count + 1) 0 map
    + Map.count map.Players
    + Map.count map.Widgets

  // ** isSpecialUpdate

  let private isSpecialUpdate
    (getter: PinGroupMap -> GroupMap)
    (slices: Map<PinId,Slices>)
    (map: PinGroupMap) =
    map
    |> getter
    |> Map.fold
      (fun result _ group ->
        if not result then
          Map.fold
            (fun any pid _ ->
              if not any
              then PinGroup.contains pid group
              else any)
            false
            slices
        else result)
      false

  // ** hasPlayerUpdate

  let hasPlayerUpdate (slices: Map<PinId,Slices>) (map: PinGroupMap) =
    isSpecialUpdate players slices map

  // ** hasPlayerUpdate

  let hasWidgetUpdate (slices: Map<PinId,Slices>) (map: PinGroupMap) =
    isSpecialUpdate widgets slices map

  // ** updateSlices

  let updateSlices (slices: Map<PinId,Slices>) (map: PinGroupMap) =
    mapGroups (PinGroup.processSlices slices) map

  // ** findPin

  let findPin (id: PinId) (map: PinGroupMap) : Map<ClientId,Pin> =
    foldGroups
      (fun out _ group ->
        group
        |> PinGroup.tryFindPin id
        |> Option.map (fun pin -> Map.add pin.ClientId pin out)
        |> Option.defaultValue out)
      Map.empty
      map

  // ** tryFindPin

  let tryFindPin (clientId: ClientId) (groupId: PinGroupId) (pinId: PinId) map =
    foldGroups
      (fun out _ (group: PinGroup) ->
        if Option.isSome out then
          out
        elif group.ClientId = clientId && group.Id = groupId then
          Map.tryFind pinId group.Pins
        else out)
      None
      map

  // ** tryFindGroup

  let tryFindGroup (cid: ClientId) (pid: PinGroupId) (map: PinGroupMap) =
    foldGroups
      (fun out gid (group: PinGroup) ->
        if gid = pid && cid = group.ClientId
        then Some group
        else out)
      None
      map

  // ** findGroup

  let findGroup (client: ClientId) (group: PinGroupId) (map: PinGroupMap) =
    Map.find client map.Groups |> Map.find group

  // ** findGroupBy

  let findGroupBy (pred: PinGroup -> bool) (map: PinGroupMap) : Map<ClientId,PinGroup> =
    foldGroups
      (fun out _ group ->
        if pred group
        then Map.add group.ClientId group out
        else out)
      Map.empty
      map

  // ** byGroup

  let byGroup (map: PinGroupMap) =
    foldGroups
      (fun out gid group -> Map.add gid group out)
      Map.empty
      map

  // ** ofSeq

  let ofSeq (groups: PinGroup seq) =
    Seq.fold (flip add) empty groups

  // ** ofList

  let ofList (groups: PinGroup list) =
    ofSeq groups

  // ** ofArray

  let ofArray (groups: PinGroup array) =
    ofSeq groups

  // ** toList

  let toList (map: PinGroupMap) =
    foldGroups
      (fun groups _ group -> group :: groups)
      List.empty
      map

  // ** toSeq

  let toSeq (map: PinGroupMap) =
    map |> toList |> Seq.ofList

  // ** toArray

  let toArray (map: PinGroupMap) =
    map |> toList |> Array.ofList

  // ** filter

  let filter (pred: PinGroup -> bool) (map: PinGroupMap) =
    foldGroups
      (fun out _ group ->
        if pred group
        then add group out
        else out)
      PinGroupMap.empty
      map

  // ** unifiedPins

  let unifiedPins (map: PinGroupMap) =
    map
    |> filter PinGroup.hasUnifiedPins
    |> mapGroups PinGroup.unifiedPins

  // ** presetPins

  let presetPins (map: PinGroupMap) =
    map
    |> filter PinGroup.hasPresetPins
    |> mapGroups PinGroup.presetPins

  // ** dirtyPins

  let dirtyPins (map: PinGroupMap) =
    map
    |> filter PinGroup.hasDirtyPins
    |> mapGroups PinGroup.dirtyPins

  // ** unpersistedPins

  let unpersistedPins (map: PinGroupMap) =
    map
    |> filter PinGroup.hasUnpersistedPins
    |> mapGroups PinGroup.unpersistedPins

  // ** hasDirtyPins

  let hasDirtyPins (map:PinGroupMap) =
    dirtyPins map |> groups |> Map.isEmpty |> not

  // ** hasUnpersistedPins

  let hasUnpersistedPins (map:PinGroupMap) =
    unpersistedPins map |> groups |> Map.isEmpty |> not

  // ** removeByClient

  let removeByClient (client: ClientId) (groups: PinGroupMap) =
    let filterAndMark (group: PinGroup) =
      group
      |> PinGroup.filter Pin.isPersisted
      |> PinGroup.map (Pin.setOnline false)

    foldGroups
      (fun map _ group ->
        if group.ClientId = client then
          if PinGroup.hasPersistedPins group
          then PinGroupMap.add (filterAndMark group) map
          else map
        else PinGroupMap.add group map)
      empty
      groups

// * Map module

module Map =

  // ** tryFindPin

  //  _              _____ _           _ ____  _
  // | |_ _ __ _   _|  ___(_)_ __   __| |  _ \(_)_ __
  // | __| '__| | | | |_  | | '_ \ / _` | |_) | | '_ \
  // | |_| |  | |_| |  _| | | | | | (_| |  __/| | | | |
  //  \__|_|   \__, |_|   |_|_| |_|\__,_|_|   |_|_| |_|
  //           |___/

  let tryFindPin (id: PinId) (groups : Map<PinGroupId, PinGroup>) : Pin option =
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

  let containsPin (id: PinId) (groups : Map<PinGroupId,PinGroup>) : bool =
    let folder m _ group =
      if m then m else PinGroup.contains id group || m
    Map.fold folder false groups
