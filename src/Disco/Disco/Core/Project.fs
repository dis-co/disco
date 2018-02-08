(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace rec Disco.Core

// * Imports

open System
open System.IO
open System.Text
open System.Reflection
open System.Collections.Generic
open Disco.Core.Utils
open Disco.Raft

#if FABLE_COMPILER

open Fable.Core
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System.Linq
open System.Net
open FlatBuffers
open Disco.Serialization

#endif

#if !FABLE_COMPILER && !DISCO_NODES

open LibGit2Sharp
open SharpYaml.Serialization

#endif

// * RaftConfig

//  ____        __ _    ____             __ _
// |  _ \ __ _ / _| |_ / ___|___  _ __  / _(_) __ _
// | |_) / _` | |_| __| |   / _ \| '_ \| |_| |/ _` |
// |  _ < (_| |  _| |_| |__| (_) | | | |  _| | (_| |
// |_| \_\__,_|_|  \__|\____\___/|_| |_|_| |_|\__, |
//                                            |___/

/// Configuration for Raft-specific, user-facing values.
type RaftConfig =
  { RequestTimeout:   Timeout
    ElectionTimeout:  Timeout
    MaxLogDepth:      int
    LogLevel:         Disco.Core.LogLevel
    DataDir:          FilePath
    MaxRetries:       int
    PeriodicInterval: Timeout }

  // ** optics

  static member RequestTimeout_ =
    (fun config -> config.RequestTimeout),
    (fun requestTimeout config -> { config with RequestTimeout = requestTimeout })

  static member ElectionTimeout_ =
    (fun config -> config.ElectionTimeout),
    (fun electionTimeout config -> { config with ElectionTimeout = electionTimeout })

  static member MaxLogDepth_ =
    (fun config -> config.MaxLogDepth),
    (fun maxLogDepth config -> { config with MaxLogDepth = maxLogDepth })

  static member LogLevel_ =
    (fun config -> config.LogLevel),
    (fun logLevel (config:RaftConfig) -> { config with LogLevel = logLevel })

  static member DataDir_ =
    (fun config -> config.DataDir),
    (fun dataDir config -> { config with DataDir = dataDir })

  static member MaxRetries_ =
    (fun config -> config.MaxRetries),
    (fun maxRetries config -> { config with MaxRetries = maxRetries })

  static member PeriodicInterval_ =
    (fun config -> config.PeriodicInterval),
    (fun periodicInterval config -> { config with PeriodicInterval = periodicInterval })

  // ** Default

  static member Default =
    { RequestTimeout   = Constants.RAFT_REQUEST_TIMEOUT * 1<ms>
      ElectionTimeout  = Constants.RAFT_ELECTION_TIMEOUT * 1<ms>
      PeriodicInterval = Constants.RAFT_PERIODIC_INTERVAL * 1<ms>
      MaxLogDepth      = Constants.RAFT_MAX_LOGDEPTH
      MaxRetries       = 10
      LogLevel         = LogLevel.Err
      DataDir          = filepath "" }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let lvl = self.LogLevel |> string |> builder.CreateString
    let dir = self.DataDir |> unwrap |> Option.mapNull builder.CreateString
    RaftConfigFB.StartRaftConfigFB(builder)
    RaftConfigFB.AddRequestTimeout(builder, int self.RequestTimeout)
    RaftConfigFB.AddElectionTimeout(builder, int self.ElectionTimeout)
    RaftConfigFB.AddMaxLogDepth(builder, self.MaxLogDepth)
    RaftConfigFB.AddLogLevel(builder, lvl)
    Option.iter (fun value -> RaftConfigFB.AddDataDir(builder,value)) dir
    RaftConfigFB.AddMaxRetries(builder, self.MaxRetries)
    RaftConfigFB.AddPeriodicInterval(builder, int self.PeriodicInterval)
    RaftConfigFB.EndRaftConfigFB(builder)

  // ** FromFB

  static member FromFB(fb: RaftConfigFB) =
    either {
      let! level = Disco.Core.LogLevel.TryParse fb.LogLevel
      return
        { RequestTimeout   = fb.RequestTimeout * 1<ms>
          ElectionTimeout  = fb.ElectionTimeout * 1<ms>
          MaxLogDepth      = fb.MaxLogDepth
          LogLevel         = level
          DataDir          = filepath fb.DataDir
          MaxRetries       = fb.MaxRetries
          PeriodicInterval = fb.PeriodicInterval * 1<ms> }
    }

// * RaftConfig module

module RaftConfig =
  open Aether

  // ** getters

  let requestTimeout = Optic.get RaftConfig.RequestTimeout_
  let electionTimeout = Optic.get RaftConfig.ElectionTimeout_
  let maxLogDepth = Optic.get RaftConfig.MaxLogDepth_
  let logLevel = Optic.get RaftConfig.LogLevel_
  let dataDir = Optic.get RaftConfig.DataDir_
  let maxRetries = Optic.get RaftConfig.MaxRetries_
  let periodicInterval = Optic.get RaftConfig.PeriodicInterval_

  // ** getters

  let setRequestTimeout = Optic.set RaftConfig.RequestTimeout_
  let setElectionTimeout = Optic.set RaftConfig.ElectionTimeout_
  let setMaxLogDepth = Optic.set RaftConfig.MaxLogDepth_
  let setLogLevel = Optic.set RaftConfig.LogLevel_
  let setDataDir = Optic.set RaftConfig.DataDir_
  let setMaxRetries = Optic.set RaftConfig.MaxRetries_
  let setPeriodicInterval = Optic.set RaftConfig.PeriodicInterval_

// * ClientExecutable

///   ____ _ _            _   _____                     _        _     _
///  / ___| (_) ___ _ __ | |_| ____|_  _____  ___ _   _| |_ __ _| |__ | | ___
/// | |   | | |/ _ \ '_ \| __|  _| \ \/ / _ \/ __| | | | __/ _` | '_ \| |/ _ \
/// | |___| | |  __/ | | | |_| |___ >  <  __/ (__| |_| | || (_| | |_) | |  __/
///  \____|_|_|\___|_| |_|\__|_____/_/\_\___|\___|\__,_|\__\__,_|_.__/|_|\___|

type ClientExecutable =
  { Id         : ClientId
    Executable : FilePath
    Version    : Disco.Core.Version
    Required   : bool }

  // ** optics

  static member Id_ =
    (fun (exe:ClientExecutable) -> exe.Id),
    (fun id (exe:ClientExecutable) -> { exe with Id = id })

  static member Executable_ =
    (fun (exe:ClientExecutable) -> exe.Executable),
    (fun executable (exe:ClientExecutable) -> { exe with Executable = executable })

  static member Version_ =
    (fun (exe:ClientExecutable) -> exe.Version),
    (fun version (exe:ClientExecutable) -> { exe with Version = version })

  static member Required_ =
    (fun (exe:ClientExecutable) -> exe.Required),
    (fun required (exe:ClientExecutable) -> { exe with Required = required })

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = ClientExecutableFB.CreateIdVector(builder,self.Id.ToByteArray())
    let path = self.Executable |> unwrap |> Option.mapNull builder.CreateString
    let version = self.Version |> unwrap |> Option.mapNull builder.CreateString
    ClientExecutableFB.StartClientExecutableFB(builder)
    ClientExecutableFB.AddId(builder, id)
    Option.iter (fun value -> ClientExecutableFB.AddExecutable(builder,value)) path
    Option.iter (fun value -> ClientExecutableFB.AddVersion(builder,value)) version
    ClientExecutableFB.AddRequired(builder, self.Required)
    ClientExecutableFB.EndClientExecutableFB(builder)

  // ** FromFB

  static member FromFB(fb: ClientExecutableFB) =
    either {
      let! id = Id.decodeId fb
      return {
        Id         = id
        Executable = filepath fb.Executable
        Version    = version fb.Version
        Required   = fb.Required
      }
    }

// * ClientExecutable module

module ClientExecutable =
  open Aether

  // ** getters

  let id = Optic.get ClientExecutable.Id_
  let executable = Optic.get ClientExecutable.Executable_
  let version = Optic.get ClientExecutable.Version_
  let required = Optic.get ClientExecutable.Required_

  // ** setters

  let setId = Optic.set ClientExecutable.Id_
  let setExecutable = Optic.set ClientExecutable.Executable_
  let setVersion = Optic.set ClientExecutable.Version_
  let setRequired = Optic.set ClientExecutable.Required_

// * ClientConfig

///   ____ _ _            _    ____             __ _
///  / ___| (_) ___ _ __ | |_ / ___|___  _ __  / _(_) __ _
/// | |   | | |/ _ \ '_ \| __| |   / _ \| '_ \| |_| |/ _` |
/// | |___| | |  __/ | | | |_| |__| (_) | | | |  _| | (_| |
///  \____|_|_|\___|_| |_|\__|\____\___/|_| |_|_| |_|\__, |
///                                                  |___/

type ClientConfig = ClientConfig of Map<ClientId,ClientExecutable>
  with
    // ** optics

    static member Map_ =
      (function ClientConfig map -> map),
      (fun map _ -> ClientConfig map)

    // ** Default

    static member Default with get() = ClientConfig Map.empty

    // ** Executables

    member config.Executables
      with get () = ClientConfig.executables config

    // ** ToOffset

    member self.ToOffset(builder: FlatBufferBuilder) =
      let exes =
        self.Executables
        |> Array.map (Binary.toOffset builder)
        |> fun offsets -> ClientConfigFB.CreateExecutablesVector(builder,offsets)
      ClientConfigFB.StartClientConfigFB(builder)
      ClientConfigFB.AddExecutables(builder, exes)
      ClientConfigFB.EndClientConfigFB(builder)

    // ** FromFB

    static member FromFB(fb: ClientConfigFB) =
      either {
        let! executables =
          List.fold
            (fun (m: Either<DiscoError, Map<ClientId,ClientExecutable>>) idx ->
              either {
                let! exes = m
                let! exe =
                  #if FABLE_COMPILER
                  fb.Executables(idx)
                  |> ClientExecutable.FromFB
                  #else
                  let exeish = fb.Executables(idx)
                  if exeish.HasValue then
                    let value = exeish.Value
                    ClientExecutable.FromFB value
                  else
                    "Could not parse empty ClientExecutableFB"
                    |> Error.asParseError "ClientConfig.FromFB"
                    |> Either.fail
                  #endif
                return Map.add exe.Id exe exes
              })
            (Right Map.empty)
            [ for n in 0 .. fb.ExecutablesLength - 1 -> n ]
        return ClientConfig executables
      }

// * ClientConfig module

module ClientConfig =

  // ** executables

  let executables = function
    | ClientConfig map -> map |> Map.toArray |> Array.map snd

  // ** ofList

  let ofList lst =
    List.fold
      (fun m (exe: ClientExecutable) -> Map.add exe.Id exe m)
      Map.empty
      lst
    |> ClientConfig

  // ** tryFind

  let tryFind (client: ClientId) = function
    | ClientConfig map -> Map.tryFind client map

// * TimingConfig

//  _____ _           _              ____             __ _
// |_   _(_)_ __ ___ (_)_ __   __ _ / ___|___  _ __  / _(_) __ _
//   | | | | '_ ` _ \| | '_ \ / _` | |   / _ \| '_ \| |_| |/ _` |
//   | | | | | | | | | | | | | (_| | |__| (_) | | | |  _| | (_| |
//   |_| |_|_| |_| |_|_|_| |_|\__, |\____\___/|_| |_|_| |_|\__, |
//                            |___/                        |___/

type TimingConfig =
  { Framebase : uint32
    Input     : string
    Servers   : IpAddress array
    UDPPort   : uint32
    TCPPort   : uint32 }

  // ** optics

  static member Framebase_ =
    (fun (config:TimingConfig) -> config.Framebase),
    (fun framebase (config:TimingConfig) -> { config with Framebase = framebase })

  static member Input_ =
    (fun (config:TimingConfig) -> config.Input),
    (fun input (config:TimingConfig) -> { config with Input = input })

  static member Servers_ =
    (fun (config:TimingConfig) -> config.Servers),
    (fun servers (config:TimingConfig) -> { config with Servers = servers })

  static member UdpPort_ =
    (fun (config:TimingConfig) -> config.UDPPort),
    (fun udpPort (config:TimingConfig) -> { config with UDPPort = udpPort })

  static member TcpPort_ =
    (fun (config:TimingConfig) -> config.TCPPort),
    (fun tcpPort (config:TimingConfig) -> { config with TCPPort = tcpPort })

  // ** Default

  static member Default =
    { Framebase = 50u
      Input     = "Disco Freerun"
      Servers   = [| |]
      UDPPort   = 8071u
      TCPPort   = 8072u }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let input = Option.mapNull builder.CreateString self.Input
    let servers =
      Array.map (string >> builder.CreateString) self.Servers
      |> fun offsets -> TimingConfigFB.CreateServersVector(builder,offsets)

    TimingConfigFB.StartTimingConfigFB(builder)
    TimingConfigFB.AddFramebase(builder,self.Framebase)
    Option.iter (fun value -> TimingConfigFB.AddInput(builder,value)) input
    TimingConfigFB.AddServers(builder, servers)
    TimingConfigFB.AddUDPPort(builder, self.UDPPort)
    TimingConfigFB.AddTCPPort(builder, self.TCPPort)
    TimingConfigFB.EndTimingConfigFB(builder)

  // ** FromFB

  static member FromFB(fb: TimingConfigFB) =
    either {
      let! (_,servers) =
        let arr =
          fb.ServersLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<DiscoError, int * IpAddress array>) _ ->
            either {
              let! (idx,servers) = m
              let! server =
                fb.Servers(idx)
                |> IpAddress.TryParse
              servers.[idx] <- server
              return (idx + 1, servers)
            })
          (Right(0, arr))
          arr

      return
        { Framebase = fb.Framebase
          Input     = fb.Input
          Servers   = servers
          UDPPort   = fb.UDPPort
          TCPPort   = fb.TCPPort }
    }

// * TimingConfig module

module TimingConfig =
  open Aether

  // ** getters

  let framebase = Optic.get TimingConfig.Framebase_
  let input = Optic.get TimingConfig.Input_
  let servers = Optic.get TimingConfig.Servers_
  let udpPort = Optic.get TimingConfig.UdpPort_
  let tcpPort = Optic.get TimingConfig.TcpPort_

  // ** setters

  let setFramebase = Optic.set TimingConfig.Framebase_
  let setInput = Optic.set TimingConfig.Input_
  let setServers = Optic.set TimingConfig.Servers_
  let setUdpPort = Optic.set TimingConfig.UdpPort_
  let setTcpPort = Optic.set TimingConfig.TcpPort_

// * AudioConfig

//     _             _ _        ____             __ _
//    / \  _   _  __| (_) ___  / ___|___  _ __  / _(_) __ _
//   / _ \| | | |/ _` | |/ _ \| |   / _ \| '_ \| |_| |/ _` |
//  / ___ \ |_| | (_| | | (_) | |__| (_) | | | |  _| | (_| |
// /_/   \_\__,_|\__,_|_|\___/ \____\___/|_| |_|_| |_|\__, |
//                                                    |___/

type AudioConfig =
  { SampleRate : uint32 }

  // ** optics

  static member SampleRate_ =
    (fun config -> config.SampleRate),
    (fun sr config -> { config with SampleRate = sr })

  // ** Default

  static member Default =
    { SampleRate = 48000u }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    AudioConfigFB.StartAudioConfigFB(builder)
    AudioConfigFB.AddSampleRate(builder, self.SampleRate)
    AudioConfigFB.EndAudioConfigFB(builder)

  // ** FromFB

  static member FromFB(fb: AudioConfigFB) =
    either {
      return { SampleRate = fb.SampleRate }
    }

// * AudioConfig module

module AudioConfig =

  open Aether

  let sampleRate = Optic.get AudioConfig.SampleRate_
  let setSampleRate = Optic.set AudioConfig.SampleRate_

// * HostGroup

//  _   _           _    ____
// | | | | ___  ___| |_ / ___|_ __ ___  _   _ _ __
// | |_| |/ _ \/ __| __| |  _| '__/ _ \| | | | '_ \
// |  _  | (_) \__ \ |_| |_| | | | (_) | |_| | |_) |
// |_| |_|\___/|___/\__|\____|_|  \___/ \__,_| .__/
//                                           |_|

type HostGroup =
  { Name    : Name
    Members : MemberId array }

  // ** optics

  static member Name_ =
    (fun (hostgroup:HostGroup) -> hostgroup.Name),
    (fun name (hostgroup:HostGroup) -> { hostgroup with Name = name })

  static member Members_ =
    (fun (hostgroup:HostGroup) -> hostgroup.Members),
    (fun members (hostgroup:HostGroup) -> { hostgroup with Members = members })

  // ** ToString

  override self.ToString() =
    sprintf "HostGroup:
              Name: %A
              Members: %A"
            self.Name
            (Array.fold (fun m s -> m + " " + string s) "" self.Members)

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString

    let members =
      Array.map (string >> builder.CreateString) self.Members
      |> fun offsets -> HostGroupFB.CreateMembersVector(builder, offsets)

    HostGroupFB.StartHostGroupFB(builder)
    Option.iter (fun value -> HostGroupFB.AddName(builder,value)) name
    HostGroupFB.AddMembers(builder,members)
    HostGroupFB.EndHostGroupFB(builder)

  // ** FromFB

  static member FromFB(fb: HostGroupFB) =
    either {
      let! (_,members) =
        let arr =
          fb.MembersLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<DiscoError, int * MemberId array>) _ ->
            either {
              let! (idx, ids) = m
              let! id = DiscoId.TryParse (fb.Members(idx))
              ids.[idx] <- id
              return (idx + 1, ids)
            })
          (Right(0, arr))
          arr

      return
        { Name    = name fb.Name
          Members = members }
    }

// * HostGroup module

module HostGroup =
  open Aether

  // ** getters

  let name = Optic.get HostGroup.Name_
  let members = Optic.get HostGroup.Members_

  // ** setters

  let setName = Optic.set HostGroup.Name_
  let setMembers = Optic.set HostGroup.Members_

// * ClusterMember

type ClusterMember =
  { Id:               MemberId
    HostName:         Name
    IpAddress:        IpAddress
    MulticastAddress: IpAddress
    MulticastPort:    Port
    HttpPort:         Port
    RaftPort:         Port
    WsPort:           Port
    GitPort:          Port
    ApiPort:          Port
    State:            MemberState
    Status:           MemberStatus }

  // ** optics

  static member Id_ =
    (fun (mem:ClusterMember) -> mem.Id),
    (fun id (mem:ClusterMember) -> { mem with Id = id })

  static member HostName_ =
    (fun (mem:ClusterMember) -> mem.HostName),
    (fun hostName (mem:ClusterMember) -> { mem with HostName = hostName })

  static member IpAddress_ =
    (fun (mem:ClusterMember) -> mem.IpAddress),
    (fun ipAddress (mem:ClusterMember) -> { mem with IpAddress = ipAddress })

  static member MulticastAddress_ =
    (fun (mem:ClusterMember) -> mem.MulticastAddress),
    (fun multicastAddress (mem:ClusterMember) -> { mem with MulticastAddress = multicastAddress })

  static member MulticastPort_ =
    (fun (mem:ClusterMember) -> mem.MulticastPort),
    (fun multicastPort (mem:ClusterMember) -> { mem with MulticastPort = multicastPort })

  static member RaftPort_ =
    (fun (mem:ClusterMember) -> mem.RaftPort),
    (fun raftPort (mem:ClusterMember) -> { mem with RaftPort = raftPort })

  static member HttpPort_ =
    (fun (mem:ClusterMember) -> mem.HttpPort),
    (fun httpPort (mem:ClusterMember) -> { mem with HttpPort = httpPort })

  static member WsPort_ =
    (fun (mem:ClusterMember) -> mem.WsPort),
    (fun wsPort (mem:ClusterMember) -> { mem with WsPort = wsPort })

  static member GitPort_ =
    (fun (mem:ClusterMember) -> mem.GitPort),
    (fun gitPort (mem:ClusterMember) -> { mem with GitPort = gitPort })

  static member ApiPort_ =
    (fun (mem:ClusterMember) -> mem.ApiPort),
    (fun apiPort (mem:ClusterMember) -> { mem with ApiPort = apiPort })

  static member State_ =
    (fun (mem:ClusterMember) -> mem.State),
    (fun state (mem:ClusterMember) -> { mem with State = state })

  static member Status_ =
    (fun (mem:ClusterMember) -> mem.Status),
    (fun status (mem:ClusterMember) -> { mem with Status = status })

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  #endif

  // ** ToOffset

  member mem.ToOffset (builder: FlatBufferBuilder) =
    let id = ClusterMemberFB.CreateIdVector(builder,mem.Id.ToByteArray())
    let ip = string mem.IpAddress |> builder.CreateString
    let mcastip = string mem.MulticastAddress |> builder.CreateString

    let hostname =
      let unwrapped = unwrap mem.HostName
      if isNull unwrapped then
        None
      else
        unwrapped |> builder.CreateString |> Some

    let state = mem.State.ToOffset(builder)
    let status = mem.Status.ToOffset()

    ClusterMemberFB.StartClusterMemberFB(builder)
    ClusterMemberFB.AddId(builder, id)

    match hostname with
    | Some hostname -> ClusterMemberFB.AddHostName(builder, hostname)
    | None -> ()

    ClusterMemberFB.AddMulticastAddress(builder, mcastip)
    ClusterMemberFB.AddMulticastPort(builder, unwrap mem.MulticastPort)
    ClusterMemberFB.AddIpAddress(builder, ip)
    ClusterMemberFB.AddRaftPort(builder, unwrap mem.RaftPort)
    ClusterMemberFB.AddHttpPort(builder, unwrap mem.HttpPort)
    ClusterMemberFB.AddWsPort(builder, unwrap mem.WsPort)
    ClusterMemberFB.AddGitPort(builder, unwrap mem.GitPort)
    ClusterMemberFB.AddApiPort(builder, unwrap mem.ApiPort)
    ClusterMemberFB.AddState(builder, state)
    ClusterMemberFB.AddStatus(builder, status)
    ClusterMemberFB.EndClusterMemberFB(builder)

  // ** FromFB

  static member FromFB (fb: ClusterMemberFB) : Either<DiscoError, ClusterMember> =
    either {
      let! id = Id.decodeId fb
      let! state = MemberState.FromFB fb.State
      let! status = MemberStatus.FromFB fb.Status
      let! ip = IpAddress.TryParse fb.IpAddress
      let! mcastip = IpAddress.TryParse fb.MulticastAddress
      return {
        Id               = id
        State            = state
        Status           = status
        HostName         = name fb.HostName
        IpAddress        = ip
        MulticastAddress = mcastip
        MulticastPort    = port fb.MulticastPort
        HttpPort         = port fb.HttpPort
        RaftPort         = port fb.RaftPort
        WsPort           = port fb.WsPort
        GitPort          = port fb.GitPort
        ApiPort          = port fb.ApiPort
      }
    }

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> ClusterMemberFB.GetRootAsClusterMemberFB
    |> ClusterMember.FromFB


// * ClusterMember module

module ClusterMember =

  open Aether

  // ** getters

  let id = Optic.get ClusterMember.Id_
  let hostName = Optic.get ClusterMember.HostName_
  let ipAddress = Optic.get ClusterMember.IpAddress_
  let multicastAddress = Optic.get ClusterMember.MulticastAddress_
  let multicastPort = Optic.get ClusterMember.MulticastPort_
  let raftPort = Optic.get ClusterMember.RaftPort_
  let httpPort = Optic.get ClusterMember.HttpPort_
  let wsPort = Optic.get ClusterMember.WsPort_
  let gitPort = Optic.get ClusterMember.GitPort_
  let apiPort = Optic.get ClusterMember.ApiPort_
  let status = Optic.get ClusterMember.Status_
  let state = Optic.get ClusterMember.State_

  // ** setters

  let setId = Optic.set ClusterMember.Id_
  let setHostName = Optic.set ClusterMember.HostName_
  let setIpAddress = Optic.set ClusterMember.IpAddress_
  let setMulticastAddress = Optic.set ClusterMember.MulticastAddress_
  let setMulticastPort = Optic.set ClusterMember.MulticastPort_
  let setRaftPort = Optic.set ClusterMember.RaftPort_
  let setHttpPort = Optic.set ClusterMember.HttpPort_
  let setWsPort = Optic.set ClusterMember.WsPort_
  let setGitPort = Optic.set ClusterMember.GitPort_
  let setApiPort = Optic.set ClusterMember.ApiPort_
  let setStatus = Optic.set ClusterMember.Status_
  let setState = Optic.set ClusterMember.State_

  // **  create

  let create id =
    #if FABLE_COMPILER
    let hostname = Fable.Import.Browser.window.location.host
    #else
    let hostname = Network.getHostName ()
    #endif
    { Id               = id
      HostName         = name hostname
      IpAddress        = IPv4Address "127.0.0.1"
      MulticastAddress = IpAddress.Parse Constants.DEFAULT_MCAST_ADDRESS
      MulticastPort    = Measure.port Constants.DEFAULT_MCAST_PORT
      HttpPort         = Measure.port Constants.DEFAULT_HTTP_PORT
      RaftPort         = Measure.port Constants.DEFAULT_RAFT_PORT
      WsPort           = Measure.port Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort          = Measure.port Constants.DEFAULT_GIT_PORT
      ApiPort          = Measure.port Constants.DEFAULT_API_PORT
      Status           = Running
      State            = Follower }

  // ** toRaftMember

  let toRaftMember (mem:ClusterMember) =
    { Id         = mem.Id
      IpAddress  = mem.IpAddress
      RaftPort   = mem.RaftPort
      Status     = mem.Status
      State      = mem.State
      Voting     = true
      VotedForMe = false
      NextIndex  = 1<index>
      MatchIndex = 0<index> }

// * ClusterConfig

//   ____ _           _
//  / ___| |_   _ ___| |_ ___ _ __
// | |   | | | | / __| __/ _ \ '__|
// | |___| | |_| \__ \ ||  __/ |
//  \____|_|\__,_|___/\__\___|_|

type ClusterConfig =
  { Id: ClusterId
    Name: Name
    Members: Map<MemberId,ClusterMember>
    Groups: HostGroup array }

  // ** optics

  static member Id_ =
    (fun (config:ClusterConfig) -> config.Id),
    (fun id (config:ClusterConfig) -> { config with Id = id })

  static member Name_ =
    (fun (config:ClusterConfig) -> config.Name),
    (fun name (config:ClusterConfig) -> { config with Name = name })

  static member Members_ =
    (fun (config:ClusterConfig) -> config.Members),
    (fun members (config:ClusterConfig) -> { config with Members = members })

  static member Groups_ =
    (fun (config:ClusterConfig) -> config.Groups),
    (fun groups (config:ClusterConfig) -> { config with Groups = groups })

  // ** Default

  static member Default
    with get () =
      { Id      = DiscoId.Create()
        Name    = name Constants.DEFAULT
        Members = Map.empty
        Groups  = [| |] }

  // ** ToString

  override self.ToString() =
    sprintf "Cluster [Id: %s Name: %A Members: %d Groups: %d]"
      (string self.Id)
      self.Name
      (Map.fold (fun m _ _ -> m + 1) 0 self.Members)
      (Array.length self.Groups)

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = ClusterConfigFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString

    let members =
      self.Members
      |> Map.toArray
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun offsets -> ClusterConfigFB.CreateMembersVector(builder, offsets)

    let groups =
      Array.map (Binary.toOffset builder) self.Groups
      |> fun offsets -> ClusterConfigFB.CreateGroupsVector(builder, offsets)

    ClusterConfigFB.StartClusterConfigFB(builder)
    ClusterConfigFB.AddId(builder, id)
    Option.iter (fun value -> ClusterConfigFB.AddName(builder,value)) name
    ClusterConfigFB.AddMembers(builder, members)
    ClusterConfigFB.AddGroups(builder, groups)
    ClusterConfigFB.EndClusterConfigFB(builder)

  // ** FromFB

  static member FromFB(fb: ClusterConfigFB) =
    either {
      let! (_,members) =
        let arr =
          fb.MembersLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<DiscoError, int * Map<MemberId,ClusterMember>>) _ ->
            either {
              let! (idx,members) = m

              let! mem =
                #if FABLE_COMPILER
                fb.Members(idx)
                |> ClusterMember.FromFB
                #else
                let memish = fb.Members(idx)
                if memish.HasValue then
                  let value = memish.Value
                  ClusterMember.FromFB value
                else
                  "Could not parse empty ClusterMemberFB"
                  |> Error.asParseError "Cluster.FromFB"
                  |> Either.fail
                #endif

              return (idx + 1, Map.add mem.Id mem members)
            })
          (Right(0, Map.empty))
          arr

      let! (_,groups) =
        let arr =
          fb.GroupsLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<DiscoError, int * HostGroup array>) _ ->
            either {
              let! (idx,groups) = m

              let! group =
                #if FABLE_COMPILER
                fb.Groups(idx)
                |> HostGroup.FromFB
                #else
                let groupish = fb.Groups(idx)
                if groupish.HasValue then
                  let value = groupish.Value
                  HostGroup.FromFB value
                else
                  "Could not parse empty HostGroupFB"
                  |> Error.asParseError "Cluster.FromFB"
                  |> Either.fail
                #endif

              groups.[idx] <- group
              return (idx + 1, groups)
            })
          (Right(0, arr))
          arr

      let! id = Id.decodeId fb

      return {
        Id      = id
        Name    = name fb.Name
        Members = members
        Groups  = groups
      }
    }

// * ClusterConfig module

module ClusterConfig =
  open Aether

  // ** getters

  let id = Optic.get ClusterConfig.Id_
  let name = Optic.get ClusterConfig.Name_
  let members = Optic.get ClusterConfig.Members_
  let groups = Optic.get ClusterConfig.Groups_

  // ** setters

  let setId = Optic.set ClusterConfig.Id_
  let setName = Optic.set ClusterConfig.Name_
  let setMembers = Optic.set ClusterConfig.Members_
  let setGroups = Optic.set ClusterConfig.Groups_

// * DiscoConfig

//  ___      _      ____             __ _
// |_ _|_ __(_)___ / ___|___  _ __  / _(_) __ _
//  | || '__| / __| |   / _ \| '_ \| |_| |/ _` |
//  | || |  | \__ \ |__| (_) | | | |  _| | (_| |
// |___|_|  |_|___/\____\___/|_| |_|_| |_|\__, |
//                                        |___/

type DiscoConfig =
  { Machine:    DiscoMachine
    ActiveSite: SiteId option
    Version:    string
    Audio:      AudioConfig
    Clients:    ClientConfig
    Raft:       RaftConfig
    Timing:     TimingConfig
    Sites:      Map<SiteId,ClusterConfig> }

  // ** optics

  static member Machine_ =
    (fun (config:DiscoConfig) -> config.Machine),
    (fun machine (config:DiscoConfig) -> { config with Machine = machine })

  static member ActiveSite_ =
    (fun (config:DiscoConfig) -> config.ActiveSite),
    (fun activeSite (config:DiscoConfig) -> { config with ActiveSite = activeSite })

  static member Version_ =
    (fun (config:DiscoConfig) -> config.Version),
    (fun version (config:DiscoConfig) -> { config with Version = version })

  static member Audio_ =
    (fun (config:DiscoConfig) -> config.Audio),
    (fun audio (config:DiscoConfig) -> { config with Audio = audio })

  static member Clients_ =
    (fun (config:DiscoConfig) -> config.Clients),
    (fun clients (config:DiscoConfig) -> { config with Clients = clients })

  static member Raft_ =
    (fun (config:DiscoConfig) -> config.Raft),
    (fun raft (config:DiscoConfig) -> { config with Raft = raft })

  static member Timing_ =
    (fun (config:DiscoConfig) -> config.Timing),
    (fun timing (config:DiscoConfig) -> { config with Timing = timing })

  static member Sites_ =
    (fun (config:DiscoConfig) -> config.Sites),
    (fun sites (config:DiscoConfig) -> { config with Sites = sites })

  // ** Default

  static member Default
    with get () =
      { Machine    = DiscoMachine.Default
        ActiveSite = None
        #if FABLE_COMPILER
        Version   = "0.0.0"
        #else
        Version   = System.Version(0,0,0).ToString()
        #endif
        Audio     = AudioConfig.Default
        Clients   = ClientConfig.Default
        Raft      = RaftConfig.Default
        Timing    = TimingConfig.Default
        Sites     = Map.empty }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let version = Option.mapNull builder.CreateString self.Version
    let audio = Binary.toOffset builder self.Audio
    let clients = Binary.toOffset builder self.Clients
    let raft = Binary.toOffset builder self.Raft
    let timing = Binary.toOffset builder self.Timing
    let machine = Binary.toOffset builder self.Machine

    let site =
      Option.map
        (fun (id:SiteId) -> ConfigFB.CreateActiveSiteVector(builder,id.ToByteArray()))
        self.ActiveSite

    let sites =
      self.Sites
      |> Map.toArray
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun sites -> ConfigFB.CreateSitesVector(builder, sites)

    ConfigFB.StartConfigFB(builder)
    Option.iter (fun value -> ConfigFB.AddVersion(builder,value)) version
    Option.iter (fun value -> ConfigFB.AddActiveSite(builder, value)) site
    ConfigFB.AddMachine(builder, machine)
    ConfigFB.AddAudioConfig(builder, audio)
    ConfigFB.AddClientConfig(builder, clients)
    ConfigFB.AddRaftConfig(builder, raft)
    ConfigFB.AddTimingConfig(builder, timing)
    ConfigFB.AddSites(builder, sites)
    ConfigFB.EndConfigFB(builder)

  // ** FromFB

  static member FromFB(fb: ConfigFB) =
    either {
      let version = fb.Version

      let! site =
        try
          if fb.ActiveSiteLength = 0
          then Either.succeed None
          else Id.decodeActiveSite fb |> Either.map Some
        with exn ->
          Either.succeed None

      let! machine =
        #if FABLE_COMPILER
        DiscoMachine.FromFB fb.Machine
        #else
        let nullable = fb.Machine
        if nullable.HasValue then
          let value = nullable.Value
          DiscoMachine.FromFB value
        else
          "Unable to parse empty DiscoMachineFB value"
          |> Error.asParseError "DiscoConfig.FromFB"
          |> Either.fail
        #endif

      let! audio =
        #if FABLE_COMPILER
        AudioConfig.FromFB fb.AudioConfig
        #else
        let audioish = fb.AudioConfig
        if audioish.HasValue then
          let value = audioish.Value
          AudioConfig.FromFB value
        else
          "Could not parse empty AudioConfigFB"
          |> Error.asParseError "DiscoConfig.FromFB"
          |> Either.fail
        #endif

      let! clients =
        #if FABLE_COMPILER
        ClientConfig.FromFB fb.ClientConfig
        #else
        let clientish = fb.ClientConfig
        if clientish.HasValue then
          let value = clientish.Value
          ClientConfig.FromFB value
        else
          "Could not parse empty ClientConfigFB"
          |> Error.asParseError "DiscoConfig.FromFB"
          |> Either.fail
        #endif

      let! raft =
        #if FABLE_COMPILER
        RaftConfig.FromFB fb.RaftConfig
        #else
        let raftish = fb.RaftConfig
        if raftish.HasValue then
          let value = raftish.Value
          RaftConfig.FromFB value
        else
          "Could not parse empty RaftConfigFB"
          |> Error.asParseError "DiscoConfig.FromFB"
          |> Either.fail
        #endif

      let! timing =
        #if FABLE_COMPILER
        TimingConfig.FromFB fb.TimingConfig
        #else
        let timingish = fb.TimingConfig
        if timingish.HasValue then
          let value = timingish.Value
          TimingConfig.FromFB value
        else
          "Could not parse empty TimingConfigFB"
          |> Error.asParseError "DiscoConfig.FromFB"
          |> Either.fail
        #endif

      let! (_, sites) =
        Array.fold
          (fun (m: Either<DiscoError, int * Map<SiteId,ClusterConfig>>) _ ->
            either {
              let! (idx, sites) = m
              let! site =
                #if FABLE_COMPILER
                fb.Sites(idx)
                |> ClusterConfig.FromFB
                #else
                let clusterish = fb.Sites(idx)
                if clusterish.HasValue then
                  let value = clusterish.Value
                  ClusterConfig.FromFB value
                else
                  "Could not parse empty ClusterConfigFB"
                  |> Error.asParseError "DiscoConfig.FromFB"
                  |> Either.fail
                #endif
              return (idx + 1, Map.add site.Id site sites)
            })
            (Right(0, Map.empty))
            [| 0 .. fb.SitesLength - 1 |]

      return {
        Machine    = machine
        ActiveSite = site
        Version    = version
        Audio      = audio
        Clients    = clients
        Raft       = raft
        Timing     = timing
        Sites      = sites
      }
    }

// * DiscoConfig module

module DiscoConfig =
  open Aether

  // ** getters

  let machine = Optic.get DiscoConfig.Machine_
  let activeSite = Optic.get DiscoConfig.ActiveSite_
  let version = Optic.get DiscoConfig.Version_
  let audio = Optic.get DiscoConfig.Audio_
  let clients = Optic.get DiscoConfig.Clients_
  let raft = Optic.get DiscoConfig.Raft_
  let timing = Optic.get DiscoConfig.Timing_
  let sites = Optic.get DiscoConfig.Sites_

  // ** setters

  let setMachine = Optic.set DiscoConfig.Machine_
  let setActiveSite = Optic.set DiscoConfig.ActiveSite_
  let setVersion = Optic.set DiscoConfig.Version_
  let setAudio = Optic.set DiscoConfig.Audio_
  let setClients = Optic.set DiscoConfig.Clients_
  let setRaft = Optic.set DiscoConfig.Raft_
  let setTiming = Optic.set DiscoConfig.Timing_
  let setSites = Optic.set DiscoConfig.Sites_

  // ** currentSite

  let currentSite (config:DiscoConfig) =
    activeSite config

// * ProjectYaml

#if !FABLE_COMPILER && !DISCO_NODES

[<RequireQualifiedAccess>]
module ProjectYaml =

  // ** ClientExecutableYaml

  type ClientExecutableYaml() =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Path: string
    [<DefaultValue>] val mutable Version: string
    [<DefaultValue>] val mutable Required: bool

  // ** TimingYaml

  type TimingYaml() =
    [<DefaultValue>] val mutable Framebase: int
    [<DefaultValue>] val mutable Input: string
    [<DefaultValue>] val mutable Servers: string array
    [<DefaultValue>] val mutable UDPPort: int
    [<DefaultValue>] val mutable TCPPort: int

  // ** AudioYaml

  type AudioYaml() =
    [<DefaultValue>] val mutable SampleRate: int

  // ** EngineYaml

  type EngineYaml() =
    [<DefaultValue>] val mutable LogLevel: string
    [<DefaultValue>] val mutable DataDir: string
    [<DefaultValue>] val mutable BindAddress: string
    [<DefaultValue>] val mutable RequestTimeout: int
    [<DefaultValue>] val mutable ElectionTimeout: int
    [<DefaultValue>] val mutable PeriodicInterval: int
    [<DefaultValue>] val mutable MaxLogDepth: int
    [<DefaultValue>] val mutable MaxRetries: int

  // ** GroupYaml

  type GroupYaml() =
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Members: string array

  // ** ClusterMemberYaml

  type ClusterMemberYaml() =
    [<DefaultValue>] val mutable Id:               string
    [<DefaultValue>] val mutable HostName:         string
    [<DefaultValue>] val mutable IpAddress:        string
    [<DefaultValue>] val mutable MulticastAddress: string
    [<DefaultValue>] val mutable MulticastPort:    uint16
    [<DefaultValue>] val mutable HttpPort:         uint16
    [<DefaultValue>] val mutable RaftPort:         uint16
    [<DefaultValue>] val mutable WsPort:           uint16
    [<DefaultValue>] val mutable GitPort:          uint16
    [<DefaultValue>] val mutable ApiPort:          uint16
    [<DefaultValue>] val mutable State:            string
    [<DefaultValue>] val mutable Status:           string

  // ** SiteYaml

  type SiteYaml () =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Members: ClusterMemberYaml array
    [<DefaultValue>] val mutable Groups: GroupYaml array

  // ** DiscoProjectYaml

  type DiscoProjectYaml() =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Version: string
    [<DefaultValue>] val mutable Copyright: string
    [<DefaultValue>] val mutable Author: string
    [<DefaultValue>] val mutable CreatedOn: string
    [<DefaultValue>] val mutable LastSaved: string
    [<DefaultValue>] val mutable ActiveSite: string
    [<DefaultValue>] val mutable Clients: ClientExecutableYaml array
    [<DefaultValue>] val mutable Engine: EngineYaml
    [<DefaultValue>] val mutable Timing: TimingYaml
    [<DefaultValue>] val mutable Audio: AudioYaml
    [<DefaultValue>] val mutable Sites:  SiteYaml array

  // ** parseTuple

  let internal parseTuple (input: string) : Either<DiscoError,int * int> =
    input.Split [| '('; ','; ' '; ')' |]       // split the string according to the specified chars
    |> Array.filter (String.length >> ((<) 0)) // filter out elements that have zero length
    |> fun parsed ->
      try
        match parsed with
        | [| x; y |] -> Right (int x, int y)
        | _ ->
          sprintf "Cannot parse %A as (int * int) tuple" input
          |> Error.asParseError "Config.parseTuple"
          |> Either.fail
      with
        | exn ->
          sprintf "Cannot parse %A as (int * int) tuple: %s" input exn.Message
          |> Error.asParseError "Config.parseTuple"
          |> Either.fail

  // ** parseStringProp

  let internal parseStringProp (str : string) : string option =
    if not (isNull str) && str.Length > 0 then Some(str) else None

  // ** parseAudio

  /// ### Parse the Audio configuration section
  ///
  /// Parses the Audio configuration section of the passed-in configuration file.
  ///
  /// # Returns: AudioConfig
  let internal parseAudio (config: DiscoProjectYaml) : Either<DiscoError, AudioConfig> =
    Either.tryWith (Error.asParseError "Config.parseAudio") <| fun _ ->
      { SampleRate = uint32 config.Audio.SampleRate }

  // ** saveAudio

  /// ### Save the AudioConfig value
  ///
  /// Transfer the configuration from `AudioConfig` values to a given config file.
  ///
  /// # Returns: ConfigFile
  let internal saveAudio (file: DiscoProjectYaml, config: DiscoConfig) =
    let audio = AudioYaml()
    audio.SampleRate <- int (config.Audio.SampleRate)
    file.Audio <- audio
    (file, config)

  // ** parseExecutable

  let internal parseExecutable (exe: ClientExecutableYaml) : Either<DiscoError, ClientExecutable> =
    either {
      let! id = DiscoId.TryParse exe.Id
      return {
        Id         = id
        Executable = filepath exe.Path
        Version    = version exe.Version
        Required   = exe.Required
      }
    }

  // ** parseClients

  let internal parseClients (file: DiscoProjectYaml) : Either<DiscoError,ClientConfig> =
    either {
      let! executables =
        Seq.fold
          (fun (m: Either<DiscoError,Map<ClientId,ClientExecutable>>) exe -> either {
            let! exes = m
            let! exe = parseExecutable exe
            return Map.add exe.Id exe exes
          })
          (Right Map.empty)
          file.Clients
      return ClientConfig executables
    }

  // ** saveClients

  let internal saveClients (file: DiscoProjectYaml, config: DiscoConfig) =
    let exes = ResizeArray()
    for exe in config.Clients.Executables do
      let entry = ClientExecutableYaml()
      entry.Id <- string exe.Id
      entry.Path <- unwrap exe.Executable
      entry.Version <- unwrap exe.Version
      entry.Required <- exe.Required
      exes.Add(entry)
    file.Clients <- exes.ToArray()
    (file, config)

  // ** parseRaft

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  /// ## Parses Raft-related values in passed configuration
  ///
  /// Parses the passed-in configuration file contents and returns a `RaftConfig` value.
  ///
  /// Returns: RaftConfig
  let internal parseRaft (config: DiscoProjectYaml) : Either<DiscoError, RaftConfig> =
    either {
      let! loglevel = Disco.Core.LogLevel.TryParse config.Engine.LogLevel

      try
        return
          { RequestTimeout   = config.Engine.RequestTimeout * 1<ms>
            ElectionTimeout  = config.Engine.ElectionTimeout * 1<ms>
            MaxLogDepth      = config.Engine.MaxLogDepth
            LogLevel         = loglevel
            DataDir          = filepath config.Engine.DataDir
            MaxRetries       = config.Engine.MaxRetries
            PeriodicInterval = config.Engine.PeriodicInterval * 1<ms> }
      with
        | exn ->
          return!
            sprintf "Could not parse Engine config: %s" exn.Message
            |> Error.asParseError "Config.parseRaft"
            |> Either.fail
    }

  // ** saveRaft

  /// ### Save the passed RaftConfig to the configuration file
  ///
  /// Save Raft algorithm specific configuration options to the configuration file object.
  ///
  /// # Returns: ConfigFile
  let internal saveRaft (file: DiscoProjectYaml, config: DiscoConfig) =
    let engine = EngineYaml()
    engine.RequestTimeout   <- int config.Raft.RequestTimeout
    engine.ElectionTimeout  <- int config.Raft.ElectionTimeout
    engine.MaxLogDepth      <- int config.Raft.MaxLogDepth
    engine.LogLevel         <- string config.Raft.LogLevel
    engine.DataDir          <- unwrap config.Raft.DataDir
    engine.MaxRetries       <- int config.Raft.MaxRetries
    engine.PeriodicInterval <- int config.Raft.PeriodicInterval
    file.Engine <- engine
    (file, config)

  // ** parseTiming

  //   _____ _           _
  //  |_   _(_)_ __ ___ (_)_ __   __ _
  //    | | | | '_ ` _ \| | '_ \ / _` |
  //    | | | | | | | | | | | | | (_| |
  //    |_| |_|_| |_| |_|_|_| |_|\__, |
  //                             |___/

  /// ### Parse the timing related configuration options
  ///
  /// Parse TimingConfig related values into a TimingConfig value and return it.
  ///
  /// # Returns: TimingConfig
  let internal parseTiming (config: DiscoProjectYaml) : Either<DiscoError,TimingConfig> =
    either {
      let timing = config.Timing
      let arr =
        timing.Servers
        |> Seq.length
        |> Array.zeroCreate

      let! (_,servers) =
        Seq.fold
          (fun (m: Either<DiscoError, int * IpAddress array>) thing -> either {
            let! (idx, lst) = m
            let! server = IpAddress.TryParse thing
            lst.[idx] <- server
            return (idx + 1, lst)
          })
          (Right(0, arr))
          timing.Servers

      try
        return
          { Framebase = uint32 timing.Framebase
            Input     = timing.Input
            Servers   = servers
            UDPPort   = uint32 timing.UDPPort
            TCPPort   = uint32 timing.TCPPort }
      with
        | exn ->
          return!
            sprintf "Could not parse Timing config: %s" exn.Message
            |> Error.asParseError "Config.parseTiming"
            |> Either.fail
    }

  // ** saveTiming

  /// Transfer the TimingConfig options to the passed configuration file
  let internal saveTiming (file: DiscoProjectYaml, config: DiscoConfig) =
    let timing = TimingYaml()
    timing.Framebase <- int (config.Timing.Framebase)
    timing.Input     <- config.Timing.Input

    let servers = ResizeArray()
    for srv in config.Timing.Servers do
      servers.Add(string srv)

    timing.Servers <- servers.ToArray()
    timing.TCPPort <- int (config.Timing.TCPPort)
    timing.UDPPort <- int (config.Timing.UDPPort)

    file.Timing <- timing
    (file, config)

  // ** parseMember

  let internal parseMember (yaml:ClusterMemberYaml) =
    either {
      let! id = DiscoId.TryParse yaml.Id
      let! ip = IpAddress.TryParse yaml.IpAddress
      let! mcastip = IpAddress.TryParse yaml.MulticastAddress
      let! state = MemberState.TryParse yaml.State
      let! status = MemberStatus.TryParse yaml.Status
      return {
        Id               = id
        HostName         = name yaml.HostName
        MulticastAddress = mcastip
        MulticastPort    = port yaml.MulticastPort
        IpAddress        = ip
        HttpPort         = port yaml.HttpPort
        RaftPort         = port yaml.RaftPort
        WsPort           = port yaml.WsPort
        GitPort          = port yaml.GitPort
        ApiPort          = port yaml.ApiPort
        State            = state
        Status           = status
      }
    }
  // ** saveMember

  let internal saveMember (mem:ClusterMember) =
    let yaml = ClusterMemberYaml()
    yaml.Id               <- string mem.Id
    yaml.HostName         <- unwrap mem.HostName
    yaml.IpAddress        <- string mem.IpAddress
    yaml.MulticastAddress <- string mem.MulticastAddress
    yaml.MulticastPort    <- unwrap mem.MulticastPort
    yaml.HttpPort         <- unwrap mem.HttpPort
    yaml.RaftPort         <- unwrap mem.RaftPort
    yaml.WsPort           <- unwrap mem.WsPort
    yaml.GitPort          <- unwrap mem.GitPort
    yaml.ApiPort          <- unwrap mem.ApiPort
    yaml.State            <- string mem.State
    yaml.Status           <- string mem.Status
    yaml


  // ** parseMembers

  /// ## Parse a collectio of Member definitions
  ///
  /// Parse an array of Member definitions. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - mems: MemberYaml collection
  ///
  /// Returns: Either<DiscoError, RaftMember array>
  let internal parseMembers mems : Either<DiscoError, Map<MemberId,ClusterMember>> =
    either {
      let! (_,mems) =
        Seq.fold
          (fun (m: Either<DiscoError, int * Map<MemberId,ClusterMember>>) mem -> either {
            let! (idx, mems) = m
            let! mem = parseMember mem
            return (idx + 1, Map.add mem.Id mem mems)
          })
          (Right(0, Map.empty))
          mems

      return mems
    }

  // ** parseGroup

  let internal parseGroup (group: GroupYaml) : Either<DiscoError, HostGroup> =
    either {
      let ids = Seq.map (string >> DiscoId.Parse) group.Members |> Seq.toArray
      return {
        Name = name group.Name
        Members = ids
      }
    }

  // ** parseGroups

  let internal parseGroups groups : Either<DiscoError, HostGroup array> =
    either {
      let arr =
        groups
        |> Seq.length
        |> Array.zeroCreate

      let! (_, groups) =
        Seq.fold
          (fun (m: Either<DiscoError, int * HostGroup array>) group -> either {
            let! (idx, groups) = m
            let! group = parseGroup group
            groups.[idx] <- group
            return (idx + 1, groups)
          })
          (Right(0,arr))
          groups

      return groups
    }

  // ** parseCluster


  /// ### Parse the Cluster configuration section
  ///
  /// Parse the cluster configuration section of a given configuration file into a `Cluster` value.
  ///
  /// # Returns: Cluster

  let internal parseCluster (cluster: SiteYaml) : Either<DiscoError, ClusterConfig> =
    either {
      let! groups = parseGroups cluster.Groups
      let! mems = parseMembers cluster.Members
      let! id = DiscoId.TryParse cluster.Id
      return {
        Id = id
        Name = name cluster.Name
        Members = mems
        Groups = groups
      }
    }

  // ** parseSites

  let internal parseSites (config: DiscoProjectYaml) =
    either {
      let! (_, sites) =
        Seq.fold
          (fun (m: Either<DiscoError, int * Map<SiteId,ClusterConfig>>) cfg ->
            either {
              let! (idx, sites) = m
              let! site = parseCluster cfg
              return (idx + 1, Map.add site.Id site sites)
            })
          (Right(0, Map.empty))
          config.Sites
      return sites
    }

  // ** saveSites

  /// ### Save a Cluster value to a configuration file
  ///
  /// Saves the passed `Cluster` value to the passed config file.
  ///
  /// # Returns: ConfigFile
  let internal saveSites (file: DiscoProjectYaml, config: DiscoConfig) =
    let sites = ResizeArray()

    match config.ActiveSite with
    | Some id -> file.ActiveSite <- string id
    | None -> file.ActiveSite <- null

    for KeyValue(id, cluster) in config.Sites do
      let cfg = SiteYaml()
      let members = ResizeArray()
      let groups = ResizeArray()

      cfg.Id <- string id
      cfg.Name <- unwrap cluster.Name

      for KeyValue(_,mem) in cluster.Members do
        let mem = saveMember mem
        members.Add(mem)

      for group in cluster.Groups do
        let g = GroupYaml()
        g.Name <- unwrap group.Name
        g.Members <- Array.map string group.Members
        groups.Add(g)

      cfg.Groups <- groups.ToArray()
      cfg.Members <- members.ToArray()
      sites.Add(cfg)

    file.Sites <- sites.ToArray()
    (file, config)

  // ** parseLastSaved (private)

  /// ### Parses the LastSaved property.
  ///
  /// Attempt to parse the LastSaved proptery from the passed `ConfigFile`.
  ///
  /// # Returns: DateTime option
  let internal parseLastSaved (config: DiscoProjectYaml) =
    if config.LastSaved.Length > 0
    then
      try
        Some(DateTime.Parse(config.LastSaved))
      with
        | _ -> None
    else None

  // ** parseCreatedOn (private)

  /// ### Parse the CreatedOn property
  ///
  /// Parse the CreatedOn property in a given ConfigFile. If the field is empty or DateTime.Parse
  /// fails to read it, the date returned will be the begin of the epoch.
  ///
  /// # Returns: DateTime
  let internal parseCreatedOn (config: DiscoProjectYaml) =
    if config.CreatedOn.Length > 0
    then
      try
        DateTime.Parse(config.CreatedOn)
      with
        | _ -> DateTime.FromFileTimeUtc(int64 0)
    else DateTime.FromFileTimeUtc(int64 0)

  // ** parse

  let internal parse (str: string) =
    try
      str
      |> Yaml.deserialize<DiscoProjectYaml>
      |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Error.asParseError "ProjectYaml.parse"
        |> Either.fail

#endif

// * Config Module

//   ____             __ _
//  / ___|___  _ __  / _(_) __ _
// | |   / _ \| '_ \| |_| |/ _` |
// | |__| (_) | | | |  _| | (_| |
//  \____\___/|_| |_|_| |_|\__, |
//                         |___/

[<RequireQualifiedAccess>]
module Config =
  // ** tag

  let private tag (str:string) = String.format "Client.{0}" str

  // ** fromFile

  #if !FABLE_COMPILER && !DISCO_NODES

  let fromFile (file: ProjectYaml.DiscoProjectYaml)
               (machine: DiscoMachine)
               : Either<DiscoError, DiscoConfig> =
    either {
      let  version   = file.Version
      let! raftcfg   = ProjectYaml.parseRaft      file
      let! timing    = ProjectYaml.parseTiming    file
      let! clients   = ProjectYaml.parseClients   file
      let! audio     = ProjectYaml.parseAudio     file
      let! sites     = ProjectYaml.parseSites     file

      let! site =
        if isNull file.ActiveSite || file.ActiveSite = ""
        then Right None
        else DiscoId.TryParse file.ActiveSite |> Either.map Some

      return {
        Machine    = machine
        ActiveSite = site
        Version    = version
        Clients    = clients
        Audio      = audio
        Raft       = raftcfg
        Timing     = timing
        Sites      = sites
      }
    }

  #endif

  // ** toFile

  #if !FABLE_COMPILER && !DISCO_NODES

  let toFile (config: DiscoConfig) (file: ProjectYaml.DiscoProjectYaml) =
    file.Version <- string config.Version
    (file, config)
    |> ProjectYaml.saveClients
    |> ProjectYaml.saveAudio
    |> ProjectYaml.saveRaft
    |> ProjectYaml.saveTiming
    |> ProjectYaml.saveSites
    |> ignore

  #endif

  // ** create

  let create (machine: DiscoMachine) =
    { Machine    = machine
      ActiveSite = None
      #if FABLE_COMPILER
      Version   = "0.0.0"
      #else
      Version   = Assembly.GetExecutingAssembly().GetName().Version.ToString()
      #endif
      Clients   = ClientConfig.Default
      Audio     = AudioConfig.Default
      Raft      = RaftConfig.Default
      Timing    = TimingConfig.Default
      Sites     = Map.empty }

  // ** updateMachine

  let updateMachine (machine: DiscoMachine) (config: DiscoConfig) =
    { config with Machine = machine }

  // ** updateClients

  let updateClients (clients: ClientConfig) (config: DiscoConfig) =
    { config with Clients = clients }

  // ** updateAudio

  let updateAudio (audio: AudioConfig) (config: DiscoConfig) =
    { config with Audio = audio }

  // ** updateEngine

  let updateEngine (engine: RaftConfig) (config: DiscoConfig) =
    { config with Raft = engine }

  // ** updateTiming

  let updateTiming (timing: TimingConfig) (config: DiscoConfig) =
    { config with Timing = timing }

  // ** updateSite

  let updateSite (site: ClusterConfig) (config: DiscoConfig) =
    { config with Sites = Map.add site.Id site config.Sites }

  // ** updateSites

  let updateSites (sites: Map<SiteId,ClusterConfig>) (config: DiscoConfig) =
    { config with Sites = sites }

  // ** findMember

  let findMember (config: DiscoConfig) (id: MemberId) =
    match config.ActiveSite with
    | Some active ->
      match Map.tryFind active config.Sites with
      | Some cluster ->
        match Map.tryFind id cluster.Members with
        | Some mem -> Either.succeed mem
        | _ ->
          ErrorMessages.PROJECT_MISSING_MEMBER + ": " + (string id)
          |> Error.asProjectError "Config.findMember"
          |> Either.fail
      | _ ->
        ErrorMessages.PROJECT_MISSING_CLUSTER + ": " + (string active)
        |> Error.asProjectError "Config.findMember"
        |> Either.fail
    | None ->
      ErrorMessages.PROJECT_NO_ACTIVE_CONFIG
      |> Error.asProjectError "Config.findMember"
      |> Either.fail

  // ** tryFindMember

  let tryFindMember (config: DiscoConfig) (id: MemberId) =
    match findMember config id with
    | Right mem -> Some mem
    | _ -> None

  // ** getMembers

  let getMembers (config: DiscoConfig) =
    match config.ActiveSite with
    | Some active ->
      match Map.tryFind active config.Sites with
      | Some site -> site.Members |> Either.succeed
      | None ->
        ErrorMessages.PROJECT_MISSING_CLUSTER + ": " + (string active)
        |> Error.asProjectError "Config.getMembers"
        |> Either.fail
    | None ->
      ErrorMessages.PROJECT_NO_ACTIVE_CONFIG
      |> Error.asProjectError "Config.getMembers"
      |> Either.fail

  // ** setActiveSite

  let setActiveSite (id: SiteId) (config: DiscoConfig) =
    if Map.containsKey id config.Sites
    then Right { config with ActiveSite = Some id }
    else
      ErrorMessages.PROJECT_MISSING_MEMBER + ": " + (string id)
      |> Error.asProjectError "Config.setActiveSite"
      |> Either.fail

  // ** getActiveSite

  let getActiveSite (config: DiscoConfig) =
    match config.ActiveSite with
    | Some id -> Map.tryFind id config.Sites
    | None -> None

  // ** getActiveMember

  let getActiveMember (config: DiscoConfig) =
    config
    |> getActiveSite
    |> Option.bind (fun (site: ClusterConfig) -> Map.tryFind config.Machine.MachineId site.Members)

  // ** setMembers

  let setMembers (mems: Map<MemberId,ClusterMember>) (config: DiscoConfig) =
    match config.ActiveSite with
    | Some active ->
      match Map.tryFind active config.Sites with
      | Some site ->
        updateSite { site with Members = mems } config
      | None -> config
    | None -> config

  // ** selfMember

  /// Find the current machine in the active site configuration.
  let selfMember (options: DiscoConfig) =
    findMember options options.Machine.MachineId

  // ** validateSettings

  /// Cross-check the settins in a given cluster member definition with this machines settings
  let validateSettings (mem: ClusterMember) (machine:DiscoMachine): Either<DiscoError,unit> =
    let errorMsg tag a b =
      sprintf "Member %s: %O is different from Machine %s: %O\n" tag a tag b
    let errors = [
      if mem.IpAddress <> machine.BindAddress then
        yield errorMsg "IP" mem.IpAddress machine.BindAddress
      if mem.RaftPort <> machine.RaftPort then
        yield errorMsg "Raft Port" mem.RaftPort machine.RaftPort
      if mem.GitPort <> machine.GitPort then
        yield errorMsg "Git Port" mem.GitPort machine.GitPort
      if mem.ApiPort <> machine.ApiPort then
        yield errorMsg "Api Port" mem.ApiPort machine.ApiPort
      if mem.WsPort <> machine.WsPort then
        yield errorMsg "WS Post" mem.WsPort machine.WsPort
    ]
    if List.isEmpty errors
    then Either.nothing
    else
      errors
      |> List.fold ((+)) ""
      |> Error.asProjectError (tag "validateSettings")
      |> Either.fail

  // ** addSitePrivate

  let private addSitePrivate (site: ClusterConfig) setActive (config: DiscoConfig) =
    let sites = Map.add site.Id site config.Sites
    if setActive
    then { config with ActiveSite = Some site.Id; Sites = sites }
    else { config with Sites = sites }

  // ** addSite

  /// Adds or replaces a site with same Id
  let addSite (site: ClusterConfig) (config: DiscoConfig) =
    addSitePrivate site false config

  // ** addSiteAndActive

  /// Adds or replaces a site with same Id and sets it as the active site
  let addSiteAndSetActive (site: ClusterConfig) (config: DiscoConfig) =
    addSitePrivate site true config

  // ** removeSite

  let removeSite (id: SiteId) (config: DiscoConfig) =
    { config with Sites = Map.remove id config.Sites }

  // ** siteByMember

  let siteByMember (memid: SiteId) (config: DiscoConfig) =
    Map.fold
      (fun (m: ClusterConfig option) _ site ->
        match m with
        | Some _ -> m
        | None ->
          if Map.containsKey memid site.Members
          then Some site
          else None)
      None
      config.Sites

  // ** findSite

  let findSite (id: SiteId) (config: DiscoConfig) =
    Map.tryFind id config.Sites

  // ** addMember

  let addMember (mem: ClusterMember) (config: DiscoConfig) =
    match config.ActiveSite with
    | Some active ->
      match Map.tryFind active config.Sites with
      | Some site ->
        let mems = Map.add mem.Id mem site.Members
        updateSite { site with Members = mems } config
      | None -> config
    | None -> config

  // ** removeMember

  let removeMember (id: MemberId) (config: DiscoConfig) =
    match config.ActiveSite with
    | Some active ->
      match Map.tryFind active config.Sites with
      | Some site ->
        let mems = Map.remove id site.Members
        updateSite { site with Members = mems } config
      | None -> config
    | None -> config

  // ** logLevel

  let logLevel (config: DiscoConfig) =
    config.Raft.LogLevel

  // ** setLogLevel

  let setLogLevel (level: Disco.Core.LogLevel) (config: DiscoConfig) =
    { config with Raft = { config.Raft with LogLevel = level } }

  // ** metadataPath

  let metadataPath (config: DiscoConfig) =
    unwrap config.Raft.DataDir <.> RAFT_METADATA_FILENAME + ASSET_EXTENSION

  // ** logDataPath

  let logDataPath (config: DiscoConfig) =
    unwrap config.Raft.DataDir <.> RAFT_LOGDATA_PATH

// * DiscoProject

//  ___      _     ____            _           _
// |_ _|_ __(_)___|  _ \ _ __ ___ (_) ___  ___| |_
//  | || '__| / __| |_) | '__/ _ \| |/ _ \/ __| __|
//  | || |  | \__ \  __/| | | (_) | |  __/ (__| |_
// |___|_|  |_|___/_|   |_|  \___// |\___|\___|\__|
//                              |__/

type DiscoProject =
  { Id        : ProjectId
    Name      : Name
    Path      : FilePath
    CreatedOn : TimeStamp
    LastSaved : TimeStamp option
    Copyright : string    option
    Author    : string    option
    Config    : DiscoConfig }

  // ** optics

  static member Id_ =
    (fun (project:DiscoProject) -> project.Id),
    (fun id (project:DiscoProject) -> { project with Id = id })

  static member Name_ =
    (fun (project:DiscoProject) -> project.Name),
    (fun name (project:DiscoProject) -> { project with Name = name })

  static member Path_ =
    (fun (project:DiscoProject) -> project.Path),
    (fun path (project:DiscoProject) -> { project with Path = path })

  static member CreatedOn_ =
    (fun (project:DiscoProject) -> project.CreatedOn),
    (fun createdOn (project:DiscoProject) -> { project with CreatedOn = createdOn })

  static member LastSaved_ =
    (fun (project:DiscoProject) -> project.LastSaved),
    (fun lastSaved (project:DiscoProject) -> { project with LastSaved = lastSaved })

  static member Copyright_ =
    (fun (project:DiscoProject) -> project.Copyright),
    (fun copyright (project:DiscoProject) -> { project with Copyright = copyright })

  static member Author_ =
    (fun (project:DiscoProject) -> project.Author),
    (fun author (project:DiscoProject) -> { project with Author = author })

  static member Config_ =
    (fun (project:DiscoProject) -> project.Config),
    (fun config (project:DiscoProject) -> { project with Config = config })

  // ** ToString

  override project.ToString() =
    sprintf @"
Id:        %s
Name:      %s
Path:      %s
Created:   %s
LastSaved: %A
Copyright: %A
Author:    %A
Config: %A
"
      (string project.Id)
      (unwrap project.Name)
      (unwrap project.Path)
      project.CreatedOn
      project.LastSaved
      project.Copyright
      project.Author
      project.Config

  // ** Empty

  static member Empty
    with get () =
      { Id        = DiscoId.Empty
        Name      = name Constants.EMPTY
        Path      = filepath ""
        CreatedOn = timestamp ""
        LastSaved = None
        Copyright = None
        Author    = None
        Config    = DiscoConfig.Default }

  // ** HasParent

  member project.HasParent with get () = false

  // ** AssetPath

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member project.AssetPath
    with get () =
      PROJECT_FILENAME + ASSET_EXTENSION
      |> filepath

  // ** Load

  #if !FABLE_COMPILER && !DISCO_NODES

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load (basepath: FilePath, machine: DiscoMachine) =
    either {
      let filename = PROJECT_FILENAME + ASSET_EXTENSION

      let normalizedPath =
        let withRoot =
          if Path.isPathRooted basepath then
            basepath
          else
            Path.getFullPath basepath
        if Path.endsWith filename withRoot then
          withRoot
        else
          unwrap withRoot <.> filename

      if not (File.exists normalizedPath) then
        return!
          sprintf "Project Not Found: %O" normalizedPath
          |> Error.asProjectError "Project.load"
          |> Either.fail
      else
        let! project = DiscoData.load normalizedPath
        return
          { project with
              Path   = Path.getDirectoryName normalizedPath |> unwrap |> filepath
              Config = Config.updateMachine machine project.Config }
    }

  // ** Save

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member project.Save (basepath: FilePath) =
    DiscoData.save basepath project

  #endif

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let strornull (str: string) =
      let nll = sprintf "%A" null
      match str with
      | null -> nll |> builder.CreateString
      | str -> str |> builder.CreateString
    let id = ProjectFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let path = self.Path |> unwrap |> Option.mapNull builder.CreateString
    let created = Option.mapNull builder.CreateString self.CreatedOn
    let lastsaved = Option.map strornull self.LastSaved
    let copyright = Option.map strornull self.Copyright
    let author = Option.map strornull self.Author
    let config = Binary.toOffset builder self.Config

    ProjectFB.StartProjectFB(builder)
    ProjectFB.AddId(builder, id)

    Option.iter (fun value -> ProjectFB.AddPath(builder,value)) path
    Option.iter (fun value -> ProjectFB.AddName(builder,value)) name
    Option.iter (fun value -> ProjectFB.AddCreatedOn(builder,value)) created
    Option.iter (fun offset -> ProjectFB.AddLastSaved(builder,offset)) lastsaved
    Option.iter (fun offset -> ProjectFB.AddCopyright(builder,offset)) copyright
    Option.iter (fun offset -> ProjectFB.AddAuthor(builder,offset)) author

    ProjectFB.AddConfig(builder, config)
    ProjectFB.EndProjectFB(builder)

  // ** ToBytes

  member self.ToBytes () =
    Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) =
    Binary.createBuffer bytes
    |> ProjectFB.GetRootAsProjectFB
    |> DiscoProject.FromFB

  // ** FromFB

  static member FromFB(fb: ProjectFB) =
    either {
      let nll = sprintf "%A" null

      let! lastsaved =
        match fb.LastSaved with
        | null    -> Right None
        | value when value = nll -> Right (Some null)
        | date -> Right (Some date)

      let! copyright =
        match fb.Copyright with
        | null   -> Right None
        | value when value = nll -> Right (Some null)
        | str -> Right (Some str)

      let! author =
        match fb.Author with
        | null   -> Right None
        | value when value = nll -> Right (Some null)
        | str -> Right (Some str)

      let! config =
        #if FABLE_COMPILER
        DiscoConfig.FromFB fb.Config
        #else
        let configish = fb.Config
        if configish.HasValue then
          let value = configish.Value
          DiscoConfig.FromFB value
        else
          "Could not parse empty ConfigFB"
          |> Error.asParseError "DiscoProject.FromFB"
          |> Either.fail
        #endif

      let! id = Id.decodeId fb

      return {
        Id        = id
        Name      = name fb.Name
        Path      = filepath fb.Path
        CreatedOn = fb.CreatedOn
        LastSaved = lastsaved
        Copyright = copyright
        Author    = author
        Config    = config
      }
    }

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYaml() =
    let config = ProjectYaml.DiscoProjectYaml()

    Config.toFile self.Config config

    // Project metadata
    config.Id        <- string self.Id
    config.Name      <- unwrap self.Name
    config.CreatedOn <- self.CreatedOn

    Option.map
      (fun author -> config.Author <- author)
      self.Author
    |> ignore

    Option.map
      (fun copyright -> config.Copyright <- copyright)
      self.Copyright
    |> ignore

    Option.map
      (fun saved -> config.LastSaved <- saved)
      self.LastSaved
    |> ignore

    config

  // ** FromYaml

  static member FromYaml(meta: ProjectYaml.DiscoProjectYaml) =
    either {
      let lastSaved =
        match meta.LastSaved with
          | null | "" -> None
          | str ->
            try
              DateTime.Parse str |> ignore
              Some str
            with
              | _ -> None

      let dummy = MachineConfig.create Constants.DEFAULT_IP None

      let! config = Config.fromFile meta dummy
      let! id = DiscoId.TryParse meta.Id

      return {
        Id        = id
        Name      = name meta.Name
        Path      = filepath (Path.GetFullPath ".")
        CreatedOn = timestamp meta.CreatedOn
        LastSaved = lastSaved
        Copyright = ProjectYaml.parseStringProp meta.Copyright
        Author    = ProjectYaml.parseStringProp meta.Author
        Config    = config
      }
    }

  #endif

// * Project module

[<RequireQualifiedAccess>]
module Project =

  open Aether

  // ** tag

  let private tag (str: string) = String.format "Project.{0}" str

  // ** getters

  let id = Optic.get DiscoProject.Id_
  let name = Optic.get DiscoProject.Name_
  let config = Optic.get DiscoProject.Config_
  let path = Optic.get DiscoProject.Path_

  // ** setters

  let setId = Optic.set DiscoProject.Id_
  let setName = Optic.set DiscoProject.Name_
  let setConfig = Optic.set DiscoProject.Config_
  let setPath = Optic.set DiscoProject.Path_

  // ** repository

  #if !FABLE_COMPILER && !DISCO_NODES

  /// ### Retrieve git repository
  ///
  /// Computes the path to the passed projects' git repository from its `Path` field and checks
  /// whether it exists. If so, construct a git Repository object and return that.

  let repository (project: DiscoProject) =
    Git.Repo.repository project.Path

  #endif

  // **  localRemote

  let localRemote (project: DiscoProject) =
    project.Config
    |> Config.getActiveMember
    |> Option.map (fun mem -> Uri.gitUri project.Name mem.IpAddress mem.GitPort)

  // ** currentBranch

  #if !FABLE_COMPILER && !DISCO_NODES

  let currentBranch (project: DiscoProject) =
    either {
      let! repo = repository project
      return Git.Branch.current repo
    }

  #endif

  // ** checkoutBranch

  #if !FABLE_COMPILER && !DISCO_NODES

  let checkoutBranch (name: string) (project: DiscoProject) =
    either {
      let! repo = repository project
      return! Git.Repo.checkout name repo
    }

  #endif

  // ** checkPath

  //  ____       _   _
  // |  _ \ __ _| |_| |__  ___
  // | |_) / _` | __| '_ \/ __|
  // |  __/ (_| | |_| | | \__ \
  // |_|   \__,_|\__|_| |_|___/

  #if !FABLE_COMPILER && !DISCO_NODES

  let checkPath (machine: DiscoMachine) (projectName: Name) =
    let file = PROJECT_FILENAME + ASSET_EXTENSION
    let path = machine.WorkSpace </> (unwrap projectName <.> file)
    if File.exists path |> not then
      sprintf "Project Not Found: %O" projectName
      |> Error.asProjectError (tag "checkPath")
      |> Either.fail
    else
      Either.succeed path

  #endif

  // ** filePath

  let filePath (project: DiscoProject) : FilePath =
    unwrap project.Path <.> PROJECT_FILENAME + ASSET_EXTENSION

  // ** userDir

  let userDir (project: DiscoProject) : FilePath =
    unwrap project.Path <.> USER_DIR

  // ** cueDir

  let cueDir (project: DiscoProject) : FilePath =
    unwrap project.Path <.> CUE_DIR

  // ** cuelistDir

  let cuelistDir (project: DiscoProject) : FilePath =
    unwrap project.Path <.> CUELIST_DIR

  // ** writeDaemonExportFile (private)

  #if !FABLE_COMPILER && !DISCO_NODES

  let private writeDaemonExportFile (repo: Repository) =
    either {
      let path = repo.Info.Path <.> "git-daemon-export-ok"
      let! _ = DiscoData.write path (Payload "")
      return ()
    }

  #endif

  // ** writeGitIgnoreFile (private)

  #if !FABLE_COMPILER && !DISCO_NODES

  let private writeGitIgnoreFile (repo: Repository) =
    either {
      let parent = Git.Repo.parentPath repo
      let path = parent </> filepath ".gitignore"
      let! _ = DiscoData.write path (Payload GITIGNORE)
      do! Git.Repo.stage repo path
    }

  #endif

  // ** createAssetDir (private)

  #if !FABLE_COMPILER && !DISCO_NODES

  let private createAssetDir (repo: Repository) (dir: FilePath) =
    either {
      let parent = Git.Repo.parentPath repo
      let target = parent </> dir
      do! FileSystem.mkDir target
      let gitkeep = target </> filepath ".gitkeep"
      let! _ = DiscoData.write gitkeep (Payload "")
      do! Git.Repo.stage repo gitkeep
    }

  #endif

  // ** commitPath (private)

  #if !FABLE_COMPILER && !DISCO_NODES

  /// ## commitPath
  ///
  /// commit a file at given path to git
  ///
  /// ### Signature:
  /// - committer : Signature of committer
  /// - msg       : commit msg
  /// - filepath  : path to file being committed
  /// - project   : DiscoProject
  ///
  /// Returns: (Commit * DiscoProject) option
  let private commitPath (filepath: FilePath)
                         (committer: Signature)
                         (msg : string)
                         (project: DiscoProject) :
                         Either<DiscoError,(Commit * DiscoProject)> =
    either {
      let! repo = repository project
      let abspath =
        if Path.isPathRooted filepath then
          filepath
        else
          project.Path </> filepath
      do! Git.Repo.stage repo abspath
      let! commit = Git.Repo.commit repo msg committer
      return commit, project
    }

  #endif

  // ** saveFile

  #if !FABLE_COMPILER && !DISCO_NODES

  let saveFile (path: FilePath)
               (contents: string)
               (committer: Signature)
               (msg : string)
               (project: DiscoProject) :
               Either<DiscoError,(Commit * DiscoProject)> =

    either {
      let info = File.info path
      do! info.Directory.FullName |> filepath |> FileSystem.mkDir
      let! _ = DiscoData.write path (Payload contents)
      return! commitPath path committer msg project
    }

  #endif

  // ** deleteFile

  #if !FABLE_COMPILER && !DISCO_NODES

  let deleteFile (path: FilePath)
                 (committer: Signature)
                 (msg : string)
                 (project: DiscoProject) :
                 Either<DiscoError,(Commit * DiscoProject)> =
    either {
      let! _ = DiscoData.remove path
      return! commitPath path committer msg project
    }

  #endif

  // ** saveAsset

  #if !FABLE_COMPILER && !DISCO_NODES

  /// ## saveAsset
  ///
  /// Attempt to save the passed thing, and, if succesful, return its
  /// FileInfo object.
  ///
  /// ### Signature:
  /// - thing: ^t the thing to save. Must implement certain methods/getters
  /// - committer: User the thing to save. Must implement certain methods/getters
  /// - project: Project to save file into
  ///
  /// Returns: Either<DiscoError,Commit * Project>
  let inline saveAsset (thing: ^t) (committer: User) (project: DiscoProject) =
    let payload = thing |> Yaml.encode
    let filepath = project.Path </> Asset.path thing
    let signature = committer.Signature
    let msg = String.Format("{0} saved {1}", committer.UserName, Path.getFileName filepath)
    saveFile filepath payload signature msg project

  #endif

  // ** deleteAsset

  #if !FABLE_COMPILER && !DISCO_NODES

  /// ## deleteAsset
  ///
  /// Delete a file path from disk and commit the change to git.
  ///
  /// ### Signature:
  /// - thing: ^t thing to delete
  /// - committer: User committing the change
  /// - msg: User committing the change
  /// - project: DiscoProject to work on
  ///
  /// Returns: Either<DiscoError, FileInfo * Commit * Project>
  let inline deleteAsset (thing: ^t) (committer: User) (project: DiscoProject) =
    let filepath = project.Path </> Asset.path thing
    let signature = committer.Signature
    let msg = String.Format("{0} deleted {1}", committer.UserName, filepath)
    deleteFile filepath signature msg project

  let private needsInit (project: DiscoProject) =
    let projPath = project.Path
    let projdir =  projPath |> Directory.exists
    let git = Directory.exists (projPath </> filepath ".git")
    let cues = Directory.exists (projPath </> filepath CUE_DIR)
    let cuelists = Directory.exists (projPath </> filepath CUELIST_DIR)
    let users = Directory.exists (projPath </> filepath USER_DIR)

    (not git)      ||
    (not cues)     ||
    (not cuelists) ||
    (not users)    ||
    (not projdir)

  #endif

  // ** initRepo (private)

  #if !FABLE_COMPILER && !DISCO_NODES

  /// ### Initialize the project git repository
  ///
  /// Given a project value, attempt to work out whether a git repository at that location already
  /// exists, otherwise creating it.
  ///
  /// # Returns: Repository
  let private initRepo (project: DiscoProject) : Either<DiscoError,unit> =
    either {
      let! repo = project.Path |> Git.Repo.init
      do! writeDaemonExportFile repo
      do! Git.Repo.setReceivePackConfig repo
      do! writeGitIgnoreFile repo
      do! List.fold
            (fun m dir -> Either.bind (fun () -> createAssetDir repo (filepath dir)) m)
            Either.nothing
            Constants.GLOBAL_ASSET_DIRS
      let relPath = Asset.path User.Admin
      let absPath = project.Path </> relPath
      let! _ =
        User.Admin
        |> Yaml.encode
        |> Payload
        |> DiscoData.write absPath
      do! Git.Repo.add repo relPath
      do! Git.Repo.stage repo absPath
    }

  #endif

  // ** create

  #if !FABLE_COMPILER && !DISCO_NODES

  /// ### Create a new project with the given name
  ///
  /// Create a new project with the given name. The default configuration will apply.
  ///
  /// # Returns: DiscoProject
  let create (path: FilePath) (projectName: string) (machine: DiscoMachine) =
    either {
      let project =
        { Id        = DiscoId.Create()
          Name      = Measure.name projectName
          Path      = path
          CreatedOn = Time.createTimestamp()
          LastSaved = Some (Time.createTimestamp ())
          Copyright = None
          Author    = None
          Config    = Config.create machine  }

      do! initRepo project
      let! _ = DiscoData.saveWithCommit path User.Admin.Signature project
      return project
    }

  #endif

  // ** updateDataDir

  let updateDataDir (raftDir: FilePath) (project: DiscoProject) : DiscoProject =
    { project.Config.Raft with DataDir = raftDir }
    |> flip Config.updateEngine project.Config
    |> flip setConfig project

  // ** addMember

  let addMember (mem: ClusterMember) (project: DiscoProject) : DiscoProject =
    project.Config
    |> Config.addMember mem
    |> flip setConfig project

  // ** updateMember

  let updateMember (mem: ClusterMember) (project: DiscoProject) : DiscoProject =
    addMember mem project

  // ** removeMember

  let removeMember (mem: MemberId) (project: DiscoProject) : DiscoProject =
    project.Config
    |> Config.removeMember mem
    |> flip setConfig project

  // ** findMember

  let findMember (mem: MemberId) (project: DiscoProject) =
    Config.findMember project.Config mem

  // ** selfMember

  let selfMember (project: DiscoProject) =
    Config.findMember project.Config project.Config.Machine.MachineId

  // ** addMembers

  let addMembers (mems: ClusterMember list) (project: DiscoProject) : DiscoProject =
    List.fold
      (fun config (mem: ClusterMember) ->
        Config.addMember mem config)
      project.Config
      mems
    |> flip setConfig project

  // ** updateMachine

  let updateMachine (machine: DiscoMachine) (project: DiscoProject) : DiscoProject =
    { project with Config = Config.updateMachine machine project.Config }

  // ** updateRemotes

  #if !FABLE_COMPILER && !DISCO_NODES

  /// Using the current active site configuration, update git remotes to reflect the configured
  /// members' details. This allows the service to use `git push` to those peers.
  let updateRemotes (project: DiscoProject) = either {
      let! repo = repository project

      // delete all current remotes
      let current = Git.Config.remotes repo
      do! Map.fold
            (fun kontinue name _ -> either {
              do! kontinue
              do! Git.Config.delRemote repo name })
            (Right ())
            current

      let! mem = Config.selfMember project.Config

      // add remotes for all other peers
      do! match Config.getActiveSite project.Config with
          | Some cluster ->
            Map.fold
              (fun kontinue id peer -> either {
                  do! kontinue
                  if id <> mem.Id then
                    let url = Uri.gitUri project.Name peer.IpAddress peer.GitPort
                    let name = string peer.Id
                    do! Git.Config.addRemote repo name url
                        |> Either.iterError (string >> Logger.err (tag "updateRemotes"))
                        |> Either.succeed
                })
              (Right ())
              cluster.Members
          | None -> Either.nothing
    }

  #endif

// * Machine module

module Machine =

  // ** toClusterMember

  let toClusterMember (machine: DiscoMachine) =
    { Id = machine.MachineId
      HostName = machine.HostName
      IpAddress = machine.BindAddress
      MulticastAddress = machine.MulticastAddress
      MulticastPort = machine.MulticastPort
      HttpPort = machine.WebPort
      RaftPort = machine.RaftPort
      WsPort = machine.WsPort
      GitPort = machine.GitPort
      ApiPort = machine.ApiPort
      State = MemberState.Follower
      Status = MemberStatus.Running }

  // ** toRaftMember

  let toRaftMember (machine: DiscoMachine) =
    { Member.create machine.MachineId with
        IpAddress = machine.BindAddress
        RaftPort = machine.RaftPort }
