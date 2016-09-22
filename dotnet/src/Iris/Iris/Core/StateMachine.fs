namespace Iris.Core

// ********************************************************************************************** //
//  ___                            _
// |_ _|_ __ ___  _ __   ___  _ __| |_ ___
//  | || '_ ` _ \| '_ \ / _ \| '__| __/ __|
//  | || | | | | | |_) | (_) | |  | |_\__ \
// |___|_| |_| |_| .__/ \___/|_|   \__|___/
//               |_|
// ********************************************************************************************** //

open Iris.Raft

#if JAVASCRIPT

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Core.JsInterop

#else

open Iris.Serialization.Raft
open FlatBuffers
open Newtonsoft.Json
open Newtonsoft.Json.Linq

#endif

// ********************************************************************************************** //
//     _                 ____                                          _
//    / \   _ __  _ __  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| |
//   / _ \ | '_ \| '_ \| |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |
//  / ___ \| |_) | |_) | |__| (_) | | | | | | | | | | | (_| | | | | (_| |
// /_/   \_\ .__/| .__/ \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|
//         |_|   |_|
// ********************************************************************************************** //

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

#if JAVASCRIPT
#else

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: AppCommandFB) =
    match fb.Command with
    | AppCommandTypeFB.UndoFB        -> Some Undo
    | AppCommandTypeFB.RedoFB        -> Some Redo
    | AppCommandTypeFB.ResetFB       -> Some Reset
    | AppCommandTypeFB.SaveProjectFB -> Some SaveProject
    | _                              -> None

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<AppCommandFB> =
    let tipe =
      match self with
      | Undo        -> AppCommandTypeFB.UndoFB
      | Redo        -> AppCommandTypeFB.RedoFB
      | Reset       -> AppCommandTypeFB.ResetFB
      | SaveProject -> AppCommandTypeFB.SaveProjectFB

    AppCommandFB.StartAppCommandFB(builder)
    AppCommandFB.AddCommand(builder, tipe)
    AppCommandFB.EndAppCommandFB(builder)

#endif

// ********************************************************************************************** //
//   ____  _        _
//  / ___|| |_ __ _| |_ ___
//  \___ \| __/ _` | __/ _ \
//   ___) | || (_| | ||  __/
//  |____/ \__\__,_|\__\___|

//  Record type containing all the actual data that gets passed around in our
//  application.
//
// ********************************************************************************************** //

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

  //  ____            _       _ _          _   _
  // / ___|  ___ _ __(_) __ _| (_)______ _| |_(_) ___  _ __
  // \___ \ / _ \ '__| |/ _` | | |_  / _` | __| |/ _ \| '_ \
  //  ___) |  __/ |  | | (_| | | |/ / (_| | |_| | (_) | | | |
  // |____/ \___|_|  |_|\__,_|_|_/___\__,_|\__|_|\___/|_| |_|

#if JAVASCRIPT
#else

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

    for i in 0 .. (fb.PatchesLength - 1) do
      let patch = fb.Patches(i)
      if patch.HasValue then
        patch.Value
        |> Patch.FromFB
        |> Option.map (fun patch -> patches <- Map.add patch.Id patch patches)
        |> ignore

    for i in 0 .. (fb.IOBoxesLength - 1) do
      let iobox = fb.IOBoxes(i)
      if iobox.HasValue then
        iobox.Value
        |> IOBox.FromFB
        |> Option.map (fun iobox -> ioboxes <- Map.add iobox.Id iobox ioboxes)
        |> ignore

    for i in 0 .. (fb.CuesLength - 1) do
      let cue = fb.Cues(i)
      if cue.HasValue then
        cue.Value
        |> Cue.FromFB
        |> Option.map (fun cue -> cues <- Map.add cue.Id cue cues)
        |> ignore

    for i in 0 .. (fb.CueListsLength - 1) do
      let cuelist = fb.CueLists(i)
      if cuelist.HasValue then
        cuelist.Value
        |> CueList.FromFB
        |> Option.map (fun cuelist -> cuelists <- Map.add cuelist.Id cuelist cuelists)
        |> ignore

    for i in 0 .. (fb.NodesLength - 1) do
      let node = fb.Nodes(i)
      if node.HasValue then
        node.Value
        |> RaftNode.FromFB
        |> Option.map (fun node -> nodes <- Map.add node.Id node nodes)
        |> ignore

    for i in 0 .. (fb.UsersLength - 1) do
      let user = fb.Users(i)
      if user.HasValue then
        user.Value
        |> User.FromFB
        |> Option.map (fun user -> users <- Map.add user.Id user users)
        |> ignore

    for i in 0 .. (fb.SessionsLength - 1) do
      let session = fb.Sessions(i)
      if session.HasValue then
        session.Value
        |> Session.FromFB
        |> Option.map (fun session -> sessions <- Map.add session.Id session sessions)
        |> ignore

    Some { Patches  = patches
         ; IOBoxes  = ioboxes
         ; Cues     = cues
         ; CueLists = cuelists
         ; Nodes    = nodes
         ; Users    = users
         ; Sessions = sessions }

  static member FromBytes (bytes: byte array) : State option =
    StateFB.GetRootAsStateFB(new ByteBuffer(bytes))
    |> State.FromFB

#endif

// ********************************************************************************************** //
//  ____  _
// / ___|| |_ ___  _ __ ___
// \___ \| __/ _ \| '__/ _ \
//  ___) | || (_) | | |  __/
// |____/ \__\___/|_|  \___|
//
// ********************************************************************************************** //

(* Action: Log entry for the Event occurred and the resulting state. *)
and StoreAction =
  { Event: StateMachine
  ; State: State }

  override self.ToString() : string =
    sprintf "%s %s" (self.Event.ToString()) (self.State.ToString())

// ********************************************************************************************** //
//  _   _ _     _
// | | | (_)___| |_ ___  _ __ _   _
// | |_| | / __| __/ _ \| '__| | | |
// |  _  | \__ \ || (_) | |  | |_| |
// |_| |_|_|___/\__\___/|_|   \__, |
//                            |___/
//
// ********************************************************************************************** //

and History (action: StoreAction) =
  let mutable depth = 10
  let mutable debug = false
  let mutable head = 1
  let mutable values = [ action ]

  (* - - - - - - - - - - Properties - - - - - - - - - - *)
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

// ********************************************************************************************** //
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
// ********************************************************************************************** //

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
      self.Notify(ev)                    // 2) notify all listeners (render as soon as possible)
      history.Append({ Event = ev        // 3) store this action the and state it produced
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

// ********************************************************************************************** //
//  _     _     _
// | |   (_)___| |_ ___ _ __   ___ _ __
// | |   | / __| __/ _ \ '_ \ / _ \ '__|
// | |___| \__ \ ||  __/ | | |  __/ |
// |_____|_|___/\__\___|_| |_|\___|_|
//
// ********************************************************************************************** //

and Listener = Store -> StateMachine -> unit

// ********************************************************************************************** //
//  ____  _        _       __  __            _     _
// / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
// \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
// |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|
//
// ********************************************************************************************** //

and StateMachine =

  // CLIENT
  // | AddClient    of string
  // | UpdateClient of string
  // | RemoveClient of string

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
    // PROJECT
    // | OpenProject   -> "OpenProject"
    // | SaveProject   -> "SaveProject"
    // | CreateProject -> "CreateProject"
    // | CloseProject  -> "CloseProject"
    // | DeleteProject -> "DeleteProject

    // CLIENT
    // | AddClient    s -> sprintf "AddClient %s"    s
    // | UpdateClient s -> sprintf "UpdateClient %s" s
    // | RemoveClient s -> sprintf "RemoveClient %s" s

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

#if JAVASCRIPT
#else

  static member FromFB (fb: StateMachineFB) =
    match fb.AppEventType with

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | StateMachineTypeFB.AddCueFB ->
      let ev = fb.AppEvent<AddCueFB>()
      if ev.HasValue then
        let appevent : AddCueFB = ev.Value
        let cue = appevent.Cue
        if cue.HasValue then
          cue.Value
          |> Cue.FromFB
          |> Option.map AddCue
        else None
      else None

    | StateMachineTypeFB.UpdateCueFB  ->
      let ev = fb.AppEvent<UpdateCueFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let cue = appevent.Cue
        if cue.HasValue then
          cue.Value
          |> Cue.FromFB
          |> Option.map UpdateCue
        else None
      else None

    | StateMachineTypeFB.RemoveCueFB  ->
      let ev = fb.AppEvent<RemoveCueFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let cue = appevent.Cue
        if cue.HasValue then
          cue.Value
          |> Cue.FromFB
          |> Option.map RemoveCue
        else None
      else None

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | StateMachineTypeFB.AddCueListFB ->
      let ev = fb.AppEvent<AddCueListFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let cuelist = appevent.CueList
        if cuelist.HasValue then
          cuelist.Value
          |> CueList.FromFB
          |> Option.map AddCueList
        else None
      else None

    | StateMachineTypeFB.UpdateCueListFB  ->
      let ev = fb.AppEvent<UpdateCueListFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let cuelist = appevent.CueList
        if cuelist.HasValue then
          cuelist.Value
          |> CueList.FromFB
          |> Option.map UpdateCueList
        else None
      else None

    | StateMachineTypeFB.RemoveCueListFB  ->
      let ev = fb.AppEvent<RemoveCueListFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let cuelist = appevent.CueList
        if cuelist.HasValue then
          cuelist.Value
          |> CueList.FromFB
          |> Option.map RemoveCueList
        else None
      else None

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | StateMachineTypeFB.AddPatchFB ->
      let ev = fb.AppEvent<AddPatchFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let patch = appevent.Patch
        if patch.HasValue then
          patch.Value
          |> Patch.FromFB
          |> Option.map AddPatch
        else None
      else None

    | StateMachineTypeFB.UpdatePatchFB  ->
      let ev = fb.AppEvent<UpdatePatchFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let patch = appevent.Patch
        if patch.HasValue then
          patch.Value
          |> Patch.FromFB
          |> Option.map UpdatePatch
        else None
      else None

    | StateMachineTypeFB.RemovePatchFB  ->
      let ev = fb.AppEvent<RemovePatchFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let patch = appevent.Patch
        if patch.HasValue then
          patch.Value
          |> Patch.FromFB
          |> Option.map RemovePatch
        else None
      else None

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    | StateMachineTypeFB.AddIOBoxFB ->
      let ev = fb.AppEvent<AddIOBoxFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let iobox = appevent.IOBox
        if iobox.HasValue then
          iobox.Value
          |> IOBox.FromFB
          |> Option.map AddIOBox
        else None
      else None

    | StateMachineTypeFB.UpdateIOBoxFB  ->
      let ev = fb.AppEvent<UpdateIOBoxFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let iobox = appevent.IOBox
        if iobox.HasValue then
          iobox.Value
          |> IOBox.FromFB
          |> Option.map UpdateIOBox
        else None
      else None

    | StateMachineTypeFB.RemoveIOBoxFB  ->
      let ev = fb.AppEvent<RemoveIOBoxFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let iobox = appevent.IOBox
        if iobox.HasValue then
          iobox.Value
          |> IOBox.FromFB
          |> Option.map RemoveIOBox
        else None
      else None

    //  _   _           _
    // | \ | | ___   __| | ___
    // |  \| |/ _ \ / _` |/ _ \
    // | |\  | (_) | (_| |  __/
    // |_| \_|\___/ \__,_|\___|

    | StateMachineTypeFB.AddNodeFB ->
      let ev = fb.AppEvent<AddNodeFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let node = appevent.Node
        if node.HasValue then
          node.Value
          |> RaftNode.FromFB
          |> Option.map AddNode
        else None
      else None

    | StateMachineTypeFB.UpdateNodeFB  ->
      let ev = fb.AppEvent<UpdateNodeFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let node = appevent.Node
        if node.HasValue then
          node.Value
          |> RaftNode.FromFB
          |> Option.map UpdateNode
        else None
      else None

    | StateMachineTypeFB.RemoveNodeFB  ->
      let ev = fb.AppEvent<RemoveNodeFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let node = appevent.Node
        if node.HasValue then
          node.Value
          |> RaftNode.FromFB
          |> Option.map RemoveNode
        else None
      else None

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | StateMachineTypeFB.AddUserFB ->
      let ev = fb.AppEvent<AddUserFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let user = appevent.User
        if user.HasValue then
          user.Value
          |> User.FromFB
          |> Option.map AddUser
        else None
      else None

    | StateMachineTypeFB.UpdateUserFB  ->
      let ev = fb.AppEvent<UpdateUserFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let user = appevent.User
        if user.HasValue then
          user.Value
          |> User.FromFB
          |> Option.map UpdateUser
        else None
      else None

    | StateMachineTypeFB.RemoveUserFB  ->
      let ev = fb.AppEvent<RemoveUserFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let user = appevent.User
        if user.HasValue then
          user.Value
          |> User.FromFB
          |> Option.map RemoveUser
        else None
      else None

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | StateMachineTypeFB.AddSessionFB ->
      let ev = fb.AppEvent<AddSessionFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let session = appevent.Session
        if session.HasValue then
          session.Value
          |> Session.FromFB
          |> Option.map AddSession
        else None
      else None

    | StateMachineTypeFB.UpdateSessionFB  ->
      let ev = fb.AppEvent<UpdateSessionFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let session = appevent.Session
        if session.HasValue then
          session.Value
          |> Session.FromFB
          |> Option.map UpdateSession
        else None
      else None

    | StateMachineTypeFB.RemoveSessionFB  ->
      let ev = fb.AppEvent<RemoveSessionFB>()
      if ev.HasValue then
        let appevent = ev.Value
        let session = appevent.Session
        if session.HasValue then
          session.Value
          |> Session.FromFB
          |> Option.map RemoveSession
        else None
      else None

    //  __  __ _
    // |  \/  (_)___  ___
    // | |\/| | / __|/ __|
    // | |  | | \__ \ (__
    // |_|  |_|_|___/\___|

    | StateMachineTypeFB.LogMsgFB     ->
      let ev = fb.AppEvent<LogMsgFB>()
      if ev.HasValue then
        let log = ev.Value
        LogLevel.Parse log.LogLevel
        |> Option.map (fun level -> LogMsg(level, log.Msg))
      else None

    | StateMachineTypeFB.AppCommandFB ->
      let ev = fb.AppEvent<AppCommandFB>()
      if ev.HasValue then
        let cmd = ev.Value
        AppCommand.FromFB cmd
        |> Option.map Command
      else None

    | StateMachineTypeFB.DataSnapshotFB ->
      let ev = fb.AppEvent<DataSnapshotFB>()
      if ev.HasValue then
        let snapshot = ev.Value
        let data = snapshot.Data
        if data.HasValue then
          data.Value
          |> State.FromFB
          |> Option.map DataSnapshot
        else None
      else None

    | _ -> None

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<StateMachineFB> =
    let mkOffset tipe value =
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAppEventType(builder, tipe)
      StateMachineFB.AddAppEvent(builder, value)
      StateMachineFB.EndStateMachineFB(builder)

    match self with
    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | AddCue cue ->
      let cuefb = cue.ToOffset(builder)
      AddCueFB.StartAddCueFB(builder)
      AddCueFB.AddCue(builder, cuefb)
      let addfb = AddCueFB.EndAddCueFB(builder)
      mkOffset StateMachineTypeFB.AddCueFB addfb.Value

    | UpdateCue cue ->
      let cuefb = cue.ToOffset(builder)
      UpdateCueFB.StartUpdateCueFB(builder)
      UpdateCueFB.AddCue(builder, cuefb)
      let updatefb = UpdateCueFB.EndUpdateCueFB(builder)
      mkOffset StateMachineTypeFB.UpdateCueFB updatefb.Value

    | RemoveCue cue ->
      let cuefb = cue.ToOffset(builder)
      RemoveCueFB.StartRemoveCueFB(builder)
      RemoveCueFB.AddCue(builder, cuefb)
      let removefb = RemoveCueFB.EndRemoveCueFB(builder)
      mkOffset StateMachineTypeFB.RemoveCueFB removefb.Value

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | AddCueList cuelist ->
      let cuelistfb = cuelist.ToOffset(builder)
      AddCueListFB.StartAddCueListFB(builder)
      AddCueListFB.AddCueList(builder, cuelistfb)
      let addfb = AddCueListFB.EndAddCueListFB(builder)
      mkOffset StateMachineTypeFB.AddCueListFB addfb.Value

    | UpdateCueList cuelist ->
      let cuelistfb = cuelist.ToOffset(builder)
      UpdateCueListFB.StartUpdateCueListFB(builder)
      UpdateCueListFB.AddCueList(builder, cuelistfb)
      let updatefb = UpdateCueListFB.EndUpdateCueListFB(builder)
      mkOffset StateMachineTypeFB.UpdateCueListFB updatefb.Value

    | RemoveCueList cuelist ->
      let cuelistfb = cuelist.ToOffset(builder)
      RemoveCueListFB.StartRemoveCueListFB(builder)
      RemoveCueListFB.AddCueList(builder, cuelistfb)
      let removefb = RemoveCueListFB.EndRemoveCueListFB(builder)
      mkOffset StateMachineTypeFB.RemoveCueListFB removefb.Value

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | AddPatch patch ->
      let patchfb = patch.ToOffset(builder)
      AddPatchFB.StartAddPatchFB(builder)
      AddPatchFB.AddPatch(builder, patchfb)
      let addfb = AddPatchFB.EndAddPatchFB(builder)
      mkOffset StateMachineTypeFB.AddPatchFB addfb.Value

    | UpdatePatch patch ->
      let patchfb = patch.ToOffset(builder)
      UpdatePatchFB.StartUpdatePatchFB(builder)
      UpdatePatchFB.AddPatch(builder, patchfb)
      let updatefb = UpdatePatchFB.EndUpdatePatchFB(builder)
      mkOffset StateMachineTypeFB.UpdatePatchFB updatefb.Value

    | RemovePatch patch ->
      let patchfb = patch.ToOffset(builder)
      RemovePatchFB.StartRemovePatchFB(builder)
      RemovePatchFB.AddPatch(builder, patchfb)
      let removefb = RemovePatchFB.EndRemovePatchFB(builder)
      mkOffset StateMachineTypeFB.RemovePatchFB removefb.Value

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    | AddIOBox iobox ->
      let ioboxfb = iobox.ToOffset(builder)
      AddIOBoxFB.StartAddIOBoxFB(builder)
      AddIOBoxFB.AddIOBox(builder, ioboxfb)
      let addfb = AddIOBoxFB.EndAddIOBoxFB(builder)
      mkOffset StateMachineTypeFB.AddIOBoxFB addfb.Value

    | UpdateIOBox iobox ->
      let ioboxfb = iobox.ToOffset(builder)
      UpdateIOBoxFB.StartUpdateIOBoxFB(builder)
      UpdateIOBoxFB.AddIOBox(builder, ioboxfb)
      let updatefb = UpdateIOBoxFB.EndUpdateIOBoxFB(builder)
      mkOffset StateMachineTypeFB.UpdateIOBoxFB updatefb.Value

    | RemoveIOBox iobox ->
      let ioboxfb = iobox.ToOffset(builder)
      RemoveIOBoxFB.StartRemoveIOBoxFB(builder)
      RemoveIOBoxFB.AddIOBox(builder, ioboxfb)
      let removefb = RemoveIOBoxFB.EndRemoveIOBoxFB(builder)
      mkOffset StateMachineTypeFB.RemoveIOBoxFB removefb.Value

    //  ____        __ _   _   _           _
    // |  _ \ __ _ / _| |_| \ | | ___   __| | ___
    // | |_) / _` | |_| __|  \| |/ _ \ / _` |/ _ \
    // |  _ < (_| |  _| |_| |\  | (_) | (_| |  __/
    // |_| \_\__,_|_|  \__|_| \_|\___/ \__,_|\___|

    | AddNode node ->
      let nodefb = node.ToOffset(builder)
      AddNodeFB.StartAddNodeFB(builder)
      AddNodeFB.AddNode(builder, nodefb)
      let addfb = AddNodeFB.EndAddNodeFB(builder)
      mkOffset StateMachineTypeFB.AddNodeFB addfb.Value

    | UpdateNode node ->
      let nodefb = node.ToOffset(builder)
      UpdateNodeFB.StartUpdateNodeFB(builder)
      UpdateNodeFB.AddNode(builder, nodefb)
      let updatefb = UpdateNodeFB.EndUpdateNodeFB(builder)
      mkOffset StateMachineTypeFB.UpdateNodeFB updatefb.Value

    | RemoveNode node ->
      let nodefb = node.ToOffset(builder)
      RemoveNodeFB.StartRemoveNodeFB(builder)
      RemoveNodeFB.AddNode(builder, nodefb)
      let removefb = RemoveNodeFB.EndRemoveNodeFB(builder)
      mkOffset StateMachineTypeFB.RemoveNodeFB removefb.Value

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | AddUser user ->
      let userfb = user.ToOffset(builder)
      AddUserFB.StartAddUserFB(builder)
      AddUserFB.AddUser(builder, userfb)
      let addfb = AddUserFB.EndAddUserFB(builder)
      mkOffset StateMachineTypeFB.AddUserFB addfb.Value

    | UpdateUser user ->
      let userfb = user.ToOffset(builder)
      UpdateUserFB.StartUpdateUserFB(builder)
      UpdateUserFB.AddUser(builder, userfb)
      let updatefb = UpdateUserFB.EndUpdateUserFB(builder)
      mkOffset StateMachineTypeFB.UpdateUserFB updatefb.Value

    | RemoveUser user ->
      let userfb = user.ToOffset(builder)
      RemoveUserFB.StartRemoveUserFB(builder)
      RemoveUserFB.AddUser(builder, userfb)
      let removefb = RemoveUserFB.EndRemoveUserFB(builder)
      mkOffset StateMachineTypeFB.RemoveUserFB removefb.Value

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | AddSession session ->
      let sessionfb = session.ToOffset(builder)
      AddSessionFB.StartAddSessionFB(builder)
      AddSessionFB.AddSession(builder, sessionfb)
      let addfb = AddSessionFB.EndAddSessionFB(builder)
      mkOffset StateMachineTypeFB.AddSessionFB addfb.Value

    | UpdateSession session ->
      let sessionfb = session.ToOffset(builder)
      UpdateSessionFB.StartUpdateSessionFB(builder)
      UpdateSessionFB.AddSession(builder, sessionfb)
      let updatefb = UpdateSessionFB.EndUpdateSessionFB(builder)
      mkOffset StateMachineTypeFB.UpdateSessionFB updatefb.Value

    | RemoveSession session ->
      let sessionfb = session.ToOffset(builder)
      RemoveSessionFB.StartRemoveSessionFB(builder)
      RemoveSessionFB.AddSession(builder, sessionfb)
      let removefb = RemoveSessionFB.EndRemoveSessionFB(builder)
      mkOffset StateMachineTypeFB.RemoveSessionFB removefb.Value

    //  __  __ _
    // |  \/  (_)___  ___
    // | |\/| | / __|/ __|
    // | |  | | \__ \ (__
    // |_|  |_|_|___/\___|

    | Command ev ->
      let cmdfb = ev.ToOffset(builder)
      mkOffset StateMachineTypeFB.AppCommandFB cmdfb.Value

    | LogMsg(level, msg) ->
      let level = string level |> builder.CreateString
      let msg = msg |> builder.CreateString
      let log = LogMsgFB.CreateLogMsgFB(builder, level, msg)
      mkOffset StateMachineTypeFB.LogMsgFB log.Value

    | DataSnapshot state ->
      let statefb = state.ToOffset(builder)
      DataSnapshotFB.StartDataSnapshotFB(builder)
      DataSnapshotFB.AddData(builder, statefb)
      let snapshot = DataSnapshotFB.EndDataSnapshotFB(builder)
      mkOffset StateMachineTypeFB.DataSnapshotFB snapshot.Value

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes (bytes: byte array) : StateMachine option =
    let msg = StateMachineFB.GetRootAsStateMachineFB(new ByteBuffer(bytes))
    StateMachine.FromFB(msg)

#endif
