namespace Iris.Service

open System.Diagnostics
open System
open System.Threading

open Iris.Core.Types
open Iris.Service.Types

open Vsync

type LookUpHandler = delegate of string -> unit

module Main =

  let HELLO = 1

  [<EntryPoint>]
  let main argv =
    printfn "starting engine"

    Environment.SetEnvironmentVariable("VSYNC_UNICAST_ONLY", "true")
    Environment.SetEnvironmentVariable("VSYNC_HOSTS", "localhost")
    
    VsyncSystem.Start()

    let g = new Group("test")

    let del = new LookUpHandler(fun (str : string) ->
                             printfn "thing: %s" str)

    g.Handlers.[HELLO] <- g.Handlers.[HELLO] + del

    g.Join()

    g.Send(HELLO, "me")

    VsyncSystem.WaitForever()

    0
