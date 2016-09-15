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
open Fable.Import.JS
open System.Collections.Generic
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

  static member Type
    with get () = Serialization.GetTypeName<AppCommand>()

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

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() : JToken =
    let json = new JObject()
    json.["$type"] <- new JValue(AppCommand.Type)

    let add (case: string) =
      json.["Case"] <- new JValue(case)

    match self with
    | Undo        -> add "Undo"
    | Redo        -> add "Redo"
    | Reset       -> add "Reset"
    | SaveProject -> add "SaveProject"

    json :> JToken

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : AppCommand option =
    try
      match string token.["Case"] with
      | "Undo"        -> Some Undo
      | "Redo"        -> Some Redo
      | "Reset"       -> Some Reset
      | "SaveProject" -> Some SaveProject
      | _             -> None
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : AppCommand option =
    JToken.Parse(str) |> AppCommand.FromJToken

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

#if JAVASCRIPT
type State =
  { Patches  : Dictionary<Id,Patch>
  ; IOBoxes  : Dictionary<Id,IOBox>
  ; Cues     : Dictionary<Id,Cue>
  ; CueLists : Dictionary<Id,CueList>
  ; Nodes    : Dictionary<Id,RaftNode>
  ; Sessions : Dictionary<Id,Session>    // could imagine a BrowserInfo type here with some info on client
  ; Users    : Dictionary<Id,User>
  }
#else
type State =
  { Patches  : Map<Id,Patch>
  ; IOBoxes  : Map<Id,IOBox>
  ; Cues     : Map<Id,Cue>
  ; CueLists : Map<Id,CueList>
  ; Nodes    : Map<Id,RaftNode>
  ; Sessions : Map<Id,Session>    // could imagine a BrowserInfo type here with some info on client
  ; Users    : Map<Id,User>
  }
#endif

#if JAVASCRIPT
  static member Empty =
    { Patches  = Dictionary<Id,Patch>()
    ; IOBoxes  = Dictionary<Id,IOBox>()
    ; Cues     = Dictionary<Id,Cue>()
    ; Nodes    = Dictionary<Id,RaftNode>()
    ; CueLists = Dictionary<Id,CueList>()
    ; Users    = Dictionary<Id,User>()
    ; Sessions = Dictionary<Id,Session>()
    }
#else
  static member Empty =
    { Patches  = Map.empty
    ; IOBoxes  = Map.empty
    ; Cues     = Map.empty
    ; Nodes    = Map.empty
    ; CueLists = Map.empty
    ; Users    = Map.empty
    ; Sessions = Map.empty }
#endif
  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  member state.AddUser (user: User) =
#if JAVASCRIPT
    // Implement immutability by copying the map with all its keys
    if state.Users.ContainsKey user.Id then
      state
    else
      let users = Dictionary<Id,User>()
      for kv in state.Users do
        users.Add(kv.Key, state.Users.[kv.Key])
      users.Add(user.Id, user)
      { state with Users = users }
#else
    // In .NET
    if Map.containsKey user.Id state.Users then
      state
    else
      let users = Map.add user.Id user state.Users
      { state with Users = users }
#endif

  member state.UpdateUser (user: User) =
#if JAVASCRIPT
    // Implement immutability by copying the map with all its keys
    if state.Users.ContainsKey user.Id then
      let users = Dictionary<Id,User>()
      for kv in state.Users do
        if user.Id = kv.Key then
          users.Add(kv.Key, user)
        else
          users.Add(kv.Key, state.Users.[kv.Key])
      { state with Users = users }
    else
      state
#else
    if Map.containsKey user.Id state.Users then
      let users = Map.add user.Id user state.Users
      { state with Users = users }
    else
      state
#endif

  member state.RemoveUser (user: User) =
#if JAVASCRIPT
    // Implement immutability by copying the map with all its keys
    if state.Users.ContainsKey user.Id then
      let users = Dictionary<Id,User>()
      for kv in state.Users do
        if kv.Key <> user.Id then
          users.Add(kv.Key, state.Users.[kv.Key])
      { state with Users = users }
    else
      state
#else
    { state with Users = Map.filter (fun k _ -> (k <> user.Id)) state.Users }
#endif

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  member state.AddSession (session: Session) =
#if JAVASCRIPT
    // Implement immutability by copying the map with all its keys
    if state.Sessions.ContainsKey session.Id  then
      state
    else
      let sessions = Dictionary<Id,Session>()
      for kv in state.Sessions do
        sessions.Add(kv.Key, state.Sessions.[kv.Key])
      sessions.Add(session.Id, session)
      { state with Sessions = sessions }
#else
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        state.Sessions
      else
        Map.add session.Id session state.Sessions
    { state with Sessions = sessions }
#endif

  member state.UpdateSession (session: Session) =
#if JAVASCRIPT
    // Implement immutability by copying the map with all its keys
    if state.Sessions.ContainsKey session.Id  then
      let sessions = Dictionary<Id,Session>()
      for kv in state.Sessions do
        if session.Id = kv.Key then
          sessions.Add(kv.Key, session)
        else
          sessions.Add(kv.Key, state.Sessions.[kv.Key])
      { state with Sessions = sessions }
    else
      state
#else
    let sessions =
      if Map.containsKey session.Id state.Sessions then
        Map.add session.Id session state.Sessions
      else
        state.Sessions
    { state with Sessions = sessions }
#endif

  member state.RemoveSession (session: Session) =
#if JAVASCRIPT
    if state.Sessions.ContainsKey session.Id  then
      let sessions = Dictionary<Id,Session>()
      for kv in state.Sessions do
        if session.Id <> kv.Key then
          sessions.Add(kv.Key, state.Sessions.[kv.Key])
      { state with Sessions = sessions }
    else
      state
#else
    { state with Sessions = Map.filter (fun k _ -> (k <> session.Id)) state.Sessions }
#endif

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  member state.AddPatch (patch : Patch) =
#if JAVASCRIPT
    if state.Patches.ContainsKey patch.Id then
      state
    else
      let patches = Dictionary<Id,Patch>()
      for kv in state.Patches do
        patches.Add(kv.Key, kv.Value)
      patches.Add(patch.Id, patch)
      { state with Patches = patches }
#else
    if Map.containsKey patch.Id state.Patches then
      state
    else
      { state with Patches = Map.add patch.Id patch state.Patches }
#endif

  member state.UpdatePatch (patch : Patch) =
#if JAVASCRIPT
    if state.Patches.ContainsKey patch.Id then
      let patches = Dictionary<Id,Patch>()
      for kv in state.Patches do
        if patch.Id = kv.Key then
          patches.Add(kv.Key, patch)
        else
          patches.Add(kv.Key, kv.Value)
      { state with Patches = patches }
    else
      state
#else
    if Map.containsKey patch.Id state.Patches then
      { state with Patches = Map.add patch.Id patch state.Patches }
    else
      state
#endif

  member state.RemovePatch (patch : Patch) =
#if JAVASCRIPT
    if state.Patches.ContainsKey patch.Id then
      let patches = Dictionary<Id,Patch>()
      for kv in state.Patches do
        if patch.Id <> kv.Key then
          patches.Add(kv.Key, kv.Value)
      { state with Patches = patches }
    else
      state
#else
    { state with Patches = Map.remove patch.Id state.Patches }
#endif

  //  ___ ___  ____
  // |_ _/ _ \| __ )  _____  __
  //  | | | | |  _ \ / _ \ \/ /
  //  | | |_| | |_) | (_) >  <
  // |___\___/|____/ \___/_/\_\

  member state.AddIOBox (iobox : IOBox) =
#if JAVASCRIPT
    if state.Patches.ContainsKey iobox.Patch then
      let patch = state.Patches.[iobox.Patch]

      if Patch.HasIOBox patch iobox.Id then
        state
      else
        let patches = Dictionary<Id,Patch>()
        for kv in state.Patches do
          if kv.Key = patch.Id then
            let updated = Patch.AddIOBox patch iobox
            patches.Add(patch.Id, updated)
          else
            patches.Add(kv.Key, kv.Value)
        { state with Patches = patches }
    else
      state
#else
    if Map.containsKey iobox.Patch state.Patches then
      let update k (patch: Patch) =
        if patch.Id = iobox.Patch then
          Patch.AddIOBox patch iobox
        else
          patch
      { state with Patches = Map.map update state.Patches }
    else
      state
#endif

  member state.UpdateIOBox (iobox : IOBox) =
#if JAVASCRIPT
    if state.Patches.ContainsKey iobox.Patch then
      let patch = state.Patches.[iobox.Patch]

      if Patch.HasIOBox patch iobox.Id then
        let patches = Dictionary<Id,Patch>()
        for kv in state.Patches do
          if kv.Key = patch.Id then
            patches.Add(patch.Id, Patch.UpdateIOBox patch iobox)
          else
            patches.Add(kv.Key, kv.Value)

        { state with Patches = patches }
      else
        state
    else
      state
#else
    let mapper (id: Id) (patch : Patch) =
      if patch.Id = iobox.Patch then
        Patch.UpdateIOBox patch iobox
      else
        patch
    { state with Patches = Map.map mapper state.Patches }
#endif

  member state.RemoveIOBox (iobox : IOBox) =
#if JAVASCRIPT
    if state.Patches.ContainsKey iobox.Patch then
      let patches = Dictionary<Id,Patch>()
      for kv in state.Patches do
        if kv.Key = iobox.Patch then
          patches.Add(kv.Key, Patch.RemoveIOBox kv.Value iobox)
        else
          patches.Add(kv.Key, kv.Value)
      { state with Patches = patches }
    else
      state
#else
    let updater _ (patch : Patch) =
      if iobox.Patch = patch.Id
      then Patch.RemoveIOBox patch iobox
      else patch
    { state with Patches = Map.map updater state.Patches }
#endif

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_ ___
  // | |  | | | |/ _ \ |   | / __| __/ __|
  // | |__| |_| |  __/ |___| \__ \ |_\__ \
  //  \____\__,_|\___|_____|_|___/\__|___/

  member state.AddCueList (cuelist : CueList) =
#if JAVASCRIPT
    if state.CueLists.ContainsKey cuelist.Id then
      state
    else
      let cuelists = Dictionary<Id,CueList>()
      for kv in state.CueLists do
        cuelists.Add(kv.Key, kv.Value)
      cuelists.Add(cuelist.Id, cuelist)
      { state with CueLists = cuelists }
#else
    if Map.containsKey cuelist.Id state.CueLists then
      state
    else
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }
#endif

  member state.UpdateCueList (cuelist : CueList) =
#if JAVASCRIPT
    if state.CueLists.ContainsKey cuelist.Id then
      let cuelists = Dictionary<Id,CueList>()
      for kv in state.CueLists do
        if kv.Key = cuelist.Id then
          cuelists.Add(cuelist.Id, cuelist)
        else
          cuelists.Add(kv.Key, kv.Value)
      { state with CueLists = cuelists }
    else
      state
#else
    if Map.containsKey cuelist.Id state.CueLists then
      { state with CueLists = Map.add cuelist.Id cuelist state.CueLists }
    else
      state
#endif

  member state.RemoveCueList (cuelist : CueList) =
#if JAVASCRIPT
    if state.CueLists.ContainsKey cuelist.Id then
      let cuelists = Dictionary<Id,CueList>()
      for kv in state.CueLists do
        if kv.Key <> cuelist.Id then
          cuelists.Add(kv.Key, kv.Value)
      { state with CueLists = cuelists }
    else
      state
#else
    { state with CueLists = Map.remove cuelist.Id state.CueLists }
#endif

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  member state.AddCue (cue : Cue) =
#if JAVASCRIPT
    if state.Cues.ContainsKey cue.Id then
      state
    else
      let cues = Dictionary<Id,Cue>()
      for kv in state.Cues do
        cues.Add(kv.Key, kv.Value)
      cues.Add(cue.Id, cue)
      { state with Cues = cues }
#else
    if Map.containsKey cue.Id state.Cues then
      state
    else
      { state with Cues = Map.add cue.Id cue state.Cues }
#endif

  member state.UpdateCue (cue : Cue) =
#if JAVASCRIPT
    if state.Cues.ContainsKey cue.Id then
      let cues = Dictionary<Id,Cue>()
      for kv in state.Cues do
        if kv.Key = cue.Id then
          cues.Add(cue.Id, cue)
        else
          cues.Add(kv.Key, kv.Value)
      { state with Cues = cues }
    else
      state
#else
    if Map.containsKey cue.Id state.Cues then
      { state with Cues = Map.add cue.Id cue state.Cues }
    else
      state
#endif

  member state.RemoveCue (cue : Cue) =
#if JAVASCRIPT
    if state.Cues.ContainsKey cue.Id then
      let cues = Dictionary<Id,Cue>()
      for kv in state.Cues do
        if kv.Key <> cue.Id then
          cues.Add(kv.Key, kv.Value)
      { state with Cues = cues }
    else
      state
#else
    { state with Cues = Map.remove cue.Id state.Cues }
#endif

  //  _   _           _
  // | \ | | ___   __| | ___
  // |  \| |/ _ \ / _` |/ _ \
  // | |\  | (_) | (_| |  __/
  // |_| \_|\___/ \__,_|\___|

  member state.AddNode (node: RaftNode) =
#if JAVASCRIPT
    if state.Nodes.ContainsKey node.Id then
      state
    else
      let nodes = Dictionary<Id,RaftNode>()
      for kv in state.Nodes do
        nodes.Add(kv.Key, kv.Value)
      nodes.Add(node.Id, node)
      { state with Nodes = nodes }
#else
    if Map.containsKey node.Id state.Nodes then
      state
    else
      { state with Nodes = Map.add node.Id node state.Nodes }
#endif

  member state.UpdateNode (node: RaftNode) =
#if JAVASCRIPT
    if state.Nodes.ContainsKey node.Id then
      let nodes = Dictionary<Id,RaftNode>()
      for kv in state.Nodes do
        if kv.Key = node.Id then
          nodes.Add(node.Id, node)
        else
          nodes.Add(kv.Key, kv.Value)
      { state with Nodes = nodes }
    else
      state
#else
    if Map.containsKey node.Id state.Nodes then
      { state with Nodes = Map.add node.Id node state.Nodes }
    else
      state
#endif

  member state.RemoveNode (node: RaftNode) =
#if JAVASCRIPT
    if state.Nodes.ContainsKey node.Id then
      let nodes = Dictionary<Id,RaftNode>()
      for kv in state.Nodes do
        if kv.Key <> node.Id then
          nodes.Add(kv.Key, kv.Value)
      { state with Nodes = nodes }
    else
      state
#else
    { state with Nodes = Map.remove node.Id state.Nodes }
#endif

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
      fb.GetPatches(i)
      |> Patch.FromFB
      |> Option.map (fun patch -> patches <- Map.add patch.Id patch patches)
      |> ignore

    for i in 0 .. (fb.IOBoxesLength - 1) do
      fb.GetIOBoxes(i)
      |> IOBox.FromFB
      |> Option.map (fun iobox -> ioboxes <- Map.add iobox.Id iobox ioboxes)
      |> ignore

    for i in 0 .. (fb.CuesLength - 1) do
      fb.GetCues(i)
      |> Cue.FromFB
      |> Option.map (fun cue -> cues <- Map.add cue.Id cue cues)
      |> ignore

    for i in 0 .. (fb.CueListsLength - 1) do
      fb.GetCueLists(i)
      |> CueList.FromFB
      |> Option.map (fun cuelist -> cuelists <- Map.add cuelist.Id cuelist cuelists)
      |> ignore

    for i in 0 .. (fb.NodesLength - 1) do
      fb.GetNodes(i)
      |> RaftNode.FromFB
      |> Option.map (fun node -> nodes <- Map.add node.Id node nodes)
      |> ignore

    for i in 0 .. (fb.UsersLength - 1) do
      fb.GetUsers(i)
      |> User.FromFB
      |> Option.map (fun user -> users <- Map.add user.Id user users)
      |> ignore

    for i in 0 .. (fb.SessionsLength - 1) do
      fb.GetSessions(i)
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

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() =
    new JObject()
    |> addMap "Patches"  self.Patches
    |> addMap "IOBoxes"  self.IOBoxes
    |> addMap "Cues"     self.Cues
    |> addMap "CueLists" self.CueLists
    |> addMap "Nodes"    self.Nodes
    |> addMap "Users"    self.Users
    |> addMap "Sessions" self.Sessions

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : State option =
    try
      Some { Patches  = fromDict "Patches"  token
           ; IOBoxes  = fromDict "IOBoxes"  token
           ; Cues     = fromDict "Cues"     token
           ; CueLists = fromDict "CueLists" token
           ; Nodes    = fromDict "Nodes"    token
           ; Users    = fromDict "Users"    token
           ; Sessions = fromDict "Sessions" token }
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : State option =
    JToken.Parse(str) |> State.FromJToken

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

  let reducer (ev : StateMachine) (state : State) =
    match ev with
    | AddCue                cue -> state.AddCue        cue
    | UpdateCue             cue -> state.UpdateCue     cue
    | RemoveCue             cue -> state.RemoveCue     cue

    | AddCueList        cuelist -> state.AddCueList    cuelist
    | UpdateCueList     cuelist -> state.UpdateCueList cuelist
    | RemoveCueList     cuelist -> state.RemoveCueList cuelist

    | AddPatch            patch -> state.AddPatch      patch
    | UpdatePatch         patch -> state.UpdatePatch   patch
    | RemovePatch         patch -> state.RemovePatch   patch

    | AddIOBox            iobox -> state.AddIOBox      iobox
    | UpdateIOBox         iobox -> state.UpdateIOBox   iobox
    | RemoveIOBox         iobox -> state.RemoveIOBox   iobox

    | AddNode              node -> state.AddNode       node
    | UpdateNode           node -> state.UpdateNode    node
    | RemoveNode           node -> state.RemoveNode    node

    | AddSession        session -> state.AddSession    session
    | UpdateSession     session -> state.UpdateSession session
    | RemoveSession     session -> state.RemoveSession session

    | AddUser              user -> state.AddUser       user
    | UpdateUser           user -> state.UpdateUser    user
    | RemoveUser           user -> state.RemoveUser    user

    | _                         -> state

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
    match ev with
    | Command (AppCommand.Redo)  -> self.Redo()
    | Command (AppCommand.Undo)  -> self.Undo()
    | Command (AppCommand.Reset) -> ()   // do nothing for now
    | _ ->
      state <- reducer ev state          // 1) create new state
      self.Notify(ev)                   // 2) notify all listeners (render as soon as possible)
      history.Append({ Event = ev       // 3) store this action the and state it produced
                      ; State = state }) // 4) append to undo history

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

  static member Type
    with get () = Serialization.GetTypeName<StateMachine>()

  static member FromFB (fb: StateMachineFB) =
    match fb.AppEventType with

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | StateMachineTypeFB.AddCueFB ->
      let ev = fb.GetAppEvent(new AddCueFB())
      ev.GetCue(new CueFB())
      |> Cue.FromFB
      |> Option.map AddCue

    | StateMachineTypeFB.UpdateCueFB  ->
      let ev = fb.GetAppEvent(new UpdateCueFB())
      ev.GetCue(new CueFB())
      |> Cue.FromFB
      |> Option.map UpdateCue

    | StateMachineTypeFB.RemoveCueFB  ->
      let ev = fb.GetAppEvent(new RemoveCueFB())
      ev.GetCue(new CueFB())
      |> Cue.FromFB
      |> Option.map RemoveCue

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | StateMachineTypeFB.AddCueListFB ->
      let ev = fb.GetAppEvent(new AddCueListFB())
      ev.GetCueList(new CueListFB())
      |> CueList.FromFB
      |> Option.map AddCueList

    | StateMachineTypeFB.UpdateCueListFB  ->
      let ev = fb.GetAppEvent(new UpdateCueListFB())
      ev.GetCueList(new CueListFB())
      |> CueList.FromFB
      |> Option.map UpdateCueList

    | StateMachineTypeFB.RemoveCueListFB  ->
      let ev = fb.GetAppEvent(new RemoveCueListFB())
      ev.GetCueList(new CueListFB())
      |> CueList.FromFB
      |> Option.map RemoveCueList

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | StateMachineTypeFB.AddPatchFB ->
      let ev = fb.GetAppEvent(new AddPatchFB())
      ev.GetPatch(new PatchFB())
      |> Patch.FromFB
      |> Option.map AddPatch

    | StateMachineTypeFB.UpdatePatchFB  ->
      let ev = fb.GetAppEvent(new UpdatePatchFB())
      ev.GetPatch(new PatchFB())
      |> Patch.FromFB
      |> Option.map UpdatePatch

    | StateMachineTypeFB.RemovePatchFB  ->
      let ev = fb.GetAppEvent(new RemovePatchFB())
      ev.GetPatch(new PatchFB())
      |> Patch.FromFB
      |> Option.map RemovePatch

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    | StateMachineTypeFB.AddIOBoxFB ->
      let ev = fb.GetAppEvent(new AddIOBoxFB())
      ev.GetIOBox(new IOBoxFB())
      |> IOBox.FromFB
      |> Option.map AddIOBox

    | StateMachineTypeFB.UpdateIOBoxFB  ->
      let ev = fb.GetAppEvent(new UpdateIOBoxFB())
      ev.GetIOBox(new IOBoxFB())
      |> IOBox.FromFB
      |> Option.map UpdateIOBox

    | StateMachineTypeFB.RemoveIOBoxFB  ->
      let ev = fb.GetAppEvent(new RemoveIOBoxFB())
      ev.GetIOBox(new IOBoxFB())
      |> IOBox.FromFB
      |> Option.map RemoveIOBox

    //  _   _           _
    // | \ | | ___   __| | ___
    // |  \| |/ _ \ / _` |/ _ \
    // | |\  | (_) | (_| |  __/
    // |_| \_|\___/ \__,_|\___|

    | StateMachineTypeFB.AddNodeFB ->
      let ev = fb.GetAppEvent(new AddNodeFB())
      ev.GetNode(new NodeFB())
      |> RaftNode.FromFB
      |> Option.map AddNode

    | StateMachineTypeFB.UpdateNodeFB  ->
      let ev = fb.GetAppEvent(new UpdateNodeFB())
      ev.GetNode(new NodeFB())
      |> RaftNode.FromFB
      |> Option.map UpdateNode

    | StateMachineTypeFB.RemoveNodeFB  ->
      let ev = fb.GetAppEvent(new RemoveNodeFB())
      ev.GetNode(new NodeFB())
      |> RaftNode.FromFB
      |> Option.map RemoveNode

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | StateMachineTypeFB.AddUserFB ->
      let ev = fb.GetAppEvent(new AddUserFB())
      ev.GetUser(new UserFB())
      |> User.FromFB
      |> Option.map AddUser

    | StateMachineTypeFB.UpdateUserFB  ->
      let ev = fb.GetAppEvent(new UpdateUserFB())
      ev.GetUser(new UserFB())
      |> User.FromFB
      |> Option.map UpdateUser

    | StateMachineTypeFB.RemoveUserFB  ->
      let ev = fb.GetAppEvent(new RemoveUserFB())
      ev.GetUser(new UserFB())
      |> User.FromFB
      |> Option.map RemoveUser

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | StateMachineTypeFB.AddSessionFB ->
      let ev = fb.GetAppEvent(new AddSessionFB())
      ev.GetSession(new SessionFB())
      |> Session.FromFB
      |> Option.map AddSession

    | StateMachineTypeFB.UpdateSessionFB  ->
      let ev = fb.GetAppEvent(new UpdateSessionFB())
      ev.GetSession(new SessionFB())
      |> Session.FromFB
      |> Option.map UpdateSession

    | StateMachineTypeFB.RemoveSessionFB  ->
      let ev = fb.GetAppEvent(new RemoveSessionFB())
      ev.GetSession(new SessionFB())
      |> Session.FromFB
      |> Option.map RemoveSession

    //  __  __ _
    // |  \/  (_)___  ___
    // | |\/| | / __|/ __|
    // | |  | | \__ \ (__
    // |_|  |_|_|___/\___|

    | StateMachineTypeFB.LogMsgFB     ->
      let ev = fb.GetAppEvent(new LogMsgFB())
      LogLevel.Parse ev.LogLevel
      |> Option.map (fun level -> LogMsg(level, ev.Msg))

    | StateMachineTypeFB.AppCommandFB ->
      let ev = fb.GetAppEvent(new AppCommandFB())
      AppCommand.FromFB ev
      |> Option.map Command

    | StateMachineTypeFB.DataSnapshotFB ->
      let snapshot = fb.GetAppEvent(new DataSnapshotFB())
      snapshot.GetData(new StateFB())
      |> State.FromFB
      |> Option.map DataSnapshot

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

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

#if JAVASCRIPT

  member self.ToJson () = toJson self

  static member FromJson (str: string) : StateMachine option =
    try
      ofJson<StateMachine> str
      |> Some
    with
      | _ -> None

#else

  member self.ToJToken () =
    let json = new JObject() |> addType StateMachine.Type

    let inline add (case: string) data =
      json |> addCase case |> addFields [| data |]

    match self with
    // NODE
    | AddNode          node -> add "AddNode"    node
    | UpdateNode       node -> add "UpdateNode" node
    | RemoveNode       node -> add "RemoveNode" node

    // PATCH
    | AddPatch        patch -> add "AddPatch"    patch
    | UpdatePatch     patch -> add "UpdatePatch" patch
    | RemovePatch     patch -> add "RemovePatch" patch

    // IOBOX
    | AddIOBox        iobox -> add "AddIOBox"    iobox
    | UpdateIOBox     iobox -> add "UpdateIOBox" iobox
    | RemoveIOBox     iobox -> add "RemoveIOBox" iobox

    // CUE
    | AddCue            cue -> add "AddCue"    cue
    | UpdateCue         cue -> add "UpdateCue" cue
    | RemoveCue         cue -> add "RemoveCue" cue

    // CUELIST
    | AddCueList    cuelist -> add "AddCueList"    cuelist
    | UpdateCueList cuelist -> add "UpdateCueList" cuelist
    | RemoveCueList cuelist -> add "RemoveCueList" cuelist

    // USER
    | AddUser          user -> add "AddUser"    user
    | UpdateUser       user -> add "UpdateUser" user
    | RemoveUser       user -> add "RemoveUser" user

    // SESSION
    | AddSession    session -> add "AddSession"    session
    | UpdateSession session -> add "UpdateSession" session
    | RemoveSession session -> add "RemoveSession" session

    | Command           cmd -> add "Command" cmd

    | DataSnapshot     data -> add "DataSnapshot" data

    | LogMsg (level, str) ->
      json |> addCase "LogMsg" |> addFields [| Wrap(string level); Wrap(str) |]

  member self.ToJson () =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : StateMachine option =
    try
      let fields = token.["Fields"] :?> JArray

      let inline parseSingle (cnst: ^t -> StateMachine) =
        Json.parse fields.[0]
        |> Option.map cnst

      match string token.["Case"] with
      // NODE
      | "AddNode"       -> parseSingle AddNode
      | "UpdateNode"    -> parseSingle UpdateNode
      | "RemoveNode"    -> parseSingle RemoveNode

      | "AddPatch"      -> parseSingle AddPatch
      | "UpdatePatch"   -> parseSingle UpdatePatch
      | "RemovePatch"   -> parseSingle RemovePatch

      | "AddIOBox"      -> parseSingle AddIOBox
      | "UpdateIOBox"   -> parseSingle UpdateIOBox
      | "RemoveIOBox"   -> parseSingle RemoveIOBox

      | "AddCue"        -> parseSingle AddCue
      | "UpdateCue"     -> parseSingle UpdateCue
      | "RemoveCue"     -> parseSingle RemoveCue

      | "AddCueList"    -> parseSingle AddCueList
      | "UpdateCueList" -> parseSingle UpdateCueList
      | "RemoveCueList" -> parseSingle RemoveCueList

      | "AddUser"       -> parseSingle AddUser
      | "UpdateUser"    -> parseSingle UpdateUser
      | "RemoveUser"    -> parseSingle RemoveUser

      | "AddSession"    -> parseSingle AddSession
      | "UpdateSession" -> parseSingle UpdateSession
      | "RemoveSession" -> parseSingle RemoveSession

      | "Command"       -> parseSingle Command

      | "DataSnapshot"  -> parseSingle DataSnapshot

      | "LogMsg" ->
        Json.parse fields.[0]
        |> Option.map (fun level -> LogMsg (level,string fields.[1]))

      | _ -> None
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : StateMachine option =
    JToken.Parse(str) |> StateMachine.FromJToken

#endif
