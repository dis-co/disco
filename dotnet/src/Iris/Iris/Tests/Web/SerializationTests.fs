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
    [| Pin.Bang      (Id.Create(), "Bang",      Id.Create(), mktags (), [|{ Index = 9u; Value = true            }|])
    ; Pin.Toggle    (Id.Create(), "Toggle",    Id.Create(), mktags (), [|{ Index = 8u; Value = true            }|])
    ; Pin.String    (Id.Create(), "string",    Id.Create(), mktags (), [|{ Index = 3u; Value = "one"           }|])
    ; Pin.MultiLine (Id.Create(), "multiline", Id.Create(), mktags (), [|{ Index = 2u; Value = "two"           }|])
    ; Pin.FileName  (Id.Create(), "filename",  Id.Create(), mktags (), "haha", [|{ Index = 1u; Value = "three" }|])
    ; Pin.Directory (Id.Create(), "directory", Id.Create(), mktags (), "hmmm", [|{ Index = 6u; Value = "four"  }|])
    ; Pin.Url       (Id.Create(), "url",       Id.Create(), mktags (), [|{ Index = 4u; Value = "five"          }|])
    ; Pin.IP        (Id.Create(), "ip",        Id.Create(), mktags (), [|{ Index = 5u; Value = "six"           }|])
    ; Pin.Float     (Id.Create(), "float",     Id.Create(), mktags (), [|{ Index = 0u; Value = 3.0             }|])
    ; Pin.Double    (Id.Create(), "double",    Id.Create(), mktags (), [|{ Index = 0u; Value = double 3.0      }|])
    ; Pin.Bytes     (Id.Create(), "bytes",     Id.Create(), mktags (), [|{ Index = 0u; Value = mkBytes ()      }|])
    ; Pin.Color     (Id.Create(), "rgba",      Id.Create(), mktags (), [|{ Index = 0u; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }|])
    ; Pin.Color     (Id.Create(), "hsla",      Id.Create(), mktags (), [|{ Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }|])
    ; Pin.Enum      (Id.Create(), "enum",      Id.Create(), mktags (), [|{ Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|] , [|{ Index = 0u; Value = { Key = "one"; Value = "two" }}|])
    |]

  let mkPin _ =
    let slice : StringSliceD = { Index = 0u; Value = "hello" }
    Pin.String(Id.Create(), "url input", Id.Create(), [| |], [| slice |])

  let mkSlices() =
    BoolSlices(Id.Create(), [|{Index=1u; Value=true}|])

  let mkCue _ : Cue =
    { Id = Id.Create(); Name = "Cue 1"; Pins = pins () }

  let mkPatch _ : Patch =
    let pins = pins () |> Array.map toPair |> Map.ofArray
    { Id = Id.Create(); Name = "Patch 3"; Pins = pins }

  let mkCueList _ : CueList =
    { Id = Id.Create(); Name = "Patch 3"; Cues = [| mkCue (); mkCue () |] }

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
    ; Patches  = mkPatch   () |> fun (patch: Patch) -> Map.ofArray [| (patch.Id, patch) |]
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

    test "Validate Patch Serialization" <| fun finish ->
      let patch : Patch = mkPatch ()
      let repatch = patch |> Binary.encode |> Binary.decode |> Either.get
      equals patch repatch
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
      [| BoolSlice     { Index = 0u; Value = true    }
      ; StringSlice   { Index = 0u; Value = "hello" }
      ; IntSlice      { Index = 0u; Value = 1234    }
      ; FloatSlice    { Index = 0u; Value = 1234.0  }
      ; DoubleSlice   { Index = 0u; Value = 1234.0  }
      ; ByteSlice     { Index = 0u; Value = mkBytes () }
      ; EnumSlice     { Index = 0u; Value = { Key = "one"; Value = "two" }}
      ; ColorSlice    { Index = 0u; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }
      ; ColorSlice    { Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }
      ; CompoundSlice { Index = 0u; Value = pins () } |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Binary.encode |> Binary.decode |> Either.get
          equals slice reslice)
      finish()

    test "Validate Slices Serialization" <| fun finish ->
      [| BoolSlices     (Id.Create(), [|{ Index = 0u; Value = true    }|])
      ; StringSlices   (Id.Create(), [|{ Index = 0u; Value = "hello" }|])
      ; IntSlices      (Id.Create(), [|{ Index = 0u; Value = 1234    }|])
      ; FloatSlices    (Id.Create(), [|{ Index = 0u; Value = 1234.0  }|])
      ; DoubleSlices   (Id.Create(), [|{ Index = 0u; Value = 1234.0  }|])
      ; ByteSlices     (Id.Create(), [|{ Index = 0u; Value = mkBytes () }|])
      ; EnumSlices     (Id.Create(), [|{ Index = 0u; Value = { Key = "one"; Value = "two" }}|])
      ; ColorSlices    (Id.Create(), [|{ Index = 0u; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }|])
      ; ColorSlices    (Id.Create(), [|{ Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }|])
      ; CompoundSlices (Id.Create(), [|{ Index = 0u; Value = pins () }|]) |]
      |> Array.iter
        (fun slices ->
          let reslices = slices |> Binary.encode |> Binary.decode |> Either.get
          equals slices reslices)
      finish()

    test "Validate Pin Serialization" <| fun finish ->
      Array.iter check (pins ())

      let compound = Pin.Compound(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = pins () }|])
      check compound

      // nested compound :P
      let nested = Pin.Compound(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = [| compound |] }|])
      check nested

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
      ; AddPatch      <| mkPatch ()
      ; UpdatePatch   <| mkPatch ()
      ; RemovePatch   <| mkPatch ()
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
      ; LogMsg(Logger.create Debug (Id.Create()) "bla" "ohai")
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
