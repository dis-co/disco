namespace Iris.Core

//  ___                            _
// |_ _|_ __ ___  _ __   ___  _ __| |_ ___
//  | || '_ ` _ \| '_ \ / _ \| '__| __/ __|
//  | || | | | | | |_) | (_) | |  | |_\__ \
// |___|_| |_| |_| .__/ \___/|_|   \__|___/
//               |_|

open Iris.Raft

#if JAVASCRIPT

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

#endif

//     _                 ____                                          _
//    / \   _ __  _ __  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| |
//   / _ \ | '_ \| '_ \| |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |
//  / ___ \| |_) | |_) | |__| (_) | | | | | | | | | | | (_| | | | | (_| |
// /_/   \_\ .__/| .__/ \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|
//         |_|   |_|

[<RequireQualifiedAccess>]
type AppCommand =
  | Undo
  | Redo
  | Reset

  // PROJECT
  | SaveProject
  // | OpenProject
  // | CreateProject
  // | CloseProject
  // | DeleteProject

  static member Parse (str: string) =
    match str with
    | "Undo"        -> Undo
    | "Redo"        -> Redo
    | "Reset"       -> Reset
    | "SaveProject" -> SaveProject
    | _             -> failwithf "AppCommand: parse error: %s" str

  static member TryParse (str: string) =
    try
      str |> AppCommand.Parse |> Some
    with
      | _ -> None

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: ActionTypeFB) =
#if JAVASCRIPT
    match fb with
    | x when x = ActionTypeFB.UndoFB        -> Some Undo
    | x when x = ActionTypeFB.RedoFB        -> Some Redo
    | x when x = ActionTypeFB.ResetFB       -> Some Reset
    | x when x = ActionTypeFB.SaveProjectFB -> Some SaveProject
    | _                                     -> None
#else
    match fb with
    | ActionTypeFB.UndoFB        -> Some Undo
    | ActionTypeFB.RedoFB        -> Some Redo
    | ActionTypeFB.ResetFB       -> Some Reset
    | ActionTypeFB.SaveProjectFB -> Some SaveProject
    | _                          -> None
#endif

  member self.ToOffset(_: FlatBufferBuilder) : ActionTypeFB =
    match self with
    | Undo        -> ActionTypeFB.UndoFB
    | Redo        -> ActionTypeFB.RedoFB
    | Reset       -> ActionTypeFB.ResetFB
    | SaveProject -> ActionTypeFB.SaveProjectFB

//  ____  _        _     __   __              _
// / ___|| |_ __ _| |_ __\ \ / /_ _ _ __ ___ | |
// \___ \| __/ _` | __/ _ \ V / _` | '_ ` _ \| |
//  ___) | || (_| | ||  __/| | (_| | | | | | | |
// |____/ \__\__,_|\__\___||_|\__,_|_| |_| |_|_|

type StateYaml(ps, ioboxes, cues, cuelists, nodes, sessions, users) as self =
  [<DefaultValue>] val mutable Patches  : PatchYaml array
  [<DefaultValue>] val mutable IOBoxes  : IOBoxYaml array
  [<DefaultValue>] val mutable Cues     : CueYaml array
  [<DefaultValue>] val mutable CueLists : CueListYaml array
  [<DefaultValue>] val mutable Nodes    : RaftNodeYaml array
  [<DefaultValue>] val mutable Sessions : SessionYaml array
  [<DefaultValue>] val mutable Users    : UserYaml array

  new () = new StateYaml(null, null, null, null, null, null, null)

  do
    self.Patches  <- ps
    self.IOBoxes  <- ioboxes
    self.Cues     <- cues
    self.CueLists <- cuelists
    self.Nodes    <- nodes
    self.Sessions <- sessions
    self.Users    <- users

//   ____  _        _
//  / ___|| |_ __ _| |_ ___
//  \___ \| __/ _` | __/ _ \
//   ___) | || (_| | ||  __/
//  |____/ \__\__,_|\__\___|

//  Record type containing all the actual data that gets passed around in our
//  application.
//

type State =
  { Patches  : Map<Id,Patch>
  ; IOBoxes  : Map<Id,IOBox>
  ; Cues     : Map<Id,Cue>
  ; CueLists : Map<Id,CueList>
  ; Nodes    : Map<Id,RaftNode>
  ; Sessions : Map<Id,Session>
  ; Users    : Map<Id,User>
  }

  static member Empty =
    { Patches  = Map.empty
    ; IOBoxes  = Map.empty
    ; Cues     = Map.empty
    ; Nodes    = Map.empty
    ; CueLists = Map.empty
    ; Users    = Map.empty
    ; Sessions = Map.empty }

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  member state.AddUser (user: User) =
    if Map.containsKey user.Id state.Users then
      state
    else
      let users = Map.add user.Id user state.Users
      { state with Users = users }

  member state.UpdateUser (user: User) =
    if Map.containsKey user.Id state.Users then
      let users = Map.add user.Id user state.Users
      { state with Users = users }
    else
      state

  member state.RemoveUser (user: User) =
    { state with Users = Map.filter (fun k _ -> (k <> user.Id)) state.Users }

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  member state.AddSession (session: Session) =
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        state.Sessions
      else
        Map.add session.Id session state.Sessions
    { state with Sessions = sessions }

  member state.UpdateSession (session: Session) =
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        Map.add session.Id session state.Sessions
      else
        state.Sessions
    { state with Sessions = sessions }

  member state.RemoveSession (session: Session) =
    { state with Sessions = Map.filter (fun k _ -> (k <> session.Id)) state.Sessions }

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  member state.AddPatch (patch : Patch) =
    if Map.containsKey patch.Id state.Patches then
      state
    else
      { state with Patches = Map.add patch.Id patch state.Patches }

  member state.UpdatePatch (patch : Patch) =
    if Map.containsKey patch.Id state.Patches then
      { state with Patches = Map.add patch.Id patch state.Patches }
    else
      state

  member state.RemovePatch (patch : Patch) =
    { state with Patches = Map.remove patch.Id state.Patches }

  //  ___ ___  ____
  // |_ _/ _ \| __ )  _____  __
  //  | | | | |  _ \ / _ \ \/ /
  //  | | |_| | |_) | (_) >  <
  // |___\___/|____/ \___/_/\_\

  member state.AddIOBox (iobox : IOBox) =
    if Map.containsKey iobox.Patch state.Patches then
      let update _ (patch: Patch) =
        if patch.Id = iobox.Patch then
          Patch.AddIOBox patch iobox
        else
          patch
      { state with Patches = Map.map update state.Patches }
    else
      state

  member state.UpdateIOBox (iobox : IOBox) =
    let mapper (_: Id) (patch : Patch) =
      if patch.Id = iobox.Patch then
        Patch.UpdateIOBox patch iobox
      else
        patch
    { state with Patches = Map.map mapper state.Patches }

  member state.RemoveIOBox (iobox : IOBox) =
    let updater _ (patch : Patch) =
      if iobox.Patch = patch.Id
      then Patch.RemoveIOBox patch iobox
      else patch
    { state with Patches = Map.map updater state.Patches }

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_ ___
  // | |  | | | |/ _ \ |   | / __| __/ __|
  // | |__| |_| |  __/ |___| \__ \ |_\__ \
  //  \____\__,_|\___|_____|_|___/\__|___/

  member state.AddCueList (cuelist : CueList) =
    if Map.containsKey cuelist.Id state.CueLists then
      state
    else
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }

  member state.UpdateCueList (cuelist : CueList) =
    if Map.containsKey cuelist.Id state.CueLists then
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }
    else
      state

  member state.RemoveCueList (cuelist : CueList) =
    { state with CueLists = Map.remove cuelist.Id state.CueLists }

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  member state.AddCue (cue : Cue) =
    if Map.containsKey cue.Id state.Cues then
      state
    else
      { state with Cues = Map.add cue.Id cue state.Cues }

  member state.UpdateCue (cue : Cue) =
    if Map.containsKey cue.Id state.Cues then
      { state with Cues = Map.add cue.Id cue state.Cues }
    else
      state

  member state.RemoveCue (cue : Cue) =
    { state with Cues = Map.remove cue.Id state.Cues }

  //  _   _           _
  // | \ | | ___   __| | ___
  // |  \| |/ _ \ / _` |/ _ \
  // | |\  | (_) | (_| |  __/
  // |_| \_|\___/ \__,_|\___|

  member state.AddNode (node: RaftNode) =
    if Map.containsKey node.Id state.Nodes then
      state
    else
      { state with Nodes = Map.add node.Id node state.Nodes }

  member state.UpdateNode (node: RaftNode) =
    if Map.containsKey node.Id state.Nodes then
      { state with Nodes = Map.add node.Id node state.Nodes }
    else
      state

  member state.RemoveNode (node: RaftNode) =
    { state with Nodes = Map.remove node.Id state.Nodes }

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    let inline encode m =
      m |> Map.toArray |> Array.map (snd >> Yaml.toYaml)

    let yaml = new StateYaml()
    yaml.Patches  <- encode self.Patches
    yaml.IOBoxes  <- encode self.IOBoxes
    yaml.Cues     <- encode self.Cues
    yaml.CueLists <- encode self.CueLists
    yaml.Nodes    <- encode self.Nodes
    yaml.Sessions <- encode self.Sessions
    yaml.Users    <- encode self.Users

    yaml

  member self.ToYaml (serializer: Serializer) =
    self |> Yaml.toYaml |> serializer.Serialize

  static member FromYamlObject (yml: StateYaml) =
    { Patches  = Yaml.arrayToMap yml.Patches
      IOBoxes  = Yaml.arrayToMap yml.IOBoxes
      Cues     = Yaml.arrayToMap yml.Cues
      CueLists = Yaml.arrayToMap yml.CueLists
      Nodes    = Yaml.arrayToMap yml.Nodes
      Sessions = Yaml.arrayToMap yml.Sessions
      Users    = Yaml.arrayToMap yml.Users
    } |> Some

  static member FromYaml (str: string) : State option =
    let serializer = new Serializer()
    serializer.Deserialize<StateYaml>(str)
    |> Yaml.fromYaml

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<StateFB> =
    let patches =
      Map.toArray self.Patches
      |> Array.map (snd >> Binary.toOffset builder)

    let patchesoffset = StateFB.CreatePatchesVector(builder, patches)

    let ioboxes =
      Map.toArray self.IOBoxes
      |> Array.map (snd >> Binary.toOffset builder)

    let ioboxesoffset = StateFB.CreateIOBoxesVector(builder, ioboxes)

    let cues =
      Map.toArray self.Cues
      |> Array.map (snd >> Binary.toOffset builder)

    let cuesoffset = StateFB.CreateCuesVector(builder, cues)

    let cuelists =
      Map.toArray self.CueLists
      |> Array.map (snd >> Binary.toOffset builder)

    let cuelistsoffset = StateFB.CreateCueListsVector(builder, cuelists)

    let nodes =
      Map.toArray self.Nodes
      |> Array.map (snd >> Binary.toOffset builder)

    let nodesoffset = StateFB.CreateNodesVector(builder, nodes)

    let users =
      Map.toArray self.Users
      |> Array.map (snd >> Binary.toOffset builder)

    let usersoffset = StateFB.CreateUsersVector(builder, users)

    let sessions =
      Map.toArray self.Sessions
      |> Array.map (snd >> Binary.toOffset builder)

    let sessionsoffset = StateFB.CreateSessionsVector(builder, sessions)

    StateFB.StartStateFB(builder)
    StateFB.AddPatches(builder, patchesoffset)
    StateFB.AddIOBoxes(builder, ioboxesoffset)
    StateFB.AddCues(builder, cuesoffset)
    StateFB.AddCueLists(builder, cuelistsoffset)
    StateFB.AddNodes(builder, nodesoffset)
    StateFB.AddSessions(builder, sessionsoffset)
    StateFB.AddUsers(builder, usersoffset)
    StateFB.EndStateFB(builder)

  member self.ToBytes() = Binary.buildBuffer self

  static member FromFB(fb: StateFB) : State option =
    let mutable patches  = Map.empty
    let mutable ioboxes  = Map.empty
    let mutable cues     = Map.empty
    let mutable cuelists = Map.empty
    let mutable nodes    = Map.empty
    let mutable users    = Map.empty
    let mutable sessions = Map.empty

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.PatchesLength do
      fb.Patches(i)
      |> Patch.FromFB
      |> Option.map (fun patch -> patches <- Map.add patch.Id patch patches)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.PatchesLength - 1) do
      let patch = fb.Patches(i)
      if patch.HasValue then
        patch.Value
        |> Patch.FromFB
        |> Option.map (fun patch -> patches <- Map.add patch.Id patch patches)
        |> ignore
#endif

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.IOBoxesLength do
      fb.IOBoxes(i)
      |> IOBox.FromFB
      |> Option.map (fun iobox -> ioboxes <- Map.add iobox.Id iobox ioboxes)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.IOBoxesLength - 1) do
      let iobox = fb.IOBoxes(i)
      if iobox.HasValue then
        iobox.Value
        |> IOBox.FromFB
        |> Option.map (fun iobox -> ioboxes <- Map.add iobox.Id iobox ioboxes)
        |> ignore
#endif

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.CuesLength do
      fb.Cues(i)
      |> Cue.FromFB
      |> Option.map (fun cue -> cues <- Map.add cue.Id cue cues)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.CuesLength - 1) do
      let cue = fb.Cues(i)
      if cue.HasValue then
        cue.Value
        |> Cue.FromFB
        |> Option.map (fun cue -> cues <- Map.add cue.Id cue cues)
        |> ignore
#endif

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.CueListsLength do
      fb.CueLists(i)
      |> CueList.FromFB
      |> Option.map
        (fun cuelist ->
          cuelists <- Map.add cuelist.Id cuelist cuelists)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.CueListsLength - 1) do
      let cuelist = fb.CueLists(i)
      if cuelist.HasValue then
        cuelist.Value
        |> CueList.FromFB
        |> Option.map
          (fun cuelist ->
            cuelists <- Map.add cuelist.Id cuelist cuelists)
        |> ignore
#endif

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.NodesLength do
      fb.Nodes(i)
      |> RaftNode.FromFB
      |> Option.map (fun node -> nodes <- Map.add node.Id node nodes)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.NodesLength - 1) do
      let node = fb.Nodes(i)
      if node.HasValue then
        node.Value
        |> RaftNode.FromFB
        |> Option.map (fun node -> nodes <- Map.add node.Id node nodes)
        |> ignore
#endif

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.UsersLength do
      fb.Users(i)
      |> User.FromFB
      |> Option.map (fun user -> users <- Map.add user.Id user users)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.UsersLength - 1) do
      let user = fb.Users(i)
      if user.HasValue then
        user.Value
        |> User.FromFB
        |> Option.map (fun user -> users <- Map.add user.Id user users)
        |> ignore
#endif

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SessionsLength do
      fb.Sessions(i)
      |> Session.FromFB
      |> Option.map
        (fun session ->
          sessions <- Map.add session.Id session sessions)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SessionsLength - 1) do
      let session = fb.Sessions(i)
      if session.HasValue then
        session.Value
        |> Session.FromFB
        |> Option.map
          (fun session ->
            sessions <- Map.add session.Id session sessions)
        |> ignore
#endif

    Some { Patches  = patches
         ; IOBoxes  = ioboxes
         ; Cues     = cues
         ; CueLists = cuelists
         ; Nodes    = nodes
         ; Users    = users
         ; Sessions = sessions }

  static member FromBytes (bytes: Binary.Buffer) : State option =
    Binary.createBuffer bytes
    |> StateFB.GetRootAsStateFB
    |> State.FromFB

//  ____  _
// / ___|| |_ ___  _ __ ___
// \___ \| __/ _ \| '__/ _ \
//  ___) | || (_) | | |  __/
// |____/ \__\___/|_|  \___|
//

(* Action: Log entry for the Event occurred and the resulting state. *)
and StoreAction =
  { Event: StateMachine
  ; State: State }

  override self.ToString() : string =
    sprintf "%s %s" (self.Event.ToString()) (self.State.ToString())

//  _   _ _     _
// | | | (_)___| |_ ___  _ __ _   _
// | |_| | / __| __/ _ \| '__| | | |
// |  _  | \__ \ || (_) | |  | |_| |
// |_| |_|_|___/\__\___/|_|   \__, |
//                            |___/
//

and History (action: StoreAction) =
  let mutable depth = 10
  let mutable debug = false
  let mutable head = 1
  let mutable values = [ action ]

  member self.Debug
    with get () = debug
    and  set b  =
      debug <- b
      if not debug then
        values <- List.take depth values

  member self.Depth
    with get () = depth
      and set n  = depth <- n

  member self.Values
    with get () = values

  member self.Length
    with get () = List.length values

  (* - - - - - - - - - - Methods - - - - - - - - - - *)
  member self.Append (value: StoreAction) : unit =
    head <- 0
    let newvalues = value :: values
    if (not debug) && List.length newvalues > depth then
      values <- List.take depth newvalues
    else
      values <- newvalues

  member self.Undo () : StoreAction option =
    let head' =
      if (head - 1) > (List.length values) then
        List.length values
      else
        head + 1

    if head <> head' then
      head <- head'

    List.tryItem head values

  member self.Redo () : StoreAction option =
    let head' =
      if   head - 1 < 0
      then 0
      else head - 1

    if head <> head' then
      head <- head'

    List.tryItem head values

//  ____  _
// / ___|| |_ ___  _ __ ___
// \___ \| __/ _ \| '__/ _ \
//  ___) | || (_) | | |  __/
// |____/ \__\___/|_|  \___|
//
// The store centrally manages all state changes and notifies interested
// parties of changes to the carried state (e.g. views, socket transport).
//
// Features:
//
// - time-traveleing debugger
// - undo/redo
//

and Store(state : State)=

  let mutable state = state

  let mutable history = new History {
      State = state;
      Event = Command(AppCommand.Reset);
    }

  let mutable listeners : Listener list = []

  // Notify all listeners of the StateMachine change
  member private store.Notify (ev : StateMachine) =
    List.iter (fun f -> f store ev) listeners

  // Turn debugging mode on or off.
  member self.Debug
    with get ()  = history.Debug
      and set dbg = history.Debug <- dbg

  (*
    * Number of undo steps to keep around.
    *
    * Overridden in debug mode.
    *)
  member self.UndoSteps
    with get () = history.Depth
      and set n  = history.Depth <- n

  (*
      Dispatch an action (StateMachine) to be executed against the current
      version of the state to produce the next state.

      Notify all listeners of the change.

      Create a history item for this change if debugging is enabled.
    *)
  member self.Dispatch (ev : StateMachine) : unit =
    let andRender (newstate: State) =
      state <- newstate                   // 1) create new state
      self.Notify(ev)                    // 2) notify all listeners
      history.Append({ Event = ev        // 3) store this action and new state
                      ; State = state }) // 4) append to undo history

    let addSession (session: Session) (state: State) =
      let sessions =
        if Map.containsKey session.Id state.Sessions then
          state.Sessions
        else
          Map.add session.Id session state.Sessions
      { state with Sessions = sessions }

    match ev with
    | Command (AppCommand.Redo)  -> self.Redo()
    | Command (AppCommand.Undo)  -> self.Undo()
    | Command (AppCommand.Reset) -> ()   // do nothing for now

    | AddCue                cue -> state.AddCue        cue     |> andRender
    | UpdateCue             cue -> state.UpdateCue     cue     |> andRender
    | RemoveCue             cue -> state.RemoveCue     cue     |> andRender

    | AddCueList        cuelist -> state.AddCueList    cuelist |> andRender
    | UpdateCueList     cuelist -> state.UpdateCueList cuelist |> andRender
    | RemoveCueList     cuelist -> state.RemoveCueList cuelist |> andRender

    | AddPatch            patch -> state.AddPatch      patch   |> andRender
    | UpdatePatch         patch -> state.UpdatePatch   patch   |> andRender
    | RemovePatch         patch -> state.RemovePatch   patch   |> andRender

    | AddIOBox            iobox -> state.AddIOBox      iobox   |> andRender
    | UpdateIOBox         iobox -> state.UpdateIOBox   iobox   |> andRender
    | RemoveIOBox         iobox -> state.RemoveIOBox   iobox   |> andRender

    | AddNode              node -> state.AddNode       node    |> andRender
    | UpdateNode           node -> state.UpdateNode    node    |> andRender
    | RemoveNode           node -> state.RemoveNode    node    |> andRender

    | AddSession        session -> addSession session state    |> andRender
    | UpdateSession     session -> state.UpdateSession session |> andRender
    | RemoveSession     session -> state.RemoveSession session |> andRender

    | AddUser              user -> state.AddUser       user    |> andRender
    | UpdateUser           user -> state.UpdateUser    user    |> andRender
    | RemoveUser           user -> state.RemoveUser    user    |> andRender

    | _ -> ()

  (*
      Subscribe a callback to changes on the store.
    *)
  member self.Subscribe (listener : Listener) =
    listeners <- listener :: listeners

  (*
      Get the current version of the Store
    *)
  member self.State with get () = state

  member self.History with get () = history

  member self.Redo() =
    match history.Redo() with
      | Some log ->
        state <- log.State
        self.Notify log.Event |> ignore
      | _ -> ()

  member self.Undo() =
    match history.Undo() with
      | Some log ->
        state <- log.State
        self.Notify log.Event |> ignore
      | _ -> ()

//  _     _     _
// | |   (_)___| |_ ___ _ __   ___ _ __
// | |   | / __| __/ _ \ '_ \ / _ \ '__|
// | |___| \__ \ ||  __/ | | |  __/ |
// |_____|_|___/\__\___|_| |_|\___|_|
//

and Listener = Store -> StateMachine -> unit

// __   __              _    ___  _     _           _
// \ \ / /_ _ _ __ ___ | |  / _ \| |__ (_) ___  ___| |_
//  \ V / _` | '_ ` _ \| | | | | | '_ \| |/ _ \/ __| __|
//   | | (_| | | | | | | | | |_| | |_) | |  __/ (__| |_
//   |_|\__,_|_| |_| |_|_|  \___/|_.__// |\___|\___|\__|
//                                   |__/

and StateMachineYaml(cmd: string, payload: obj) as self =
  [<DefaultValue>] val mutable Action : string
  [<DefaultValue>] val mutable Payload : obj

  new () = new StateMachineYaml(null, null)

  do
    self.Action  <- cmd
    self.Payload <- payload

  static member AddNode (node: RaftNode) =
    new StateMachineYaml("AddNode", Yaml.toYaml node)

  static member UpdateNode (node: RaftNode) =
    new StateMachineYaml("UpdateNode", Yaml.toYaml node)

  static member RemoveNode (node: RaftNode) =
    new StateMachineYaml("RemoveNode", Yaml.toYaml node)

  static member AddPatch (patch: Patch) =
    new StateMachineYaml("AddPatch", Yaml.toYaml patch)

  static member UpdatePatch (patch: Patch) =
    new StateMachineYaml("UpdatePatch", Yaml.toYaml patch)

  static member RemovePatch (patch: Patch) =
    new StateMachineYaml("RemovePatch", Yaml.toYaml patch)

  static member AddIOBox (iobox: IOBox) =
    new StateMachineYaml("AddIOBox", Yaml.toYaml iobox)

  static member UpdateIOBox (iobox: IOBox) =
    new StateMachineYaml("UpdateIOBox", Yaml.toYaml iobox)

  static member RemoveIOBox (iobox: IOBox) =
    new StateMachineYaml("RemoveIOBox", Yaml.toYaml iobox)

  static member AddCue (cue: Cue) =
    new StateMachineYaml("AddCue", Yaml.toYaml cue)

  static member UpdateCue (cue: Cue) =
    new StateMachineYaml("UpdateCue", Yaml.toYaml cue)

  static member RemoveCue (cue: Cue) =
    new StateMachineYaml("RemoveCue", Yaml.toYaml cue)

  static member AddCueList (cuelist: CueList) =
    new StateMachineYaml("AddCueList", Yaml.toYaml cuelist)

  static member UpdateCueList (cuelist: CueList) =
    new StateMachineYaml("UpdateCueList", Yaml.toYaml cuelist)

  static member RemoveCueList (cuelist: CueList) =
    new StateMachineYaml("RemoveCueList", Yaml.toYaml cuelist)

  static member AddUser (user: User) =
    new StateMachineYaml("AddUser", Yaml.toYaml user)

  static member UpdateUser (user: User) =
    new StateMachineYaml("UpdateUser", Yaml.toYaml user)

  static member RemoveUser (user: User) =
    new StateMachineYaml("RemoveUser", Yaml.toYaml user)

  static member AddSession (session: Session) =
    new StateMachineYaml("AddSession", Yaml.toYaml session)

  static member UpdateSession (session: Session) =
    new StateMachineYaml("UpdateSession", Yaml.toYaml session)

  static member RemoveSession (session: Session) =
    new StateMachineYaml("RemoveSession", Yaml.toYaml session)

  static member Command (cmd: AppCommand) =
    new StateMachineYaml("Command", string cmd)

  static member LogMsg (loglevel, str) =
    new StateMachineYaml("LogMsg", sprintf "%A %s" loglevel str)

  static member DataSnapshot (state: State) =
    new StateMachineYaml("DataSnapshot", Yaml.toYaml state)

//  ____  _        _       __  __            _     _
// / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
// \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
// |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

and StateMachine =

  // NODE
  | AddNode       of RaftNode
  | UpdateNode    of RaftNode
  | RemoveNode    of RaftNode

  // PATCH
  | AddPatch      of Patch
  | UpdatePatch   of Patch
  | RemovePatch   of Patch

  // IOBOX
  | AddIOBox      of IOBox
  | UpdateIOBox   of IOBox
  | RemoveIOBox   of IOBox

  // CUE
  | AddCue        of Cue
  | UpdateCue     of Cue
  | RemoveCue     of Cue

  // CUE
  | AddCueList    of CueList
  | UpdateCueList of CueList
  | RemoveCueList of CueList

  // User
  | AddUser       of User
  | UpdateUser    of User
  | RemoveUser    of User

  // Session
  | AddSession    of Session
  | UpdateSession of Session
  | RemoveSession of Session

  | Command       of AppCommand

  | DataSnapshot  of State

  | LogMsg        of LogLevel * string

  override self.ToString() : string =
    match self with

    // NODE
    | AddNode    node       -> sprintf "AddNode %s"    (string node)
    | UpdateNode node       -> sprintf "UpdateNode %s" (string node)
    | RemoveNode node       -> sprintf "RemoveNode %s" (string node)

    // PATCH
    | AddPatch    patch     -> sprintf "AddPatch %s"    (string patch)
    | UpdatePatch patch     -> sprintf "UpdatePatch %s" (string patch)
    | RemovePatch patch     -> sprintf "RemovePatch %s" (string patch)

    // IOBOX
    | AddIOBox    iobox     -> sprintf "AddIOBox %s"    (string iobox)
    | UpdateIOBox iobox     -> sprintf "UpdateIOBox %s" (string iobox)
    | RemoveIOBox iobox     -> sprintf "RemoveIOBox %s" (string iobox)

    // CUE
    | AddCue    cue         -> sprintf "AddCue %s"    (string cue)
    | UpdateCue cue         -> sprintf "UpdateCue %s" (string cue)
    | RemoveCue cue         -> sprintf "RemoveCue %s" (string cue)

    // CUELIST
    | AddCueList    cuelist -> sprintf "AddCueList %s"    (string cuelist)
    | UpdateCueList cuelist -> sprintf "UpdateCueList %s" (string cuelist)
    | RemoveCueList cuelist -> sprintf "RemoveCueList %s" (string cuelist)

    // User
    | AddUser    user       -> sprintf "AddUser %s"    (string user)
    | UpdateUser user       -> sprintf "UpdateUser %s" (string user)
    | RemoveUser user       -> sprintf "RemoveUser %s" (string user)

    // Session
    | AddSession    session -> sprintf "AddSession %s"    (string session)
    | UpdateSession session -> sprintf "UpdateSession %s" (string session)
    | RemoveSession session -> sprintf "RemoveSession %s" (string session)

    | Command    ev         -> sprintf "Command: %s"  (string ev)
    | DataSnapshot state    -> sprintf "DataSnapshot: %A" state
    | LogMsg(level, msg)    -> sprintf "LogMsg: [%A] %s" level msg

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else

  member self.ToYamlObject() : StateMachineYaml =
    match self with
    | AddNode    node       -> StateMachineYaml.AddNode(node)
    | UpdateNode node       -> StateMachineYaml.UpdateNode(node)
    | RemoveNode node       -> StateMachineYaml.RemoveNode(node)

    // PATCH
    | AddPatch    patch     -> StateMachineYaml.AddPatch(patch)
    | UpdatePatch patch     -> StateMachineYaml.UpdatePatch(patch)
    | RemovePatch patch     -> StateMachineYaml.RemovePatch(patch)

    // IOBOX
    | AddIOBox    iobox     -> StateMachineYaml.AddIOBox(iobox)
    | UpdateIOBox iobox     -> StateMachineYaml.UpdateIOBox(iobox)
    | RemoveIOBox iobox     -> StateMachineYaml.RemoveIOBox(iobox)

    // CUE
    | AddCue    cue         -> StateMachineYaml.AddCue(cue)
    | UpdateCue cue         -> StateMachineYaml.UpdateCue(cue)
    | RemoveCue cue         -> StateMachineYaml.RemoveCue(cue)

    // CUELIST
    | AddCueList    cuelist -> StateMachineYaml.AddCueList(cuelist)
    | UpdateCueList cuelist -> StateMachineYaml.UpdateCueList(cuelist)
    | RemoveCueList cuelist -> StateMachineYaml.RemoveCueList(cuelist)

    // User
    | AddUser    user       -> StateMachineYaml.AddUser(user)
    | UpdateUser user       -> StateMachineYaml.UpdateUser(user)
    | RemoveUser user       -> StateMachineYaml.RemoveUser(user)

    // Session
    | AddSession    session -> StateMachineYaml.AddSession(session)
    | UpdateSession session -> StateMachineYaml.UpdateSession(session)
    | RemoveSession session -> StateMachineYaml.RemoveSession(session)

    | Command         ev    -> StateMachineYaml.Command(ev)
    | DataSnapshot state    -> StateMachineYaml.DataSnapshot(state)
    | LogMsg(level, msg)    -> StateMachineYaml.LogMsg(level,msg)

  member self.ToYaml (serializer: Serializer) =
    self |> Yaml.toYaml |> serializer.Serialize

  static member FromYamlObject (yaml: StateMachineYaml) =
    match yaml.Action with
    | "AddNode" -> maybe {
        let! node = yaml.Payload :?> RaftNodeYaml |> Yaml.fromYaml
        return AddNode(node)
      }
    | "UpdateNode" -> maybe {
        let! node = yaml.Payload :?> RaftNodeYaml |> Yaml.fromYaml
        return UpdateNode(node)
      }
    | "RemoveNode" -> maybe {
        let! node = yaml.Payload :?> RaftNodeYaml |> Yaml.fromYaml
        return RemoveNode(node)
      }
    | "AddPatch" -> maybe {
        let! patch = yaml.Payload :?> PatchYaml |> Yaml.fromYaml
        return AddPatch(patch)
      }
    | "UpdatePatch" -> maybe {
        let! patch = yaml.Payload :?> PatchYaml |> Yaml.fromYaml
        return UpdatePatch(patch)
      }
    | "RemovePatch" -> maybe {
        let! patch = yaml.Payload :?> PatchYaml |> Yaml.fromYaml
        return RemovePatch(patch)
      }
    | "AddIOBox" -> maybe {
        let! iobox = yaml.Payload :?> IOBoxYaml |> Yaml.fromYaml
        return AddIOBox(iobox)
      }
    | "UpdateIOBox" -> maybe {
        let! iobox = yaml.Payload :?> IOBoxYaml |> Yaml.fromYaml
        return UpdateIOBox(iobox)
      }
    | "RemoveIOBox" -> maybe {
        let! iobox = yaml.Payload :?> IOBoxYaml |> Yaml.fromYaml
        return RemoveIOBox(iobox)
      }
    | "AddCue" -> maybe {
        let! cue = yaml.Payload :?> CueYaml |> Yaml.fromYaml
        return AddCue(cue)
      }
    | "UpdateCue" -> maybe {
        let! cue = yaml.Payload :?> CueYaml |> Yaml.fromYaml
        return UpdateCue(cue)
      }
    | "RemoveCue" -> maybe {
        let! cue = yaml.Payload :?> CueYaml |> Yaml.fromYaml
        return RemoveCue(cue)
      }
    | "AddCueList" -> maybe {
        let! cuelist = yaml.Payload :?> CueListYaml |> Yaml.fromYaml
        return AddCueList(cuelist)
      }
    | "UpdateCueList" -> maybe {
        let! cuelist = yaml.Payload :?> CueListYaml |> Yaml.fromYaml
        return UpdateCueList(cuelist)
      }
    | "RemoveCueList" -> maybe {
        let! cuelist = yaml.Payload :?> CueListYaml |> Yaml.fromYaml
        return RemoveCueList(cuelist)
      }
    | "AddUser" -> maybe {
        let! user = yaml.Payload :?> UserYaml |> Yaml.fromYaml
        return AddUser(user)
      }
    | "UpdateUser" -> maybe {
        let! user = yaml.Payload :?> UserYaml |> Yaml.fromYaml
        return UpdateUser(user)
      }
    | "RemoveUser" -> maybe {
        let! user = yaml.Payload :?> UserYaml |> Yaml.fromYaml
        return RemoveUser(user)
      }
    | "AddSession" -> maybe {
        let! session = yaml.Payload :?> SessionYaml |> Yaml.fromYaml
        return AddSession(session)
      }
    | "UpdateSession" -> maybe {
        let! session = yaml.Payload :?> SessionYaml |> Yaml.fromYaml
        return UpdateSession(session)
      }
    | "RemoveSession" -> maybe {
        let! session = yaml.Payload :?> SessionYaml |> Yaml.fromYaml
        return RemoveSession(session)
      }
    | "Command" -> maybe {
        let! cmd = yaml.Payload :?> string |> AppCommand.TryParse
        return Command(cmd)
      }
    | "DataSnapshot" -> maybe {
        let! data = yaml.Payload :?> StateYaml |> Yaml.fromYaml
        return DataSnapshot(data)
      }
    | "LogMsg" -> maybe {
        let payload = yaml.Payload :?> string
        let! levelstr, str = match split [| ';' |] payload with
                             | [| level; str |] -> Some (level, str)
                             | _              -> None
        let! loglevel = LogLevel.TryParse levelstr
        return LogMsg(loglevel, str)
      }
    | _ -> None

  static member FromYaml (str: string) : StateMachine option =
    let serializer = new Serializer()
    serializer.Deserialize<StateMachineYaml>(str)
    |> Yaml.fromYaml

#endif

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

#if JAVASCRIPT
  static member FromFB (fb: ApiActionFB) =
    match fb.PayloadType with
    | x when x = PayloadFB.NodeFB ->
      let node = fb.NodeFB |> RaftNode.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Option.map AddNode node
      | x when x = ActionTypeFB.UpdateFB ->
        Option.map UpdateNode node
      | x when x = ActionTypeFB.RemoveFB ->
        Option.map RemoveNode node
      | _ -> None

    | x when x = PayloadFB.PatchFB ->
      let patch = fb.PatchFB |> Patch.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Option.map AddPatch patch
      | x when x = ActionTypeFB.UpdateFB ->
        Option.map UpdatePatch patch
      | x when x = ActionTypeFB.RemoveFB ->
        Option.map RemovePatch patch
      | _ -> None

    | x when x = PayloadFB.IOBoxFB ->
      let iobox = fb.IOBoxFB |> IOBox.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Option.map AddIOBox iobox
      | x when x = ActionTypeFB.UpdateFB ->
        Option.map UpdateIOBox iobox
      | x when x = ActionTypeFB.RemoveFB ->
        Option.map RemoveIOBox iobox
      | _ -> None

    | x when x = PayloadFB.CueFB ->
      let cue = fb.CueFB |> Cue.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Option.map AddCue cue
      | x when x = ActionTypeFB.UpdateFB ->
        Option.map UpdateCue cue
      | x when x = ActionTypeFB.RemoveFB ->
        Option.map RemoveCue cue
      | _ -> None

    | x when x = PayloadFB.CueListFB ->
      let cuelist = fb.CueListFB |> CueList.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Option.map AddCueList cuelist
      | x when x = ActionTypeFB.UpdateFB ->
        Option.map UpdateCueList cuelist
      | x when x = ActionTypeFB.RemoveFB ->
        Option.map RemoveCueList cuelist
      | _ -> None

    | x when x = PayloadFB.UserFB ->
      let user = fb.UserFB |> User.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Option.map AddUser user
      | x when x = ActionTypeFB.UpdateFB ->
        Option.map UpdateUser user
      | x when x = ActionTypeFB.RemoveFB ->
        Option.map RemoveUser user
      | _ -> None

    | x when x = PayloadFB.SessionFB ->
      let session = fb.SessionFB |> Session.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Option.map AddSession session
      | x when x = ActionTypeFB.UpdateFB ->
        Option.map UpdateSession session
      | x when x = ActionTypeFB.RemoveFB ->
        Option.map RemoveSession session
      | _ -> None

    | x when x = PayloadFB.StateFB && fb.Action = ActionTypeFB.DataSnapshotFB ->
      fb.StateFB
      |> State.FromFB
      |> Option.map DataSnapshot

    | x when x = PayloadFB.LogMsgFB ->
      let msg = fb.LogMsgFB
      msg.LogLevel
      |> LogLevel.Parse
      |> Option.map (fun level -> LogMsg(level, msg.Msg))

    | _ ->
      fb.Action
      |> AppCommand.FromFB
      |> Option.map Command

#else
  static member FromFB (fb: ApiActionFB) =
    match fb.PayloadType with
    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | PayloadFB.CueFB ->
      let cue =
        let cueish = fb.Payload<CueFB>()
        if cueish.HasValue then
          cueish.Value
          |> Cue.FromFB
        else None
      match fb.Action with
      | ActionTypeFB.AddFB    -> Option.map AddCue    cue
      | ActionTypeFB.UpdateFB -> Option.map UpdateCue cue
      | ActionTypeFB.RemoveFB -> Option.map RemoveCue cue
      | _                     -> None

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | PayloadFB.CueListFB ->
      let cuelist =
        let cuelistish = fb.Payload<CueListFB>()
        if cuelistish.HasValue then
          cuelistish.Value
          |> CueList.FromFB
        else None
      match fb.Action with
      | ActionTypeFB.AddFB    -> Option.map AddCueList    cuelist
      | ActionTypeFB.UpdateFB -> Option.map UpdateCueList cuelist
      | ActionTypeFB.RemoveFB -> Option.map RemoveCueList cuelist
      | _                     -> None

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | PayloadFB.PatchFB ->
      let patch =
        let patchish = fb.Payload<PatchFB>()
        if patchish.HasValue then
          patchish.Value
          |> Patch.FromFB
        else None
      match fb.Action with
      | ActionTypeFB.AddFB    -> Option.map AddPatch    patch
      | ActionTypeFB.UpdateFB -> Option.map UpdatePatch patch
      | ActionTypeFB.RemoveFB -> Option.map RemovePatch patch
      | _                     -> None

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    | PayloadFB.IOBoxFB ->
      let iobox =
        let ioboxish = fb.Payload<IOBoxFB>()
        if ioboxish.HasValue then
          ioboxish.Value
          |> IOBox.FromFB
        else None
      match fb.Action with
      | ActionTypeFB.AddFB    -> Option.map AddIOBox    iobox
      | ActionTypeFB.UpdateFB -> Option.map UpdateIOBox iobox
      | ActionTypeFB.RemoveFB -> Option.map RemoveIOBox iobox
      | _                     -> None

    //  _   _           _
    // | \ | | ___   __| | ___
    // |  \| |/ _ \ / _` |/ _ \
    // | |\  | (_) | (_| |  __/
    // |_| \_|\___/ \__,_|\___|

    | PayloadFB.NodeFB ->
      let node =
        let nodeish = fb.Payload<NodeFB>()
        if nodeish.HasValue then
          nodeish.Value
          |> RaftNode.FromFB
        else None
      match fb.Action with
      | ActionTypeFB.AddFB    -> Option.map AddNode    node
      | ActionTypeFB.UpdateFB -> Option.map UpdateNode node
      | ActionTypeFB.RemoveFB -> Option.map RemoveNode node
      | _                     -> None

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | PayloadFB.UserFB ->
      let user =
        let userish = fb.Payload<UserFB>()
        if userish.HasValue then
          userish.Value
          |> User.FromFB
        else None
      match fb.Action with
      | ActionTypeFB.AddFB    -> Option.map AddUser    user
      | ActionTypeFB.UpdateFB -> Option.map UpdateUser user
      | ActionTypeFB.RemoveFB -> Option.map RemoveUser user
      | _                     -> None

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | PayloadFB.SessionFB ->
      let session =
        let sessionish = fb.Payload<SessionFB>()
        if sessionish.HasValue then
          sessionish.Value
          |> Session.FromFB
        else None
      match fb.Action with
      | ActionTypeFB.AddFB    -> Option.map AddSession    session
      | ActionTypeFB.UpdateFB -> Option.map UpdateSession session
      | ActionTypeFB.RemoveFB -> Option.map RemoveSession session
      | _                     -> None

    //  __  __ _
    // |  \/  (_)___  ___
    // | |\/| | / __|/ __|
    // | |  | | \__ \ (__
    // |_|  |_|_|___/\___|

    | PayloadFB.LogMsgFB ->
      let logish = fb.Payload<LogMsgFB>()
      if logish.HasValue then
        let log = logish.Value
        log.LogLevel
        |> LogLevel.TryParse
        |> Option.map (fun level -> LogMsg(level, log.Msg))
      else None

    | PayloadFB.StateFB ->
      let stateish = fb.Payload<StateFB>()
      if stateish.HasValue then
        let state = stateish.Value
        state
        |> State.FromFB
        |> Option.map DataSnapshot
      else None

    | _ ->
      AppCommand.FromFB fb.Action |> Option.map Command

#endif

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<ApiActionFB> =
    match self with
    | AddNode       node ->
      let node = node.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.NodeFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, node)
#else
      ApiActionFB.AddPayload(builder, node.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdateNode    node ->
      let node = node.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.NodeFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, node)
#else
      ApiActionFB.AddPayload(builder, node.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemoveNode    node ->
      let node = node.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.NodeFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, node)
#else
      ApiActionFB.AddPayload(builder, node.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddPatch       patch ->
      let patch = patch.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.PatchFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, patch)
#else
      ApiActionFB.AddPayload(builder, patch.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdatePatch    patch ->
      let patch = patch.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.PatchFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, patch)
#else
      ApiActionFB.AddPayload(builder, patch.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemovePatch    patch ->
      let patch = patch.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.PatchFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, patch)
#else
      ApiActionFB.AddPayload(builder, patch.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddIOBox       iobox ->
      let iobox = iobox.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.IOBoxFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, iobox)
#else
      ApiActionFB.AddPayload(builder, iobox.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdateIOBox    iobox ->
      let iobox = iobox.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.IOBoxFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, iobox)
#else
      ApiActionFB.AddPayload(builder, iobox.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemoveIOBox    iobox ->
      let iobox = iobox.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.IOBoxFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, iobox)
#else
      ApiActionFB.AddPayload(builder, iobox.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddCue cue ->
      let cue = cue.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.CueFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, cue)
#else
      ApiActionFB.AddPayload(builder, cue.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdateCue cue ->
      let cue = cue.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.CueFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, cue)
#else
      ApiActionFB.AddPayload(builder, cue.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemoveCue cue ->
      let cue = cue.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.CueFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, cue)
#else
      ApiActionFB.AddPayload(builder, cue.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.CueListFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, cuelist)
#else
      ApiActionFB.AddPayload(builder, cuelist.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdateCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.CueListFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, cuelist)
#else
      ApiActionFB.AddPayload(builder, cuelist.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemoveCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.CueListFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, cuelist)
#else
      ApiActionFB.AddPayload(builder, cuelist.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddUser user ->
      let user = user.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.UserFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, user)
#else
      ApiActionFB.AddPayload(builder, user.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdateUser user ->
      let user = user.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.UserFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, user)
#else
      ApiActionFB.AddPayload(builder, user.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemoveUser user ->
      let user = user.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.UserFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, user)
#else
      ApiActionFB.AddPayload(builder, user.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddSession session ->
      let session = session.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.SessionFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, session)
#else
      ApiActionFB.AddPayload(builder, session.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdateSession session ->
      let session = session.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.SessionFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, session)
#else
      ApiActionFB.AddPayload(builder, session.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemoveSession session ->
      let session = session.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.SessionFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, session)
#else
      ApiActionFB.AddPayload(builder, session.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | Command appcommand ->
      let cmd = appcommand.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, cmd)
      ApiActionFB.EndApiActionFB(builder)

    | DataSnapshot state ->
      let data = state.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.DataSnapshotFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.StateFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, data)
#else
      ApiActionFB.AddPayload(builder, data.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | LogMsg (level, msg) ->
      let level = string level |> builder.CreateString
      let msg = msg |> builder.CreateString
      LogMsgFB.StartLogMsgFB(builder)
      LogMsgFB.AddLogLevel(builder, level)
      LogMsgFB.AddMsg(builder, msg)
      let offset = LogMsgFB.EndLogMsgFB(builder)

      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.LogMsgFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.LogMsgFB)
#if JAVASCRIPT
      ApiActionFB.AddPayload(builder, offset)
#else
      ApiActionFB.AddPayload(builder, offset.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)


  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes (bytes: Binary.Buffer) : StateMachine option =
    Binary.createBuffer bytes
    |> ApiActionFB.GetRootAsApiActionFB
    |> StateMachine.FromFB
