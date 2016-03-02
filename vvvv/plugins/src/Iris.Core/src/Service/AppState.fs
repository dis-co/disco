namespace Iris.Service.Core

open System
open LibGit2Sharp
open Iris.Core.Utils
open Iris.Core.Types

[<AutoOpen>]
module AppState =

  let flip (f : 'a -> 'b -> 'c) (b : 'b) (a : 'a) = f a b
 
  //     _               ____  _        _
  //    / \   _ __  _ __/ ___|| |_ __ _| |_ ___
  //   / _ \ | '_ \| '_ \___ \| __/ _` | __/ _ \
  //  / ___ \| |_) | |_) |__) | || (_| | ||  __/
  // /_/   \_\ .__/| .__/____/ \__\__,_|\__\___|
  //         |_|   |_|
  type AppState =
    { Members  : Member list
    ; Projects : Map<Guid,Project>
    }
    static member empty
      with get () =  { Members = List.empty; Projects = Map.empty }

  let addProject (project : Project) (state : AppState) : Either<string,AppState> =
    { state with Projects = Map.add project.Id project state.Projects }
    |> succeed

  let findProject guid state : Either<string, Project> =
    match Map.tryFind guid state.Projects with
      | Some project -> succeed project
      | _ -> fail "Project not found"
    
  let loadProject path state : Either<string,(Project * AppState)> =
    Project.Load path
      >>= fun project -> combine project (addProject project state)

  let saveProject (guid : Guid) (sign : Signature) msg state : Either<string,(Commit * AppState)> =
    findProject guid state
      >>= fun project ->
        try
          project.Save(sign, msg)
            >>= (fun commit -> succeed (commit, state))
        with
          | exn -> fail exn.Message

  let createProject name path (sign : Signature) state : Either<string,(Project * AppState)> = 
    let now = System.DateTime.Now.ToLongTimeString()
    let msg = sprintf "On %s, %s created %s" now sign.Name name
    let project = Project.Create name
    project.Path <- Some(path)
    addProject project state 
      >>= saveProject project.Id sign msg
      >>= (fun (_,s) -> succeed (project, s))
  
  let closeProject (guid : Guid) (state : AppState) : Either<string, AppState> =
    { state with Projects = Map.remove guid state.Projects }
    |> succeed
  
  let projectLoaded (guid : Guid) (state : AppState) : bool =
    isSuccess (findProject guid state)

  let addMember (mem: Member) (state: AppState) : AppState =
    { state with Members = mem :: state.Members }
  
  let updateMember (newmem: Member) (state: AppState) : AppState =
    let helper old = if sameAs old newmem then newmem else old
    { state with Members = List.map helper state.Members  }

  let removeMember (mem: Member) (state: AppState) : AppState = 
    { state with Members = List.filter (not << sameAs mem) state.Members  }
