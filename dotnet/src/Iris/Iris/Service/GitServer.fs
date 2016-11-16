namespace Iris.Service

// * Imports

open Iris.Core
open Iris.Core.Utils
open Iris.Service.Utilities

open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Management
open Microsoft.FSharp.Control


// * GitServer

//   ____ _ _   ____
//  / ___(_) |_/ ___|  ___ _ ____   _____ _ __
// | |  _| | __\___ \ / _ \ '__\ \ / / _ \ '__|
// | |_| | | |_ ___) |  __/ |   \ V /  __/ |
//  \____|_|\__|____/ \___|_|    \_/ \___|_|

/// ## GitServer
///
/// GitServer to provide data sync services between Iris nodes.
///
/// ### Signature:
/// - project: IrisProject to work on
///
/// Returns: GitServer
type GitServer (project: IrisProject) =

  let loco : obj  = new obj()

  let mutable logger : (CallSite -> LogLevel -> string -> unit) option = None

  let mutable starter : AutoResetEvent = null
  let mutable stopper : AutoResetEvent = null
  let mutable status  : ServiceStatus = ServiceStatus.Stopped
  let mutable thread  : Thread = null
  let mutable running : bool = false

  let mutable stdoutToken : CancellationTokenSource = null
  let mutable stderrToken : CancellationTokenSource = null

  let mutable onExitEvent : IDisposable = null

  // ** do

  /// ## constructor
  ///
  /// Creates two `AutoResetEvent`s to control the running thread and process.
  ///
  /// Returns: unit
  do
    starter <- new AutoResetEvent(false)
    stopper <- new AutoResetEvent(false)

  // ** log

  /// ## log
  ///
  /// Uses the passed logging function to log events inside the worker.
  ///
  /// ### Signature:
  /// - level: LogLevel to use
  /// - str: string to log
  ///
  /// Returns: unit
  let log level str =
    match logger with
    | Some cb -> cb (typeof<GitServer>) level str
    | _       -> ()

  // ** streamReader

  /// ## streamReader
  ///
  /// Reads asynchronously from a StreamReader and uses `log` to notify the application of output
  /// received from the `git` daemon. Since there is no realiable way to determine whether the `git
  /// daemon` has in fact successfully started, we wait for string _Ready to rumble_ to appear
  /// before control is passed back to the caller of the `Start()` method.
  ///
  /// ### Signature:
  /// - tag: LogLevel to distinguish between Stdout and StdErr
  /// - stream: StreamReader to monitor
  ///
  /// Returns: CancellationTokenSource
  let streamReader (tag: LogLevel) (stream: StreamReader) =
    let cts = new CancellationTokenSource()
    let action =
      async {
        while running && stream.Peek() <> -1 do
          let line = stream.ReadLine()    // blocks

          if line.Contains "Ready to rumble" then
            log LogLevel.Debug "setting starter to return to caller of .Start()"
            starter.Set() |> ignore

          log tag line

        log LogLevel.Debug (sprintf "streamReader done. running=%b eof=%b" running (not running))
      }
    Async.Start(action, cts.Token)
    cts

  // ** exitHandler

  /// ## exitHandler
  ///
  /// Handler to monitor the sub-processes. If the `git daemon` process exits for whichever reason
  /// (it might not have been able to start because of erroneous port settings for instance), then
  /// this handler detects this, creates an error from this and ensures control is passed back to
  /// the caller of `Start()`. It also ensures that all resources on the thread will get cleaned up
  /// properly.
  ///
  /// ### Signature:
  /// - proc: Process to monitor (mostly to get ExitCode from)
  ///
  /// Returns: unit
  let exitHandler (proc: Process) _ =
    if proc.ExitCode > 0 then
      status <-
        sprintf "Non-zero ExitCode: %d" proc.ExitCode
        |> Other
        |> ServiceStatus.Failed

    lock loco <| fun _ ->
      Monitor.Pulse(loco)

    starter.Set() |> ignore

  // ** worker

  /// ## worker
  ///
  /// Action to execute on another Thread.
  ///
  /// The steps taken are:
  ///
  /// 1) setup of the Process
  /// 2) hook up the `onExitEvent` Observable
  /// 3) start the Process and check whether it was successful
  /// 4a) if successful, run the following steps
  ///     4.1) change status to "Running"
  ///     4.2) wait for Monitor.Pulse to allow us to move on to clean up the resources and shut down
  /// 4b) if unsuccessful, record the failure in the status and pass back control to the caller
  /// 5) Clean up Resouces
  ///     5.1) StdErr/StdOut StreamReader actions
  ///     5.2) Kill the process
  ///     5.3) dispose the Process
  /// 6) If this is a regular `Stop()`, set the status to `Stopped`. Other leave as is, as it will
  ///    contain the error
  /// 7) Signal to caller that shutdown is complete
  ///
  /// ### Signature:
  /// - path: FilePath to Project repository
  /// - addr: IpAddr to bind the daemon to
  /// - port: uint16 port to bind daemon to
  ///
  /// Returns: unit
  let worker path addr port () =
    /// 1) Set up the Process

    let basedir = Path.GetDirectoryName path

    sprintf "starting on %s:%d in base path: %A with dir: %A"
      (string addr)
      port
      basedir
      path
    |> log LogLevel.Debug

    let args =
      sprintf """daemon \
                 --verbose \
                 --reuseaddr \
                 --listen=%s \
                 --port=%d \
                 --strict-paths \
                 --base-path=%s \
                 %s/.git"""
        (string addr)
        port
        basedir
        path

    let proc = new Process()
    proc.StartInfo.FileName <- "git"
    proc.StartInfo.Arguments <- args
    proc.EnableRaisingEvents <- true
    proc.StartInfo.CreateNoWindow <- true
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError <- true

    /// 2) Hook up `onExitEvent` callback
    onExitEvent <-
      proc.Exited
      |> Observable.subscribe (exitHandler proc)

    /// 3) Start the Process
    if proc.Start() then

      /// 4.1) Setting the Status to Running
      running <- true

      stdoutToken <- streamReader LogLevel.Info proc.StandardOutput
      stderrToken <- streamReader LogLevel.Err  proc.StandardError

      log LogLevel.Debug "setting status to running"
      status <- ServiceStatus.Running

      /// 4.2) Waiting for the Signal to shut down
      log LogLevel.Debug "waiting for Stop signal"
      lock loco <| fun _ ->
        Monitor.Wait(loco) |> ignore

      /// 5.1) Cleaing up Resources - StdErr/StdOut StreamReader actions
      log LogLevel.Debug "stopping streamReaders"
      cancelToken stdoutToken
      cancelToken stderrToken

      /// 5.2) Exit event handler
      try
        log LogLevel.Debug "disposing event handlers"
        dispose onExitEvent
      with
        | exn -> log LogLevel.Info (sprintf "could not dispose of event handler: %s" exn.Message)

      /// 5.3) Kill the process
      try
        log LogLevel.Debug "killing process"
        proc.Kill()
      with
        | exn -> log LogLevel.Info (sprintf "could not kill process: %s" exn.Message)

      /// 5.4) dispose the Process
      try
        log LogLevel.Debug "disposing process"
        dispose proc
      with
        | exn -> log LogLevel.Info (sprintf "could not dispose of process: %s" exn.Message)

      /// 6) Set status to Stopped
      if Service.isStopping status then
        log LogLevel.Debug "setting status to Stopped"
        status <- ServiceStatus.Stopped

      /// 7) Signal shutdown is complete
      stopper.Set() |> ignore
      log LogLevel.Debug "shutdown in thread complete"
    else
      /// 4.2) Signal startup is complete (but failed)
      log LogLevel.Err "starting child process unsuccessful"
      status <- ServiceStatus.Failed (Other "Git process could not be started")
      starter.Set() |> ignore

  // ** OnLogMsg

  /// ## OnLogMsg
  ///
  /// Setter for logging callback
  ///
  /// ### Signature:
  /// - callback: (CallSite -> LogLevel -> string -> unit) callback to use for logging
  ///
  /// Returns: unit
  member self.OnLogMsg
    with set callback = logger <- Some callback

  // ** Status

  /// ## Status
  ///
  /// Getter for current server status.g
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: ServiceStatus
  member self.Status
    with get () = status

  // ** Start

  /// ## Start
  ///
  /// Start the `GitServer` daemon. Returns when either the `git daemon` was successfully started
  /// (i.e. when it output "Ready to rumble"), or if it could not be started for whatever reason.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member self.Start() =
    if Service.isStopped status then
      match project.Path, Config.selfNode project.Config with
      | Some path, Right node  ->
        log LogLevel.Info "starting"
        status <- ServiceStatus.Starting
        thread <- new Thread(new ThreadStart(worker path node.IpAddr node.GitPort))
        thread.Start()
        starter.WaitOne() |> ignore
        log LogLevel.Info "started sucessfully"

      | None, _ ->
        log LogLevel.Err "cannot start without a project path"

      | _, Left error ->
        log LogLevel.Err (sprintf "cannot start: %A" error)

    else
      log Err (sprintf "cannot not start. wrong status: %A" status)

  // ** Stop

  /// ## Stop
  ///
  /// Stop the `GitServer` daemon. Only takes effect if the server is currently running.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member self.Stop() =
    if Service.isRunning status then
      log LogLevel.Debug "setting status to Stopping"
      status <- ServiceStatus.Stopping

      log LogLevel.Debug "waiting for stop signal"
      lock loco <| fun _ ->
        Monitor.Pulse(loco)

      log LogLevel.Debug "waiting for final signal to shut down"
      stopper.WaitOne() |> ignore

      log LogLevel.Debug "shutdown complete"

  // ** Running

  /// ## Running
  ///
  /// Check if the server is running.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: bool
  member self.Running() =
    Service.isRunning status && thread.IsAlive

  // ** IDisposable

  /// ## IDisposable
  ///
  /// Dispose this `GitServer`.
  ///
  /// ### Signature:
  /// - unit: unit
  /// - arg: arg
  /// - arg: arg
  ///
  /// Returns: unit
  interface IDisposable with
    member self.Dispose() =
      self.Stop()