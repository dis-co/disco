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

  type PinDict = Dictionary<Id,Pin>

  (* ---------- PinAction ---------- *)

  type PinAction =
    | Add
    | Update
    | Delete
    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Add    -> 1
          | Update -> 2
          | Delete -> 3

  (* ---------- PinGroup ---------- *)

  type PinGroup(grpname) as self = 
    [<DefaultValue>] val mutable group : IrisGroup<PinAction, Pin>

    let mutable pins : PinDict = new Dictionary<Id,Pin>()

    let AddHandler(action, cb) =
      self.group.AddHandler(action, cb)

    let AllHandlers =
      [ (PinAction.Add,    self.PinAdded)
      ; (PinAction.Update, self.PinUpdated)
      ; (PinAction.Delete, self.PinDeleted)
      ]

    do
      self.group <- new IrisGroup<PinAction,Pin>(grpname)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
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
      self.group.Send(action, p)

    (* State initialization and transfer *)
    member self.Initialize() =
      printfn "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      printfn "makeing a snapshot. %d pins in it" pins.Count
      for pair in pins do
        self.group.SendCheckpoint(pair.Value)
      self.group.DoneCheckpoint()

    member self.LoadCheckpoint(pin : Pin) =
      if not <| pins.ContainsKey pin.Id
      then pins.Add(pin.Id, pin)
      else pins.[pin.Id] <- pin

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
