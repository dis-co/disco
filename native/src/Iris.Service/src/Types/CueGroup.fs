namespace Iris.Service.Types

open System.Collections.Generic
open Nessos.FsPickler
open Vsync

[<AutoOpen>]
module CueGroup =

  type Id = string

  (* ---------- Cue ---------- *)
  type Cue =
    { Id   : Id
    ; Name : string
    ; Pins : string array
    }

    static member FromBytes(data : byte[]) : Cue =
      let s = FsPickler.CreateBinarySerializer()
      s.UnPickle<Cue> data
    
    member self.ToBytes() =
      let s = FsPickler.CreateBinarySerializer()
      s.Pickle self

  type CueDict = Dictionary<Id,Cue>

  (* ---------- CueAction ---------- *)

  type CueAction =
    | Add    = 1
    | Update = 2
    | Delete = 3

  (* ---------- CueGroup ---------- *)

  type CueGroup(grpname) as self = 
    [<DefaultValue>] val mutable group : IrisGroup

    let mutable cues : CueDict = new CueDict()

    let toI (pa : CueAction) : int = int pa

    let bToC (f : Cue -> unit) (bytes : byte[]) =
      f <| Cue.FromBytes(bytes)
      
    let cToB (c : Cue) : byte[] =
      c.ToBytes()

    let AddHandler(action, cb) =
      self.group.AddHandler(toI action, mkHandler(bToC cb))

    let AllHandlers =
      [ (CueAction.Add,    self.CueAdded)
      ; (CueAction.Update, self.CueUpdated)
      ; (CueAction.Delete, self.CueDeleted)
      ]

    (* constructor *)
    do
      self.group <- new IrisGroup(grpname)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.AddCheckpointMaker(self.MakeCheckpoint)
      self.group.AddCheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    member self.Dump() =
      for cue in cues do
        printfn "cue id: %s" cue.Key

    member self.Add(c : Cue) =
      cues.Add(c.Id, c)

    (* Become member of group *)
    member self.Join() = self.group.Join()

    (* CueAction on the group *)
    member self.Send(action : CueAction, c : Cue) =
      self.group.Send(toI action, c.ToBytes())

    (* State initialization and transfer *)
    member self.Initialize() =
      printfn "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      printfn "makeing a snapshot. %d cues in it" cues.Count
      let s = FsPickler.CreateBinarySerializer()
      self.group.SendChkpt(s.Pickle cues)
      self.group.EndOfChkpt()

    member self.LoadCheckpoint(bytes : byte[]) =
      let s = FsPickler.CreateBinarySerializer()
      cues <- s.UnPickle<CueDict> bytes
      printfn "loaded a snapshot. %d cues in it" cues.Count

    (* View changes *)
    member self.ViewChanged(view : View) : unit =
      printfn "viewid: %d" <| view.GetViewid() 

    (* Event Handlers for CueAction *)
    member self.CueAdded(cue : Cue) : unit =
      if not <| cues.ContainsKey(cue.Id)
      then
        self.Add(cue)
        printfn "cue added cb: "
        self.Dump()

    member self.CueUpdated(cue : Cue) : unit =
      printfn "%s updated" cue.Name

    member self.CueDeleted(cue : Cue) : unit =
      printfn "%s removed" cue.Name
