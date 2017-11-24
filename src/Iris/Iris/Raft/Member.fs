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

// * RaftMemberYaml

#if !FABLE_COMPILER && !IRIS_NODES

type RaftMemberYaml() =
  [<DefaultValue>] val mutable Id         : string
  [<DefaultValue>] val mutable HostName   : string
  [<DefaultValue>] val mutable IpAddress  : string
  [<DefaultValue>] val mutable RaftPort   : uint16
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
    IpAddress  : IpAddress
    RaftPort   : Port
    WsPort     : Port
    GitPort    : Port
    ApiPort    : Port
    Voting     : bool
    VotedForMe : bool
    State      : RaftMemberState
    NextIndex  : Index
    MatchIndex : Index }

  // ** optics

  static member Id_ =
    (fun (mem:RaftMember) -> mem.Id),
    (fun id (mem:RaftMember) -> { mem with Id = id })

  static member HostName_ =
    (fun (mem:RaftMember) -> mem.HostName),
    (fun hostName (mem:RaftMember) -> { mem with HostName = hostName })

  static member IpAddress_ =
    (fun (mem:RaftMember) -> mem.IpAddress),
    (fun ipAddress (mem:RaftMember) -> { mem with IpAddress = ipAddress })

  static member RaftPort_ =
    (fun (mem:RaftMember) -> mem.RaftPort),
    (fun raftPort (mem:RaftMember) -> { mem with RaftPort = raftPort })

  static member WsPort_ =
    (fun (mem:RaftMember) -> mem.WsPort),
    (fun wsPort (mem:RaftMember) -> { mem with WsPort = wsPort })

  static member GitPort_ =
    (fun (mem:RaftMember) -> mem.GitPort),
    (fun gitPort (mem:RaftMember) -> { mem with GitPort = gitPort })

  static member ApiPort_ =
    (fun (mem:RaftMember) -> mem.ApiPort),
    (fun apiPort (mem:RaftMember) -> { mem with ApiPort = apiPort })

  static member Voting_ =
    (fun (mem:RaftMember) -> mem.Voting),
    (fun voting (mem:RaftMember) -> { mem with Voting = voting })

  static member VotedForMe_ =
    (fun (mem:RaftMember) -> mem.VotedForMe),
    (fun votedForMe (mem:RaftMember) -> { mem with VotedForMe = votedForMe })

  static member State_ =
    (fun (mem:RaftMember) -> mem.State),
    (fun state (mem:RaftMember) -> { mem with State = state })

  static member NextIndex_ =
    (fun (mem:RaftMember) -> mem.NextIndex),
    (fun nextIndex (mem:RaftMember) -> { mem with NextIndex = nextIndex })

  static member MatchIndex_ =
    (fun (mem:RaftMember) -> mem.MatchIndex),
    (fun matchIndex (mem:RaftMember) -> { mem with MatchIndex = matchIndex })

  // ** ToString

  override self.ToString() =
    sprintf "%s on %s (%s:%d) %s %s %s"
      (string self.Id)
      (string self.HostName)
      (string self.IpAddress)
      self.RaftPort
      (string self.State)
      (sprintf "(NxtIdx %A)" self.NextIndex)
      (sprintf "(MtchIdx %A)" self.MatchIndex)

  // ** ToYaml

  #if !FABLE_COMPILER && !IRIS_NODES

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYaml () =
    let yaml = RaftMemberYaml()
    yaml.Id         <- string self.Id
    yaml.HostName   <- unwrap self.HostName
    yaml.IpAddress  <- string self.IpAddress
    yaml.RaftPort   <- unwrap self.RaftPort
    yaml.WsPort     <- unwrap self.WsPort
    yaml.GitPort    <- unwrap self.GitPort
    yaml.ApiPort    <- unwrap self.ApiPort
    yaml.State      <- string self.State
    yaml.NextIndex  <- self.NextIndex
    yaml.MatchIndex <- self.MatchIndex
    yaml.Voting     <- self.Voting
    yaml.VotedForMe <- self.VotedForMe
    yaml

  // ** FromYaml

  static member FromYaml (yaml: RaftMemberYaml) : Either<IrisError, RaftMember> =
    either {
      let! id = IrisId.TryParse yaml.Id
      let! ip = IpAddress.TryParse yaml.IpAddress
      let! state = RaftMemberState.TryParse yaml.State
      return {
        Id         = id
        HostName   = name yaml.HostName
        IpAddress  = ip
        RaftPort   = port yaml.RaftPort
        WsPort     = port yaml.WsPort
        GitPort    = port yaml.GitPort
        ApiPort    = port yaml.ApiPort
        Voting     = yaml.Voting
        VotedForMe = yaml.VotedForMe
        NextIndex  = yaml.NextIndex
        MatchIndex = yaml.MatchIndex
        State      = state
      }
    }

  #endif

  // ** ToOffset

  member mem.ToOffset (builder: FlatBufferBuilder) =
    let id = RaftMemberFB.CreateIdVector(builder,mem.Id.ToByteArray())
    let ip = string mem.IpAddress |> builder.CreateString

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

    RaftMemberFB.AddIpAddress(builder, ip)
    RaftMemberFB.AddRaftPort(builder, unwrap mem.RaftPort)
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
      let! id = Id.decodeId fb
      let! state = RaftMemberState.FromFB fb.State
      return {
        Id         = id
        State      = state
        HostName   = name fb.HostName
        IpAddress  = IpAddress.Parse fb.IpAddress
        RaftPort   = port fb.RaftPort
        WsPort     = port fb.WsPort
        GitPort    = port fb.GitPort
        ApiPort    = port fb.ApiPort
        Voting     = fb.Voting
        VotedForMe = fb.VotedForMe
        NextIndex  = index fb.NextIndex
        MatchIndex = index fb.MatchIndex
      }
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

  // ** ToYaml

  #if !FABLE_COMPILER && !IRIS_NODES

  member self.ToYaml() =
    match self with
    | MemberAdded mem   -> mem |> Yaml.toYaml |> ConfigChangeYaml.MemberAdded
    | MemberRemoved mem -> mem |> Yaml.toYaml |> ConfigChangeYaml.MemberRemoved

  // ** FromYaml

  static member FromYaml (yml: ConfigChangeYaml) =
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
      |> Error.asParseError "ConfigChange.FromYaml"
      |> Either.fail

  #endif

// * Member module

[<RequireQualifiedAccess>]
module Member =

  open Aether

  // ** getters

  let id = Optic.get RaftMember.Id_
  let hostName = Optic.get RaftMember.HostName_
  let ipAddress = Optic.get RaftMember.IpAddress_
  let raftPort = Optic.get RaftMember.RaftPort_
  let wsPort = Optic.get RaftMember.WsPort_
  let gitPort = Optic.get RaftMember.GitPort_
  let apiPort = Optic.get RaftMember.ApiPort_
  let voting = Optic.get RaftMember.Voting_
  let votedForMe = Optic.get RaftMember.VotedForMe_
  let state = Optic.get RaftMember.State_
  let nextIndex = Optic.get RaftMember.NextIndex_
  let matchIndex = Optic.get RaftMember.MatchIndex_

  // ** setters

  let setId = Optic.set RaftMember.Id_
  let setHostName = Optic.set RaftMember.HostName_
  let setIpAddress = Optic.set RaftMember.IpAddress_
  let setRaftPort = Optic.set RaftMember.RaftPort_
  let setWsPort = Optic.set RaftMember.WsPort_
  let setGitPort = Optic.set RaftMember.GitPort_
  let setApiPort = Optic.set RaftMember.ApiPort_
  let setVoting = Optic.set RaftMember.Voting_
  let setVotedForMe = Optic.set RaftMember.VotedForMe_
  let setState = Optic.set RaftMember.State_
  let setNextIndex = Optic.set RaftMember.NextIndex_
  let setMatchIndex = Optic.set RaftMember.MatchIndex_

  // ** create

  let create id =
    #if FABLE_COMPILER
    let hostname = Fable.Import.Browser.window.location.host
    #else
    let hostname = Network.getHostName ()
    #endif
    { Id         = id
      HostName   = name hostname
      IpAddress  = IPv4Address "127.0.0.1"
      RaftPort   = Measure.port Constants.DEFAULT_RAFT_PORT
      WsPort     = Measure.port Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort    = Measure.port Constants.DEFAULT_GIT_PORT
      ApiPort    = Measure.port Constants.DEFAULT_API_PORT
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

  // ** canVote

  let canVote peer =
    isVoting peer && hasVoteForMe peer

  // ** added

  let private added oldmems newmems =
    let folder changes (mem: RaftMember) =
      match Array.tryFind (Member.id >> ((=) mem.Id)) oldmems with
        | Some _ -> changes
        | _ -> MemberAdded(mem) :: changes
    Array.fold folder [] newmems

  // ** removed

  let private removed oldmems newmems =
    let folder changes (mem: RaftMember) =
      match Array.tryFind (Member.id >> ((=) mem.Id)) newmems with
        | Some _ -> changes
        | _ -> MemberAdded(mem) :: changes
    Array.fold folder [] oldmems

  // ** changes

  let changes (oldmems: RaftMember array) (newmems: RaftMember array) =
    []
    |> List.append (added oldmems newmems)
    |> List.append (removed oldmems newmems)
    |> Array.ofList
