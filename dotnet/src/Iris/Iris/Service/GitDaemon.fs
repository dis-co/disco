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

    let mutable starter : AutoResetEvent = null
    let mutable stopper : AutoResetEvent = null

    let mutable started : bool = false
    let mutable running : bool = false
    let mutable worker  : Thread option = None

    do
      starter <- new AutoResetEvent(false)
      stopper <- new AutoResetEvent(false)

    member self.Runner path () =
      let basedir = Path.GetDirectoryName path
      let folder = Path.GetFileName path

      let mutable initialized = false

      let args = sprintf "daemon --reuseaddr --strict-paths --base-path=%s %s/.git" basedir path
      let proc = Process.Start("git", args)

      lock loco <| fun _ ->
        while running do
          if not initialized then
            starter.Set() |> ignore
            initialized <- true
          Monitor.Wait(loco) |> ignore

      Process.kill proc.Id

      stopper.Set() |> ignore

    member self.Start() =
      if not started then
        match (!project).Path with
        | Some path ->
          running <- true
          let thread = new Thread(new ThreadStart(self.Runner path))
          thread.Start()
          starter.WaitOne() |> ignore
          worker <- Some(thread)
          started <- true
        | _ -> ()

    member self.Stop() =
      if started && running then
        lock loco <| fun _ ->
          running <- false
          Monitor.Pulse(loco)
          started <- false
          stopper.WaitOne() |> ignore

    member self.Running() =
      started
      && running
      && Option.isSome worker
      && Option.get(worker).IsAlive
