namespace Iris.Raft

open System
open System.Net
open Iris.Core

//  _   _           _      ____  _        _
// | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___
// |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \
// | |\  | (_) | (_| |  __/___) | || (_| | ||  __/
// |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|

type NodeState =
  | Joining                             // excludes node from voting
  | Running                             // normal execution state
  | Failed                              // node has failed for some reason

  with
    override self.ToString() =
      match self with
      | Joining -> "Joining"
      | Running -> "Running"
      | Failed  -> "Failed"

    static member Parse (str: string) =
      match str with
      | "Joining" -> Joining
      | "Running" -> Running
      | "Failed"  -> Failed
      | _         -> failwithf "NodeState: failed to parse %s" str

//  _   _           _
// | \ | | ___   __| | ___
// |  \| |/ _ \ / _` |/ _ \
// | |\  | (_) | (_| |  __/
// |_| \_|\___/ \__,_|\___|

type Node<'node> =
  { Id         : NodeId
  ; Data       : 'node
  ; Voting     : bool
  ; VotedForMe : bool
  ; State      : NodeState
  ; NextIndex  : Index
  ; MatchIndex : Index
  }

  override self.ToString() =
    sprintf "%s %s %s %s"
      (string self.Id)
      (string self.State)
      (sprintf "(NxtIdx %A)" self.NextIndex)
      (sprintf "(MtchIdx %A)" self.MatchIndex)

//   ____             __ _          ____ _
//  / ___|___  _ __  / _(_) __ _   / ___| |__   __ _ _ __   __ _  ___
// | |   / _ \| '_ \| |_| |/ _` | | |   | '_ \ / _` | '_ \ / _` |/ _ \
// | |__| (_) | | | |  _| | (_| | | |___| | | | (_| | | | | (_| |  __/
//  \____\___/|_| |_|_| |_|\__, |  \____|_| |_|\__,_|_| |_|\__, |\___|
//                         |___/                           |___/

type ConfigChange<'n> =
  | NodeAdded   of Node<'n>
  | NodeRemoved of Node<'n>

  override self.ToString() =
    match self with
    | NodeAdded   n -> sprintf "NodeAdded (%s)" (string n.Id)
    | NodeRemoved n ->sprintf "NodeRemoved (%s)" (string n.Id)

[<RequireQualifiedAccess>]
module Node =

  let create id data =
    { Id         = id
    ; Data       = data
    ; State      = Running
    ; Voting     = true
    ; VotedForMe = false
    ; NextIndex  = 1UL
    ; MatchIndex = 0UL
    }

  let isVoting (node : Node<'node>) : bool =
    node.State = Running && node.Voting

  let setVoting node voting =
    { node with Voting = voting }

  let voteForMe node vote =
    { node with VotedForMe = vote }

  let hasVoteForMe node = node.VotedForMe

  let setHasSufficientLogs node =
    { node with
        State = Running
        Voting = true }

  let hasSufficientLogs node =
    node.State = Running

  let canVote peer =
    isVoting peer && hasVoteForMe peer && peer.State = Running

  let getId node = node.Id
  let getData node = node.Data
  let getState node = node.State
  let getNextIndex  node = node.NextIndex
  let getMatchIndex node = node.MatchIndex

  let private added oldnodes newnodes =
    let folder changes (node: Node<_>) =
      match Array.tryFind (getId >> ((=) node.Id)) oldnodes with
        | Some _ -> changes
        | _ -> NodeAdded(node) :: changes
    Array.fold folder [] newnodes

  let private removed oldnodes newnodes =
    let folder changes (node: Node<_>) =
      match Array.tryFind (getId >> ((=) node.Id)) newnodes with
        | Some _ -> changes
        | _ -> NodeAdded(node) :: changes
    Array.fold folder [] oldnodes

  let changes (oldnodes: Node<_> array) (newnodes: Node<_> array) =
    []
    |> List.append (added oldnodes newnodes)
    |> List.append (removed oldnodes newnodes)
    |> Array.ofList
