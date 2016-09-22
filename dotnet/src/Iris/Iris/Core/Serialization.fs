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
#else
    let builder = new FlatBufferBuilder(1)
#endif
    let offset = toOffset builder thing
    builder.Finish(offset.Value)
    builder.SizedByteArray()
