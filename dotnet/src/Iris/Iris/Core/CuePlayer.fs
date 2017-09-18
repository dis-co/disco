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

#endif


// * CuePlayerItemYaml

#if !FABLE_COMPILER && !IRIS_NODES

type CuePlayerItemYaml() =
  [<DefaultValue>] val mutable Type: string
  [<DefaultValue>] val mutable Value: string

  static member FromItem(item: CuePlayerItem) =
    let yaml = CuePlayerItemYaml()
    yaml.Type <- item.Type
    yaml.Value <- item.Value
    yaml

  member yaml.ToItem() =
    match yaml.Type with
    | "CueList" ->
      IrisId.TryParse yaml.Value
      |> Either.map CuePlayerItem.CueList
    | "Headline" ->
      Headline yaml.Value
      |> Either.succeed
    | other ->
      other
      |> String.format "Could not parse {0} as CuePlayerItem"
      |> Error.asParseError "CuePlayerItemYaml.ToItem"
      |> Either.fail

// * CuePlayerYaml

type CuePlayerYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Locked: bool
  [<DefaultValue>] val mutable Items: CuePlayerItemYaml array
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
    yaml.Items <- Array.map CuePlayerItemYaml.FromItem player.Items
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
      let! items =
        Array.fold
          (fun (m: Either<IrisError,ResizeArray<CuePlayerItem>>) (yaml: CuePlayerItemYaml) ->
            either {
              let! arr = m
              let! item = yaml.ToItem()
              do arr.Add(item)
              return arr
            })
          (Right (ResizeArray()))
          yaml.Items
      return {
        Id = id
        Name = name yaml.Name
        Locked = yaml.Locked
        Items = items.ToArray()
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

// * CuePlayerItem

type CuePlayerItem =
  | CueList  of CueListId
  | Headline of text:string

  // ** Type

  member item.Type
    with get () =
      match item with
      | CueList  _ -> "CueList"
      | Headline _ -> "Headline"

  // ** Value

  member item.Value
    with get () =
      match item with
      | CueList id -> string id
      | Headline txt -> txt

  // ** ToOffset

  member item.ToOffset(builder: FlatBufferBuilder) =
    match item with
    | CueList id ->
      let id = id |> string |> builder.CreateString
      CuePlayerItemFB.StartCuePlayerItemFB(builder)
      CuePlayerItemFB.AddType(builder, CuePlayerItemTypeFB.CueListFB)
      CuePlayerItemFB.AddValue(builder, id)
      CuePlayerItemFB.EndCuePlayerItemFB(builder)
    | Headline txt ->
      let txt = builder.CreateString txt
      CuePlayerItemFB.StartCuePlayerItemFB(builder)
      CuePlayerItemFB.AddType(builder, CuePlayerItemTypeFB.HeadlineFB)
      CuePlayerItemFB.AddValue(builder, txt)
      CuePlayerItemFB.EndCuePlayerItemFB(builder)

  // ** FromFB

  static member FromFB(fb: CuePlayerItemFB) =
    match fb.Type with
    #if FABLE_COMPILER
    | x when x = CuePlayerItemTypeFB.CueListFB ->
      fb.Value |> IrisId.TryParse |> Either.map CuePlayerItem.CueList
    | x when x = CuePlayerItemTypeFB.HeadlinFB ->
      CuePlayerItem.Headline fb.Value
      |> Either.succeed
    #else
    | CuePlayerItemTypeFB.CueListFB ->
      fb.Value |> IrisId.TryParse |> Either.map CuePlayerItem.CueList
    | CuePlayerItemTypeFB.HeadlineFB ->
      CuePlayerItem.Headline fb.Value
      |> Either.succeed
    #endif
    | other ->
      other
      |> String.format "Unknown CuePlayerItemTypeFB value {0}"
      |> Error.asParseError "CuePlayerItem.FromFB"
      |> Either.fail

// * CuePlayer

type CuePlayer =
  { Id: PlayerId
    Name: Name
    Items: CuePlayerItem array
    Locked: bool
    Selected: int<index>
    RemainingWait: int
    CallId: PinId                           // should be Bang pin type
    NextId: PinId                           // should be Bang pin type
    PreviousId: PinId                       // should be Bang pin type
    LastCalledId: CueId option
    LastCallerId: IrisId option }

  // ** ToOffset

  member player.ToOffset(builder: FlatBufferBuilder) =
    let id = CuePlayerFB.CreateIdVector(builder, player.Id.ToByteArray())
    let name = player.Name |> unwrap |> Option.mapNull builder.CreateString
    let items =
      player.Items
      |> Array.map (Binary.toOffset builder)
      |> fun items -> CuePlayerFB.CreateItemsVector(builder, items)
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
    CuePlayerFB.AddLocked(builder, player.Locked)
    CuePlayerFB.AddSelected(builder, int player.Selected)
    CuePlayerFB.AddRemainingWait(builder, player.RemainingWait)
    CuePlayerFB.AddCallId(builder, call)
    CuePlayerFB.AddNextId(builder, next)
    CuePlayerFB.AddPreviousId(builder, previous)
    CuePlayerFB.AddItems(builder, items)
    Option.iter (fun cl -> CuePlayerFB.AddLastCalledId(builder, cl)) lastcalled
    Option.iter (fun cl -> CuePlayerFB.AddLastCallerId(builder, cl)) lastcaller
    CuePlayerFB.EndCuePlayerFB(builder)

  // ** FromFB

  static member FromFB(fb: CuePlayerFB) =
    either {
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

      let! items =
        Array.fold
          (fun (m: Either<IrisError,ResizeArray<CuePlayerItem>>) (idx: int) ->
            either {
              let! arr = m
              let! item =
              #if FABLE_COMPILER
                fb.Items(idx)
                |> CuePlayerItem.FromFB
              #else
                let itemish = fb.Items(idx)
                if itemish.HasValue then
                  let item = itemish.Value
                  CuePlayerItem.FromFB item
                else
                  "Could not parse empty CuePlayerItem"
                  |> Error.asParseError "CuePlayer.FromFB"
                  |> Either.fail
              #endif
              do arr.Add(item)
              return arr
            })
          (Right (ResizeArray()))
          [| 0 .. fb.ItemsLength - 1 |]

      return {
        Id = id
        Name = name fb.Name
        Locked = fb.Locked
        Selected = index fb.Selected
        RemainingWait = fb.RemainingWait
        Items = items.ToArray()
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

  open NameUtils

  // ** create

  let create (playerName: Name) (items: CuePlayerItem array) =
    let id = IrisId.Create()
    { Id            = id
      Name          = playerName
      Locked        = false
      RemainingWait = -1
      Selected      = -1<index>
      Items         = items
      CallId        = Pin.Player.callId     id
      NextId        = Pin.Player.nextId     id
      PreviousId    = Pin.Player.previousId id
      LastCalledId  = None
      LastCallerId  = None }

  // ** assetPath

  let assetPath (player: CuePlayer) =
    CUEPLAYER_DIR <.> sprintf "%O%s" player.Id ASSET_EXTENSION
