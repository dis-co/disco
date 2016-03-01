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
  type ServiceContext(signature) =
    let tag = "ServiceContext"

    let mutable Signature : Signature option            = Some(signature)
    let mutable Members   : Map<string, Member>         = Map.empty
    let mutable Projects  : Map<string, ProjectContext> = Map.empty

    interface IDisposable with
      member self.Dispose() = ()

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
    member self.GetProjects
      with get() : ProjectData array =
        Map.toArray Projects
        |> Array.map (fun (id', ctx) -> ctx.Project.Data) 

    member self.LoadProject(path : FilePath) : Project option =
      let result = Project.Load path
      match result with
        | Some project ->
          let ctx = new ProjectContext(project)
          Projects <- Map.add project.Id ctx Projects
        | None -> logger tag "project could not be loaded"
      result

    member self.SaveProject(id' : string, msg : string) : unit =
      match Signature with
        | Some (signature) ->
          try
            let ctx = Map.find id' Projects 
            ctx.Project.Save(signature, msg)
          with
            | exn -> logger tag "Unable to save project. Project not found."
        | _ -> logger tag "Unable to save project. No signature supplied."

    member self.CreateProject(name : Name, path : FilePath) =
      let project = Project.Create name
      project.Path <- Some(path)
      project.Save(Option.get Signature, sprintf "Created %s" name)
      let ctx = new ProjectContext(project)
      Projects <- Map.add project.Id ctx Projects

    member self.CloseProject(id' : string) =
      try
        let ctx = Map.find id' Projects
        (ctx :> IDisposable).Dispose()
        Projects <- Map.remove id' Projects
      with
        | _ -> logger tag "project not loaded"
      
    member self.ProjectLoaded(id' : string) = 
      Map.containsKey id' Projects
    
