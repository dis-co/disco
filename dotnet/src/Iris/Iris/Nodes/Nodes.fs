namespace VVVV.Nodes

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Raft
open Iris.Core
open Iris.Nodes


[<AutoOpen>]
module MockData =
  let rand = new Random()

  let rndstr() =
    Id.Create()
    |> string

  let mkTags () =
    [| for n in 0 .. rand.Next(1,20) do
        let guid = Guid.NewGuid()
        yield guid.ToString() |]

  let mk() = Id.Create()

  let mkPin() =
    Pin.Toggle(mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = true }|])

  let mkPins () =
    let props = [| { Key = "one"; Value = "two" }; { Key = "three"; Value = "four"} |]
    let selected = props.[0]
    let rgba = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy }
    let hsla = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy }
    [| Pin.Bang      (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = true    }|])
    ; Pin.Toggle    (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = true    }|])
    ; Pin.String    (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = rndstr()   }|])
    ; Pin.MultiLine (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = rndstr()   }|])
    ; Pin.FileName  (mk(), rndstr(), mk(), mkTags(), rndstr(), [|{ Index = 0u; Value = rndstr() }|])
    ; Pin.Directory (mk(), rndstr(), mk(), mkTags(), rndstr(), [|{ Index = 0u; Value = rndstr() }|])
    ; Pin.Url       (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = rndstr()  }|])
    ; Pin.IP        (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = rndstr()  }|])
    ; Pin.Float     (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = 3.0    }|])
    ; Pin.Double    (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = double 3.0 }|])
    ; Pin.Bytes     (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = [| 2uy; 9uy |] }|])
    ; Pin.Color     (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = rgba }|])
    ; Pin.Color     (mk(), rndstr(), mk(), mkTags(), [|{ Index = 0u; Value = hsla }|])
    ; Pin.Enum      (mk(), rndstr(), mk(), mkTags(), props, [|{ Index = 0u; Value = selected }|])
    |]

  let mkUser () =
    { Id = Id.Create()
      UserName = rndstr()
      FirstName = rndstr()
      LastName = rndstr()
      Email =  rndstr()
      Password = rndstr()
      Salt = rndstr()
      Joined = System.DateTime.Now
      Created = System.DateTime.Now }

  let mkUsers () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkUser() |]

  let mkCue () : Cue =
    { Id = Id.Create(); Name = rndstr(); Pins = mkPins() }

  let mkCues () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkCue() |]

  let mkPatch () : Iris.Core.Patch =
    let pins =
      mkPins ()
      |> Array.map toPair
      |> Map.ofArray

    { Id = Id.Create()
      Name = rndstr()
      Pins = pins }

  let mkPatches () : Iris.Core.Patch array =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkPatch() |]

  let mkCueList () : CueList =
    { Id = Id.Create(); Name = "Patch 3"; Cues = mkCues() }

  let mkCueLists () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkCueList() |]

  let mkMember () = Id.Create() |> Member.create

  let mkMembers () =
    [| for _ in 0 .. rand.Next(1, 6) do
        yield mkMember () |]

  let mkSession () =
    { Id = Id.Create()
      IpAddress = IPv4Address "127.0.0.1"
      UserAgent = "Oh my goodness" }

  let mkSessions () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkSession() |]

  let mkProject () =
    let machine =
      { MachineId = Id.Create()
        HostName  = Network.getHostName()
        WorkSpace = "C:/dev/null"
        WebIP     = "127.0.0.1"
        WebPort   = 8080us
        Version   = System.Version(1,0,0) }
    { Id        = Id.Create()
    ; Name      = rndstr()
    ; Path      = "C:/dev/null"
    ; CreatedOn = Time.createTimestamp()
    ; LastSaved = Some (Time.createTimestamp ())
    ; Copyright = None
    ; Author    = None
    ; Config    = Config.create (rndstr()) machine  }

  let mkClient () : IrisClient =
    { Id = Id.Create ()
      Name = "Nice client"
      Role = Role.Renderer
      Status = ServiceStatus.Running
      IpAddress = IPv4Address "127.0.0.1"
      Port = 8921us }

  let mkClients () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkClient() |]

  let inline asMap arr =
    arr
    |> Array.map toPair
    |> Map.ofArray

  let mkState path : State =
    { Project  = mkProject ()
      Patches  = mkPatches () |> asMap
      Cues     = mkCues    () |> asMap
      CueLists = mkCueLists() |> asMap
      Sessions = mkSessions() |> asMap
      Users    = mkUsers   () |> asMap
      Clients  = mkClients () |> asMap }

//  ___      _
// |_ _|_ __(_)___
//  | || '__| / __|
//  | || |  | \__ \
// |___|_|  |_|___/

[<PluginInfo(Name="Iris", Category="Iris", AutoEvaluate=true)>]
type Iris() =

  [<Import();DefaultValue>]
  val mutable V1Host: IPluginHost

  [<Import();DefaultValue>]
  val mutable V2Host: IHDEHost

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Server", IsSingle = true)>]
  val mutable InServer: IDiffSpread<string>

  [<DefaultValue>]
  [<Input("Port", IsSingle = true)>]
  val mutable InPort: IDiffSpread<uint16>

  [<DefaultValue>]
  [<Input("Debug", IsSingle = true, DefaultValue = 0.0)>]
  val mutable InDebug: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("State", IsSingle = true)>]
  val mutable OutState: ISpread<Iris.Core.State>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Connected", IsSingle = true, DefaultValue = 0.0)>]
  val mutable OutConnected: ISpread<bool>

  [<DefaultValue>]
  [<Output("Status", IsSingle = true)>]
  val mutable OutStatus: ISpread<string>

  let mutable initialized = false
  let mutable banged = false
  let mutable state = Unchecked.defaultof<GraphApi.PluginState>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if banged then
        banged <- false
        self.OutUpdate.[0] <- false

      if not initialized then
        let state' =
          { GraphApi.PluginState.Create() with
              V1Host = self.V1Host
              V2Host = self.V2Host
              Logger = self.Logger
              InServer = self.InServer
              InPort = self.InPort
              InDebug = self.InDebug
              OutState = self.OutState
              OutConnected = self.OutConnected
              OutStatus = self.OutStatus }
        state <- state'
        initialized <- true

        self.OutState.[0] <- mkState()
        self.OutUpdate.[0] <- true
        banged <- true

      state <- GraphApi.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state

//  ____  _        _
// / ___|| |_ __ _| |_ ___
// \___ \| __/ _` | __/ _ \
//  ___) | || (_| | ||  __/
// |____/ \__\__,_|\__\___|

[<PluginInfo(Name="State", Category="Iris", AutoEvaluate=true)>]
type State() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("State", IsSingle = true)>]
  val mutable InState: ISpread<Iris.Core.State>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Project", IsSingle = true)>]
  val mutable OutProject: ISpread<IrisProject>

  [<DefaultValue>]
  [<Output("Cues")>]
  val mutable OutCues: ISpread<Cue>

  [<DefaultValue>]
  [<Output("CueLists")>]
  val mutable OutCueLists: ISpread<CueList>

  [<DefaultValue>]
  [<Output("Sessions")>]
  val mutable OutSessions: ISpread<Session>

  [<DefaultValue>]
  [<Output("Users")>]
  val mutable OutUsers: ISpread<User>

  [<DefaultValue>]
  [<Output("Clients")>]
  val mutable OutClients: ISpread<IrisClient>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InState.[0]) then
        let state = self.InState.[0]
        self.OutProject.[0] <- state.Project

        let cues =
          state.Cues
          |> Map.toArray
          |> Array.map snd

        let cuelists =
          state.CueLists
          |> Map.toArray
          |> Array.map snd

        let sessions =
          state.Sessions
          |> Map.toArray
          |> Array.map snd

        let users =
          state.Users
          |> Map.toArray
          |> Array.map snd

        let clients =
          state.Clients
          |> Map.toArray
          |> Array.map snd

        self.OutCues.AssignFrom cues
        self.OutCueLists.AssignFrom cuelists
        self.OutSessions.AssignFrom sessions
        self.OutUsers.AssignFrom users
        self.OutClients.AssignFrom clients

//  ____            _           _
// |  _ \ _ __ ___ (_) ___  ___| |_
// | |_) | '__/ _ \| |/ _ \/ __| __|
// |  __/| | | (_) | |  __/ (__| |_
// |_|   |_|  \___// |\___|\___|\__|
//               |__/

[<PluginInfo(Name="Project", Category="Iris", AutoEvaluate=true)>]
type Project() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Project", IsSingle = true)>]
  val mutable InProject: ISpread<IrisProject>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Id", IsSingle = true)>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Name", IsSingle = true)>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Path", IsSingle = true)>]
  val mutable OutPath: ISpread<string>

  [<DefaultValue>]
  [<Output("CreatedOn", IsSingle = true)>]
  val mutable OutCreatedOn: ISpread<string>

  [<DefaultValue>]
  [<Output("LastSaved", IsSingle = true)>]
  val mutable OutLastSaved: ISpread<string>

  [<DefaultValue>]
  [<Output("Config", IsSingle = true)>]
  val mutable OutConfig: ISpread<IrisConfig>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InProject.[0]) then
        let project = self.InProject.[0]
        let lastSaved =
          match project.LastSaved with
          | Some str -> str
          | None -> ""

        self.OutId.[0] <- string project.Id
        self.OutName.[0] <- project.Name
        self.OutPath.[0] <- project.Path
        self.OutCreatedOn.[0] <- sprintf "%A" project.CreatedOn
        self.OutLastSaved.[0] <- lastSaved
        self.OutConfig.[0] <- project.Config

//   ____             __ _
//  / ___|___  _ __  / _(_) __ _
// | |   / _ \| '_ \| |_| |/ _` |
// | |__| (_) | | | |  _| | (_| |
//  \____\___/|_| |_|_| |_|\__, |
//                         |___/

[<PluginInfo(Name="Config", Category="Iris", AutoEvaluate=true)>]
type Config() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Config", IsSingle = true)>]
  val mutable InConfig: ISpread<IrisConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("MachineId", IsSingle = true)>]
  val mutable OutMachineId: ISpread<string>

  [<DefaultValue>]
  [<Output("Audio", IsSingle = true)>]
  val mutable OutAudio: ISpread<AudioConfig>

  [<DefaultValue>]
  [<Output("Vvvv", IsSingle = true)>]
  val mutable OutVvvv: ISpread<VvvvConfig>

  [<DefaultValue>]
  [<Output("Raft", IsSingle = true)>]
  val mutable OutRaft: ISpread<RaftConfig>

  [<DefaultValue>]
  [<Output("Timing", IsSingle = true)>]
  val mutable OutTiming: ISpread<TimingConfig>

  [<DefaultValue>]
  [<Output("Cluster", IsSingle = true)>]
  val mutable OutCluster: ISpread<ClusterConfig>

  [<DefaultValue>]
  [<Output("Viewports")>]
  val mutable OutViewports: ISpread<ViewPort>

  [<DefaultValue>]
  [<Output("Displays")>]
  val mutable OutDisplays: ISpread<Display>

  [<DefaultValue>]
  [<Output("Tasks")>]
  val mutable OutTasks: ISpread<Task>

  [<DefaultValue>]
  [<Output("Version", IsSingle = true)>]
  val mutable OutVersion: ISpread<string>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InConfig.[0]) then
        let config = self.InConfig.[0]

        self.OutMachineId.[0] <- string config.MachineId
        self.OutAudio.[0] <- config.Audio
        self.OutVvvv.[0] <- config.Vvvv
        self.OutRaft.[0] <- config.Raft
        self.OutTiming.[0] <- config.Timing
        self.OutCluster.[0] <- config.Cluster
        self.OutViewports.AssignFrom config.ViewPorts
        self.OutDisplays.AssignFrom config.Displays
        self.OutTasks.AssignFrom config.Tasks
        self.OutVersion.[0] <- string config.Version

//     _             _ _
//    / \  _   _  __| (_) ___
//   / _ \| | | |/ _` | |/ _ \
//  / ___ \ |_| | (_| | | (_) |
// /_/   \_\__,_|\__,_|_|\___/


[<PluginInfo(Name="AudioConfig", Category="Iris", AutoEvaluate=true)>]
type Audio() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Audio", IsSingle = true)>]
  val mutable InAudio: ISpread<AudioConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("SampleRate", IsSingle = true)>]
  val mutable OutSampleRate: ISpread<int>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InAudio.[0]) then
        let config = self.InAudio.[0]
        self.OutSampleRate.[0] <- int config.SampleRate

// __     __
// \ \   / /_   ____   ____   __
//  \ \ / /\ \ / /\ \ / /\ \ / /
//   \ V /  \ V /  \ V /  \ V /
//    \_/    \_/    \_/    \_/

[<PluginInfo(Name="VvvvConfig", Category="Iris", AutoEvaluate=true)>]
type Vvvv() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Vvvv", IsSingle = true)>]
  val mutable InVvvv: ISpread<VvvvConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Executables")>]
  val mutable OutExecutables: ISpread<VvvvExe>

  [<DefaultValue>]
  [<Output("Plugins")>]
  val mutable OutPlugins: ISpread<VvvvPlugin>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InVvvv.[0]) then
        let config = self.InVvvv.[0]
        self.OutExecutables.AssignFrom config.Executables
        self.OutExecutables.AssignFrom config.Executables

//  _____                     _        _     _
// | ____|_  _____  ___ _   _| |_ __ _| |__ | | ___
// |  _| \ \/ / _ \/ __| | | | __/ _` | '_ \| |/ _ \
// | |___ >  <  __/ (__| |_| | || (_| | |_) | |  __/
// |_____/_/\_\___|\___|\__,_|\__\__,_|_.__/|_|\___|

[<PluginInfo(Name="VvvvExecutable", Category="Iris", AutoEvaluate=true)>]
type VvvvExecutable() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("VvvvExe")>]
  val mutable InExe: ISpread<VvvvExe>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Executable")>]
  val mutable OutExecutable: ISpread<string>

  [<DefaultValue>]
  [<Output("Version")>]
  val mutable OutVersion: ISpread<string>

  [<DefaultValue>]
  [<Output("Required")>]
  val mutable OutRequired: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InExe.[0]) then
            let config = self.InExe.[n]
            self.OutExecutable.[n] <- config.Executable
            self.OutVersion.[n] <- config.Version
            self.OutRequired.[n] <- config.Required

//  ____  _             _
// |  _ \| |_   _  __ _(_)_ __
// | |_) | | | | |/ _` | | '_ \
// |  __/| | |_| | (_| | | | | |
// |_|   |_|\__,_|\__, |_|_| |_|
//                |___/

[<PluginInfo(Name="VvvvPlugin", Category="Iris", AutoEvaluate=true)>]
type VvvvPlug() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("VvvvPlugin")>]
  val mutable InPlugin: ISpread<VvvvPlugin>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Path")>]
  val mutable OutPath: ISpread<string>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InPlugin.[0])  then
            let config = self.InPlugin.[n]
            self.OutName.[n] <- config.Name
            self.OutPath.[n] <- config.Path

//  ____  _                   _
// / ___|(_) __ _ _ __   __ _| |
// \___ \| |/ _` | '_ \ / _` | |
//  ___) | | (_| | | | | (_| | |
// |____/|_|\__, |_| |_|\__,_|_|
//          |___/

[<PluginInfo(Name="Signal", Category="Iris", AutoEvaluate=true)>]
type SignalNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Signal")>]
  val mutable InSignal: ISpread<Signal>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Size")>]
  val mutable OutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Coordinate")>]
  val mutable OutCoordinate: ISpread<ISpread<int>>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InSignal.[0]) then
            let config = self.InSignal.[n]
            self.OutSize.[n].[0] <- config.Size.X
            self.OutSize.[n].[1] <- config.Size.Y
            self.OutCoordinate.[n].[0] <- config.Position.X
            self.OutCoordinate.[n].[1] <- config.Position.Y

//  ____            _
// |  _ \ ___  __ _(_) ___  _ __
// | |_) / _ \/ _` | |/ _ \| '_ \
// |  _ <  __/ (_| | | (_) | | | |
// |_| \_\___|\__, |_|\___/|_| |_|
//            |___/

[<PluginInfo(Name="Region", Category="Iris", AutoEvaluate=true)>]
type RegionNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Region")>]
  val mutable InRegion: ISpread<Region>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Source Position")>]
  val mutable OutSrcPos: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Source Size")>]
  val mutable OutSrcSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Output Size")>]
  val mutable OutOutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Output Position")>]
  val mutable OutOutPos: ISpread<ISpread<int>>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InRegion.[n]) then
            let config = self.InRegion.[n]
            self.OutId.[n] <- string config.Id
            self.OutName.[n] <- config.Name
            self.OutSrcSize.[n].[0] <- config.SrcSize.X
            self.OutSrcSize.[n].[1] <- config.SrcSize.Y
            self.OutSrcPos.[n].[0] <- config.SrcPosition.X
            self.OutSrcPos.[n].[1] <- config.SrcPosition.Y
            self.OutOutSize.[n].[0] <- config.OutputSize.X
            self.OutOutSize.[n].[1] <- config.OutputSize.Y
            self.OutOutPos.[n].[0] <- config.OutputPosition.X
            self.OutOutPos.[n].[1] <- config.OutputPosition.Y

//  ____            _             __  __
// |  _ \ ___  __ _(_) ___  _ __ |  \/  | __ _ _ __
// | |_) / _ \/ _` | |/ _ \| '_ \| |\/| |/ _` | '_ \
// |  _ <  __/ (_| | | (_) | | | | |  | | (_| | |_) |
// |_| \_\___|\__, |_|\___/|_| |_|_|  |_|\__,_| .__/
//            |___/                           |_|

[<PluginInfo(Name="RegionMap", Category="Iris", AutoEvaluate=true)>]
type RegionMapNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Region")>]
  val mutable InRegionMap: ISpread<RegionMap>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Source Viewport Id")>]
  val mutable OutSrcId: ISpread<string>

  [<DefaultValue>]
  [<Output("Regions")>]
  val mutable OutRegions: ISpread<ISpread<Region>>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InRegionMap.[n]) then
            let config = self.InRegionMap.[n]
            self.OutSrcId.[n] <- string config.SrcViewportId
            self.OutRegions.[n].AssignFrom config.Regions

//  ____  _           _
// |  _ \(_)___ _ __ | | __ _ _   _
// | | | | / __| '_ \| |/ _` | | | |
// | |_| | \__ \ |_) | | (_| | |_| |
// |____/|_|___/ .__/|_|\__,_|\__, |
//             |_|            |___/

[<PluginInfo(Name="Display", Category="Iris", AutoEvaluate=true)>]
type DisplayNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Display")>]
  val mutable InDisplay: ISpread<Display>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Size")>]
  val mutable OutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Signals")>]
  val mutable OutSignals: ISpread<ISpread<Signal>>

  [<DefaultValue>]
  [<Output("RegionMap")>]
  val mutable OutRegionMap: ISpread<RegionMap>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InDisplay.[n]) then
            let config = self.InDisplay.[n]
            self.OutId.[n] <- string config.Id
            self.OutName.[n] <- config.Name
            self.OutSize.[n].[0] <- config.Size.X
            self.OutSize.[n].[1] <- config.Size.Y
            self.OutSignals.[n].AssignFrom config.Signals
            self.OutRegionMap.[n] <- config.RegionMap

// __     ___               ____            _
// \ \   / (_) _____      _|  _ \ ___  _ __| |_
//  \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
//   \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
//    \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|

[<PluginInfo(Name="ViewPort", Category="Iris", AutoEvaluate=true)>]
type ViewPortNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("ViewPort")>]
  val mutable InViewPort: ISpread<ViewPort>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Position")>]
  val mutable OutPosition: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Size")>]
  val mutable OutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Output Position")>]
  val mutable OutOutPosition: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Output Size")>]
  val mutable OutOutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Overlap")>]
  val mutable OutOverlap: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Description")>]
  val mutable OutDescription: ISpread<string>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InViewPort.[n]) then
            let config = self.InViewPort.[n]
            self.OutId.[n] <- string config.Id
            self.OutName.[n] <- config.Name
            self.OutPosition.[n].[0] <- config.Position.X
            self.OutPosition.[n].[1] <- config.Position.Y
            self.OutSize.[n].[0] <- config.Size.X
            self.OutSize.[n].[1] <- config.Size.Y
            self.OutOutPosition.[n].[0] <- config.OutputPosition.X
            self.OutOutPosition.[n].[1] <- config.OutputPosition.Y
            self.OutOutSize.[n].[0] <- config.OutputSize.X
            self.OutOutSize.[n].[1] <- config.OutputSize.Y
            self.OutOverlap.[n].[0] <- config.Overlap.X
            self.OutOverlap.[n].[1] <- config.Overlap.Y
            self.OutDescription.[n] <- config.Description

//  _____         _
// |_   _|_ _ ___| | __
//   | |/ _` / __| |/ /
//   | | (_| \__ \   <
//   |_|\__,_|___/_|\_\

[<PluginInfo(Name="Task", Category="Iris", AutoEvaluate=true)>]
type TaskNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Task")>]
  val mutable InTask: ISpread<Task>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Description")>]
  val mutable OutDescription: ISpread<string>

  [<DefaultValue>]
  [<Output("DisplayId")>]
  val mutable OutDisplayId: ISpread<string>

  [<DefaultValue>]
  [<Output("AudioStream")>]
  val mutable OutAudioStream: ISpread<string>

  [<DefaultValue>]
  [<Output("Arguments")>]
  val mutable OutArguments: ISpread<ISpread<string>>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InTask.[n]) then
            let config = self.InTask.[n]
            let keys = Array.map fst config.Arguments
            let vals = Array.map snd config.Arguments
            self.OutId.[n] <- string config.Id
            self.OutDisplayId.[n] <- string config.DisplayId
            self.OutDescription.[n] <- config.Description
            self.OutAudioStream.[n] <- config.AudioStream
            self.OutArguments.[n].AssignFrom keys

//  _   _           _    ____
// | | | | ___  ___| |_ / ___|_ __ ___  _   _ _ __
// | |_| |/ _ \/ __| __| |  _| '__/ _ \| | | | '_ \
// |  _  | (_) \__ \ |_| |_| | | | (_) | |_| | |_) |
// |_| |_|\___/|___/\__|\____|_|  \___/ \__,_| .__/
//                                           |_|

[<PluginInfo(Name="HostGroup", Category="Iris", AutoEvaluate=true)>]
type HostGroupNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("HostGroup")>]
  val mutable InHostGroup: ISpread<HostGroup>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Members")>]
  val mutable OutMembers: ISpread<ISpread<string>>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InHostGroup.[n]) then
            let config = self.InHostGroup.[n]
            self.OutName.[n] <- config.Name
            self.OutMembers.[n].AssignFrom (Array.map string config.Members)

//  _____ _           _
// |_   _(_)_ __ ___ (_)_ __   __ _
//   | | | | '_ ` _ \| | '_ \ / _` |
//   | | | | | | | | | | | | | (_| |
//   |_| |_|_| |_| |_|_|_| |_|\__, |
//                            |___/

[<PluginInfo(Name="TimingConfig", Category="Iris", AutoEvaluate=true)>]
type TimingConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Timing")>]
  val mutable InTiming: ISpread<TimingConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Framebase")>]
  val mutable OutFramebase: ISpread<int>

  [<DefaultValue>]
  [<Output("Input")>]
  val mutable OutInput: ISpread<string>

  [<DefaultValue>]
  [<Output("Servers")>]
  val mutable OutServers: ISpread<ISpread<string>>

  [<DefaultValue>]
  [<Output("UDPPort")>]
  val mutable OutUDPPort: ISpread<int>

  [<DefaultValue>]
  [<Output("TCPPort")>]
  val mutable OutTCPPort: ISpread<int>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InTiming.[n]) then
            let config = self.InTiming.[n]
            self.OutFramebase.[n] <- int config.Framebase
            self.OutInput.[n] <- config.Input
            self.OutServers.[n].AssignFrom (Array.map string config.Servers)
            self.OutUDPPort.[n] <- int config.UDPPort
            self.OutTCPPort.[n] <- int config.TCPPort
