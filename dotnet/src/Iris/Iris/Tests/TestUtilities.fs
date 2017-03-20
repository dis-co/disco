namespace Iris.Tests

open Expecto
open System
open System.IO
open Iris.Raft
open Iris.Core

[<AutoOpen>]
module TestUtilities =

  /// abstract over Assert.Equal to create pipe-lineable assertions
  let expect (msg : string) (a : 'a) (b : 't -> 'a) (t : 't) =
    Expect.equal (b t) a msg // apply t to b

  let assume (msg : string) (a : 'a) (b : 't -> 'a) (t : 't) =
    Expect.equal (b t) a msg // apply t to b
    t

  let pending (msg: string) =
    testCase msg <| fun _ -> skiptest "NOT YET IMPLEMENTED"

  let mkUuid () =
    let uuid = Guid.NewGuid()
    string uuid

  let inline expectE (msg: string) (exp: 'b) (f: 'a -> 'b) (input: Either<IrisError,'a>) =
    either {
      let! value = input
      let result = f value
      if result <> exp then
        return!
          sprintf "Expected %A but got %A in %A" exp result msg
          |> Error.asOther "expectE"
          |> Either.fail
      else
        return ()
    }

  let inline count< ^a when ^a : (member Count: int)> (thing: ^a) : int =
    (^a : (member Count: int) thing)

  let inline noError (input: Either<IrisError,'a>) =
    match input with
    | Right _ -> ()
    | Left error ->
      error
      |> Error.toMessage
      |> Tests.failtest

[<AutoOpen>]
module TestData =

  let rand = new Random()

  let rndstr() =
    Id.Create()
    |> string

  let mkTags () =
    [| for n in 0 .. rand.Next(1,20) do
        let guid = Guid.NewGuid()
        yield guid.ToString() |]

  let mk() = Id.Create()

  let mkTmpDir () =
    let path = Path.GetTempPath() </> Path.GetRandomFileName()
    Directory.CreateDirectory path |> ignore
    path

  let mkPin() =
    Pin.Toggle(mk(), rndstr(), mk(), mkTags(), [| true |])

  let mkPins () =
    let props = [| { Key = "one"; Value = "two" }; { Key = "three"; Value = "four"} |]
    let selected = props.[0]
    let rgba = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy }
    let hsla = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy }
    [| Pin.Bang      (mk(), rndstr(), mk(), mkTags(), [| true |])
    ;  Pin.Toggle    (mk(), rndstr(), mk(), mkTags(), [| true |])
    ;  Pin.String    (mk(), rndstr(), mk(), mkTags(), [| rndstr() |])
    ;  Pin.MultiLine (mk(), rndstr(), mk(), mkTags(), [| rndstr() |])
    ;  Pin.FileName  (mk(), rndstr(), mk(), mkTags(), [| rndstr() |])
    ;  Pin.Directory (mk(), rndstr(), mk(), mkTags(), [| rndstr() |])
    ;  Pin.Url       (mk(), rndstr(), mk(), mkTags(), [| rndstr() |])
    ;  Pin.IP        (mk(), rndstr(), mk(), mkTags(), [| rndstr() |])
    ;  Pin.Number    (mk(), rndstr(), mk(), mkTags(), [| double 3.0 |])
    ;  Pin.Bytes     (mk(), rndstr(), mk(), mkTags(), [| [| 2uy; 9uy |] |])
    ;  Pin.Color     (mk(), rndstr(), mk(), mkTags(), [| rgba |])
    ;  Pin.Color     (mk(), rndstr(), mk(), mkTags(), [| hsla |])
    ;  Pin.Enum      (mk(), rndstr(), mk(), mkTags(), props, [| selected |])
    |]

  let mkSlices() =
    BoolSlices(mk(), [| true |])

  let inline asMap arr =
    arr
    |> Array.map toPair
    |> Map.ofArray

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

  let mkPinGroup () : Iris.Core.PinGroup =
    let pins =
      mkPins ()
      |> Array.map toPair
      |> Map.ofArray

    { Id = Id.Create()
      Name = rndstr()
      Client = Id.Create()
      Pins = pins }

  let mkPinGroups () : Iris.Core.PinGroup array =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkPinGroup() |]

  let mkCueList () : CueList =
    { Id = Id.Create(); Name = "PinGroup 3"; Cues = mkCues() }

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

  let mkProject path =
    let machine = MachineConfig.create ()
    Project.create path (rndstr()) machine

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

  let mkState path : Either<IrisError,State> =
    either {
      let! project = mkProject path
      return
        { Project  = project
          PinGroups  = mkPinGroups () |> asMap
          Cues     = mkCues    () |> asMap
          CueLists = mkCueLists() |> asMap
          Sessions = mkSessions() |> asMap
          Users    = mkUsers   () |> asMap
          Clients  = mkClients () |> asMap
          // TODO: Test DiscoveredServices
          DiscoveredServices = Map.empty }
    }

  let mkChange _ =
    match rand.Next(0,2) with
    | n when n > 0 -> MemberAdded(mkMember ())
    |          _   -> MemberRemoved(mkMember ())

  let mkChanges _ =
    let n = rand.Next(1, 6)
    [| for _ in 0 .. n do
        yield mkChange () |]

  let mkLog _ : Either<IrisError,RaftLog> =
    either {
      let! state = mkTmpDir() |> mkState
      return
        LogEntry(Id.Create(), 7u, 1u, DataSnapshot(state),
          Some <| LogEntry(Id.Create(), 6u, 1u, DataSnapshot(state),
            Some <| Configuration(Id.Create(), 5u, 1u, [| mkMember () |],
              Some <| JointConsensus(Id.Create(), 4u, 1u, mkChanges (),
                Some <| Snapshot(Id.Create(), 3u, 1u, 2u, 1u, mkMembers (), DataSnapshot(state))))))
        |> Log.fromEntries
    }

  let testRepo () =
    mkTmpDir ()
    |> fun path ->
      LibGit2Sharp.Repository.Init path |> ignore
      new LibGit2Sharp.Repository(path)
