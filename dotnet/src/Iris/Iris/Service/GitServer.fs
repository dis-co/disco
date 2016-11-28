namespace Iris.Service

// * Imports

open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Utilities

open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Management
open Microsoft.FSharp.Control
open FSharpx.Functional

// * GitServer

//   ____ _ _   ____
//  / ___(_) |_/ ___|  ___ _ ____   _____ _ __
// | |  _| | __\___ \ / _ \ '__\ \ / / _ \ '__|
// | |_| | | |_ ___) |  __/ |   \ V /  __/ |
//  \____|_|\__|____/ \___|_|    \_/ \___|_|

// = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =
//
// EXAMPLE CLONES
//
// [10422] Ready to rumble
// [11331] Connection from 127.0.0.1:43218
// [11331] Extended attributes (21 bytes) exist <host=localhost:6000>
// [11331] Request upload-pack for '/gittest/.git'
// [10422] [11331] Disconnected
// [11921] Connection from 127.0.0.1:43224
// [11921] Extended attributes (21 bytes) exist <host=localhost:6000>
// [11921] Request upload-pack for '/gittest/.git'
// [10422] [11921] Disconnected
//
// = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =

module Git =

  [<Literal>]
  let private tag = "GitServer"

  // ** GitEvent
  type GitEvent =
    | Pull of address:string * port:uint16
    | Started
    | Exited of int

  // ** IGitServer

  type IGitServer =
    inherit IDisposable

    abstract Status : ServiceStatus
    abstract Subscribe : (GitEvent -> unit) -> IDisposable
    abstract Start: unit -> Either<IrisError,unit>

  // ** Msg
  type private Msg =
    | Status
    | Exit   of int
    | Log    of string

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<GitEvent>>

  // ** GitAgent

  type private GitAgent = MailboxProcessor<Msg>

  // ** createProcess

  let private createProcess (id: Id) (path: FilePath) (addr: string) (port: uint16) =
    let basedir =
      Path.GetDirectoryName path
      |> String.replace '\\' '/'

    let sanepath = String.replace '\\' '/' path

    sprintf "starting on %s:%d in base path: %A with dir: %A"
      (string addr)
      port
      basedir
      path
    |> Logger.debug id tag

    let args =
      [| "daemon"
      ; "--verbose"
      ; "--strict-paths"
      ; (sprintf "--base-path=%s" basedir)
      ; (if Platform.isUnix then "--reuseaddr" else "")
      ; (addr |> string |> sprintf "--listen=%s")
      ; (sprintf "--port=%d" port)
      ; (sprintf "%s/.git" sanepath) |]
      |> String.join " "

    let proc = new Process()
    proc.StartInfo.FileName <- "git"
    proc.StartInfo.Arguments <- args
    proc.EnableRaisingEvents <- true
    proc.StartInfo.CreateNoWindow <- true
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError <- true

    proc

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
  let private streamReader (stream: StreamReader) (agent: GitAgent) (cts: CancellationTokenSource) =
    let action =
      async {
        while true do
          let line = stream.ReadLine()    // blocks
          if not (isNull line) then
            agent.Post(Log line)
      }
    Async.Start(action, cts.Token)

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
  let private exitHandler (proc: Process) (agent: GitAgent) _ =
    Exit proc.ExitCode
    |> agent.Post

  [<RequireQualifiedAccess>]
  module GitServer =

    let start (node: RaftNode) (project: IrisProject) =
      match project.Path with
      | Some path ->
        let stdoutToken = new CancellationTokenSource()
        let stderrToken = new CancellationTokenSource()
        let subscriptions = new Subscriptions()

        let listener =
          { new IObservable<GitEvent> with
              member self.Subscribe(obs) =
                lock subscriptions <| fun _ ->
                  subscriptions.Add obs

                { new IDisposable with
                    member self.Dispose () =
                      lock subscriptions <| fun _ ->
                        subscriptions.Remove obs
                        |> ignore } }

        let proc = createProcess node.Id path (string node.IpAddr) node.GitPort

        proc.OutputDataReceived

        let onExitEvent =
          Observable.subscribe (exitHandler proc) proc.Exited

        { new IGitServer with
            member self.Status
              with get () = implement "Status"

            member self.Subscribe(callback: string -> unit) =
              { new IObserver<string> with
                  member self.OnCompleted() = ()
                  member self.OnError(error) = ()
                  member self.OnNext(value) = callback value }
              |> listener.Subscribe

            member self.Start () =
              /// 3) Start the Process
              if proc.Start() then

                /// 4.1) Setting the Status to Running

                streamReader proc.StandardOutput stdoutToken
                streamReader proc.StandardError  stderrToken

            member self.Dispose() =
              dispose stdoutToken
              dispose stderrToken
              dispose onExitEvent
              try
                Process.kill proc.Id
              finally
                dispose proc
          }
        |> Either.succeed

      | None ->
        ProjectPathError
        |> Either.fail

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


    /// 2) Hook up `onExitEvent` callback

      Logger.debug nodeid tag "setting status to running"
      status <- ServiceStatus.Running

      /// 4.2) Waiting for the Signal to shut down
      Logger.debug nodeid tag "waiting for Stop signal"
      lock loco <| fun _ ->
        Monitor.Wait(loco) |> ignore

      /// 5.1) Cleaing up Resources - StdErr/StdOut StreamReader actions
      Logger.debug nodeid tag "stopping streamReaders"
      cancelToken stdoutToken
      cancelToken stderrToken

      /// 5.2) Exit event handler
      try
        Logger.debug nodeid tag "disposing event handlers"
        dispose onExitEvent
      with
        | exn ->
          sprintf "could not dispose of event handler: %s" exn.Message
          |> Logger.info nodeid tag

      /// 5.3) Kill the process
      try
        Logger.debug nodeid tag "killing process"
        Process.kill pid
      with
        | exn ->
          sprintf "could not kill process: %s" exn.Message
          |> Logger.info nodeid tag

      /// 5.4) dispose the Process
      try
        Logger.debug nodeid tag "disposing process"
        dispose proc
      with
        | exn ->
          sprintf "could not dispose of process: %s" exn.Message
          |> Logger.info nodeid tag

      /// 6) Set status to Stopped
      if Service.isStopping status then
        Logger.debug nodeid tag "setting status to Stopped"
        status <- ServiceStatus.Stopped

      /// 7) Signal shutdown is complete
      stopper.Set() |> ignore
      Logger.debug nodeid tag "shutdown in thread complete"
    else
      /// 4.2) Signal startup is complete (but failed)
      Logger.err nodeid tag "starting child process unsuccessful"
      status <- ServiceStatus.Failed (Other "Git process could not be started")
      starter.Set() |> ignore

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
        Logger.info nodeid tag "starting"
        status <- ServiceStatus. Start
    | Status
    | Sto
        thread <- new Thread(new ThreadStart(worker path node.IpAddr node.GitPort))
        thread.Start()
        starter.WaitOne() |> ignore
        Logger.info nodeid tag "started sucessfully"

      | None, _ ->
        Logger.err nodeid tag "cannot start without a project path"

      | _, Left error ->
        sprintf "cannot start: %A" error
        |> Logger.err nodeid tag

    else
      sprintf "cannot not start. wrong status: %A" status
      |> Logger.err nodeid tag

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
      Logger.debug nodeid tag "setting status to Stopping"
      status <- ServiceStatus.Stopping

      Logger.debug nodeid tag "waiting for stop signal"
      lock loco <| fun _ ->
        Monitor.Pulse(loco)

      Logger.debug nodeid tag "waiting for final signal to shut down"
      stopper.WaitOne() |> ignore

      Logger.debug nodeid tag "shutdown complete"

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
    pid >= 0                  &&
    Service.isRunning status &&
    Process.isRunning pid    &&
    thread.IsAlive

  // ** Pid

  /// ## Pid
  ///
  /// Get the PID of the underlying `git daemon` process.
  ///
  /// ### Signature:
  /// - unit: unit
  /// - arg: arg
  /// - arg: arg
  ///
  /// Returns: int
  member self.Pid
    with get () = pid

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
