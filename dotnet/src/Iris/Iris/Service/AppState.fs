namespace Iris.Service

open System
open LibGit2Sharp
open Iris.Core.Utils
open Iris.Core
open Pallet.Core

//     _               ____  _        _
//    / \   _ __  _ __/ ___|| |_ __ _| |_ ___
//   / _ \ | '_ \| '_ \___ \| __/ _` | __/ _ \
//  / ___ \| |_) | |_) |__) | || (_| | ||  __/
// /_/   \_\ .__/| .__/____/ \__\__,_|\__\___|
//         |_|   |_|

type AppState =
  { Clients:  IrisNode       list
  ; Sessions: BrowserSession list
  ; Projects: Map<Guid, Project>
  ; Raft:     Raft
  }

[<AutoOpen>]
module AppStateUtils =

  let addProject (project : Project) (state : AppState) : Either<string,AppState> =
    { state with Projects = Map.add project.Id project state.Projects }
    |> Either.succeed

  let findProject guid state : Either<string, Project> =
    match Map.tryFind guid state.Projects with
      | Some project -> Either.succeed project
      | _ -> Either.fail "Project not found"
    
  let loadProject path state : Either<string,(Project * AppState)> =
    match Project.Load path with
      | Success project -> Either.combine project (addProject project state)
      | Fail err        -> Either.fail err

  let saveProject (guid : Guid) (sign : Signature) msg state : Either<string,(Commit * AppState)> =
    match findProject guid state with
      | Success project ->
          try match project.Save(sign, msg) with
                | Success commit -> Either.succeed (commit, state)
                | Fail err       -> Either.fail err
          with
            | exn -> Either.fail exn.Message
      | Fail err -> Either.fail err
        
  let createProject name path (sign : Signature) state : Either<string,(Project * AppState)> = 
    let now = System.DateTime.Now
    let msg = sprintf "On %s, %s created %s" (now.ToLongTimeString()) sign.Name name
    let project = Project.Create name
    project.Path <- Some(path)
    match addProject project state with
      | Success state' ->
        match saveProject project.Id sign msg state' with
          | Success(commit, state'') -> Either.succeed (project, state'')
          | Fail err                 -> Either.fail err
      | Fail err -> Either.fail err

  let closeProject (guid : Guid) (state : AppState) : Either<string, AppState> =
    { state with Projects = Map.remove guid state.Projects }
    |> Either.succeed
  
  let projectLoaded (guid : Guid) (state : AppState) : bool =
    Either.isSuccess (findProject guid state)

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  let updateRaft (state: AppState) (raft: Raft) : AppState =
    { state with Raft = raft }

  //  _   _           _
  // | \ | | ___   __| | ___  ___
  // |  \| |/ _ \ / _` |/ _ \/ __|
  // | |\  | (_) | (_| |  __/\__ \
  // |_| \_|\___/ \__,_|\___||___/

  let addNode (node: IrisNode) (state: AppState) : AppState =
    failwith "FIXME: implement addNode AppState"
  
  let updateMember (newmem: IrisNode) (state: AppState) : AppState =
    failwith "FIXME: implement updateMember AppState"

  let removeMember (mem: IrisNode) (state: AppState) : AppState = 
    failwith "FIXME: implement removeNode AppState"
