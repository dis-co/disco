(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace rec Disco.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System
open System.Text
open FlatBuffers
open Disco.Serialization

#endif

// * PropertyYaml

#if !FABLE_COMPILER && !DISCO_NODES

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

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  member self.ToYaml() =
    PropertyYaml(self.Key, self.Value)

  // ** FromYaml

  static member FromYaml(yml: PropertyYaml) : Either<DiscoError,Property> =
    try
      { Key = yml.Key; Value = yml.Value }
      |> Either.succeed
    with
      | exn ->
        ("Property.FromYaml",sprintf "Could not parse PropteryYaml: %s" exn.Message)
        |> ParseError
        |> Either.fail

  #endif
