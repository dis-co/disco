namespace Iris.Service

open System.Diagnostics
open System
open System.Threading

open Nessos.FsPickler
open Iris.Core.Types
open Iris.Service.Types

open Vsync

type LookUpHandler = delegate of string -> unit

module Main =

  [<EntryPoint>]
  let main argv =
    printfn "starting engine.."

    let options =
     { VsyncConfig.Default with
         UnicastOnly = Some(true);
         Hosts = Some([ "localhost" ]) }

    options.Apply()
    
    VsyncSystem.Start()

    printfn "done."

    let pins = new PinGroup("iris.pins")
    pins.group.Join()

    let pin : Pin =
      { Id = System.Guid.NewGuid().ToString()
      ; Name = "YeahPin"
      ; IOBoxes = Array.empty
      }

    pins.Add(pin)
    pins.Send(PinAction.Add, pin)
    pins.Dump()

    VsyncSystem.WaitForever()

    0
