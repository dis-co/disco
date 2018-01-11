(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace rec Disco.Core

// * Imports

#if FABLE_COMPILER

open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Disco.Serialization

#endif

#if !FABLE_COMPILER && !DISCO_NODES

open SharpYaml.Serialization

#endif

// * CueList Yaml

#if !FABLE_COMPILER && !DISCO_NODES

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
          (fun (m: Either<DiscoError,int * CueGroup array>) itemish -> either {
            let! (i, arr) = m
            let! (item: CueGroup) = Yaml.fromYaml itemish
            arr.[i] <- item
            return (i + 1, arr)
          })
          (Right (0, arr))
          yaml.Items
        |> Either.map snd

      let! id = DiscoId.TryParse yaml.Id

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

  // ** Item

  member cueList.Item(idx:int) =
    cueList.Items.[idx]

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

  static member FromFB(fb: CueListFB) : Either<DiscoError, CueList> =
    either {
      let! items =
        let arr = Array.zeroCreate fb.ItemsLength
        Array.fold
          (fun (m: Either<DiscoError,int * CueGroup array>) _ -> either {
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

  #if !FABLE_COMPILER && !DISCO_NODES

  // ** ToYaml

  member cuelist.ToYaml() = CueListYaml.From(cuelist)

  // ** FromYaml

  static member FromYaml(yml: CueListYaml) : Either<DiscoError,CueList> =
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

  static member Load(path: FilePath) : Either<DiscoError, CueList> =
    DiscoData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<DiscoError, CueList array> =
    basePath </> filepath CUELIST_DIR
    |> DiscoData.loadAll

  // ** Save

  member cuelist.Save (basePath: FilePath) =
    DiscoData.save basePath cuelist

  // ** Delete

  member cuelist.Delete (basePath: FilePath) =
    DiscoData.save basePath cuelist

  #endif

// * CueList module

module CueList =

  open Aether

  // ** id

  let id = Optic.get CueList.Id_
  let setId = Optic.set CueList.Id_

  // ** name

  let name = Optic.get CueList.Name_
  let setName = Optic.set CueList.Name_

  // ** items

  let items = Optic.get CueList.Items_
  let setItems = Optic.set CueList.Items_

  // ** create

  let create (title:string) items =
    { Id = DiscoId.Create()
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

  // ** cueRefs

  let cueRefs = items >> Array.map CueGroup.cueRefs >> Array.concat

  // ** cueCount

  let cueCount (cueList: CueList) =
    Array.fold (fun m (group:CueGroup) -> m + Array.length group.CueRefs) 0 cueList.Items
