(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.JS

// ------------------ 8< ------------------
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
    // abstract prototype: ByteBuffer with get, set

    [<Emit("new flatbuffers.ByteBuffer($1)")>]
    abstract Create: bytes: byte[] -> ByteBuffer

  and [<Erase>] Offset<'a> = Offset of int

  //  ____        _ _     _
  // | __ ) _   _(_) | __| | ___ _ __
  // |  _ \| | | | | |/ _` |/ _ \ '__|
  // | |_) | |_| | | | (_| |  __/ |
  // |____/ \__,_|_|_|\__,_|\___|_|

  and FlatBufferBuilder =
    [<Emit("new Uint8Array($0.asUint8Array().buffer.slice($0.dataBuffer().position(),$0.dataBuffer().position() + $0.offset()))")>]
    abstract SizedByteArray: unit -> byte[]

    [<Emit("$0.createString($1)")>]
    abstract CreateString: string -> Offset<string>

    [<Emit("$0.finish($1)")>]
    abstract Finish: Offset<'a> -> unit

  and FlatBufferBuilderConstructor =
    // abstract prototype: FlatBufferBuilder with get, set

    [<Emit("new flatbuffers.Builder($1)")>]
    abstract Create: size: int -> FlatBufferBuilder

  let ByteBuffer: ByteBufferConstructor = createEmpty
  let FlatBufferBuilder: FlatBufferBuilderConstructor = createEmpty
