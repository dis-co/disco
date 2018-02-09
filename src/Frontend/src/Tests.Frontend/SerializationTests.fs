namespace Test.Units

[<RequireQualifiedAccess>]
module SerializationTests =

  open System
  open Fable.Core
  open Fable.Import

  open Disco.Raft
  open Disco.Core
  open Disco.Web.Core
  open Disco.Web.Tests

  open Disco.Core.FlatBuffers
  open Disco.Web.Core.FlatBufferTypes

  let rand = new System.Random()

  let mk() = DiscoId.Create()

  let rndstr() = mk() |> string
  let rndname() = mk() |> string |> name

  let rndport() = rand.Next(0,65535) |> uint16 |> port

  let rndbool() = rand.Next(0,2) |> function
    | 0 -> false
    | _ -> true

  let rndint() = rand.Next()

  let rndchar() = Convert.ToChar(rand.Next() |> uint16)

  let rndplatform() =
    if rand.Next(0,2) > 0
    then Platform.Windows
    else Platform.Unix

  let rndfilepath() =
    rndstr() |> filepath

  let mkBytes _ =
    let num = rand.Next(3, 10)
    let bytes = Array.zeroCreate<byte> num
    for i in 0 .. (num - 1) do
      bytes.[i] <- byte i
    bytes

  let mktags _ =
    [| for n in 0 .. rand.Next(2,8) do
        yield DiscoId.Create() |> string |> astag |]

  let mkProp () =
    { Key = rndstr(); Value = rndstr() }

  let mkProps () =
    [| for n = 0 to rand.Next(2,8) do yield mkProp() |]

  let mkProject _ =
    DiscoProject.Empty

  let pins _ =
    [| Pin.Sink.bang      (mk()) (name "Bang")      (mk()) (mk()) [| true  |]
    ;  Pin.Sink.toggle    (mk()) (name "Toggle")    (mk()) (mk()) [| true  |]
    ;  Pin.Sink.string    (mk()) (name "string")    (mk()) (mk()) [| "one" |]
    ;  Pin.Sink.multiLine (mk()) (name "multiline") (mk()) (mk()) [| "two" |]
    ;  Pin.Sink.fileName  (mk()) (name "filename")  (mk()) (mk()) [| "three" |]
    ;  Pin.Sink.directory (mk()) (name "directory") (mk()) (mk()) [| "four"  |]
    ;  Pin.Sink.url       (mk()) (name "url")       (mk()) (mk()) [| "five" |]
    ;  Pin.Sink.ip        (mk()) (name "ip")        (mk()) (mk()) [| "six"  |]
    ;  Pin.Sink.number    (mk()) (name "number")    (mk()) (mk()) [| double 3.0 |]
    ;  Pin.Sink.bytes     (mk()) (name "bytes")     (mk()) (mk()) [| mkBytes () |]
    ;  Pin.Sink.color     (mk()) (name "rgba")      (mk()) (mk()) [| RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } |]
    ;  Pin.Sink.color     (mk()) (name "hsla")      (mk()) (mk()) [| HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } |]
    ;  Pin.Sink.enum      (mk()) (name "enum")      (mk()) (mk()) [| { Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|]  [| { Key = "one"; Value = "two" } |]
    |]

  let mkPin _ =
    Pin.Sink.string
      (DiscoId.Create())
      (name "url input")
      (DiscoId.Create())
      (DiscoId.Create())
      [| "hello" |]

  let mkSlices() =
    BoolSlices(DiscoId.Create(), None, false, [| true; false; true; true; false |])

  let mkSlicesMap() =
    let slices = mkSlices ()
    [ (slices.PinId, slices) ]
    |> Map.ofList
    |> SlicesMap

  let mkCue _ : Cue =
    { Id = DiscoId.Create()
      Name = name "Cue 1"
      Slices = [| mkSlices() |] }

  let mkCueRef () : CueReference =
    { Id = DiscoId.Create()
      CueId = DiscoId.Create()
      AutoFollow = rndbool()
      Duration = rndint()
      Prewait = rndint() }

  let mkCueRefs () : CueReference array =
    [| for n in 0 .. rand.Next(1,20) -> mkCueRef() |]

  let mkCueGroup () : CueGroup =
    { Id = DiscoId.Create()
      Name = Some (rndname())
      AutoFollow = false
      CueRefs = mkCueRefs() }

  let mkCueListItems () =
    [| for n in 0 .. rand.Next(1,20) -> mkCueGroup() |]

  let mkPinGroup _ : PinGroup =
    let pins = pins () |> Array.map toPair |> Map.ofArray
    { Id = DiscoId.Create()
      Name = name "PinGroup"
      ClientId = DiscoId.Create()
      Path = None
      RefersTo = None
      Pins = pins }

  let mkPinGroupMap() =
    [ for n in 0 .. rand.Next(1,20) -> mkPinGroup() ]
    |> PinGroupMap.ofList

  let mkCueList _ : CueList =
    { Id = DiscoId.Create(); Name = name "PinGroup 3"; Items = mkCueListItems() }

  let mkUser _ =
    { Id = DiscoId.Create()
    ; UserName = name "krgn"
    ; FirstName = name "Karsten"
    ; LastName = name "Gebbert"
    ; Email = email "k@ioctl.it"
    ; Password = checksum "1234"
    ; Salt = checksum "090asd902"
    ; Joined = DateTime.UtcNow
    ; Created = DateTime.UtcNow
    }

  let mkClient () : DiscoClient =
    { Id = DiscoId.Create ()
      Name = name "Nice client"
      Role = Role.Renderer
      Status = ServiceStatus.Running
      ServiceId = DiscoId.Create()
      IpAddress = IPv4Address "127.0.0.1"
      Port = port 8921us }

  let mkDiscoveredService(): DiscoveredService =
    { Id = DiscoId.Create ()
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

  let mkMember _ : ClusterMember =
    { Id               = DiscoId.Create()
      HostName         = rndname ()
      IpAddress        = IPv4Address "127.0.0.1"
      MulticastAddress = IPv4Address "224.0.0.1"
      MulticastPort    = rndport()
      HttpPort         = rndport()
      RaftPort         = rndport()
      WsPort           = rndport()
      GitPort          = rndport()
      ApiPort          = rndport()
      State            = Follower
      Status           = Running }

  let mkSession _ =
    { Id = DiscoId.Create()
      IpAddress = IPv4Address "127.0.0.1"
      UserAgent = "Oh my goodness" }

  let mkPinMapping _ =
    { Id = DiscoId.Create()
      Source = DiscoId.Create()
      Sinks = Set [| DiscoId.Create(); DiscoId.Create() |] }

  let mkPinWidget _ =
    { Id = DiscoId.Create()
      Name = rndname()
      WidgetType = DiscoId.Create() }

  let mkCuePlayer() =
    let rndopt () =
      if rand.Next(0,2) > 0 then
        Some (DiscoId.Create())
      else
        None

    { Id = DiscoId.Create()
      Name = rndname ()
      Locked = false
      Active = false
      Selected = 1<index> * (rand.Next(0,1000))
      RemainingWait = rand.Next(0,1000)
      CueListId = rndopt ()
      CallId = DiscoId.Create()
      NextId = DiscoId.Create()
      PreviousId = DiscoId.Create()
      LastCallerId = rndopt()
      LastCalledId = rndopt() }

  let mkFsPath _ =
    { Drive = rndchar()
      Platform = rndplatform()
      Elements = [ rndstr() ]}

  let mkFsDir path children =
    FsEntry.Directory(
      { Path = path
        Name = FsPath.fileName path
        Size = uint32 (Map.count children)
        MimeType = "application/x-directory"
        Filtered = 0u
      }, children)

  let mkFsFile path =
    FsEntry.File(
      { Path = path
        Name = FsPath.fileName path
        MimeType = "image/png"
        Size = 0u
        Filtered = 0u })

  let mkFsEntry _ =
    let root = mkFsPath()
    let dir1 = root + (rndfilepath())
    let dir2 = root + (rndfilepath())
    let dir3 = root + (rndfilepath())
    let file1 = dir1 + (rndfilepath())
    let file2 = dir2 + (rndfilepath())
    let file3 = dir3 + (rndfilepath())
    FsEntry.Directory(
      { Path = root
        Name = FsPath.fileName root
        MimeType = "application/x-directory"
        Size = 0u
        Filtered = 0u
      },Map [
        dir1, mkFsDir dir1 (Map [ file1, mkFsFile file1 ])
        dir2, mkFsDir dir2 (Map [ file2, mkFsFile file2 ])
        dir3, mkFsDir dir3 (Map [ file3, mkFsFile file3 ])
      ])

  let mkFsTree _ = { HostId = DiscoId.Create(); Root = mkFsEntry(); Filters = Array.empty }

  let mkState _ =
    { Project    = mkProject ()
      PinGroups  = mkPinGroupMap ()
      PinMappings = mkPinMapping () |> fun (map: PinMapping) -> Map.ofArray [| (map.Id, map) |]
      PinWidgets = mkPinWidget () |> fun (map: PinWidget) -> Map.ofArray [| (map.Id, map) |]
      Cues       = mkCue () |> fun (cue: Cue) -> Map.ofArray [| (cue.Id, cue) |]
      CueLists   = mkCueList () |> fun (cuelist: CueList) -> Map.ofArray [| (cuelist.Id, cuelist) |]
      Sessions   = mkSession () |> fun (session: Session) -> Map.ofArray [| (session.Id, session) |]
      Users      = mkUser () |> fun (user: User) -> Map.ofArray [| (user.Id, user) |]
      Clients    = mkClient () |> fun (client: DiscoClient) -> Map.ofArray [| (client.Id, client) |]
      CuePlayers = mkCuePlayer() |> fun (player: CuePlayer) -> Map.ofArray [| (player.Id, player) |]
      FsTrees    = mkFsTree() |> fun (tree: FsTree) -> Map.ofArray [| (tree.Id, tree) |]
      DiscoveredServices = let ser = mkDiscoveredService() in Map.ofArray [| (ser.Id, ser) |] }

  let inline check thing =
    let thong = thing |> Binary.encode |> Binary.decode |> Result.get
    equals thing thong

  let main () =
    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.SerializationTests"
    (* ------------------------------------------------------------------------ *)

    test "should serialize/deserialize StateMachineBatch correctly" <| fun finish ->
      let batch =
        Transaction
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
            AddPinMapping           <| mkPinMapping ()
            UpdatePinMapping        <| mkPinMapping ()
            RemovePinMapping        <| mkPinMapping ()
            AddPinWidget            <| mkPinWidget ()
            UpdatePinWidget         <| mkPinWidget ()
            RemovePinWidget         <| mkPinWidget ()
            AddClient               <| mkClient ()
            UpdateSlices            <| mkSlicesMap ()
            UpdateClient            <| mkClient ()
            RemoveClient            <| mkClient ()
            AddPin                  <| mkPin ()
            UpdatePin               <| mkPin ()
            RemovePin               <| mkPin ()
            AddFsEntry              (DiscoId.Create(), mkFsEntry ())
            UpdateFsEntry           (DiscoId.Create(), mkFsEntry ())
            RemoveFsEntry           (DiscoId.Create(), mkFsPath ())
            AddFsTree               <| mkFsTree()
            RemoveFsTree            <| DiscoId.Create()
            AddMachine              <| Member.create (DiscoId.Create())
            UpdateMachine           <| Member.create (DiscoId.Create())
            RemoveMachine           <| Member.create (DiscoId.Create())
            AddMember               <| mkMember()
            UpdateMember            <| mkMember()
            RemoveMember            <| mkMember()
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
      [| for _ in 0 .. 20 ->  mkCue () |]
      |> Array.iter check
      finish()

    testSync "Validate PinWidget Serialization" <| fun () ->
      let widget : PinWidget = mkPinWidget ()
      let rewidget = widget |> Binary.encode |> Binary.decode |> Result.get
      equals widget rewidget

    testSync "Validate PinMapping Serialization" <| fun () ->
      let mapping : PinMapping = mkPinMapping ()
      let remapping = mapping |> Binary.encode |> Binary.decode |> Result.get
      equals mapping remapping

    testSync "Validate Cue Serialization" <| fun () ->
      let cue : Cue = mkCue ()
      let recue = cue |> Binary.encode |> Binary.decode |> Result.get
      equals cue recue

    testSync "Validate CueReference Serialization" <| fun () ->
      let cueReference : CueReference = mkCueRef ()
      let recueReference = cueReference |> Binary.encode |> Binary.decode |> Result.get
      equals cueReference recueReference

    testSync "Validate CueGroup Serialization" <| fun () ->
      let cueGroup : CueGroup = mkCueGroup ()
      let recueGroup = cueGroup |> Binary.encode |> Binary.decode |> Result.get
      equals cueGroup recueGroup

    test "Validate CueList Serialization" <| fun finish ->
      let cuelist : CueList = mkCueList ()
      let recuelist = cuelist |> Binary.encode |> Binary.decode |> Result.get
      equals cuelist recuelist
      finish()

    test "Validate PinGroup Serialization" <| fun finish ->
      let group : PinGroup = mkPinGroup ()
      let regroup = group |> Binary.encode |> Binary.decode |> Result.get
      equals group regroup
      finish()

    test "Validate Session Serialization" <| fun finish ->
      let session : Session = mkSession ()
      let resession = session |> Binary.encode |> Binary.decode |> Result.get
      equals session resession
      finish()

    test "Validate User Serialization" <| fun finish ->
      let user : User = mkUser ()
      let reuser = user |> Binary.encode |> Binary.decode |> Result.get
      equals user reuser
      finish()

    test "Validate Member Serialization" <| fun finish ->
      let mem = DiscoId.Create() |> Member.create
      check mem
      finish ()

    test "Validate Slice Serialization" <| fun finish ->
      [| BoolSlice  (0<index>, false, true)
      ; StringSlice (0<index>, "hello")
      ; NumberSlice (0<index>, 1234.0)
      ; ByteSlice   (0<index>, mkBytes())
      ; EnumSlice   (0<index>, { Key = "one"; Value = "two" })
      ; ColorSlice  (0<index>, RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy })
      ; ColorSlice  (0<index>, HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy })
      |]
      |> Array.iter check
      finish()

    test "Validate Slices Serialization" <| fun finish ->
      [| BoolSlices    (mk(), None, true, [| true    |])
      ; StringSlices   (mk(), None, [| "hello" |])
      ; NumberSlices   (mk(), None, [| 1234.0  |])
      ; ByteSlices     (mk(), None, [| mkBytes () |])
      ; EnumSlices     (mk(), None, [| { Key = "one"; Value = "two" } |])
      ; ColorSlices    (mk(), None, [| RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } |])
      ; ColorSlices    (mk(), None, [| HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } |])
      |]
      |> Array.iter check
      finish()

    test "Validate Pin Serialization" <| fun finish ->
      Array.iter check (pins ())
      finish()

    test "Validate State Serialization" <| fun finish ->
      mkState () |> check
      finish ()

    test "Validate DiscoProject Binary Serializaton" <| fun finish ->
      mkProject()
      |> (fun project ->
          let reproject = project |> Binary.encode |> Binary.decode |> Result.get
          if project <> reproject then
            printfn "project: %O" project
            printfn "reproject: %O" reproject
          equals project reproject)
      finish ()

    test "Validate StateMachine Serialization" <| fun finish ->
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
        AddPinMapping           <| mkPinMapping ()
        UpdatePinMapping        <| mkPinMapping ()
        RemovePinMapping        <| mkPinMapping ()
        AddPinWidget            <| mkPinWidget ()
        UpdatePinWidget         <| mkPinWidget ()
        RemovePinWidget         <| mkPinWidget ()
        AddClient               <| mkClient ()
        UpdateSlices            <| mkSlicesMap ()
        UpdateClient            <| mkClient ()
        RemoveClient            <| mkClient ()
        AddPin                  <| mkPin ()
        UpdatePin               <| mkPin ()
        RemovePin               <| mkPin ()
        AddMachine              <| Member.create (DiscoId.Create())
        UpdateMachine           <| Member.create (DiscoId.Create())
        RemoveMachine           <| Member.create (DiscoId.Create())
        AddMember               <| mkMember()
        UpdateMember            <| mkMember()
        RemoveMember            <| mkMember()
        AddDiscoveredService    <| mkDiscoveredService ()
        UpdateDiscoveredService <| mkDiscoveredService ()
        RemoveDiscoveredService <| mkDiscoveredService ()
        DataSnapshot            <| mkState ()
        Command AppCommand.Undo
        LogMsg(Logger.create Debug "bla" "ohai")
        SetLogLevel Warn
      ]
      |> List.iter
        (fun ting ->
          let reting = ting |> Binary.encode |> Binary.decode |> Result.get
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
            |> DiscoError.FromFB
            |> Result.get
          equals error reerror)

      finish()

    test "Validate MachineStatus Binary Serialization" <| fun finish ->
      MachineStatus.Busy (DiscoId.Create(), name (rndstr()))
      |> check
      finish()

    test "Validate DiscoveredService Binary Serialization" <| fun finish ->
      mkDiscoveredService() |> check
      finish()

    test "Validate CuePlayer Binary Serialization" <| fun finish ->
      mkCuePlayer() |> check
      finish()

    test "Validate FsPath Binary Serialization" <| fun finish ->
      mkFsPath() |> check
      finish()

    test "Validate FsEntry Binary Serialization" <| fun finish ->
      mkFsEntry() |> check
      finish()

    test "Validate FsTree Binary Serialization" <| fun finish ->
      mkFsTree() |> check
      finish()
