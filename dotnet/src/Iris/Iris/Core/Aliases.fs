namespace Iris.Core

open System.Text.RegularExpressions

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
      self.ToString() |> hashCode

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
