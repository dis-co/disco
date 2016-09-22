namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Fable.Import
open Fable.Import.JS

// ------------------ 8< ------------------
[<Import("*", from="flatbuffers")>]
module FlatBuffers =

  //  ____        _       ____         __  __
  // | __ ) _   _| |_ ___| __ ) _   _ / _|/ _| ___ _ __
  // |  _ \| | | | __/ _ \  _ \| | | | |_| |_ / _ \ '__|
  // | |_) | |_| | ||  __/ |_) | |_| |  _|  _|  __/ |
  // |____/ \__, |\__\___|____/ \__,_|_| |_|  \___|_|
  //        |___/

  type ByteBuffer =
    abstract bytes: unit -> Fable.Import.JS.Uint8Array

  and ByteBufferConstructor =
    abstract prototype: ByteBuffer with get, set

    [<Emit("new flatbuffers.flatbuffers.$0($1)")>]
    abstract Create: bytes: Fable.Import.JS.Uint8Array -> ByteBuffer

  //   ___   __  __          _
  //  / _ \ / _|/ _|___  ___| |_
  // | | | | |_| |_/ __|/ _ \ __|
  // | |_| |  _|  _\__ \  __/ |_
  //  \___/|_| |_| |___/\___|\__|

  and Offset<'a> =
    abstract Value: unit -> int

  //  ____        _ _     _
  // | __ ) _   _(_) | __| | ___ _ __
  // |  _ \| | | | | |/ _` |/ _ \ '__|
  // | |_) | |_| | | | (_| |  __/ |
  // |____/ \__,_|_|_|\__,_|\___|_|

  and FlatBufferBuilder =
    [<Emit("$0.asUint8Array()")>]
    abstract AsUint8Array: unit -> Fable.Import.JS.Uint8Array

    [<Emit("$0.dataBuffer()")>]
    abstract DataBuffer: unit -> ByteBuffer

    [<Emit("$0.asUint8Array().buffer.slice($0.dataBuffer().position(),$0.dataBuffer().position() + $0.offset())")>]
    abstract SizedByteArray: unit -> Fable.Import.JS.ArrayBuffer

    [<Emit("$0.createString($1)")>]
    abstract CreateString: string -> Offset<string>

    [<Emit("$0.finish($1)")>]
    abstract Finish: Offset<'a> -> unit

  and FlatBufferBuilderConstructor =
    abstract prototype: FlatBufferBuilder with get, set

    [<Emit("new flatbuffers.flatbuffers.Builder($1)")>]
    abstract Create: size: int -> FlatBufferBuilder

  let [<Global>] ByteBuffer: ByteBufferConstructor = failwith "JS only"
  let [<Global>] FlatBufferBuilder: FlatBufferBuilderConstructor = failwith "JS only"

#endif
