namespace Iris.Core.Types

open System
open LibGit2Sharp
open Iris.Core.Utils
open Iris.Service.Core

[<AutoOpen>]
module Context =

  type Context(signature) as this =
    let tag = "Context"

    let mutable GitDaemon : Git.Daemon option  = None
    let mutable Signature : Signature option   = Some(signature)
    let mutable Members   : Map<string,Member> = Map.empty

    [<DefaultValue>] val mutable Project : Project option

    interface IDisposable with
      member self.Dispose() =
        self.StopDaemon()

    do this.Project <- None

    //   ____ _ _
    //  / ___(_) |_
    // | |  _| | __|
    // | |_| | | |_
    //  \____|_|\__| daemon
    //
    member self.StopDaemon() =
      match GitDaemon with
        | Some(daemon) -> daemon.Stop()
        | _ -> ()

    member self.StartDaemon() =
      match self.Project with
        | Some(project) ->
          self.StopDaemon()
          new Git.Daemon(Option.get project.Path)
          |> (fun daemon ->
            daemon.Start()
            GitDaemon <- Some(daemon))
        | None -> logger tag "project not found"

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
    member self.LoadProject(path : FilePath) : unit =
      self.StopDaemon()
      self.Project <- loadProject path
      self.StartDaemon()

    member self.SaveProject(msg : string) : unit =
      if Option.isSome Signature
      then
        let signature = Option.get Signature
        match self.Project with
          | Some(project) -> saveProject project signature msg
          | _ -> logger tag "No project loaded."
      else logger tag "Unable to save project. No signature supplied."

    member self.CreateProject(name : Name, path : FilePath) =
      self.StopDaemon()
      let project = createProject name
      project.Path <- Some(path)
      self.Project <- Some(project)
      self.SaveProject(sprintf "Created %s" name)
      self.StartDaemon()

    member self.CloseProject(name : Name) =
      if self.ProjectLoaded(name)
      then
        self.StopDaemon()
        self.Project <- None
      
    member self.ProjectLoaded(name : Name) = 
      Option.isSome self.Project && (Option.get self.Project).Name = name
    
    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __ ___
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
    // | |  | |  __/ | | | | | |_) |  __/ |  \__ \
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|  |___/
    //
    member self.AddMember(mem : Member) =
      Members <- Map.add mem.Name mem Members

    member self.UpdateMember(mem : Member) =
      Members <- Map.add mem.Name mem Members

    member self.RemoveMember(mem : Member) =
      Members <- Map.remove mem.Name Members

    member self.GetMembers() : Member [] =
      Map.toArray Members
      |> Array.map snd 
