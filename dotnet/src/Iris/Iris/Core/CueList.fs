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

      return { Id = Id yaml.Id
               Name = name yaml.Name
               Groups = groups }
    }

#endif

// * CueList

type CueList =
  { Id     : Id
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
    let id = self.Id |> string |> builder.CreateString
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

      return { Id = Id fb.Id
               Name = name fb.Name
               Groups = groups }
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

  // ** ToYamlObject

  member cuelist.ToYamlObject() = CueListYaml.From(cuelist)

  // ** FromYamlObject

  static member FromYamlObject(yml: CueListYaml) : Either<IrisError,CueList> =
    yml.ToCueList()

  // ** ToYaml

  member cuelist.ToYaml(serializer: Serializer) =
    cuelist |> Yaml.toYaml |> serializer.Serialize

  // ** FromYaml

  static member FromYaml(str: string) : Either<IrisError, CueList> =
    let serializer = new Serializer()
    serializer.Deserialize<CueListYaml>(str)
    |> Yaml.fromYaml

  // ** AssetPath

  member self.AssetPath
    with get () =
      CUELIST_DIR <.> sprintf "%s%s" (string self.Id) ASSET_EXTENSION

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

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member cuelist.Save (basePath: FilePath) =
    IrisData.save basePath cuelist

  #endif
