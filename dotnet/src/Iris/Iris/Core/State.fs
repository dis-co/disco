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
    { Patches : Patch    array
    ; Cues    : Cue      array
    ; Nodes   : RaftNode array
    ; Users   : Map<Name,Email>
    }
    static member Empty =
      { Patches = [| |]
      ; Cues    = [| |]
      ; Nodes   = [| |]
      ; Users   = Map.empty }

    (*  ADD  *)
    member state.AddPatch (patch : Patch) =
      let exists = Array.exists (fun (p : Patch) -> p.Id = patch.Id) state.Patches
      if exists then
        state
      else
        let patches' = Array.copy state.Patches // copy the array
        { state with Patches = Array.append patches' [| patch |]  }

    member state.AddIOBox (iobox : IOBox) =
      let updater (patch : Patch) =
        if iobox.Patch = patch.Id then
          Patch.AddIOBox patch iobox
        else patch
      { state with Patches = Array.map updater state.Patches }

    member state.AddCue (cue : Cue) =
      let copy = Array.copy state.Cues
      { state with Cues =  Array.append copy [| cue |] }

    member state.AddNode (node: RaftNode) =
      let copy = Array.copy state.Nodes
      { state with Nodes =  Array.append copy [| node |] }

    (*  UPDATE  *)
    member state.UpdatePatch (patch : Patch) =
      { state with
          Patches = let mapper (oldpatch : Patch) =
                        if patch.Id = oldpatch.Id
                        then patch
                        else oldpatch
                    in Array.map mapper state.Patches }

    member state.UpdateIOBox (iobox : IOBox) =
      let mapper (patch : Patch) = Patch.UpdateIOBox patch iobox
      { state with Patches = Array.map mapper state.Patches }

    member state.UpdateCue (cue : Cue) =
      let mapper (cue' : Cue) =
        if cue.Id = cue'.Id then cue' else cue
      { state with Cues = Array.map mapper state.Cues }

    member state.UpdateNode (node: RaftNode) =
      let mapper (_node : RaftNode) =
        if _node.Id = node.Id then node else _node
      { state with Nodes = Array.map mapper state.Nodes }

    (* REMOVE *)
    member state.RemovePatch (patch : Patch) =
      let pred (patch' : Patch) = patch.Id <> patch'.Id
      { state with Patches = Array.filter pred state.Patches }

    member state.RemoveCue (cue : Cue) =
      let pred (cue' : Cue) = cue.Id <> cue'.Id
      { state with Cues = Array.filter pred state.Cues }

    member state.RemoveIOBox (iobox : IOBox) =
      let updater (patch : Patch) =
        if iobox.Patch = patch.Id
        then Patch.RemoveIOBox patch iobox
        else patch
      { state with Patches = Array.map updater state.Patches }

    member state.RemoveNode (node: RaftNode) =
      { state with Nodes = Array.filter (fun other -> other.Id <> node.Id) state.Nodes }
