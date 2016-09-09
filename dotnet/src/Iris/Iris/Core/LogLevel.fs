namespace Iris.Core

open Newtonsoft.Json
open Newtonsoft.Json.Linq

//  _                _                   _
// | |    ___   __ _| |    _____   _____| |
// | |   / _ \ / _` | |   / _ \ \ / / _ \ |
// | |__| (_) | (_| | |__|  __/\ V /  __/ |
// |_____\___/ \__, |_____\___| \_/ \___|_|
//             |___/
type LogLevel =
  | Debug
  | Info
  | Warn
  | Err

  static member Type
    with get () = "Iris.Core.LogLevel"

  static member Parse (str: string) =
    match toLower str with
    | "debug"         -> Debug
    | "info"          -> Info
    | "warn"          -> Warn
    | "err" | "error" -> Err
    | _               -> Debug

  override self.ToString() =
    match self with
    | Debug -> "debug"
    | Info  -> "info"
    | Warn  -> "warn"
    | Err   -> "err"

#if JAVASCRIPT
#else

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() : JToken =
    let json = new JObject()
    json.Add("$type", new JValue(LogLevel.Type))

    match self with
    | Debug -> json.Add("Case", new JValue("Debug"))
    | Info  -> json.Add("Case", new JValue("Info"))
    | Warn  -> json.Add("Case", new JValue("Warn"))
    | Err   -> json.Add("Case", new JValue("Err"))

    json :> JToken

  member self.ToJson() =
    self.ToJToken() |> string


  static member FromJToken(token: JToken) : LogLevel option =
    try
      let tag = string token.["$type"]

      if tag = LogLevel.Type then
        match string token.["Case"] with
        | "Debug" -> Some Debug
        | "Info"  -> Some Info
        | "Warn"  -> Some Warn
        | "Err"   -> Some Err
        | _       -> None
      else
          failwithf "$type not correct or missing: %s" LogLevel.Type
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : LogLevel option =
    JObject.Parse(str) |> LogLevel.FromJToken

#endif
