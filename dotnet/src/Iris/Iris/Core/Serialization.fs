namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Fable.Import
open Fable.Import.JS
open Iris.Core.FlatBuffers

#else

open FlatBuffers

#endif

//  ____  _
// | __ )(_)_ __   __ _ _ __ _   _
// |  _ \| | '_ \ / _` | '__| | | |
// | |_) | | | | | (_| | |  | |_| |
// |____/|_|_| |_|\__,_|_|   \__, |
//                           |___/

[<RequireQualifiedAccess>]
module Binary =
#if JAVASCRIPT
  type Buffer = ArrayBuffer
#else
  type Buffer = byte array
#endif

  let createBuffer (bytes: Buffer) : ByteBuffer =
#if JAVASCRIPT
    ByteBuffer.Create(bytes)
#else
    new ByteBuffer(bytes)
#endif

  let inline encode (value : ^t when ^t : (member ToBytes : unit -> Buffer)) =
    (^t : (member ToBytes : unit -> Buffer) value)

  let inline decode< ^t when ^t : (static member FromBytes : Buffer -> ^t option)>
                                  (bytes: Buffer) :
                                  ^t option =
    (^t : (static member FromBytes : Buffer -> ^t option) bytes)

  let inline toOffset< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)>
                     (builder: FlatBufferBuilder)
                     (thing: ^a)
                     : Offset< ^t > =
    (^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >) (thing,builder))

  let inline buildBuffer< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)> (thing: ^a) : Buffer =
#if JAVASCRIPT
    let builder = FlatBufferBuilder.Create(1)
    let offset = toOffset builder thing
    builder.Finish(offset)
    builder.SizedByteArray()
#else
    let builder = new FlatBufferBuilder(1)
    let offset = toOffset builder thing
    builder.Finish(offset.Value)
    builder.SizedByteArray()
#endif

// __   __              _
// \ \ / /_ _ _ __ ___ | |
//  \ V / _` | '_ ` _ \| |
//   | | (_| | | | | | | |
//   |_|\__,_|_| |_| |_|_|

#if JAVSCRIPT
#else

[<RequireQualifiedAccess>]
module Yaml =
  open SharpYaml.Serialization

  let inline encode< ^t when ^t : (member ToYaml : Serializer -> string)> (thing: ^t) =
    let serializer = new Serializer()
    (^t : (member ToYaml : Serializer -> string) thing,serializer)

  let inline decode< ^t when ^t : (static member FromYaml : string -> ^t option)> (str: string) =
    (^t : (static member FromYaml : string -> ^t option) str)

#endif
