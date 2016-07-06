namespace Iris.Core

//  ____            _                                     _
// |  _ \ ___ _ __ | | __ _  ___ ___ _ __ ___   ___ _ __ | |_ ___
// | |_) / _ \ '_ \| |/ _` |/ __/ _ \ '_ ` _ \ / _ \ '_ \| __/ __|
// |  _ <  __/ |_) | | (_| | (_|  __/ | | | | |  __/ | | | |_\__ \
// |_| \_\___| .__/|_|\__,_|\___\___|_| |_| |_|\___|_| |_|\__|___/
//           |_|

#if JAVASCRIPT
[<AutoOpen>]
module Replacements =
  open Fable.Core

  [<Emit("return $0")>]
  let uint8 (_: 't) : uint8 = failwith "ONLY IN JS"

  [<Emit("return 0")>]
  let sizeof<'t> : int = failwith "ONLY IN JS"
#endif

//     _    _ _
//    / \  | (_) __ _ ___  ___  ___
//   / _ \ | | |/ _` / __|/ _ \/ __|
//  / ___ \| | | (_| \__ \  __/\__ \
// /_/   \_\_|_|\__,_|___/\___||___/

type Id         = Guid
type Long       = uint64
type Index      = Long
type Name       = string
type Tag        = string
type NodePath   = string
type OSCAddress = string
type Version    = string
type VectorSize = int    option
type Min        = int    option
type Max        = int    option
type Unit       = string option
type FileMask   = string option
type Precision  = int    option
type MaxChars   = int
type FilePath   = string
type Property   = (string * string)
type UserName   = string
type UserAgent  = string

type ClientLog = string
type ProjectId = Id
type MemberId  = Id
type SessionId = Id
type Session   = string
type Error     = string

type Actor<'t> = MailboxProcessor<'t>

/// ## Coordinate
///
/// Represents a point in Euclidian space
///
type Coordinate = Coordinate of (int * int)
  with
    override self.ToString() =
      match self with
      | Coordinate (x, y) -> "(" + string x + ", " + string y + ")"

/// ## Rect
///
/// Represents a rectangle in by width * height
///
type Rect = Rect of (int * int)
  with
    override self.ToString() =
      match self with
      | Rect (x, y) -> "(" + string x + ", " + string y + ")"

//   ____      _
//  / ___|___ | | ___  _ __ ___
// | |   / _ \| |/ _ \| '__/ __|
// | |__| (_) | | (_) | |  \__ \
//  \____\___/|_|\___/|_|  |___/

[<Struct>]
type RGBAValue =
  val Red   : uint8
  val Green : uint8
  val Blue  : uint8
  val Alpha : uint8

  new (r,g,b) =
    { Red   = r
    ; Green = g
    ; Blue  = b
    ; Alpha = uint8 255 }

  new (r,g,b,a) =
    { Red   = r
    ; Green = g
    ; Blue  = b
    ; Alpha = a }

[<Struct>]
type HSLAValue =
  val Hue        : uint8
  val Saturation : uint8
  val Lightness  : uint8
  val Alpha      : uint8

  new (h,s,l) =
    { Hue        = h
    ; Saturation = s
    ; Lightness  = l
    ; Alpha      = uint8 255 }


  new (h,s,l,a) =
    { Hue        = h
    ; Saturation = s
    ; Lightness  = l
    ; Alpha      = a }

type ColorSpace =
  | RGBA of RGBAValue
  | HSLA of HSLAValue
