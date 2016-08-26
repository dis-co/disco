namespace Iris.Service

open LibGit2Sharp
open Iris.Core.Utils
open Iris.Core
open Iris.Raft
open Iris.Service.Raft

//     _               ____  _        _
//    / \   _ __  _ __/ ___|| |_ __ _| |_ ___
//   / _ \ | '_ \| '_ \___ \| __/ _` | __/ _ \
//  / ___ \| |_) | |_) |__) | || (_| | ||  __/
// /_/   \_\ .__/| .__/____/ \__\__,_|\__\___|
//         |_|   |_|

[<NoComparison;NoEquality>]
type AppState =
  { Clients:     IrisNode       list
  ; Sessions:    BrowserSession list
  ; Projects:    Map<ProjectId, Project>
  ; Peers:       Map<MemberId, string>
  ; Context:     ZeroMQ.ZContext
  ; Raft:        Raft
  ; Options:     Config
  }

  with
    override self.ToString() =
      sprintf "Clients: %A\nSessions: %A\nProjects: %A\nPeers: %A\nRaft: %A"
        self.Clients
        self.Sessions
        self.Projects
        self.Peers
        self.Raft

[<AutoOpen>]
module AppStateUtils =

  /// ## Add Project to AppState
  ///
  /// Unsafely adds a `Project` to an `AppState`, meaning no checks are performed to indicate
  /// whether the project already existed in the Map or not.
  ///
  /// ### Signature:
  /// - `project`: Project to add
  /// - `state`: AppState to add project to
  ///
  /// Returns: AppState
  let internal appendProject (project: Project) (state: AppState) : AppState =
    { state with Projects = Map.add project.Id project state.Projects }


  /// ## Add a project to AppState
  ///
  /// Add a `Project` to current `AppState` value. If the `Project` is already added indicate
  /// failure by returning `None`.
  ///
  /// ### Signature:
  /// - `project`: Project to add
  /// - `state`: AppState to add project to
  ///
  /// Returns: AppState option
  let addProject (project: Project) (state: AppState) : AppState option =
    if Map.containsKey project.Id state.Projects |> not
    then appendProject project state |> Some
    else None

  /// ## Update a project loaded into AppState
  ///
  /// Update an existing `Project` in given `AppState`. Indicate failure to find existing entry by
  /// returning `None`.
  ///
  /// ### Signature:
  /// - `project`: Project to add
  /// - `state`: AppState to add project to
  ///
  /// Returns: AppState option
  let updateProject (project: Project) (state: AppState) =
    if Map.containsKey project.Id state.Projects
    then appendProject project state |> Some
    else None

  /// ## Find a project loaded into AppState
  ///
  /// Find a given `ProjectId` in current `AppState`. Indicate failure to do so by returning `None`.
  ///
  /// ### Signature:
  /// - `id`; ProjectId of Project
  /// - `state`: AppState to find Project in
  ///
  /// Returns: Project option
  let findProject (id: ProjectId) state : Project option =
    match Map.tryFind id state.Projects with
      | Some _ as project -> project
      |      _            -> None

  /// ## Load a Project into given AppState
  ///
  /// Attempt to load a `Project` into `AppState`. Indicate failure to do so by returning `None`.
  ///
  /// ### Signature:
  /// - `path`: FilePath to project yaml
  /// - `state`: AppState to load Project into
  ///
  /// Returns: AppState option
  let loadProject (path: FilePath) (state: AppState) : AppState option =
    match load path with
      | Some project -> addProject project state
      | _            -> None

  /// ## Save a loaded project to disk
  ///
  /// Save a project loaded into AppState to disk. Indicate failure to find or save the Project by
  /// returning `None`
  ///
  /// ### Signature:
  /// - `id`: Project Id
  /// - `committer`: the signature (name, email) of the person invoking the operation
  /// - `msg`: the commit message associated with the operation
  /// - `state`: the current `AppState`
  ///
  /// Returns: (Commit * AppState) option
  let saveProject (id : ProjectId) (committer : Signature) msg state : (Commit * AppState) option =
    match findProject id state with
      | Some project ->
        try
          match save committer msg project with
          | Some (commit, project) ->
            (commit, { state with Projects = Map.add id project state.Projects })
            |> Some
          | _ ->
            None
        with
          | exn -> None
      | _ -> None

  /// ## Create a new Project
  ///
  /// Create a new `Project`, save it to disk and load it into given AppState.
  ///
  /// ### Signature:
  /// - name: Project name
  /// - path: destination path for new Project
  /// - committer: signature of the person creating the project
  /// - state: current AppState
  ///
  /// Returns: (Project * AppState) option
  let createProject name path (committer: Signature) state : (Project * AppState) option =
    let now = System.DateTime.Now
    let msg = sprintf "On %s, %s created %s" (now.ToLongTimeString()) committer.Name name
    let project = { Project.Create name with Path = Some(path) }

    match addProject project state with
      | Some state ->
        match saveProject project.Id committer msg state with
          | Some (commit, state) -> Some (project, state)
          | _                    -> None
      | _ -> None

  /// ## Close/unload a loaded project
  ///
  /// Attempt to close a loaded project. If the project was not loaded, indicate failure by
  /// returning `None`.
  ///
  /// ### Signature:
  /// - id: ProjectId of Project to remove
  /// - state: current AppState
  ///
  /// Returns: AppState option
  let closeProject (id : ProjectId) (state : AppState) : AppState option =
    if Map.containsKey id state.Projects then
      { state with Projects = Map.remove id state.Projects } |> Some
    else None

  /// ## Check if a project is loaded
  ///
  /// Try to find a given `ProjectId` in the passed `AppState`.
  ///
  /// ### Signature:
  /// - id: ProjectId to search for
  /// - state: AppState to search project in
  ///
  /// Returns: boolean
  let projectLoaded (id : ProjectId) (state : AppState) : bool =
    findProject id state |> Option.isSome

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  /// ## Update Raft in AppState
  ///
  /// Update the Raft field of a given AppState
  ///
  /// ### Signature:
  /// - raft: new Raft value to add to AppState
  /// - state: AppState to update
  ///
  /// Returns: AppState
  let updateRaft (raft: Raft) (state: AppState) : AppState =
    { state with Raft = raft }

  //  _   _           _
  // | \ | | ___   __| | ___  ___
  // |  \| |/ _ \ / _` |/ _ \/ __|
  // | |\  | (_) | (_| |  __/\__ \
  // |_| \_|\___/ \__,_|\___||___/

  /// ## Add a Node to current AppState
  ///
  /// Add a new node to current `Raft` and `AppState`.
  ///
  /// ### Signature:
  /// - node: the Node to add
  /// - state: the AppState to add it to
  ///
  /// Returns: AppState
  let addNode (node: IrisNode) (state: AppState) : AppState =
    failwith "FIXME: implement addNode AppState"

  let updateMember (newmem: IrisNode) (state: AppState) : AppState =
    failwith "FIXME: implement updateMember AppState"

  let removeMember (mem: IrisNode) (state: AppState) : AppState =
    failwith "FIXME: implement removeNode AppState"
