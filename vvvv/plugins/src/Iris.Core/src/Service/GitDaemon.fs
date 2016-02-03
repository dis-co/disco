namespace Iris.Service.Core

open Iris.Core.Types
open System.Threading
open System.Diagnostics

module Git =
  
  type Daemon(path : FilePath) as self =
    let         loco    : obj  = new obj()
    let mutable running : bool = true

    let mutable Worker : Thread = new Thread(new ThreadStart(self.Runner))

    member self.Runner () =
      let proc = Process.Start("git", "daemon") // add base path arg

      lock loco <| fun _ ->
        while running do
          Monitor.Wait(loco) |> ignore

      // FIXME: must kill all child processes
      proc.Kill()
        
    member self.Start() =
      Worker.Start()
      
    member self.Stop() =
      running <- false
      lock loco <| fun _ -> 
        Monitor.Pulse(loco)
