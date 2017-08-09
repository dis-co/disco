namespace Iris.Core

// * Imports

open System
open FlatBuffers

#if FABLE_COMPILER

open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open Iris.Serialization

#endif

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

#endif

// * LogLevel

//  _                _                   _
// | |    ___   __ _| |    _____   _____| |
// | |   / _ \ / _` | |   / _ \ \ / / _ \ |
// | |__| (_) | (_| | |__|  __/\ V /  __/ |
// |_____\___/ \__, |_____\___| \_/ \___|_|
//             |___/
type LogLevel =
  | Trace
  | Debug
  | Info
  | Warn
  | Err

  // ** Parse

  static member Parse (str: string) =
    match String.toLower str with
    | "trace"         -> Trace
    | "debug"         -> Debug
    | "info"          -> Info
    | "warn"          -> Warn
    | "err" | "error" -> Err
    | _               -> failwithf "could not parse %s" str

  // ** TryParse

  static member TryParse (str: string) =
    Either.tryWith (Error.asParseError "LogLevel.TryParse") <| fun _ ->
      str |> LogLevel.Parse

  // ** ToString

  override self.ToString() =
    match self with
    | Trace -> "trace"
    | Debug -> "debug"
    | Info  -> "info"
    | Warn  -> "warn"
    | Err   -> "error"

// * Tier

/// ## Tier
///
/// Tier models the different types of locations in an Iris cluster.
///
/// - FrontEnd: log events from the frontend
/// - Client:   log events from Iris Service clients such as VVVV
/// - Service:  log events from the Iris Services
///
/// Returns: Tier
[<RequireQualifiedAccess>]
type Tier =
  | FrontEnd
  | Client
  | Service

  // ** ToString

  override self.ToString() =
    match self with
    | FrontEnd -> "frontend"
    | Client   -> "client"
    | Service  -> "service"

  // ** Parse

  static member Parse (str: string) =
    match str.ToLower() with
    | "frontend" | "ui" -> FrontEnd
    | "client"  -> Client
    | "service" -> Service
    | _         -> failwithf "could not parse %s" str

  // ** TryParse

  static member TryParse (str: string) =
    Either.tryWith (Error.asParseError "Tier.TryParse") <| fun _ ->
      str |> Tier.Parse

// * LogEventYaml

#if !FABLE_COMPILER && !IRIS_NODES

type LogEventYaml() =
  [<DefaultValue>] val mutable Time      : uint32
  [<DefaultValue>] val mutable Thread    : int
  [<DefaultValue>] val mutable Tier      : string
  [<DefaultValue>] val mutable Id        : string
  [<DefaultValue>] val mutable Tag       : string
  [<DefaultValue>] val mutable LogLevel  : string
  [<DefaultValue>] val mutable Message   : string

#endif

// * LogEvent

/// ## LogEvent
///
/// Structured log format record.
///
/// ## Fields:
///
/// - Time:     int64 unixtime in milliseconds
/// - Thread:   int ID of Thread the log event was collected
/// - Tier:     application tier where log was collected
/// - Id:       Id of cluster particiant where log was collected. Depends on Tier.
/// - Tag:      call site tag describing source code location where log was collected
/// - LogLevel: LogLevel of collected log message
/// - Message:  log message
///
/// Returns: LogEvent
type LogEvent =
  { Time      : uint32
    Thread    : int
    Tier      : Tier
    Id        : Id
    Tag       : string
    LogLevel  : LogLevel
    Message   : string }

  // ** ToString

  override self.ToString() =
    sprintf "[%s - %s - %s - %d - %d - %s]: %s"
      (System.String.Format("{0,-5}",string self.LogLevel))
      (System.String.Format("{0,-8}",string self.Tier))
      (System.String.Format("{0,-8}",self.Id.Prefix))
      self.Time
      self.Thread
      self.Tag
      self.Message

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let tier = builder.CreateString (string self.Tier)
    let id = builder.CreateString (string self.Id)
    let tag = Option.mapNull builder.CreateString self.Tag
    let level = builder.CreateString (string self.LogLevel)
    let msg = Option.mapNull builder.CreateString self.Message

    LogEventFB.StartLogEventFB(builder)
    LogEventFB.AddTime(builder, self.Time)
    LogEventFB.AddThread(builder, self.Thread)
    LogEventFB.AddTier(builder, tier)
    LogEventFB.AddId(builder,id)
    Option.iter (fun value -> LogEventFB.AddTag(builder,value)) tag
    LogEventFB.AddLogLevel(builder, level)
    Option.iter (fun value -> LogEventFB.AddMessage(builder,value)) msg
    LogEventFB.EndLogEventFB(builder)

  // ** FromFB

  static member FromFB(fb: LogEventFB) = either {
      let id = Id fb.Id
      let! tier = Tier.TryParse fb.Tier
      let! level = LogLevel.TryParse fb.LogLevel
      return { Time     = fb.Time
               Thread   = fb.Thread
               Tier     = tier
               Id       = id
               Tag      = fb.Tag
               LogLevel = level
               Message  = fb.Message }
    }

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member self.ToYamlObject() =
    let yaml = LogEventYaml()
    yaml.Time     <- self.Time
    yaml.Thread   <- self.Thread
    yaml.Tier     <- string self.Tier
    yaml.Id       <- string self.Id
    yaml.Tag      <- self.Tag
    yaml.LogLevel <- string self.LogLevel
    yaml.Message  <- self.Message
    yaml

  // ** ToYaml

  member self.ToYaml(serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  // ** FromYamlObject

  static member FromYamlObject(yaml: LogEventYaml) = either {
      let id = Id yaml.Id
      let! level = LogLevel.TryParse yaml.LogLevel
      let! tier = Tier.TryParse yaml.Tier
      return { Time     = yaml.Time
               Thread   = yaml.Thread
               Tier     = tier
               Id       = id
               Tag      = yaml.Tag
               LogLevel = level
               Message  = yaml.Message }
    }

  // ** FromYaml

  static member FromYaml(str: string) =
    let serializer = Serializer()
    str
    |> serializer.Deserialize
    |> LogEvent.FromYamlObject

  #endif

// * LoggingSettings

type LoggingSettings =
  { Id: Id
    Level: LogLevel
    UseColors: bool
    Tier: Tier }

// * LoggingSettings

module LoggingSettings =

  let defaultSettings =
    { Id = Id.Create()
      Level = LogLevel.Debug
      UseColors = true
      Tier = Tier.Service }

// * Logger

[<RequireQualifiedAccess>]
module Logger =

  // ** Imports

  open System
  open System.Threading
  open Iris.Core

  // ** settings

  let mutable private settings =
    { Id = Id "<uninitialized>"
      Level = LogLevel.Debug
      UseColors = true
      Tier = Tier.Service }

  // ** initialize

  let initialize config = settings <- config

  // ** colors

  // Black         - The color black.
  // Blue          - The color blue.
  // Cyan          - The color cyan (blue-green).
  // DarkBlue      - The color dark blue.
  // DarkCyan      - The color dark cyan (dark blue-green).
  // DarkGray      - The color dark gray.
  // DarkGreen     - The color dark green.
  // DarkMagenta   - The color dark magenta (dark purplish-red).
  // DarkRed       - The color dark red.
  // DarkYellow    - The color dark yellow (ochre).
  // Gray          - The color gray.
  // Green         - The color green.
  // Magenta       - The color magenta (purplish-red).
  // Red           - The color red.
  // White         - The color white.
  // Yellow        - The color yellow.

  // ** withForeground

  let private withForeground pat fg (o: obj) =
    let prevFg = Console.ForegroundColor
    Console.ForegroundColor <- fg
    Console.Write(pat,o)
    Console.ForegroundColor <- prevFg

  let private black pat (thing: obj) =
    withForeground pat ConsoleColor.Black thing

  let private white pat (thing: obj) =
    withForeground pat ConsoleColor.White thing

  let private blue pat (thing: obj) =
    withForeground pat ConsoleColor.Blue thing

  let private darkBlue pat (thing: obj) =
    withForeground pat ConsoleColor.DarkBlue thing

  let private cyan pat (thing: obj) =
    withForeground pat ConsoleColor.Cyan thing

  let private darkCyan pat (thing: obj) =
    withForeground pat ConsoleColor.DarkCyan thing

  let private gray pat (thing: obj) =
    withForeground pat ConsoleColor.Gray thing

  let private darkGray pat (thing: obj) =
    withForeground pat ConsoleColor.DarkGray thing

  let private green pat (thing: obj) =
    withForeground pat ConsoleColor.Green thing

  let private darkGreen pat (thing: obj) =
    withForeground pat ConsoleColor.DarkGreen thing

  let private magenta pat (thing: obj) =
    withForeground pat ConsoleColor.Magenta thing

  let private darkMagenta pat (thing: obj) =
    withForeground pat ConsoleColor.DarkMagenta thing

  let private red pat (thing: obj) =
    withForeground pat ConsoleColor.Red thing

  let private darkRed pat (thing: obj) =
    withForeground pat ConsoleColor.DarkRed thing

  let private yellow pat (thing: obj) =
    withForeground pat ConsoleColor.Yellow thing

  let private darkYellow pat (thing: obj) =
    withForeground pat ConsoleColor.DarkYellow thing

  // ** stdout

  /// ## stdout
  ///
  /// Simple logging to stdout
  ///
  /// ### Signature:
  /// - log: LogEvent
  ///
  /// Returns: unit
  let stdout (log: LogEvent) =
    if settings.UseColors then
      darkGreen "{0}" "["
      match log.LogLevel with
      | LogLevel.Trace -> gray   "{0,-5}" log.LogLevel
      | LogLevel.Debug -> white  "{0,-5}" log.LogLevel
      | LogLevel.Info  -> green  "{0,-5}" log.LogLevel
      | LogLevel.Warn  -> yellow "{0,-5}" log.LogLevel
      | LogLevel.Err   -> red    "{0,-5}" log.LogLevel
      darkGreen "{0}" "] "

      darkGreen "{0}:" "ts"
      white     "{0} " log.Time

      darkGreen "{0}:" "id"
      white     "{0} " log.Id.Prefix

      darkGreen "{0}:"    "type"
      white     "{0,-7} " log.Tier

      darkGreen "{0}:"     "in"
      yellow    "{0,-30} " log.Tag

      white  "{0}"  log.Message
      Console.Write(System.Environment.NewLine)
    else
      Console.WriteLine("{0}", log)

  // ** filter

  let filter (level: LogLevel) (logger: LogEvent -> unit) (log: LogEvent) =
    if level = log.LogLevel then
      logger log

  // ** stdoutWith

  let stdoutWith (level: LogLevel) (log: LogEvent) =
    match level, log.LogLevel with
    | Trace, _ -> stdout log
    | Debug, Debug | Debug, Info | Debug, Warn | Debug, Err -> stdout log
    | Info, Info | Info, Warn | Info, Err ->
      stdout log
    | Warn, Warn | Warn, Err ->
      stdout log
    | Err, Err ->
      stdout log
    | _ -> ()

  // ** subscriptions

  let private subscriptions = new ResizeArray<IObserver<LogEvent>>()

  // ** listener

  let private listener =
    { new IObservable<LogEvent> with
        member self.Subscribe(obs) =

          #if FABLE_COMPILER
          subscriptions.Add obs
          #else
          lock subscriptions <| fun _ ->
            subscriptions.Add obs
          #endif

          // Subscribe must return an IDisposable so the observer can be gc'd
          { new IDisposable with
              member self.Dispose() =
                #if FABLE_COMPILER
                subscriptions.Remove obs
                |> ignore
                #else
                lock subscriptions <| fun _ ->
                  subscriptions.Remove obs
                  |> ignore
                #endif
              } }

  // ** agent

  /// ## agent
  ///
  /// Logging agent. Hidden.
  ///
  /// Returns: MailboxProcessor<LogEvent>
  let private agent =
    MailboxProcessor<LogEvent>.Start <| fun inbox -> async {
      while true do
        let! log = inbox.Receive()
        for sub in subscriptions do
          sub.OnNext log
    }

  // ** subscribe

  /// ## subscribe
  ///
  /// Log the given string.
  let subscribe cb =
    { new IObserver<LogEvent> with
        member x.OnCompleted() = ()
        member x.OnError(error) = ()
        member x.OnNext(value) = cb value }
    |> listener.Subscribe

  // ** create

  /// ## create
  ///
  /// Create a new LogEvent, hiding some of the nitty gritty details.
  ///
  /// ### Signature:
  /// - arg: arg
  /// - arg: arg
  /// - arg: arg
  ///
  /// Returns: LogEvent
  let create (level: LogLevel) (callsite: CallSite) (msg: string) =
    let now  = DateTime.UtcNow |> Time.unixTime
    { Time     = uint32 now
      #if FABLE_COMPILER
      Thread   = 1
      #else
      Thread   = Thread.CurrentThread.ManagedThreadId
      #endif
      Tier     = settings.Tier
      Id       = settings.Id
      Tag      = callsite
      LogLevel = level
      Message  = msg }

  // ** append

  let append (log: LogEvent) = agent.Post log

  // ** log

  /// ## log
  ///
  /// Log the given string.
  ///
  /// ### Signature:
  /// - level: LogLevel
  /// - callside: CallSite
  /// - msg: string
  ///
  /// Returns: unit
  let log (level: LogLevel) (callsite: CallSite) (msg: string) =
    msg
    |> create level callsite
    |> append

  // ** trace

  /// ## trace
  ///
  /// Shorthand for creating a Trace event.
  ///
  /// ### Signature:
  /// - callsite: location where even was generated
  /// - msg: log message
  ///
  /// Returns: unit
  let trace (callsite: CallSite) (msg: string) =
    msg
    |> create LogLevel.Trace callsite
    |> append

  // ** debug

  /// ## debug
  ///
  /// Shorthand for creating a Debug event.
  ///
  /// ### Signature:
  /// - callsite: location where even was generated
  /// - msg: log message
  ///
  /// Returns: unit
  let debug (callsite: CallSite) (msg: string) =
    msg
    |> create LogLevel.Debug callsite
    |> append

  // ** info

  /// ## info
  ///
  /// Shorthand for creating a Info event.
  ///
  /// ### Signature:
  /// - callsite: location where even was generated
  /// - msg: log message
  ///
  /// Returns: LogEvent
  let info (callsite: CallSite) (msg: string) =
    msg
    |> create LogLevel.Info callsite
    |> append

  // ** warn

  /// ## warn
  ///
  /// Shorthand for creating a Warn event.
  ///
  /// ### Signature:
  /// - callsite: location where even was generated
  /// - msg: log message
  ///
  /// Returns: LogEvent
  let warn (callsite: CallSite) (msg: string) =
    msg
    |> create LogLevel.Warn callsite
    |> append

  // ** err

  /// ## err
  ///
  /// Shorthand for creating a Err event.
  ///
  /// ### Signature:
  /// - callsite: location where even was generated
  /// - msg: log message
  ///
  /// Returns: LogEvent
  let err (callsite: CallSite) (msg: string) =
    msg
    |> create LogLevel.Err callsite
    |> append

// * LogFile

#if !FABLE_COMPILER

[<NoComparison;NoEquality>]
type LogFile =
  { FilePath: FilePath
    Created: DateTime
    Stream: StreamWriter }

  interface IDisposable with
    member self.Dispose() =
      try
        self.Stream.Flush()
        self.Stream.Close()
        dispose self.Stream
      with
        | _ -> ()

// * LogFile module

module LogFile =

  // ** tag

  let private tag (str: string) = String.format "LogFile.{0}" str

  // ** write

  let write (file: LogFile) (log: LogEvent) =
    try
      log
      |> string
      |> file.Stream.WriteLine
      |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "write")
        |> Either.fail

  // ** create

  let create (machine: Id) (path: FilePath) =
    let ts = DateTime.Now
    let fn = String.Format("iris-{0}-{1:yyyy-MM-dd_hh-mm-ss-tt}.log", machine.Prefix, ts)
    let fp = Path.Combine(unwrap path, fn)
    try
      let writer = File.AppendText fp
      writer.AutoFlush <- true
      { FilePath = filepath fp
        Created = ts
        Stream = writer }
      |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "create")
        |> Either.fail

#endif
