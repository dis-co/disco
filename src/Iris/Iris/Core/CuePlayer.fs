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

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

// * CuePlayerYaml

type CuePlayerYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Locked: bool
  [<DefaultValue>] val mutable Active: bool
  [<DefaultValue>] val mutable CueListId: string
  [<DefaultValue>] val mutable Selected: int
  [<DefaultValue>] val mutable CallId: string
  [<DefaultValue>] val mutable NextId: string
  [<DefaultValue>] val mutable PreviousId: string
  [<DefaultValue>] val mutable RemainingWait: int
  [<DefaultValue>] val mutable LastCalledId: string
  [<DefaultValue>] val mutable LastCallerId: string

  // ** From

  static member FromPlayer(player: CuePlayer) =
    let yaml = CuePlayerYaml()
    let opt2str opt =
      match opt with
      | Some thing -> string thing
      | None -> null
    yaml.Id <- string player.Id
    yaml.Name <- unwrap player.Name
    yaml.Locked <- player.Locked
    yaml.Active <- player.Active
    Option.iter (fun id -> yaml.CueListId <- string id) player.CueListId
    yaml.Selected <- int player.Selected
    yaml.CallId <- string player.CallId
    yaml.NextId <- string player.NextId
    yaml.PreviousId <- string player.PreviousId
    yaml.RemainingWait <- player.RemainingWait
    yaml.LastCallerId <- opt2str player.LastCallerId
    yaml.LastCalledId <- opt2str player.LastCalledId
    yaml

  // ** ToPlayer

  member yaml.ToPlayer() =
    either {
      let str2opt str =
        match str with
        | null -> None
        | _    -> Some (IrisId.Parse str)
      let! id = IrisId.TryParse yaml.Id
      let! call = IrisId.TryParse yaml.CallId
      let! next = IrisId.TryParse yaml.NextId
      let! previous = IrisId.TryParse yaml.PreviousId
      return {
        Id = id
        Name = name yaml.Name
        Locked = yaml.Locked
        Active = yaml.Active
        CueListId = str2opt yaml.CueListId
        Selected = index yaml.Selected
        CallId = call
        NextId = next
        PreviousId = previous
        RemainingWait = yaml.RemainingWait
        LastCallerId = str2opt yaml.LastCallerId
        LastCalledId = str2opt yaml.LastCalledId
      }
    }

#endif

// * CuePlayer

type CuePlayer =
  { Id: PlayerId
    Name: Name
    CueListId: CueListId option
    Locked: bool
    Active: bool
    Selected: int<index>
    RemainingWait: int
    CallId: PinId                           // should be Bang pin type
    NextId: PinId                           // should be Bang pin type
    PreviousId: PinId                       // should be Bang pin type
    LastCalledId: CueId option
    LastCallerId: IrisId option }

  // ** optics

  static member Id_ =
   (fun (player:CuePlayer) -> player.Id),
    (fun id (player:CuePlayer) -> { player with Id = id })

  static member Name_ =
   (fun (player:CuePlayer) -> player.Name),
    (fun name (player:CuePlayer) -> { player with Name = name })

  static member Locked_ =
    (fun (player:CuePlayer) -> player.Locked),
    (fun locked (player:CuePlayer) -> { player with Locked = locked })

  static member Active_ =
    (fun (player:CuePlayer) -> player.Active),
    (fun active (player:CuePlayer) -> { player with Active = active })

  static member RemainingWait_ =
    (fun (player:CuePlayer) -> player.RemainingWait),
    (fun remainingWait (player:CuePlayer) -> { player with RemainingWait = remainingWait })

  static member Selected_ =
    (fun (player:CuePlayer) -> player.Selected),
    (fun selected (player:CuePlayer) -> { player with Selected = selected })

  static member CueListId_ =
    (fun (player:CuePlayer) -> player.CueListId),
    (fun cueListId (player:CuePlayer) -> { player with CueListId = cueListId })

  static member NextId_ =
    (fun (player:CuePlayer) -> player.NextId),
    (fun nextId (player:CuePlayer) -> { player with NextId = nextId })

  static member PreviousId_ =
    (fun (player:CuePlayer) -> player.PreviousId),
    (fun previousId (player:CuePlayer) -> { player with PreviousId = previousId })

  static member CallId_ =
    (fun (player:CuePlayer) -> player.CallId),
    (fun callId (player:CuePlayer) -> { player with CallId = callId })

  static member LastCalledId_ =
    (fun (player:CuePlayer) -> player.LastCalledId),
    (fun lastCalledId (player:CuePlayer) -> { player with LastCalledId = lastCalledId })

  static member LastCallerId_ =
    (fun (player:CuePlayer) -> player.LastCallerId),
    (fun lastCallerId (player:CuePlayer) -> { player with LastCallerId = lastCallerId })

  // ** ToOffset

  member player.ToOffset(builder: FlatBufferBuilder) =
    let id = CuePlayerFB.CreateIdVector(builder, player.Id.ToByteArray())
    let name = player.Name |> unwrap |> Option.mapNull builder.CreateString
    let cuelist =
      Option.map
        (fun (id:CueListId) ->
          CuePlayerFB.CreateCueListIdVector(builder,id.ToByteArray()))
        player.CueListId
    let call = CuePlayerFB.CreateCallIdVector(builder,player.CallId.ToByteArray())
    let next = CuePlayerFB.CreateNextIdVector(builder,player.NextId.ToByteArray())
    let previous = CuePlayerFB.CreatePreviousIdVector(builder,player.PreviousId.ToByteArray())
    let lastcalled =
      Option.map
        (fun (id:PinId) -> CuePlayerFB.CreateLastCalledIdVector(builder,id.ToByteArray()))
        player.LastCalledId
    let lastcaller =
      Option.map
        (fun (id:PinId) -> CuePlayerFB.CreateLastCallerIdVector(builder,id.ToByteArray()))
        player.LastCallerId
    CuePlayerFB.StartCuePlayerFB(builder)
    CuePlayerFB.AddId(builder, id)
    Option.iter (fun value -> CuePlayerFB.AddName(builder,value)) name
    Option.iter (fun value -> CuePlayerFB.AddCueListId(builder,value)) cuelist
    CuePlayerFB.AddLocked(builder, player.Locked)
    CuePlayerFB.AddActive(builder, player.Active)
    CuePlayerFB.AddSelected(builder, int player.Selected)
    CuePlayerFB.AddRemainingWait(builder, player.RemainingWait)
    CuePlayerFB.AddCallId(builder, call)
    CuePlayerFB.AddNextId(builder, next)
    CuePlayerFB.AddPreviousId(builder, previous)
    Option.iter (fun cl -> CuePlayerFB.AddLastCalledId(builder, cl)) lastcalled
    Option.iter (fun cl -> CuePlayerFB.AddLastCallerId(builder, cl)) lastcaller
    CuePlayerFB.EndCuePlayerFB(builder)

  // ** FromFB

  static member FromFB(fb: CuePlayerFB) =
    either {
      let! cuelist =
        try
          if fb.CueListIdLength = 0
          then Either.succeed None
          else Id.decodeCueListId fb |> Either.map Some
        with exn ->
          Either.succeed None

      let! lastcalled =
        try
          if fb.LastCalledIdLength = 0
          then Either.succeed None
          else Id.decodeLastCalledId fb |> Either.map Some
        with exn ->
          Either.succeed None

      let! lastcaller =
        try
          if fb.LastCallerIdLength = 0
          then Either.succeed None
          else Id.decodeLastCallerId fb |> Either.map Some
        with exn ->
          Either.succeed None

      let! id = Id.decodeId fb
      let! call = Id.decodeCallId fb
      let! next = Id.decodeNextId fb
      let! previous = Id.decodePreviousId fb

      return {
        Id = id
        Name = name fb.Name
        Locked = fb.Locked
        Active = fb.Active
        Selected = index fb.Selected
        RemainingWait = fb.RemainingWait
        CueListId = cuelist
        CallId = call
        NextId = next
        PreviousId = previous
        LastCalledId = lastcalled
        LastCallerId = lastcaller
      }
    }

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,CuePlayer> =
    Binary.createBuffer bytes
    |> CuePlayerFB.GetRootAsCuePlayerFB
    |> CuePlayer.FromFB

  // ** ToYaml

  #if !FABLE_COMPILER && !IRIS_NODES

  member player.ToYaml() = CuePlayerYaml.FromPlayer(player)

  // ** FromYaml

  static member FromYaml(yaml: CuePlayerYaml) = yaml.ToPlayer()

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

  open Aether

  // ** getters

  let id = Optic.get CuePlayer.Id_
  let name = Optic.get CuePlayer.Name_
  let active = Optic.get CuePlayer.Active_
  let locked = Optic.get CuePlayer.Locked_
  let remainingWait = Optic.get CuePlayer.RemainingWait_
  let selected = Optic.get CuePlayer.Selected_
  let cueListId = Optic.get CuePlayer.CueListId_
  let callId = Optic.get CuePlayer.CallId_
  let nextId = Optic.get CuePlayer.NextId_
  let previousId = Optic.get CuePlayer.PreviousId_
  let lastCalledId = Optic.get CuePlayer.LastCalledId_
  let lastCallerId = Optic.get CuePlayer.LastCallerId_

  // ** setters

  let setId = Optic.set CuePlayer.Id_
  let setName = Optic.set CuePlayer.Name_
  let setActive = Optic.set CuePlayer.Active_
  let setLocked = Optic.set CuePlayer.Locked_
  let setRemainingWait = Optic.set CuePlayer.RemainingWait_
  let setSelected = Optic.set CuePlayer.Selected_
  let setCallId = Optic.set CuePlayer.CallId_
  let setNextId = Optic.set CuePlayer.NextId_
  let setPreviousId = Optic.set CuePlayer.PreviousId_

  let setCueListId id = Optic.set CuePlayer.CueListId_ (Some id)
  let setLastCalledId id = Optic.set CuePlayer.LastCalledId_ (Some id)
  let setLastCallerId id = Optic.set CuePlayer.LastCallerId_ (Some id)
  let unsetCueListId = Optic.set CuePlayer.CueListId_ None
  let unsetLastCalledId = Optic.set CuePlayer.LastCalledId_ None
  let unsetLastCallerId = Optic.set CuePlayer.LastCallerId_ None

  // ** create

  let create (playerName: string) (cuelist: CueListId option) =
    let id = IrisId.Create()
    { Id            = id
      Name          = Measure.name playerName
      Active        = false
      Locked        = false
      RemainingWait = -1
      Selected      = -1<index>
      CueListId     = cuelist
      CallId        = IrisId.Create()
      NextId        = IrisId.Create()
      PreviousId    = IrisId.Create()
      LastCalledId  = None
      LastCallerId  = None }

  // ** assetPath

  let assetPath (player: CuePlayer) =
    CUEPLAYER_DIR <.> sprintf "%O%s" player.Id ASSET_EXTENSION

  // ** contains

  let contains (cuelistId: CueListId) (player: CuePlayer) =
    match player.CueListId with
    | Some id -> cuelistId = id
    | _ -> false

  // ** setCueList

  let setCueList id player = Optic.set CuePlayer.CueListId_ (Some id) player

  // ** unsetCueList

  let unsetCueList player = Optic.set CuePlayer.CueListId_ None player

  // ** increaseSelected

  let increaseSelected player =
    setSelected (player.Selected + 1<index>) player

  let decreaseSelected player =
    setSelected (player.Selected - 1<index>) player

  // ** updatePins

  let updatePins (slices:Slices) (player:CuePlayer) =
    if slices.PinId = player.NextId
    then increaseSelected player
    elif slices.PinId = player.PreviousId
    then decreaseSelected player
    else player

  // ** processSlices

  let processSlices (slices:Map<PinId,Slices>) (player:CuePlayer) =
    Map.fold
      (fun (player:CuePlayer) _ (slices:Slices) ->
        updatePins slices player)
      player
      slices
