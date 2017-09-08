namespace rec Iris.Core

// * Imports

open System

#if FABLE_COMPILER

open Fable.Core
open Fable.Core.JsInterop


// * Date

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

// * Replacements (Fable)

//  _____  _  _
// |  ___|| || |_
// | |_ |_  ..  _|
// |  _||_      _|
// |_|    |_||_|

[<AutoOpen>]
module Replacements =

  // * uint8

  [<Emit("return $0")>]
  let uint8 (_: 't) : uint8 = failwith "ONLY IN JS"

  // * sizeof

  [<Emit("return 0")>]
  let sizeof<'t> : int = failwith "ONLY IN JS"

  // * encodeBase16

  [<Emit("($0).toString(16)")>]
  let inline encodeBase16 (_: ^a) : string = failwith "ONLY IN JS"

  // * charCodeAt

  [<Emit("($0).charCodeAt($1)")>]
  let charCodeAt (_: string) (_: int) = failwith "ONLY IN JS"

  // * substr

  [<Emit("($1).substring($0)")>]
  let substr (_: int) (_: string) : string = failwith "ONLY IN JS"

// * JsUtilities (Fable)

[<AutoOpen>]
module JsUtilities =

  // ** hashCode

  let hashCode (str: string) : int =
    let mutable hash = 0
    for n in  0 .. str.Length - 1 do
      let code = charCodeAt str n
      hash <- ((hash <<< 5) - hash) + code
      hash <- hash ||| 0
    hash

  // ** mkGuid

  let mkGuid _ =
    let s4 _ =
      float ((1 + Math.random()) * 65536)
      |> Math.floor
      |> encodeBase16
      |> substr 1

    [| for _ in 0 .. 3 do yield s4() |]
    |> Array.fold (fun m str -> m + "-" + str) (s4())

// * Id (Fable)

open System.Text.RegularExpressions

type Id =
  | Id of string

  // ** toString

  member self.toString() = toJson self

  // ** ToString

  override self.ToString() = match self with | Id str -> str

  // ** Create

  static member Create _ = mkGuid () |> Id

  // ** Prefix

  member id.Prefix
    with get () =
      let str = string id
      let m = Regex.Match(str, "^([a-zA-Z0-9]{8})-") // match the first 8 chars of a guid
      if m.Success
      then m.Groups.[1].Value           // use the first block of characters
      else str                          // just use the string as-is

#else

// * Id (.NET)

//    _   _ _____ _____
//   | \ | | ____|_   _|
//   |  \| |  _|   | |
//  _| |\  | |___  | |
// (_)_| \_|_____| |_|

open System.Text.RegularExpressions

[<MeasureAnnotatedAbbreviation>]
type Id<[<Measure>] 'Measure> = Id
type Id =
  | Id of string

  // ** ToString

  override id.ToString() =
    match id with | Id str -> str

  // ** Prefix

  member id.Prefix
    with get () =
      let str = string id
      let m = Regex.Match(str, "^([a-zA-Z0-9]{8})-") // match the first 8 chars of a guid
      if m.Success
      then m.Groups.[1].Value           // use the first block of characters
      else str                          // just use the string as-is

  // ** Parse

  static member Parse (str: string) = Id str

  // ** TryParse

  static member TryParse (str: string) = Id str |> Some

  // ** Create

  /// ## Create
  ///
  /// Create a new Guid.
  ///
  /// ### Signature:
  /// - unit: .
  ///
  /// Returns: Guid
  static member Create() =
    System.Guid.NewGuid()
    |> string
    |> Id

  // ** IsUoM

  static member IsUoM(_ : Id, _ : Id<'Measure>) = ()

#endif

// * NewId

module IrisId =
  open FlatBuffers
  open Iris.Serialization

  [<Struct>]
  type IrisId private (guid: Guid) =
    member private self.Value with get () = guid

    static member Create() =
      IrisId(Guid.NewGuid())

    static member FromGuid(guid) =
      IrisId(guid)

    member self.ToByteArray() =
      guid.ToByteArray()

    static member FromByteArray(bytes: byte array) =
      IrisId(Guid bytes)

    static member Parse(str: string) =
      IrisId(Guid.Parse str)

    static member TryParse(str: string) =
      try IrisId.Parse str |> Some
      with exn -> None

    override id.ToString() =
      string id.Value

  let encodeWith (t: FlatBufferBuilder * byte[] -> VectorOffset) builder (id: IrisId) =
    t(builder, id.ToByteArray())

  let decodeWith (t: int -> byte) =
      [| 0 .. 15 |]
      |> Array.map t
      |> IrisId.FromByteArray
      |> Either.succeed

  let encodeId builder id =
    encodeWith TestBytesFB.CreateIdVector builder id

  let inline decodeId (fb: ^t) =
    (fun idx -> (^t : (member Id: int -> byte) fb, idx))
    |> decodeWith

#if INTERACTIVE

open Iris.Core
open Iris.Core.IrisId
open FlatBuffers
open Iris.Serialization

let encode (id: IrisId) =
  let builder = FlatBufferBuilder(1)
  let vec = encodeId builder id
  TestBytesFB.StartTestBytesFB(builder)
  TestBytesFB.AddId(builder, vec)
  let offset = TestBytesFB.EndTestBytesFB(builder)
  builder.Finish(offset.Value)
  builder.SizedByteArray()

let decode (raw: byte[]) =
  raw
  |> Binary.createBuffer
  |> TestBytesFB.GetRootAsTestBytesFB
  |> decodeId

let id = IrisId.Create()
let reid = id |> encodeBytes |> decodeBytes |> Either.get

id = reid

#endif
