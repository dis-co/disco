namespace Iris.Service

open System.Diagnostics
open System
open System.Threading

open Iris.Core.Types
open Iris.Service.Types

open Vsync

type LookUpHandler = delegate of string -> unit

module Main =

  type IrisActions =
    | Init
    | Update
    | Close
    interface Intable with
      member self.ToInt() =
        match self with
          | Init   -> 1
          | Update -> 2
          | Close  -> 3

  [<EntryPoint>]
  let main argv =
    printfn "starting engine"

    Environment.SetEnvironmentVariable("VSYNC_UNICAST_ONLY", "true")
    Environment.SetEnvironmentVariable("VSYNC_HOSTS", "localhost")
    
    VsyncSystem.Start()

    let g = new IrisGroup<string,unit> "test"

    g.AddHandler(Init, new Handler<string,unit>(fun str -> printfn "%s" str))

    g.Join()
    
    g.Send(Init, "me")

    VsyncSystem.WaitForever()

    0
