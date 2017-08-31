namespace Test.Units

[<RequireQualifiedAccess>]
module SerializationTests =

  open System
  open Fable.Core
  open Fable.Import

  open Iris.Raft
  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests

  open Iris.Core.FlatBuffers
  open Iris.Web.Core.FlatBufferTypes

  let rand = new System.Random()

  let mk() = Id.Create()

  let rndstr() = mk() |> string
  let rndname() = mk() |> string |> name

  let rndport() = rand.Next(0,65535) |> uint16 |> port

  let rndint() = rand.Next()

  let mkBytes _ =
    let num = rand.Next(3, 10)
    let bytes = Array.zeroCreate<byte> num
    for i in 0 .. (num - 1) do
      bytes.[i] <- byte i
    bytes

  let mktags _ =
    [| for n in 0 .. rand.Next(2,8) do
        yield Id.Create() |> string |> astag |]

  let mkProp () =
    { Key = rndstr(); Value = rndstr() }

  let mkProps () =
    [| for n = 0 to rand.Next(2,8) do yield mkProp() |]

  let mkProject _ =
    IrisProject.Empty

  let pins _ =
    [| Pin.bang      (mk()) (name "Bang")      (mk()) (mktags()) [| true  |]
    ;  Pin.toggle    (mk()) (name "Toggle")    (mk()) (mktags()) [| true  |]
    ;  Pin.string    (mk()) (name "string")    (mk()) (mktags()) [| "one" |]
    ;  Pin.multiLine (mk()) (name "multiline") (mk()) (mktags()) [| "two" |]
    ;  Pin.fileName  (mk()) (name "filename")  (mk()) (mktags()) [| "three" |]
    ;  Pin.directory (mk()) (name "directory") (mk()) (mktags()) [| "four"  |]
    ;  Pin.url       (mk()) (name "url")       (mk()) (mktags()) [| "five" |]
    ;  Pin.ip        (mk()) (name "ip")        (mk()) (mktags()) [| "six"  |]
    ;  Pin.number    (mk()) (name "number")    (mk()) (mktags()) [| double 3.0 |]
    ;  Pin.bytes     (mk()) (name "bytes")     (mk()) (mktags()) [| mkBytes () |]
    ;  Pin.color     (mk()) (name "rgba")      (mk()) (mktags()) [| RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } |]
    ;  Pin.color     (mk()) (name "hsla")      (mk()) (mktags()) [| HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } |]
    ;  Pin.enum      (mk()) (name "enum")      (mk()) (mktags()) [| { Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|]  [| { Key = "one"; Value = "two" } |]
    |]

  let mkPin _ =
    Pin.string (Id.Create()) (name "url input") (Id.Create()) [| |] [| "hello" |]

  let mkSlices() =
    BoolSlices(Id.Create(), [| true; false; true; true; false |])

  let mkSlicesMap() =
    let slices = mkSlices ()
    [ (slices.Id, slices) ]
    |> Map.ofList
    |> SlicesMap

  let mkCue _ : Cue =
    { Id = Id.Create(); Name = name "Cue 1"; Slices = [| mkSlices() |] }

  let mkCueRef () : CueReference =
    { Id = Id.Create(); CueId = Id.Create(); AutoFollow = rndint(); Duration = rndint(); Prewait = rndint() }

  let mkCueRefs () : CueReference array =
    [| for n in 0 .. rand.Next(1,20) -> mkCueRef() |]

  let mkCueGroup () : CueGroup =
    { Id = Id.Create(); Name = rndname(); CueRefs = mkCueRefs() }

  let mkCueGroups () : CueGroup array =
    [| for n in 0 .. rand.Next(1,20) -> mkCueGroup() |]

  let mkPinGroup _ : PinGroup =
    let pins = pins () |> Array.map toPair |> Map.ofArray
    { Id = Id.Create()
      Name = name "PinGroup 3"
      Client = Id.Create()
      Path = None
      Pins = pins }

  let mkCueList _ : CueList =
    { Id = Id.Create(); Name = name "PinGroup 3"; Groups = mkCueGroups() }

  let mkUser _ =
    { Id = Id.Create()
    ; UserName = name "krgn"
    ; FirstName = name "Karsten"
    ; LastName = name "Gebbert"
    ; Email = email "k@ioctl.it"
    ; Password = checksum "1234"
    ; Salt = checksum "090asd902"
    ; Joined = DateTime.UtcNow
    ; Created = DateTime.UtcNow
    }

  let mkClient () : IrisClient =
    { Id = Id.Create ()
      Name = name "Nice client"
      Role = Role.Renderer
      Status = ServiceStatus.Running
      ServiceId = Id.Create()
      IpAddress = IPv4Address "127.0.0.1"
      Port = port 8921us }

  let mkDiscoveredService(): DiscoveredService =
    { Id = Id.Create ()
      Name = "Nice service"
      FullName = "Really nice service"
      HostName = "remotehost"
      HostTarget = "localhost"
      Status = Idle
      Aliases = [||]
      Protocol = IPProtocol.IPv4
      AddressList = [||]
      Services = [||]
      ExtraMetadata = [||] }

  let mkClients () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkClient() |]

  let mkMember _ = Id.Create() |> Member.create

  let mkSession _ =
    { Id = Id.Create()
    ; IpAddress = IPv4Address "127.0.0.1"
    ; UserAgent = "Oh my goodness" }

  let mkPinMapping _ =
    { Id = Id.Create()
      Source = Id.Create()
      Sinks = Set [| Id.Create(); Id.Create() |] }

  let mkCuePlayer() =
    let rndopt () =
      if rand.Next(0,2) > 0 then
        Some (rndstr() |> Id)
      else
        None

    { Id = Id.Create()
      Name = rndname ()
      Locked = false
      CueList = rndopt ()
      Selected = index (rand.Next(0,1000))
      Call = mkPin()
      Next = mkPin()
      Previous = mkPin()
      RemainingWait = rand.Next(0,1000)
      LastCaller = rndopt()
      LastCalled = rndopt() }

  let mkState _ =
    { Project    = mkProject ()
    ; PinGroups  = mkPinGroup () |> fun (group: PinGroup) -> Map.ofArray [| (group.Id, group) |]
    ; PinMappings = mkPinMapping () |> fun (map: PinMapping) -> Map.ofArray [| (map.Id, map) |]
    ; Cues       = mkCue () |> fun (cue: Cue) -> Map.ofArray [| (cue.Id, cue) |]
    ; CueLists   = mkCueList () |> fun (cuelist: CueList) -> Map.ofArray [| (cuelist.Id, cuelist) |]
    ; Sessions   = mkSession () |> fun (session: Session) -> Map.ofArray [| (session.Id, session) |]
    ; Users      = mkUser    () |> fun (user: User) -> Map.ofArray [| (user.Id, user) |]
    ; Clients    = mkClient  () |> fun (client: IrisClient) -> Map.ofArray [| (client.Id, client) |]
    ; CuePlayers = mkCuePlayer() |> fun (player: CuePlayer) -> Map.ofArray [| (player.Id, player) |]
    ; DiscoveredServices = let ser = mkDiscoveredService() in Map.ofArray [| (ser.Id, ser) |]
    }

  let inline check thing =
    let thong = thing |> Binary.encode |> Binary.decode |> Either.get
    equals thing thong

  let main () =
    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.SerializationTests"
    (* ------------------------------------------------------------------------ *)

    test "should serialize/deserialize StateMachineBatch correctly" <| fun finish ->
      let batch =
        StateMachineBatch
          [ AddCue                  <| mkCue ()
            UpdateCue               <| mkCue ()
            RemoveCue               <| mkCue ()
            AddCueList              <| mkCueList ()
            UpdateCueList           <| mkCueList ()
            RemoveCueList           <| mkCueList ()
            AddCuePlayer            <| mkCuePlayer ()
            UpdateCuePlayer         <| mkCuePlayer ()
            RemoveCuePlayer         <| mkCuePlayer ()
            AddSession              <| mkSession ()
            UpdateSession           <| mkSession ()
            RemoveSession           <| mkSession ()
            AddUser                 <| mkUser ()
            UpdateUser              <| mkUser ()
            RemoveUser              <| mkUser ()
            AddPinGroup             <| mkPinGroup ()
            UpdatePinGroup          <| mkPinGroup ()
            RemovePinGroup          <| mkPinGroup ()
            AddClient               <| mkClient ()
            UpdateSlices            <| mkSlicesMap ()
            UpdateClient            <| mkClient ()
            RemoveClient            <| mkClient ()
            AddPin                  <| mkPin ()
            UpdatePin               <| mkPin ()
            RemovePin               <| mkPin ()
            AddMember               <| Member.create (Id.Create())
            UpdateMember            <| Member.create (Id.Create())
            RemoveMember            <| Member.create (Id.Create())
            AddDiscoveredService    <| mkDiscoveredService ()
            UpdateDiscoveredService <| mkDiscoveredService ()
            RemoveDiscoveredService <| mkDiscoveredService ()
            DataSnapshot            <| mkState ()
            Command AppCommand.Undo
            LogMsg(Logger.create Debug "bla" "ohai")
            SetLogLevel Warn ]
      check batch
      finish()

    test "should serialize/deserialize cue correctly" <| fun finish ->
      [| for i in 0 .. 20 do
          yield  mkCue () |]
      |> Array.iter check
      finish()

    testSync "Validate PinMapping Serialization" <| fun () ->
      let mapping : PinMapping = mkPinMapping ()
      let remapping = mapping |> Binary.encode |> Binary.decode |> Either.get
      equals mapping remapping

    testSync "Validate Cue Serialization" <| fun () ->
      let cue : Cue = mkCue ()
      let recue = cue |> Binary.encode |> Binary.decode |> Either.get
      equals cue recue

    testSync "Validate CueReference Serialization" <| fun () ->
      let cueReference : CueReference = mkCueRef ()
      let recueReference = cueReference |> Binary.encode |> Binary.decode |> Either.get
      equals cueReference recueReference

    testSync "Validate CueGroup Serialization" <| fun () ->
      let cueGroup : CueGroup = mkCueGroup ()
      let recueGroup = cueGroup |> Binary.encode |> Binary.decode |> Either.get
      equals cueGroup recueGroup

    test "Validate CueList Serialization" <| fun finish ->
      let cuelist : CueList = mkCueList ()
      let recuelist = cuelist |> Binary.encode |> Binary.decode |> Either.get
      equals cuelist recuelist
      finish()

    test "Validate PinGroup Serialization" <| fun finish ->
      let group : PinGroup = mkPinGroup ()
      let regroup = group |> Binary.encode |> Binary.decode |> Either.get
      equals group regroup
      finish()

    test "Validate Session Serialization" <| fun finish ->
      let session : Session = mkSession ()
      let resession = session |> Binary.encode |> Binary.decode |> Either.get
      equals session resession
      finish()

    test "Validate User Serialization" <| fun finish ->
      let user : User = mkUser ()
      let reuser = user |> Binary.encode |> Binary.decode |> Either.get
      equals user reuser
      finish()

    test "Validate Member Serialization" <| fun finish ->
      let mem = Id.Create() |> Member.create
      check mem
      finish ()

    test "Validate Slice Serialization" <| fun finish ->
      [| BoolSlice  (0<index>, true    )
      ; StringSlice (0<index>, "hello" )
      ; NumberSlice (0<index>, 1234.0  )
      ; ByteSlice   (0<index>, mkBytes ())
      ; EnumSlice   (0<index>, { Key = "one"; Value = "two" })
      ; ColorSlice  (0<index>, RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy })
      ; ColorSlice  (0<index>, HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy })
      |]
      |> Array.iter check
      finish()

    test "Validate Slices Serialization" <| fun finish ->
      [| BoolSlices    (mk(), [| true    |])
      ; StringSlices   (mk(), [| "hello" |])
      ; NumberSlices   (mk(), [| 1234.0  |])
      ; ByteSlices     (mk(), [| mkBytes () |])
      ; EnumSlices     (mk(), [| { Key = "one"; Value = "two" } |])
      ; ColorSlices    (mk(), [| RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } |])
      ; ColorSlices    (mk(), [| HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } |])
      |]
      |> Array.iter check
      finish()

    test "Validate Pin Serialization" <| fun finish ->
      Array.iter check (pins ())
      finish()

    test "Validate State Serialization" <| fun finish ->
      mkState () |> check
      finish ()

    test "Validate IrisProject Binary Serializaton" <| fun finish ->
      mkProject()
      |> (fun project ->
          let reproject = project |> Binary.encode |> Binary.decode |> Either.get
          if project <> reproject then
            printfn "project: %O" project
            printfn "reproject: %O" reproject
          equals project reproject)
      finish ()

    test "Validate StateMachine Serialization" <| fun finish ->
      [ AddCue                  <| mkCue ()
      ; UpdateCue               <| mkCue ()
      ; RemoveCue               <| mkCue ()
      ; AddCueList              <| mkCueList ()
      ; UpdateCueList           <| mkCueList ()
      ; RemoveCueList           <| mkCueList ()
      ; AddCuePlayer            <| mkCuePlayer ()
      ; UpdateCuePlayer         <| mkCuePlayer ()
      ; RemoveCuePlayer         <| mkCuePlayer ()
      ; AddSession              <| mkSession ()
      ; UpdateSession           <| mkSession ()
      ; RemoveSession           <| mkSession ()
      ; AddUser                 <| mkUser ()
      ; UpdateUser              <| mkUser ()
      ; RemoveUser              <| mkUser ()
      ; AddPinGroup             <| mkPinGroup ()
      ; UpdatePinGroup          <| mkPinGroup ()
      ; RemovePinGroup          <| mkPinGroup ()
      ; AddClient               <| mkClient ()
      ; UpdateSlices            <| mkSlicesMap ()
      ; UpdateClient            <| mkClient ()
      ; RemoveClient            <| mkClient ()
      ; AddPin                  <| mkPin ()
      ; UpdatePin               <| mkPin ()
      ; RemovePin               <| mkPin ()
      ; AddMember               <| Member.create (Id.Create())
      ; UpdateMember            <| Member.create (Id.Create())
      ; RemoveMember            <| Member.create (Id.Create())
      ; AddDiscoveredService    <| mkDiscoveredService ()
      ; UpdateDiscoveredService <| mkDiscoveredService ()
      ; RemoveDiscoveredService <| mkDiscoveredService ()
      ; DataSnapshot            <| mkState ()
      ; Command AppCommand.Undo
      ; LogMsg(Logger.create Debug "bla" "ohai")
      ; SetLogLevel Warn
      ]
      |> List.iter
        (fun ting ->
          let reting = ting |> Binary.encode |> Binary.decode |> Either.get
          if ting <> reting then
            printfn "ting: %O" ting
            printfn "reting: %O" reting
          equals ting reting)
      // |> List.iter check
      finish()

    test "Validate Error Serialization" <| fun finish ->
      [ OK
        GitError ("one","two")
        ProjectError ("one","two")
        ParseError ("one","two")
        SocketError ("one","two")
        ClientError ("one","two")
        IOError ("one","two")
        AssetError ("one","two")
        RaftError ("one","two")
        Other  ("one","two")
      ] |> List.iter
        (fun error ->
          let reerror =
            error
            |> Binary.buildBuffer
            |> Binary.createBuffer
            |> ErrorFB.GetRootAsErrorFB
            |> IrisError.FromFB
            |> Either.get
          equals error reerror)

      finish()

    test "Validate MachineStatus Binary Serialization" <| fun finish ->
      MachineStatus.Busy (Id.Create(), name (rndstr()))
      |> check
      finish()

    test "Validate DiscoveredService Binary Serialization" <| fun finish ->
      mkDiscoveredService() |> check
      finish()

    test "Validate CuePlayer Binary Serialization" <| fun finish ->
      mkCuePlayer() |> check
      finish()
