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

type Id         = string
type IP         = string
type Index      = uint32
type Name       = string
type Tag        = string
type IrisId     = string
type IrisIP     = string
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

type Coordinate = Coordinate of (int * int)
type Rect       = Rect       of (int * int)


type Actor<'t> = MailboxProcessor<'t>

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
