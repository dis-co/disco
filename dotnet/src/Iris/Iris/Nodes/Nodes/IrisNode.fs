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
type IrisNode() =

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
