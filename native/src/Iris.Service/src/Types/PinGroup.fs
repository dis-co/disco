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

  type PinDict = Dictionary<Id,Pin>

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
    [<DefaultValue>] val mutable group : IrisGroup

    let mutable pins : PinDict = new Dictionary<Id,Pin>()

    let toI (pa : PinAction) : int = int pa

    let bToP (f : Pin -> unit) (bytes : byte[]) =
      f <| Pin.FromBytes(bytes)
      
    let pToB (p : Pin) : byte[] =
      p.ToBytes()

    let AddHandler(action, cb) =
      self.group.AddHandler(toI action, mkHandler(bToP cb))

    let AllHandlers =
      [ (PinAction.Add,    self.PinAdded)
      ; (PinAction.Update, self.PinUpdated)
      ; (PinAction.Delete, self.PinDeleted)
      ]

    do
      self.group <- new IrisGroup(grpname)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.AddCheckpointMaker(self.MakeCheckpoint)
      self.group.AddCheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    member self.Dump() =
      for pin in pins do
        printfn "pin id: %s" pin.Key

    member self.Add(p : Pin) =
      pins.Add(p.Id, p)

    (* Become member of group *)
    member self.Join() = self.group.Join()

    (* PinAction on the group *)
    member self.Send(action : PinAction, p : Pin) =
      self.group.Send(toI action, p.ToBytes())

    (* State initialization and transfer *)
    member self.Initialize() =
      printfn "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      printfn "makeing a snapshot. %d pins in it" pins.Count
      let s = FsPickler.CreateBinarySerializer()
      self.group.SendChkpt(s.Pickle pins)
      self.group.EndOfChkpt()

    member self.LoadCheckpoint(bytes : byte[]) =
      let s = FsPickler.CreateBinarySerializer()
      pins <- s.UnPickle<PinDict> bytes
      printfn "loaded a snapshot. %d pins in it" pins.Count

    (* View changes *)
    member self.ViewChanged(view : View) : unit =
      printfn "viewid: %d" <| view.GetViewid() 

    (* Event Handlers for PinAction *)
    member self.PinAdded(pin : Pin) : unit =
      if not <| pins.ContainsKey(pin.Id)
      then
        self.Add(pin)
        printfn "pin added cb: "
        self.Dump()

    member self.PinUpdated(pin : Pin) : unit =
      printfn "%s updated" pin.Name

    member self.PinDeleted(pin : Pin) : unit =
      printfn "%s removed" pin.Name
