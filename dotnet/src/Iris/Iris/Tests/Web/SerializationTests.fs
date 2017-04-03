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

  let rand = new System.Random()

  let mk() = Id.Create()

  let mkBytes _ =
    let num = rand.Next(3, 10)
    let bytes = JS.Uint8Array.Create(JS.ArrayBuffer.Create(float num))
    for i in 0 .. (num - 1) do
      bytes.[i] <- float i
    bytes.buffer

  let mktags _ =
    [| for n in 0 .. rand.Next(2,8) do
        yield Id.Create() |> string |]

  let mkProject _ =
    IrisProject.Empty

  let pins _ =
    [| Pin.Bang      (mk(), "Bang",      mk(), mktags (), [| true  |])
    ;  Pin.Toggle    (mk(), "Toggle",    mk(), mktags (), [| true  |])
    ;  Pin.String    (mk(), "string",    mk(), mktags (), [| "one" |])
    ;  Pin.MultiLine (mk(), "multiline", mk(), mktags (), [| "two" |])
    ;  Pin.FileName  (mk(), "filename",  mk(), mktags (), [| "three" |])
    ;  Pin.Directory (mk(), "directory", mk(), mktags (), [| "four"  |])
    ;  Pin.Url       (mk(), "url",       mk(), mktags (), [| "five" |])
    ;  Pin.IP        (mk(), "ip",        mk(), mktags (), [| "six"  |])
    ;  Pin.Number    (mk(), "number",    mk(), mktags (), [| double 3.0 |])
    ;  Pin.Bytes     (mk(), "bytes",     mk(), mktags (), [| mkBytes () |])
    ;  Pin.Color     (mk(), "rgba",      mk(), mktags (), [| RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } |])
    ;  Pin.Color     (mk(), "hsla",      mk(), mktags (), [| HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } |])
    ;  Pin.Enum      (mk(), "enum",      mk(), mktags (), [| { Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|] , [| { Key = "one"; Value = "two" } |])
    |]

  let mkPin _ =
    Pin.String(Id.Create(), "url input", Id.Create(), [| |], [| "hello" |])

  let mkSlices() =
    BoolSlices(Id.Create(), [| true; false; true; true; false |])

  let mkCue _ : Cue =
    { Id = Id.Create(); Name = "Cue 1"; Pins = pins () }

  let mkPinGroup _ : PinGroup =
    let pins = pins () |> Array.map toPair |> Map.ofArray
    { Id = Id.Create()
      Name = "PinGroup 3"
      Client = Id.Create()
      Pins = pins }

  let mkCueList _ : CueList =
    { Id = Id.Create(); Name = "PinGroup 3"; Cues = [| mkCue (); mkCue () |] }

  let mkUser _ =
    { Id = Id.Create()
    ; UserName = "krgn"
    ; FirstName = "Karsten"
    ; LastName = "Gebbert"
    ; Email = "k@ioctl.it"
    ; Password = "1234"
    ; Salt = "090asd902"
    ; Joined = DateTime.UtcNow
    ; Created = DateTime.UtcNow
    }

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

  let mkMember _ = Id.Create() |> Member.create

  let mkSession _ =
    { Id = Id.Create()
    ; IpAddress = IPv4Address "127.0.0.1"
    ; UserAgent = "Oh my goodness" }

  let mkState _ =
    { Project  = mkProject ()
    ; PinGroups  = mkPinGroup   () |> fun (group: PinGroup) -> Map.ofArray [| (group.Id, group) |]
    ; Cues     = mkCue     () |> fun (cue: Cue) -> Map.ofArray [| (cue.Id, cue) |]
    ; CueLists = mkCueList () |> fun (cuelist: CueList) -> Map.ofArray [| (cuelist.Id, cuelist) |]
    ; Sessions = mkSession () |> fun (session: Session) -> Map.ofArray [| (session.Id, session) |]
    ; Users    = mkUser    () |> fun (user: User) -> Map.ofArray [| (user.Id, user) |]
    ; Clients  = mkClient  () |> fun (client: IrisClient) -> Map.ofArray [| (client.Id, client) |]
    // TODO: Test DiscoveredServices
    ; DiscoveredServices = Map.empty
    }

  let inline check thing =
    thing |> Binary.encode |> Binary.decode |> Either.get
    |> fun thong -> equals thong thing

  let main () =
    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.SerializationTests"
    (* ------------------------------------------------------------------------ *)

    test "should serialize/deserialize cue correctly" <| fun finish ->
      [| for i in 0 .. 20 do
          yield  mkCue () |]
      |> Array.iter check
      finish()

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
      [| BoolSlice    (0u, true    )
      ; StringSlice   (0u, "hello" )
      ; NumberSlice   (0u, 1234.0  )
      ; ByteSlice     (0u, mkBytes ())
      ; EnumSlice     (0u, { Key = "one"; Value = "two" })
      ; ColorSlice    (0u, RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy })
      ; ColorSlice    (0u, HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy })
      |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Binary.encode |> Binary.decode |> Either.get
          equals slice reslice)
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
      |> Array.iter
        (fun slices ->
          let reslices = slices |> Binary.encode |> Binary.decode |> Either.get
          equals slices reslices)
      finish()

    test "Validate Pin Serialization" <| fun finish ->
      Array.iter check (pins ())
      finish()

    test "Validate State Serialization" <| fun finish ->
      let state : State = mkState ()
      let restate : State = state |> Binary.encode |> Binary.decode |> Either.get
      equals restate state
      finish ()

    test "Validate IrisProject Binary Serializaton" <| fun finish ->
      let project = mkProject()
      let reproject = project |> Binary.encode |> Binary.decode |> Either.get
      equals project reproject
      finish ()

    test "Validate StateMachine Serialization" <| fun finish ->
      [ AddCue        <| mkCue ()
      ; UpdateCue     <| mkCue ()
      ; RemoveCue     <| mkCue ()
      ; AddCueList    <| mkCueList ()
      ; UpdateCueList <| mkCueList ()
      ; RemoveCueList <| mkCueList ()
      ; AddSession    <| mkSession ()
      ; UpdateSession <| mkSession ()
      ; RemoveSession <| mkSession ()
      ; AddUser       <| mkUser ()
      ; UpdateUser    <| mkUser ()
      ; RemoveUser    <| mkUser ()
      ; AddPinGroup      <| mkPinGroup ()
      ; UpdatePinGroup   <| mkPinGroup ()
      ; RemovePinGroup   <| mkPinGroup ()
      ; AddClient     <| mkClient ()
      ; UpdateSlices  <| mkSlices ()
      ; UpdateClient  <| mkClient ()
      ; RemoveClient  <| mkClient ()
      ; AddPin        <| mkPin ()
      ; UpdatePin     <| mkPin ()
      ; RemovePin     <| mkPin ()
      ; AddMember     <| Member.create (Id.Create())
      ; UpdateMember  <| Member.create (Id.Create())
      ; RemoveMember  <| Member.create (Id.Create())
      ; DataSnapshot  <| mkState ()
      ; Command AppCommand.Undo
      ; LogMsg(Logger.create Debug "bla" "ohai")
      ; SetLogLevel Warn
      ]
      |> List.iter check
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
      ] |> List.iter check

      finish()
