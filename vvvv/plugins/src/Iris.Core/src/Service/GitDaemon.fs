namespace Iris.Service.Core

open Iris.Core.Utils
open Iris.Core.Types

open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Management

module Git =

  //  _    _ _ _ 
  // | | _(_) | |
  // | |/ / | | |
  // |   <| | | |
  // |_|\_\_|_|_|
  //             
  let rec kill (pid : int) =
    if isLinux
    then
      Process.Start("kill", string pid)
      |> ignore
    else
      let query = sprintf "Select * From Win32_Process Where ParentProcessID=%d" pid
      let searcher = new ManagementObjectSearcher(query);
      let moc = searcher.Get();
      for mo in moc do
        kill <| (mo.GetPropertyValue("ProcessID") :?> int)
      let proc = Process.GetProcessById(pid)
      proc.Kill();

  //  ____                                   
  // |  _ \  __ _  ___ _ __ ___   ___  _ __  
  // | | | |/ _` |/ _ \ '_ ` _ \ / _ \| '_ \ 
  // | |_| | (_| |  __/ | | | | | (_) | | | |
  // |____/ \__,_|\___|_| |_| |_|\___/|_| |_|
  //                                         
  type Daemon(path : FilePath) =
    let loco : obj  = new obj()
    let mutable started : bool = false
    let mutable running : bool = false
    let mutable Worker  : Thread option = None

    member self.Runner () =
      let basedir = Workspace()

      let args = sprintf "daemon --reuseaddr --strict-paths --base-path=%s %s/.git" basedir path
      let proc = Process.Start("git", args)

      lock loco <| fun _ ->
        while running do
          Monitor.Wait(loco) |> ignore

      kill proc.Id

    member self.Start() =
      if not started
      then
        running <- true
        let worker = new Thread(new ThreadStart(self.Runner))
        worker.Start()
        Worker <- Some(worker)
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
      && Option.isSome Worker
      && Option.get(Worker).IsAlive
