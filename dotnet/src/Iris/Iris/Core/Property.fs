namespace rec Iris.Core

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

// * PropertyYaml

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

type PropertyYaml(key, value) as self =
  [<DefaultValue>] val mutable Key   : string
  [<DefaultValue>] val mutable Value : string

  new () = PropertyYaml(null, null)

  do
    self.Key <- key
    self.Value <- value

#endif

// * Property

//  ____                            _
// |  _ \ _ __ ___  _ __   ___ _ __| |_ _   _
// | |_) | '__/ _ \| '_ \ / _ \ '__| __| | | |
// |  __/| | | (_) | |_) |  __/ |  | |_| |_| |
// |_|   |_|  \___/| .__/ \___|_|   \__|\__, |
//                 |_|                  |___/

type Property =
  { Key: string; Value: string }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let key = if isNull self.Key then None else Some (builder.CreateString self.Key)
    let value = if isNull self.Value then None else Some (builder.CreateString self.Value)
    KeyValueFB.StartKeyValueFB(builder)
    Option.iter (fun data -> KeyValueFB.AddKey(builder, data)) key
    Option.iter (fun data -> KeyValueFB.AddValue(builder, data)) value
    KeyValueFB.EndKeyValueFB(builder)

  // ** FromOffset

  static member FromFB(fb: KeyValueFB) =
    { Key = fb.Key; Value = fb.Value }
    |> Either.succeed

  // ** ToYamlObject

  #if !FABLE_COMPILER && !IRIS_NODES

  member self.ToYamlObject() =
    PropertyYaml(self.Key, self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yml: PropertyYaml) : Either<IrisError,Property> =
    try
      { Key = yml.Key; Value = yml.Value }
      |> Either.succeed
    with
      | exn ->
        ("Property.FromYamlObject",sprintf "Could not parse PropteryYaml: %s" exn.Message)
        |> ParseError
        |> Either.fail

  #endif
