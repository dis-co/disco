namespace Iris.Core


#if JAVASCRIPT

open Fable.Core

//  __  __       _   _
// |  \/  | __ _| |_| |__
// | |\/| |/ _` | __| '_ \
// | |  | | (_| | |_| | | |
// |_|  |_|\__,_|\__|_| |_|

[<AutoOpen>]
module Date =

  [<Emit("new Date())")>]
  type JsDate() =

    [<Emit("$0.getTime()")>]
    member __.GetTime
      with get () : int = failwith "ONLY IN JS"

[<RequireQualifiedAccess>]
module Math =

  [<Emit("Math.random()")>]
  let random _ : int = failwith "ONLY IN JS"

  [<Emit("Math.floor($0)")>]
  let floor (_: float) : int = failwith "ONLY IN JS"

//  _____  _  _
// |  ___|| || |_
// | |_ |_  ..  _|
// |  _||_      _|
// |_|    |_||_|

[<AutoOpen>]
module Replacements =

  [<Emit("return $0")>]
  let uint8 (_: 't) : uint8 = failwith "ONLY IN JS"

  [<Emit("return 0")>]
  let sizeof<'t> : int = failwith "ONLY IN JS"

  [<Emit("($0).toString(16)")>]
  let inline encodeBase16 (_: ^a) : string = failwith "ONLY IN JS"

  [<Emit("($0).charCodeAt($1)")>]
  let charCodeAt (_: string) (_: int) = failwith "ONLY IN JS"

  [<Emit("($1).substring($0)")>]
  let substr (_: int) (_: string) : string = failwith "ONLY IN JS"

[<AutoOpen>]
module JsUtilities =

  let hashCode (str: string) : int =
    let mutable hash = 0
    for n in  0 .. str.Length - 1 do
      let code = charCodeAt str n
      hash <- ((hash <<< 5) - hash) + code
      hash <- hash ||| 0
    hash

  let mkGuid _ =
    let s4 _ =
      float ((1 + Math.random()) * 65536)
      |> Math.floor
      |> encodeBase16
      |> substr 1

    [| for n in 0 .. 3 do yield s4() |]
    |> Array.fold (fun m str -> m + "-" + str) (s4())

type Id =
  | Id of string

  override self.ToString() = match self with | Id str -> str

  static member Create _ = mkGuid () |> Id

#else

//    _   _ _____ _____
//   | \ | | ____|_   _|
//   |  \| |  _|   | |
//  _| |\  | |___  | |
// (_)_| \_|_____| |_|

open System.Text.RegularExpressions

type Id =
  | Id of string

  override id.ToString() =
    match id with | Id str -> str

  static member Parse (str: string) = Id str

  static member TryParse (str: string) = Id str |> Some

  /// ## Create
  ///
  /// Create a new Guid.
  ///
  /// ### Signature:
  /// - unit: .
  ///
  /// Returns: Guid
  static member Create() =
    let sanitize (str: string) =
      Regex.Replace(str, "[\+|\/|\=]","").ToLower()

    let guid = System.Guid.NewGuid()
    guid.ToByteArray()
    |> System.Convert.ToBase64String
    |> sanitize
    |> Id

#endif
