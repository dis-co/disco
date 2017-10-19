namespace rec Iris.Core

// * Imports

#if FABLE_COMPILER

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Iris.Serialization

#endif

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

#endif

// * CueListItem

type CueListItem =
  | Headline of IrisId * string
  | CueGroup of CueGroup

  // ** Id

  member item.Id =
    match item with
    | Headline (id, _) -> id
    | CueGroup group -> group.Id

  // ** FromFB

  static member FromFB(fb: CueListItemFB) =
    either {
      match fb.ItemType with
      #if FABLE_COMPILER
      | x when x = CueListItemTypeFB.HeadlineFB ->
        let hfb = fb.Item(HeadlineFB.Create())
        let! id = Id.decodeId hfb
        return Headline (id, hfb.Content)
      | x when x = CueListItemTypeFB.CueGroupFB ->
        let group = fb.Item(CueGroupFB.Create())
        let! parsed = CueGroup.FromFB group
        return CueGroup parsed
      | x ->
        return!
          x
          |> String.format "Could not parse unknown CueListItemTypeFB {0}"
          |> Error.asParseError "CueListItem.FromFB"
          |> Either.fail
      #else
      | CueListItemTypeFB.HeadlineFB ->
        let hlish = fb.Item<HeadlineFB>()
        if hlish.HasValue then
          let value = hlish.Value
          let! id = Id.decodeId value
          return Headline (id, value.Content)
        else
          return!
            "Could not parse empty HeadlineFB"
            |> Error.asParseError "CueListItem.FromFB"
            |> Either.fail
      | CueListItemTypeFB.CueGroupFB ->
        let groupish = fb.Item<CueGroupFB>()
        if groupish.HasValue then
          let value = groupish.Value
          let! group = CueGroup.FromFB value
          return CueGroup group
        else
          return!
            "Could not parse empty CueGroup value"
            |> Error.asParseError "CueListItem.FromFB"
            |> Either.fail
      | x ->
        return!
          x
          |> String.format "Could not parse unknown CueListItemTypeFB {0}"
          |> Error.asParseError "CueListItem.FromFB"
          |> Either.fail
      #endif
    }

  // ** ToOffset

  member item.ToOffset(builder: FlatBufferBuilder): Offset<CueListItemFB> =
      match item with
      | Headline (id, headline) when isNull headline ->
        let hid = id.ToByteArray()
        let idoffset = HeadlineFB.CreateIdVector(builder, hid)
        HeadlineFB.StartHeadlineFB(builder)
        HeadlineFB.AddId(builder, idoffset)
        let offset = HeadlineFB.EndHeadlineFB(builder)
        CueListItemFB.StartCueListItemFB(builder)
        CueListItemFB.AddItemType(builder, CueListItemTypeFB.HeadlineFB)
        #if FABLE_COMPILER
        CueListItemFB.AddItem(builder, offset)
        #else
        CueListItemFB.AddItem(builder, offset.Value)
        #endif
        CueListItemFB.EndCueListItemFB(builder)
      | Headline (id,headline) ->
        let hid = id.ToByteArray()
        let idoffset = HeadlineFB.CreateIdVector(builder, hid)
        let headline = builder.CreateString headline
        HeadlineFB.StartHeadlineFB(builder)
        HeadlineFB.AddId(builder, idoffset)
        HeadlineFB.AddContent(builder,headline)
        let offset = HeadlineFB.EndHeadlineFB(builder)
        CueListItemFB.StartCueListItemFB(builder)
        CueListItemFB.AddItemType(builder, CueListItemTypeFB.HeadlineFB)
        #if FABLE_COMPILER
        CueListItemFB.AddItem(builder, offset)
        #else
        CueListItemFB.AddItem(builder, offset.Value)
        #endif
        CueListItemFB.EndCueListItemFB(builder)
      | CueGroup group ->
        let offset = Binary.toOffset builder group
        CueListItemFB.StartCueListItemFB(builder)
        CueListItemFB.AddItemType(builder, CueListItemTypeFB.CueGroupFB)
        #if FABLE_COMPILER
        CueListItemFB.AddItem(builder, offset)
        #else
        CueListItemFB.AddItem(builder, offset.Value)
        #endif
        CueListItemFB.EndCueListItemFB(builder)

  // ** ToBytes

  member item.ToBytes() = Binary.buildBuffer item

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<IrisError,CueListItem> =
    bytes
    |> Binary.createBuffer
    |> CueListItemFB.GetRootAsCueListItemFB
    |> CueListItem.FromFB

  // ** ToYaml

  #if !FABLE_COMPILER && !IRIS_NODES

  member item.ToYaml() = CueListItemYaml.From(item)

  // ** FromYaml

  static member FromYaml(yaml: CueListItemYaml) : Either<IrisError,CueListItem> =
    yaml.ToCueListItem()

  #endif

// * CueListItemYaml

#if !FABLE_COMPILER && !IRIS_NODES

type HeadlineYaml() =
  [<DefaultValue>] val mutable Id:string
  [<DefaultValue>] val mutable Headline:string

  static member From id headline =
    let yaml = HeadlineYaml()
    yaml.Id <- string id
    yaml.Headline <- headline
    yaml

type CueListItemYaml() =
  [<DefaultValue>] val mutable Type:string
  [<DefaultValue>] val mutable Value:obj

  // ** From

  static member From(item: CueListItem) =
    let yaml = CueListItemYaml()
    match item with
    | Headline (id, str) ->
      yaml.Type <- "Headline"
      yaml.Value <- HeadlineYaml.From id str
    | CueGroup group ->
      yaml.Type <- "CueGroup"
      yaml.Value <- CueGroupYaml.From group
    yaml

  // ** ToCueListItem

  member yaml.ToCueListItem() =
    either {
      match yaml.Type with
      | "Headline" ->
        let headline = yaml.Value :?> HeadlineYaml
        let! id = IrisId.TryParse headline.Id
        return CueListItem.Headline (id, headline.Headline)
      | "CueGroup" ->
        let yaml = yaml.Value :?> CueGroupYaml
        let! group = yaml.ToCueGroup()
        return CueListItem.CueGroup group
      | other ->
        return!
          other
          |> String.format "Unsuppored CueList item type: {0}"
          |> Error.asParseError "CueListItem.ToCueListItem"
          |> Either.fail
    }

// * CueList Yaml

type CueListYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Items: CueListItemYaml array

  static member From(cuelist: CueList) =
    let yaml = CueListYaml()
    yaml.Id   <- string cuelist.Id
    yaml.Name <- unwrap cuelist.Name
    yaml.Items <- Array.map Yaml.toYaml cuelist.Items
    yaml

  member yaml.ToCueList() =
    either {
      let! items =
        let arr = Array.zeroCreate yaml.Items.Length
        Array.fold
          (fun (m: Either<IrisError,int * CueListItem array>) itemish -> either {
            let! (i, arr) = m
            let! (item: CueListItem) = Yaml.fromYaml itemish
            arr.[i] <- item
            return (i + 1, arr)
          })
          (Right (0, arr))
          yaml.Items
        |> Either.map snd

      let! id = IrisId.TryParse yaml.Id

      return {
        Id = id
        Name = name yaml.Name
        Items = items
      }
    }

#endif

// * CueListItem module

module CueListItem =

  // ** createHeadline

  let inline createHeadline str = Headline(IrisId.Create(), str)

// * CueList

type CueList =
  { Id: CueListId
    Name: Name
    Items: CueListItem array }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = CueListFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let itemoffsets = Array.map (Binary.toOffset builder) self.Items
    let itemsvec = CueListFB.CreateItemsVector(builder, itemoffsets)
    CueListFB.StartCueListFB(builder)
    CueListFB.AddId(builder, id)
    Option.iter (fun value -> CueListFB.AddName(builder, value)) name
    CueListFB.AddItems(builder, itemsvec)
    CueListFB.EndCueListFB(builder)

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** FromFB

  static member FromFB(fb: CueListFB) : Either<IrisError, CueList> =
    either {
      let! items =
        let arr = Array.zeroCreate fb.ItemsLength
        Array.fold
          (fun (m: Either<IrisError,int * CueListItem array>) _ -> either {
            let! (i, items) = m

            #if FABLE_COMPILER

            let! item =
              fb.Items(i) |> CueListItem.FromFB
            #else

            let! item =
              let value = fb.Items(i)
              if value.HasValue then
                CueListItem.FromFB value.Value
              else
                "Could not parse empty CueListItemFB"
                |> Error.asParseError "CueList.FromFB"
                |> Either.fail

            #endif

            items.[i] <- item
            return (i + 1, items)
          })
          (Right (0, arr))
          arr
        |> Either.map snd

      let! id = Id.decodeId fb

      return {
        Id = id
        Name = name fb.Name
        Items = items
      }
    }

  // ** FromBytes

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> CueListFB.GetRootAsCueListFB
    |> CueList.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  // ** ToYaml

  member cuelist.ToYaml() = CueListYaml.From(cuelist)

  // ** FromYaml

  static member FromYaml(yml: CueListYaml) : Either<IrisError,CueList> =
    yml.ToCueList()

  // ** AssetPath

  member self.AssetPath
    with get () =
      CUELIST_DIR <.> sprintf "%s%s" (string self.Id) ASSET_EXTENSION

  // ** HasParent

  /// CueLists don't live in nested directories, hence false
  member list.HasParent with get () = false

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load(path: FilePath) : Either<IrisError, CueList> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError, CueList array> =
    basePath </> filepath CUELIST_DIR
    |> IrisData.loadAll

  // ** Save

  member cuelist.Save (basePath: FilePath) =
    IrisData.save basePath cuelist

  // ** Delete

  member cuelist.Delete (basePath: FilePath) =
    IrisData.save basePath cuelist

  #endif

// * CueList module

module CueList =

  // ** map

  /// execute a function on each of the CueLists items and return the updated CueList
  let map (f: CueListItem -> CueListItem) (cueList:CueList) =
    { cueList with Items = Array.map f cueList.Items }

  // ** replace

  /// replace a CueGroup
  let replace (item:CueListItem) cueList =
    flip map cueList <| fun existing ->
      if existing.Id = item.Id
      then item
      else item

  // ** foldi

  let foldi (f: 'm -> int -> CueListItem -> 'm) (state:'m) (cueList:CueList) =
    cueList.Items
    |> Array.fold (fun (s,i) t -> (f s i t, i + 1)) (state,0)
    |> fst

  // ** fold

  let fold (f: 'm -> CueListItem -> 'm) (state:'m) (cueList:CueList) =
    Array.fold f state cueList.Items

  // ** insertAfter

  let insertAfter (idx:int) (item:CueListItem) cueList =
    let folder (state:ResizeArray<_>) curr existing =
      if curr = idx + 1
      then
        state.Add item
        state.Add existing
        state
      else
        state.Add existing
        state
    let items = foldi folder (ResizeArray()) cueList
    { cueList with Items = items.ToArray() }

  // ** filterItems

  let filterItems (f: CueListItem -> bool) (cueList:CueList) =
    { cueList with Items = Array.filter f cueList.Items }

  // ** filter

  let filter (f: CueList -> bool) (map: Map<CueListId,CueList>) =
    Map.filter (fun _ -> f) map

  // ** contains

  let contains (cueId: CueId) (cuelist: CueList) =
    Array.fold
      (fun result -> function
        | CueGroup group when not result ->
          CueGroup.contains cueId group
        | _ -> result)
      true
      cuelist.Items
