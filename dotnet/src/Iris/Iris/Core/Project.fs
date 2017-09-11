namespace rec Iris.Core

// * Imports

open System
open System.IO
open System.Text
open System.Reflection
open System.Collections.Generic
open Iris.Core.Utils
open Iris.Raft

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.Linq
open System.Net
open FlatBuffers
open Iris.Serialization

#endif

#if !FABLE_COMPILER && !IRIS_NODES

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
    LogLevel:         Iris.Core.LogLevel
    DataDir:          FilePath
    MaxRetries:       int
    PeriodicInterval: Timeout }

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
      let! level = Iris.Core.LogLevel.TryParse fb.LogLevel
      return
        { RequestTimeout   = fb.RequestTimeout * 1<ms>
          ElectionTimeout  = fb.ElectionTimeout * 1<ms>
          MaxLogDepth      = fb.MaxLogDepth
          LogLevel         = level
          DataDir          = filepath fb.DataDir
          MaxRetries       = fb.MaxRetries
          PeriodicInterval = fb.PeriodicInterval * 1<ms> }
    }

// * ClientExecutable

///   ____ _ _            _   _____                     _        _     _
///  / ___| (_) ___ _ __ | |_| ____|_  _____  ___ _   _| |_ __ _| |__ | | ___
/// | |   | | |/ _ \ '_ \| __|  _| \ \/ / _ \/ __| | | | __/ _` | '_ \| |/ _ \
/// | |___| | |  __/ | | | |_| |___ >  <  __/ (__| |_| | || (_| | |_) | |  __/
///  \____|_|_|\___|_| |_|\__|_____/_/\_\___|\___|\__,_|\__\__,_|_.__/|_|\___|

type ClientExecutable =
  { Id         : ClientId
    Executable : FilePath
    Version    : Iris.Core.Version
    Required   : bool }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = Id.encodeId<ClientExecutableFB> builder self.Id
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

// * ClientConfig

///   ____ _ _            _    ____             __ _
///  / ___| (_) ___ _ __ | |_ / ___|___  _ __  / _(_) __ _
/// | |   | | |/ _ \ '_ \| __| |   / _ \| '_ \| |_| |/ _` |
/// | |___| | |  __/ | | | |_| |__| (_) | | | |  _| | (_| |
///  \____|_|_|\___|_| |_|\__|\____\___/|_| |_|_| |_|\__, |
///                                                  |___/

type ClientConfig = ClientConfig of Map<ClientId,ClientExecutable>
  with
    // ** Default

    static member Default = ClientConfig Map.empty

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
            (fun (m: Either<IrisError, Map<ClientId,ClientExecutable>>) idx ->
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

  // ** Default

  static member Default =
    { Framebase = 50u
      Input     = "Iris Freerun"
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
          (fun (m: Either<IrisError, int * IpAddress array>) _ ->
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

// * AudioConfig

//     _             _ _        ____             __ _
//    / \  _   _  __| (_) ___  / ___|___  _ __  / _(_) __ _
//   / _ \| | | |/ _` | |/ _ \| |   / _ \| '_ \| |_| |/ _` |
//  / ___ \ |_| | (_| | | (_) | |__| (_) | | | |  _| | (_| |
// /_/   \_\__,_|\__,_|_|\___/ \____\___/|_| |_|_| |_|\__, |
//                                                    |___/

type AudioConfig =
  { SampleRate : uint32 }

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

// * HostGroup

//  _   _           _    ____
// | | | | ___  ___| |_ / ___|_ __ ___  _   _ _ __
// | |_| |/ _ \/ __| __| |  _| '__/ _ \| | | | '_ \
// |  _  | (_) \__ \ |_| |_| | | | (_) | |_| | |_) |
// |_| |_|\___/|___/\__|\____|_|  \___/ \__,_| .__/
//                                           |_|

type HostGroup =
  { Name    : Name
    Members : Id array }

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
          (fun (m: Either<IrisError, int * Id array>) _ ->
            either {
              let! (idx, ids) = m
              let! id = Id.TryParse (fb.Members(idx))
              ids.[idx] <- id
              return (idx + 1, ids)
            })
          (Right(0, arr))
          arr

      return
        { Name    = name fb.Name
          Members = members }
    }

// * Cluster

//   ____ _           _
//  / ___| |_   _ ___| |_ ___ _ __
// | |   | | | | / __| __/ _ \ '__|
// | |___| | |_| \__ \ ||  __/ |
//  \____|_|\__,_|___/\__\___|_|

type ClusterConfig =
  { Id: Id
    Name: Name
    Members: Map<MemberId,RaftMember>
    Groups: HostGroup array }

  // ** Default

  static member Default
    with get () =
      { Id      = Id.Create()
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
    let id = Id.encodeId<ClusterConfigFB> builder self.Id
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
          (fun (m: Either<IrisError, int * Map<MemberId,RaftMember>>) _ ->
            either {
              let! (idx,members) = m

              let! mem =
                #if FABLE_COMPILER
                fb.Members(idx)
                |> RaftMember.FromFB
                #else
                let memish = fb.Members(idx)
                if memish.HasValue then
                  let value = memish.Value
                  RaftMember.FromFB value
                else
                  "Could not parse empty RaftMemberFB"
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
          (fun (m: Either<IrisError, int * HostGroup array>) _ ->
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

// * IrisConfig

//  ___      _      ____             __ _
// |_ _|_ __(_)___ / ___|___  _ __  / _(_) __ _
//  | || '__| / __| |   / _ \| '_ \| |_| |/ _` |
//  | || |  | \__ \ |__| (_) | | | |  _| | (_| |
// |___|_|  |_|___/\____\___/|_| |_|_| |_|\__, |
//                                        |___/

type IrisConfig =
  { Machine:    IrisMachine
    ActiveSite: Id option
    Version:    string
    Audio:      AudioConfig
    Clients:    ClientConfig
    Raft:       RaftConfig
    Timing:     TimingConfig
    Sites:      ClusterConfig array }

  // ** Default

  static member Default
    with get () =
      { Machine    = IrisMachine.Default
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
        Sites     = [| |] }

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
        (Id.encodeActiveSite<ConfigFB> builder)
         self.ActiveSite

    let sites =
      Array.map (Binary.toOffset builder) self.Sites
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
        IrisMachine.FromFB fb.Machine
        #else
        let nullable = fb.Machine
        if nullable.HasValue then
          let value = nullable.Value
          IrisMachine.FromFB value
        else
          "Unable to parse empty IrisMachineFB value"
          |> Error.asParseError "IrisConfig.FromFB"
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
          |> Error.asParseError "IrisConfig.FromFB"
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
          |> Error.asParseError "IrisConfig.FromFB"
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
          |> Error.asParseError "IrisConfig.FromFB"
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
          |> Error.asParseError "IrisConfig.FromFB"
          |> Either.fail
        #endif

      let! (_, sites) =
        let arr =
          fb.SitesLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * ClusterConfig array>) _ ->
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
                  |> Error.asParseError "IrisConfig.FromFB"
                  |> Either.fail
                #endif
              sites.[idx] <- site
              return (idx + 1, sites)
            })
            (Right(0, arr))
            arr

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

// * ProjectYaml

#if !FABLE_COMPILER && !IRIS_NODES

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

  // ** SiteYaml

  type SiteYaml () =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Members: RaftMemberYaml array
    [<DefaultValue>] val mutable Groups: GroupYaml array

  // ** IrisProjectYaml

  type IrisProjectYaml() =
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

  let internal parseTuple (input: string) : Either<IrisError,int * int> =
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
  let internal parseAudio (config: IrisProjectYaml) : Either<IrisError, AudioConfig> =
    Either.tryWith (Error.asParseError "Config.parseAudio") <| fun _ ->
      { SampleRate = uint32 config.Audio.SampleRate }

  // ** saveAudio

  /// ### Save the AudioConfig value
  ///
  /// Transfer the configuration from `AudioConfig` values to a given config file.
  ///
  /// # Returns: ConfigFile
  let internal saveAudio (file: IrisProjectYaml, config: IrisConfig) =
    let audio = AudioYaml()
    audio.SampleRate <- int (config.Audio.SampleRate)
    file.Audio <- audio
    (file, config)

  // ** parseExecutable

  let internal parseExecutable (exe: ClientExecutableYaml) : Either<IrisError, ClientExecutable> =
    either {
      let! id = Id.TryParse exe.Id
      return {
        Id         = id
        Executable = filepath exe.Path
        Version    = version exe.Version
        Required   = exe.Required
      }
    }

  // ** parseClients

  let internal parseClients (file: IrisProjectYaml) : Either<IrisError,ClientConfig> =
    either {
      let! executables =
        Seq.fold
          (fun (m: Either<IrisError,Map<ClientId,ClientExecutable>>) exe -> either {
            let! exes = m
            let! exe = parseExecutable exe
            return Map.add exe.Id exe exes
          })
          (Right Map.empty)
          file.Clients
      return ClientConfig executables
    }

  // ** saveClients

  let internal saveClients (file: IrisProjectYaml, config: IrisConfig) =
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
  let internal parseRaft (config: IrisProjectYaml) : Either<IrisError, RaftConfig> =
    either {
      let! loglevel = Iris.Core.LogLevel.TryParse config.Engine.LogLevel

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
  let internal saveRaft (file: IrisProjectYaml, config: IrisConfig) =
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
  let internal parseTiming (config: IrisProjectYaml) : Either<IrisError,TimingConfig> =
    either {
      let timing = config.Timing
      let arr =
        timing.Servers
        |> Seq.length
        |> Array.zeroCreate

      let! (_,servers) =
        Seq.fold
          (fun (m: Either<IrisError, int * IpAddress array>) thing -> either {
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

  /// ### Transfer the TimingConfig options to the passed configuration file
  ///
  ///
  ///
  /// # Returns: ConfigFile
  let internal saveTiming (file: IrisProjectYaml, config: IrisConfig) =
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

  //    ____ _           _
  //   / ___| |_   _ ___| |_ ___ _ __
  //  | |   | | | | / __| __/ _ \ '__|
  //  | |___| | |_| \__ \ ||  __/ |
  //   \____|_|\__,_|___/\__\___|_|
  //

  /// ## Parse a single Member definition
  ///
  /// Parse a single Member definition. Returns a ParseError on failiure.
  ///
  /// ### Signature:
  /// - mem: MemberYaml
  ///
  /// Returns: Either<IrisError, RaftMember>
  let internal parseMember (mem: RaftMemberYaml) : Either<IrisError, RaftMember> =
    either {
      try
        let! id = Id.TryParse mem.Id
        let! ip = IpAddress.TryParse mem.IpAddr
        let! state = RaftMemberState.TryParse mem.State
        return {
          Id         = id
          HostName   = name mem.HostName
          IpAddr     = ip
          Port       = mem.Port    |> uint16 |> port
          WsPort     = mem.WsPort  |> uint16 |> port
          GitPort    = mem.GitPort |> uint16 |> port
          ApiPort    = mem.ApiPort |> uint16 |> port
          State      = state
          Voting     = true
          VotedForMe = false
          NextIndex  = index 1
          MatchIndex = index 0
        }
      with exn ->
        return!
          sprintf "Could not parse Member definition: %s" exn.Message
          |> Error.asParseError "Config.parseMember"
          |> Either.fail
    }

  // ** parseMember

  /// ## Parse a collectio of Member definitions
  ///
  /// Parse an array of Member definitions. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - mems: MemberYaml collection
  ///
  /// Returns: Either<IrisError, RaftMember array>
  let internal parseMembers mems : Either<IrisError, Map<MemberId,RaftMember>> =
    either {
      let! (_,mems) =
        Seq.fold
          (fun (m: Either<IrisError, int * Map<MemberId,RaftMember>>) mem -> either {
            let! (idx, mems) = m
            let! mem = parseMember mem
            return (idx + 1, Map.add mem.Id mem mems)
          })
          (Right(0, Map.empty))
          mems

      return mems
    }

  // ** parseGroup

  let internal parseGroup (group: GroupYaml) : Either<IrisError, HostGroup> =
    either {
      let ids = Seq.map (string >> Id.Parse) group.Members |> Seq.toArray
      return {
        Name = name group.Name
        Members = ids
      }
    }

  // ** parseGroups

  let internal parseGroups groups : Either<IrisError, HostGroup array> =
    either {
      let arr =
        groups
        |> Seq.length
        |> Array.zeroCreate

      let! (_, groups) =
        Seq.fold
          (fun (m: Either<IrisError, int * HostGroup array>) group -> either {
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

  let internal parseCluster (cluster: SiteYaml) : Either<IrisError, ClusterConfig> =
    either {
      let! groups = parseGroups cluster.Groups
      let! mems = parseMembers cluster.Members
      let! id = Id.TryParse cluster.Id
      return {
        Id = id
        Name = name cluster.Name
        Members = mems
        Groups = groups
      }
    }

  // ** parseSites

  let internal parseSites (config: IrisProjectYaml) : Either<IrisError, ClusterConfig array> =
    either {
      let arr =
        config.Sites
        |> Seq.length
        |> Array.zeroCreate

      let! (_, sites) =
        Seq.fold
          (fun (m: Either<IrisError, int * ClusterConfig array>) cfg ->
            either {
              let! (idx, sites) = m
              let! site = parseCluster cfg
              sites.[idx] <- site
              return (idx + 1, sites)
            })
          (Right(0, arr))
          config.Sites

      return sites
    }

  // ** saveSites

  /// ### Save a Cluster value to a configuration file
  ///
  /// Saves the passed `Cluster` value to the passed config file.
  ///
  /// # Returns: ConfigFile
  let internal saveSites (file: IrisProjectYaml, config: IrisConfig) =
    let sites = ResizeArray()

    match config.ActiveSite with
    | Some id -> file.ActiveSite <- string id
    | None -> file.ActiveSite <- null

    for cluster in config.Sites do
      let cfg = SiteYaml()
      let members = ResizeArray()
      let groups = ResizeArray()

      cfg.Id <- string cluster.Id
      cfg.Name <- unwrap cluster.Name

      for KeyValue(memId,mem) in cluster.Members do
        let n = RaftMemberYaml()
        n.Id       <- string memId
        n.IpAddr   <- string mem.IpAddr
        n.HostName <- unwrap mem.HostName
        n.Port     <- unwrap mem.Port
        n.WsPort   <- unwrap mem.WsPort
        n.GitPort  <- unwrap mem.GitPort
        n.ApiPort  <- unwrap mem.ApiPort
        n.State    <- string mem.State
        members.Add(n)

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
  let internal parseLastSaved (config: IrisProjectYaml) =
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
  let internal parseCreatedOn (config: IrisProjectYaml) =
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
      |> Yaml.deserialize<IrisProjectYaml>
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

  // ** fromFile

  #if !FABLE_COMPILER && !IRIS_NODES

  let fromFile (file: ProjectYaml.IrisProjectYaml)
               (machine: IrisMachine)
               : Either<IrisError, IrisConfig> =
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
        else Id.TryParse file.ActiveSite |> Either.map Some

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

  #if !FABLE_COMPILER && !IRIS_NODES

  let toFile (config: IrisConfig) (file: ProjectYaml.IrisProjectYaml) =
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

  let create (machine: IrisMachine) =
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
      Sites     = [| |] }

  // ** updateMachine

  let updateMachine (machine: IrisMachine) (config: IrisConfig) =
    { config with Machine = machine }

  // ** updateClients

  let updateClients (clients: ClientConfig) (config: IrisConfig) =
    { config with Clients = clients }

  // ** updateAudio

  let updateAudio (audio: AudioConfig) (config: IrisConfig) =
    { config with Audio = audio }

  // ** updateEngine

  let updateEngine (engine: RaftConfig) (config: IrisConfig) =
    { config with Raft = engine }

  // ** updateTiming

  let updateTiming (timing: TimingConfig) (config: IrisConfig) =
    { config with Timing = timing }

  // ** updateCluster

  let updateCluster (cluster: ClusterConfig) (config: IrisConfig) =
    let sites =
      Array.map
        (fun (site: ClusterConfig) ->
          if cluster.Id = site.Id
          then cluster
          else site)
        config.Sites
    { config with Sites = sites }

  // ** updateSites

  let updateSites (sites: ClusterConfig array) (config: IrisConfig) =
    { config with Sites = sites }

  // ** findMember

  let findMember (config: IrisConfig) (id: Id) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst: ClusterConfig) -> clst.Id = active) config.Sites with
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

  // ** getMembers

  let getMembers (config: IrisConfig) : Either<IrisError,Map<MemberId,RaftMember>> =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst: ClusterConfig) -> clst.Id = active) config.Sites with
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

  let setActiveSite (id: Id) (config: IrisConfig) =
    if config.Sites |> Array.exists (fun x -> x.Id = id)
    then Right { config with ActiveSite = Some id }
    else
      ErrorMessages.PROJECT_MISSING_MEMBER + ": " + (string id)
      |> Error.asProjectError "Config.setActiveSite"
      |> Either.fail

  // ** getActiveSite

  let getActiveSite (config: IrisConfig) =
    match config.ActiveSite with
    | Some id -> Array.tryFind (fun (site: ClusterConfig) -> site.Id = id) config.Sites
    | None -> None

  // ** getActiveMember

  let getActiveMember (config: IrisConfig) =
    config
    |> getActiveSite
    |> Option.bind (fun (site: ClusterConfig) -> Map.tryFind config.Machine.MachineId site.Members)

  // ** setMembers

  let setMembers (mems: Map<MemberId,RaftMember>) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst: ClusterConfig) -> clst.Id = active) config.Sites with
      | Some site ->
        updateCluster { site with Members = mems } config
      | None -> config
    | None -> config

  // ** selfMember

  let selfMember (options: IrisConfig) =
    findMember options options.Machine.MachineId

  // ** addSitePrivate

  let private addSitePrivate (site: ClusterConfig) setActive (config: IrisConfig) =
    let i = config.Sites |> Array.tryFindIndex (fun s -> s.Id = site.Id)
    let copy = Array.zeroCreate (config.Sites.Length + (if Option.isSome i then 0 else 1))
    Array.iteri (fun i s -> copy.[i] <- s) config.Sites
    copy.[match i with Some i -> i | None -> config.Sites.Length] <- site
    if setActive
    then { config with ActiveSite = Some site.Id; Sites = copy }
    else { config with Sites = copy }

  // ** addSite

  /// Adds or replaces a site with same Id
  let addSite (site: ClusterConfig) (config: IrisConfig) =
    addSitePrivate site false config

  // ** addSiteAndActive

  /// Adds or replaces a site with same Id and sets it as the active site
  let addSiteAndSetActive (site: ClusterConfig) (config: IrisConfig) =
    addSitePrivate site true config

  // ** removeSite

  let removeSite (id: Id) (config: IrisConfig) =
    let sites = Array.filter (fun (site: ClusterConfig) -> site.Id <> id) config.Sites
    { config with Sites = sites }

  // ** siteByMember

  let siteByMember (memid: Id) (config: IrisConfig) =
    Array.fold
      (fun (m: ClusterConfig option) site ->
        match m with
        | Some _ -> m
        | None ->
          if Map.containsKey memid site.Members then
            Some site
          else None)
      None
      config.Sites

  // ** findSite

  let findSite (id: Id) (config: IrisConfig) =
    Array.tryFind (fun (site: ClusterConfig) -> site.Id = id) config.Sites

  // ** addMember

  let addMember (mem: RaftMember) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst: ClusterConfig) -> clst.Id = active) config.Sites with
      | Some site ->
        let mems = Map.add mem.Id mem site.Members
        updateCluster { site with Members = mems } config
      | None -> config
    | None -> config

  // ** removeMember

  let removeMember (id: Id) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst:ClusterConfig) -> clst.Id = active) config.Sites with
      | Some site ->
        let mems = Map.remove id site.Members
        updateCluster { site with Members = mems } config
      | None -> config
    | None -> config

  // ** logLevel

  let logLevel (config: IrisConfig) =
    config.Raft.LogLevel

  // ** setLogLevel

  let setLogLevel (level: Iris.Core.LogLevel) (config: IrisConfig) =
    { config with Raft = { config.Raft with LogLevel = level } }

  // ** metadataPath

  let metadataPath (config: IrisConfig) =
    unwrap config.Raft.DataDir <.> RAFT_METADATA_FILENAME + ASSET_EXTENSION

  // ** logDataPath

  let logDataPath (config: IrisConfig) =
    unwrap config.Raft.DataDir <.> RAFT_LOGDATA_PATH

// * IrisProject

//  ___      _     ____            _           _
// |_ _|_ __(_)___|  _ \ _ __ ___ (_) ___  ___| |_
//  | || '__| / __| |_) | '__/ _ \| |/ _ \/ __| __|
//  | || |  | \__ \  __/| | | (_) | |  __/ (__| |_
// |___|_|  |_|___/_|   |_|  \___// |\___|\___|\__|
//                              |__/

type IrisProject =
  { Id        : Id
    Name      : Name
    Path      : FilePath
    CreatedOn : TimeStamp
    LastSaved : TimeStamp option
    Copyright : string    option
    Author    : string    option
    Config    : IrisConfig }

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
      { Id        = Id.Empty
        Name      = name Constants.EMPTY
        Path      = filepath ""
        CreatedOn = timestamp ""
        LastSaved = None
        Copyright = None
        Author    = None
        Config    = IrisConfig.Default }

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

  #if !FABLE_COMPILER && !IRIS_NODES

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load (basepath: FilePath, machine: IrisMachine) =
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
        let! project = IrisData.load normalizedPath
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
    IrisData.save basepath project

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
    let id = Id.encodeId<ProjectFB> builder self.Id
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
    |> IrisProject.FromFB

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
        IrisConfig.FromFB fb.Config
        #else
        let configish = fb.Config
        if configish.HasValue then
          let value = configish.Value
          IrisConfig.FromFB value
        else
          "Could not parse empty ConfigFB"
          |> Error.asParseError "IrisProject.FromFB"
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

  #if !FABLE_COMPILER && !IRIS_NODES

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYaml() =
    let config = ProjectYaml.IrisProjectYaml()

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

  static member FromYaml(meta: ProjectYaml.IrisProjectYaml) =
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
      let! id = Id.TryParse meta.Id

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

  // ** tag

  let private tag (str: string) = String.format "Project.{0}" str

  // ** toFilePath

  let toFilePath (path: FilePath) =
    path |> unwrap |> filepath

  // ** ofFilePath

  let ofFilePath (path: FilePath) =
    path |> unwrap |> filepath

  // ** repository

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ### Retrieve git repository
  ///
  /// Computes the path to the passed projects' git repository from its `Path` field and checks
  /// whether it exists. If so, construct a git Repository object and return that.
  ///
  /// # Returns: Repository option
  let repository (project: IrisProject) =
    project.Path
    |> Git.Repo.repository

  #endif

  // **  localRemote

  let localRemote (project: IrisProject) =
    project.Config
    |> Config.getActiveMember
    |> Option.map (Uri.gitUri project.Name)

  // ** currentBranch

  #if !FABLE_COMPILER && !IRIS_NODES

  let currentBranch (project: IrisProject) =
    either {
      let! repo = repository project
      return Git.Branch.current repo
    }

  #endif

  // ** checkoutBranch

  #if !FABLE_COMPILER && !IRIS_NODES

  let checkoutBranch (name: string) (project: IrisProject) =
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

  #if !FABLE_COMPILER && !IRIS_NODES

  let checkPath (machine: IrisMachine) (projectName: Name) =
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

  let filePath (project: IrisProject) : FilePath =
    unwrap project.Path <.> PROJECT_FILENAME + ASSET_EXTENSION

  // ** userDir

  let userDir (project: IrisProject) : FilePath =
    unwrap project.Path <.> USER_DIR

  // ** cueDir

  let cueDir (project: IrisProject) : FilePath =
    unwrap project.Path <.> CUE_DIR

  // ** cuelistDir

  let cuelistDir (project: IrisProject) : FilePath =
    unwrap project.Path <.> CUELIST_DIR

  // ** writeDaemonExportFile (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  let private writeDaemonExportFile (repo: Repository) =
    either {
      let path = repo.Info.Path <.> "git-daemon-export-ok"
      let! _ = IrisData.write path (Payload "")
      return ()
    }

  #endif

  // ** writeGitIgnoreFile (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  let private writeGitIgnoreFile (repo: Repository) =
    either {
      let parent = Git.Repo.parentPath repo
      let path = parent </> filepath ".gitignore"
      let! _ = IrisData.write path (Payload GITIGNORE)
      do! Git.Repo.stage repo path
    }

  #endif

  // ** createAssetDir (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  let private createAssetDir (repo: Repository) (dir: FilePath) =
    either {
      let parent = Git.Repo.parentPath repo
      let target = parent </> dir
      do! FileSystem.mkDir target
      let gitkeep = target </> filepath ".gitkeep"
      let! _ = IrisData.write gitkeep (Payload "")
      do! Git.Repo.stage repo gitkeep
    }

  #endif

  // ** commitPath (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## commitPath
  ///
  /// commit a file at given path to git
  ///
  /// ### Signature:
  /// - committer : Signature of committer
  /// - msg       : commit msg
  /// - filepath  : path to file being committed
  /// - project   : IrisProject
  ///
  /// Returns: (Commit * IrisProject) option
  let private commitPath (filepath: FilePath)
                         (committer: Signature)
                         (msg : string)
                         (project: IrisProject) :
                         Either<IrisError,(Commit * IrisProject)> =
    either {
      let! repo = repository project
      let abspath =
        if Path.isPathRooted filepath then
          filepath
        else
          toFilePath project.Path </> filepath
      do! Git.Repo.stage repo abspath
      let! commit = Git.Repo.commit repo msg committer
      return commit, project
    }

  #endif

  // ** saveFile

  #if !FABLE_COMPILER && !IRIS_NODES

  let saveFile (path: FilePath)
               (contents: string)
               (committer: Signature)
               (msg : string)
               (project: IrisProject) :
               Either<IrisError,(Commit * IrisProject)> =

    either {
      let info = File.info path
      do! info.Directory.FullName |> filepath |> FileSystem.mkDir
      let! _ = IrisData.write path (Payload contents)
      return! commitPath path committer msg project
    }

  #endif

  // ** deleteFile

  #if !FABLE_COMPILER && !IRIS_NODES

  let deleteFile (path: FilePath)
                 (committer: Signature)
                 (msg : string)
                 (project: IrisProject) :
                 Either<IrisError,(Commit * IrisProject)> =
    either {
      let! _ = IrisData.remove path
      return! commitPath path committer msg project
    }

  #endif

  // ** saveAsset

  #if !FABLE_COMPILER && !IRIS_NODES

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
  /// Returns: Either<IrisError,Commit * Project>
  let inline saveAsset (thing: ^t) (committer: User) (project: IrisProject) =
    let payload = thing |> Yaml.encode
    let filepath = project.Path </> Asset.path thing
    let signature = committer.Signature
    let msg = String.Format("{0} saved {1}", committer.UserName, Path.getFileName filepath)
    saveFile filepath payload signature msg project

  #endif

  // ** deleteAsset

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## deleteAsset
  ///
  /// Delete a file path from disk and commit the change to git.
  ///
  /// ### Signature:
  /// - thing: ^t thing to delete
  /// - committer: User committing the change
  /// - msg: User committing the change
  /// - project: IrisProject to work on
  ///
  /// Returns: Either<IrisError, FileInfo * Commit * Project>
  let inline deleteAsset (thing: ^t) (committer: User) (project: IrisProject) =
    let filepath = project.Path </> Asset.path thing
    let signature = committer.Signature
    let msg = String.Format("{0} deleted {1}", committer.UserName, filepath)
    deleteFile filepath signature msg project

  let private needsInit (project: IrisProject) =
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

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ### Initialize the project git repository
  ///
  /// Given a project value, attempt to work out whether a git repository at that location already
  /// exists, otherwise creating it.
  ///
  /// # Returns: Repository
  let private initRepo (project: IrisProject) : Either<IrisError,unit> =
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
        |> IrisData.write absPath
      do! Git.Repo.add repo relPath
      do! Git.Repo.stage repo absPath
    }

  #endif

  // ** create

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ### Create a new project with the given name
  ///
  /// Create a new project with the given name. The default configuration will apply.
  ///
  /// # Returns: IrisProject
  let create (path: FilePath) (projectName: string) (machine: IrisMachine) =
    either {
      let project =
        { Id        = Id.Create()
          Name      = name projectName
          Path      = path
          CreatedOn = Time.createTimestamp()
          LastSaved = Some (Time.createTimestamp ())
          Copyright = None
          Author    = None
          Config    = Config.create machine  }

      do! initRepo project
      let! _ = IrisData.saveWithCommit (toFilePath path) User.Admin.Signature project
      return project
    }

  #endif

  // ** config

  let config (project: IrisProject) : IrisConfig = project.Config

  // ** updatePath

  let updatePath (path: FilePath) (project: IrisProject) : IrisProject =
    { project with Path = path }

  // ** updateConfig

  let updateConfig (config: IrisConfig) (project: IrisProject) : IrisProject =
    { project with Config = config }

  // ** updateDataDir

  let updateDataDir (raftDir: FilePath) (project: IrisProject) : IrisProject =
    { project.Config.Raft with DataDir = raftDir }
    |> flip Config.updateEngine project.Config
    |> flip updateConfig project

  // ** addMember

  let addMember (mem: RaftMember) (project: IrisProject) : IrisProject =
    project.Config
    |> Config.addMember mem
    |> flip updateConfig project

  // ** updateMember

  let updateMember (mem: RaftMember) (project: IrisProject) : IrisProject =
    addMember mem project

  // ** removeMember

  let removeMember (mem: MemberId) (project: IrisProject) : IrisProject =
    project.Config
    |> Config.removeMember mem
    |> flip updateConfig project

  // ** findMember

  let findMember (mem: MemberId) (project: IrisProject) =
    Config.findMember project.Config mem

  // ** selfMember

  let selfMember (project: IrisProject) =
    Config.findMember project.Config project.Config.Machine.MachineId

  // ** addMembers

  let addMembers (mems: RaftMember list) (project: IrisProject) : IrisProject =
    List.fold
      (fun config (mem: RaftMember) ->
        Config.addMember mem config)
      project.Config
      mems
    |> flip updateConfig project

  // ** updateMachine

  let updateMachine (machine: IrisMachine) (project: IrisProject) : IrisProject =
    { project with Config = Config.updateMachine machine project.Config }

  // ** updateRemotes

  #if !FABLE_COMPILER && !IRIS_NODES

  let updateRemotes (project: IrisProject) = either {
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
                    let url = Uri.gitUri project.Name peer
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
