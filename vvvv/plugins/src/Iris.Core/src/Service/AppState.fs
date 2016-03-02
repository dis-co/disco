namespace Iris.Service.Core

open System
open LibGit2Sharp
open Iris.Core.Utils
open Iris.Core.Types

[<AutoOpen>]
module AppState =

  //     _               ____  _        _
  //    / \   _ __  _ __/ ___|| |_ __ _| |_ ___
  //   / _ \ | '_ \| '_ \___ \| __/ _` | __/ _ \
  //  / ___ \| |_) | |_) |__) | || (_| | ||  __/
  // /_/   \_\ .__/| .__/____/ \__\__,_|\__\___|
  //         |_|   |_|
  type AppState() as self =
    let tag = "AppState"

    [<DefaultValue>] val mutable Members  : Member list
    [<DefaultValue>] val mutable Projects : Map<Guid, Project>

    do self.Members  <- List.empty
       self.Projects <- Map.empty

    static member empty
      with get () =  new AppState()

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_ ___
    // | |_) | '__/ _ \| |/ _ \/ __| __/ __|
    // |  __/| | | (_) | |  __/ (__| |_\__ \
    // |_|   |_|  \___// |\___|\___|\__|___/
    //               |__/
    member self.Add(project : Project) =
      self.Projects <- Map.add project.Id project self.Projects

    member self.Load(path) : Either<string, Project> =
      match Project.Load path with
        | Success project as result->
          self.Add project
          result
        | err -> err

    member self.Find(pid : Guid) =
      Map.tryFind pid self.Projects

    member self.Save(pid, sign, msg) : Either<string,Commit> =
      match self.Find(pid) with
        | Some(project) -> project.Save(sign, msg)
        | _ -> Fail "project not found"

    member self.Create(name, path, sign : Signature) : Either<string, Project> =
      let now = System.DateTime.Now.ToLongTimeString()
      let project = Project.Create name
      project.Path <- Some(path)
      try 
        let msg = sprintf "On %s, %s created %s" now sign.Name name
        project.Save(sign, msg) |> ignore
        self.Add(project)
        Success(project)
      with
        | exn -> Fail(exn.Message)

    member self.Close(pid) : Either<string, Project> =
      match self.Find(pid) with
        | Some project -> 
          self.Projects <- Map.remove pid self.Projects
          Success project
        | _ -> Fail("Project not found.")

    member self.Loaded(pid) =
      Map.toList self.Projects
      |> List.exists (fun (_, p: Project)-> p.Id = pid)

    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __ ___
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
    // | |  | |  __/ | | | | | |_) |  __/ |  \__ \
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|  |___/
    member self.Add(mem : Member) =
      self.Members <- mem :: self.Members

    member self.Update(mem : Member) =
      self.Members <- List.map (fun m -> if mem = m then mem else m) self.Members

    member self.Remove (mem : Member) =
      self.Members <- List.filter ((=) mem) self.Members
