namespace Test.Units

[<RequireQualifiedAccess>]
module SerializationTests =

  open Fable.Core
  open Fable.Import

  open Iris.Raft
  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests
  open Iris.Web.Views

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

  let ioboxes _ =
    [| IOBox.Bang       (Id.Create(), "Bang",      Id.Create(), mktags (), [|{ Index = 9u; Value = true            }|])
    ; IOBox.Toggle     (Id.Create(), "Toggle",    Id.Create(), mktags (), [|{ Index = 8u; Value = true            }|])
    ; IOBox.String     (Id.Create(), "string",    Id.Create(), mktags (), [|{ Index = 3u; Value = "one"           }|])
    ; IOBox.MultiLine  (Id.Create(), "multiline", Id.Create(), mktags (), [|{ Index = 2u; Value = "two"           }|])
    ; IOBox.FileName   (Id.Create(), "filename",  Id.Create(), mktags (), "haha", [|{ Index = 1u; Value = "three" }|])
    ; IOBox.Directory  (Id.Create(), "directory", Id.Create(), mktags (), "hmmm", [|{ Index = 6u; Value = "four"  }|])
    ; IOBox.Url        (Id.Create(), "url",       Id.Create(), mktags (), [|{ Index = 4u; Value = "five"          }|])
    ; IOBox.IP         (Id.Create(), "ip",        Id.Create(), mktags (), [|{ Index = 5u; Value = "six"           }|])
    ; IOBox.Float      (Id.Create(), "float",     Id.Create(), mktags (), [|{ Index = 0u; Value = 3.0             }|])
    ; IOBox.Double     (Id.Create(), "double",    Id.Create(), mktags (), [|{ Index = 0u; Value = double 3.0      }|])
    ; IOBox.Bytes      (Id.Create(), "bytes",     Id.Create(), mktags (), [|{ Index = 0u; Value = mkBytes ()      }|])
    ; IOBox.Color      (Id.Create(), "rgba",      Id.Create(), mktags (), [|{ Index = 0u; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }|])
    ; IOBox.Color      (Id.Create(), "hsla",      Id.Create(), mktags (), [|{ Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }|])
    ; IOBox.Enum       (Id.Create(), "enum",      Id.Create(), mktags (), [|{ Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|] , [|{ Index = 0u; Value = { Key = "one"; Value = "two" }}|])
    |]

  let mkIOBox _ =
    let slice : StringSliceD = { Index = 0u; Value = "hello" }
    IOBox.String(Id.Create(), "url input", Id.Create(), [| |], [| slice |])

  let mkCue _ : Cue =
    { Id = Id.Create(); Name = "Cue 1"; IOBoxes = ioboxes () }

  let mkPatch _ : Patch =
    let ioboxes = ioboxes () |> Array.map toPair |> Map.ofArray
    { Id = Id.Create(); Name = "Patch 3"; IOBoxes = ioboxes }

  let mkCueList _ : CueList =
    { Id = Id.Create(); Name = "Patch 3"; Cues = [| mkCue (); mkCue () |] }

  let mkUser _ =
    { Id = Id.Create()
    ; UserName = "krgn"
    ; FirstName = "Karsten"
    ; LastName = "Gebbert"
    ; Email = "k@ioctl.it"
    ; Joined = "1"
    ; Created = "2"
    }

  let mkNode _ = Id.Create() |> Node.create

  let mkSession _ =
    { Id = Id.Create()
    ; UserName = "krgn"
    ; IpAddress = IPv4Address "127.0.0.1"
    ; UserAgent = "Oh my goodness"
    }

  let mkState _ =
    { Patches  = Map.empty // mkPatch   () |> fun (patch: Patch) -> Map.ofList [ (patch.Id, patch) ]
    ; IOBoxes  = Map.empty // ioboxes   () |> (fun (boxes: IOBox array) -> Array.map toPair boxes) |> Map.ofArray
    ; Cues     = Map.empty // mkCue     () |> fun (cue: Cue) -> Map.ofList [ (cue.Id, cue) ]
    ; CueLists = Map.empty // mkCueList () |> fun (cuelist: CueList) -> Map.ofList [ (cuelist.Id, cuelist) ]
    ; Nodes    = Map.empty // mkNode    () |> fun (node: RaftNode) -> Map.ofList [ (node.Id, node) ]
    ; Sessions = Map.empty // mkSession () |> fun (session: Session) -> Map.ofList [ (session.Id, session) ]
    ; Users    = mkUser    () |> fun (user: User) -> Map.ofList [ (user.Id, user) ]
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

    test "Validate Node Serialization" <| fun finish ->
      let node = Id.Create() |> Node.create
      check node
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
      ; CompoundSlice { Index = 0u; Value = ioboxes () } |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Binary.encode |> Binary.decode |> Either.get
          equals slice reslice)
      finish()

    test "Validate IOBox Serialization" <| fun finish ->
      Array.iter check (ioboxes ())

      let compound = IOBox.CompoundBox(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = ioboxes () }|])
      check compound

      // nested compound :P
      let nested = IOBox.CompoundBox(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = [| compound |] }|])
      check nested

      finish()

    test "Validate State Serialization" <| fun finish ->
      let state : State = mkState ()
      let restate : State = state |> Binary.encode |> Binary.decode |> Either.get
      equals restate state
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
      ; AddIOBox      <| mkIOBox ()
      ; UpdateIOBox   <| mkIOBox ()
      ; RemoveIOBox   <| mkIOBox ()
      ; AddNode       <| Node.create (Id.Create())
      ; UpdateNode    <| Node.create (Id.Create())
      ; RemoveNode    <| Node.create (Id.Create())
      ; DataSnapshot  <| mkState ()
      ; Command AppCommand.Undo
      // ; LogMsg(Debug, "ohai")
      ]
      |> List.iter check
      finish()

    test "Validate Error Serialization" <| fun finish ->
      [ OK
      ; BranchNotFound        "bla"
      ; BranchDetailsNotFound "haha"
      ; RepositoryNotFound    "haha"
      ; RepositoryInitFailed  "haha"
      ; CommitError           "haha"
      ; GitError              "haha"
      ; ProjectNotFound       "aklsdfl"
      ; ProjectPathError
      ; ProjectSaveError      "lskdfj"
      ; ProjectParseError     "lskdfj"
      ; MissingNodeId
      ; MissingNode           "lak"
      ; ProjectInitError      "oiwe"
      ; ParseError            "blaociasd"
      ; MetaDataNotFound
      ; MissingStartupDir
      ; CliParseError
      ; AssetSaveError        "lskd"
      ; AssetDeleteError      "lskd"
      ; AlreadyVoted
      ; AppendEntryFailed
      ; CandidateUnknown
      ; EntryInvalidated
      ; InvalidCurrentIndex
      ; InvalidLastLog
      ; InvalidLastLogTerm
      ; InvalidTerm
      ; LogFormatError
      ; LogIncomplete
      ; NoError
      ; NoNode
      ; NotCandidate
      ; NotLeader
      ; NotVotingState
      ; ResponseTimeout
      ; SnapshotFormatError
      ; StaleResponse
      ; UnexpectedVotingChange
      ; VoteTermMismatch
      ; Other "whatever"
      ] |> List.iter check

      finish()
