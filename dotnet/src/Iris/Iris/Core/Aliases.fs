namespace Iris.Core

// * Custom Units

[<Measure>] type filepath
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

[<AutoOpen>]
module Measure =
  let filepath p: FilePath = UoM.wrap p
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

type IPProtocol =
  | IPv4
  | IPv6

type Actor<'t> = MailboxProcessor<'t>

type StringPayload = Payload of string

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

  override self.ToString() =
    match self with
    | Starting     -> "Starting"
    | Running      -> "Running"
    | Stopping     -> "Stopping"
    | Stopped      -> "Stopped"
    | Degraded err -> sprintf "Degraded %A" err
    | Failed   err -> sprintf "Failed %A" err

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

  let hasFailed = function
    | ServiceStatus.Failed _ -> true
    |                      _ -> false
