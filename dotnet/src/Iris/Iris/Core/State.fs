namespace Iris.Core

[<AutoOpen>]
module State =

#if JAVASCRIPT
  open Fable.Core
  open Fable.Core.JsInterop
  open Fable.Import
  open Fable.Import.JS
  open System.Collections.Generic
#endif

  open Iris.Raft

  (*   ____  _        _
      / ___|| |_ __ _| |_ ___
      \___ \| __/ _` | __/ _ \
       ___) | || (_| | ||  __/
      |____/ \__\__,_|\__\___|

      Record type containing all the actual data that gets passed around in our
      application.
  *)
#if JAVASCRIPT
  type State =
    { Patches  : Dictionary<Id,Patch>
    ; IOBoxes  : Dictionary<Id,IOBox>
    ; Cues     : Dictionary<Id,Cue>
    ; CueLists : Dictionary<Id,CueList>
    ; Nodes    : Dictionary<Id,RaftNode>
    ; Sessions : Dictionary<Id,Session>    // could imagine a BrowserInfo type here with some info on client
    ; Users    : Dictionary<Name,User>
    }
#else
  type State =
    { Patches  : Map<Id,Patch>
    ; IOBoxes  : Map<Id,IOBox>
    ; Cues     : Map<Id,Cue>
    ; CueLists : Map<Id,CueList>
    ; Nodes    : Map<Id,RaftNode>
    ; Sessions : Map<Id,Session>    // could imagine a BrowserInfo type here with some info on client
    ; Users    : Map<Name,User>
    }
#endif

#if JAVASCRIPT
    static member Empty =
      { Patches  = Dictionary<Id,Patch>()
      ; IOBoxes  = Dictionary<Id,IOBox>()
      ; Cues     = Dictionary<Id,Cue>()
      ; Nodes    = Dictionary<Id,RaftNode>()
      ; CueLists = Dictionary<Id,CueList>()
      ; Users    = Dictionary<Name,User>()
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
      if state.Users.ContainsKey user.UserName then
        state
      else
        let users = Dictionary<Name,User>()
        for kv in state.Users do
          users.Add(kv.Key, state.Users.[kv.Key])
        users.Add(user.UserName, user)
        { state with Users = users }
#else
      // In .NET
      if Map.containsKey user.UserName state.Users then
        state
      else
        let users = Map.add user.UserName user state.Users
        { state with Users = users }
#endif

    member state.UpdateUser (user: User) =
#if JAVASCRIPT
      // Implement immutability by copying the map with all its keys
      if state.Users.ContainsKey user.UserName then
        let users = Dictionary<Name,User>()
        for kv in state.Users do
          if user.UserName = kv.Key then
            users.Add(kv.Key, user)
          else
            users.Add(kv.Key, state.Users.[kv.Key])
        { state with Users = users }
      else
        state
#else
      if Map.containsKey user.UserName state.Users then
        let users = Map.add user.UserName user state.Users
        { state with Users = users }
      else
        state
#endif

    member state.RemoveUser (user: User) =
#if JAVASCRIPT
      // Implement immutability by copying the map with all its keys
      if state.Users.ContainsKey user.UserName then
        let users = Dictionary<Name,User>()
        for kv in state.Users do
          if kv.Key <> user.UserName then
            users.Add(kv.Key, state.Users.[kv.Key])
        { state with Users = users }
      else
        state
#else
      { state with Users = Map.filter (fun k _ -> (k <> user.UserName)) state.Users }
#endif

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    member state.AddSession (session: Session) =
#if JAVASCRIPT
      // Implement immutability by copying the map with all its keys
      if state.Sessions.ContainsKey session.SessionId  then
        state
      else
        let sessions = Dictionary<Id,Session>()
        for kv in state.Sessions do
          sessions.Add(kv.Key, state.Sessions.[kv.Key])
        sessions.Add(session.SessionId, session)
        { state with Sessions = sessions }
#else
      let sessions =
        if Map.containsKey session.SessionId state.Sessions then
          state.Sessions
        else
          Map.add session.SessionId session state.Sessions
      { state with Sessions = sessions }
#endif

    member state.UpdateSession (session: Session) =
#if JAVASCRIPT
      // Implement immutability by copying the map with all its keys
      if state.Sessions.ContainsKey session.SessionId  then
        let sessions = Dictionary<Id,Session>()
        for kv in state.Sessions do
          if session.SessionId = kv.Key then
            sessions.Add(kv.Key, session)
          else
            sessions.Add(kv.Key, state.Sessions.[kv.Key])
        { state with Sessions = sessions }
      else
        state
#else
      let sessions =
        if Map.containsKey session.SessionId state.Sessions then
          Map.add session.SessionId session state.Sessions
        else
          state.Sessions
      { state with Sessions = sessions }
#endif

    member state.RemoveSession (session: Session) =
#if JAVASCRIPT
      if state.Sessions.ContainsKey session.SessionId  then
        let sessions = Dictionary<Id,Session>()
        for kv in state.Sessions do
          if session.SessionId <> kv.Key then
            sessions.Add(kv.Key, state.Sessions.[kv.Key])
        { state with Sessions = sessions }
      else
        state
#else
      { state with Sessions = Map.filter (fun k _ -> (k <> session.SessionId)) state.Sessions }
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
