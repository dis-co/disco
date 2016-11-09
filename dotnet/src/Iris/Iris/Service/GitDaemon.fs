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
  type Daemon(path : FilePath) =
    let loco : obj  = new obj()
    let mutable started : bool = false
    let mutable running : bool = false
    let mutable worker  : Thread option = None

    member self.Runner () =
      let basedir = Path.GetDirectoryName path
      let folder = Path.GetFileName path

      let args = sprintf "daemon --reuseaddr --strict-paths --base-path=%s %s/.git" basedir path
      let proc = Process.Start("git", args)

      lock loco <| fun _ ->
        while running do
          Monitor.Wait(loco) |> ignore

      Process.kill proc.Id

    member self.Start() =
      if not started
      then
        running <- true
        let thread = new Thread(new ThreadStart(self.Runner))
        thread.Start()
        worker <- Some(thread)
        started <- true

    member self.Stop() =
      if started && running
      then
        lock loco <| fun _ ->
          running <- false
          Monitor.Pulse(loco)
          started <- false

    member self.Running() =
      started
      && running
      && Option.isSome worker
      && Option.get(worker).IsAlive
