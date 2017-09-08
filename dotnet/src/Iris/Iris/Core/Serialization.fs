namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers

#else

open FlatBuffers

#endif

// * EitherExt module

[<RequireQualifiedAccess>]
module EitherExt =

  let bindGeneratorToArray loc length generator (f: 'a -> Either<IrisError,'b>) =
    let mutable i = 0
    let mutable error = None
    let arr = Array.zeroCreate length
    while i < arr.Length && Option.isNone error do
      #if !FABLE_COMPILER
      let item: System.Nullable<'a> = generator i
      if not item.HasValue then
        error <- ParseError(loc, "Could not parse empty item") |> Some
      else
        let item = item.Value
      #else
        let item = generator i
      #endif
        match f item with
        | Right value -> arr.[i] <- value; i <- i + 1
        | Left err -> error <- Some err
    match error with
    | Some err -> Left err
    | None -> Right arr

// * Binary module

//  ____  _
// | __ )(_)_ __   __ _ _ __ _   _
// |  _ \| | '_ \ / _` | '__| | | |
// | |_) | | | | | (_| | |  | |_| |
// |____/|_|_| |_|\__,_|_|   \__, |
//                           |___/

[<RequireQualifiedAccess>]
module Binary =

  // ** createBuffer

  let createBuffer (bytes: byte[]) : ByteBuffer =
#if FABLE_COMPILER
    ByteBuffer.Create(bytes)
#else
    ByteBuffer(bytes)
#endif

  // ** encode

  let inline encode (value : ^t when ^t : (member ToBytes : unit -> byte[])) =
    (^t : (member ToBytes : unit -> byte[]) value)

  // ** decode

  let inline decode< ^t when ^t : (static member FromBytes : byte[] -> Either<IrisError, ^t>)>
                                  (bytes: byte[]) :
                                  Either<IrisError, ^t > =
    try
      (^t : (static member FromBytes : byte[] -> Either<IrisError, ^t>) bytes)
    with
      | exn ->
        ((typeof< ^t >).Name + ".FromBytes", exn.Message)
        |> ParseError
        |> Either.fail

  // ** toOffset

  let inline toOffset< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)>
                     (builder: FlatBufferBuilder)
                     (thing: ^a)
                     : Offset< ^t > =
    (^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >) (thing,builder))

  // ** buildBuffer

  let inline buildBuffer< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)> (thing: ^a) : byte[] =
#if FABLE_COMPILER
    let builder = FlatBufferBuilder.Create(1)
    let offset = toOffset builder thing
    builder.Finish(offset)
    builder.SizedByteArray()
#else
    let builder = FlatBufferBuilder(1)
    let offset = toOffset builder thing
    builder.Finish(offset.Value)
    builder.SizedByteArray()
#endif

// * Yaml module

// __   __              _
// \ \ / /_ _ _ __ ___ | |
//  \ V / _` | '_ ` _ \| |
//   | | (_| | | | | | | |
//   |_|\__,_|_| |_| |_|_|

#if !FABLE_COMPILER && !IRIS_NODES

[<RequireQualifiedAccess>]
module Yaml =
  open SharpYaml.Serialization

  // ** serialize

  let serialize (thing: obj) =
    let options = SerializerSettings()
    /// options.EmitTags <- false
    /// options.EmitAlias <- false
    let serializer = Serializer(options)
    serializer.Serialize thing

  // ** deserialize

  let inline deserialize< ^t > (str: string) : ^t =
    let serializer = Serializer()
    serializer.Deserialize< ^t >(str)

  // ** toYaml

  let inline toYaml< ^a, ^t when ^t : (member ToYaml : unit -> ^a)> (thing: ^t) : ^a =
    (^t : (member ToYaml : unit -> ^a) thing)

  // ** fromYaml

  let inline fromYaml< ^err, ^a, ^t when ^t : (static member FromYaml : ^a -> Either< ^err, ^t >)>
                     (thing: ^a) =
    (^t : (static member FromYaml : ^a -> Either< ^err, ^t >) thing)

  // ** encode

  let inline encode< ^a, ^t when ^t : (member ToYaml : unit -> ^a)> (thing: ^t) =
    thing |> toYaml |> serialize

  // ** decode

  let inline decode< ^err, ^a, ^t when ^t : (static member FromYaml: ^a -> Either< ^err, ^t >)>
                   (str: string) =
    let thing = str |> deserialize< ^a >
    (^t : (static member FromYaml : ^a -> Either< ^err, ^t >) thing)

#endif
