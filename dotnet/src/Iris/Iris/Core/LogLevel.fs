namespace Iris.Core

open Newtonsoft.Json
open Newtonsoft.Json.Linq

//  _                _                   _
// | |    ___   __ _| |    _____   _____| |
// | |   / _ \ / _` | |   / _ \ \ / / _ \ |
// | |__| (_) | (_| | |__|  __/\ V /  __/ |
// |_____\___/ \__, |_____\___| \_/ \___|_|
//             |___/
#if JAVASCRIPT
open Fable.Core

[<StringEnum>]
#endif
type LogLevel =
  | Debug
  | Info
  | Warn
  | Err

  static member Parse (str: string) =
    match toLower str with
    | "debug"         -> Some Debug
    | "info"          -> Some Info
    | "warn"          -> Some Warn
    | "err" | "error" -> Some Err
    | _               -> None

  override self.ToString() =
    match self with
    | Debug -> "debug"
    | Info  -> "info"
    | Warn  -> "warn"
    | Err   -> "err"
