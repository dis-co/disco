namespace rec Iris.Raft

// * Imports

open Iris.Core

#if FABLE_COMPILER

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.Net
open FlatBuffers
open Iris.Serialization

#endif

// * RaftMemberState

type RaftMemberState =
  | Joining                             // excludes mem from voting
  | Running                             // normal execution state
  | Failed                              // mem has failed for some reason

  // ** ToString

  override self.ToString() =
    match self with
    | Joining -> "Joining"
    | Running -> "Running"
    | Failed  -> "Failed"

  // ** Parse

  static member Parse (str: string) =
    match str with
    | "Joining" -> Joining
    | "Running" -> Running
    | "Failed"  -> Failed
    | _         -> failwithf "MemberState: failed to parse %s" str

  // ** TryParse

  static member TryParse (str: string) =
    try
      str |> RaftMemberState.Parse |> Either.succeed
    with
      | exn ->
        sprintf "Could not parse RaftMemberState: %s" exn.Message
        |> Error.asParseError "RaftMemberState.TryParse"
        |> Either.fail

  // ** ToOffset

  member self.ToOffset () =
    match self with
      | Running -> RaftMemberStateFB.RunningFB
      | Joining -> RaftMemberStateFB.JoiningFB
      | Failed  -> RaftMemberStateFB.FailedFB

  // ** FromFB

  static member FromFB (fb: RaftMemberStateFB) =
    #if FABLE_COMPILER
    match fb with
      | x when x = RaftMemberStateFB.JoiningFB -> Right Joining
      | x when x = RaftMemberStateFB.RunningFB -> Right Running
      | x when x = RaftMemberStateFB.FailedFB  -> Right Failed
      | x ->
        sprintf "Could not parse RaftMemberState: %A" x
        |> Error.asParseError "RaftMemberState.FromFB"
        |> Either.fail
    #else
    match fb with
      | RaftMemberStateFB.JoiningFB -> Right Joining
      | RaftMemberStateFB.RunningFB -> Right Running
      | RaftMemberStateFB.FailedFB  -> Right Failed
      | x ->
        sprintf "Could not parse RaftMemberState: %A" x
        |> Error.asParseError "RaftMemberState.FromFB"
        |> Either.fail
    #endif

// * RaftMembeYaml

#if !FABLE_COMPILER && !IRIS_NODES

type RaftMemberYaml() =
  [<DefaultValue>] val mutable Id         : string
  [<DefaultValue>] val mutable HostName   : string
  [<DefaultValue>] val mutable IpAddr     : string
  [<DefaultValue>] val mutable Port       : uint16
  [<DefaultValue>] val mutable WebPort    : uint16
  [<DefaultValue>] val mutable WsPort     : uint16
  [<DefaultValue>] val mutable GitPort    : uint16
  [<DefaultValue>] val mutable ApiPort    : uint16
  [<DefaultValue>] val mutable State      : string
  [<DefaultValue>] val mutable NextIndex  : Index
  [<DefaultValue>] val mutable MatchIndex : Index
  [<DefaultValue>] val mutable Voting     : bool
  [<DefaultValue>] val mutable VotedForMe : bool

#endif

// * RaftMember

type RaftMember =
  { Id         : MemberId
    HostName   : Name
    IpAddr     : IpAddress
    Port       : Port
    WsPort     : Port
    GitPort    : Port
    ApiPort    : Port
    Voting     : bool
    VotedForMe : bool
    State      : RaftMemberState
    NextIndex  : Index
    MatchIndex : Index }

  // ** ToString

  override self.ToString() =
    sprintf "%s on %s (%s:%d) %s %s %s"
      (string self.Id)
      (string self.HostName)
      (string self.IpAddr)
      self.Port
      (string self.State)
      (sprintf "(NxtIdx %A)" self.NextIndex)
      (sprintf "(MtchIdx %A)" self.MatchIndex)

  // ** ToYamlObject

  #if !FABLE_COMPILER && !IRIS_NODES

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    let yaml = RaftMemberYaml()
    yaml.Id         <- string self.Id
    yaml.HostName   <- unwrap self.HostName
    yaml.IpAddr     <- string self.IpAddr
    yaml.Port       <- unwrap self.Port
    yaml.WsPort     <- unwrap self.WsPort
    yaml.GitPort    <- unwrap self.GitPort
    yaml.ApiPort    <- unwrap self.ApiPort
    yaml.State      <- string self.State
    yaml.NextIndex  <- self.NextIndex
    yaml.MatchIndex <- self.MatchIndex
    yaml.Voting     <- self.Voting
    yaml.VotedForMe <- self.VotedForMe
    yaml

  // ** FromYamlObject

  static member FromYamlObject (yaml: RaftMemberYaml) : Either<IrisError, RaftMember> =
    either {
      let! ip = IpAddress.TryParse yaml.IpAddr
      let! state = RaftMemberState.TryParse yaml.State
      return { Id         = Id yaml.Id
             ; HostName   = name yaml.HostName
             ; IpAddr     = ip
             ; Port       = port yaml.Port
             ; WsPort     = port yaml.WsPort
             ; GitPort    = port yaml.GitPort
             ; ApiPort    = port yaml.ApiPort
             ; Voting     = yaml.Voting
             ; VotedForMe = yaml.VotedForMe
             ; NextIndex  = yaml.NextIndex
             ; MatchIndex = yaml.MatchIndex
             ; State      = state }
    }

  #endif

  // ** ToOffset

  member mem.ToOffset (builder: FlatBufferBuilder) =
    let id = string mem.Id |> builder.CreateString
    let ip = string mem.IpAddr |> builder.CreateString

    let hostname =
      let unwrapped = unwrap mem.HostName
      if isNull unwrapped then
        None
      else
        unwrapped |> builder.CreateString |> Some

    let state = mem.State.ToOffset()

    RaftMemberFB.StartRaftMemberFB(builder)
    RaftMemberFB.AddId(builder, id)

    match hostname with
    | Some hostname -> RaftMemberFB.AddHostName(builder, hostname)
    | None -> ()

    RaftMemberFB.AddIpAddr(builder, ip)
    RaftMemberFB.AddPort(builder, unwrap mem.Port)
    RaftMemberFB.AddWsPort(builder, unwrap mem.WsPort)
    RaftMemberFB.AddGitPort(builder, unwrap mem.GitPort)
    RaftMemberFB.AddApiPort(builder, unwrap mem.ApiPort)
    RaftMemberFB.AddVoting(builder, mem.Voting)
    RaftMemberFB.AddVotedForMe(builder, mem.VotedForMe)
    RaftMemberFB.AddState(builder, state)
    RaftMemberFB.AddNextIndex(builder, int mem.NextIndex)
    RaftMemberFB.AddMatchIndex(builder, int mem.MatchIndex)
    RaftMemberFB.EndRaftMemberFB(builder)

  // ** FromFB

  static member FromFB (fb: RaftMemberFB) : Either<IrisError, RaftMember> =
    either {
      let! state = RaftMemberState.FromFB fb.State
      return { Id         = Id fb.Id
               State      = state
               HostName   = name fb.HostName
               IpAddr     = IpAddress.Parse fb.IpAddr
               Port       = port fb.Port
               WsPort     = port fb.WsPort
               GitPort    = port fb.GitPort
               ApiPort    = port fb.ApiPort
               Voting     = fb.Voting
               VotedForMe = fb.VotedForMe
               NextIndex  = index fb.NextIndex
               MatchIndex = index fb.MatchIndex }
    }

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> RaftMemberFB.GetRootAsRaftMemberFB
    |> RaftMember.FromFB

// * ConfigChangeYaml

#if !FABLE_COMPILER && !IRIS_NODES

type ConfigChangeYaml() =
  [<DefaultValue>] val mutable ChangeType : string
  [<DefaultValue>] val mutable Member       : RaftMemberYaml

  // ** MemberAdded

  static member MemberAdded (mem: RaftMemberYaml) =
    let yaml = new ConfigChangeYaml()
    yaml.ChangeType <- "MemberAdded"
    yaml.Member <- mem
    yaml

  // ** MemberRemoved

  static member MemberRemoved (mem: RaftMemberYaml) =
    let yaml = new ConfigChangeYaml()
    yaml.ChangeType <- "MemberRemoved"
    yaml.Member <- mem
    yaml

#endif

// * ConfigChange

type ConfigChange =
  | MemberAdded   of RaftMember
  | MemberRemoved of RaftMember

  // ** ToString

  override self.ToString() =
    match self with
    | MemberAdded   n -> sprintf "MemberAdded (%s)"   (string n.Id)
    | MemberRemoved n ->sprintf "MemberRemoved (%s)" (string n.Id)

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    match self with
      | MemberAdded mem ->
        let mem = mem.ToOffset(builder)
        ConfigChangeFB.StartConfigChangeFB(builder)
        ConfigChangeFB.AddType(builder, ConfigChangeTypeFB.MemberAdded)
        ConfigChangeFB.AddMember(builder, mem)
        ConfigChangeFB.EndConfigChangeFB(builder)
      | MemberRemoved mem ->
        let mem = mem.ToOffset(builder)
        ConfigChangeFB.StartConfigChangeFB(builder)
        ConfigChangeFB.AddType(builder, ConfigChangeTypeFB.MemberRemoved)
        ConfigChangeFB.AddMember(builder, mem)
        ConfigChangeFB.EndConfigChangeFB(builder)

  // ** FromFB

  static member FromFB (fb: ConfigChangeFB) : Either<IrisError,ConfigChange> =
    either {
      #if FABLE_COMPILER
      let! mem = fb.Member |> RaftMember.FromFB
      match fb.Type with
      | x when x = ConfigChangeTypeFB.MemberAdded   -> return (MemberAdded   mem)
      | x when x = ConfigChangeTypeFB.MemberRemoved -> return (MemberRemoved mem)
      | x ->
        return!
          sprintf "Could not parse ConfigChangeTypeFB %A" x
          |> Error.asParseError "ConfigChange.FromFB"
          |> Either.fail
      #else
      let nullable = fb.Member
      if nullable.HasValue then
        let! mem = RaftMember.FromFB nullable.Value
        match fb.Type with
        | ConfigChangeTypeFB.MemberAdded   -> return (MemberAdded   mem)
        | ConfigChangeTypeFB.MemberRemoved -> return (MemberRemoved mem)
        | x ->
          return!
            sprintf "Could not parse ConfigChangeTypeFB %A" x
            |> Error.asParseError "ConfigChange.FromFB"
            |> Either.fail
      else
        return!
          "Could not parse empty ConfigChangeFB payload"
          |> Error.asParseError "ConfigChange.FromFB"
          |> Either.fail
      #endif
    }

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> ConfigChangeFB.GetRootAsConfigChangeFB
    |> ConfigChange.FromFB

  // ** ToYamlObject

  #if !FABLE_COMPILER && !IRIS_NODES

  member self.ToYamlObject() =
    match self with
    | MemberAdded mem   -> mem |> Yaml.toYaml |> ConfigChangeYaml.MemberAdded
    | MemberRemoved mem -> mem |> Yaml.toYaml |> ConfigChangeYaml.MemberRemoved

  // ** FromYamlObject

  static member FromYamlObject (yml: ConfigChangeYaml) =
    match yml.ChangeType with
    | "MemberAdded" -> either {
        let! mem = Yaml.fromYaml yml.Member
        return MemberAdded(mem)
      }
    | "MemberRemoved" -> either {
        let! mem = Yaml.fromYaml yml.Member
        return MemberRemoved(mem)
      }
    | x ->
      sprintf "Could not parse %s as ConfigChange" x
      |> Error.asParseError "ConfigChange.FromYamlObject"
      |> Either.fail

  #endif

// * Member module

[<RequireQualifiedAccess>]
module Member =

  // ** create

  let create id =
    #if FABLE_COMPILER
    let hostname = Fable.Import.Browser.window.location.host
    #else
    let hostname = Network.getHostName ()
    #endif
    { Id         = id
      HostName   = name hostname
      IpAddr     = IPv4Address "127.0.0.1"
      Port       = port Constants.DEFAULT_RAFT_PORT
      WsPort     = port Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort    = port Constants.DEFAULT_GIT_PORT
      ApiPort    = port Constants.DEFAULT_API_PORT
      State      = Running
      Voting     = true
      VotedForMe = false
      NextIndex  = index 1
      MatchIndex = index 0 }

  // ** isVoting

  let isVoting (mem : RaftMember) : bool =
    match mem.State, mem.Voting with
    | Running, true -> true
    | _ -> false

  // ** setVoting

  let setVoting mem voting =
    { mem with Voting = voting }

  // ** voteForMe

  let voteForMe mem vote =
    { mem with VotedForMe = vote }

  // ** hasVotedForMe

  let hasVoteForMe mem = mem.VotedForMe

  // ** setHasSufficientLogs

  let setHasSufficientLogs mem =
    { mem with
        State = Running
        Voting = true }

  // ** hasSufficientLogs

  let hasSufficientLogs mem =
    mem.State = Running

  // ** hostName

  let hostName mem = mem.HostName

  // ** canVote

  let canVote peer =
    isVoting peer && hasVoteForMe peer

  // ** getId

  let getId mem = mem.Id

  // ** getState

  let getState mem = mem.State

  // ** getNextIndex

  let getNextIndex  mem = mem.NextIndex

  // ** getMatchIndex

  let getMatchIndex mem = mem.MatchIndex

  // ** added

  let private added oldmems newmems =
    let folder changes (mem: RaftMember) =
      match Array.tryFind (getId >> ((=) mem.Id)) oldmems with
        | Some _ -> changes
        | _ -> MemberAdded(mem) :: changes
    Array.fold folder [] newmems

  // ** removed

  let private removed oldmems newmems =
    let folder changes (mem: RaftMember) =
      match Array.tryFind (getId >> ((=) mem.Id)) newmems with
        | Some _ -> changes
        | _ -> MemberAdded(mem) :: changes
    Array.fold folder [] oldmems

  // ** changes

  let changes (oldmems: RaftMember array) (newmems: RaftMember array) =
    []
    |> List.append (added oldmems newmems)
    |> List.append (removed oldmems newmems)
    |> Array.ofList

  // ** ipAddr

  let ipAddr mem = mem.IpAddr

  // ** raftPort

  let raftPort (mem:RaftMember) = mem.Port

  // ** setRaftPort

  let setRaftPort (port: Port) (mem: RaftMember) =
    { mem with Port = port }

  // ** setGitPort

  let setGitPort (port: Port) (mem: RaftMember) =
    { mem with GitPort = port }

  // ** setWsPort

  let setWsPort (port: Port) (mem: RaftMember) =
    { mem with WsPort = port }
