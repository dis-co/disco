namespace Iris.Core

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
  with
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
