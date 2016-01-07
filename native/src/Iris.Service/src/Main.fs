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

  type IrisActions =
    | Init
    | Update
    | Close
    interface Intable<IrisActions> with
      member self.ToInt() =
        match self with
          | Init   -> 1
          | Update -> 2
          | Close  -> 3

  let initialize (msg : byte [])= 
    let s = IrisMsg.FromBytes msg
    printfn "%s" <| s.ToString()
 
  [<EntryPoint>]
  let main argv =
    printfn "starting engine"

    Environment.SetEnvironmentVariable("VSYNC_UNICAST_ONLY", "true")
    Environment.SetEnvironmentVariable("VSYNC_HOSTS", "localhost")
    
    VsyncSystem.Start()

    let g = new IrisGroup<IrisActions,byte[]> "test"

    g.AddHandler(Init, new Handler<byte[]>(initialize))
    g.AddViewHandler(fun view -> printfn "new view: %s" <| view.ToString())
    g.Join()

    let k = new IrisMsg("karsten")

    g.MySend(Init, k.ToBytes())

    VsyncSystem.WaitForever()

    0
