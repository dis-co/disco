namespace Iris.Service.Contexts

open System
open LibGit2Sharp
open Iris.Core.Utils
open Iris.Core.Types
open Iris.Service.Core

[<AutoOpen>]
module Service =

  //  ____                  _           ____            _            _
  // / ___|  ___ _ ____   _(_) ___ ___ / ___|___  _ __ | |_ _____  _| |_
  // \___ \ / _ \ '__\ \ / / |/ __/ _ \ |   / _ \| '_ \| __/ _ \ \/ / __|
  //  ___) |  __/ |   \ V /| | (_|  __/ |__| (_) | | | | ||  __/>  <| |_
  // |____/ \___|_|    \_/ |_|\___\___|\____\___/|_| |_|\__\___/_/\_\\__|
  type ServiceContext(signature) as this =
    let tag = "Context"

    let mutable Signature : Signature option   = Some(signature)
    let mutable Members   : Map<string,Member> = Map.empty

    [<DefaultValue>] val mutable Project : Project option

    interface IDisposable with
      member self.Dispose() = ()

    do this.Project <- None

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
    member self.LoadProject(path : FilePath) : unit =
      self.Project <- loadProject path

    member self.SaveProject(msg : string) : unit =
      if Option.isSome Signature
      then
        let signature = Option.get Signature
        match self.Project with
          | Some(project) -> saveProject project signature msg
          | _ -> logger tag "No project loaded."
      else logger tag "Unable to save project. No signature supplied."

    member self.CreateProject(name : Name, path : FilePath) =
      let project = createProject name
      project.Path <- Some(path)
      self.Project <- Some(project)
      self.SaveProject(sprintf "Created %s" name)

    member self.CloseProject(name : Name) =
      if self.ProjectLoaded(name)
      then
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
