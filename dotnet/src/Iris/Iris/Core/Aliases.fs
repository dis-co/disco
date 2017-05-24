namespace Iris.Core

// * Units

[<Measure>] type filepath
[<Measure>] type projectpath
[<Measure>] type name
[<Measure>] type password
[<Measure>] type checksum
[<Measure>] type timestamp
[<Measure>] type frame
[<Measure>] type sec                    // seconds
[<Measure>] type ms                     // milliseconds
[<Measure>] type ns                     // nanoseconds
[<Measure>] type us                     // microseconds
[<Measure>] type fps = frame/sec
[<Measure>] type email
[<Measure>] type index
[<Measure>] type term
[<Measure>] type port
[<Measure>] type chars
[<Measure>] type version
[<Measure>] type tag
[<Measure>] type uri

// * Aliases

//     _    _ _
//    / \  | (_) __ _ ___  ___  ___
//   / _ \ | | |/ _` / __|/ _ \/ __|
//  / ___ \| | | (_| \__ \  __/\__ \
// /_/   \_\_|_|\__,_|___/\___||___/

type NodeId     = Id
type MemberId   = Id
type Index      = int<index>
type Term       = int<term>
type Name       = string<name>
type Email      = string<email>
type Tag        = string<tag>
type NodePath   = string
type OSCAddress = string
type Version    = string<version>
type Min        = int    option
type Max        = int    option
type Unit       = string option
type Filemask   = string option
type Precision  = int    option
type MaxChars   = int<chars>
type FilePath   = string<filepath>
type UserName   = string<name>
type UserAgent  = string
type TimeStamp  = string
type CallSite   = string
type FileName   = string
type Hash       = string<checksum>
type Password   = string<password>
type Salt       = string<checksum>
type Port       = uint16<port>
type Timeout    = int<ms>
type Url        = string<uri>

// * Measure module

[<AutoOpen>]
module Measure =
  let filepath p: FilePath = UoM.wrap p
  let projectpath p: ProjectPath = UoM.wrap p
  let name u: Name = UoM.wrap u
  let password p: Password = UoM.wrap p
  let timestamp t: TimeStamp = UoM.wrap t
  let email e: Email = UoM.wrap e
  let port p: Port = UoM.wrap p
  let checksum t: Hash = UoM.wrap t
  let index i: Index = i * 1<index>
  let term t: Term = t * 1<term>
  let version v: Version = UoM.wrap v
  let astag t: Tag = UoM.wrap t
  let url t: Url = UoM.wrap t

// * IPProtocol

type IPProtocol =
  | IPv4
  | IPv6

// * Actor<'t>

type Actor<'t> = MailboxProcessor<'t>

// * StringPayload

type StringPayload = Payload of string

// * Coordinate

/// ## Coordinate
///
/// Represents a point in Euclidian space
///
type Coordinate = Coordinate of (int * int) with

  override self.ToString() =
    match self with
    | Coordinate (x, y) -> "(" + string x + ", " + string y + ")"

  member self.X
    with get () =
      match self with
      | Coordinate (x,_) -> x

  member self.Y
    with get () =
      match self with
      | Coordinate (_,y) -> y

// * Rect

/// ## Rect
///
/// Represents a rectangle in by width * height
///
type Rect = Rect of (int * int) with

  override self.ToString() =
    match self with
    | Rect (x, y) -> "(" + string x + ", " + string y + ")"

  member self.X
    with get () =
      match self with
      | Rect (x,_) -> x

  member self.Y
    with get () =
      match self with
      | Rect (_,y) -> y

// * ServiceStatus

#if FABLE_COMPILER

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open Iris.Serialization
open FlatBuffers

#endif

//  ____                  _          ____  _        _
// / ___|  ___ _ ____   _(_) ___ ___/ ___|| |_ __ _| |_ _   _ ___
// \___ \ / _ \ '__\ \ / / |/ __/ _ \___ \| __/ _` | __| | | / __|
//  ___) |  __/ |   \ V /| | (_|  __/___) | || (_| | |_| |_| \__ \
// |____/ \___|_|    \_/ |_|\___\___|____/ \__\__,_|\__|\__,_|___/

[<RequireQualifiedAccess>]
type ServiceStatus =
  | Starting
  | Running
  | Stopping
  | Stopped
  | Degraded of IrisError
  | Failed   of IrisError
  | Disposed

  // ** ToString

  override self.ToString() =
    match self with
    | Starting     -> "Starting"
    | Running      -> "Running"
    | Stopping     -> "Stopping"
    | Stopped      -> "Stopped"
    | Degraded err -> sprintf "Degraded %A" err
    | Failed   err -> sprintf "Failed %A" err
    | Disposed     -> "Disposed"

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    match self with
    | Starting ->
      ServiceStatusFB.StartServiceStatusFB(builder)
      ServiceStatusFB.AddType(builder, ServiceStatusTypeFB.StartingFB)
      ServiceStatusFB.EndServiceStatusFB(builder)
    | Running ->
      ServiceStatusFB.StartServiceStatusFB(builder)
      ServiceStatusFB.AddType(builder, ServiceStatusTypeFB.RunningFB)
      ServiceStatusFB.EndServiceStatusFB(builder)
    | Stopping ->
      ServiceStatusFB.StartServiceStatusFB(builder)
      ServiceStatusFB.AddType(builder, ServiceStatusTypeFB.StoppingFB)
      ServiceStatusFB.EndServiceStatusFB(builder)
    | Stopped ->
      ServiceStatusFB.StartServiceStatusFB(builder)
      ServiceStatusFB.AddType(builder, ServiceStatusTypeFB.StoppedFB)
      ServiceStatusFB.EndServiceStatusFB(builder)
    | Degraded error ->
      let offset = error.ToOffset(builder)
      ServiceStatusFB.StartServiceStatusFB(builder)
      ServiceStatusFB.AddType(builder, ServiceStatusTypeFB.DegradedFB)
      ServiceStatusFB.AddError(builder, offset)
      ServiceStatusFB.EndServiceStatusFB(builder)
    | Failed error ->
      let offset = error.ToOffset(builder)
      ServiceStatusFB.StartServiceStatusFB(builder)
      ServiceStatusFB.AddType(builder, ServiceStatusTypeFB.FailedFB)
      ServiceStatusFB.AddError(builder, offset)
      ServiceStatusFB.EndServiceStatusFB(builder)
    | Disposed ->
      ServiceStatusFB.StartServiceStatusFB(builder)
      ServiceStatusFB.AddType(builder, ServiceStatusTypeFB.DisposedFB)
      ServiceStatusFB.EndServiceStatusFB(builder)

  // ** FromFB

  static member FromFB (fb: ServiceStatusFB) =
    either {
      return!
        #if FABLE_COMPILER

        match fb.Type with
        | x when x = ServiceStatusTypeFB.RunningFB  -> Right Running
        | x when x = ServiceStatusTypeFB.StartingFB -> Right Starting
        | x when x = ServiceStatusTypeFB.StoppingFB -> Right Stopping
        | x when x = ServiceStatusTypeFB.StoppedFB  -> Right Stopped
        | x when x = ServiceStatusTypeFB.DisposedFB -> Right Disposed
        | x when x = ServiceStatusTypeFB.DegradedFB ->
          fb.Error |> IrisError.FromFB |> Either.map Degraded
        | x when x = ServiceStatusTypeFB.FailedFB ->
          fb.Error |> IrisError.FromFB |> Either.map Failed
        | other ->
          other
          |> sprintf "could not parse empty Error payload: %O"
          |> Error.asParseError "ServiceStatus.FromFB"
          |> Either.fail

        #else

        match fb.Type with
        | ServiceStatusTypeFB.RunningFB -> Right Running
        | ServiceStatusTypeFB.StartingFB -> Right Starting
        | ServiceStatusTypeFB.StoppingFB -> Right Stopping
        | ServiceStatusTypeFB.StoppedFB -> Right Stopped
        | ServiceStatusTypeFB.DisposedFB -> Right Disposed
        | ServiceStatusTypeFB.DegradedFB ->
          let valueish = fb.Error
          if valueish.HasValue then
            let value = valueish.Value
            IrisError.FromFB value
            |> Either.map Degraded
          else
            "could not parse empty Error payload"
            |> Error.asParseError "ServiceStatus.FromFB"
            |> Either.fail
        | ServiceStatusTypeFB.FailedFB ->
          let valueish = fb.Error
          if valueish.HasValue then
            let value = valueish.Value
            IrisError.FromFB value
            |> Either.map Failed
          else
            "could not parse empty Error payload"
            |> Error.asParseError "ServiceStatus.FromFB"
            |> Either.fail
        | other ->
          other
          |> sprintf "could not parse empty Error payload: %O"
          |> Error.asParseError "ServiceStatus.FromFB"
          |> Either.fail
        #endif
    }

// * Service module

[<RequireQualifiedAccess>]
module Service =

  let isRunning = function
    | ServiceStatus.Running -> true
    | _                     -> false

  let isStopping = function
    | ServiceStatus.Stopping -> true
    | _                      -> false

  let isStopped = function
    | ServiceStatus.Stopped -> true
    | _                     -> false

  let isDisposed = function
    | ServiceStatus.Disposed -> true
    | _                      -> false

  let hasFailed = function
    | ServiceStatus.Failed _ -> true
    |                      _ -> false
