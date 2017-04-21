namespace Iris.Raft

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

//  _   _           _      ____  _        _
// | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___
// |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \
// | |\  | (_) | (_| |  __/___) | || (_| | ||  __/
// |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|

type RaftMemberState =
  | Joining                             // excludes mem from voting
  | Running                             // normal execution state
  | Failed                              // mem has failed for some reason

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
    | _         -> failwithf "MemberState: failed to parse %s" str

  static member TryParse (str: string) =
    try
      str |> RaftMemberState.Parse |> Either.succeed
    with
      | exn ->
        sprintf "Could not parse RaftMemberState: %s" exn.Message
        |> Error.asParseError "RaftMemberState.TryParse"
        |> Either.fail

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset () =
    match self with
      | Running -> RaftMemberStateFB.RunningFB
      | Joining -> RaftMemberStateFB.JoiningFB
      | Failed  -> RaftMemberStateFB.FailedFB

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

//  _   _           _
// | \ | | ___   __| | ___
// |  \| |/ _ \ / _` |/ _ \
// | |\  | (_) | (_| |  __/
// |_| \_|\___/ \__,_|\___|

and RaftMember =
  { Id         : MemberId
  ; HostName   : string
  ; IpAddr     : IpAddress
  ; Port       : uint16
  ; WsPort     : uint16
  ; GitPort    : uint16
  ; ApiPort    : uint16
  ; Voting     : bool
  ; VotedForMe : bool
  ; State      : RaftMemberState
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

#if !FABLE_COMPILER

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    let yaml = new RaftMemberYaml()
    yaml.Id         <- string self.Id
    yaml.HostName   <- self.HostName
    yaml.IpAddr     <- string self.IpAddr
    yaml.Port       <- self.Port
    yaml.WsPort     <- self.WsPort
    yaml.GitPort    <- self.GitPort
    yaml.ApiPort    <- self.ApiPort
    yaml.State      <- string self.State
    yaml.NextIndex  <- self.NextIndex
    yaml.MatchIndex <- self.MatchIndex
    yaml.Voting     <- self.Voting
    yaml.VotedForMe <- self.VotedForMe
    yaml

  static member FromYamlObject (yaml: RaftMemberYaml) : Either<IrisError, RaftMember> =
    either {
      let! ip = IpAddress.TryParse yaml.IpAddr
      let! state = RaftMemberState.TryParse yaml.State
      return { Id         = Id yaml.Id
             ; HostName   = yaml.HostName
             ; IpAddr     = ip
             ; Port       = yaml.Port
             ; WsPort     = yaml.WsPort
             ; GitPort    = yaml.GitPort
             ; ApiPort    = yaml.ApiPort
             ; Voting     = yaml.Voting
             ; VotedForMe = yaml.VotedForMe
             ; NextIndex  = yaml.NextIndex
             ; MatchIndex = yaml.MatchIndex
             ; State      = state }
    }

#endif

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member mem.ToOffset (builder: FlatBufferBuilder) =
    let id = string mem.Id |> builder.CreateString
    let ip = string mem.IpAddr |> builder.CreateString
    let hostname = mem.HostName |> builder.CreateString
    let state = mem.State.ToOffset()

    RaftMemberFB.StartRaftMemberFB(builder)
    RaftMemberFB.AddId(builder, id)
    RaftMemberFB.AddHostName(builder, hostname)
    RaftMemberFB.AddIpAddr(builder, ip)
    RaftMemberFB.AddPort(builder, mem.Port)
    RaftMemberFB.AddWsPort(builder, mem.WsPort)
    RaftMemberFB.AddGitPort(builder, mem.GitPort)
    RaftMemberFB.AddApiPort(builder, mem.ApiPort)
    RaftMemberFB.AddVoting(builder, mem.Voting)
    RaftMemberFB.AddVotedForMe(builder, mem.VotedForMe)
    RaftMemberFB.AddState(builder, state)
    RaftMemberFB.AddNextIndex(builder, mem.NextIndex)
    RaftMemberFB.AddMatchIndex(builder, mem.MatchIndex)
    RaftMemberFB.EndRaftMemberFB(builder)

  static member FromFB (fb: RaftMemberFB) : Either<IrisError, RaftMember> =
    either {
      let! state = RaftMemberState.FromFB fb.State
      return { Id         = Id fb.Id
               State      = state
               HostName   = fb.HostName
               IpAddr     = IpAddress.Parse fb.IpAddr
               Port       = fb.Port
               WsPort     = fb.WsPort
               GitPort    = fb.GitPort
               ApiPort    = fb.ApiPort
               Voting     = fb.Voting
               VotedForMe = fb.VotedForMe
               NextIndex  = fb.NextIndex
               MatchIndex = fb.MatchIndex }
    }

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> RaftMemberFB.GetRootAsRaftMemberFB
    |> RaftMember.FromFB

// __   __              _   _____
// \ \ / /_ _ _ __ ___ | | |_   _|   _ _ __   ___
//  \ V / _` | '_ ` _ \| |   | || | | | '_ \ / _ \
//   | | (_| | | | | | | |   | || |_| | |_) |  __/
//   |_|\__,_|_| |_| |_|_|   |_| \__, | .__/ \___|
//                               |___/|_|

type ConfigChangeYaml() =
  [<DefaultValue>] val mutable ChangeType : string
  [<DefaultValue>] val mutable Member       : RaftMemberYaml

  static member MemberAdded (mem: RaftMemberYaml) =
    let yaml = new ConfigChangeYaml()
    yaml.ChangeType <- "MemberAdded"
    yaml.Member <- mem
    yaml

  static member MemberRemoved (mem: RaftMemberYaml) =
    let yaml = new ConfigChangeYaml()
    yaml.ChangeType <- "MemberRemoved"
    yaml.Member <- mem
    yaml

//   ____             __ _        ____ _
//  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
// | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
// | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
//  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
//                         |___/                         |___/

and ConfigChange =
  | MemberAdded   of RaftMember
  | MemberRemoved of RaftMember

  override self.ToString() =
    match self with
    | MemberAdded   n -> sprintf "MemberAdded (%s)"   (string n.Id)
    | MemberRemoved n ->sprintf "MemberRemoved (%s)" (string n.Id)

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

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


  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> ConfigChangeFB.GetRootAsConfigChangeFB
    |> ConfigChange.FromFB

#if !FABLE_COMPILER

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|


  member self.ToYamlObject() =
    match self with
    | MemberAdded mem   -> mem |> Yaml.toYaml |> ConfigChangeYaml.MemberAdded
    | MemberRemoved mem -> mem |> Yaml.toYaml |> ConfigChangeYaml.MemberRemoved

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

[<RequireQualifiedAccess>]
module Member =

  let create id =
#if FABLE_COMPILER
    let hostname = Fable.Import.Browser.window.location.host
#else
    let hostname = Network.getHostName ()
#endif
    { Id         = id
    ; HostName   = hostname
    ; IpAddr     = IPv4Address "127.0.0.1"
    ; Port       = Constants.DEFAULT_RAFT_PORT
    ; WsPort     = Constants.DEFAULT_WEB_SOCKET_PORT
    ; GitPort    = Constants.DEFAULT_GIT_PORT
    ; ApiPort    = Constants.DEFAULT_API_PORT
    ; State      = Running
    ; Voting     = true
    ; VotedForMe = false
    ; NextIndex  = 1u
    ; MatchIndex = 0u
    }

  let isVoting (mem : RaftMember) : bool =
    match mem.State, mem.Voting with
    | Running, true -> true
    | _ -> false

  let setVoting mem voting =
    { mem with Voting = voting }

  let voteForMe mem vote =
    { mem with VotedForMe = vote }

  let hasVoteForMe mem = mem.VotedForMe

  let setHasSufficientLogs mem =
    { mem with
        State = Running
        Voting = true }

  let hasSufficientLogs mem =
    mem.State = Running

  let hostName mem = mem.HostName

  let ipAddr mem = mem.IpAddr

  let port mem = mem.Port

  let canVote peer =
    isVoting peer && hasVoteForMe peer

  let getId mem = mem.Id
  let getState mem = mem.State
  let getNextIndex  mem = mem.NextIndex
  let getMatchIndex mem = mem.MatchIndex

  let private added oldmems newmems =
    let folder changes (mem: RaftMember) =
      match Array.tryFind (getId >> ((=) mem.Id)) oldmems with
        | Some _ -> changes
        | _ -> MemberAdded(mem) :: changes
    Array.fold folder [] newmems

  let private removed oldmems newmems =
    let folder changes (mem: RaftMember) =
      match Array.tryFind (getId >> ((=) mem.Id)) newmems with
        | Some _ -> changes
        | _ -> MemberAdded(mem) :: changes
    Array.fold folder [] oldmems

  let changes (oldmems: RaftMember array) (newmems: RaftMember array) =
    []
    |> List.append (added oldmems newmems)
    |> List.append (removed oldmems newmems)
    |> Array.ofList

  let setPort (port: uint16) (mem: RaftMember) =
    { mem with Port = port }

  let setGitPort (port: uint16) (mem: RaftMember) =
    { mem with GitPort = port }

  let setWsPort (port: uint16) (mem: RaftMember) =
    { mem with WsPort = port }
