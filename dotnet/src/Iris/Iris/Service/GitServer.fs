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

  // ** do

  do
    starter <- new AutoResetEvent(false)
    stopper <- new AutoResetEvent(false)

  // ** log

  let log level str =
    match logger with
    | Some cb -> cb (typeof<GitServer>) level str
    | _       -> ()

  // ** logReader

  let streamReader (tag: LogLevel) (stream: StreamReader) =
    let cts = new CancellationTokenSource()
    let action =
      async {
        log LogLevel.Debug "enterting streamReader loop"

        while running do
          let line = stream.ReadLine()    // blocks
          log tag line

        log LogLevel.Debug "leaving streamReader task"
      }
    Async.Start(action, cts.Token)
    cts

  // ** worker

  let worker path addr port () =
    let basedir = Path.GetDirectoryName path
    let folder = Path.GetFileName path

    sprintf "starting on %s:%d in base path: %A with dir: %A"
      (string addr)
      port
      basedir
      path
    |> log LogLevel.Debug

    let args =
      sprintf "daemon \
                 --verbose \
                 --reuseaddr \
                 --strict-paths \
                 --listen=%s \
                 --port=%d \
                 --base-path=%s \
                 %s/.git"
        (string addr)
        port
        basedir
        path

    let proc = new Process()
    proc.StartInfo.FileName <- "git"
    proc.StartInfo.Arguments <- args
    proc.StartInfo.CreateNoWindow <- true
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.RedirectStandardError <- true

    if proc.Start() then
      running <- true
      status <- ServiceStatus.Running
      starter.Set() |> ignore
      log LogLevel.Debug "entering loop"

      stdoutToken <- streamReader LogLevel.Info proc.StandardOutput
      stderrToken <- streamReader LogLevel.Err  proc.StandardError

      log LogLevel.Debug "reaching the lock"

      lock loco <| fun _ ->
        Monitor.Wait(loco)
        |> ignore

      log LogLevel.Debug "staring to dispose stuff"

      try
        log LogLevel.Debug "stopping streamReaders"
        cancelToken stdoutToken
        cancelToken stderrToken

        log LogLevel.Debug "killing process"
        proc.Kill()
        log LogLevel.Debug "disposing process"
        dispose proc
      finally
        log LogLevel.Debug "setting status to Stopped"
        status <- ServiceStatus.Stopped
        stopper.Set() |> ignore
        log LogLevel.Debug "shutdown complete"
    else
      log LogLevel.Err "starting child process unsuccessful"
      status <- ServiceStatus.Failed (Other "Git process could not be started")

  // ** OnLogMsg

  member self.OnLogMsg
    with set callback = logger <- Some callback

  // ** Status

  member self.Status
    with get () = status

  // ** Start

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

  member self.Stop() =
    if Service.isRunning status then
      lock loco <| fun _ ->
        log LogLevel.Debug "setting status to Stopping"
        status <- ServiceStatus.Stopping
        log LogLevel.Debug "pulsing the lock object"
        Monitor.Pulse(loco)
      log LogLevel.Debug "waiting for stop signal"
      stopper.WaitOne() |> ignore
      log LogLevel.Debug "got stop signal. done"
    else
      log LogLevel.Debug (sprintf "stop called but wrong status %A" status)

  // ** Running

  member self.Running() =
    Service.isRunning status && thread.IsAlive

  // ** IDisposable

  interface IDisposable with
    member self.Dispose() =
      self.Stop()
