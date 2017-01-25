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
open Iris.Serialization
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

  static member FromFB (fb: RaftActionTypeFB) =
#if FABLE_COMPILER
    match fb with
    | x when x = RaftActionTypeFB.UndoFB        -> Right Undo
    | x when x = RaftActionTypeFB.RedoFB        -> Right Redo
    | x when x = RaftActionTypeFB.ResetFB       -> Right Reset
    | x when x = RaftActionTypeFB.SaveProjectFB -> Right SaveProject
    | x ->
      sprintf "Could not parse %A as AppCommand" x
      |> Error.asParseError "AppCommand.FromFB"
      |> Either.fail
#else
    match fb with
    | RaftActionTypeFB.UndoFB        -> Right Undo
    | RaftActionTypeFB.RedoFB        -> Right Redo
    | RaftActionTypeFB.ResetFB       -> Right Reset
    | RaftActionTypeFB.SaveProjectFB -> Right SaveProject
    | x ->
      sprintf "Could not parse %A as AppCommand" x
      |> Error.asParseError "AppCommand.FromFB"
      |> Either.fail
#endif

  // ** ToOffset

  member self.ToOffset(_: FlatBufferBuilder) : RaftActionTypeFB =
    match self with
    | Undo        -> RaftActionTypeFB.UndoFB
    | Redo        -> RaftActionTypeFB.RedoFB
    | Reset       -> RaftActionTypeFB.ResetFB
    | SaveProject -> RaftActionTypeFB.SaveProjectFB

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
    Users    : Map<Id,User>
    Clients  : Map<Id,IrisClient> }

  // ** Empty

  static member Empty
    with get () =
      { Project  = IrisProject.Empty
        Patches  = Map.empty
        Cues     = Map.empty
        CueLists = Map.empty
        Sessions = Map.empty
        Users    = Map.empty
        Clients  = Map.empty }

  // ** Load

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load (path: FilePath, machine: IrisMachine) =
    either {
      let! project  = Asset.loadWithMachine path machine
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
          Sessions = Map.empty
          Clients  = Map.empty }
    }

  #endif

  // ** Save

  #if !FABLE_COMPILER && !IRIS_NODES

  member state.Save (basePath: FilePath) =
    either {
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.Patches
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.Cues
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.CueLists
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.Users
      do! Asset.save basePath state.Project
    }

  #endif

  // ** addUser

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  static member addUser (user: User) (state: State) =
    if Map.containsKey user.Id state.Users then
      state
    else
      let users = Map.add user.Id user state.Users
      { state with Users = users }

  // ** updateUser

  static member updateUser (user: User) (state: State) =
    if Map.containsKey user.Id state.Users then
      let users = Map.add user.Id user state.Users
      { state with Users = users }
    else
      state

  // ** RemoveUser

  static member removeUser (user: User) (state: State) =
    { state with Users = Map.filter (fun k _ -> (k <> user.Id)) state.Users }

  // ** addSession

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  static member addSession (session: Session) (state: State) =
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        state.Sessions
      else
        Map.add session.Id session state.Sessions
    { state with Sessions = sessions }

  // ** updateSession

  static member updateSession (session: Session) (state: State) =
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        Map.add session.Id session state.Sessions
      else
        state.Sessions
    { state with Sessions = sessions }

  // ** removeSession

  static member removeSession (session: Session) (state: State) =
    { state with Sessions = Map.filter (fun k _ -> (k <> session.Id)) state.Sessions }

  // ** addPatch

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  static member addPatch (patch : Patch) (state: State) =
    if Map.containsKey patch.Id state.Patches then
      state
    else
      { state with Patches = Map.add patch.Id patch state.Patches }

  // ** updatePatch

  static member updatePatch (patch : Patch) (state: State) =
    if Map.containsKey patch.Id state.Patches then
      { state with Patches = Map.add patch.Id patch state.Patches }
    else
      state

  // ** removePatch

  static member removePatch (patch : Patch) (state: State) =
    { state with Patches = Map.remove patch.Id state.Patches }


  // ** addPin

  //  ___ ___  ____
  // |_ _/ _ \| __ )  _____  __
  //  | | | | |  _ \ / _ \ \/ /
  //  | | |_| | |_) | (_) >  <
  // |___\___/|____/ \___/_/\_\

  static member addPin (pin : Pin) (state: State) =
    if Map.containsKey pin.Patch state.Patches then
      let update _ (patch: Patch) =
        if patch.Id = pin.Patch then
          Patch.AddPin patch pin
        else
          patch
      { state with Patches = Map.map update state.Patches }
    else
      state

  // ** updatePin

  static member updatePin (pin : Pin) (state: State) =
    let mapper (_: Id) (patch : Patch) =
      if patch.Id = pin.Patch then
        Patch.UpdatePin patch pin
      else
        patch
    { state with Patches = Map.map mapper state.Patches }

  // ** removePin

  static member removePin (pin : Pin) (state: State) =
    let updater _ (patch : Patch) =
      if pin.Patch = patch.Id
      then Patch.RemovePin patch pin
      else patch
    { state with Patches = Map.map updater state.Patches }

  // ** addCueList

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_ ___
  // | |  | | | |/ _ \ |   | / __| __/ __|
  // | |__| |_| |  __/ |___| \__ \ |_\__ \
  //  \____\__,_|\___|_____|_|___/\__|___/

  static member addCueList (cuelist : CueList) (state: State) =
    if Map.containsKey cuelist.Id state.CueLists then
      state
    else
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }

  // ** updateCueList

  static member updateCueList (cuelist : CueList) (state: State) =
    if Map.containsKey cuelist.Id state.CueLists then
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }
    else
      state

  // ** removeCueList

  static member removeCueList (cuelist : CueList) (state: State) =
    { state with CueLists = Map.remove cuelist.Id state.CueLists }

  // ** AddCue

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  static member addCue (cue : Cue) (state: State) =
    if Map.containsKey cue.Id state.Cues then
      state
    else
      { state with Cues = Map.add cue.Id cue state.Cues }

  // ** updateCue

  static member updateCue (cue : Cue) (state: State) =
    if Map.containsKey cue.Id state.Cues then
      { state with Cues = Map.add cue.Id cue state.Cues }
    else
      state

  // ** removeCue

  static member removeCue (cue : Cue) (state: State) =
    { state with Cues = Map.remove cue.Id state.Cues }

  //  __  __                _
  // |  \/  | ___ _ __ ___ | |__   ___ _ __
  // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
  // | |  | |  __/ | | | | | |_) |  __/ |
  // |_|  |_|\___|_| |_| |_|_.__/ \___|_|

  // ** addMember

  static member addMember (mem: RaftMember) (state: State) =
    { state with Project = Project.addMember mem state.Project }

  // ** updateMember

  static member updateMember (mem: RaftMember) (state: State) =
    { state with Project = Project.updateMember mem state.Project }

  // ** removeMember

  static member removeMember (mem: RaftMember) (state: State) =
    { state with Project = Project.removeMember mem.Id state.Project }

  //   ____ _ _            _
  //  / ___| (_) ___ _ __ | |_
  // | |   | | |/ _ \ '_ \| __|
  // | |___| | |  __/ | | | |_
  //  \____|_|_|\___|_| |_|\__|

  // ** addClient

  static member addClient (client: IrisClient) (state: State) =
    if Map.containsKey client.Id state.Clients then
      state
    else
      { state with Clients = Map.add client.Id client state.Clients }

  // ** updateClient

  static member updateClient (client: IrisClient) (state: State) =
    if Map.containsKey client.Id state.Clients then
      { state with Clients = Map.add client.Id client state.Clients }
    else
      state

  // ** removeClient

  static member removeClient (client: IrisClient) (state: State) =
    { state with Clients = Map.remove client.Id state.Clients }

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/

  // ** updateMachine

  static member updateMachine (machine: IrisMachine) (state: State) =
    { state with Project = Project.updateMachine machine state.Project }

  // ** updateConfig

  static member updateConfig (config: IrisConfig) (state: State) =
    { state with Project = Project.updateConfig config state.Project }

  // ** updateProject

  static member updateProject (project: IrisProject) (state: State) =
    { state with Project = project }

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

    let clients =
      Map.toArray self.Clients
      |> Array.map (snd >> Binary.toOffset builder)

    let clientsoffset = StateFB.CreateClientsVector(builder, clients)

    StateFB.StartStateFB(builder)
    StateFB.AddProject(builder, project)
    StateFB.AddPatches(builder, patchesoffset)
    StateFB.AddCues(builder, cuesoffset)
    StateFB.AddCueLists(builder, cuelistsoffset)
    StateFB.AddSessions(builder, sessionsoffset)
    StateFB.AddClients(builder, clientsoffset)
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

      // CLIENTS

      let! clients =
        let arr = Array.zeroCreate fb.ClientsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, IrisClient>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! client = fb.Clients(i) |> IrisClient.FromFB
            #else
            let! client =
              let value = fb.Clients(i)
              if value.HasValue then
                value.Value
                |> IrisClient.FromFB
              else
                "Could not parse empty Client payload"
                |> Error.asParseError "Client.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add client.Id client map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      return { Project  = project
               Patches  = patches
               Cues     = cues
               CueLists = cuelists
               Users    = users
               Sessions = sessions
               Clients  = clients }
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
      self.Notify(ev)                    // 2) notify all
      history.Append({ Event = ev        // 3) store this action and new state
                       State = state })  // 4) append to undo history

    match ev with
    | Command (AppCommand.Redo)  -> self.Redo()
    | Command (AppCommand.Undo)  -> self.Undo()
    | Command (AppCommand.Reset) -> ()   // do nothing for now

    | AddCue                cue -> State.addCue        cue     state |> andRender
    | UpdateCue             cue -> State.updateCue     cue     state |> andRender
    | RemoveCue             cue -> State.removeCue     cue     state |> andRender

    | AddCueList        cuelist -> State.addCueList    cuelist state |> andRender
    | UpdateCueList     cuelist -> State.updateCueList cuelist state |> andRender
    | RemoveCueList     cuelist -> State.removeCueList cuelist state |> andRender

    | AddPatch            patch -> State.addPatch      patch   state |> andRender
    | UpdatePatch         patch -> State.updatePatch   patch   state |> andRender
    | RemovePatch         patch -> State.removePatch   patch   state |> andRender

    | AddPin                pin -> State.addPin        pin     state |> andRender
    | UpdatePin             pin -> State.updatePin     pin     state |> andRender
    | RemovePin             pin -> State.removePin     pin     state |> andRender

    | AddMember             mem -> State.addMember     mem     state |> andRender
    | UpdateMember          mem -> State.updateMember  mem     state |> andRender
    | RemoveMember          mem -> State.removeMember  mem     state |> andRender

    | AddClient          client -> State.addClient     client  state |> andRender
    | UpdateClient       client -> State.updateClient  client  state |> andRender
    | RemoveClient       client -> State.removeClient  client  state |> andRender

    | AddSession        session -> State.addSession    session state |> andRender
    | UpdateSession     session -> State.updateSession session state |> andRender
    | RemoveSession     session -> State.removeSession session state |> andRender

    | AddUser              user -> State.addUser       user    state |> andRender
    | UpdateUser           user -> State.updateUser    user    state |> andRender
    | RemoveUser           user -> State.removeUser    user    state |> andRender

    | UpdateProject     project -> State.updateProject project state |> andRender

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
  // Project
  | UpdateProject of IrisProject

  // Member
  | AddMember     of RaftMember
  | UpdateMember  of RaftMember
  | RemoveMember  of RaftMember

  // Client
  | AddClient     of IrisClient
  | UpdateClient  of IrisClient
  | RemoveClient  of IrisClient

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
    // Project
    | UpdateProject project -> sprintf "UpdateProject %s" project.Name

    // Member
    | AddMember    mem      -> sprintf "AddMember %s"    (string mem)
    | UpdateMember mem      -> sprintf "UpdateMember %s" (string mem)
    | RemoveMember mem      -> sprintf "RemoveMember %s" (string mem)

    // Client
    | AddClient    client  -> sprintf "AddClient %s"    (string client)
    | UpdateClient client  -> sprintf "UpdateClient %s" (string client)
    | RemoveClient client  -> sprintf "RemoveClient %s" (string client)

    // PATCH
    | AddPatch    patch     -> sprintf "AddPatch %s"    (string patch)
    | UpdatePatch patch     -> sprintf "UpdatePatch %s" (string patch)
    | RemovePatch patch     -> sprintf "RemovePatch %s" (string patch)

    // PIN
    | AddPin    pin         -> sprintf "AddPin %s"    (string pin)
    | UpdatePin pin         -> sprintf "UpdatePin %s" (string pin)
    | RemovePin pin         -> sprintf "RemovePin %s" (string pin)

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
  static member FromFB (fb: RaftApiActionFB) =
    match fb.PayloadType with
    | x when x = RaftPayloadFB.ProjectFB ->
      let project = fb.ProjectFB |> IrisProject.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdateProject project
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.RaftMemberFB ->
      let mem = fb.RaftMemberFB |> RaftMember.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddMember mem
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdateMember mem
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemoveMember mem
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.IrisClientFB ->
      let client = fb.IrisClientFB |> IrisClient.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddClient client
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdateClient client
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemoveClient client
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.PatchFB ->
      let patch = fb.PatchFB |> Patch.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddPatch patch
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdatePatch patch
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemovePatch patch
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.PinFB ->
      let pin = fb.PinFB |> Pin.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddPin pin
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdatePin pin
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemovePin pin
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.CueFB ->
      let cue = fb.CueFB |> Cue.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddCue cue
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdateCue cue
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemoveCue cue
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.CueListFB ->
      let cuelist = fb.CueListFB |> CueList.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddCueList cuelist
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdateCueList cuelist
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemoveCueList cuelist
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.UserFB ->
      let user = fb.UserFB |> User.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddUser user
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdateUser user
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemoveUser user
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.IrisClientFB ->
      let client = fb.IrisClientFB |> IrisClient.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddClient client
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdateClient client
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemoveClient client
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.SessionFB ->
      let session = fb.SessionFB |> Session.FromFB
      match fb.Action with
      | x when x = RaftActionTypeFB.AddFB ->
        Either.map AddSession session
      | x when x = RaftActionTypeFB.UpdateFB ->
        Either.map UpdateSession session
      | x when x = RaftActionTypeFB.RemoveFB ->
        Either.map RemoveSession session
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = RaftPayloadFB.StateFB && fb.Action = RaftActionTypeFB.DataSnapshotFB ->
      fb.StateFB
      |> State.FromFB
      |> Either.map DataSnapshot

    | x when x = RaftPayloadFB.LogEventFB ->
      fb.LogEventFB
      |> LogEvent.FromFB
      |> Either.map LogMsg

    | x when x = RaftPayloadFB.StringFB ->
      match fb.Action with
      | x when x = RaftActionTypeFB.SetLogLevelFB ->
        fb.StringFB.Value
        |> LogLevel.TryParse
        |> Either.map SetLogLevel
      | x ->
        sprintf "Could not parse unknown RaftActionTypeFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail
    | _ ->
      fb.Action
      |> AppCommand.FromFB
      |> Either.map Command

#else

  // ** FromFB (.NET)

  static member FromFB (fb: RaftApiActionFB) =
    match fb.PayloadType with

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/

    | RaftPayloadFB.ProjectFB ->
      either {
        let! project =
          let projectish = fb.Payload<ProjectFB>()
          if projectish.HasValue then
            projectish.Value
            |> IrisProject.FromFB
          else
            "Could not parse empty project payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | RaftActionTypeFB.UpdateFB -> return (UpdateProject project)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | RaftPayloadFB.CueFB ->
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
        | RaftActionTypeFB.AddFB    -> return (AddCue cue)
        | RaftActionTypeFB.UpdateFB -> return (UpdateCue cue)
        | RaftActionTypeFB.RemoveFB -> return (RemoveCue cue)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //   ____ _ _            _
    //  / ___| (_) ___ _ __ | |_
    // | |   | | |/ _ \ '_ \| __|
    // | |___| | |  __/ | | | |_
    //  \____|_|_|\___|_| |_|\__|

    | RaftPayloadFB.IrisClientFB ->
      either {
        let! client =
          let clientish = fb.Payload<IrisClientFB>()
          if clientish.HasValue then
            clientish.Value
            |> IrisClient.FromFB
          else
            "Could not parse empty client payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | RaftActionTypeFB.AddFB    -> return (AddClient client)
        | RaftActionTypeFB.UpdateFB -> return (UpdateClient client)
        | RaftActionTypeFB.RemoveFB -> return (RemoveClient client)
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

    | RaftPayloadFB.CueListFB ->
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
        | RaftActionTypeFB.AddFB    -> return (AddCueList    cuelist)
        | RaftActionTypeFB.UpdateFB -> return (UpdateCueList cuelist)
        | RaftActionTypeFB.RemoveFB -> return (RemoveCueList cuelist)
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

    | RaftPayloadFB.PatchFB ->
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
        | RaftActionTypeFB.AddFB    -> return (AddPatch    patch)
        | RaftActionTypeFB.UpdateFB -> return (UpdatePatch patch)
        | RaftActionTypeFB.RemoveFB -> return (RemovePatch patch)
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

    | RaftPayloadFB.PinFB ->
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
        | RaftActionTypeFB.AddFB    -> return (AddPin    pin)
        | RaftActionTypeFB.UpdateFB -> return (UpdatePin pin)
        | RaftActionTypeFB.RemoveFB -> return (RemovePin pin)
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

    | RaftPayloadFB.RaftMemberFB ->
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
        | RaftActionTypeFB.AddFB    -> return (AddMember    mem)
        | RaftActionTypeFB.UpdateFB -> return (UpdateMember mem)
        | RaftActionTypeFB.RemoveFB -> return (RemoveMember mem)
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

    | RaftPayloadFB.UserFB ->
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
        | RaftActionTypeFB.AddFB    -> return (AddUser    user)
        | RaftActionTypeFB.UpdateFB -> return (UpdateUser user)
        | RaftActionTypeFB.RemoveFB -> return (RemoveUser user)
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

    | RaftPayloadFB.SessionFB ->
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
        | RaftActionTypeFB.AddFB    -> return (AddSession    session)
        | RaftActionTypeFB.UpdateFB -> return (UpdateSession session)
        | RaftActionTypeFB.RemoveFB -> return (RemoveSession session)
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

    | RaftPayloadFB.LogEventFB ->
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

    | RaftPayloadFB.StateFB ->
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

    | RaftPayloadFB.StringFB ->
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

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<RaftApiActionFB> =
    match self with
    | UpdateProject project ->
      let offset = project.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.ProjectFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, offset)
#else
      RaftApiActionFB.AddPayload(builder, offset.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | AddMember       mem ->
      let mem = mem.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.AddFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, mem)
#else
      RaftApiActionFB.AddPayload(builder, mem.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | UpdateMember    mem ->
      let mem = mem.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, mem)
#else
      RaftApiActionFB.AddPayload(builder, mem.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | RemoveMember    mem ->
      let mem = mem.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.RemoveFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, mem)
#else
      RaftApiActionFB.AddPayload(builder, mem.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | AddClient       client ->
      let client = client.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.AddFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.IrisClientFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, client)
#else
      RaftApiActionFB.AddPayload(builder, client.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | UpdateClient    client ->
      let client = client.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.IrisClientFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, client)
#else
      RaftApiActionFB.AddPayload(builder, client.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | RemoveClient    client ->
      let client = client.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.RemoveFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.IrisClientFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, client)
#else
      RaftApiActionFB.AddPayload(builder, client.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | AddPatch       patch ->
      let patch = patch.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.AddFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.PatchFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, patch)
#else
      RaftApiActionFB.AddPayload(builder, patch.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | UpdatePatch    patch ->
      let patch = patch.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.PatchFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, patch)
#else
      RaftApiActionFB.AddPayload(builder, patch.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | RemovePatch    patch ->
      let patch = patch.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.RemoveFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.PatchFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, patch)
#else
      RaftApiActionFB.AddPayload(builder, patch.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | AddPin       pin ->
      let pin = pin.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.AddFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.PinFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, pin)
#else
      RaftApiActionFB.AddPayload(builder, pin.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | UpdatePin    pin ->
      let pin = pin.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.PinFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, pin)
#else
      RaftApiActionFB.AddPayload(builder, pin.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | RemovePin    pin ->
      let pin = pin.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.RemoveFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.PinFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, pin)
#else
      RaftApiActionFB.AddPayload(builder, pin.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | AddCue cue ->
      let cue = cue.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.AddFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.CueFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, cue)
#else
      RaftApiActionFB.AddPayload(builder, cue.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | UpdateCue cue ->
      let cue = cue.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.CueFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, cue)
#else
      RaftApiActionFB.AddPayload(builder, cue.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | RemoveCue cue ->
      let cue = cue.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.RemoveFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.CueFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, cue)
#else
      RaftApiActionFB.AddPayload(builder, cue.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | AddCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.AddFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.CueListFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, cuelist)
#else
      RaftApiActionFB.AddPayload(builder, cuelist.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | UpdateCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.CueListFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, cuelist)
#else
      RaftApiActionFB.AddPayload(builder, cuelist.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | RemoveCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.RemoveFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.CueListFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, cuelist)
#else
      RaftApiActionFB.AddPayload(builder, cuelist.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | AddUser user ->
      let user = user.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.AddFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.UserFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, user)
#else
      RaftApiActionFB.AddPayload(builder, user.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | UpdateUser user ->
      let user = user.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.UserFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, user)
#else
      RaftApiActionFB.AddPayload(builder, user.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | RemoveUser user ->
      let user = user.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.RemoveFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.UserFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, user)
#else
      RaftApiActionFB.AddPayload(builder, user.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | AddSession session ->
      let session = session.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.AddFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.SessionFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, session)
#else
      RaftApiActionFB.AddPayload(builder, session.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | UpdateSession session ->
      let session = session.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.UpdateFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.SessionFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, session)
#else
      RaftApiActionFB.AddPayload(builder, session.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | RemoveSession session ->
      let session = session.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.RemoveFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.SessionFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, session)
#else
      RaftApiActionFB.AddPayload(builder, session.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | Command appcommand ->
      let cmd = appcommand.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, cmd)
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | DataSnapshot state ->
      let offset = state.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.DataSnapshotFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.StateFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, offset)
#else
      RaftApiActionFB.AddPayload(builder, offset.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | LogMsg log ->
      let offset = log.ToOffset(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.LogEventFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.LogEventFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, offset)
#else
      RaftApiActionFB.AddPayload(builder, offset.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

    | SetLogLevel level ->
      let str = builder.CreateString (string level)
      StringFB.StartStringFB(builder)
      StringFB.AddValue(builder,str)
      let offset = StringFB.EndStringFB(builder)
      RaftApiActionFB.StartRaftApiActionFB(builder)
      RaftApiActionFB.AddAction(builder, RaftActionTypeFB.SetLogLevelFB)
      RaftApiActionFB.AddPayloadType(builder, RaftPayloadFB.StringFB)
#if FABLE_COMPILER
      RaftApiActionFB.AddPayload(builder, offset)
#else
      RaftApiActionFB.AddPayload(builder, offset.Value)
#endif
      RaftApiActionFB.EndRaftApiActionFB(builder)

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: Binary.Buffer) : Either<IrisError,StateMachine> =
    Binary.createBuffer bytes
    |> RaftApiActionFB.GetRootAsRaftApiActionFB
    |> StateMachine.FromFB
