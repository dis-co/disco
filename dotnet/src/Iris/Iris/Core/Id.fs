namespace Iris.Core

// * Imports

open System
open System.Text.RegularExpressions

#if FABLE_COMPILER

open Fable.Core
open Fable.Core.JsInterop
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization

#endif

// * IrisId<'Measure>

/// [<MeasureAnnotatedAbbreviation>]
/// type IrisId<[<Measure>] 'Measure> = IrisId

// * IrisId

[<CustomComparison;CustomEquality>]
type IrisId =
  struct
    val Guid: Guid
    new (g: Guid) = { Guid = g }
  end

  // ** ToString

  override id.ToString() = string id.Guid

  // ** Prefix

  member id.Prefix() =
    id.Guid.ToString().[..7]

  // ** Parse

  static member Parse (str: string) = IrisId (Guid.Parse str)

  // ** TryParse

  static member TryParse (str: string) =
    try IrisId.Parse str |> Either.succeed
    with exn ->
      exn.Message
      |> Error.asParseError "IrisId"
      |> Either.fail

  // ** FromGuid

  static member FromGuid(guid) =
    IrisId(guid)

  // ** ToByteArray

  member id.ToByteArray() =
    id.Guid.ToByteArray()

  // ** FromByteArray

  static member FromByteArray(bytes: byte array) =
    IrisId(Guid bytes)

  // ** Create

  /// Create a new Guid.
  static member Create() = System.Guid.NewGuid() |> IrisId

  // ** Empty

  static member Empty = IrisId(Guid.Empty)

  // ** IsUoM

  /// static member IsUoM(_ : IrisId, _ : IrisId<'Measure>) = ()

  // ** GetHashCode

  override id.GetHashCode() = id.Guid.GetHashCode()

  // ** Equals

  override self.Equals(o: obj) =
    match o with
    | :? IrisId as id -> (self :> System.IEquatable<IrisId>).Equals id
    | _ -> false

  // ** IEquatable.Equals

  interface System.IEquatable<IrisId> with
    member id.Equals(other: IrisId) =
      id.Guid = other.Guid

  // ** IComparable.CompareTo

  interface System.IComparable with
    member id.CompareTo(o: obj) =
      match o with
      | :? IrisId as other -> compare id.Guid (other.Guid)
      | _ -> 0

// * Id module

module Id =

  let create() = IrisId.Create()

  let decodeWith (t: int -> byte) =
    [| 0 .. 15 |]
    |> Array.map t
    |> IrisId.FromByteArray
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

// * Playground

#if INTERACTIVE

type MyId = struct
  val mutable Guid: System.Guid
  new (g: System.Guid) = { Guid = g }
end

let g = System.Guid.NewGuid()

let g1 = MyId(g)
let g2 = MyId(g)

g1 = g2

#endif
