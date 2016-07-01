namespace Pallet.Core

open System
open System.Net

//  _   _           _      ____  _        _
// | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___
// |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \
// | |\  | (_) | (_| |  __/___) | || (_| | ||  __/
// |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|

type NodeState =
  | Joining                             // excludes node from voting
  | Running                             // normal execution state
  | Failed                              // node has failed for some reason

//  _   _           _
// | \ | | ___   __| | ___
// |  \| |/ _ \ / _` |/ _ \
// | |\  | (_) | (_| |  __/
// |_| \_|\___/ \__,_|\___|

type Node< 'node, ^id when ^id : (static member Create : unit -> ^id) > =
  { Id         : ^id
  ; Data       : 'node
  ; Voting     : bool
  ; VotedForMe : bool
  ; State      : NodeState
  ; NextIndex  : Index
  ; MatchIndex : Index
  }


//   ____             __ _          ____ _
//  / ___|___  _ __  / _(_) __ _   / ___| |__   __ _ _ __   __ _  ___
// | |   / _ \| '_ \| |_| |/ _` | | |   | '_ \ / _` | '_ \ / _` |/ _ \
// | |__| (_) | | | |  _| | (_| | | |___| | | | (_| | | | | (_| |  __/
//  \____\___/|_| |_|_| |_|\__, |  \____|_| |_|\__,_|_| |_|\__, |\___|
//                         |___/                           |___/

type ConfigChange<'node, ^id when ^id : (static member Create : unit -> ^id)> =
  | NodeAdded   of Node<'node,^id>
  | NodeRemoved of Node<'node,^id>

[<RequireQualifiedAccess>]
module Node =

  let inline create data =
    let id = (^id : (static member Create : unit -> ^id) ())
    { Id         = id
    ; Data       = data
    ; State      = Running
    ; Voting     = true
    ; VotedForMe = false
    ; NextIndex  = 1UL
    ; MatchIndex = 0UL
    }

  let inline isVoting (node : Node<_,_>) : bool =
    node.State = Running && node.Voting

  let inline setVoting node voting =
    { node with Voting = voting }

  let inline voteForMe node vote =
    { node with VotedForMe = vote }

  let inline hasVoteForMe node = node.VotedForMe

  let inline setHasSufficientLogs node =
    { node with
        State = Running
        Voting = true }

  let inline hasSufficientLogs node =
    node.State = Running

  let inline canVote peer =
    isVoting peer && hasVoteForMe peer && peer.State = Running

  let inline getId node = node.Id
  let inline getData node = node.Data
  let inline getState node = node.State
  let inline getNextIndex  node = node.NextIndex
  let inline getMatchIndex node = node.MatchIndex

  let inline private added oldnodes newnodes =
    let folder changes (node: Node<_,_>) =
      match Array.tryFind (getId >> ((=) node.Id)) oldnodes with
        | Some _ -> changes
        | _ -> NodeAdded(node) :: changes
    Array.fold folder List.empty newnodes

  let inline private removed oldnodes newnodes =
    let folder changes (node: Node<_,_>) =
      match Array.tryFind (getId >> ((=) node.Id)) newnodes with
        | Some _ -> changes
        | _ -> NodeAdded(node) :: changes
    Array.fold folder List.empty oldnodes

  let inline changes (oldnodes: Node<_,_> array) (newnodes: Node<_,_> array) =
    List.empty
    |> List.append (added oldnodes newnodes)
    |> List.append (removed oldnodes newnodes)
    |> Array.ofList
