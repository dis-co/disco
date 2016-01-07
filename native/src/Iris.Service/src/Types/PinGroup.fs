namespace Iris.Service.Types

open System.Collections.Generic
open Nessos.FsPickler
open Vsync

[<AutoOpen>]
module PinGroup =

  type Id = string

  (* ---------- Pin ---------- *)
  type Pin =
    { Id      : Id
    ; Name    : string
    ; IOBoxes : string array
    }

    static member FromBytes(data : byte[]) : Pin =
      let s = FsPickler.CreateBinarySerializer()
      s.UnPickle<Pin> data
    
    member self.ToBytes() =
      let s = FsPickler.CreateBinarySerializer()
      s.Pickle self
    
  (* ---------- Host ---------- *)

  type Host =
    { Id   : string
    ; Pins : Dictionary<Id,Pin>
    }
    static member Create(id : Id) =
      { Id = id; Pins = new Dictionary<Id, Pin>() }

  (* ---------- PinAction ---------- *)

  type PinAction =
    | Add    = 1
    | Update = 2
    | Delete = 3

  (* ---------- PinGroup ---------- *)

  type PinGroup(grpname) as self = 
    let group = new IrisGroup("iris.pins")

    let pins : Dictionary<Id, Pin> = new Dictionary<Id,Pin>()

    let toI (pa : PinAction) : int = int pa

    let bToP (f : Pin -> unit) (bytes : byte[]) =
      f <| Pin.FromBytes(bytes)
      
    let pToB (p : Pin) : byte[] =
      p.ToBytes()

    let AllHandlers =
      [ (PinAction.Add,    self.PinAdded)
      ; (PinAction.Update, self.PinUpdated)
      ; (PinAction.Delete, self.PinDeleted)
      ]

    do
      group.AddViewHandler(self.ViewChanged)
      List.iter (fun (a,cb) -> group.AddHandler(toI a, mkHandler(bToP cb))) AllHandlers

    member self.Join() = group.Join()

    member self.Dump() =
      for pin in pins do
        printfn "pin id: %s" pin.Key

    member self.Send(action : PinAction, p : Pin) =
      group.Send(toI action, p.ToBytes())

    member self.Add(p : Pin) =
      pins.Add(p.Id, p)
      self.Send(PinAction.Add, p)

    member self.ViewChanged(view : View) : unit =
      printfn "viewid: %d" <| view.GetViewid() 

    member self.PinAdded(pin : Pin) : unit =
      if not <| pins.ContainsKey(pin.Id)
      then
        self.Add(pin)
        printfn "pin added cb: "
      else
        printfn "pin already present"
      self.Dump()

    member self.PinUpdated(pin : Pin) : unit =
      printfn "%s updated" pin.Name

    member self.PinDeleted(pin : Pin) : unit =
      printfn "%s removed" pin.Name
