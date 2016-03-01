namespace Iris.Service.Core

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
    [<DefaultValue>] val mutable Members  : Member list
    [<DefaultValue>] val mutable Projects : Map<string, Project>

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

    member self.Load(path) : Project option =
      match Project.Load path with
        | Some project as result ->
          self.Add project
          result
        | none -> none

    member self.Find(pid : string) =
      Map.tryFind pid self.Projects

    member self.Save(pid, sign, msg) =
      match self.Find(pid) with
        | Some(project) -> project.Save(sign, msg)
        | _ -> ()

    member self.Create(name, path) =
      let project = Project.Create name
      project.Path <- Some(path)
      self.Add(project)

    member self.Close(pid) =
      match self.Find(pid) with
        | Some project as result -> 
          self.Projects <- Map.remove pid self.Projects
          result
        | none -> none

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

  // Option_  ___  _   _    _    ____  _
  // |  \/  |/ _ \| \ | |  / \  |  _ \| |
  // | |\/| | | | |  \| | / _ \ | | | | |
  // | |  | | |_| | |\  |/ ___ \| |_| |_|
  // |_|  |_|\___/|_| \_/_/   \_\____/(_)ish
  let (>>=) (a : 'a option) (f : 'a -> 'b option) =
    match a with
      | Some(t) -> f t
      | None    -> None

  let returnO a = Some(a)
