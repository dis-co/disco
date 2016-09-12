namespace Iris.Core

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
type Email      = string
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


//  ____                            _
// |  _ \ _ __ ___  _ __   ___ _ __| |_ _   _
// | |_) | '__/ _ \| '_ \ / _ \ '__| __| | | |
// |  __/| | | (_) | |_) |  __/ |  | |_| |_| |
// |_|   |_|  \___/| .__/ \___|_|   \__|\__, |
//                 |_|                  |___/

#if JAVASCRIPT
#else

open Newtonsoft.Json
open Newtonsoft.Json.Linq

#endif

type Property =
  { Key: string; Value: string }

#if JAVASCRIPT
#else

  static member Type
    with get () = Serialization.GetTypeName<Property>()

  member self.ToJToken() =
    JToken.FromObject self

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : Property option =
    try
      { Key   = string token.["Key"]
      ; Value = string token.["Value"]
      } |> Some
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : Property option =
    JObject.Parse(str) |> Property.FromJToken

#endif
