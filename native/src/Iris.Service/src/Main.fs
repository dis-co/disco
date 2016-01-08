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

  type IrisMsg(name') =
    let mutable name = name'
  
    do printfn "name is: %s" name
  
    member self.Name
      with get () = name
      and  set n  = name <- n
  
    override self.ToString () =
      sprintf "IrisMsg: %s" name

    member self.ToBytes() : byte[] =
      let s = FsPickler.CreateBinarySerializer()
      s.Pickle self

    static member FromBytes(data : byte[]) : IrisMsg =
      let s = FsPickler.CreateBinarySerializer()
      s.UnPickle<IrisMsg> data

  let initialize (msg : byte [])= 
    let s = IrisMsg.FromBytes msg
    printfn "%s" <| s.ToString()
 
  let oldman () =
    printfn "starting engine"

    Environment.SetEnvironmentVariable("VSYNC_UNICAST_ONLY", "true")
    Environment.SetEnvironmentVariable("VSYNC_HOSTS", "localhost")
    
    VsyncSystem.Start()

    let g = new PinGroup("iris.pins")
    g.group.Join()

    let p : Pin =
      { Id = System.Guid.NewGuid().ToString()
      ; Name = "YeahPin"
      ; IOBoxes = Array.empty
      }

    g.Add(p)
    g.Send(PinAction.Add, p)
    g.Dump()

    VsyncSystem.WaitForever()

    0

  [<EntryPoint>]
  let main argv =
    printfn "workspace: %s" WORKSPACE
    createProject "yea I am super awesome"
    0
