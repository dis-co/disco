namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.Text
open FlatBuffers
open Iris.Serialization
open SharpYaml.Serialization

#endif

//  ____                            _
// |  _ \ _ __ ___  _ __   ___ _ __| |_ _   _
// | |_) | '__/ _ \| '_ \ / _ \ '__| __| | | |
// |  __/| | | (_) | |_) |  __/ |  | |_| |_| |
// |_|   |_|  \___/| .__/ \___|_|   \__|\__, |
//                 |_|                  |___/

#if FABLE_COMPILER

type Property =
  { Key: string; Value: string }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let key, value =
        builder.CreateString self.Key, builder.CreateString self.Value
    EnumPropertyFB.StartEnumPropertyFB(builder)
    EnumPropertyFB.AddKey(builder, key)
    EnumPropertyFB.AddValue(builder, value)
    EnumPropertyFB.EndEnumPropertyFB(builder)

  static member FromFB(fb: EnumPropertyFB) =
    { Key = fb.Key; Value = fb.Value }
    |> Either.succeed

#else

open SharpYaml.Serialization

type PropertyYaml(key, value) as self =
  [<DefaultValue>] val mutable Key   : string
  [<DefaultValue>] val mutable Value : string

  new () = new PropertyYaml(null, null)

  do
    self.Key <- key
    self.Value <- value

and Property =
  { Key: string; Value: string }

  member self.ToYamlObject() =
    new PropertyYaml(self.Key, self.Value)

  static member FromYamlObject(yml: PropertyYaml) : Either<IrisError,Property> =
    try
      { Key = yml.Key; Value = yml.Value }
      |> Either.succeed
    with
      | exn ->
        ("Property.FromYamlObject",sprintf "Could not parse PropteryYaml: %s" exn.Message)
        |> ParseError
        |> Either.fail

  member self.ToOffset(builder: FlatBufferBuilder) =
    let key, value =
        builder.CreateString self.Key, builder.CreateString self.Value
    EnumPropertyFB.StartEnumPropertyFB(builder)
    EnumPropertyFB.AddKey(builder, key)
    EnumPropertyFB.AddValue(builder, value)
    EnumPropertyFB.EndEnumPropertyFB(builder)

  static member FromFB(fb: EnumPropertyFB) =
    { Key = fb.Key; Value = fb.Value }
    |> Either.succeed

    #endif
