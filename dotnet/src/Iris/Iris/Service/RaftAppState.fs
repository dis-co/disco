namespace Iris.Service

open LibGit2Sharp
open Iris.Core.Utils
open Iris.Core
open Iris.Raft

//  ____        __ _      _               ____  _        _
// |  _ \ __ _ / _| |_   / \   _ __  _ __/ ___|| |_ __ _| |_ ___
// | |_) / _` | |_| __| / _ \ | '_ \| '_ \___ \| __/ _` | __/ _ \
// |  _ < (_| |  _| |_ / ___ \| |_) | |_) |__) | || (_| | ||  __/
// |_| \_\__,_|_|  \__|_/   \_\ .__/| .__/____/ \__\__,_|\__\___|
//                            |_|   |_|
[<AutoOpen>]
module RaftAppState =

  [<NoComparison;NoEquality>]
  type RaftAppState =
    { Context: ZeroMQ.ZContext
    ; Raft:    Raft
    ; Options: IrisConfig }

  with
    override self.ToString() =
      sprintf "Raft: %A" self.Raft


  /// ## Update Raft in RaftAppState
  ///
  /// Update the Raft field of a given RaftAppState
  ///
  /// ### Signature:
  /// - raft: new Raft value to add to RaftAppState
  /// - state: RaftAppState to update
  ///
  /// Returns: RaftAppState
  let updateRaft (raft: Raft) (state: RaftAppState) : RaftAppState =
    { state with Raft = raft }

  (*
  /// ## Add Project to RaftAppState
  ///
  /// Unsafely adds a `Project` to an `RaftAppState`, meaning no checks are performed to indicate
  /// whether the project already existed in the Map or not.
  ///
  /// ### Signature:
  /// - `project`: Project to add
  /// - `state`: RaftAppState to add project to
  ///
  /// Returns: RaftAppState
  let internal appendProject (project: Project) (state: RaftAppState) : RaftAppState =
    { state with Projects = Map.add project.Id project state.Projects }


  /// ## Add a project to RaftAppState
  ///
  /// Add a `Project` to current `RaftAppState` value. If the `Project` is already added indicate
  /// failure by returning `None`.
  ///
  /// ### Signature:
  /// - `project`: Project to add
  /// - `state`: RaftAppState to add project to
  ///
  /// Returns: RaftAppState option
  let addProject (project: Project) (state: RaftAppState) : RaftAppState option =
    if Map.containsKey project.Id state.Projects |> not
    then appendProject project state |> Some
    else None

  /// ## Update a project loaded into RaftAppState
  ///
  /// Update an existing `Project` in given `RaftAppState`. Indicate failure to find existing entry by
  /// returning `None`.
  ///
  /// ### Signature:
  /// - `project`: Project to add
  /// - `state`: RaftAppState to add project to
  ///
  /// Returns: RaftAppState option
  let updateProject (project: Project) (state: RaftAppState) =
    if Map.containsKey project.Id state.Projects
    then appendProject project state |> Some
    else None

  /// ## Find a project loaded into RaftAppState
  ///
  /// Find a given `ProjectId` in current `RaftAppState`. Indicate failure to do so by returning `None`.
  ///
  /// ### Signature:
  /// - `id`; ProjectId of Project
  /// - `state`: RaftAppState to find Project in
  ///
  /// Returns: Project option
  let findProject (id: ProjectId) state : Project option =
    match Map.tryFind id state.Projects with
      | Some _ as project -> project
      |      _            -> None

  /// ## Load a Project into given RaftAppState
  ///
  /// Attempt to load a `Project` into `RaftAppState`. Indicate failure to do so by returning `None`.
  ///
  /// ### Signature:
  /// - `path`: FilePath to project yaml
  /// - `state`: RaftAppState to load Project into
  ///
  /// Returns: RaftAppState option
  let loadProject (path: FilePath) (state: RaftAppState) : RaftAppState option =
    match load path with
      | Some project -> addProject project state
      | _            -> None

  /// ## Save a loaded project to disk
  ///
  /// Save a project loaded into RaftAppState to disk. Indicate failure to find or save the Project by
  /// returning `None`
  ///
  /// ### Signature:
  /// - `id`: Project Id
  /// - `committer`: the signature (name, email) of the person invoking the operation
  /// - `msg`: the commit message associated with the operation
  /// - `state`: the current `RaftAppState`
  ///
  /// Returns: (Commit * RaftAppState) option
  let saveProject (id : ProjectId) (committer : Signature) msg state : (Commit * RaftAppState) option =
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
  /// Create a new `Project`, save it to disk and load it into given RaftAppState.
  ///
  /// ### Signature:
  /// - name: Project name
  /// - path: destination path for new Project
  /// - committer: signature of the person creating the project
  /// - state: current RaftAppState
  ///
  /// Returns: (Project * RaftAppState) option
  let createProject name path (committer: Signature) state : (Project * RaftAppState) option =
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
  /// - state: current RaftAppState
  ///
  /// Returns: RaftAppState option
  let closeProject (id : ProjectId) (state : RaftAppState) : RaftAppState option =
    if Map.containsKey id state.Projects then
      { state with Projects = Map.remove id state.Projects } |> Some
    else None

  /// ## Check if a project is loaded
  ///
  /// Try to find a given `ProjectId` in the passed `RaftAppState`.
  ///
  /// ### Signature:
  /// - id: ProjectId to search for
  /// - state: RaftAppState to search project in
  ///
  /// Returns: boolean
  let projectLoaded (id : ProjectId) (state : RaftAppState) : bool =
    findProject id state |> Option.isSome

  //  _   _           _
  // | \ | | ___   __| | ___  ___
  // |  \| |/ _ \ / _` |/ _ \/ __|
  // | |\  | (_) | (_| |  __/\__ \
  // |_| \_|\___/ \__,_|\___||___/

  /// ## Add a Node to current RaftAppState
  ///
  /// Add a new node to current `Raft` and `RaftAppState`.
  ///
  /// ### Signature:
  /// - node: the Node to add
  /// - state: the RaftAppState to add it to
  ///
  /// Returns: RaftAppState
  let addNode (node: string) (state: RaftAppState) : RaftAppState =
    failwith "FIXME: implement addNode RaftAppState"

  let updateMember (newmem: string) (state: RaftAppState) : RaftAppState =
    failwith "FIXME: implement updateMember RaftAppState"

  let removeMember (mem: string) (state: RaftAppState) : RaftAppState =
    failwith "FIXME: implement removeNode RaftAppState"

  *)
