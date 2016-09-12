namespace Iris.Core

[<AutoOpen>]
module State =

#if JAVASCRIPT
  open Fable.Core
  open Fable.Import
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
    { Patches  : Patch    array
    ; Cues     : Cue      array
    ; Nodes    : RaftNode array
    ; Sessions : Map<Session,string>    // could imagine a BrowserInfo type here with some info on client
    ; Users    : Map<Name,Email>
    }
    static member Empty =
      { Patches = [| |]
      ; Cues    = [| |]
      ; Nodes   = [| |]
      ; Users   = Map.empty }

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    member state.AddUser (name: Name) (email: Email) =
      let users =
        if Map.contains name state.Users then
          state.Users
        else
          Map.add name email state.Users
      { state with Users = users }

    member state.UpdateUser (name: Name) (email: Email) =
      let users =
        if Map.contains name state.Users then
          Map.add name email state.Users
        else
          state.Users
      { state with Users = users }

    member state.RemoveUser (name: Name) =
      { state with Users = Map.filter (fun k _ -> (k <> name)) state.Users }

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    member state.AddSession (session: Session) (ua: string) =
      let sessions =
        if Map.contains session state.Sessions then
          state.Sessions
        else
          Map.add session ua state.Sessions
      { state with Sessions = sessions }

    member state.UpdateSession (session: Session) (ua: string) =
      let sessions =
        if Map.contains session state.Sessions then
          Map.add session ua state.Sessions
        else
          state.Sessions
      { state with Sessions = sessions }

    member state.RemoveSession (session: Session) =
      { state with Sessions = Map.filter (fun k _ -> (k <> session)) state.Sessions }

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    member state.AddPatch (patch : Patch) =
      let exists = Array.exists (fun (p : Patch) -> p.Id = patch.Id) state.Patches
      if exists then
        state
      else
        let patches' = Array.copy state.Patches // copy the array
        { state with Patches = Array.append patches' [| patch |]  }

    member state.UpdatePatch (patch : Patch) =
      { state with
          Patches = let mapper (oldpatch : Patch) =
                        if patch.Id = oldpatch.Id
                        then patch
                        else oldpatch
                    in Array.map mapper state.Patches }

    member state.RemovePatch (patch : Patch) =
      let pred (patch' : Patch) = patch.Id <> patch'.Id
      { state with Patches = Array.filter pred state.Patches }

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    member state.AddIOBox (iobox : IOBox) =
      let updater (patch : Patch) =
        if iobox.Patch = patch.Id then
          Patch.AddIOBox patch iobox
        else patch
      { state with Patches = Array.map updater state.Patches }

    member state.UpdateIOBox (iobox : IOBox) =
      let mapper (patch : Patch) = Patch.UpdateIOBox patch iobox
      { state with Patches = Array.map mapper state.Patches }

    member state.RemoveIOBox (iobox : IOBox) =
      let updater (patch : Patch) =
        if iobox.Patch = patch.Id
        then Patch.RemoveIOBox patch iobox
        else patch
      { state with Patches = Array.map updater state.Patches }

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    member state.AddCue (cue : Cue) =
      let copy = Array.copy state.Cues
      { state with Cues =  Array.append copy [| cue |] }

    member state.UpdateCue (cue : Cue) =
      let mapper (cue' : Cue) =
        if cue.Id = cue'.Id then cue' else cue
      { state with Cues = Array.map mapper state.Cues }

    member state.RemoveCue (cue : Cue) =
      let pred (cue' : Cue) = cue.Id <> cue'.Id
      { state with Cues = Array.filter pred state.Cues }

    //  _   _           _
    // | \ | | ___   __| | ___
    // |  \| |/ _ \ / _` |/ _ \
    // | |\  | (_) | (_| |  __/
    // |_| \_|\___/ \__,_|\___|

    member state.AddNode (node: RaftNode) =
      let copy = Array.copy state.Nodes
      { state with Nodes =  Array.append copy [| node |] }

    member state.UpdateNode (node: RaftNode) =
      let mapper (_node : RaftNode) =
        if _node.Id = node.Id then node else _node
      { state with Nodes = Array.map mapper state.Nodes }

    member state.RemoveNode (node: RaftNode) =
      { state with Nodes = Array.filter (fun other -> other.Id <> node.Id) state.Nodes }
