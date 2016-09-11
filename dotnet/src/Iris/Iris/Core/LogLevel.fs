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

#if JAVASCRIPT
#else

  static member Type
    with get () = Serialization.GetTypeName<LogLevel>()

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() : JToken =
    new JValue(string self) :> JToken

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : LogLevel option =
    try
      LogLevel.Parse (string token)
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : LogLevel option =
    JObject.Parse(str) |> LogLevel.FromJToken

#endif
