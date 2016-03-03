namespace Iris.Service.Groups

open System.Collections.Generic
open Iris.Core
open Iris.Core.Utils
open Iris.Core.Types
open Nessos.FsPickler
open Vsync

/// Group for 
[<AutoOpen>]
module PinGroup =

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
  type PinGroup(project : Project, grpname) as self = 
    let tag = "PinGroup"
 
    [<DefaultValue>] val mutable uri   : string
    [<DefaultValue>] val mutable group : VsyncGroup<BoxAction>

    let AddHandler(action, cb) =
      self.group.AddHandler<IOBox>(action, cb)

    let AllHandlers =
      [ (BoxAction.Add,    self.PinAdded)
      ; (BoxAction.Update, self.PinUpdated)
      ; (BoxAction.Delete, self.PinDeleted)
      ]

    do
      self.uri <- Uri.mkPinUri project grpname 
      self.group <- new VsyncGroup<BoxAction>(self.uri)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    // member self.Dump() =
    //   for box in boxes do
    //     logger tag <| sprintf "pin id: %s" box.Key

    member self.Add(box : IOBox) =
      logger tag "add box"

    (* Become member of group *)
    member self.Join()  = self.group.Join()

    member self.Leave() =
      self.group.Leave()
      self.group.Dispose()

    (* BoxAction on the group *)
    member self.Send(action : BoxAction, box : IOBox) =
      self.group.Send(action, box)

    (* State initialization and transfer *)
    member self.Initialize() =
      logger tag "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      logger tag <| sprintf "makeing a snapshot. %d pins in it" 0
      // for pair in boxes do
      //   self.group.SendCheckpoint(pair.Value)
      self.group.DoneCheckpoint()

    member self.LoadCheckpoint(box : IOBox) =
      // if not <| boxes.ContainsKey box.Id
      // then boxes.Add(box.Id, box)
      // else boxes.[box.Id] <- box
      logger tag <| sprintf "loaded a snapshot. %d pins in it" 0

    (* View changes *)
    member self.ViewChanged(view : View) : unit =
      logger tag <| sprintf "viewid: %d" (view.GetViewid()) 

    (* Event Handlers for BoxAction *)
    member self.PinAdded(box : IOBox) : unit =
      logger tag "pin added"
      // if not <| boxes.ContainsKey(box.Id)
      // then
      //   self.Add(box)
      //   logger tag "pin added cb: "
      //   self.Dump()

    member self.PinUpdated(box : IOBox) : unit =
      logger tag <| sprintf "%s updated" box.Name

    member self.PinDeleted(box : IOBox) : unit =
      logger tag <| sprintf "%s removed" box.Name
