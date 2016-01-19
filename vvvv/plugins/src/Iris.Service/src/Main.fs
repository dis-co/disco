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

  (* IrisMsg *)

  type IrisMsg(name') =
    let mutable name = name'
  
    do printfn "name is: %s" name
  
    member self.Name
      with get () = name
      and  set n  = name <- n
  
    override self.ToString () =
      sprintf "IrisMsg: %s" name

    member self.ToBytes() : byte[] =
      let pickler = FsPickler.CreateBinarySerializer()
      pickler.Pickle self

    static member FromBytes(data : byte[]) : IrisMsg =
      let pickler = FsPickler.CreateBinarySerializer()
      pickler.UnPickle<IrisMsg> data

  let initialize (msg : byte [])= 
    let s = IrisMsg.FromBytes msg
    printfn "%s" <| s.ToString()
 
  [<EntryPoint>]
  let main argv =
    printfn "starting engine.."

    Environment.SetEnvironmentVariable("VSYNC_UNICAST_ONLY", "true")
    Environment.SetEnvironmentVariable("VSYNC_HOSTS", "localhost")
    
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
