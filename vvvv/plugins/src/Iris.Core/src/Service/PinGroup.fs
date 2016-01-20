namespace Iris.Service

open System.Collections.Generic
open Iris.Core.Types
open Nessos.FsPickler
open Vsync

/// Group for 
[<AutoOpen>]
module PinGroup =

  type IOBoxDict = Dictionary<Id,IOBox>

  /// Actions defined for 
  type BoxAction =
    | Add
    | Update
    | Delete
    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Add    -> 1
          | Update -> 2
          | Delete -> 3

  /// Group to manage all replicated IOBox state in the distributed app
  type PinGroup(grpname) as self = 
    [<DefaultValue>] val mutable group : IrisGroup<BoxAction, IOBox>

    let mutable boxes : IOBoxDict = new IOBoxDict()

    let AddHandler(action, cb) =
      self.group.AddHandler(action, cb)

    let AllHandlers =
      [ (BoxAction.Add,    self.PinAdded)
      ; (BoxAction.Update, self.PinUpdated)
      ; (BoxAction.Delete, self.PinDeleted)
      ]

    do
      self.group <- new IrisGroup<BoxAction,IOBox>(grpname)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    member self.Dump() =
      for box in boxes do
        printfn "pin id: %s" box.Key

    member self.Add(box : IOBox) =
      boxes.Add(box.Id, box)

    (* Become member of group *)
    member self.Join() = self.group.Join()

    (* BoxAction on the group *)
    member self.Send(action : BoxAction, box : IOBox) =
      self.group.Send(action, box)

    (* State initialization and transfer *)
    member self.Initialize() =
      printfn "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      printfn "makeing a snapshot. %d pins in it" boxes.Count
      for pair in boxes do
        self.group.SendCheckpoint(pair.Value)
      self.group.DoneCheckpoint()

    member self.LoadCheckpoint(box : IOBox) =
      if not <| boxes.ContainsKey box.Id
      then boxes.Add(box.Id, box)
      else boxes.[box.Id] <- box

      printfn "loaded a snapshot. %d pins in it" boxes.Count

    (* View changes *)
    member self.ViewChanged(view : View) : unit =
      printfn "viewid: %d" <| view.GetViewid() 

    (* Event Handlers for BoxAction *)
    member self.PinAdded(box : IOBox) : unit =
      if not <| boxes.ContainsKey(box.Id)
      then
        self.Add(box)
        printfn "pin added cb: "
        self.Dump()

    member self.PinUpdated(box : IOBox) : unit =
      printfn "%s updated" box.Name

    member self.PinDeleted(box : IOBox) : unit =
      printfn "%s removed" box.Name
