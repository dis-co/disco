namespace Iris.Core

// * Imports

//  ___                            _
// |_ _|_ __ ___  _ __   ___  _ __| |_ ___
//  | || '_ ` _ \| '_ \ / _ \| '__| __/ __|
//  | || | | | | | |_) | (_) | |  | |_\__ \
// |___|_| |_| |_| .__/ \___/|_|   \__|___/
//               |_|

open Iris.Raft

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

#endif

// * AppCommand

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

  // ** ToString

  override self.ToString() =
    match self with
    | Undo -> "Undo"
    | Redo -> "Redo"
    | Reset -> "Reset"
    // PROJECT
    | SaveProject -> "SaveProject"

  // ** Parse

  static member Parse (str: string) =
    match str with
    | "Undo"        -> Undo
    | "Redo"        -> Redo
    | "Reset"       -> Reset
    | "SaveProject" -> SaveProject
    | _             -> failwithf "AppCommand: parse error: %s" str

  // ** TryParse

  static member TryParse (str: string) =
    Either.tryWith (Error.asParseError "AppCommand.TryParse") <| fun _ ->
      str |> AppCommand.Parse

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: ActionTypeFB) =
#if FABLE_COMPILER
    match fb with
    | x when x = ActionTypeFB.UndoFB        -> Right Undo
    | x when x = ActionTypeFB.RedoFB        -> Right Redo
    | x when x = ActionTypeFB.ResetFB       -> Right Reset
    | x when x = ActionTypeFB.SaveProjectFB -> Right SaveProject
    | x ->
      sprintf "Could not parse %A as AppCommand" x
      |> Error.asParseError "AppCommand.FromFB"
      |> Either.fail
#else
    match fb with
    | ActionTypeFB.UndoFB        -> Right Undo
    | ActionTypeFB.RedoFB        -> Right Redo
    | ActionTypeFB.ResetFB       -> Right Reset
    | ActionTypeFB.SaveProjectFB -> Right SaveProject
    | x ->
      sprintf "Could not parse %A as AppCommand" x
      |> Error.asParseError "AppCommand.FromFB"
      |> Either.fail
#endif

  // ** ToOffset

  member self.ToOffset(_: FlatBufferBuilder) : ActionTypeFB =
    match self with
    | Undo        -> ActionTypeFB.UndoFB
    | Redo        -> ActionTypeFB.RedoFB
    | Reset       -> ActionTypeFB.ResetFB
    | SaveProject -> ActionTypeFB.SaveProjectFB

// * State Type

//   ____  _        _
//  / ___|| |_ __ _| |_ ___
//  \___ \| __/ _` | __/ _ \
//   ___) | || (_| | ||  __/
//  |____/ \__\__,_|\__\___|

//  Record type containing all the actual data that gets passed around in our
//  application.
//

type State =
  { Project  : IrisProject
    Patches  : Map<Id,Patch>
    Cues     : Map<Id,Cue>
    CueLists : Map<Id,CueList>
    Sessions : Map<Id,Session>
    Users    : Map<Id,User> }

  // ** Empty

  static member Empty
    with get () =
      { Project  = IrisProject.Empty
        Patches  = Map.empty
        Cues     = Map.empty
        CueLists = Map.empty
        Sessions = Map.empty
        Users    = Map.empty }

  // ** Load

  #if !FABLE_COMPILER

  static member Load (path: FilePath) =
    either {
      let! machine  = MachineConfig.load None
      let! project  = Project.load path machine
      let! users    = Asset.loadAll project.Path
      let! cues     = Asset.loadAll project.Path
      let! cuelists = Asset.loadAll project.Path
      let! patches  = Asset.loadAll project.Path

      return
        { Project  = project
          Users    = Array.map toPair users    |> Map.ofArray
          Cues     = Array.map toPair cues     |> Map.ofArray
          CueLists = Array.map toPair cuelists |> Map.ofArray
          Patches  = Array.map toPair patches  |> Map.ofArray
          Sessions = Map.empty }
    }

  #endif

  // ** Save

  #if !FABLE_COMPILER

  member state.Save (basePath: FilePath) =
    either {
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.Patches
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.Cues
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.CueLists
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.Users
      do! Asset.save state.Project basePath
    }

  #endif

  // ** UpdateProject

  member state.UpdateProject (project: IrisProject) =
    { state with Project = project }

  // ** AddUser

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

  // ** UpdateUser

  member state.UpdateUser (user: User) =
    if Map.containsKey user.Id state.Users then
      let users = Map.add user.Id user state.Users
      { state with Users = users }
    else
      state

  // ** RemoveUser

  member state.RemoveUser (user: User) =
    { state with Users = Map.filter (fun k _ -> (k <> user.Id)) state.Users }

  // ** AddSession

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

  // ** UpdateSession

  member state.UpdateSession (session: Session) =
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        Map.add session.Id session state.Sessions
      else
        state.Sessions
    { state with Sessions = sessions }

  // ** RemoveSession

  member state.RemoveSession (session: Session) =
    { state with Sessions = Map.filter (fun k _ -> (k <> session.Id)) state.Sessions }

  // ** AddPatch

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

  // ** UpdatePatch

  member state.UpdatePatch (patch : Patch) =
    if Map.containsKey patch.Id state.Patches then
      { state with Patches = Map.add patch.Id patch state.Patches }
    else
      state

  // ** RemovePatch

  member state.RemovePatch (patch : Patch) =
    { state with Patches = Map.remove patch.Id state.Patches }


  // ** AddPin

  //  ___ ___  ____
  // |_ _/ _ \| __ )  _____  __
  //  | | | | |  _ \ / _ \ \/ /
  //  | | |_| | |_) | (_) >  <
  // |___\___/|____/ \___/_/\_\

  member state.AddPin (pin : Pin) =
    if Map.containsKey pin.Patch state.Patches then
      let update _ (patch: Patch) =
        if patch.Id = pin.Patch then
          Patch.AddPin patch pin
        else
          patch
      { state with Patches = Map.map update state.Patches }
    else
      state

  // ** UpdatePin

  member state.UpdatePin (pin : Pin) =
    let mapper (_: Id) (patch : Patch) =
      if patch.Id = pin.Patch then
        Patch.UpdatePin patch pin
      else
        patch
    { state with Patches = Map.map mapper state.Patches }

  // ** RemovePin

  member state.RemovePin (pin : Pin) =
    let updater _ (patch : Patch) =
      if pin.Patch = patch.Id
      then Patch.RemovePin patch pin
      else patch
    { state with Patches = Map.map updater state.Patches }

  // ** AddCueList

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

  // ** UpdateCueList

  member state.UpdateCueList (cuelist : CueList) =
    if Map.containsKey cuelist.Id state.CueLists then
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }
    else
      state

  // ** RemoveCueList

  member state.RemoveCueList (cuelist : CueList) =
    { state with CueLists = Map.remove cuelist.Id state.CueLists }

  // ** AddCue

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

  // ** UpdateCue

  member state.UpdateCue (cue : Cue) =
    if Map.containsKey cue.Id state.Cues then
      { state with Cues = Map.add cue.Id cue state.Cues }
    else
      state

  // ** RemoveCue

  member state.RemoveCue (cue : Cue) =
    { state with Cues = Map.remove cue.Id state.Cues }

  // ** AddMember

  member state.AddMember (mem: RaftMember) =
    { state with Project = Project.addMember mem state.Project }

  // ** UpdateMember

  member state.UpdateMember (mem: RaftMember) =
    { state with Project = Project.updateMember mem state.Project }

  // ** RemoveMember

  member state.RemoveMember (mem: RaftMember) =
    { state with Project = Project.removeMember mem.Id state.Project }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<StateFB> =
    let project = Binary.toOffset builder self.Project

    let patches =
      Map.toArray self.Patches
      |> Array.map (snd >> Binary.toOffset builder)

    let patchesoffset = StateFB.CreatePatchesVector(builder, patches)

    let cues =
      Map.toArray self.Cues
      |> Array.map (snd >> Binary.toOffset builder)

    let cuesoffset = StateFB.CreateCuesVector(builder, cues)

    let cuelists =
      Map.toArray self.CueLists
      |> Array.map (snd >> Binary.toOffset builder)

    let cuelistsoffset = StateFB.CreateCueListsVector(builder, cuelists)

    let users =
      Map.toArray self.Users
      |> Array.map (snd >> Binary.toOffset builder)

    let usersoffset = StateFB.CreateUsersVector(builder, users)

    let sessions =
      Map.toArray self.Sessions
      |> Array.map (snd >> Binary.toOffset builder)

    let sessionsoffset = StateFB.CreateSessionsVector(builder, sessions)

    StateFB.StartStateFB(builder)
    StateFB.AddProject(builder, project)
    StateFB.AddPatches(builder, patchesoffset)
    StateFB.AddCues(builder, cuesoffset)
    StateFB.AddCueLists(builder, cuelistsoffset)
    StateFB.AddSessions(builder, sessionsoffset)
    StateFB.AddUsers(builder, usersoffset)
    StateFB.EndStateFB(builder)

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** FromFB

  static member FromFB(fb: StateFB) : Either<IrisError, State> =
    either {
      // PROJECT

      let! project =
        #if FABLE_COMPILER
        IrisProject.FromFB fb.Project
        #else
        let pfb = fb.Project
        if pfb.HasValue then
          let projectish = pfb.Value
          IrisProject.FromFB projectish
        else
          "Could not parse empty ProjectFB"
          |> Error.asParseError "State.FromFB"
          |> Either.fail
        #endif

      // PATCHES

      let! patches =
        let arr = Array.zeroCreate fb.PatchesLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, Patch>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! patch = fb.Patches(i) |> Patch.FromFB
            #else
            let! patch =
              let value = fb.Patches(i)
              if value.HasValue then
                value.Value
                |> Patch.FromFB
              else
                "Could not parse empty patch payload"
                |> Error.asParseError "State.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add patch.Id patch map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      // CUES

      let! cues =
        let arr = Array.zeroCreate fb.CuesLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, Cue>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! cue = fb.Cues(i) |> Cue.FromFB
            #else
            let! cue =
              let value = fb.Cues(i)
              if value.HasValue then
                value.Value
                |> Cue.FromFB
              else
                "Could not parse empty Cue payload"
                |> Error.asParseError "State.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add cue.Id cue map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      // CUELISTS

      let! cuelists =
        let arr = Array.zeroCreate fb.CueListsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, CueList>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! cuelist = fb.CueLists(i) |> CueList.FromFB
            #else
            let! cuelist =
              let value = fb.CueLists(i)
              if value.HasValue then
                value.Value
                |> CueList.FromFB
              else
                "Could not parse empty CueList payload"
                |> Error.asParseError "State.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add cuelist.Id cuelist map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      // USERS

      let! users =
        let arr = Array.zeroCreate fb.UsersLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, User>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! user = fb.Users(i) |> User.FromFB
            #else
            let! user =
              let value = fb.Users(i)
              if value.HasValue then
                value.Value
                |> User.FromFB
              else
                "Could not parse empty User payload"
                |> Error.asParseError "State.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add user.Id user map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      // SESSIONS

      let! sessions =
        let arr = Array.zeroCreate fb.SessionsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, Session>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! session = fb.Sessions(i) |> Session.FromFB
            #else
            let! session =
              let value = fb.Sessions(i)
              if value.HasValue then
                value.Value
                |> Session.FromFB
              else
                "Could not parse empty Session payload"
                |> Error.asParseError "State.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add session.Id session map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      return { Project  = project
               Patches  = patches
               Cues     = cues
               CueLists = cuelists
               Users    = users
               Sessions = sessions }
    }

  // ** FromBytes

  static member FromBytes (bytes: Binary.Buffer) : Either<IrisError,State> =
    Binary.createBuffer bytes
    |> StateFB.GetRootAsStateFB
    |> State.FromFB

// * Store Action

//  ____  _                      _        _   _
// / ___|| |_ ___  _ __ ___     / \   ___| |_(_) ___  _ __
// \___ \| __/ _ \| '__/ _ \   / _ \ / __| __| |/ _ \| '_ \
//  ___) | || (_) | | |  __/  / ___ \ (__| |_| | (_) | | | |
// |____/ \__\___/|_|  \___| /_/   \_\___|\__|_|\___/|_| |_|


/// ## StoreAction
///
/// Wraps a StateMachine command and the current state it is applied to in a record type. This type
/// is used internally in `Store` (more specifically `History`) to achieve the ability to easily
/// undo/redo changes made to the state. It also enables time-travelling debugging (specifically in
/// the front-end).
///
/// Returns: StoreAction
and StoreAction =
  { Event: StateMachine
  ; State: State }

  override self.ToString() : string =
    sprintf "%s %s" (self.Event.ToString()) (self.State.ToString())

// * History

//  _   _ _     _
// | | | (_)___| |_ ___  _ __ _   _
// | |_| | / __| __/ _ \| '__| | | |
// |  _  | \__ \ || (_) | |  | |_| |
// |_| |_|_|___/\__\___/|_|   \__, |
//                            |___/
//

/// ## History
///
/// Keep a history of state changes. Tracks `StoreActions` in a list to enable easy undo/redo
/// functionality.
///
/// ### Signature:
/// - action: `StoreAction` - the initial `StoreAction` beyond which there is no history
///
/// Returns: History
and History (action: StoreAction) =
  let mutable depth = 10
  let mutable debug = false
  let mutable head = 1
  let mutable values = [ action ]

  // ** Debug

  member self.Debug
    with get () = debug
    and  set b  =
      debug <- b
      if not debug then
        values <- List.take depth values

  // ** Depth

  member self.Depth
    with get () = depth
      and set n  = depth <- n

  // ** Values

  member self.Values
    with get () = values

  // ** Length

  member self.Length
    with get () = List.length values

  // ** Append

  member self.Append (value: StoreAction) : unit =
    head <- 0
    let newvalues = value :: values
    if (not debug) && List.length newvalues > depth then
      values <- List.take depth newvalues
    else
      values <- newvalues

  // ** Undo

  member self.Undo () : StoreAction option =
    let head' =
      if (head - 1) > (List.length values) then
        List.length values
      else
        head + 1

    if head <> head' then
      head <- head'

    List.tryItem head values

  // ** Redo

  member self.Redo () : StoreAction option =
    let head' =
      if   head - 1 < 0
      then 0
      else head - 1

    if head <> head' then
      head <- head'

    List.tryItem head values

// * Store

//  ____  _
// / ___|| |_ ___  _ __ ___
// \___ \| __/ _ \| '__/ _ \
//  ___) | || (_) | | |  __/
// |____/ \__\___/|_|  \___|
//

/// ## Store
///
/// The `Store` centrally manages all state changes and notifies interested parties of changes to
/// the carried state (e.g. views, socket transport). Clients of the `Store` can subscribe to change
/// notifications by regis a callback handler. `Store` is used in all parts of the Iris cluster
/// application, from the front-end, at the service level, to all registered clients. `StateMachine`
/// commands replicated via `Raft` are applied in the same order to it to ensure that all parties
/// have the same data.
///
/// ### Signature:
/// - state: `State` - the intitial state to use for the store
///
/// Returns: Store
and Store(state : State)=

  let mutable state = state

  let mutable history = new History {
      State = state;
      Event = Command(AppCommand.Reset);
    }

  let mutable listeners : Listener list = []

  // ** Notify

  /// ## Notify
  ///
  /// Notify all listeners (registered callbacks) of the StateMachine command that was just applied
  /// to the `State` atom.
  ///
  /// ### Signature:
  /// - ev: `StateMachine` - command that was applied to `State`
  ///
  /// Returns: unit
  member private store.Notify (ev : StateMachine) =
    List.iter (fun f -> f store ev) listeners


  // ** Debug

  /// ## Debug property
  ///
  /// Turn debugging of Store on or off.
  ///
  /// Returns: set: bool -> unit, get: bool
  member self.Debug
    with get ()  = history.Debug
      and set dbg = history.Debug <- dbg

  // ** UndoSteps

  /// ## UndoSteps property
  ///
  /// Number of undo steps to keep around. This property is not honored if `Debug` is `true`.
  ///
  /// Returns: set: int -> unit, get: int
  member self.UndoSteps
    with get () = history.Depth
      and set n  = history.Depth <- n

  // ** Dispatch

  /// ## Dispatch
  ///
  /// Dispatch an action (StateMachine command) to be executed against the current version of the
  /// `State` to produce the next `State`.
  ///
  /// Then notify all listeners of the change, and record a history item for this change.
  ///
  /// ### Signature:
  /// - ev: `StateMachine` - command to apply to the `State`
  ///
  /// Returns: unit
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

    | AddPin            pin -> state.AddPin      pin   |> andRender
    | UpdatePin         pin -> state.UpdatePin   pin   |> andRender
    | RemovePin         pin -> state.RemovePin   pin   |> andRender

    | AddMember            mem -> state.AddMember     mem    |> andRender
    | UpdateMember         mem -> state.UpdateMember  mem    |> andRender
    | RemoveMember         mem -> state.RemoveMember  mem    |> andRender

    | AddSession        session -> addSession session state    |> andRender
    | UpdateSession     session -> state.UpdateSession session |> andRender
    | RemoveSession     session -> state.RemoveSession session |> andRender

    | AddUser              user -> state.AddUser       user    |> andRender
    | UpdateUser           user -> state.UpdateUser    user    |> andRender
    | RemoveUser           user -> state.RemoveUser    user    |> andRender

    | _ -> ()

  // ** Subscribe

  /// ## Subscribe
  ///
  /// Register a callback to be invoked when a state change occurred.
  ///
  /// ### Signature:
  /// - listener: `Listener` - function of type `Listener` to be invoked
  ///
  /// Returns: unit
  member self.Subscribe (listener : Listener) =
    listeners <- listener :: listeners

  // ** State

  /// ## State
  ///
  /// Get the current `State`. This is a read-only property.
  ///
  /// Returns: State
  member self.State with get () = state

  // ** History

  /// ## History
  ///
  /// Get the current History. This is exposed mainly for debugging purposes.
  ///
  /// Returns: History
  member self.History with get () = history

  // ** Redo

  /// ## Redo
  ///
  /// Redo an undone change.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member self.Redo() =
    match history.Redo() with
      | Some log ->
        state <- log.State
        self.Notify log.Event |> ignore
      | _ -> ()

  // ** Undo

  /// ## Undo
  ///
  /// Undo the last change to the state atom.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member self.Undo() =
    match history.Undo() with
      | Some log ->
        state <- log.State
        self.Notify log.Event |> ignore
      | _ -> ()

// * Listener

//  _     _     _
// | |   (_)___| |_ ___ _ __   ___ _ __
// | |   | / __| __/ _ \ '_ \ / _ \ '__|
// | |___| \__ \ ||  __/ | | |  __/ |
// |_____|_|___/\__\___|_| |_|\___|_|
//

/// ## Listener
///
/// A `Listener` is type alias over a function that takes a `Store` and a `StateMachine` command,
/// which gets invoked once a state change occurred.
///
/// Returns: Store -> StateMachine -> unit
and Listener = Store -> StateMachine -> unit


// * StateMachine

//  ____  _        _       __  __            _     _
// / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
// \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
// |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

and StateMachine =

  // Member
  | AddMember     of RaftMember
  | UpdateMember  of RaftMember
  | RemoveMember  of RaftMember

  // PATCH
  | AddPatch      of Patch
  | UpdatePatch   of Patch
  | RemovePatch   of Patch

  // PIN
  | AddPin      of Pin
  | UpdatePin   of Pin
  | RemovePin   of Pin

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

  | SetLogLevel   of LogLevel

  | LogMsg        of LogEvent

  // ** ToString

  override self.ToString() : string =
    match self with

    // Member
    | AddMember    mem     -> sprintf "AddMember %s"    (string mem)
    | UpdateMember mem     -> sprintf "UpdateMember %s" (string mem)
    | RemoveMember mem     -> sprintf "RemoveMember %s" (string mem)

    // PATCH
    | AddPatch    patch     -> sprintf "AddPatch %s"    (string patch)
    | UpdatePatch patch     -> sprintf "UpdatePatch %s" (string patch)
    | RemovePatch patch     -> sprintf "RemovePatch %s" (string patch)

    // PIN
    | AddPin    pin     -> sprintf "AddPin %s"    (string pin)
    | UpdatePin pin     -> sprintf "UpdatePin %s" (string pin)
    | RemovePin pin     -> sprintf "RemovePin %s" (string pin)

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
    | SetLogLevel level     -> sprintf "SetLogLevel: %A" level
    | LogMsg log            -> sprintf "LogMsg: [%A] %s" log.LogLevel log.Message

  // ** FromFB (JavaScript)

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

#if FABLE_COMPILER
  static member FromFB (fb: ApiActionFB) =
    match fb.PayloadType with
    | x when x = PayloadFB.RaftMemberFB ->
      let mem = fb.RaftMemberFB |> RaftMember.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Either.map AddMember mem
      | x when x = ActionTypeFB.UpdateFB ->
        Either.map UpdateMember mem
      | x when x = ActionTypeFB.RemoveFB ->
        Either.map RemoveMember mem
      | x ->
        sprintf "Could not parse unknown ActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = PayloadFB.PatchFB ->
      let patch = fb.PatchFB |> Patch.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Either.map AddPatch patch
      | x when x = ActionTypeFB.UpdateFB ->
        Either.map UpdatePatch patch
      | x when x = ActionTypeFB.RemoveFB ->
        Either.map RemovePatch patch
      | x ->
        sprintf "Could not parse unknown ActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = PayloadFB.PinFB ->
      let pin = fb.PinFB |> Pin.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Either.map AddPin pin
      | x when x = ActionTypeFB.UpdateFB ->
        Either.map UpdatePin pin
      | x when x = ActionTypeFB.RemoveFB ->
        Either.map RemovePin pin
      | x ->
        sprintf "Could not parse unknown ActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = PayloadFB.CueFB ->
      let cue = fb.CueFB |> Cue.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Either.map AddCue cue
      | x when x = ActionTypeFB.UpdateFB ->
        Either.map UpdateCue cue
      | x when x = ActionTypeFB.RemoveFB ->
        Either.map RemoveCue cue
      | x ->
        sprintf "Could not parse unknown ActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = PayloadFB.CueListFB ->
      let cuelist = fb.CueListFB |> CueList.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Either.map AddCueList cuelist
      | x when x = ActionTypeFB.UpdateFB ->
        Either.map UpdateCueList cuelist
      | x when x = ActionTypeFB.RemoveFB ->
        Either.map RemoveCueList cuelist
      | x ->
        sprintf "Could not parse unknown ActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = PayloadFB.UserFB ->
      let user = fb.UserFB |> User.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Either.map AddUser user
      | x when x = ActionTypeFB.UpdateFB ->
        Either.map UpdateUser user
      | x when x = ActionTypeFB.RemoveFB ->
        Either.map RemoveUser user
      | x ->
        sprintf "Could not parse unknown ActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = PayloadFB.SessionFB ->
      let session = fb.SessionFB |> Session.FromFB
      match fb.Action with
      | x when x = ActionTypeFB.AddFB ->
        Either.map AddSession session
      | x when x = ActionTypeFB.UpdateFB ->
        Either.map UpdateSession session
      | x when x = ActionTypeFB.RemoveFB ->
        Either.map RemoveSession session
      | x ->
        sprintf "Could not parse unknown ActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = PayloadFB.StateFB && fb.Action = ActionTypeFB.DataSnapshotFB ->
      fb.StateFB
      |> State.FromFB
      |> Either.map DataSnapshot

    | x when x = PayloadFB.LogEventFB ->
      fb.LogEventFB
      |> LogEvent.FromFB
      |> Either.map LogMsg

    | x when x = PayloadFB.StringFB ->
      match fb.Action with
      | x when x = ActionTypeFB.SetLogLevelFB ->
        fb.StringFB.Value
        |> LogLevel.TryParse
        |> Either.map SetLogLevel
      | x ->
        sprintf "Could not parse unknown ActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail
    | _ ->
      fb.Action
      |> AppCommand.FromFB
      |> Either.map Command

#else

  // ** FromFB (.NET)

  static member FromFB (fb: ApiActionFB) =
    match fb.PayloadType with
    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | PayloadFB.CueFB ->
      either {
        let! cue =
          let cueish = fb.Payload<CueFB>()
          if cueish.HasValue then
            cueish.Value
            |> Cue.FromFB
          else
            "Could not parse empty cue payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | ActionTypeFB.AddFB    -> return (AddCue cue)
        | ActionTypeFB.UpdateFB -> return (UpdateCue cue)
        | ActionTypeFB.RemoveFB -> return (RemoveCue cue)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | PayloadFB.CueListFB ->
      either {
        let! cuelist =
          let cuelistish = fb.Payload<CueListFB>()
          if cuelistish.HasValue then
            cuelistish.Value
            |> CueList.FromFB
          else
            "Could not parse empty cuelist payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | ActionTypeFB.AddFB    -> return (AddCueList    cuelist)
        | ActionTypeFB.UpdateFB -> return (UpdateCueList cuelist)
        | ActionTypeFB.RemoveFB -> return (RemoveCueList cuelist)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | PayloadFB.PatchFB ->
      either {
        let! patch =
          let patchish = fb.Payload<PatchFB>()
          if patchish.HasValue then
            patchish.Value
            |> Patch.FromFB
          else
            "Could not parse empty patche payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | ActionTypeFB.AddFB    -> return (AddPatch    patch)
        | ActionTypeFB.UpdateFB -> return (UpdatePatch patch)
        | ActionTypeFB.RemoveFB -> return (RemovePatch patch)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    | PayloadFB.PinFB ->
      either {
        let! pin =
          let pinish = fb.Payload<PinFB>()
          if pinish.HasValue then
            pinish.Value
            |> Pin.FromFB
          else
            "Could not parse empty pin payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | ActionTypeFB.AddFB    -> return (AddPin    pin)
        | ActionTypeFB.UpdateFB -> return (UpdatePin pin)
        | ActionTypeFB.RemoveFB -> return (RemovePin pin)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //  _   _           _
    // | \ | | ___   __| | ___
    // |  \| |/ _ \ / _` |/ _ \
    // | |\  | (_) | (_| |  __/
    // |_| \_|\___/ \__,_|\___|

    | PayloadFB.RaftMemberFB ->
      either {
        let! mem =
          let memish = fb.Payload<RaftMemberFB>()
          if memish.HasValue then
            memish.Value
            |> RaftMember.FromFB
          else
            "Could not parse empty mem payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | ActionTypeFB.AddFB    -> return (AddMember    mem)
        | ActionTypeFB.UpdateFB -> return (UpdateMember mem)
        | ActionTypeFB.RemoveFB -> return (RemoveMember mem)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | PayloadFB.UserFB ->
      either {
        let! user =
          let userish = fb.Payload<UserFB>()
          if userish.HasValue then
            userish.Value
            |> User.FromFB
          else
            "Could not parse empty user payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | ActionTypeFB.AddFB    -> return (AddUser    user)
        | ActionTypeFB.UpdateFB -> return (UpdateUser user)
        | ActionTypeFB.RemoveFB -> return (RemoveUser user)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | PayloadFB.SessionFB ->
      either {
        let! session =
          let sessionish = fb.Payload<SessionFB>()
          if sessionish.HasValue then
            sessionish.Value
            |> Session.FromFB
          else
            "Could not parse empty session payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | ActionTypeFB.AddFB    -> return (AddSession    session)
        | ActionTypeFB.UpdateFB -> return (UpdateSession session)
        | ActionTypeFB.RemoveFB -> return (RemoveSession session)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }
    //  __  __ _
    // |  \/  (_)___  ___
    // | |\/| | / __|/ __|
    // | |  | | \__ \ (__
    // |_|  |_|_|___/\___|

    | PayloadFB.LogEventFB ->
      either {
        let logish = fb.Payload<LogEventFB>()
        if logish.HasValue then
          let! log = LogEvent.FromFB logish.Value
          return LogMsg(log)
        else
          return!
            "Could not parse empty LogEvent payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    | PayloadFB.StateFB ->
      either {
        let stateish = fb.Payload<StateFB>()
        if stateish.HasValue then
          let state = stateish.Value
          let! parsed = State.FromFB state
          return (DataSnapshot parsed)
        else
          return!
            "Could not parse empty state payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    | PayloadFB.StringFB ->
      either {
        let stringish = fb.Payload<StringFB> ()
        if stringish.HasValue then
          let value = stringish.Value
          let! parsed = LogLevel.TryParse value.Value
          return (SetLogLevel parsed)
        else
          return!
            "Could not parse empty string payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    | _ -> either {
      let! cmd = AppCommand.FromFB fb.Action
      return (Command cmd)
    }

#endif
  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<ApiActionFB> =
    match self with
    | AddMember       mem ->
      let mem = mem.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, mem)
#else
      ApiActionFB.AddPayload(builder, mem.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdateMember    mem ->
      let mem = mem.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, mem)
#else
      ApiActionFB.AddPayload(builder, mem.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemoveMember    mem ->
      let mem = mem.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, mem)
#else
      ApiActionFB.AddPayload(builder, mem.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddPatch       patch ->
      let patch = patch.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.PatchFB)
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, patch)
#else
      ApiActionFB.AddPayload(builder, patch.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddPin       pin ->
      let pin = pin.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.PinFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, pin)
#else
      ApiActionFB.AddPayload(builder, pin.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | UpdatePin    pin ->
      let pin = pin.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.UpdateFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.PinFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, pin)
#else
      ApiActionFB.AddPayload(builder, pin.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | RemovePin    pin ->
      let pin = pin.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.RemoveFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.PinFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, pin)
#else
      ApiActionFB.AddPayload(builder, pin.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | AddCue cue ->
      let cue = cue.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.AddFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.CueFB)
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
#if FABLE_COMPILER
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
      let offset = state.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.DataSnapshotFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.StateFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, offset)
#else
      ApiActionFB.AddPayload(builder, offset.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | LogMsg log ->
      let offset = log.ToOffset(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.LogEventFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.LogEventFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, offset)
#else
      ApiActionFB.AddPayload(builder, offset.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

    | SetLogLevel level ->
      let str = builder.CreateString (string level)
      StringFB.StartStringFB(builder)
      StringFB.AddValue(builder,str)
      let offset = StringFB.EndStringFB(builder)
      ApiActionFB.StartApiActionFB(builder)
      ApiActionFB.AddAction(builder, ActionTypeFB.SetLogLevelFB)
      ApiActionFB.AddPayloadType(builder, PayloadFB.StringFB)
#if FABLE_COMPILER
      ApiActionFB.AddPayload(builder, offset)
#else
      ApiActionFB.AddPayload(builder, offset.Value)
#endif
      ApiActionFB.EndApiActionFB(builder)

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: Binary.Buffer) : Either<IrisError,StateMachine> =
    Binary.createBuffer bytes
    |> ApiActionFB.GetRootAsApiActionFB
    |> StateMachine.FromFB
