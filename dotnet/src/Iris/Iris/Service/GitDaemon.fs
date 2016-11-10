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
  type Daemon(project: IrisProject ref) =

    let loco : obj  = new obj()

    let mutable logger : (LogLevel -> string -> unit) option = None

    let mutable starter : AutoResetEvent = null
    let mutable stopper : AutoResetEvent = null

    let mutable started : bool = false
    let mutable running : bool = false
    let mutable proc    : Thread option = None

    do
      starter <- new AutoResetEvent(false)
      stopper <- new AutoResetEvent(false)

    let worker path () =
      let basedir = Path.GetDirectoryName path
      let folder = Path.GetFileName path

      let mutable initialized = false

      let args =
        sprintf "daemon --reuseaddr --strict-paths --listen=%s --port=%d --base-path=%s %s/.git"
          addr
          port
          basedir
          path

      let proc = new Process()
      proc.StartInfo.FileName <- "git"
      proc.StartInfo.Arguments <- args
      proc.StartInfo.CreateNoWindow <- true
      proc.StartInfo.UseShellExecute <- false
      proc.StartInfo.RedirectStandardError <- true

      proc.Start()

      starter.Set() |> ignore

      while running do
        printfn "DO SOMETHING SENSIBLE"

      proc.Kill()
      dispose proc

      stopper.Set() |> ignore

    member self.OnLogMsg
      with set callback = logger <- Some callback

    member self.Start() =
      if not started then
        match (!project).Path with
        | Some path ->
          running <- true
          let thread = new Thread(new ThreadStart(worker path))
          thread.Start()
          starter.WaitOne() |> ignore
          proc <- Some(thread)
          started <- true
        | _ -> ()

    member self.Stop() =
      if started && running then
        lock loco <| fun _ ->
          running <- false
          started <- false
          stopper.WaitOne() |> ignore

    member self.Running() =
      match started && running, proc with
      | true, Some t -> t.IsAlive
      | _            -> false
