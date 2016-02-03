namespace Iris.Service.Core

open Iris.Core.Utils
open Iris.Core.Types
open System
open System.Threading
open System.Diagnostics
open System.Management

module Git =

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

  type Daemon(path : FilePath) =
    let loco : obj  = new obj()
    let mutable started : bool = false
    let mutable running : bool = false
    let mutable Worker  : Thread option = None

    member self.Runner () =
      let proc = Process.Start("git", "daemon") // add base path arg

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
