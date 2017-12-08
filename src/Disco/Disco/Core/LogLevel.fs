namespace Disco.Core


//  _                _                   _
// | |    ___   __ _| |    _____   _____| |
// | |   / _ \ / _` | |   / _ \ \ / / _ \ |
// | |__| (_) | (_| | |__|  __/\ V /  __/ |
// |_____\___/ \__, |_____\___| \_/ \___|_|
//             |___/
// #if FABLE_COMPILER
// open Fable.Core

// [<StringEnum>]
// #endif
type LogLevel =
  | Debug
  | Info
  | Warn
  | Err

  static member Parse (str: string) =
    match toLower str with
    | "debug"         -> Debug
    | "info"          -> Info
    | "warn"          -> Warn
    | "err" | "error" -> Err
    | _               -> failwithf "could not parse %s" str

  static member TryParse (str: string) =
    Either.tryWith ParseError "LogLevel" <| fun _ ->
      str |> LogLevel.Parse

  override self.ToString() =
    match self with
    | Debug -> "debug"
    | Info  -> "info"
    | Warn  -> "warn"
    | Err   -> "err"
