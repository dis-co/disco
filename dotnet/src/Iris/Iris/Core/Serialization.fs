namespace Iris.Core

// open System.Runtime.CompilerServices
// open Iris.Serialization.Raft
// open Iris.Raft

open FlatBuffers

//  ____            _       _ _          _   _
// / ___|  ___ _ __(_) __ _| (_)______ _| |_(_) ___  _ __
// \___ \ / _ \ '__| |/ _` | | |_  / _` | __| |/ _ \| '_ \
//  ___) |  __/ |  | | (_| | | |/ / (_| | |_| | (_) | | | |
// |____/ \___|_|  |_|\__,_|_|_/___\__,_|\__|_|\___/|_| |_|

[<AutoOpen>]
module Serialization =

  //  _____                     _
  // | ____|_ __   ___ ___   __| | ___
  // |  _| | '_ \ / __/ _ \ / _` |/ _ \
  // | |___| | | | (_| (_) | (_| |  __/
  // |_____|_| |_|\___\___/ \__,_|\___|

  let inline encode (value : ^t when ^t : (member ToBytes : unit -> byte array)) =
    (^t : (member ToBytes : unit -> byte array) value)

  //  ____                     _
  // |  _ \  ___  ___ ___   __| | ___
  // | | | |/ _ \/ __/ _ \ / _` |/ _ \
  // | |_| |  __/ (_| (_) | (_| |  __/
  // |____/ \___|\___\___/ \__,_|\___|

  let inline decode< ^t when ^t : (static member FromBytes : byte array -> ^t option)>
                                  (bytes: byte array) :
                                  ^t option =
    (^t : (static member FromBytes : byte array -> ^t option) bytes)

  let inline buildBuffer< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)> (thing: ^a) : byte array =
    let builder = new FlatBufferBuilder(1)
    let offset = (^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >) (thing, builder))
    builder.Finish(offset.Value)
    builder.SizedByteArray()
