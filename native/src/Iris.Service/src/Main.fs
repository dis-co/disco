namespace Iris.Service

open System.Diagnostics
open System

open Iris.Core.Types
open Iris.Service.Types

open Vsync

module Main =

  [<EntryPoint>]
  let main argv =
    printfn "starting engine"

    Environment.SetEnvironmentVariable("VSYNC_UNICAST_ONLY", "true")
    Environment.SetEnvironmentVariable("VSYNC_HOSTS", "localhost")
    
    VsyncSystem.Start()

    printfn "done."

    VsyncSystem.WaitForever()

    0
