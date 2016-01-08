namespace Iris.Service.Types

open System.Collections.Generic
open Nessos.FsPickler
open Vsync

[<AutoOpen>]
module ProjectGroup =

  (* standard location to clone repo to *)
  let WORKSPACE = "/home/k/Iris/"

  type FilePath = string

  type CueList =
    { Id   : Id
    ; Name : string
    ; Cues : Id array
    }

  (* ---------- Project ---------- *)
  type Project =
    { Id       : Id
    ; Name     : string
    ; Path     : FilePath
    ; Cues     : Cue array
    ; CueLists : CueList array
    }

    static member FromBytes(data : byte[]) : Project =
      let s = FsPickler.CreateBinarySerializer()
      s.UnPickle<Project> data
    
    member self.ToBytes() =
      let s = FsPickler.CreateBinarySerializer()
      s.Pickle self

  (* ---------- ProjectAction ---------- *)

  type ProjectAction =
    | Create  = 1
    | Load    = 2
    | Save    = 3
    | Clone   = 4
    | Pull    = 5

  (* ---------- ProjectGroup ---------- *)

  type ProjectGroup(grpname : string) as self = 
    [<DefaultValue>] val mutable group   : IrisGroup
    [<DefaultValue>] val mutable project : Project option

    let toI (pa : ProjectAction) : int = int pa

    let bToP (f : Project -> unit) (bytes : byte[]) =
      f <| Project.FromBytes(bytes)
      
    let pToB (p : Project) : byte[] =
      p.ToBytes()

    let AddHandler(action, cb) =
      self.group.AddHandler(toI action, mkHandler(bToP cb))

    let AllHandlers =
      [ (ProjectAction.Load,  self.ProjectLoaded)
      ; (ProjectAction.Save,  self.ProjectSaved)
      ; (ProjectAction.Clone, self.ProjectCloned)
      ; (ProjectAction.Pull,  self.ProjectPull)
      ]

    do
      self.group <- new IrisGroup(grpname)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.AddCheckpointMaker(self.MakeCheckpoint)
      self.group.AddCheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    member self.Dump() =
      match self.project with
        | Some(project) -> 
          printfn "[project] name=%s path=%s" project.Name project.Path
        | None -> 
          printfn "[project] not loaded."

    member self.Load(p : Project) =
      printfn "should load project now"

    member self.Save(p : Project) =
      printfn "should save project now"

    member self.Clone(p : Project) =
      printfn "should clone project now"

    member self.Pull(p : Project) =
      printfn "should pull project from remote now"

    (* Become member of group *)
    member self.Join() = self.group.Join()

    (* ProjectAction on the group *)
    member self.Send(action : ProjectAction, p : Project) =
      self.group.Send(toI action, p.ToBytes())

    (* State initialization and transfer *)
    member self.Initialize() =
      printfn "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      let s = FsPickler.CreateBinarySerializer()
      match self.project with
        | Some(project) -> 
          printfn "makeing a snapshot. %s" project.Name
          self.group.SendChkpt(s.Pickle project)
        | _ -> self.group.SendChkpt(s.Pickle None)
      self.group.EndOfChkpt()

    member self.LoadCheckpoint(bytes : byte[]) =
      let s = FsPickler.CreateBinarySerializer()
      self.project <- s.UnPickle<Project option> bytes

      match self.project with
        | Some(p) -> printfn "loaded a snapshot. project: %s" p.Name
        | None -> printfn "loaded snapshot. no project loaded yet."

    (* View changes *)
    member self.ViewChanged(view : View) : unit =
      printfn "viewid: %d" <| view.GetViewid() 

    (* Event Handlers for ProjectAction *)
    member self.ProjectLoaded(project : Project) : unit =
      printfn "project loaded "

    member self.ProjectSaved(project : Project) : unit =
      printfn "project saved"

    member self.ProjectCloned(project : Project) : unit =
      printfn "project cloned"

    member self.ProjectPull(project : Project) : unit =
      printfn "project pull"
