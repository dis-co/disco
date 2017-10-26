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

// * CueGroupYaml

open System
open Path

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml
open SharpYaml.Serialization

type CueGroupYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable AutoFollow: bool
  [<DefaultValue>] val mutable CueRefs: CueReferenceYaml array

  // ** From

  static member From(cueGroup: CueGroup) =
    let yaml = CueGroupYaml()
    yaml.Id <- string cueGroup.Id
    yaml.Name <- cueGroup.Name |> Option.map unwrap |> Option.defaultValue null
    yaml.AutoFollow <- cueGroup.AutoFollow
    yaml.CueRefs <- Array.map Yaml.toYaml cueGroup.CueRefs
    yaml

  // ** ToCueGroup

  member yaml.ToCueGroup() =
    either {
      let! id = IrisId.TryParse yaml.Id
      let! cues = Either.bindArray Yaml.fromYaml yaml.CueRefs
      let name =
        if System.String.IsNullOrWhiteSpace yaml.Name
        then None
        else Some (name yaml.Name)
      return {
        Id = id
        Name = name
        AutoFollow = yaml.AutoFollow
        CueRefs = cues
      }
    }

#endif

// * CueGroup

[<StructuralEquality; StructuralComparison>]
type CueGroup =
  { Id:         CueGroupId
    Name:       Name option
    AutoFollow: bool
    CueRefs:    CueReference array }

  // ** optics

  static member Id_ =
    (fun (group:CueGroup) -> group.Id),
    (fun id (group:CueGroup) -> { group with Id = id })

  static member Name_ =
    (fun (group:CueGroup) -> group.Name),
    (fun name (group:CueGroup) -> { group with Name = name })

  static member AutoFollow_ =
    (fun (group:CueGroup) -> group.AutoFollow),
    (fun autoFollow (group:CueGroup) -> { group with AutoFollow = autoFollow })

  static member CueRefs_ =
    (fun (group:CueGroup) -> group.CueRefs),
    (fun cueRefs (group:CueGroup) -> { group with CueRefs = cueRefs })

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: CueGroupFB) : Either<IrisError,CueGroup> =
    either {
      let! cues =
        EitherExt.bindGeneratorToArray
          "CueGroup.FromFB"
          fb.CueRefsLength
          fb.CueRefs
          CueReference.FromFB
      let! id = Id.decodeId fb
      let name =
        match fb.Name with
        | null -> None
        | str -> Some (name str)
      return {
        Id = id
        Name = name
        AutoFollow = fb.AutoFollow
        CueRefs = cues
      }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueGroupFB> =
    let id = CueGroupFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> Option.map (unwrap >> builder.CreateString)
    let cueoffsets = Array.map (Binary.toOffset builder) self.CueRefs
    let cuesvec = CueGroupFB.CreateCueRefsVector(builder, cueoffsets)
    CueGroupFB.StartCueGroupFB(builder)
    CueGroupFB.AddId(builder, id)
    CueGroupFB.AddAutoFollow(builder, self.AutoFollow)
    Option.iter (fun value -> CueGroupFB.AddName(builder,value)) name
    CueGroupFB.AddCueRefs(builder, cuesvec)
    CueGroupFB.EndCueGroupFB(builder)

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<IrisError,CueGroup> =
    bytes
    |> Binary.createBuffer
    |> CueGroupFB.GetRootAsCueGroupFB
    |> CueGroup.FromFB

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** ToYaml

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member cue.ToYaml() = CueGroupYaml.From(cue)

  // ** FromYaml

  static member FromYaml(yaml: CueGroupYaml) : Either<IrisError,CueGroup> =
    yaml.ToCueGroup()

  #endif

// * CueGroup module

module CueGroup =

  open Aether

  // ** create

  let create refs =
    { Id = IrisId.Create()
      Name = None
      AutoFollow = false
      CueRefs = refs }

  // ** getters

  let id = Optic.get CueGroup.Id_
  let name = Optic.get CueGroup.Name_
  let autoFollow = Optic.get CueGroup.AutoFollow_
  let cueRefs = Optic.get CueGroup.CueRefs_

  // ** setters

  let setId = Optic.set CueGroup.Id_
  let setName = Optic.set CueGroup.Name_
  let setAutoFollow = Optic.set CueGroup.AutoFollow_
  let setCueRefs = Optic.set CueGroup.CueRefs_

  // ** map

  let map f (group:CueGroup) = Optic.map CueGroup.CueRefs_ f group

  // ** filter

  let filter (f: CueGroup -> bool) (groups: CueGroup array) =
    Array.filter f groups

  // ** update

  let updateRef (ref:CueReference) group =
    map
      (Array.map <| function
        | { Id = id } when id = ref.Id -> ref
        | other -> other)
      group

  // ** contains

  let contains (cueId: CueId) (group: CueGroup) =
    Array.fold
      (fun result ref ->
        if not result
        then ref.CueId = cueId
        else result)
      false
      group.CueRefs

  // ** insertAfter

  let insertAfter (idx:int) (item:CueReference) cueGroup =
    { cueGroup with CueRefs = Array.insertAfter idx item cueGroup.CueRefs }
