namespace Iris.Core

[<AutoOpen>]
module State =

#if JAVASCRIPT
  open Fable.Core
  open Fable.Core.JsInterop
  open Fable.Import
  open Fable.Import.JS
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

  type State =
    { Patches  : Map<Id,Patch>
    ; Cues     : Map<Id,Cue>
    ; CueLists : Map<Id,CueList>
    ; Nodes    : Map<Id,RaftNode>
    ; Sessions : Map<Id,Session>    // could imagine a BrowserInfo type here with some info on client
    ; Users    : Map<Name,User>
    }

#if JAVASCRIPT
    static member Empty =
      { Patches  = Map.Create<Id,Patch>()
      ; Cues     = Map.Create<Id,Cue>()
      ; Nodes    = Map.Create<Id,Nodes>()
      ; CueLists = Map.Create<Id,CueList>()
      ; Users    = Map.Create<Name,User>()
      ; Sessions = Map.Create<Id,Session>()
      }
#else
    static member Empty =
      { Patches  = Map.empty
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
      let users =
        if Map.containsKey user.UserName state.Users then
          state.Users
        else
          Map.add user.UserName user state.Users
      { state with Users = users }

    member state.UpdateUser (user: User) =
      let users =
        if Map.containsKey user.UserName state.Users then
          Map.add user.UserName user state.Users
        else
          state.Users
      { state with Users = users }

    member state.RemoveUser (user: User) =
      { state with Users = Map.filter (fun k _ -> (k <> user.UserName)) state.Users }

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    member state.AddSession (session: Session) =
      let sessions =
        if Map.containsKey session.SessionId state.Sessions then
          state.Sessions
        else
          Map.add session.SessionId session state.Sessions
      { state with Sessions = sessions }

    member state.UpdateSession (session: Session) =
      let sessions =
        if Map.containsKey session.SessionId state.Sessions then
          Map.add session.SessionId session state.Sessions
        else
          state.Sessions
      { state with Sessions = sessions }

    member state.RemoveSession (session: Session) =
      { state with Sessions = Map.filter (fun k _ -> (k <> session.SessionId)) state.Sessions }

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
      let updater (id: Id) (patch : Patch) =
        if iobox.Patch = id then
          Patch.AddIOBox patch iobox
        else patch
      { state with Patches = Map.map updater state.Patches }

    member state.UpdateIOBox (iobox : IOBox) =
      let mapper (id: Id) (patch : Patch) = Patch.UpdateIOBox patch iobox
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
