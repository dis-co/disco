namespace rec Iris.Core

// * Imports

//  ___                            _
// |_ _|_ __ ___  _ __   ___  _ __| |_ ___
//  | || '_ ` _ \| '_ \ / _ \| '__| __/ __|
//  | || | | | | | |_) | (_) | |  | |_\__ \
// |___|_| |_| |_| .__/ \___/|_|   \__|___/
//               |_|

open Iris.Raft

#if FABLE_COMPILER

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

  static member FromFB (fb: StateMachineActionFB) =
#if FABLE_COMPILER
    match fb with
    | x when x = StateMachineActionFB.UndoFB        -> Right Undo
    | x when x = StateMachineActionFB.RedoFB        -> Right Redo
    | x when x = StateMachineActionFB.ResetFB       -> Right Reset
    | x when x = StateMachineActionFB.SaveProjectFB -> Right SaveProject
    | x ->
      sprintf "Could not parse %A as AppCommand" x
      |> Error.asParseError "AppCommand.FromFB"
      |> Either.fail
#else
    match fb with
    | StateMachineActionFB.UndoFB        -> Right Undo
    | StateMachineActionFB.RedoFB        -> Right Redo
    | StateMachineActionFB.ResetFB       -> Right Reset
    | StateMachineActionFB.SaveProjectFB -> Right SaveProject
    | x ->
      sprintf "Could not parse %A as AppCommand" x
      |> Error.asParseError "AppCommand.FromFB"
      |> Either.fail
#endif

  // ** ToOffset

  member self.ToOffset(_: FlatBufferBuilder) : StateMachineActionFB =
    match self with
    | Undo        -> StateMachineActionFB.UndoFB
    | Redo        -> StateMachineActionFB.RedoFB
    | Reset       -> StateMachineActionFB.ResetFB
    | SaveProject -> StateMachineActionFB.SaveProjectFB

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
  { Project:            IrisProject
    PinGroups:          Map<Id,PinGroup>
    Cues:               Map<Id,Cue>
    CueLists:           Map<Id,CueList>
    Sessions:           Map<Id,Session>
    Users:              Map<Id,User>
    Clients:            Map<Id,IrisClient>
    CuePlayers:         Map<Id,CuePlayer>
    DiscoveredServices: Map<Id,DiscoveredService> }

  // ** Empty

  static member Empty
    with get () =
      { Project  = IrisProject.Empty
        PinGroups  = Map.empty
        Cues       = Map.empty
        CueLists   = Map.empty
        Sessions   = Map.empty
        Users      = Map.empty
        Clients    = Map.empty
        CuePlayers = Map.empty
        DiscoveredServices = Map.empty }

  // ** Load

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load (path: FilePath, machine: IrisMachine) =
    either {
      let inline toMap value = Either.map (Array.map toPair >> Map.ofArray) value

      let! project  = Asset.loadWithMachine path machine
      let! users    = Asset.loadAll project.Path |> toMap
      let! cues     = Asset.loadAll project.Path |> toMap
      let! cuelists = Asset.loadAll project.Path |> toMap
      let! groups   = Asset.loadAll project.Path |> toMap
      let! players  = Asset.loadAll project.Path |> toMap

      return
        { Project            = project
          Users              = users
          Cues               = cues
          CueLists           = cuelists
          PinGroups          = groups
          CuePlayers         = players
          Sessions           = Map.empty
          Clients            = Map.empty
          DiscoveredServices = Map.empty }
    }

  #endif

  // ** Save

  #if !FABLE_COMPILER && !IRIS_NODES

  member state.Save (basePath: FilePath) =
    either {
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.PinGroups
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.Cues
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.CueLists
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.Users
      do! Map.fold (Asset.saveMap basePath) (Right ()) state.CuePlayers
      do! Asset.save basePath state.Project
    }

  #endif

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<StateFB> =
    let project = Binary.toOffset builder self.Project

    let groups =
      Map.toArray self.PinGroups
      |> Array.map (snd >> Binary.toOffset builder)

    let groupsoffset = StateFB.CreatePinGroupsVector(builder, groups)

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

    let players =
      Map.toArray self.CuePlayers
      |> Array.map (snd >> Binary.toOffset builder)

    let playersoffset = StateFB.CreateCuePlayersVector(builder, players)

    let services =
      Map.toArray self.DiscoveredServices
      |> Array.map (snd >> Binary.toOffset builder)

    let servicesoffset = StateFB.CreateDiscoveredServicesVector(builder, services)

    StateFB.StartStateFB(builder)
    StateFB.AddProject(builder, project)
    StateFB.AddPinGroups(builder, groupsoffset)
    StateFB.AddCues(builder, cuesoffset)
    StateFB.AddCueLists(builder, cuelistsoffset)
    StateFB.AddSessions(builder, sessionsoffset)
    StateFB.AddClients(builder, clientsoffset)
    StateFB.AddUsers(builder, usersoffset)
    StateFB.AddCuePlayers(builder, playersoffset)
    StateFB.AddDiscoveredServices(builder, servicesoffset)
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

      // GROUPS

      let! groups =
        let arr = Array.zeroCreate fb.PinGroupsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, PinGroup>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! group = fb.PinGroups(i) |> PinGroup.FromFB
            #else
            let! group =
              let value = fb.PinGroups(i)
              if value.HasValue then
                value.Value
                |> PinGroup.FromFB
              else
                "Could not parse empty group payload"
                |> Error.asParseError "State.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add group.Id group map)
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

      // PLAYERS

      let! players =
        let arr = Array.zeroCreate fb.CuePlayersLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, CuePlayer>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! player = fb.CuePlayers(i) |> CuePlayer.FromFB
            #else
            let! player =
              let value = fb.CuePlayers(i)
              if value.HasValue then
                value.Value
                |> CuePlayer.FromFB
              else
                "Could not parse empty CuePlayer payload"
                |> Error.asParseError "CuePlayer.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add player.Id player map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      // DISCOVERED SERVICES

      let! discoveredServices =
        let arr = Array.zeroCreate fb.DiscoveredServicesLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id, DiscoveredService>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! service = fb.DiscoveredServices(i) |> DiscoveredService.FromFB
            #else
            let! service =
              let value = fb.DiscoveredServices(i)
              if value.HasValue then
                value.Value
                |> DiscoveredService.FromFB
              else
                "Could not parse empty DiscoveredService payload"
                |> Error.asParseError "DiscoveredService.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add service.Id service map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      return { Project            = project
               PinGroups          = groups
               Cues               = cues
               CueLists           = cuelists
               Users              = users
               Sessions           = sessions
               Clients            = clients
               CuePlayers         = players
               DiscoveredServices = discoveredServices }
    }

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,State> =
    Binary.createBuffer bytes
    |> StateFB.GetRootAsStateFB
    |> State.FromFB

// * State module

module State =

  // ** addCuePlayer

  let addCuePlayer (player: CuePlayer) (state: State) =
    if Map.containsKey player.Id state.CuePlayers then
      state
    else
      let players = Map.add player.Id player state.CuePlayers
      { state with CuePlayers = players }

  // ** updateCuePlayer

  let updateCuePlayer (player: CuePlayer) (state: State) =
    if Map.containsKey player.Id state.CuePlayers then
      let players = Map.add player.Id player state.CuePlayers
      { state with CuePlayers = players }
    else
      state

  // ** removeCuePlayer

  let removeCuePlayer (player: CuePlayer) (state: State) =
    { state with CuePlayers = Map.remove player.Id state.CuePlayers }

  // ** addUser

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  let addUser (user: User) (state: State) =
    if Map.containsKey user.Id state.Users then
      state
    else
      let users = Map.add user.Id user state.Users
      { state with Users = users }

  // ** updateUser

  let updateUser (user: User) (state: State) =
    if Map.containsKey user.Id state.Users then
      let users = Map.add user.Id user state.Users
      { state with Users = users }
    else
      state

  // ** removeUser

  let removeUser (user: User) (state: State) =
    { state with Users = Map.remove user.Id state.Users }

  // ** addOrUpdateService

  let addOrUpdateService (service: DiscoveredService) (state: State) =
    { state with DiscoveredServices = Map.add service.Id service state.DiscoveredServices }

  // ** removeService

  let removeService (service: DiscoveredService) (state: State) =
    { state with DiscoveredServices = Map.remove service.Id state.DiscoveredServices }

  // ** addSession

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  let addSession (session: Session) (state: State) =
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        state.Sessions
      else
        Map.add session.Id session state.Sessions
    { state with Sessions = sessions }

  // ** updateSession

  let updateSession (session: Session) (state: State) =
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        Map.add session.Id session state.Sessions
      else
        state.Sessions
    { state with Sessions = sessions }

  // ** removeSession

  let removeSession (session: Session) (state: State) =
    { state with Sessions = Map.remove session.Id state.Sessions }

  // ** addPinGroup

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  let addPinGroup (group : PinGroup) (state: State) =
    if Map.containsKey group.Id state.PinGroups then
      state
    else
      { state with PinGroups = Map.add group.Id group state.PinGroups }

  // ** updatePinGroup

  let updatePinGroup (group : PinGroup) (state: State) =
    if Map.containsKey group.Id state.PinGroups then
      { state with PinGroups = Map.add group.Id group state.PinGroups }
    else
      state

  // ** removePinGroup

  let removePinGroup (group : PinGroup) (state: State) =
    { state with PinGroups = Map.remove group.Id state.PinGroups }


  // ** addPin

  //  ____  _
  // |  _ \(_)_ __
  // | |_) | | '_ \
  // |  __/| | | | |
  // |_|   |_|_| |_|

  let addPin (pin: Pin) (state: State) =
    if Map.containsKey pin.PinGroup state.PinGroups then
      let update _ (group: PinGroup) =
        if group.Id = pin.PinGroup then
          PinGroup.addPin pin group
        else
          group
      { state with PinGroups = Map.map update state.PinGroups }
    else
      state

  // ** updatePin

  let updatePin (pin : Pin) (state: State) =
    let mapper (_: Id) (group : PinGroup) =
      if group.Id = pin.PinGroup then
        PinGroup.updatePin pin group
      else
        group
    { state with PinGroups = Map.map mapper state.PinGroups }

  // ** updateSlices

  let updateSlices (slices: Slices) (state: State) =
    { state with
        PinGroups = Map.map
                      (fun _ group -> PinGroup.updateSlices slices group)
                      state.PinGroups
        CuePlayers = Map.map
                      (fun _ player -> CuePlayer.updateSlices slices player)
                      state.CuePlayers }
  // ** removePin

  let removePin (pin : Pin) (state: State) =
    let updater _ (group : PinGroup) =
      if pin.PinGroup = group.Id
      then PinGroup.removePin pin group
      else group
    { state with PinGroups = Map.map updater state.PinGroups }

  // ** tryFindPin

  let tryFindPin (id: Id) (state: State) =
    Map.fold
      (fun (m: Pin option) _ (group: PinGroup) ->
        match m with
        | Some _ -> m
        | _ -> Map.tryFind id group.Pins)
      None
      state.PinGroups

  // ** tryFindPinGroup

  let tryFindPinGroup (id: Id) (state: State) =
    Map.tryFind id state.PinGroups

  // ** findPinGroupBy

  let findPinGroupBy (pred: PinGroup -> bool) (state: State) =
    Map.fold
      (fun m _ (grp: PinGroup) ->
        match m with
        | None when pred grp -> Some grp
        | None -> None
        | Some _ -> m)
      None
      state.PinGroups


  // ** addCueList

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_ ___
  // | |  | | | |/ _ \ |   | / __| __/ __|
  // | |__| |_| |  __/ |___| \__ \ |_\__ \
  //  \____\__,_|\___|_____|_|___/\__|___/

  let addCueList (cuelist : CueList) (state: State) =
    if Map.containsKey cuelist.Id state.CueLists then
      state
    else
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }

  // ** updateCueList

  let updateCueList (cuelist : CueList) (state: State) =
    if Map.containsKey cuelist.Id state.CueLists then
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }
    else
      state

  // ** removeCueList

  let removeCueList (cuelist : CueList) (state: State) =
    { state with CueLists = Map.remove cuelist.Id state.CueLists }

  // ** AddCue

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let addCue (cue : Cue) (state: State) =
    if Map.containsKey cue.Id state.Cues then
      state
    else
      { state with Cues = Map.add cue.Id cue state.Cues }

  // ** updateCue

  let updateCue (cue : Cue) (state: State) =
    if Map.containsKey cue.Id state.Cues then
      { state with Cues = Map.add cue.Id cue state.Cues }
    else
      state

  // ** removeCue

  let removeCue (cue : Cue) (state: State) =
    { state with Cues = Map.remove cue.Id state.Cues }

  //  __  __                _
  // |  \/  | ___ _ __ ___ | |__   ___ _ __
  // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
  // | |  | |  __/ | | | | | |_) |  __/ |
  // |_|  |_|\___|_| |_| |_|_.__/ \___|_|

  // ** addMember

  let addMember (mem: RaftMember) (state: State) =
    { state with Project = Project.addMember mem state.Project }

  // ** updateMember

  let updateMember (mem: RaftMember) (state: State) =
    { state with Project = Project.updateMember mem state.Project }

  // ** removeMember

  let removeMember (mem: RaftMember) (state: State) =
    { state with Project = Project.removeMember mem.Id state.Project }

  //   ____ _ _            _
  //  / ___| (_) ___ _ __ | |_
  // | |   | | |/ _ \ '_ \| __|
  // | |___| | |  __/ | | | |_
  //  \____|_|_|\___|_| |_|\__|

  // ** addClient

  let addClient (client: IrisClient) (state: State) =
    if Map.containsKey client.Id state.Clients then
      state
    else
      { state with Clients = Map.add client.Id client state.Clients }

  // ** updateClient

  let updateClient (client: IrisClient) (state: State) =
    if Map.containsKey client.Id state.Clients then
      { state with Clients = Map.add client.Id client state.Clients }
    else
      state

  // ** removeClient

  let removeClient (client: IrisClient) (state: State) =
    { state with Clients = Map.remove client.Id state.Clients }

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/

  // ** updateMachine

  let updateMachine (machine: IrisMachine) (state: State) =
    { state with Project = Project.updateMachine machine state.Project }

  // ** updateConfig

  let updateConfig (config: IrisConfig) (state: State) =
    { state with Project = Project.updateConfig config state.Project }

  // ** updateProject

  let updateProject (project: IrisProject) (state: State) =
    { state with Project = project }

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
type [<NoComparison>] StoreAction =
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
type History (action: StoreAction) =
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
type Store(state : State)=

  let mutable state = state

  let mutable history = History {
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
  /// Disgroup an action (StateMachine command) to be executed against the current version of the
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
    | Command (AppCommand.Redo)     -> self.Redo()
    | Command (AppCommand.Undo)     -> self.Undo()
    | Command (AppCommand.Reset)    -> ()   // do nothing for now

    | AddCue            cue         -> State.addCue         cue     state |> andRender
    | UpdateCue         cue         -> State.updateCue      cue     state |> andRender
    | RemoveCue         cue         -> State.removeCue      cue     state |> andRender

    | AddCueList    cuelist         -> State.addCueList     cuelist state |> andRender
    | UpdateCueList cuelist         -> State.updateCueList  cuelist state |> andRender
    | RemoveCueList cuelist         -> State.removeCueList  cuelist state |> andRender

    | AddCuePlayer    player        -> State.addCuePlayer    player state |> andRender
    | UpdateCuePlayer player        -> State.updateCuePlayer player state |> andRender
    | RemoveCuePlayer player        -> State.removeCuePlayer player state |> andRender

    | AddPinGroup     group         -> State.addPinGroup    group   state |> andRender
    | UpdatePinGroup  group         -> State.updatePinGroup group   state |> andRender
    | RemovePinGroup  group         -> State.removePinGroup group   state |> andRender

    | AddPin            pin         -> State.addPin         pin     state |> andRender
    | UpdatePin         pin         -> State.updatePin      pin     state |> andRender
    | RemovePin         pin         -> State.removePin      pin     state |> andRender
    | UpdateSlices   slices         -> State.updateSlices   slices  state |> andRender

    | AddMember         mem         -> State.addMember      mem     state |> andRender
    | UpdateMember      mem         -> State.updateMember   mem     state |> andRender
    | RemoveMember      mem         -> State.removeMember   mem     state |> andRender

    | AddClient      client         -> State.addClient      client  state |> andRender
    | UpdateClient   client         -> State.updateClient   client  state |> andRender
    | RemoveClient   client         -> State.removeClient   client  state |> andRender

    | AddSession    session         -> State.addSession     session state |> andRender
    | UpdateSession session         -> State.updateSession  session state |> andRender
    | RemoveSession session         -> State.removeSession  session state |> andRender

    | AddUser          user         -> State.addUser        user    state |> andRender
    | UpdateUser       user         -> State.updateUser     user    state |> andRender
    | RemoveUser       user         -> State.removeUser     user    state |> andRender

    | UpdateProject project         -> State.updateProject  project state |> andRender
    | UnloadProject                 -> self.Notify(ev) // This event doesn't actually modify the state

    // It may happen that a service didn't make it into the state and an update service
    // event is received. For those cases just add/update the service into the state.
    | AddDiscoveredService    service
    | UpdateDiscoveredService service -> State.addOrUpdateService    service state |> andRender
    | RemoveDiscoveredService service -> State.removeService service state |> andRender

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
type Listener = Store -> StateMachine -> unit

// * StateMachine

//  ____  _        _       __  __            _     _
// / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
// \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
// |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

type StateMachine =
  // Project
  | UpdateProject         of IrisProject
  | UnloadProject

  // Member
  | AddMember             of RaftMember
  | UpdateMember          of RaftMember
  | RemoveMember          of RaftMember

  // Client
  | AddClient             of IrisClient
  | UpdateClient          of IrisClient
  | RemoveClient          of IrisClient

  // GROUP
  | AddPinGroup           of PinGroup
  | UpdatePinGroup        of PinGroup
  | RemovePinGroup        of PinGroup

  // PIN
  | AddPin                of Pin
  | UpdatePin             of Pin
  | RemovePin             of Pin
  | UpdateSlices          of Slices

  // CUE
  | AddCue                of Cue
  | UpdateCue             of Cue
  | RemoveCue             of Cue
  | CallCue               of Cue

  // CUELIST
  | AddCueList            of CueList
  | UpdateCueList         of CueList
  | RemoveCueList         of CueList

  // CUEPLAYER
  | AddCuePlayer          of CuePlayer
  | UpdateCuePlayer       of CuePlayer
  | RemoveCuePlayer       of CuePlayer

  // User
  | AddUser               of User
  | UpdateUser            of User
  | RemoveUser            of User

  // Session
  | AddSession            of Session
  | UpdateSession         of Session
  | RemoveSession         of Session

  // Discovery
  | AddDiscoveredService    of DiscoveredService
  | UpdateDiscoveredService of DiscoveredService
  | RemoveDiscoveredService of DiscoveredService

  | UpdateClock           of uint32

  | Command               of AppCommand

  | DataSnapshot          of State

  | SetLogLevel           of LogLevel

  | LogMsg                of LogEvent

  // ** ToString

  override self.ToString() : string =
    match self with
    // Project
    | UpdateProject           _ -> "UpdateProject "
    | UnloadProject             -> "UnloadProject"

    // Member
    | AddMember               _ -> "AddMember"
    | UpdateMember            _ -> "UpdateMember"
    | RemoveMember            _ -> "RemoveMember"

    // Client
    | AddClient               _ -> "AddClient"
    | UpdateClient            _ -> "UpdateClient"
    | RemoveClient            _ -> "RemoveClient"

    // GROUP
    | AddPinGroup             _ -> "AddPinGroup"
    | UpdatePinGroup          _ -> "UpdatePinGroup"
    | RemovePinGroup          _ -> "RemovePinGroup"

    // PIN
    | AddPin                  _ -> "AddPin"
    | UpdatePin               _ -> "UpdatePin"
    | RemovePin               _ -> "RemovePin"
    | UpdateSlices            _ -> "UpdateSlices"

    // CUE
    | AddCue                  _ -> "AddCue"
    | UpdateCue               _ -> "UpdateCue"
    | RemoveCue               _ -> "RemoveCue"
    | CallCue                 _ -> "CallCue"

    // CUELIST
    | AddCueList              _ -> "AddCueList"
    | UpdateCueList           _ -> "UpdateCueList"
    | RemoveCueList           _ -> "RemoveCueList"

    // CUEPLAYER
    | AddCuePlayer            _ -> "AddCuePlayer"
    | UpdateCuePlayer         _ -> "UpdateCuePlayer"
    | RemoveCuePlayer         _ -> "RemoveCuePlayer"

    // User
    | AddUser                 _ -> "AddUser"
    | UpdateUser              _ -> "UpdateUser"
    | RemoveUser              _ -> "RemoveUser"

    // Session
    | AddSession              _ -> "AddSession"
    | UpdateSession           _ -> "UpdateSession"
    | RemoveSession           _ -> "RemoveSession"

    // Discovery
    | AddDiscoveredService    _ -> "AddDiscoveredService"
    | UpdateDiscoveredService _ -> "UpdateDiscoveredService"
    | RemoveDiscoveredService _ -> "RemoveDiscoveredService"

    | Command                 _ -> "Command"
    | DataSnapshot            _ -> "DataSnapshot"
    | SetLogLevel             _ -> "SetLogLevel"
    | LogMsg                  _ -> "LogMsg"

    | UpdateClock             _ -> "UpdateClock"

  // ** FromFB (JavaScript)

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  #if FABLE_COMPILER

  static member FromFB (fb: StateMachineFB) =
    match fb.PayloadType with
    | x when x = StateMachinePayloadFB.ProjectFB ->
      match fb.Action with
      | x when x = StateMachineActionFB.UpdateFB ->
        let project = fb.ProjectFB |> IrisProject.FromFB
        Either.map UpdateProject project
      | x when x = StateMachineActionFB.RemoveFB ->
        Right UnloadProject
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.RaftMemberFB ->
      let mem = fb.RaftMemberFB |> RaftMember.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddMember mem
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateMember mem
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveMember mem
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.IrisClientFB ->
      let client = fb.IrisClientFB |> IrisClient.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddClient client
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateClient client
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveClient client
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.PinGroupFB ->
      let group = fb.PinGroupFB |> PinGroup.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddPinGroup group
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdatePinGroup group
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemovePinGroup group
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.PinFB ->
      let pin = fb.PinFB |> Pin.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddPin pin
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdatePin pin
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemovePin pin
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.SlicesFB ->

      let slices = SlicesFB.Create() |> fb.Payload |> Slices.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateSlices slices
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.CueFB ->
      let cue = fb.CueFB |> Cue.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddCue cue
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateCue cue
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveCue cue
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.CueListFB ->
      let cuelist = fb.CueListFB |> CueList.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddCueList cuelist
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateCueList cuelist
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveCueList cuelist
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.CuePlayerFB ->
      let cuelist = fb.CuePlayerFB |> CuePlayer.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddCuePlayer cuelist
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateCuePlayer cuelist
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveCuePlayer cuelist
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.UserFB ->
      let user = fb.UserFB |> User.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddUser user
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateUser user
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveUser user
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.IrisClientFB ->
      let client = fb.IrisClientFB |> IrisClient.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddClient client
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateClient client
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveClient client
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.SessionFB ->
      let session = fb.SessionFB |> Session.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddSession session
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateSession session
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveSession session
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.DiscoveredServiceFB ->
      let discoveredService = fb.DiscoveredServiceFB |> DiscoveredService.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddDiscoveredService discoveredService
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateDiscoveredService discoveredService
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemoveDiscoveredService discoveredService
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.StateFB && fb.Action = StateMachineActionFB.DataSnapshotFB ->
      fb.StateFB
      |> State.FromFB
      |> Either.map DataSnapshot

    | x when x = StateMachinePayloadFB.LogEventFB ->
      fb.LogEventFB
      |> LogEvent.FromFB
      |> Either.map LogMsg

    | x when x = StateMachinePayloadFB.StringFB ->
      match fb.Action with
      | x when x = StateMachineActionFB.SetLogLevelFB ->
        fb.StringFB.Value
        |> LogLevel.TryParse
        |> Either.map SetLogLevel
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    | x when x = StateMachinePayloadFB.ClockFB ->
      UpdateClock(fb.ClockFB.Value)
      |> Either.succeed

    | _ ->
      fb.Action
      |> AppCommand.FromFB
      |> Either.map Command

  #else

  // ** FromFB (.NET)

  static member FromFB (fb: StateMachineFB) =
    match fb.PayloadType with

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/

    | StateMachinePayloadFB.ProjectFB ->
      either {
        match fb.Action with
        | StateMachineActionFB.UpdateFB ->
          let projectish = fb.Payload<ProjectFB>()
          return!
            if projectish.HasValue then
              projectish.Value
              |> IrisProject.FromFB
              |> Either.map UpdateProject
            else
              "Could not parse empty project payload"
              |> Error.asParseError "StateMachine.FromFB"
              |> Either.fail
        | StateMachineActionFB.RemoveFB -> return UnloadProject
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

    | StateMachinePayloadFB.CueFB ->
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
        | StateMachineActionFB.AddFB    -> return (AddCue cue)
        | StateMachineActionFB.UpdateFB -> return (UpdateCue cue)
        | StateMachineActionFB.RemoveFB -> return (RemoveCue cue)
        | StateMachineActionFB.CallFB   -> return (CallCue cue)
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

    | StateMachinePayloadFB.IrisClientFB ->
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
        | StateMachineActionFB.AddFB    -> return (AddClient client)
        | StateMachineActionFB.UpdateFB -> return (UpdateClient client)
        | StateMachineActionFB.RemoveFB -> return (RemoveClient client)
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

    | StateMachinePayloadFB.CueListFB ->
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
        | StateMachineActionFB.AddFB    -> return (AddCueList    cuelist)
        | StateMachineActionFB.UpdateFB -> return (UpdateCueList cuelist)
        | StateMachineActionFB.RemoveFB -> return (RemoveCueList cuelist)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //   ____           ____  _
    //  / ___|   _  ___|  _ \| | __ _ _   _  ___ _ __
    // | |  | | | |/ _ \ |_) | |/ _` | | | |/ _ \ '__|
    // | |__| |_| |  __/  __/| | (_| | |_| |  __/ |
    //  \____\__,_|\___|_|   |_|\__,_|\__, |\___|_|
    //                                |___/

    | StateMachinePayloadFB.CuePlayerFB ->
      either {
        let! player =
          let playerish = fb.Payload<CuePlayerFB>()
          if playerish.HasValue then
            playerish.Value
            |> CuePlayer.FromFB
          else
            "Could not parse empty player payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | StateMachineActionFB.AddFB    -> return (AddCuePlayer    player)
        | StateMachineActionFB.UpdateFB -> return (UpdateCuePlayer player)
        | StateMachineActionFB.RemoveFB -> return (RemoveCuePlayer player)
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

    | StateMachinePayloadFB.PinGroupFB ->
      either {
        let! group =
          let groupish = fb.Payload<PinGroupFB>()
          if groupish.HasValue then
            groupish.Value
            |> PinGroup.FromFB
          else
            "Could not parse empty groupe payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | StateMachineActionFB.AddFB    -> return (AddPinGroup    group)
        | StateMachineActionFB.UpdateFB -> return (UpdatePinGroup group)
        | StateMachineActionFB.RemoveFB -> return (RemovePinGroup group)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //  ____  _
    // |  _ \(_)_ __
    // | |_) | | '_ \
    // |  __/| | | | |
    // |_|   |_|_| |_|

    | StateMachinePayloadFB.PinFB ->
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
        | StateMachineActionFB.AddFB    -> return (AddPin    pin)
        | StateMachineActionFB.UpdateFB -> return (UpdatePin pin)
        | StateMachineActionFB.RemoveFB -> return (RemovePin pin)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //  ____  _ _
    // / ___|| (_) ___ ___  ___
    // \___ \| | |/ __/ _ \/ __|
    //  ___) | | | (_|  __/\__ \
    // |____/|_|_|\___\___||___/

    | StateMachinePayloadFB.SlicesFB ->
      either {
        let! slices =
          let slicish = fb.Payload<SlicesFB>()
          if slicish.HasValue then
            slicish.Value
            |> Slices.FromFB
          else
            "Could not parse empty slices payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | StateMachineActionFB.UpdateFB -> return (UpdateSlices slices)
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

    | StateMachinePayloadFB.RaftMemberFB ->
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
        | StateMachineActionFB.AddFB    -> return (AddMember    mem)
        | StateMachineActionFB.UpdateFB -> return (UpdateMember mem)
        | StateMachineActionFB.RemoveFB -> return (RemoveMember mem)
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

    | StateMachinePayloadFB.UserFB ->
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
        | StateMachineActionFB.AddFB    -> return (AddUser    user)
        | StateMachineActionFB.UpdateFB -> return (UpdateUser user)
        | StateMachineActionFB.RemoveFB -> return (RemoveUser user)
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

    | StateMachinePayloadFB.SessionFB ->
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
        | StateMachineActionFB.AddFB    -> return (AddSession    session)
        | StateMachineActionFB.UpdateFB -> return (UpdateSession session)
        | StateMachineActionFB.RemoveFB -> return (RemoveSession session)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //  ____  _                                     _
    // |  _ \(_)___  ___ _____   _____ _ __ ___  __| |
    // | | | | / __|/ __/ _ \ \ / / _ \ '__/ _ \/ _` |
    // | |_| | \__ \ (_| (_) \ V /  __/ | |  __/ (_| |
    // |____/|_|___/\___\___/ \_/ \___|_|  \___|\__,_|

    | StateMachinePayloadFB.DiscoveredServiceFB ->
      either {
        let! discoveredService =
          let discoveredServiceish = fb.Payload<DiscoveredServiceFB>()
          if discoveredServiceish.HasValue then
            discoveredServiceish.Value
            |> DiscoveredService.FromFB
          else
            "Could not parse empty discoveredService payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail

        match fb.Action with
        | StateMachineActionFB.AddFB    -> return (AddDiscoveredService    discoveredService)
        | StateMachineActionFB.UpdateFB -> return (UpdateDiscoveredService discoveredService)
        | StateMachineActionFB.RemoveFB -> return (RemoveDiscoveredService discoveredService)
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

    | StateMachinePayloadFB.LogEventFB ->
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

    | StateMachinePayloadFB.StateFB ->
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

    | StateMachinePayloadFB.StringFB ->
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

    | StateMachinePayloadFB.ClockFB ->
      either {
        let clockish = fb.Payload<ClockFB> ()
        if clockish.HasValue then
          let clock = clockish.Value
          return (UpdateClock clock.Value)
        else
          return!
            "Could not parse empty clock payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    | _ -> either {
      let! cmd = AppCommand.FromFB fb.Action
      return (Command cmd)
    }

  #endif

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<StateMachineFB> =
    let inline addDiscoveredServicePayload (service: DiscoveredService) action =
      let offset = service.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, action)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.DiscoveredServiceFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, offset)
#else
      StateMachineFB.AddPayload(builder, offset.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    match self with
    | UpdateProject project ->
      let offset = project.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.ProjectFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, offset)
#else
      StateMachineFB.AddPayload(builder, offset.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UnloadProject ->
      StateMachineFB.StartStateMachineFB(builder)
      // This is not exactly removing a project, but we use RemoveFB to avoid having
      // another action just for UnloadProject
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.ProjectFB)
      StateMachineFB.EndStateMachineFB(builder)

    | AddMember       mem ->
      let mem = mem.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, mem)
#else
      StateMachineFB.AddPayload(builder, mem.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdateMember    mem ->
      let mem = mem.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, mem)
#else
      StateMachineFB.AddPayload(builder, mem.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemoveMember    mem ->
      let mem = mem.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.RaftMemberFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, mem)
#else
      StateMachineFB.AddPayload(builder, mem.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddClient       client ->
      let client = client.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.IrisClientFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, client)
#else
      StateMachineFB.AddPayload(builder, client.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdateClient    client ->
      let client = client.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.IrisClientFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, client)
#else
      StateMachineFB.AddPayload(builder, client.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemoveClient    client ->
      let client = client.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.IrisClientFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, client)
#else
      StateMachineFB.AddPayload(builder, client.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddPinGroup       group ->
      let group = group.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinGroupFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, group)
#else
      StateMachineFB.AddPayload(builder, group.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdatePinGroup    group ->
      let group = group.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinGroupFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, group)
#else
      StateMachineFB.AddPayload(builder, group.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemovePinGroup    group ->
      let group = group.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinGroupFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, group)
#else
      StateMachineFB.AddPayload(builder, group.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddPin       pin ->
      let pin = pin.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, pin)
#else
      StateMachineFB.AddPayload(builder, pin.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdatePin    pin ->
      let pin = pin.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, pin)
#else
      StateMachineFB.AddPayload(builder, pin.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemovePin    pin ->
      let pin = pin.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, pin)
#else
      StateMachineFB.AddPayload(builder, pin.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdateSlices slices ->
      let slices = slices.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.SlicesFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, slices)
#else
      StateMachineFB.AddPayload(builder, slices.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddCue cue ->
      let cue = cue.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CueFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, cue)
#else
      StateMachineFB.AddPayload(builder, cue.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdateCue cue ->
      let cue = cue.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CueFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, cue)
#else
      StateMachineFB.AddPayload(builder, cue.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemoveCue cue ->
      let cue = cue.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CueFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, cue)
#else
      StateMachineFB.AddPayload(builder, cue.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | CallCue cue ->
      let cue = cue.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.CallFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CueFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, cue)
#else
      StateMachineFB.AddPayload(builder, cue.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CueListFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, cuelist)
#else
      StateMachineFB.AddPayload(builder, cuelist.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdateCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CueListFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, cuelist)
#else
      StateMachineFB.AddPayload(builder, cuelist.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemoveCueList cuelist ->
      let cuelist = cuelist.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CueListFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, cuelist)
#else
      StateMachineFB.AddPayload(builder, cuelist.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddCuePlayer player ->
      let player = player.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CuePlayerFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, player)
#else
      StateMachineFB.AddPayload(builder, player.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdateCuePlayer player ->
      let player = player.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CuePlayerFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, player)
#else
      StateMachineFB.AddPayload(builder, player.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemoveCuePlayer player ->
      let player = player.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.CuePlayerFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, player)
#else
      StateMachineFB.AddPayload(builder, player.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddUser user ->
      let user = user.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.UserFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, user)
#else
      StateMachineFB.AddPayload(builder, user.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdateUser user ->
      let user = user.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.UserFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, user)
#else
      StateMachineFB.AddPayload(builder, user.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemoveUser user ->
      let user = user.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.UserFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, user)
#else
      StateMachineFB.AddPayload(builder, user.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddSession session ->
      let session = session.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.SessionFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, session)
#else
      StateMachineFB.AddPayload(builder, session.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdateSession session ->
      let session = session.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.SessionFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, session)
#else
      StateMachineFB.AddPayload(builder, session.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemoveSession session ->
      let session = session.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.SessionFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, session)
#else
      StateMachineFB.AddPayload(builder, session.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | Command appcommand ->
      let cmd = appcommand.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, cmd)
      StateMachineFB.EndStateMachineFB(builder)

    | DataSnapshot state ->
      let offset = state.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.DataSnapshotFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.StateFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, offset)
#else
      StateMachineFB.AddPayload(builder, offset.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | LogMsg log ->
      let offset = log.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.LogEventFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.LogEventFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, offset)
#else
      StateMachineFB.AddPayload(builder, offset.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | SetLogLevel level ->
      let str = builder.CreateString (string level)
      StringFB.StartStringFB(builder)
      StringFB.AddValue(builder,str)
      let offset = StringFB.EndStringFB(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.SetLogLevelFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.StringFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, offset)
#else
      StateMachineFB.AddPayload(builder, offset.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | AddDiscoveredService    service ->
      addDiscoveredServicePayload service StateMachineActionFB.AddFB

    | UpdateDiscoveredService    service ->
      addDiscoveredServicePayload service StateMachineActionFB.UpdateFB

    | RemoveDiscoveredService    service ->
      addDiscoveredServicePayload service StateMachineActionFB.RemoveFB

    | UpdateClock value ->
      ClockFB.StartClockFB(builder)
      ClockFB.AddValue(builder, value)
      let offset = ClockFB.EndClockFB(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.ClockFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, offset)
#else
      StateMachineFB.AddPayload(builder, offset.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)


  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,StateMachine> =
    Binary.createBuffer bytes
    |> StateMachineFB.GetRootAsStateMachineFB
    |> StateMachine.FromFB
