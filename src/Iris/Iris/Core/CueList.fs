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
// * CueList Yaml

type CueListYaml() =
  [<DefaultValue>] val mutable Id   : string
  [<DefaultValue>] val mutable Name : string
  [<DefaultValue>] val mutable Groups : CueGroupYaml array

  static member From(cuelist: CueList) =
    let yaml = CueListYaml()
    yaml.Id   <- string cuelist.Id
    yaml.Name <- unwrap cuelist.Name
    yaml.Groups <- Array.map Yaml.toYaml cuelist.Groups
    yaml

  member yaml.ToCueList() =
    either {
      let! groups =
        let arr = Array.zeroCreate yaml.Groups.Length
        Array.fold
          (fun (m: Either<IrisError,int * CueGroup array>) cueish -> either {
            let! (i, arr) = m
            let! (group: CueGroup) = Yaml.fromYaml cueish
            arr.[i] <- group
            return (i + 1, arr)
          })
          (Right (0, arr))
          yaml.Groups
        |> Either.map snd

      let! id = IrisId.TryParse yaml.Id

      return {
        Id = id
        Name = name yaml.Name
        Groups = groups
      }
    }

#endif

// * CueList

type CueList =
  { Id     : CueListId
    Name   : Name
    Groups : CueGroup array }

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
    let groupoffsets = Array.map (Binary.toOffset builder) self.Groups
    let groupsvec = CueListFB.CreateGroupsVector(builder, groupoffsets)
    CueListFB.StartCueListFB(builder)
    CueListFB.AddId(builder, id)
    Option.iter (fun value -> CueListFB.AddName(builder, value)) name
    CueListFB.AddGroups(builder, groupsvec)
    CueListFB.EndCueListFB(builder)

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** FromFB

  static member FromFB(fb: CueListFB) : Either<IrisError, CueList> =
    either {
      let! groups =
        let arr = Array.zeroCreate fb.GroupsLength
        Array.fold
          (fun (m: Either<IrisError,int * CueGroup array>) _ -> either {
            let! (i, groups) = m

            #if FABLE_COMPILER

            let! group =
              fb.Groups(i)
              |> CueGroup.FromFB
            #else

            let! group =
              let value = fb.Groups(i)
              if value.HasValue then
                value.Value
                |> CueGroup.FromFB
              else
                "Could not parse empty CueGroupFB"
                |> Error.asParseError "CueList.FromFB"
                |> Either.fail

            #endif

            groups.[i] <- group
            return (i + 1, groups)
          })
          (Right (0, arr))
          arr
        |> Either.map snd

      let! id = Id.decodeId fb

      return {
        Id = id
        Name = name fb.Name
        Groups = groups
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
      cuelist.Groups
