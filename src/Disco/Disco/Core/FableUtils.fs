namespace Disco.Core

// * FableUtils

[<AutoOpen>]
module FableUtils =

  open Fable.Core
  open Fable.Core.JsInterop

  // ** Date

  [<AutoOpen>]
  module Date =

    [<Emit("new Date())")>]
    type JsDate() =

      [<Emit("$0.getTime()")>]
      member __.GetTime
        with get () : int = failwith "ONLY IN JS"

  // ** Math

  [<RequireQualifiedAccess>]
  module Math =

    [<Emit("Math.random()")>]
    let random _ : int = failwith "ONLY IN JS"

    [<Emit("Math.floor($0)")>]
    let floor (_: float) : int = failwith "ONLY IN JS"

  // ** Replacements (Fable)

  //  _____  _  _
  // |  ___|| || |_
  // | |_ |_  ..  _|
  // |  _||_      _|
  // |_|    |_||_|

  [<AutoOpen>]
  module Replacements =

    // *** uint8

    [<Emit("return $0")>]
    let uint8 (_: 't) : uint8 = failwith "ONLY IN JS"

    // *** sizeof

    [<Emit("return 0")>]
    let sizeof<'t> : int = failwith "ONLY IN JS"

    // *** encodeBase16

    [<Emit("($0).toString(16)")>]
    let inline encodeBase16 (_: ^a) : string = failwith "ONLY IN JS"

    // *** charCodeAt

    [<Emit("($0).charCodeAt($1)")>]
    let charCodeAt (_: string) (_: int) = failwith "ONLY IN JS"

    // *** substr

    [<Emit("($1).substring($0)")>]
    let substr (_: int) (_: string) : string = failwith "ONLY IN JS"


  // ** JsUtilities (Fable)

  [<AutoOpen>]
  module JsUtilities =

    // *** hashCode

    let hashCode (str: string) : int =
      let mutable hash = 0
      for n in  0 .. str.Length - 1 do
        let code = charCodeAt str n
        hash <- ((hash <<< 5) - hash) + code
        hash <- hash ||| 0
      hash
