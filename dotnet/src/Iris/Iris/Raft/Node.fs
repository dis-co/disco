namespace Iris.Raft

open Iris.Core

#if JAVASCRIPT
#else

open System
open System.Net
open FlatBuffers
open Iris.Serialization.Raft
open Newtonsoft.Json
open Newtonsoft.Json.Linq

#endif

//  _   _           _      ____  _        _
// | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___
// |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \
// | |\  | (_) | (_| |  __/___) | || (_| | ||  __/
// |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|

type RaftNodeState =
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

#if JAVASCRIPT
#else

    //  ____  _
    // | __ )(_)_ __   __ _ _ __ _   _
    // |  _ \| | '_ \ / _` | '__| | | |
    // | |_) | | | | | (_| | |  | |_| |
    // |____/|_|_| |_|\__,_|_|   \__, |
    //                           |___/

    member self.ToOffset () =
      match self with
        | Running -> NodeStateFB.RunningFB
        | Joining -> NodeStateFB.JoiningFB
        | Failed  -> NodeStateFB.FailedFB

    static member FromFB (fb: NodeStateFB) =
      match fb with
        | NodeStateFB.JoiningFB -> Some Joining
        | NodeStateFB.RunningFB -> Some Running
        | NodeStateFB.FailedFB  -> Some Failed
        | _                     -> None

    //      _
    //     | |___  ___  _ __
    //  _  | / __|/ _ \| '_ \
    // | |_| \__ \ (_) | | | |
    //  \___/|___/\___/|_| |_|

    member self.ToJToken() =
      let json = new JObject()
      json.Add("$type", new JValue("Iris.Raft.RaftNode"))

      match self with
      | Running -> json.Add("Case", new JValue("Running"))
      | Joining -> json.Add("Case", new JValue("Joining"))
      | Failed  -> json.Add("Case", new JValue("Failed"))

      json :> JToken

    member self.ToJson() =
      self.ToJToken() |> string

#endif

//  _   _           _
// | \ | | ___   __| | ___
// |  \| |/ _ \ / _` |/ _ \
// | |\  | (_) | (_| |  __/
// |_| \_|\___/ \__,_|\___|

type RaftNode =
  { Id         : NodeId
  ; HostName   : string
  ; IpAddr     : IpAddress
  ; Port       : uint16
  ; Voting     : bool
  ; VotedForMe : bool
  ; State      : RaftNodeState
  ; NextIndex  : Index
  ; MatchIndex : Index }

  with
    override self.ToString() =
      sprintf "%s on %s (%s:%d) %s %s %s"
        (string self.Id)
        (string self.HostName)
        (string self.IpAddr)
        self.Port
        (string self.State)
        (sprintf "(NxtIdx %A)" self.NextIndex)
        (sprintf "(MtchIdx %A)" self.MatchIndex)

#if JAVASCRIPT
#else

    //  ____  _
    // | __ )(_)_ __   __ _ _ __ _   _
    // |  _ \| | '_ \ / _` | '__| | | |
    // | |_) | | | | | (_| | |  | |_| |
    // |____/|_|_| |_|\__,_|_|   \__, |
    //                           |___/

    member node.ToOffset (builder: FlatBufferBuilder) =
      let id = string node.Id |> builder.CreateString
      let ip = string node.IpAddr |> builder.CreateString
      let hostname = node.HostName |> builder.CreateString
      let state = node.State.ToOffset()

      NodeFB.StartNodeFB(builder)
      NodeFB.AddId(builder, id)
      NodeFB.AddHostName(builder, hostname)
      NodeFB.AddIpAddr(builder, ip)
      NodeFB.AddPort(builder, int node.Port)
      NodeFB.AddVoting(builder, node.Voting)
      NodeFB.AddVotedForMe(builder, node.VotedForMe)
      NodeFB.AddState(builder, state)
      NodeFB.AddNextIndex(builder, node.NextIndex)
      NodeFB.AddMatchIndex(builder, node.MatchIndex)
      NodeFB.EndNodeFB(builder)

    static member FromFB (fb: NodeFB) : RaftNode option =
      try
        RaftNodeState.FromFB fb.State
        |> Option.map
          (fun state ->
            { Id = Id fb.Id
            ; State = state
            ; HostName = fb.HostName
            ; IpAddr = IpAddress.Parse fb.IpAddr
            ; Port = uint16 fb.Port
            ; Voting = fb.Voting
            ; VotedForMe = fb.VotedForMe
            ; NextIndex = fb.NextIndex
            ; MatchIndex = fb.MatchIndex })
      with
        | _ -> None

    member self.ToBytes () = Binary.buildBuffer self

    static member FromBytes (bytes: byte array) =
      NodeFB.GetRootAsNodeFB(new ByteBuffer(bytes))
      |> RaftNode.FromFB

    //      _
    //     | |___  ___  _ __
    //  _  | / __|/ _ \| '_ \
    // | |_| \__ \ (_) | | | |
    //  \___/|___/\___/|_| |_|

    member self.ToJToken() =
      let json = new JObject()
      json.Add("$type", new JValue("Iris.Raft.RaftNode"))
      json.Add("Id", new JValue(string self.Id))
      json.Add("HostName", new JValue(self.HostName))
      json.Add("IpAddr", Json.tokenize self.IpAddr)
      json.Add("Port", new JValue(self.Port))
      json.Add("Voting", new JValue(self.Voting))
      json.Add("VotedForMe", new JValue(self.VotedForMe))
      json.Add("State", Json.tokenize self.State)
      json.Add("NextIndex", new JValue(self.NextIndex))
      json.Add("MatchIndex", new JValue(self.MatchIndex))
      json :> JToken

    member self.ToJson() =
      self.ToJToken() |> string

#endif



//   ____             __ _        ____ _
//  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
// | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
// | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
//  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
//                         |___/                         |___/

type ConfigChange =
  | NodeAdded   of RaftNode
  | NodeRemoved of RaftNode

  with
    override self.ToString() =
      match self with
      | NodeAdded   n -> sprintf "NodeAdded (%s)"   (string n.Id)
      | NodeRemoved n ->sprintf "NodeRemoved (%s)" (string n.Id)

#if JAVASCRIPT
#else
    member self.ToOffset(builder: FlatBufferBuilder) =
      match self with
        | NodeAdded node ->
          let node = node.ToOffset(builder)
          ConfigChangeFB.StartConfigChangeFB(builder)
          ConfigChangeFB.AddType(builder, ConfigChangeTypeFB.NodeAdded)
          ConfigChangeFB.AddNode(builder, node)
          ConfigChangeFB.EndConfigChangeFB(builder)
        | NodeRemoved node ->
          let node = node.ToOffset(builder)
          ConfigChangeFB.StartConfigChangeFB(builder)
          ConfigChangeFB.AddType(builder, ConfigChangeTypeFB.NodeRemoved)
          ConfigChangeFB.AddNode(builder, node)
          ConfigChangeFB.EndConfigChangeFB(builder)

    static member FromFB (fb: ConfigChangeFB) : ConfigChange option =
      RaftNode.FromFB fb.Node
      |> Option.bind
        (fun node ->
          match fb.Type with
            | ConfigChangeTypeFB.NodeAdded   -> Some (NodeAdded   node)
            | ConfigChangeTypeFB.NodeRemoved -> Some (NodeRemoved node)
            | _                              -> None)

    member self.ToBytes () = Binary.buildBuffer self

    static member FromBytes (bytes: byte array) =
      ConfigChangeFB.GetRootAsConfigChangeFB(new ByteBuffer(bytes))
      |> ConfigChange.FromFB

[<RequireQualifiedAccess>]
module Node =

  let create id =
    { Id         = id
    ; HostName   = System.Net.Dns.GetHostName()
    ; IpAddr     = IPv4Address "127.0.0.1"
    ; Port       = 9000us
    ; State      = Running
    ; Voting     = true
    ; VotedForMe = false
    ; NextIndex  = 1UL
    ; MatchIndex = 0UL
    }

  let isVoting (node : RaftNode) : bool =
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

  let hostName node = node.HostName

  let ipAddr node = node.IpAddr

  let port node = node.Port

  let canVote peer =
    isVoting peer && hasVoteForMe peer && peer.State = Running

  let getId node = node.Id
  let getState node = node.State
  let getNextIndex  node = node.NextIndex
  let getMatchIndex node = node.MatchIndex

  let private added oldnodes newnodes =
    let folder changes (node: RaftNode) =
      match Array.tryFind (getId >> ((=) node.Id)) oldnodes with
        | Some _ -> changes
        | _ -> NodeAdded(node) :: changes
    Array.fold folder [] newnodes

  let private removed oldnodes newnodes =
    let folder changes (node: RaftNode) =
      match Array.tryFind (getId >> ((=) node.Id)) newnodes with
        | Some _ -> changes
        | _ -> NodeAdded(node) :: changes
    Array.fold folder [] oldnodes

  let changes (oldnodes: RaftNode array) (newnodes: RaftNode array) =
    []
    |> List.append (added oldnodes newnodes)
    |> List.append (removed oldnodes newnodes)
    |> Array.ofList

#endif
