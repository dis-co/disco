namespace Iris.Raft

open Iris.Core

#if JAVASCRIPT

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.Net
open FlatBuffers
open Iris.Serialization.Raft

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

  static member TryParse (str: string) =
    try
      str |> RaftNodeState.Parse |> Some
    with
      | _ -> None

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
#if JAVASCRIPT
    match fb with
      | x when x = NodeStateFB.JoiningFB -> Some Joining
      | x when x = NodeStateFB.RunningFB -> Some Running
      | x when x = NodeStateFB.FailedFB  -> Some Failed
      | _                                -> None
#else
    match fb with
      | NodeStateFB.JoiningFB -> Some Joining
      | NodeStateFB.RunningFB -> Some Running
      | NodeStateFB.FailedFB  -> Some Failed
      | _                     -> None
#endif

type RaftNodeYaml(id, hostname, ip, port, web, ws, git, state) as self =
  [<DefaultValue>] val mutable Id       : string
  [<DefaultValue>] val mutable HostName : string
  [<DefaultValue>] val mutable IpAddr   : string
  [<DefaultValue>] val mutable Port     : uint16
  [<DefaultValue>] val mutable WebPort  : uint16
  [<DefaultValue>] val mutable WsPort   : uint16
  [<DefaultValue>] val mutable GitPort  : uint16
  [<DefaultValue>] val mutable State    : string

  new () = new RaftNodeYaml(null, null, null, 0us, 0us, 0us, 0us, null)

  do
    self.Id       <- id
    self.HostName <- hostname
    self.IpAddr   <- ip
    self.Port     <- port
    self.WebPort  <- web
    self.WsPort   <- ws
    self.GitPort  <- git
    self.State    <- state

//  _   _           _
// | \ | | ___   __| | ___
// |  \| |/ _ \ / _` |/ _ \
// | |\  | (_) | (_| |  __/
// |_| \_|\___/ \__,_|\___|

and RaftNode =
  { Id         : NodeId
  ; HostName   : string
  ; IpAddr     : IpAddress
  ; Port       : uint16
  ; WebPort    : uint16
  ; WsPort     : uint16
  ; GitPort    : uint16
  ; Voting     : bool
  ; VotedForMe : bool
  ; State      : RaftNodeState
  ; NextIndex  : Index
  ; MatchIndex : Index }

  override self.ToString() =
    sprintf "%s on %s (%s:%d) %s %s %s"
      (string self.Id)
      (string self.HostName)
      (string self.IpAddr)
      self.Port
      (string self.State)
      (sprintf "(NxtIdx %A)" self.NextIndex)
      (sprintf "(MtchIdx %A)" self.MatchIndex)

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    let yaml = new RaftNodeYaml()
    yaml.Id <- string self.Id
    yaml.HostName <- self.HostName
    yaml.IpAddr <- string self.IpAddr
    yaml.Port <- self.Port
    yaml.WebPort <- self.WebPort
    yaml.WsPort <- self.WsPort
    yaml.GitPort <- self.GitPort
    yaml.State <- string self.State
    yaml

  static member FromYamlObject (yaml: RaftNodeYaml) : RaftNode option =
    maybe {
      let! ip = IpAddress.TryParse yaml.IpAddr
      let! state = RaftNodeState.TryParse yaml.State
      return { Id = Id yaml.Id
             ; HostName = yaml.HostName
             ; IpAddr = ip
             ; Port = yaml.Port
             ; WebPort = yaml.WebPort
             ; WsPort = yaml.WsPort
             ; GitPort = yaml.GitPort
             ; Voting = true
             ; VotedForMe = false
             ; NextIndex = 0u
             ; MatchIndex = 0u
             ; State = state
             }
    }

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
    NodeFB.AddWebPort(builder, int node.WebPort)
    NodeFB.AddWsPort(builder, int node.WsPort)
    NodeFB.AddGitPort(builder, int node.GitPort)
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
          ; WebPort = uint16 fb.WebPort
          ; WsPort = uint16 fb.WsPort
          ; GitPort = uint16 fb.GitPort
          ; Voting = fb.Voting
          ; VotedForMe = fb.VotedForMe
          ; NextIndex = fb.NextIndex
          ; MatchIndex = fb.MatchIndex })
    with
      | _ -> None

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes (bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> NodeFB.GetRootAsNodeFB
    |> RaftNode.FromFB

// __   __              _   _____
// \ \ / /_ _ _ __ ___ | | |_   _|   _ _ __   ___
//  \ V / _` | '_ ` _ \| |   | || | | | '_ \ / _ \
//   | | (_| | | | | | | |   | || |_| | |_) |  __/
//   |_|\__,_|_| |_| |_|_|   |_| \__, | .__/ \___|
//                               |___/|_|

type ConfigChangeYaml(tipe: string, id: string) as self =
  [<DefaultValue>] val mutable ChangeType : string
  [<DefaultValue>] val mutable NodeId     : string

  new () = new ConfigChangeYaml(null, null)

  do
    self.ChangeType <- tipe
    self.NodeId     <- id

  member self.ToConfigChange (nodes: RaftNode array) =
    match self.ChangeType with
      | "NodeAdded"   -> Some (NodeAdded (failwith "ohai"))
      | "NodeRemoved" -> Some (NodeRemoved (failwith "ohai"))
      | _ -> None

  static member FromConfigChange (chng: ConfigChange) =
    match chng with
    | NodeAdded node -> ConfigChangeYaml.NodeAdded(node.Id)
    | NodeRemoved node -> ConfigChangeYaml.NodeRemoved(node.Id)

  static member NodeAdded (id: Id) =
    new ConfigChangeYaml("NodeAdded", string id)

  static member NodeRemoved (id: Id) =
    new ConfigChangeYaml("NodeRemoved", string id)

//   ____             __ _        ____ _
//  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
// | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
// | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
//  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
//                         |___/                         |___/

and ConfigChange =
  | NodeAdded   of RaftNode
  | NodeRemoved of RaftNode

  override self.ToString() =
    match self with
    | NodeAdded   n -> sprintf "NodeAdded (%s)"   (string n.Id)
    | NodeRemoved n ->sprintf "NodeRemoved (%s)" (string n.Id)

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

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
#if JAVASCRIPT
    fb.Node
    |> RaftNode.FromFB
    |> Option.bind
      (fun node ->
        match fb.Type with
          | x when x = ConfigChangeTypeFB.NodeAdded   -> Some (NodeAdded   node)
          | x when x = ConfigChangeTypeFB.NodeRemoved -> Some (NodeRemoved node)
          | _                                         -> None)
#else
    let nullable = fb.Node
    if nullable.HasValue then
      RaftNode.FromFB nullable.Value
      |> Option.bind
        (fun node ->
          match fb.Type with
            | ConfigChangeTypeFB.NodeAdded   -> Some (NodeAdded   node)
            | ConfigChangeTypeFB.NodeRemoved -> Some (NodeRemoved node)
            | _                              -> None)
    else None
#endif

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes (bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> ConfigChangeFB.GetRootAsConfigChangeFB
    |> ConfigChange.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|


  member self.ToYamlObject() = ConfigChangeYaml.FromConfigChange self

  static member FromYamlObject (yml: ConfigChangeYaml) =
    implement "ConfigChange.FromYamlObject"

[<RequireQualifiedAccess>]
module Node =

  let create id =
#if JAVASCRIPT
    let hostname = Fable.Import.Browser.window.location.host
#else
    let hostname = System.Net.Dns.GetHostName()
#endif
    { Id         = id
    ; HostName   = hostname
    ; IpAddr     = IPv4Address "127.0.0.1"
    ; Port       = 6000us
    ; WebPort    = 7000us
    ; WsPort     = 8000us
    ; GitPort    = 9000us
    ; State      = Running
    ; Voting     = true
    ; VotedForMe = false
    ; NextIndex  = 1u
    ; MatchIndex = 0u
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
