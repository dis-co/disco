namespace Iris.Service.Groups

open System.Collections.Generic
open Nessos.FsPickler
open Iris.Core
open Iris.Core.Utils
open Iris.Core.Types
open Vsync

[<AutoOpen>]
module CueGroup =

  type Id = string

  type CueDict = Dictionary<Id,Cue>

  (* ---------- CueAction ---------- *)

  type CueAction =
    | Add 
    | Update 
    | Delete 
    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Add    -> 1
          | Update -> 2 
          | Delete -> 3 

  (* ---------- CueGroup ---------- *)

  type CueGroup(project : Project, grpname) as self = 
    let tag = "CueGroup"
 
    [<DefaultValue>] val mutable uri   : string
    [<DefaultValue>] val mutable group : VsyncGroup<CueAction>

    let AddHandler(action, cb) =
      self.group.AddHandler<Cue>(action, cb)

    let AllHandlers =
      [ (CueAction.Add,    self.CueAdded)
      ; (CueAction.Update, self.CueUpdated)
      ; (CueAction.Delete, self.CueDeleted)
      ]

    (* constructor *)
    do
      self.uri <- Uri.mkCueUri project grpname
      self.group <- new VsyncGroup<CueAction>(self.uri)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    // member self.Dump() =
    //   for cue in cues do
    //     logger tag <| sprintf "cue id: %s" cue.Key

    member self.Add(c : Cue) =
      //cues.Add(c.Id, c)
      logger tag "add"

    (* Become member of group *)
    member self.Join() = self.group.Join()
    member self.Leave() = self.group.Leave()

    (* CueAction on the group *)
    member self.Send(action : CueAction, cue : Cue) =
      self.group.Send(action, cue)

    (* State initialization and transfer *)
    member self.Initialize() =
      logger tag <| sprintf "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      logger tag <| sprintf "makeing a snapshot. %d cues in it" 0
      // for pair in cues do
      //   self.group.SendCheckpoint(pair.Value)
      // self.group.DoneCheckpoint()

    member self.LoadCheckpoint(cue : Cue) =
      // if cues.ContainsKey(cue.Id)
      // then cues.[cue.Id] <- cue
      // else cues.Add(cue.Id, cue)
      logger tag <| sprintf "loaded a snapshot. %d cues in it" 0

    (* View changes *)
    member self.ViewChanged(view : View) : unit =
      logger tag <| sprintf "viewid: %d" (view.GetViewid()) 

    (* Event Handlers for CueAction *)
    member self.CueAdded(cue : Cue) : unit =
      // if not <| cues.ContainsKey(cue.Id)
      // then
      //   self.Add(cue)
      //   logger tag "cue added cb"
      //   self.Dump()
      logger tag "cue added"

    member self.CueUpdated(cue : Cue) : unit =
      logger tag <| sprintf "%s updated" cue.Name

    member self.CueDeleted(cue : Cue) : unit =
      logger tag <| sprintf "%s removed" cue.Name
