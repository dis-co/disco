namespace Iris.Core

open System.Text.RegularExpressions


//  ____            _                                     _
// |  _ \ ___ _ __ | | __ _  ___ ___ _ __ ___   ___ _ __ | |_ ___
// | |_) / _ \ '_ \| |/ _` |/ __/ _ \ '_ ` _ \ / _ \ '_ \| __/ __|
// |  _ <  __/ |_) | | (_| | (_|  __/ | | | | |  __/ | | | |_\__ \
// |_| \_\___| .__/|_|\__,_|\___\___|_| |_| |_|\___|_| |_|\__|___/
//           |_|

#if JAVASCRIPT
open Fable.Core

//  __  __       _   _
// |  \/  | __ _| |_| |__
// | |\/| |/ _` | __| '_ \
// | |  | | (_| | |_| | | |
// |_|  |_|\__,_|\__|_| |_|


[<RequireQualifiedAccess>]
module Math =

  [<Emit("return Math.random()")>]
  let random _ : int = failwith "ONLY IN JS"

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

  [<Emit("return ($0).toString(16)")>]
  let inline encodeBase16 (_: ^a) : string = failwith "ONLY IN JS"

[<AutoOpen>]
module JsUtilities =

  let mkGuid _ =
    let lut =
      [| 0 .. 255 |]
      |> Array.map
        (fun i ->
          let n = if i < 16 then "0" else ""
          n + encodeBase16 i)

    let d0 = Math.random ()
    let d1 = Math.random ()
    let d2 = Math.random ()
    let d3 = Math.random ()

    lut.[d0 &&& 0xff]                 +
    lut.[d0 >>> 8  &&& 0xff]          +
    lut.[d0 >>> 16 &&& 0xff]          +
    lut.[d0 >>> 24 &&& 0xff]          +
    lut.[d1 &&& 0xff]                 +
    lut.[d1 >>> 8 &&& 0xff]           +
    lut.[d1 >>> 16 &&& 0x0f ||| 0x40] +
    lut.[d1 >>> 24 &&& 0xff]          +
    lut.[d2 &&& 0x3f ||| 0x80]        +
    lut.[d2 >>> 8  &&& 0xff]          +
    lut.[d2 >>> 16 &&& 0xff]          +
    lut.[d2 >>> 24 &&& 0xff]          +
    lut.[d3 &&& 0xff]                 +
    lut.[d3 >>> 8  &&& 0xff]          +
    lut.[d3 >>> 16 &&& 0xff]          +
    lut.[d3 >>> 24 &&& 0xff]

#endif

//   ____       _     _
//  / ___|_   _(_) __| |
// | |  _| | | | |/ _` |
// | |_| | |_| | | (_| |
//  \____|\__,_|_|\__,_|

#if JAVASCRIPT
open Fable.Core

[<Erase>]
[<CustomEquality>]
[<CustomComparison>]
#endif
type Id =
  | Id of string

  with
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
#if JAVASCRIPT
      mkGuid () |> Id
#else
      let sanitize (str: string) =
        Regex.Replace(str, "[\+|\/|\=]","").ToLower()

      let guid = System.Guid.NewGuid()
      guid.ToByteArray()
      |> System.Convert.ToBase64String
      |> sanitize
      |> Id
#endif

#if JAVASCRIPT
    override self.Equals(o) =
      match o with
      | :? Id -> self.ToString() = o.ToString()
      | _     -> false

    override self.GetHashCode() =
      self.ToString().GetHashCode()

    interface System.IComparable with
      member self.CompareTo(o: obj) =
        let me = self.ToString()
        let arr = [| me; o.ToString() |] |> Array.sort

        if Array.findIndex ((=) me) arr = 0 then
          -1
        else
          1
#endif

//     _    _ _
//    / \  | (_) __ _ ___  ___  ___
//   / _ \ | | |/ _` / __|/ _ \/ __|
//  / ___ \| | | (_| \__ \  __/\__ \
// /_/   \_\_|_|\__,_|___/\___||___/

type NodeId     = Id
type Long       = uint64
type Index      = Long
type Term       = Long
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
type TaskId    = Id
type MemberId  = Id
type SessionId = Id
type Session   = string
type Error     = string
type TimeStamp = string

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
