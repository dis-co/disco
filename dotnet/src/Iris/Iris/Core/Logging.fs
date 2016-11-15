namespace Iris.Core


// * LogLevel

//  _                _                   _
// | |    ___   __ _| |    _____   _____| |
// | |   / _ \ / _` | |   / _ \ \ / / _ \ |
// | |__| (_) | (_| | |__|  __/\ V /  __/ |
// |_____\___/ \__, |_____\___| \_/ \___|_|
//             |___/
// #if JAVASCRIPT
// open Fable.Core

// [<StringEnum>]
// #endif
type LogLevel =
  | Debug
  | Info
  | Warn
  | Err

  static member Parse (str: string) =
    match String.toLower str with
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


// * Logger

#if !JAVASCRIPT

[<RequireQualifiedAccess>]
module Logger =

  // ** Imports

  open System
  open System.Threading

  open Iris.Core

  // ** log

  /// ## log
  ///
  /// `Logger.log` is the central piece in the logging infrastructure on the server side.
  ///
  /// ### Signature:
  /// - config: RaftConfig to use for settings
  /// - level: LogLevel of incoming log
  /// - msg: string to log
  ///
  /// Returns: unit
  let log (id: Id) (current: LogLevel) (callsite: CallSite) (level: LogLevel) (msg: string) =
    let doLog msg =
      let now  = DateTime.Now |> Time.unixTime
      let tipe = sprintf "[%s]" callsite.Name
      let tid  = String.Format("[Thread: {0,2}]", Thread.CurrentThread.ManagedThreadId)
      let lvl  = String.Format("[{0,5}]", string level)
      let nid  = String.Format("[Host: {0,8}]", id |> string |> String.subString 0 8)
      let log  = sprintf "%d %s %s %s %s %s" now lvl tid nid tipe msg

      printfn "%s" log

    /// ## To `log` or not, that is the question.
    match current with
    /// In Debug, all messages get logged
    | Debug -> doLog msg

    // In Info mode, all messages except `Debug` ones get logged
    | Info  ->
      match level with
      | Info | Warn | Err -> doLog msg
      | _ -> ()

    // In Warn mode, messages of type `Err` and `Warn` get logged
    | Warn  ->
      match level with
      | Warn | Err   -> doLog msg
      | _ -> ()

    // In Err mode, only messages of type `Err` get logged
    | Err   ->
      match level with
      | Err -> doLog msg
      | _ -> ()

#endif
