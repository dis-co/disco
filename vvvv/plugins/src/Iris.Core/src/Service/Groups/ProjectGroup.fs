namespace Iris.Service.Groups

open System
open System.IO
open System.Collections.Generic
open Nessos.FsPickler
open LibGit2Sharp
open Iris.Core.Types
open Vsync
open Iris.Core
open Iris.Core.Utils

[<AutoOpen>]
module ProjectGroup =

  (* ---------- ProjectAction ---------- *)
  [<RequireQualifiedAccess>]
  type Actions =
    | Pull

    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Pull -> 1

  (* ---------- ProjectGroup ---------- *)
  type ProjectGroup(grpname : string) as self =
    let tag = "ProjectGroup"
    
    [<DefaultValue>] val mutable group   : IrisGroup<Actions,Project>
    [<DefaultValue>] val mutable project : Project option

    let AddHandler(action, cb) =
      self.group.AddHandler(action, cb)

    let AllHandlers =
      [ (Actions.Pull,  self.ProjectPull)
      ]

    do
      self.group <- new IrisGroup<Actions,Project>(grpname)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    member self.Dump() =
      match self.project with
        | Some(project) ->
          if Option.isSome project.Path
          then logger tag <| sprintf "[project Dump] name=%s path=%s" project.Name (Option.get project.Path)
          else logger tag <| sprintf "[project Dump] name=%s path=<empty>" project.Name
        | None ->
          logger tag "[project Dump] not loaded."

    member self.Load(p : Project) =
      logger tag "should load project now"

    member self.Save(p : Project) =
      logger tag "should save project now"

    member self.Clone(p : Project) =
      logger tag "should clone project now"

    member self.Pull(p : Project) =
      logger tag "should pull project from remote now"

    (* Become member of group *)
    member self.Join() = self.group.Join()

    (* Actions on the group *)
    member self.Send(action : Actions, project : Project) =
      self.group.Send(action, project)

    (* State initialization and transfer *)
    member self.Initialize() =
      logger tag "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      match self.project with
        | Some(project) ->
          logger tag <| sprintf "makeing a snapshot. %s" project.Name
          self.group.SendCheckpoint(project)
        | _ -> logger tag "no project loaded. nothing to checkpoint"
      self.group.DoneCheckpoint()

    member self.LoadCheckpoint(project : Project) =
      self.project <- Some(project)

      match self.project with
        | Some(p) -> logger tag <| sprintf "loaded a snapshot. project: %s" p.Name
        | None -> logger tag "loaded snapshot. no project loaded yet."

    (* View changes *)
    member self.ViewChanged(view : View) : unit =
      logger tag <| sprintf "viewid: %d" (view.GetViewid())

    (* Event Handlers for Actions *)
    member self.ProjectLoaded(project : Project) : unit =
      logger tag "project loaded "

    member self.ProjectSaved(project : Project) : unit =
      logger tag "project saved"

    member self.ProjectCloned(project : Project) : unit =
      logger tag "project cloned"

    member self.ProjectPull(project : Project) : unit =
      logger tag "project pull"
