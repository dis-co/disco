namespace rec Iris.Core

// * Imports

//  ___                            _
// |_ _|_ __ ___  _ __   ___  _ __| |_ ___
//  | || '_ ` _ \| '_ \ / _ \| '__| __/ __|
//  | || | | | | | |_) | (_) | |  | |_\__ \
// |___|_| |_| |_| .__/ \___/|_|   \__|___/
//               |_|

open Aether
open Aether.Operators
open Iris.Raft

#if FABLE_COMPILER

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization

#endif

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

#endif

// * PersistenceStrategy

type PersistenceStrategy =
  | Commit
  | Save
  | Ignore

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
  | Save

  // ** ToString

  override self.ToString() =
    match self with
    | Undo -> "Undo"
    | Redo -> "Redo"
    | Reset -> "Reset"
    // PROJECT
    | Save -> "Save"

  // ** Parse

  static member Parse (str: string) =
    match str with
    | "Undo" -> Undo
    | "Redo" -> Redo
    | "Reset"-> Reset
    | "Save" -> Save
    | _      -> failwithf "AppCommand: parse error: %s" str

  // ** TryParse

  static member TryParse (str: string) =
    Either.tryWith (Error.asParseError "AppCommand.TryParse") <| fun _ ->
      str |> AppCommand.Parse

  // ** FromFB

  static member FromFB (fb: StateMachineActionFB) =
#if FABLE_COMPILER
    match fb with
    | x when x = StateMachineActionFB.UndoFB  -> Right Undo
    | x when x = StateMachineActionFB.RedoFB  -> Right Redo
    | x when x = StateMachineActionFB.ResetFB -> Right Reset
    | x when x = StateMachineActionFB.SaveFB  -> Right Save
    | x ->
      sprintf "Could not parse %A as AppCommand" x
      |> Error.asParseError "AppCommand.FromFB"
      |> Either.fail
#else
    match fb with
    | StateMachineActionFB.UndoFB  -> Right Undo
    | StateMachineActionFB.RedoFB  -> Right Redo
    | StateMachineActionFB.ResetFB -> Right Reset
    | StateMachineActionFB.SaveFB  -> Right Save
    | x ->
      sprintf "Could not parse %A as AppCommand" x
      |> Error.asParseError "AppCommand.FromFB"
      |> Either.fail
#endif

  // ** ToOffset

  member self.ToOffset(_: FlatBufferBuilder) : StateMachineActionFB =
    match self with
    | Undo  -> StateMachineActionFB.UndoFB
    | Redo  -> StateMachineActionFB.RedoFB
    | Reset -> StateMachineActionFB.ResetFB
    | Save  -> StateMachineActionFB.SaveFB

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
    PinGroups:          PinGroupMap
    PinMappings:        Map<PinMappingId,PinMapping>
    PinWidgets:         Map<WidgetId,PinWidget>
    Cues:               Map<CueId,Cue>
    CueLists:           Map<CueListId,CueList>
    Sessions:           Map<SessionId,Session>
    Users:              Map<UserId,User>
    Clients:            Map<ClientId,IrisClient>
    CuePlayers:         Map<PlayerId,CuePlayer>
    DiscoveredServices: Map<ServiceId,DiscoveredService> }

  // ** optics

  static member Project_ =
    (fun (state:State) -> state.Project),
    (fun project (state:State) -> { state with Project = project })

  static member PinGroups_ =
    (fun (state:State) -> state.PinGroups),
    (fun pinGroups (state:State) -> { state with PinGroups = pinGroups })

  static member PinGroup_ clientId groupId =
    State.PinGroups_ >-> PinGroupMap.Group_ clientId groupId

  static member PinMappings_ =
    (fun (state:State) -> state.PinMappings),
    (fun pinMappings (state:State) -> { state with PinMappings = pinMappings })

  static member PinMapping_ (id:PinMappingId) =
    State.PinMappings_ >-> Map.value_ id >-> Option.value_

  static member PinWidgets_ =
    (fun (state:State) -> state.PinWidgets),
    (fun pinWidgets (state:State) -> { state with PinWidgets = pinWidgets })

  static member PinWidget_ (id:WidgetId) =
    State.PinWidgets_ >-> Map.value_ id >-> Option.value_

  static member Cues_ =
    (fun (state:State) -> state.Cues),
    (fun cues (state:State) -> { state with Cues = cues })

  static member Cue_ (id:CueId) =
    State.Cues_ >-> Map.value_ id >-> Option.value_

  static member CueLists_ =
    (fun (state:State) -> state.CueLists),
    (fun cueLists (state:State) -> { state with CueLists = cueLists })

  static member CueList_ (id:CueListId) =
    State.CueLists_ >-> Map.value_ id >-> Option.value_

  static member Sessions_ =
    (fun (state:State) -> state.Sessions),
    (fun sessions (state:State) -> { state with Sessions = sessions })

  static member Session_ (id:SessionId) =
    State.Sessions_ >-> Map.value_ id >-> Option.value_

  static member Users_ =
    (fun (state:State) -> state.Users),
    (fun users (state:State) -> { state with Users = users })

  static member User_ (id:UserId) =
    State.Users_ >-> Map.value_ id >-> Option.value_

  static member Clients_ =
    (fun (state:State) -> state.Clients),
    (fun clients (state:State) -> { state with Clients = clients })

  static member Client_ (id:ClientId) =
    State.Clients_ >-> Map.value_ id >-> Option.value_

  static member CuePlayers_ =
    (fun (state:State) -> state.CuePlayers),
    (fun cuePlayers (state:State) -> { state with CuePlayers = cuePlayers })

  static member CuePlayer_ (id:PlayerId) =
    State.CuePlayers_ >-> Map.value_ id >-> Option.value_

  static member DiscoveredServices_ =
    (fun (state:State) -> state.DiscoveredServices),
    (fun discoveredServices (state:State) -> { state with DiscoveredServices = discoveredServices })

  static member DiscoveredService_ (id:IrisId) =
    State.DiscoveredServices_ >-> Map.value_ id >-> Option.value_

  // ** Empty

  static member Empty
    with get () =
      { Project     = IrisProject.Empty
        PinGroups   = PinGroupMap.empty
        PinMappings = Map.empty
        PinWidgets  = Map.empty
        Cues        = Map.empty
        CueLists    = Map.empty
        Sessions    = Map.empty
        Users       = Map.empty
        Clients     = Map.empty
        CuePlayers  = Map.empty
        DiscoveredServices = Map.empty }

  // ** Load

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load (path: FilePath, machine: IrisMachine) =
    either {
      let inline toMap value = Either.map (Array.map toPair >> Map.ofArray) value
      let! project  = Asset.loadWithMachine path machine
      let! groups   = Asset.load    project.Path
      let! widgets  = Asset.loadAll project.Path |> toMap
      let! mappings = Asset.loadAll project.Path |> toMap
      let! users    = Asset.loadAll project.Path |> toMap
      let! cues     = Asset.loadAll project.Path |> toMap
      let! cuelists = Asset.loadAll project.Path |> toMap
      let! players  = Asset.loadAll project.Path |> toMap
      return
        { Project            = project
          Users              = users
          Cues               = cues
          CueLists           = cuelists
          PinGroups          = groups
          PinMappings        = mappings
          PinWidgets         = widgets
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
      do! Map.fold (Asset.saveMap basePath) Either.nothing state.PinMappings
      do! Map.fold (Asset.saveMap basePath) Either.nothing state.PinWidgets
      do! Map.fold (Asset.saveMap basePath) Either.nothing state.Cues
      do! Map.fold (Asset.saveMap basePath) Either.nothing state.CueLists
      do! Map.fold (Asset.saveMap basePath) Either.nothing state.Users
      do! Map.fold (Asset.saveMap basePath) Either.nothing state.CuePlayers
      do! Asset.save basePath state.PinGroups
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
    let groups = Binary.toOffset builder self.PinGroups

    let mappings =
      Map.toArray self.PinMappings
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun mappings -> StateFB.CreatePinMappingsVector(builder, mappings)

    let widgets =
      Map.toArray self.PinWidgets
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun widgets -> StateFB.CreatePinWidgetsVector(builder, widgets)

    let cues =
      Map.toArray self.Cues
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun cues -> StateFB.CreateCuesVector(builder, cues)

    let cuelists =
      Map.toArray self.CueLists
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun cuelists -> StateFB.CreateCueListsVector(builder, cuelists)

    let users =
      Map.toArray self.Users
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun users -> StateFB.CreateUsersVector(builder, users)

    let sessions =
      Map.toArray self.Sessions
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun sessions -> StateFB.CreateSessionsVector(builder, sessions)

    let clients =
      Map.toArray self.Clients
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun clients -> StateFB.CreateClientsVector(builder, clients)

    let players =
      Map.toArray self.CuePlayers
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun players -> StateFB.CreateCuePlayersVector(builder, players)

    let services =
      Map.toArray self.DiscoveredServices
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun services -> StateFB.CreateDiscoveredServicesVector(builder, services)

    StateFB.StartStateFB(builder)
    StateFB.AddProject(builder, project)
    StateFB.AddPinGroups(builder, groups)
    StateFB.AddPinMappings(builder, mappings)
    StateFB.AddPinWidgets(builder, widgets)
    StateFB.AddCues(builder, cues)
    StateFB.AddCueLists(builder, cuelists)
    StateFB.AddSessions(builder, sessions)
    StateFB.AddClients(builder, clients)
    StateFB.AddUsers(builder, users)
    StateFB.AddCuePlayers(builder, players)
    StateFB.AddDiscoveredServices(builder, services)
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
        #if FABLE_COMPILER
        fb.PinGroups |> PinGroupMap.FromFB
        #else
        let value = fb.PinGroups
        if value.HasValue then
          value.Value
          |> PinGroupMap.FromFB
        else
          "Could not parse empty group map payload"
          |> Error.asParseError "State.FromFB"
          |> Either.fail
        #endif

      // MAPPINGS

      let! mappings =
        let arr = Array.zeroCreate fb.PinMappingsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<PinMappingId, PinMapping>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! group = fb.PinMappings(i) |> PinMapping.FromFB
            #else
            let! group =
              let value = fb.PinMappings(i)
              if value.HasValue then
                value.Value
                |> PinMapping.FromFB
              else
                "Could not parse empty PinMapping payload"
                |> Error.asParseError "State.FromFB"
                |> Either.fail
            #endif

            return (i + 1, Map.add group.Id group map)
          })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      // WIDGETS

      let! widgets =
        let arr = Array.zeroCreate fb.PinWidgetsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<WidgetId, PinWidget>>) _ -> either {
            let! (i, map) = m

            #if FABLE_COMPILER
            let! group = fb.PinWidgets(i) |> PinWidget.FromFB
            #else
            let! group =
              let value = fb.PinWidgets(i)
              if value.HasValue then
                value.Value
                |> PinWidget.FromFB
              else
                "Could not parse empty PinWidget payload"
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
          (fun (m: Either<IrisError,int * Map<CueId, Cue>>) _ -> either {
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
          (fun (m: Either<IrisError,int * Map<CueListId, CueList>>) _ -> either {
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
          (fun (m: Either<IrisError,int * Map<UserId, User>>) _ -> either {
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
          (fun (m: Either<IrisError,int * Map<SessionId, Session>>) _ -> either {
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
          (fun (m: Either<IrisError,int * Map<ClientId, IrisClient>>) _ -> either {
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
          (fun (m: Either<IrisError,int * Map<PlayerId, CuePlayer>>) _ -> either {
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
          (fun (m: Either<IrisError,int * Map<ServiceId, DiscoveredService>>) _ -> either {
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

      return {
        Project            = project
        PinGroups          = groups
        PinMappings        = mappings
        PinWidgets         = widgets
        Cues               = cues
        CueLists           = cuelists
        Users              = users
        Sessions           = sessions
        Clients            = clients
        CuePlayers         = players
        DiscoveredServices = discoveredServices
      }
    }

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,State> =
    Binary.createBuffer bytes
    |> StateFB.GetRootAsStateFB
    |> State.FromFB

// * State module

module State =

  // ** optics

  let pinGroups_ = State.PinGroups_ >-> PinGroupMap.Groups_
  let pinGroup_ clid gid = State.PinGroups_ >-> PinGroupMap.Group_ clid gid
  let playerGroups_ = State.PinGroups_ >-> PinGroupMap.Players_
  let playerGroup_ pid = State.PinGroups_ >-> PinGroupMap.Player_ pid
  let widgetGroups_ = State.PinGroups_ >-> PinGroupMap.Widgets_
  let widgetGroup_ wid = State.PinGroups_ >-> PinGroupMap.Widget_ wid

  // ** getters

  let project = Optic.get State.Project_
  let pinGroupMap = Optic.get State.PinGroups_
  let pinGroups = Optic.get pinGroups_
  let pinGroup clid gid = Optic.get (pinGroup_ clid gid)
  let playerGroups = Optic.get playerGroups_
  let playerGroup pid = Optic.get (playerGroup_ pid)
  let widgetGroups = Optic.get widgetGroups_
  let widgetGroup wid = Optic.get (widgetGroup_ wid)
  let pinMappings = Optic.get State.PinMappings_
  let pinMapping mid = Optic.get (State.PinMapping_ mid)
  let pinWidgets = Optic.get State.PinWidgets_
  let pinWidget wid = Optic.get (State.PinWidget_ wid)
  let cues = Optic.get State.Cues_
  let cue id = Optic.get (State.Cue_ id)
  let cueLists = Optic.get State.CueLists_
  let cueList id = Optic.get (State.CueList_ id)
  let sessions = Optic.get State.Sessions_
  let session id = Optic.get (State.Session_ id)
  let users = Optic.get State.Users_
  let user id = Optic.get (State.User_ id)
  let clients = Optic.get State.Clients_
  let client id = Optic.get (State.Client_ id)
  let cuePlayers = Optic.get State.CuePlayers_
  let cuePlayer id = Optic.get (State.CuePlayer_ id)
  let discoveredServices = Optic.get State.DiscoveredServices_
  let discoveredService id = Optic.get (State.DiscoveredService_ id)

  // ** setters

  let setProject = Optic.set State.Project_
  let setPinGroupMap = Optic.set State.PinGroups_
  let setPinGroups = Optic.set pinGroups_
  let setPinMappings = Optic.set State.PinMappings_
  let setPinWidgets = Optic.set State.PinWidgets_
  let setCues = Optic.set State.Cues_
  let setCueLists = Optic.set State.CueLists_
  let setSessions = Optic.set State.Sessions_
  let setUsers = Optic.set State.Users_
  let setClients = Optic.set State.Clients_
  let setCuePlayers = Optic.set State.CuePlayers_
  let setDiscoveredServices = Optic.set State.DiscoveredServices_

  // ** addPinWidget

  let addPinWidget (widget: PinWidget) (state: State) =
    if Map.containsKey widget.Id state.PinWidgets then
      state
    else
      { state with
          PinWidgets = Map.add widget.Id widget state.PinWidgets
          PinGroups = PinGroupMap.addWidget widget state.PinGroups }

  // ** updatePinWidget

  let updatePinWidget (mappping: PinWidget) (state: State) =
    if Map.containsKey mappping.Id state.PinWidgets then
      let mappings = Map.add mappping.Id mappping state.PinWidgets
      { state with PinWidgets = mappings }
    else
      state

  // ** removePinWidget

  let removePinWidget (widget: PinWidget) (state: State) =
    { state with
        PinGroups = PinGroupMap.removeWidget widget state.PinGroups
        PinWidgets = Map.remove widget.Id state.PinWidgets }

  // ** addPinMapping

  let addPinMapping (mappping: PinMapping) (state: State) =
    if Map.containsKey mappping.Id state.PinMappings then
      state
    else
      let mappings = Map.add mappping.Id mappping state.PinMappings
      { state with PinMappings = mappings }

  // ** updatePinMapping

  let updatePinMapping (mappping: PinMapping) (state: State) =
    if Map.containsKey mappping.Id state.PinMappings then
      let mappings = Map.add mappping.Id mappping state.PinMappings
      { state with PinMappings = mappings }
    else
      state

  // ** removePinMapping

  let removePinMapping (mappping: PinMapping) (state: State) =
    { state with PinMappings = Map.remove mappping.Id state.PinMappings }

  // ** addCuePlayer

  let addCuePlayer (player: CuePlayer) (state: State) =
    if Map.containsKey player.Id state.CuePlayers then
      state
    else
      { state with
          PinGroups = PinGroupMap.addPlayer player state.PinGroups
          CuePlayers = Map.add player.Id player state.CuePlayers }

  // ** updateCuePlayer

  let updateCuePlayer (player: CuePlayer) (state: State) =
    if Map.containsKey player.Id state.CuePlayers then
      let players = Map.add player.Id player state.CuePlayers
      { state with CuePlayers = players }
    else
      state

  // ** removeCuePlayer

  let removeCuePlayer (player: CuePlayer) (state: State) =
    { state with
        PinGroups = PinGroupMap.removePlayer player state.PinGroups
        CuePlayers = Map.remove player.Id state.CuePlayers }

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
    { state with PinGroups = PinGroupMap.add group state.PinGroups }

  // ** updatePinGroup

  let updatePinGroup (group : PinGroup) (state: State) =
    { state with PinGroups = PinGroupMap.add group state.PinGroups }

  // ** removePinGroup

  let removePinGroup (group : PinGroup) (state: State) =
    { state with PinGroups = PinGroupMap.remove group state.PinGroups }

  // ** addPin

  let addPin (pin: Pin) (state: State) =
    { state with PinGroups = PinGroupMap.addPin pin state.PinGroups }

  // ** updatePin

  let updatePin (pin: Pin) (state: State) =
    if Map.containsKey pin.ClientId state.Clients || pin.Persisted
    /// base case: update the pin since its parent process is running
    then { state with PinGroups = PinGroupMap.updatePin pin state.PinGroups }
    else
      /// find the correct pin group and remove the pin by folding over all groups
      PinGroupMap.foldGroups
        (fun map gid group ->
          if gid = pin.PinGroupId && group.ClientId = pin.ClientId then
            /// if the group still has other pins, then remove only the one
            if group.Pins.Count > 1 then
              group
              |> PinGroup.removePin pin
              |> flip PinGroupMap.add map
            /// else just skip this group (and thereby remove)
            else map
          else PinGroupMap.add group map)
        PinGroupMap.empty
        state.PinGroups
      |> fun groups -> { state with PinGroups = groups }

  // ** removePin

  let removePin (pin : Pin) (state: State) =
    { state with PinGroups = PinGroupMap.removePin pin state.PinGroups }

  // ** updateSlices

  let updateSlices (map: SlicesMap) (state: State) =
    let players =
      if PinGroupMap.hasPlayerUpdate map.Slices state.PinGroups
      then Map.map (fun _ player -> CuePlayer.processSlices map.Slices player) state.CuePlayers
      else state.CuePlayers

    let widgets =
      if PinGroupMap.hasWidgetUpdate map.Slices state.PinGroups
      then Map.map (fun _ widget -> PinWidget.processSlices map.Slices widget) state.PinWidgets
      else state.PinWidgets

    { state with
        CuePlayers = players
        PinWidgets = widgets
        PinGroups = PinGroupMap.updateSlices map.Slices state.PinGroups }

  // ** tryFindPin

  let tryFindPin (id: PinId) (state: State) =
    PinGroupMap.findPin id state.PinGroups

  // ** tryFindPinGroup

  let tryFindPinGroup (client: ClientId) (group: PinGroupId) (state: State) =
    PinGroupMap.tryFindGroup client group state.PinGroups

  // ** findPinGroupBy

  let findPinGroupBy (pred: PinGroup -> bool) (state: State) =
    PinGroupMap.findGroupBy pred state.PinGroups

  // ** addCueList

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_ ___
  // | |  | | | |/ _ \ |   | / __| __/ __|
  // | |__| |_| |  __/ |___| \__ \ |_\__ \
  //  \____\__,_|\___|_____|_|___/\__|___/

  let addCueList (cuelist : CueList) (state: State) =
    if Map.containsKey cuelist.Id state.CueLists
    then state
    else { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }

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
    { state with
        Clients = Map.remove client.Id state.Clients
        PinGroups = PinGroupMap.removeByClient client.Id state.PinGroups }

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

  // ** onSave

  /// resets dirty flags on pins that are marked as persisted
  let onSave (state: State) =
    { state with PinGroups = PinGroupMap.mapPins (Pin.setDirty false) state.PinGroups }

  // ** update

  let update (state: State) = function
    | AddCue            cue           -> addCue         cue     state
    | UpdateCue         cue           -> updateCue      cue     state
    | RemoveCue         cue           -> removeCue      cue     state

    | AddCueList    cuelist           -> addCueList     cuelist state
    | UpdateCueList cuelist           -> updateCueList  cuelist state
    | RemoveCueList cuelist           -> removeCueList  cuelist state

    | AddCuePlayer    player          -> addCuePlayer    player state
    | UpdateCuePlayer player          -> updateCuePlayer player state
    | RemoveCuePlayer player          -> removeCuePlayer player state

    | AddPinGroup     group           -> addPinGroup    group   state
    | UpdatePinGroup  group           -> updatePinGroup group   state
    | RemovePinGroup  group           -> removePinGroup group   state

    | AddPinWidget     widget         -> addPinWidget    widget   state
    | UpdatePinWidget  widget         -> updatePinWidget widget   state
    | RemovePinWidget  widget         -> removePinWidget widget   state

    | AddPinMapping     mapping       -> addPinMapping    mapping   state
    | UpdatePinMapping  mapping       -> updatePinMapping mapping   state
    | RemovePinMapping  mapping       -> removePinMapping mapping   state

    | AddPin            pin           -> addPin         pin     state
    | UpdatePin         pin           -> updatePin      pin     state
    | RemovePin         pin           -> removePin      pin     state
    | UpdateSlices   slices           -> updateSlices   slices  state

    | AddMember         mem           -> addMember      mem     state
    | UpdateMember      mem           -> updateMember   mem     state
    | RemoveMember      mem           -> removeMember   mem     state

    | AddClient      client           -> addClient      client  state
    | UpdateClient   client           -> updateClient   client  state
    | RemoveClient   client           -> removeClient   client  state

    | AddSession    session           -> addSession     session state
    | UpdateSession session           -> updateSession  session state
    | RemoveSession session           -> removeSession  session state

    | AddUser          user           -> addUser        user    state
    | UpdateUser       user           -> updateUser     user    state
    | RemoveUser       user           -> removeUser     user    state

    | UpdateProject project           -> updateProject  project state

    // It may happen that a service didn't make it into the state and an update service
    // event is received. For those cases just add/update the service into the state.
    | AddDiscoveredService    service
    | UpdateDiscoveredService service -> addOrUpdateService    service state
    | RemoveDiscoveredService service -> removeService         service state

    | Command AppCommand.Save         -> onSave state

    | _ -> state

  // ** processBatch

  let processBatch (state: State) (batch: Transaction) =
    List.fold update state batch.Commands

  // ** initialize

  let initialize (state: State) =
    { state with
        PinGroups = PinGroupMap.mapGroups PinGroup.setPinsOffline state.PinGroups }

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
    State: State }

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
    | Command (AppCommand.Redo)  -> self.Redo()
    | Command (AppCommand.Undo)  -> self.Undo()
    | Command (AppCommand.Reset) -> ()   // do nothing for now

    | UnloadProject              -> self.Notify(ev) // This event doesn't actually modify the state

    | CommandBatch batch         -> State.processBatch state batch |> andRender

    | other                      -> State.update state other |> andRender

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

// * Transaction

type Transaction = Transaction of StateMachine list
  with
    // ** Commands

    member batch.Commands
      with get () = match batch with | Transaction commands -> commands

    // ** ToOffset

    member batch.ToOffset(builder: FlatBufferBuilder) =
      let serialized =
        batch.Commands
        |> List.map (Binary.toOffset builder)
        |> List.toArray
        |> fun arr -> TransactionFB.CreateCommandsVector(builder, arr)
      TransactionFB.StartTransactionFB(builder)
      TransactionFB.AddCommands(builder, serialized)
      TransactionFB.EndTransactionFB(builder)

    // ** FromFB

    static member FromFB(batch: TransactionFB) =
      either {
        let input = Array.zeroCreate batch.CommandsLength

        let! (_,commands) =
          #if FABLE_COMPILER
          Array.fold
            (fun (m: Either<IrisError, int * StateMachine array>) _ -> either {
                let! (idx, arr) = m
                let! cmd = batch.Commands(idx) |>  StateMachine.FromFB
                do arr.[idx] <- cmd
                return (idx +  1, arr)
              })
            (Right (0, input))
            input
          #else
          Array.fold
            (fun (m: Either<IrisError, int * StateMachine array>)  _ -> either {
                let! (idx, arr) = m
                let cmdish = batch.Commands(idx)
                if cmdish.HasValue then
                  let raw = cmdish.Value
                  let! cmd = StateMachine.FromFB raw
                  do arr.[idx] <- cmd
                  return (idx +  1, arr)
                else
                  return!
                    "Could not parse empty CommandBatch *value* payload"
                    |> Error.asParseError "StateMachine.FromFB"
                    |> Either.fail
              })
            (Right (0, input))
            input
          #endif
        return Transaction (List.ofArray commands)
      }

    // ** ToBytes

    member batch.ToBytes() =
      Binary.buildBuffer batch

    // ** FromBytes

    static member FromBytes(raw: byte array) =
      raw
      |> Binary.createBuffer
      |> TransactionFB.GetRootAsTransactionFB
      |> Transaction.FromFB

// * SlicesMap

type SlicesMap = SlicesMap of Map<PinId,Slices>
  with
    member map.Slices
      with get () = match map with SlicesMap slices -> slices

    member map.ToOffset(builder: FlatBufferBuilder) =
      let vector =
        map.Slices
        |> Map.toArray
        |> Array.map (snd >> Binary.toOffset builder)
        |> fun arr -> SlicesMapFB.CreateSlicesVector(builder, arr)
      SlicesMapFB.StartSlicesMapFB(builder)
      SlicesMapFB.AddSlices(builder, vector)
      SlicesMapFB.EndSlicesMapFB(builder)

    static member FromFB(fb: SlicesMapFB) =
      [ 0 .. fb.SlicesLength - 1 ]
      |> List.fold
        (fun (m: Either<IrisError,Map<PinId,Slices>>) (idx: int) -> either {
            let! output = m
            #if FABLE_COMPILER
            let! parsed = fb.Slices(idx) |> Slices.FromFB
            return Map.add parsed.PinId parsed output
            #else
            let slicish = fb.Slices(idx)
            if slicish.HasValue then
              let slices = slicish.Value
              let! parsed = Slices.FromFB slices
              return Map.add parsed.PinId parsed output
            else
              return!
                "Could not parse empty SlicesFB value"
                |> Error.asParseError "SlicesMap"
                |> Either.fail
            #endif
          })
        (Right Map.empty)
      |> Either.map SlicesMap

// * SlicesMap module

module SlicesMap =

  // ** empty

  let empty = SlicesMap Map.empty

  // ** add

  let add (map: SlicesMap) (slices: Slices) =
    map.Slices
    |> Map.add slices.PinId slices
    |> SlicesMap

  // ** remove

  let remove (map: SlicesMap) (slices: Slices) =
    map.Slices
    |> Map.remove slices.PinId
    |> SlicesMap

  // ** containsKey

  let containsKey (map: SlicesMap) (key: PinId) =
    map.Slices |> Map.containsKey key

  // ** isEmpty

  let isEmpty (map: SlicesMap) = map.Slices.IsEmpty

  // ** fold

  let fold (folder: 'a -> PinId -> Slices -> 'a) (state: 'a) (map: SlicesMap) =
    Map.fold folder state map.Slices

  // ** merge

  /// Merge two slices maps, wherein the pin slices inside the first argument are superceded by the
  /// values inside the second argument SlicesMap.
  let merge (current: SlicesMap) (updates:SlicesMap): SlicesMap =
    fold (fun next _ slices -> add next slices) current updates

  // ** keys

  let keys (map: SlicesMap) =
    fold (fun out id _ -> id :: out) List.empty map

// * StateMachine

//  ____  _        _       __  __            _     _
// / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
// \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
// |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

type StateMachine =
  // Project
  | UpdateProject           of IrisProject
  | UnloadProject

  // Member
  | AddMember               of RaftMember
  | UpdateMember            of RaftMember
  | RemoveMember            of RaftMember

  // Client
  | AddClient               of IrisClient
  | UpdateClient            of IrisClient
  | RemoveClient            of IrisClient

  // GROUP
  | AddPinGroup             of PinGroup
  | UpdatePinGroup          of PinGroup
  | RemovePinGroup          of PinGroup

  // MAPPING
  | AddPinMapping           of PinMapping
  | UpdatePinMapping        of PinMapping
  | RemovePinMapping        of PinMapping

  // WIDGET
  | AddPinWidget            of PinWidget
  | UpdatePinWidget         of PinWidget
  | RemovePinWidget         of PinWidget

  // PIN
  | AddPin                  of Pin
  | UpdatePin               of Pin
  | RemovePin               of Pin
  | UpdateSlices            of SlicesMap

  // CUE
  | AddCue                  of Cue
  | UpdateCue               of Cue
  | RemoveCue               of Cue
  | CallCue                 of Cue

  // CUE
  | AddCueList              of CueList
  | UpdateCueList           of CueList
  | RemoveCueList           of CueList

  // CUEPLAYER
  | AddCuePlayer            of CuePlayer
  | UpdateCuePlayer         of CuePlayer
  | RemoveCuePlayer         of CuePlayer

  // User
  | AddUser                 of User
  | UpdateUser              of User
  | RemoveUser              of User

  // Session
  | AddSession              of Session
  | UpdateSession           of Session
  | RemoveSession           of Session

  // Discovery
  | AddDiscoveredService    of DiscoveredService
  | UpdateDiscoveredService of DiscoveredService
  | RemoveDiscoveredService of DiscoveredService

  | UpdateClock             of uint32

  | Command                 of AppCommand

  | DataSnapshot            of State

  | CommandBatch            of Transaction

  | SetLogLevel             of LogLevel

  | LogMsg                  of LogEvent

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

    // MAPPING
    | AddPinMapping           _ -> "AddPinMapping"
    | UpdatePinMapping        _ -> "UpdatePinMapping"
    | RemovePinMapping        _ -> "RemovePinMapping"

    // WIDGET
    | AddPinWidget            _ -> "AddPinWidget"
    | UpdatePinWidget         _ -> "UpdatePinWidget"
    | RemovePinWidget         _ -> "RemovePinWidget"

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
    | CommandBatch            _ -> "CommandBatch"
    | DataSnapshot            _ -> "DataSnapshot"
    | SetLogLevel             _ -> "SetLogLevel"
    | LogMsg                  _ -> "LogMsg"

    | UpdateClock             _ -> "UpdateClock"

  // ** PersistenceStrategy

  member cmd.PersistenceStrategy
    with get () =
      match cmd with
      // Project
      | UpdateProject           _      -> Save
      | UnloadProject           _      -> Ignore

      // Member
      | UpdateMember            _      -> Save
      | AddMember               _
      | RemoveMember            _      -> Commit

      // Client
      | AddClient               _
      | UpdateClient            _
      | RemoveClient            _      -> Ignore

      // GROUP
      | AddPinGroup             _
      | UpdatePinGroup          _
      | RemovePinGroup          _      -> Save

      // MAPPING
      | AddPinMapping           _
      | UpdatePinMapping        _
      | RemovePinMapping        _      -> Save

      // WIDGET
      | AddPinWidget            _
      | UpdatePinWidget         _
      | RemovePinWidget         _      -> Save

      // PIN
      | AddPin                  _
      | UpdatePin               _
      | RemovePin               _      -> Save
      | UpdateSlices            _      -> Ignore

      // CUE
      | AddCue                  _
      | UpdateCue               _
      | RemoveCue               _      -> Save
      | CallCue                 _      -> Ignore

      // CUELIST
      | AddCueList              _
      | UpdateCueList           _
      | RemoveCueList           _      -> Save

      // CUEPLAYER
      | AddCuePlayer            _
      | UpdateCuePlayer         _
      | RemoveCuePlayer         _      -> Save

      // User
      | AddUser                 _
      | UpdateUser              _
      | RemoveUser              _      -> Save

      // Session
      | AddSession              _
      | UpdateSession           _
      | RemoveSession           _      -> Ignore

      // Discovery
      | AddDiscoveredService    _
      | UpdateDiscoveredService _
      | RemoveDiscoveredService _      -> Ignore

      | CommandBatch            _      -> Ignore

      | UpdateClock             _      -> Ignore

      | Command AppCommand.Save        -> Commit
      | Command                 _      -> Ignore

      | DataSnapshot            _      -> Ignore

      | SetLogLevel             _      -> Ignore

      | LogMsg                  _      -> Ignore

  // ** ApiParameterType

  #if !FABLE_COMPILER

  member cmd.ApiParameterType
    with get () =
      match cmd with
      | UpdateProject           _  -> ParameterFB.ProjectFB
      | UnloadProject              -> ParameterFB.NONE

      | AddMember               _
      | UpdateMember            _
      | RemoveMember            _  -> ParameterFB.RaftMemberFB

      | AddClient               _
      | UpdateClient            _
      | RemoveClient            _  -> ParameterFB.IrisClientFB

      | AddPinGroup             _
      | UpdatePinGroup          _
      | RemovePinGroup          _  -> ParameterFB.PinGroupFB

      | AddPinMapping           _
      | UpdatePinMapping        _
      | RemovePinMapping        _  -> ParameterFB.PinMappingFB

      | AddPinWidget            _
      | UpdatePinWidget         _
      | RemovePinWidget         _  -> ParameterFB.PinWidgetFB

      | AddPin                  _
      | UpdatePin               _
      | RemovePin               _  -> ParameterFB.PinFB

      | UpdateSlices            _  -> ParameterFB.SlicesFB

      | AddCue                  _
      | UpdateCue               _
      | RemoveCue               _
      | CallCue                 _  -> ParameterFB.CueFB

      | AddCueList              _
      | UpdateCueList           _
      | RemoveCueList           _  -> ParameterFB.CueListFB

      | AddCuePlayer            _
      | UpdateCuePlayer         _
      | RemoveCuePlayer         _  -> ParameterFB.CuePlayerFB

      | AddUser                 _
      | UpdateUser              _
      | RemoveUser              _  -> ParameterFB.UserFB

      | AddSession              _
      | UpdateSession           _
      | RemoveSession           _  -> ParameterFB.SessionFB

      | AddDiscoveredService    _
      | UpdateDiscoveredService _
      | RemoveDiscoveredService _  -> ParameterFB.DiscoveredServiceFB

      | UpdateClock             _  -> ParameterFB.ClockFB

      | Command                 _  -> ParameterFB.NONE

      | DataSnapshot            _  -> ParameterFB.StateFB

      | CommandBatch            _  -> ParameterFB.TransactionFB

      | SetLogLevel             _  -> ParameterFB.StringFB

      | LogMsg                  _  -> ParameterFB.LogEventFB

  // ** ApiCommand

  member cmd.ApiCommand
    with get () =
      match cmd with
      | UnloadProject              -> ApiCommandFB.UnloadFB

      | AddDiscoveredService    _
      | AddUser                 _
      | AddSession              _
      | AddCuePlayer            _
      | AddCueList              _
      | AddCue                  _
      | AddPin                  _
      | AddPinGroup             _
      | AddPinMapping           _
      | AddPinWidget            _
      | AddClient               _
      | AddMember               _ -> ApiCommandFB.AddFB

      | UpdateClock             _
      | UpdateDiscoveredService _
      | UpdateUser              _
      | UpdateSession           _
      | UpdateCuePlayer         _
      | UpdateCueList           _
      | UpdateCue               _
      | UpdatePin               _
      | UpdateSlices            _
      | UpdatePinGroup          _
      | UpdatePinMapping        _
      | UpdatePinWidget         _
      | UpdateClient            _
      | UpdateMember            _
      | UpdateProject           _  -> ApiCommandFB.UpdateFB

      | RemoveDiscoveredService _
      | RemoveUser              _
      | RemoveSession           _
      | RemoveCuePlayer         _
      | RemoveCueList           _
      | RemoveCue               _
      | RemovePin               _
      | RemovePinGroup          _
      | RemovePinMapping        _
      | RemovePinWidget         _
      | RemoveClient            _
      | RemoveMember            _ -> ApiCommandFB.RemoveFB

      | CallCue                 _ -> ApiCommandFB.CallCueFB

      | Command AppCommand.Undo   -> ApiCommandFB.UndoFB
      | Command AppCommand.Redo   -> ApiCommandFB.RedoFB
      | Command AppCommand.Reset  -> ApiCommandFB.ResetFB
      | Command AppCommand.Save   -> ApiCommandFB.SaveFB

      | DataSnapshot            _ -> ApiCommandFB.SnapshotFB

      | CommandBatch            _ -> ApiCommandFB.BatchFB

      | SetLogLevel             _ -> ApiCommandFB.SetLogLevelFB

      | LogMsg                  _ -> ApiCommandFB.LogEventFB

  #endif

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
    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
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

    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
    // | |  | |  __/ | | | | | |_) |  __/ |
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|
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

    //   ____ _ _            _
    //  / ___| (_) ___ _ __ | |_
    // | |   | | |/ _ \ '_ \| __|
    // | |___| | |  __/ | | | |_
    //  \____|_|_|\___|_| |_|\__|
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

    //   ____
    //  / ___|_ __ ___  _   _ _ __
    // | |  _| '__/ _ \| | | | '_ \
    // | |_| | | | (_) | |_| | |_) |
    //  \____|_|  \___/ \__,_| .__/
    //                       |_|
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

    //  __  __                   _
    // |  \/  | __ _ _ __  _ __ (_)_ __   __ _
    // | |\/| |/ _` | '_ \| '_ \| | '_ \ / _` |
    // | |  | | (_| | |_) | |_) | | | | | (_| |
    // |_|  |_|\__,_| .__/| .__/|_|_| |_|\__, |
    //              |_|   |_|            |___/
    | x when x = StateMachinePayloadFB.PinMappingFB ->
      let mapping = fb.PinMappingFB |> PinMapping.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddPinMapping mapping
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdatePinMapping mapping
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemovePinMapping mapping
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    // __        ___     _            _
    // \ \      / (_) __| | __ _  ___| |_
    //  \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
    //   \ V  V / | | (_| | (_| |  __/ |_
    //    \_/\_/  |_|\__,_|\__, |\___|\__|
    //                     |___/
    | x when x = StateMachinePayloadFB.PinWidgetFB ->
      let widget = fb.PinWidgetFB |> PinWidget.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.AddFB ->
        Either.map AddPinWidget widget
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdatePinWidget widget
      | x when x = StateMachineActionFB.RemoveFB ->
        Either.map RemovePinWidget widget
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    //  ____  _
    // |  _ \(_)_ __
    // | |_) | | '_ \
    // |  __/| | | | |
    // |_|   |_|_| |_|
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

    //  ____  _ _
    // / ___|| (_) ___ ___  ___
    // \___ \| | |/ __/ _ \/ __|
    //  ___) | | | (_|  __/\__ \
    // |____/|_|_|\___\___||___/
    | x when x = StateMachinePayloadFB.SlicesMapFB ->
      let slices = fb.SlicesMapFB |> SlicesMap.FromFB
      match fb.Action with
      | x when x = StateMachineActionFB.UpdateFB ->
        Either.map UpdateSlices slices
      | x ->
        sprintf "Could not parse unknown StateMachineActionFB %A" x
        |> Error.asParseError "StateMachine.FromFB"
        |> Either.fail

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|
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

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|
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

    //   ____           ____  _
    //  / ___|   _  ___|  _ \| | __ _ _   _  ___ _ __
    // | |  | | | |/ _ \ |_) | |/ _` | | | |/ _ \ '__|
    // | |__| |_| |  __/  __/| | (_| | |_| |  __/ |
    //  \____\__,_|\___|_|   |_|\__,_|\__, |\___|_|
    //                                |___/
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

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|
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

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|
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

    //  ____  _                                     _
    // |  _ \(_)___  ___ _____   _____ _ __ ___  __| |
    // | | | | / __|/ __/ _ \ \ / / _ \ '__/ _ \/ _` |
    // | |_| | \__ \ (_| (_) \ V /  __/ | |  __/ (_| |
    // |____/|_|___/\___\___/ \_/ \___|_|  \___|\__,_|
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

    //  ____                        _           _
    // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
    // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
    //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
    // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
    //                   |_|
    | x when x = StateMachinePayloadFB.StateFB && fb.Action = StateMachineActionFB.DataSnapshotFB ->
      fb.StateFB
      |> State.FromFB
      |> Either.map DataSnapshot

    //  _                _____                 _
    // | |    ___   __ _| ____|_   _____ _ __ | |_
    // | |   / _ \ / _` |  _| \ \ / / _ \ '_ \| __|
    // | |__| (_) | (_| | |___ \ V /  __/ | | | |_
    // |_____\___/ \__, |_____| \_/ \___|_| |_|\__|
    //             |___/
    | x when x = StateMachinePayloadFB.LogEventFB ->
      fb.LogEventFB
      |> LogEvent.FromFB
      |> Either.map LogMsg

    //  ____  _        _
    // / ___|| |_ _ __(_)_ __   __ _
    // \___ \| __| '__| | '_ \ / _` |
    //  ___) | |_| |  | | | | | (_| |
    // |____/ \__|_|  |_|_| |_|\__, |
    //                         |___/
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

    //   ____ _            _
    //  / ___| | ___   ___| | __
    // | |   | |/ _ \ / __| |/ /
    // | |___| | (_) | (__|   <
    //  \____|_|\___/ \___|_|\_\
    | x when x = StateMachinePayloadFB.ClockFB ->
      UpdateClock(fb.ClockFB.Value)
      |> Either.succeed

    //  ____        _       _
    // | __ )  __ _| |_ ___| |__
    // |  _ \ / _` | __/ __| '_ \
    // | |_) | (_| | || (__| | | |
    // |____/ \__,_|\__\___|_| |_|
    | x when x = StateMachinePayloadFB.TransactionFB ->
      either {
        let fb = fb.TransactionFB
        let! batch = Transaction.FromFB fb
        return CommandBatch batch
      }

    //   ____                                          _
    //  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| |
    // | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |
    // | |__| (_) | | | | | | | | | | | (_| | | | | (_| |
    //  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|
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

    //   ____
    //  / ___|_ __ ___  _   _ _ __
    // | |  _| '__/ _ \| | | | '_ \
    // | |_| | | | (_) | |_| | |_) |
    //  \____|_|  \___/ \__,_| .__/
    //                       |_|
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

    //  __  __                   _
    // |  \/  | __ _ _ __  _ __ (_)_ __   __ _
    // | |\/| |/ _` | '_ \| '_ \| | '_ \ / _` |
    // | |  | | (_| | |_) | |_) | | | | | (_| |
    // |_|  |_|\__,_| .__/| .__/|_|_| |_|\__, |
    //              |_|   |_|            |___/
    | StateMachinePayloadFB.PinMappingFB ->
      either {
        let! mapping =
          let mappingish = fb.Payload<PinMappingFB>()
          if mappingish.HasValue then
            mappingish.Value
            |> PinMapping.FromFB
          else
            "Could not parse empty mapping payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
        match fb.Action with
        | StateMachineActionFB.AddFB    -> return (AddPinMapping    mapping)
        | StateMachineActionFB.UpdateFB -> return (UpdatePinMapping mapping)
        | StateMachineActionFB.RemoveFB -> return (RemovePinMapping mapping)
        | x ->
          return!
            sprintf "Could not parse command. Unknown ActionTypeFB: %A" x
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    // __        ___     _            _
    // \ \      / (_) __| | __ _  ___| |_
    //  \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
    //   \ V  V / | | (_| | (_| |  __/ |_
    //    \_/\_/  |_|\__,_|\__, |\___|\__|
    //                     |___/
    | StateMachinePayloadFB.PinWidgetFB ->
      either {
        let! widget =
          let widgetish = fb.Payload<PinWidgetFB>()
          if widgetish.HasValue then
            widgetish.Value
            |> PinWidget.FromFB
          else
            "Could not parse empty widget payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
        match fb.Action with
        | StateMachineActionFB.AddFB    -> return (AddPinWidget    widget)
        | StateMachineActionFB.UpdateFB -> return (UpdatePinWidget widget)
        | StateMachineActionFB.RemoveFB -> return (RemovePinWidget widget)
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
    | StateMachinePayloadFB.SlicesMapFB ->
      either {
        let! slices =
          let slicesMapish = fb.Payload<SlicesMapFB>()
          if slicesMapish.HasValue then
            let slicesMap = slicesMapish.Value
            SlicesMap.FromFB slicesMap
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

    //  ____                        _           _
    // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
    // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
    //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
    // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
    //                   |_|
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

    //  ____  _        _
    // / ___|| |_ _ __(_)_ __   __ _
    // \___ \| __| '__| | '_ \ / _` |
    //  ___) | |_| |  | | | | | (_| |
    // |____/ \__|_|  |_|_| |_|\__, |
    //                         |___/
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

    //   ____ _            _
    //  / ___| | ___   ___| | __
    // | |   | |/ _ \ / __| |/ /
    // | |___| | (_) | (__|   <
    //  \____|_|\___/ \___|_|\_\
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

    //   ____                                          _ ____        _       _
    //  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| | __ )  __ _| |_ ___| |__
    // | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |  _ \ / _` | __/ __| '_ \
    // | |__| (_) | | | | | | | | | | | (_| | | | | (_| | |_) | (_| | || (__| | | |
    //  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|____/ \__,_|\__\___|_| |_|
    | StateMachinePayloadFB.TransactionFB ->
      either {
        let batchish = fb.Payload<TransactionFB> ()
        if batchish.HasValue then
          let batch = batchish.Value
          let! commands = Transaction.FromFB batch
          return CommandBatch commands
        else
          return!
            "Could not parse empty CommandBatch payload"
            |> Error.asParseError "StateMachine.FromFB"
            |> Either.fail
      }

    //   ____                                          _
    //  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| |
    // | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |
    // | |__| (_) | | | | | | | | | | | (_| | | | | (_| |
    //  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|
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
    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/
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

    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
    // | |  | |  __/ | | | | | |_) |  __/ |
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|
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

    //   ____ _ _            _
    //  / ___| (_) ___ _ __ | |_
    // | |   | | |/ _ \ '_ \| __|
    // | |___| | |  __/ | | | |_
    //  \____|_|_|\___|_| |_|\__|
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

    //  __  __                   _
    // |  \/  | __ _ _ __  _ __ (_)_ __   __ _
    // | |\/| |/ _` | '_ \| '_ \| | '_ \ / _` |
    // | |  | | (_| | |_) | |_) | | | | | (_| |
    // |_|  |_|\__,_| .__/| .__/|_|_| |_|\__, |
    //              |_|   |_|            |___/
    | AddPinMapping       mapping ->
      let mapping = mapping.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinMappingFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, mapping)
#else
      StateMachineFB.AddPayload(builder, mapping.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdatePinMapping    mapping ->
      let mapping = mapping.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinMappingFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, mapping)
#else
      StateMachineFB.AddPayload(builder, mapping.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemovePinMapping    mapping ->
      let mapping = mapping.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinMappingFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, mapping)
#else
      StateMachineFB.AddPayload(builder, mapping.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    // __        ___     _            _
    // \ \      / (_) __| | __ _  ___| |_
    //  \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
    //   \ V  V / | | (_| | (_| |  __/ |_
    //    \_/\_/  |_|\__,_|\__, |\___|\__|
    //                     |___/
    | AddPinWidget       widget ->
      let widget = widget.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.AddFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinWidgetFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, widget)
#else
      StateMachineFB.AddPayload(builder, widget.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | UpdatePinWidget    widget ->
      let widget = widget.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinWidgetFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, widget)
#else
      StateMachineFB.AddPayload(builder, widget.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    | RemovePinWidget    widget ->
      let widget = widget.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.RemoveFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.PinWidgetFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, widget)
#else
      StateMachineFB.AddPayload(builder, widget.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    //   ____
    //  / ___|_ __ ___  _   _ _ __
    // | |  _| '__/ _ \| | | | '_ \
    // | |_| | | | (_) | |_| | |_) |
    //  \____|_|  \___/ \__,_| .__/
    //                       |_|
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

    //  ____  _
    // |  _ \(_)_ __
    // | |_) | | '_ \
    // |  __/| | | | |
    // |_|   |_|_| |_|
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

    //  ____  _ _
    // / ___|| (_) ___ ___  ___
    // \___ \| | |/ __/ _ \/ __|
    //  ___) | | | (_|  __/\__ \
    // |____/|_|_|\___\___||___/
    | UpdateSlices slices ->
      let slices = Binary.toOffset builder slices
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.UpdateFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.SlicesMapFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, slices)
#else
      StateMachineFB.AddPayload(builder, slices.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|
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

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|
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

    //  ____  _
    // |  _ \| | __ _ _   _  ___ _ __
    // | |_) | |/ _` | | | |/ _ \ '__|
    // |  __/| | (_| | |_| |  __/ |
    // |_|   |_|\__,_|\__, |\___|_|
    //                |___/
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

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|
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

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|
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

    //   ____                                          _
    //  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| |
    // | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |
    // | |__| (_) | | | | | | | | | | | (_| | | | | (_| |
    //  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|
    | Command appcommand ->
      let cmd = appcommand.ToOffset(builder)
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, cmd)
      StateMachineFB.EndStateMachineFB(builder)

    //  ____                        _           _
    // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
    // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
    //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
    // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
    //                   |_|
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

    //  _                __  __
    // | |    ___   __ _|  \/  |___  __ _
    // | |   / _ \ / _` | |\/| / __|/ _` |
    // | |__| (_) | (_| | |  | \__ \ (_| |
    // |_____\___/ \__, |_|  |_|___/\__, |
    //             |___/            |___/
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

    //  ____       _   _                _                   _
    // / ___|  ___| |_| |    ___   __ _| |    _____   _____| |
    // \___ \ / _ \ __| |   / _ \ / _` | |   / _ \ \ / / _ \ |
    //  ___) |  __/ |_| |__| (_) | (_| | |__|  __/\ V /  __/ |
    // |____/ \___|\__|_____\___/ \__, |_____\___| \_/ \___|_|
    //                            |___/
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

    //  ____  _                                     _
    // |  _ \(_)___  ___ _____   _____ _ __ ___  __| |
    // | | | | / __|/ __/ _ \ \ / / _ \ '__/ _ \/ _` |
    // | |_| | \__ \ (_| (_) \ V /  __/ | |  __/ (_| |
    // |____/|_|___/\___\___/ \_/ \___|_|  \___|\__,_|
    | AddDiscoveredService    service ->
      addDiscoveredServicePayload service StateMachineActionFB.AddFB

    | UpdateDiscoveredService    service ->
      addDiscoveredServicePayload service StateMachineActionFB.UpdateFB

    | RemoveDiscoveredService    service ->
      addDiscoveredServicePayload service StateMachineActionFB.RemoveFB

    //   ____                                          _ ____        _       _
    //  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| | __ )  __ _| |_ ___| |__
    // | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |  _ \ / _` | __/ __| '_ \
    // | |__| (_) | | | | | | | | | | | (_| | | | | (_| | |_) | (_| | || (__| | | |
    //  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|____/ \__,_|\__\___|_| |_|
    | CommandBatch commands ->
      let offset = Binary.toOffset builder commands
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAction(builder, StateMachineActionFB.BatchFB)
      StateMachineFB.AddPayloadType(builder, StateMachinePayloadFB.TransactionFB)
#if FABLE_COMPILER
      StateMachineFB.AddPayload(builder, offset)
#else
      StateMachineFB.AddPayload(builder, offset.Value)
#endif
      StateMachineFB.EndStateMachineFB(builder)

    //  _   _           _       _        ____ _            _
    // | | | |_ __   __| | __ _| |_ ___ / ___| | ___   ___| | __
    // | | | | '_ \ / _` |/ _` | __/ _ \ |   | |/ _ \ / __| |/ /
    // | |_| | |_) | (_| | (_| | ||  __/ |___| | (_) | (__|   <
    //  \___/| .__/ \__,_|\__,_|\__\___|\____|_|\___/ \___|_|\_\
    //       |_|
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

// * CommandBatch

module CommandBatch =

  let ofList = Transaction >> CommandBatch

// * UpdateSlices module

module UpdateSlices =

  let ofSlices (slices: Slices) =
    Map.ofList [(slices.PinId, slices)]
    |> SlicesMap
    |> UpdateSlices

  let ofArray (slices: Slices array) =
    slices
    |> Array.map (fun slices -> slices.PinId, slices)
    |> Map.ofArray
    |> SlicesMap
    |> UpdateSlices

  let ofList (slices: Slices list) =
    slices
    |> List.map (fun slices -> slices.PinId, slices)
    |> Map.ofList
    |> SlicesMap
    |> UpdateSlices

// * CuePlayerExtensions module

[<AutoOpen>]
module CuePlayerExtensions =

  type CuePlayer with

    static member next (cue:Cue) (player:CuePlayer) =
      CommandBatch.ofList [
        UpdateSlices.ofList [ BoolSlices(player.NextId, None, [| true |]) ]
        CallCue cue
      ]

    static member previous (cue:Cue) (player:CuePlayer) =
      CommandBatch.ofList [
        UpdateSlices.ofList [ BoolSlices(player.PreviousId, None, [| true |]) ]
        CallCue cue
      ]

    static member call (cue:Cue) (player:CuePlayer) =
      CommandBatch.ofList [
        UpdateSlices.ofList [ BoolSlices(player.CallId, None, [| true |]) ]
        CallCue cue
      ]
