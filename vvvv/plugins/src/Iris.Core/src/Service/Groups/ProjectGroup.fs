namespace Iris.Service.Groups

open System
open System.IO
open System.Collections.Generic
open Nessos.FsPickler
open LibGit2Sharp
open Iris.Core.Types
open Vsync
open Iris.Core

[<AutoOpen>]
module ProjectGroup =
  (* ---------- ProjectAction ---------- *)
  type ProjectAction =
    | Create 
    | Load 
    | Save
    | Clone  
    | Pull
    
    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Create -> 1
          | Load   -> 2
          | Save   -> 3
          | Clone  -> 4
          | Pull   -> 5
          
  (* ---------- ProjectGroup ---------- *)
  type ProjectGroup(grpname : string) as self = 
    [<DefaultValue>] val mutable group   : IrisGroup<ProjectAction,Project>
    [<DefaultValue>] val mutable project : Project option

    let AddHandler(action, cb) =
      self.group.AddHandler(action, cb)

    let AllHandlers =
      [ (ProjectAction.Load,  self.ProjectLoaded)
      ; (ProjectAction.Save,  self.ProjectSaved)
      ; (ProjectAction.Clone, self.ProjectCloned)
      ; (ProjectAction.Pull,  self.ProjectPull)
      ]

    do
      self.group <- new IrisGroup<ProjectAction,Project>(grpname)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    member self.Dump() =
      match self.project with
        | Some(project) -> 
          if Option.isSome project.Path
          then printfn "[project] name=%s path=%s" project.Name (Option.get project.Path)
          else printfn "[project] name=%s path=<empty>" project.Name
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
    member self.Send(action : ProjectAction, project : Project) =
      self.group.Send(action, project)

    (* State initialization and transfer *)
    member self.Initialize() =
      printfn "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      match self.project with
        | Some(project) -> 
          printfn "makeing a snapshot. %s" project.Name
          self.group.SendCheckpoint(project)
        | _ -> printfn "no project loaded. nothing to checkpoint"
      self.group.DoneCheckpoint()

    member self.LoadCheckpoint(project : Project) =
      self.project <- Some(project)

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
