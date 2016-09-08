namespace Iris.Core

// open System.Runtime.CompilerServices
// open Iris.Serialization.Raft
// open Iris.Raft

open FlatBuffers
open Newtonsoft.Json
open Newtonsoft.Json.Linq


//  ____  _
// | __ )(_)_ __   __ _ _ __ _   _
// |  _ \| | '_ \ / _` | '__| | | |
// | |_) | | | | | (_| | |  | |_| |
// |____/|_|_| |_|\__,_|_|   \__, |
//                           |___/

[<RequireQualifiedAccess>]
module Binary =

  let inline encode (value : ^t when ^t : (member ToBytes : unit -> byte array)) =
    (^t : (member ToBytes : unit -> byte array) value)

  let inline decode< ^t when ^t : (static member FromBytes : byte array -> ^t option)>
                                  (bytes: byte array) :
                                  ^t option =
    (^t : (static member FromBytes : byte array -> ^t option) bytes)

  let inline buildBuffer< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)> (thing: ^a) : byte array =
    let builder = new FlatBufferBuilder(1)
    let offset = (^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >) (thing, builder))
    builder.Finish(offset.Value)
    builder.SizedByteArray()


//      _
//     | |___  ___  _ __
//  _  | / __|/ _ \| '_ \
// | |_| \__ \ (_) | | | |
//  \___/|___/\___/|_| |_|

[<RequireQualifiedAccess>]
module Json =

  let inline encode (value: ^t when ^t : (member ToJson : unit -> string)) : string =
    (^t : (member ToJson : unit -> string) value)

  let inline decode< ^t when ^t : (static member FromJson : string -> ^t option)> (str: string) : ^t option =
    (^t : (static member FromJson : string -> ^t option) str)

  let inline tokenize (value: ^t when ^t : (member ToJToken : unit -> JToken)) : JToken =
    (^t : (member ToJToken : unit -> JToken) value)
