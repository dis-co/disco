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

  let mkMachine () =
    { MachineId = Id.Create()
      HostName  = Network.getHostName()
      WorkSpace = "C:/dev/null"
      WebIP     = "127.0.0.1"
      WebPort   = 8080us
      Version   = System.Version(1,0,0) }

  let mkVvvv () =
    let exes =
      [| for n in 0 .. (rand.Next(2,8)) do
          yield { Executable = rndstr()
                  Version = rndstr()
                  Required = (n % 2) = 0 } |]

    let plugins : VvvvPlugin array =
      [| for n in 0 .. (rand.Next(2,8)) do
          yield { Name = rndstr()
                  Path = rndstr() } |]

    { Executables = exes
      Plugins = plugins }

  let mkHostGroup () : HostGroup =
    let members =
      [| for n in 0 .. (rand.Next(2,8)) do
          yield Id.Create() |]
    { Name = rndstr()
      Members = members }

  let mkCluster() =
    let members =
      mkMembers ()
      |> Array.map (fun mem -> mem.Id,mem)
      |> Map.ofArray

    let groups =
      [| for n in 0 .. (rand.Next(2,8)) do
          yield mkHostGroup () |]

    { Name = rndstr()
      Members = members
      Groups = groups }

  let mkViewPort () =
    { Id = Id.Create()
      Name = rndstr()
      Position = Coordinate(rand.Next(0,1280),rand.Next(0,1920))
      Size = Rect(rand.Next(0,1280),rand.Next(0,1920))
      OutputPosition = Coordinate(rand.Next(0,1280),rand.Next(0,1920))
      OutputSize = Rect(rand.Next(0,1280),rand.Next(0,1920))
      Overlap = Rect(rand.Next(0,1280),rand.Next(0,1920))
      Description = rndstr() }

  let mkViewPorts () =
    [| for n in 0 .. (rand.Next(1,8)) do
        yield mkViewPort() |]

  let mkSignal () : Signal =
    { Position = Coordinate(rand.Next(0,1280),rand.Next(0,1920))
      Size = Rect(rand.Next(0,1280),rand.Next(0,1920))}

  let mkSignals () =
    [| for n in 0 .. (rand.Next(2,8)) do
        yield mkSignal() |]

  let mkRegion () =
    { Id = Id.Create()
      Name = rndstr()
      SrcPosition = Coordinate(rand.Next(0,1280),rand.Next(0,1920))
      SrcSize = Rect(rand.Next(0,1280),rand.Next(0,1920))
      OutputPosition = Coordinate(rand.Next(0,1280),rand.Next(0,1920))
      OutputSize = Rect(rand.Next(0,1280),rand.Next(0,1920))
      }

  let mkRegions () =
    [| for n in 0 .. (rand.Next(2,8)) do
        yield mkRegion() |]

  let mkRegionMap () =
    { SrcViewportId = Id.Create()
      Regions = mkRegions() }

  let mkDisplay () =
    { Id = Id.Create()
      Name = rndstr()
      Size = Rect(rand.Next(0,1280),rand.Next(0,1920))
      Signals = mkSignals()
      RegionMap = mkRegionMap() }

  let mkDisplays () =
    [| for n in 0 .. (rand.Next(2,9)) do
        yield mkDisplay() |]

  let mkArguments () =
    [| for n in 0 .. (rand.Next(2,8)) do
        yield (rndstr(), rndstr()) |]

  let mkTask () =
    { Id = Id.Create()
      Description = rndstr()
      DisplayId = Id.Create()
      AudioStream = rndstr()
      Arguments = mkArguments() }

  let mkTasks () =
    [| for n in 0 .. (rand.Next(2,8)) do
        yield mkTask() |]

  let mkConfig () =
    { Config.create (rndstr()) (mkMachine()) with
        Vvvv = mkVvvv ()
        Cluster = mkCluster()
        Tasks = mkTasks()
        ViewPorts = mkViewPorts()
        Displays = mkDisplays () }

  let mkProject () =
    { Id        = Id.Create()
    ; Name      = rndstr()
    ; Path      = "C:/dev/null"
    ; CreatedOn = Time.createTimestamp()
    ; LastSaved = Some (Time.createTimestamp ())
    ; Copyright = None
    ; Author    = None
    ; Config    = mkConfig ()  }

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


//  ____  _        _
// / ___|| |_ __ _| |_ ___
// \___ \| __/ _` | __/ _ \
//  ___) | || (_| | ||  __/
// |____/ \__\__,_|\__\___|

[<PluginInfo(Name="State", Category="Iris", AutoEvaluate=true)>]
type StateNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("State", IsSingle = true)>]
  val mutable InState: ISpread<Iris.Core.State>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Project", IsSingle = true)>]
  val mutable OutProject: ISpread<IrisProject>

  [<DefaultValue>]
  [<Output("Patches")>]
  val mutable OutPatches: ISpread<Patch>

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

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  let mutable init = false
  let mutable state = Unchecked.defaultof<State>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if not init then
        state <- mkState()
        init <- true

      if self.InUpdate.[0] (* && not (Util.isNull self.InState.[0]) *) then
        // let state = self.InState.[0]
        state <- mkState()
        self.OutProject.[0] <- state.Project

        let patches =
          state.Patches
          |> Map.toArray
          |> Array.map snd

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

        self.OutPatches.SliceCount <- Array.length patches
        self.OutCues.SliceCount <- Array.length cues
        self.OutCueLists.SliceCount <- Array.length cuelists
        self.OutSessions.SliceCount <- Array.length sessions
        self.OutUsers.SliceCount <- Array.length users
        self.OutClients.SliceCount <- Array.length clients

        self.OutPatches.AssignFrom patches
        self.OutCues.AssignFrom cues
        self.OutCueLists.AssignFrom cuelists
        self.OutSessions.AssignFrom sessions
        self.OutUsers.AssignFrom users
        self.OutClients.AssignFrom clients

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
