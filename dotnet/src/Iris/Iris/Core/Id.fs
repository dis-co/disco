namespace rec Iris.Core

// * Imports

open System

#if FABLE_COMPILER

open Fable.Core
open Fable.Core.JsInterop
open System.Text.RegularExpressions

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


#endif

// * Id

//    _   _ _____ _____
//   | \ | | ____|_   _|
//   |  \| |  _|   | |
//  _| |\  | |___  | |
// (_)_| \_|_____| |_|

open System.Text.RegularExpressions

[<MeasureAnnotatedAbbreviation>]
type Id<[<Measure>] 'Measure> = Id

[<Struct>]
type Id private (guid: Guid) =

  // ** toString

  #if FABLE_COMPILER

  member self.toString() = toJson self

  #endif

  // ** ToString

  override id.ToString() = string guid

  // ** Prefix

  member __.Prefix
    with get () =
      let str = string guid
      let m = Regex.Match(str, "^([a-zA-Z0-9]{8})-") // match the first 8 chars of a guid
      if m.Success
      then m.Groups.[1].Value           // use the first block of characters
      else str                          // just use the string as-is

  // ** Parse

  static member Parse (str: string) = Id (Guid.Parse str)

  // ** TryParse

  static member TryParse (str: string) =
    try Id.Parse str |> Either.succeed
    with exn ->
      exn.Message
      |> Error.asParseError "Id"
      |> Either.fail

  // ** FromGuid

  member __.ToGuid() = guid

  // ** FromGuid

  static member FromGuid(guid) =
    Id(guid)

  // ** ToByteArray

  member __.ToByteArray() =
    guid.ToByteArray()

  // ** FromByteArray

  static member FromByteArray(bytes: byte array) =
    Id(Guid bytes)

  // ** Create

  /// Create a new Guid.
  static member Create() = System.Guid.NewGuid() |> Id

  // ** Empty

  static member Empty = Id(Guid.Empty)

  // ** IsUoM

  static member IsUoM(_ : Id, _ : Id<'Measure>) = ()

// * Id module

module Id =
  open FlatBuffers
  open Iris.Serialization

  let inline encodeSource< ^t when ^t : (static member CreateSourceVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                         (builder: FlatBufferBuilder)
                         (id: Id) =
    (^t : (static member CreateSourceVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeActiveSite< ^t when ^t : (static member CreateActiveSiteVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                             (builder: FlatBufferBuilder)
                             (id: Id) =
    (^t : (static member CreateActiveSiteVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeId< ^t when ^t : (static member CreateIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                     (builder: FlatBufferBuilder)
                     (id: Id) =
    (^t : (static member CreateIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeCueId< ^t when ^t : (static member CreateCueIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                        (builder: FlatBufferBuilder)
                        (id: Id) =
    (^t : (static member CreateCueIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeClientId< ^t when ^t : (static member CreateClientIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                           (builder: FlatBufferBuilder)
                           (id: Id) =
    (^t : (static member CreateClientIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeServiceId< ^t when ^t : (static member CreateServiceIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                            (builder: FlatBufferBuilder)
                            (id: Id) =
    (^t : (static member CreateServiceIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeProjectId< ^t when ^t : (static member CreateProjectIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                            (builder: FlatBufferBuilder)
                            (id: Id) =
    (^t : (static member CreateProjectIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeMachineId< ^t when ^t : (static member CreateMachineIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                            (builder: FlatBufferBuilder)
                            (id: Id) =
    (^t : (static member CreateMachineIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodePinGroupId< ^t when ^t : (static member CreatePinGroupIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                             (builder: FlatBufferBuilder)
                             (id: Id) =
    (^t : (static member CreatePinGroupIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodePinId< ^t when ^t : (static member CreatePinIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                        (builder: FlatBufferBuilder)
                        (id: Id) =
    (^t : (static member CreatePinIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeCallId< ^t when ^t : (static member CreateCallIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                        (builder: FlatBufferBuilder)
                        (id: Id) =
    (^t : (static member CreateCallIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeNextId< ^t when ^t : (static member CreateNextIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                        (builder: FlatBufferBuilder)
                        (id: Id) =
    (^t : (static member CreateNextIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodePreviousId< ^t when ^t : (static member CreatePreviousIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                             (builder: FlatBufferBuilder)
                             (id: Id) =
    (^t : (static member CreatePreviousIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeLastCalledId< ^t when ^t : (static member CreateLastCalledIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                               (builder: FlatBufferBuilder)
                               (id: Id) =
    (^t : (static member CreateLastCalledIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeLastCallerId< ^t when ^t : (static member CreateLastCallerIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                               (builder: FlatBufferBuilder)
                               (id: Id) =
    (^t : (static member CreateLastCallerIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeWidgetType< ^t when ^t : (static member CreateWidgetTypeVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                             (builder: FlatBufferBuilder)
                             (id: Id) =
    (^t : (static member CreateWidgetTypeVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeLeaderId< ^t when ^t : (static member CreateLeaderIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                           (builder: FlatBufferBuilder)
                           (id: Id) =
    (^t : (static member CreateLeaderIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let inline encodeMemberId< ^t when ^t : (static member CreateMemberIdVector: FlatBufferBuilder * byte[] -> VectorOffset)>
                           (builder: FlatBufferBuilder)
                           (id: Id) =
    (^t : (static member CreateMemberIdVector: FlatBufferBuilder * byte[] -> VectorOffset) builder, id.ToByteArray())

  let decodeWith (t: int -> byte) =
    [| 0 .. 15 |]
    |> Array.map t
    |> Id.FromByteArray
    |> Either.succeed

  let inline decodeId (fb: ^t) =
    (fun idx -> (^t : (member Id: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeCueId (fb: ^t) =
    (fun idx -> (^t : (member CueId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeClientId (fb: ^t) =
    (fun idx -> (^t : (member ClientId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeServiceId (fb: ^t) =
    (fun idx -> (^t : (member ServiceId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeProjectId (fb: ^t) =
    (fun idx -> (^t : (member ProjectId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeMachineId (fb: ^t) =
    (fun idx -> (^t : (member MachineId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodePinGroupId (fb: ^t) =
    (fun idx -> (^t : (member PinGroupId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodePinId (fb: ^t) =
    (fun idx -> (^t : (member PinId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeCueListId (fb: ^t) =
    (fun idx -> (^t : (member CueListId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeLastCalledId (fb: ^t) =
    (fun idx -> (^t : (member LastCalledId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeLastCallerId (fb: ^t) =
    (fun idx -> (^t : (member LastCallerId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeCallId (fb: ^t) =
    (fun idx -> (^t : (member CallId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeNextId (fb: ^t) =
    (fun idx -> (^t : (member NextId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodePreviousId (fb: ^t) =
    (fun idx -> (^t : (member PreviousId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeWidgetType (fb: ^t) =
    (fun idx -> (^t : (member WidgetType: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeActiveSite (fb: ^t) =
    (fun idx -> (^t : (member ActiveSite: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeSource (fb: ^t) =
    (fun idx -> (^t : (member Source: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeLeaderId (fb: ^t) =
    (fun idx -> (^t : (member LeaderId: int -> byte) fb, idx))
    |> decodeWith

  let inline decodeMemberId (fb: ^t) =
    (fun idx -> (^t : (member MemberId: int -> byte) fb, idx))
    |> decodeWith
