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

// * CueList Yaml

#if !FABLE_COMPILER && !IRIS_NODES

type CueListYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Items: CueGroupYaml array

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
          (fun (m: Either<IrisError,int * CueGroup array>) itemish -> either {
            let! (i, arr) = m
            let! (item: CueGroup) = Yaml.fromYaml itemish
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

// * CueList

type CueList =
  { Id: CueListId
    Name: Name
    Items: CueGroup array }

  // ** optics

  static member Id_ =
    (fun (cuelist:CueList) -> cuelist.Id),
    (fun id (cuelist:CueList) -> { cuelist with Id = id })

  static member Name_ =
    (fun (cuelist:CueList) -> cuelist.Name),
    (fun name (cuelist:CueList) -> { cuelist with Name = name })

  static member Items_ =
    (fun (cuelist:CueList) -> cuelist.Items),
    (fun items (cuelist:CueList) -> { cuelist with Items = items })

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
          (fun (m: Either<IrisError,int * CueGroup array>) _ -> either {
            let! (i, items) = m

            #if FABLE_COMPILER
            let! item = fb.Items(i) |> CueGroup.FromFB
            #else
            let! item =
              let value = fb.Items(i)
              if value.HasValue then
                CueGroup.FromFB value.Value
              else
                "Could not parse empty CueGroupFB"
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

  open Aether

  // ** id =

  let id = Optic.get CueList.Id_
  let setId = Optic.set CueList.Id_

  // ** name

  let name = Optic.get CueList.Name_
  let setName = Optic.set CueList.Name_

  // ** create

  let create (title:string) items =
    { Id = IrisId.Create()
      Name = Measure.name title
      Items = items }

  // ** map

  /// execute a function on each of the CueLists items and return the updated CueList
  let map (f: CueGroup -> CueGroup) (cueList:CueList) =
    { cueList with Items = Array.map f cueList.Items }

  // ** replace

  /// replace a CueGroup
  let replace (item:CueGroup) cueList =
    map
      (function
       | { Id = id } when id = item.Id -> item
       | existing -> existing)
      cueList

  // ** foldi

  let foldi (f: 'm -> int -> CueGroup -> 'm) (state:'m) (cueList:CueList) =
    cueList.Items
    |> Array.fold (fun (s,i) t -> (f s i t, i + 1)) (state,0)
    |> fst

  // ** fold

  let fold (f: 'm -> CueGroup -> 'm) (state:'m) (cueList:CueList) =
    Array.fold f state cueList.Items

  // ** insertAfter

  let insertAfter (idx:int) (item:CueGroup) cueList =
    { cueList with Items = Array.insertAfter idx item cueList.Items }

  // ** filterItems

  let filterItems (f: CueGroup -> bool) (cueList:CueList) =
    { cueList with Items = Array.filter f cueList.Items }

  // ** filter

  let filter (f: CueList -> bool) (map: Map<CueListId,CueList>) =
    Map.filter (fun _ -> f) map

  // ** contains

  let contains (cueId: CueId) (cuelist: CueList) =
    Array.fold
      (fun result group ->
        if not result
        then CueGroup.contains cueId group
        else result)
      true
      cuelist.Items
