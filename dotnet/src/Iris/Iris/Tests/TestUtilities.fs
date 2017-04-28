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

  let rndname() =
    Id.Create()
    |> string
    |> name

  let rndport() =
    rand.Next(0, int UInt16.MaxValue)
    |> uint16
    |> port

  let mkTags () =
    [| for n in 0 .. rand.Next(1,20) do
        let guid = Guid.NewGuid()
        yield guid.ToString() |> astag |]

  let mk() = Id.Create()

  let mkTmpDir () =
    let path = Path.getTempPath() </> Path.getRandomFileName()
    Directory.createDirectory path |> ignore
    path

  let mkByte() =
    let arr = rand.Next(2,12) |> Array.zeroCreate
    rand.NextBytes(arr)
    arr

  let mkBytes() =
    [| for n in 0 .. rand.Next(2,12) -> mkByte() |]

  let mkBools() =
    [| for n in 0 .. rand.Next(2,9) -> rand.Next(0,2) > 0 |]

  let mkStrings() =
    [| for n in 0 .. rand.Next(2,8) -> rndstr() |]

  let mkNumbers() =
    [| for n in 0 .. rand.Next(2,8) -> rand.NextDouble() |]

  let mkProps() =
    [| for n in 0 .. rand.Next(2,12) -> { Key = rndstr(); Value = rndstr() } |]

  let mkPin() =
    Pin.Toggle(mk(), rndstr(), mk(), mkTags(), [| true |])

  let mkColors() =
    [| for n in 0 .. rand.Next(2,12) do
         if rand.Next(0,2) > 0 then
            yield RGBA { Red   = uint8 (rand.Next(0,255))
                         Green = uint8 (rand.Next(0,255))
                         Blue  = uint8 (rand.Next(0,255))
                         Alpha = uint8 (rand.Next(0,255)) }
         else
            yield HSLA { Hue        = uint8 (rand.Next(0,255))
                         Saturation = uint8 (rand.Next(0,255))
                         Lightness  = uint8 (rand.Next(0,255))
                         Alpha      = uint8 (rand.Next(0,255)) } |]

  let mkPins () =
    [| Pin.Bang      (mk(), rndstr(), mk(), mkTags(), mkBools())
    ;  Pin.Toggle    (mk(), rndstr(), mk(), mkTags(), mkBools())
    ;  Pin.String    (mk(), rndstr(), mk(), mkTags(), mkStrings())
    ;  Pin.MultiLine (mk(), rndstr(), mk(), mkTags(), mkStrings())
    ;  Pin.FileName  (mk(), rndstr(), mk(), mkTags(), mkStrings())
    ;  Pin.Directory (mk(), rndstr(), mk(), mkTags(), mkStrings())
    ;  Pin.Url       (mk(), rndstr(), mk(), mkTags(), mkStrings())
    ;  Pin.IP        (mk(), rndstr(), mk(), mkTags(), mkStrings())
    ;  Pin.Number    (mk(), rndstr(), mk(), mkTags(), mkNumbers())
    ;  Pin.Bytes     (mk(), rndstr(), mk(), mkTags(), mkBytes())
    ;  Pin.Color     (mk(), rndstr(), mk(), mkTags(), mkColors())
    ;  Pin.Enum      (mk(), rndstr(), mk(), mkTags(), mkProps(), mkProps())
    |]

  let mkSlice() =
    match rand.Next(0,6) with
    | 0 -> BoolSlices(mk(), mkBools())
    | 1 -> StringSlices(mk(), mkStrings())
    | 2 -> NumberSlices(mk(), mkNumbers())
    | 3 -> ByteSlices(mk(), mkBytes())
    | 4 -> ColorSlices(mk(), mkColors())
    | _ -> EnumSlices(mk(), mkProps())

  let mkSlices() =
    [| for n in 0 .. rand.Next(2,12) -> mkSlice() |]

  let inline asMap arr =
    arr
    |> Array.map toPair
    |> Map.ofArray

  let mkUser () =
    { Id = Id.Create()
      UserName = rndname ()
      FirstName = rndname ()
      LastName = rndname ()
      Email = email (rndstr())
      Password = checksum (rndstr())
      Salt = checksum (rndstr())
      Joined = System.DateTime.Now
      Created = System.DateTime.Now }

  let mkUsers () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkUser() |]

  let mkCue () : Cue =
    { Id = Id.Create(); Name = rndstr(); Slices = mkSlices() }

  let mkCues () =
    [| for n in 0 .. rand.Next(1,20) -> mkCue() |]

  let mkPinGroup () : Iris.Core.PinGroup =
    let pins =
      mkPins ()
      |> Array.map toPair
      |> Map.ofArray

    { Id = Id.Create()
      Name = rndname ()
      Client = Id.Create()
      Pins = pins }

  let mkPinGroups () : Iris.Core.PinGroup array =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkPinGroup() |]

  let mkCueList () : CueList =
    { Id = Id.Create(); Name = name "PinGroup 3"; Cues = mkCues() }

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
    let machine = MachineConfig.create()
    Project.create path (rndstr()) machine

  let mkClient () : IrisClient =
    { Id = Id.Create ()
      Name = "Nice client"
      Role = Role.Renderer
      Status = ServiceStatus.Running
      IpAddress = IPv4Address "127.0.0.1"
      Port = port 8921us }

  let mkClients () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkClient() |]

  let mkDiscoveredService(): DiscoveredService =
    { Id = Id.Create ()
      Name = "Nice service"
      WebPort = port 8921us
      FullName = "Really nice service"
      HostName = "remotehost"
      HostTarget = "localhost"
      Status = Idle
      Aliases = [||]
      Protocol = IPProtocol.IPv4
      AddressList = [||]
      Services = [||]
      ExtraMetadata = [||] }

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
          DiscoveredServices = let ser = mkDiscoveredService() in Map.ofArray [| (ser.Id, ser) |] }
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
        LogEntry(Id.Create(), index 7, term 1, DataSnapshot(state),
          Some <| LogEntry(Id.Create(), index 6, term 1, DataSnapshot(state),
            Some <| Configuration(Id.Create(), index 5, term 1, [| mkMember () |],
              Some <| JointConsensus(Id.Create(), index 4, term 1, mkChanges (),
                Some <| Snapshot(Id.Create(), index 3, term 1, index 2, term 1, mkMembers (), DataSnapshot(state))))))
        |> Log.fromEntries
    }

  let testRepo () =
    mkTmpDir ()
    |> fun path ->
      path |> unwrap |> LibGit2Sharp.Repository.Init |> ignore
      new LibGit2Sharp.Repository(unwrap path)

  let inline binaryEncDec (thing: ^t) =
    let rething: ^t = thing |> Binary.encode |> Binary.decode |> Either.get
    expect "Should be equal" thing id rething

  let inline yamlEncDec (thing: ^t) =
    let rething: ^t = thing |> Yaml.encode |> Yaml.decode |> Either.get
    expect "Should be equal" thing id rething
