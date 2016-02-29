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
open Iris.Service.Core

[<AutoOpen>]
module ProjectGroup =

  //     _        _   _
  //    / \   ___| |_(_) ___  _ __  ___
  //   / _ \ / __| __| |/ _ \| '_ \/ __|
  //  / ___ \ (__| |_| | (_) | | | \__ \
  // /_/   \_\___|\__|_|\___/|_| |_|___/
  //
  [<RequireQualifiedAccess>]
  type Actions =
    | Pull

    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Pull -> 1

  //   ____
  //  / ___|_ __ ___  _   _ _ __
  // | |  _| '__/ _ \| | | | '_ \
  // | |_| | | | (_) | |_| | |_) |
  //  \____|_|  \___/ \__,_| .__/
  //                       |_|
  type ProjectGroup(project : Project) as self =
    let tag = "ProjectGroup"

    [<DefaultValue>] val mutable uri     : string
    [<DefaultValue>] val mutable group   : VsyncGroup<Actions>
    [<DefaultValue>] val mutable Project : Project

    let AddHandler(action, cb) =
      self.group.AddHandler<Project>(action, cb)

    let AllHandlers = [ (Actions.Pull, self.Pull) ]

    do
      self.Project <- project
      self.uri <- Uri.mkProjectUri project
      self.group <- new VsyncGroup<Actions>(self.uri)
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    //  ____       _
    // |  _ \  ___| |__  _   _  __ _
    // | | | |/ _ \ '_ \| | | |/ _` |
    // | |_| |  __/ |_) | |_| | (_| |
    // |____/ \___|_.__/ \__,_|\__, |
    //                         |___/
    member self.Dump() =
      if Option.isSome project.Path
        then sprintf "[project Dump] name=%s path=%s" project.Name (Option.get project.Path)
        else sprintf "[project Dump] name=%s path=<empty>" project.Name
      |> logger tag

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
    member self.Load(p : Project) =
      logger tag "should load project now"

    member self.Save(p : Project) =
      logger tag "should save project now"

    member self.Clone(p : Project) =
      logger tag "should clone project now"

    member self.Pull(p : Project) =
      logger tag "should pull project from remote now"

    member self.ChangeBranch(name : string) =
      logger tag "change branch now"

    member self.Join() = self.group.Join()
    member self.Leave() = self.group.Leave()

    (* Actions on the group *)
    member self.Send(action : Actions, project : Project) =
      self.group.Send(action, project)

    (* State initialization and transfer *)
    member self.Initialize() =
      logger tag "should load state from disk/vvvv now"

    member self.MakeCheckpoint(view : View) =
      self.group.SendCheckpoint(project)
      self.group.DoneCheckpoint()

      sprintf "made a snapshot. %s" project.Name
      |> logger tag

    member self.LoadCheckpoint(project : Project) =
      self.Project <- project

      sprintf "loaded a snapshot. project: %s" project.Name
      |> logger tag

    (* View changes *)
    member self.ViewChanged(view : View) : unit =
      sprintf "viewid: %d" (view.GetViewid())
      |> logger tag
