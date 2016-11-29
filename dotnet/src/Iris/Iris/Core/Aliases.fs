namespace Iris.Core

//     _    _ _
//    / \  | (_) __ _ ___  ___  ___
//   / _ \ | | |/ _` / __|/ _ \/ __|
//  / ___ \| | | (_| \__ \  __/\__ \
// /_/   \_\_|_|\__,_|___/\___||___/

type NodeId     = Id
type Long       = uint32
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
type ClientLog  = string
type TimeStamp  = string
type CallSite   = string

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

#if FABLE_COMPILER

type Property =
  { Key: string; Value: string }

#else

open SharpYaml.Serialization

type PropertyYaml(key, value) as self =
  [<DefaultValue>] val mutable Key   : string
  [<DefaultValue>] val mutable Value : string

  new () = new PropertyYaml(null, null)

  do
    self.Key <- key
    self.Value <- value

and Property =
  { Key: string; Value: string }

  member self.ToYamlObject() =
    new PropertyYaml(self.Key, self.Value)

  static member FromYamlObject(yml: PropertyYaml) : Either<IrisError,Property> =
    try
      { Key = yml.Key; Value = yml.Value }
      |> Either.succeed
    with
      | exn ->
        sprintf "Could not parse PropteryYaml: %s" exn.Message
        |> ParseError
        |> Either.fail

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
