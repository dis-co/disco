namespace Iris.Service

open Iris.Core
open Iris.Core.Utils

open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Management

module Git =

  //  ____
  // |  _ \  __ _  ___ _ __ ___   ___  _ __
  // | | | |/ _` |/ _ \ '_ ` _ \ / _ \| '_ \
  // | |_| | (_| |  __/ | | | | | (_) | | | |
  // |____/ \__,_|\___|_| |_| |_|\___/|_| |_|
  //

  /// ## Daemon
  ///
  /// Description
  ///
  /// ### Signature:
  /// - arg: arg
  /// - arg: arg
  /// - arg: arg
  ///
  /// Returns: Type
  type Daemon(project: IrisProject) =

    let loco : obj  = new obj()

    let mutable logger : (LogLevel -> string -> unit) option = None

    let mutable starter : AutoResetEvent = null
    let mutable stopper : AutoResetEvent = null
    let mutable status  : ServiceStatus = ServiceStatus.Stopped
    let mutable started : bool = false
    let mutable running : bool = false
    let mutable proc    : Thread option = None

    do
      starter <- new AutoResetEvent(false)
      stopper <- new AutoResetEvent(false)

    let log level str =
      match logger with
      | Some cb -> cb level str
      | _       -> ()

    let worker path () =
      let basedir = Path.GetDirectoryName path
      let folder = Path.GetFileName path
      let addr, port =
        match Config.selfNode project.Config with
        | Right node -> node.IpAddr, node.GitPort
        | _ -> failwith "hu"

      let args =
        sprintf "daemon --reuseaddr --strict-paths --listen=%s --port=%d --base-path=%s %s/.git"
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

      while running do
        if proc.StandardError.Peek() > -1 then
          let stderr = proc.StandardError.ReadToEnd()
          log Err stderr
        else
          Thread.Sleep 10

        if proc.StandardOutput.Peek() > -1 then
          let stdout = proc.StandardOutput.ReadToEnd()
          log Info stdout
        else
          Thread.Sleep 10

      try
        proc.Kill()
        dispose proc
      finally
        status <- ServiceStatus.Stopped
        stopper.Set() |> ignore

    member self.OnLogMsg
      with set callback = logger <- Some callback

    member self.Status
      with get () = status

    member self.Start() =
      if not started then
        match project.Path with
        | Some path ->
          status <- ServiceStatus.Starting
          let thread = new Thread(new ThreadStart(worker path))
          thread.Start()
          starter.WaitOne() |> ignore
          proc <- Some(thread)
          started <- true
        | _ -> ()

    member self.Stop() =
      if started && running then
        lock loco <| fun _ ->
          status <- ServiceStatus.Stopping
          running <- false
          started <- false
          stopper.WaitOne() |> ignore

    member self.Running() =
      match started && running, proc with
      | true, Some t -> t.IsAlive
      | _            -> false
