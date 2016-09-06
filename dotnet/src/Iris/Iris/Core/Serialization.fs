namespace Iris.Core

// open System.Runtime.CompilerServices
// open Iris.Serialization.Raft
// open Iris.Raft

open FlatBuffers

type IFlatBufferable =
  abstract ToOffset : FlatBufferBuilder -> Offset<'a>

//  ____            _       _ _          _   _
// / ___|  ___ _ __(_) __ _| (_)______ _| |_(_) ___  _ __
// \___ \ / _ \ '__| |/ _` | | |_  / _` | __| |/ _ \| '_ \
//  ___) |  __/ |  | | (_| | | |/ / (_| | |_| | (_) | | | |
// |____/ \___|_|  |_|\__,_|_|_/___\__,_|\__|_|\___/|_| |_|

[<AutoOpen>]
module Serialization =

  let inline encode (value : ^t when ^t : (member ToBytes : unit -> byte array)) =
    (^t : (member ToBytes : unit -> byte array) value)


  let inline decode< ^t when ^t : (static member FromBytes : byte array -> ^t option)>
                                  (bytes: byte array) :
                                  ^t option =
    (^t : (static member FromBytes : byte array -> ^t option) bytes)
