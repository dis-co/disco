(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open Expecto
open System
open System.IO
open System.Threading
open System.Collections.Concurrent
open Disco.Raft
open Disco.Core
open SharpYaml.Serialization

[<AutoOpen>]
module TestUtilities =

  [<Literal>]
  let DISCO_EVENT_TIMEOUT = 30000.0

  type WaitEvent() =
    let queue = new BlockingCollection<unit>()

    member ev.Set() =
      try queue.Add <| () with exn -> Logger.info "WaitEvent.Set" exn.Message

    member ev.WaitOne(tmo: TimeSpan) =
      try
        let mutable result = ()
        queue.TryTake(&result, tmo)
      with exn ->
        Logger.info "WaitEvent.WaitOne" exn.Message
        false

    interface IDisposable with
      member self.Dispose() =
        try queue.Dispose() with _ -> ()

  let waitFor (tag: string) (we: WaitEvent) =
    if we.WaitOne(TimeSpan.FromMilliseconds DISCO_EVENT_TIMEOUT)
    then Either.succeed()
    else
      sprintf "Timout after %f waiting for %s" DISCO_EVENT_TIMEOUT tag
      |> Error.asOther "test"
      |> Either.fail

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

  let inline expectE (msg: string) (exp: 'b) (f: 'a -> 'b) (input: Either<DiscoError,'a>) =
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

  let inline noError (input: Either<DiscoError,'a>) =
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
    DiscoId.Create()
    |> string

  let rndname() =
    DiscoId.Create()
    |> string
    |> name

  let rndport() =
    rand.Next(0, int UInt16.MaxValue)
    |> uint16
    |> port

  let rndint() =
    rand.Next()

  let rndbool() =
    rand.Next(0,2) |> Convert.ToBoolean

  let mkTags () =
    [| for n in 0 .. rand.Next(1,20) do
        let guid = Guid.NewGuid()
        yield guid.ToString() |> astag |]

  let mk() = DiscoId.Create()

  let maybe_mk() =
    if rand.Next(0,2) > 0
    then Some(mk())
    else None

  let mkTmpDir () =
    let path = Path.getTempPath() </> Path.getRandomFileName()
    Directory.createDirectory path |> ignore
    path

  let mkByte() =
    let arr = rand.Next(2,12) |> Array.zeroCreate
    rand.NextBytes(arr)
    arr

  let mkBool() = rand.Next(0,2) > 0

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
    Pin.Sink.toggle (mk()) (rndname()) (mk()) (mk()) [| true |]

  let mkOptional(f:unit->'T): 'T option =
    if rand.Next(0,2) > 0 then f() |> Some else None

  let mkDiscoveredService() =
    { Id = DiscoId.Create()
      Name = rndstr()
      FullName = rndstr()
      HostName = rndstr()
      HostTarget = rndstr()
      Status = MachineStatus.Busy (DiscoId.Create(), name (rndstr()))
      Aliases = [| for n in 0 .. rand.Next(2,4) -> rndstr() |]
      Protocol = IPProtocol.IPv4
      AddressList = [| IPv4Address "127.0.0.1" |]
      Services = [| { ServiceType = ServiceType.Git; Port = rndport() }
                    { ServiceType = ServiceType.Raft; Port = rndport() }
                    { ServiceType = ServiceType.Api; Port = rndport() }
                    { ServiceType = ServiceType.Http; Port = rndport() }
                    { ServiceType = ServiceType.WebSocket; Port = rndport() } |]
      ExtraMetadata = mkProps() }

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
    [| Pin.Sink.bang      (mk()) (rndname()) (mk()) (mk()) (mkBools())
    ;  Pin.Sink.toggle    (mk()) (rndname()) (mk()) (mk()) (mkBools())
    ;  Pin.Sink.string    (mk()) (rndname()) (mk()) (mk()) (mkStrings())
    ;  Pin.Sink.multiLine (mk()) (rndname()) (mk()) (mk()) (mkStrings())
    ;  Pin.Sink.fileName  (mk()) (rndname()) (mk()) (mk()) (mkStrings())
    ;  Pin.Sink.directory (mk()) (rndname()) (mk()) (mk()) (mkStrings())
    ;  Pin.Sink.url       (mk()) (rndname()) (mk()) (mk()) (mkStrings())
    ;  Pin.Sink.ip        (mk()) (rndname()) (mk()) (mk()) (mkStrings())
    ;  Pin.Sink.number    (mk()) (rndname()) (mk()) (mk()) (mkNumbers())
    ;  Pin.Sink.bytes     (mk()) (rndname()) (mk()) (mk()) (mkBytes())
    ;  Pin.Sink.color     (mk()) (rndname()) (mk()) (mk()) (mkColors())
    ;  Pin.Sink.enum      (mk()) (rndname()) (mk()) (mk()) (mkProps()) (mkProps())
    |]

  let mkSlice() =
    match rand.Next(0,6) with
    | 0 -> BoolSlices(mk(), maybe_mk(), mkBool(), mkBools())
    | 1 -> StringSlices(mk(), maybe_mk(), mkStrings())
    | 2 -> NumberSlices(mk(), maybe_mk(), mkNumbers())
    | 3 -> ByteSlices(mk(), maybe_mk(), mkBytes())
    | 4 -> ColorSlices(mk(), maybe_mk(), mkColors())
    | _ -> EnumSlices(mk(), maybe_mk(), mkProps())

  let mkSlices() =
    [| for n in 0 .. rand.Next(2,12) -> mkSlice() |]

  let inline asMap arr =
    arr
    |> Array.map toPair
    |> Map.ofArray

  let mkUser () =
    { Id = DiscoId.Create()
      UserName = rndname ()
      FirstName = rndname ()
      LastName = rndname ()
      Email = email (rndstr())
      Password = checksum (rndstr())
      Salt = checksum (rndstr())
      Joined = System.DateTime.Now
      Created = System.DateTime.Now }

  let mkCuePlayer() =
    let rndopt () =
      if rand.Next(0,2) > 0 then
        Some (DiscoId.Create())
      else
        None

    { Id = DiscoId.Create()
      Name = rndname ()
      Locked = rndbool ()
      Active = rndbool ()
      CueListId = rndopt()
      Selected = index (rand.Next(0,1000))
      CallId = DiscoId.Create()
      NextId = DiscoId.Create()
      PreviousId = DiscoId.Create()
      RemainingWait = rand.Next(0,1000)
      LastCallerId = rndopt()
      LastCalledId = rndopt() }

  let mkUsers () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkUser() |]

  let mkCue () : Cue =
    { Id = DiscoId.Create(); Name = rndname(); Slices = mkSlices() }

  let mkCues () =
    [| for n in 0 .. rand.Next(1,20) -> mkCue() |]

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

  let mkCueGroups () : CueGroup array =
    [| for n in 0 .. rand.Next(1,20) -> mkCueGroup() |]

  let mkPinGroup () : Disco.Core.PinGroup =
    let pins =
      mkPins ()
      |> Array.map (Pin.setPersisted true >> toPair)
      |> Map.ofArray

    { Id = DiscoId.Create()
      Name = rndname ()
      Path = Some (filepath "/dev/null")
      ClientId = DiscoId.Create()
      RefersTo = None
      Pins = pins }

  let mkPinMapping() =
    { Id = DiscoId.Create()
      Source = DiscoId.Create()
      Sinks = Set [ DiscoId.Create() ] }

  let mkPinWidget() =
    { Id = DiscoId.Create()
      Name = rndname()
      WidgetType = DiscoId.Create() }

  let mkPinGroups () : Disco.Core.PinGroup array =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkPinGroup() |]

  let mkPinGroupMap() =
    mkPinGroups () |> PinGroupMap.ofArray

  let mkPinMappings () : Disco.Core.PinMapping array =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkPinMapping() |]

  let mkPinWidgets () : Disco.Core.PinWidget array =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkPinWidget() |]

  let mkCueListItems() =
    Array.empty

  let mkCueList () : CueList =
    { Id = DiscoId.Create(); Name = name "PinGroup 3"; Items = mkCueListItems() }

  let mkCueLists () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkCueList() |]

  let mkMember () = DiscoId.Create() |> Member.create

  let mkMembers () =
    [| for _ in 0 .. rand.Next(1, 6) do
        yield mkMember () |]

  let mkSession () =
    { Id = DiscoId.Create()
      IpAddress = IPv4Address "127.0.0.1"
      UserAgent = "Oh my goodness" }

  let mkSessions () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkSession() |]

  let mkProject path =
    let machine = MachineConfig.create "127.0.0.1" None
    Project.create path (rndstr()) machine

  let mkClient () : DiscoClient =
    { Id = DiscoId.Create ()
      Name = name "Nice client"
      Role = Role.Renderer
      Status = ServiceStatus.Running
      ServiceId = DiscoId.Create()
      IpAddress = IPv4Address "127.0.0.1"
      Port = port 8921us }

  let mkClients () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkClient() |]

  let mkPlayers () =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkCuePlayer() |]

  let mkDiscoveredServices() =
    [| for n in 0 .. rand.Next(1,20) do
        yield mkDiscoveredService() |]

  let mkTrees() =
    [| for n in 0 .. rand.Next(1,10) do
        yield FsTreeTesting.makeTree (rand.Next(1,3)) (rand.Next(1,10)) |]

  let mkState path : Either<DiscoError,State> =
    either {
      let! project = mkProject path
      return
        { Project            = project
          PinGroups          = mkPinGroupMap()
          PinMappings        = mkPinMappings()        |> asMap
          PinWidgets         = mkPinWidgets()         |> asMap
          Cues               = mkCues()               |> asMap
          CueLists           = mkCueLists()           |> asMap
          Sessions           = mkSessions()           |> asMap
          Users              = mkUsers()              |> asMap
          Clients            = mkClients()            |> asMap
          CuePlayers         = mkPlayers()            |> asMap
          FsTrees            = mkTrees()              |> asMap
          DiscoveredServices = mkDiscoveredServices() |> asMap }
    }

  let mkChange _ =
    match rand.Next(0,2) with
    | n when n > 0 -> ConfigChange.MemberAdded(mkMember ())
    |          _   -> ConfigChange.MemberRemoved(mkMember ())

  let mkChanges _ =
    let n = rand.Next(1, 6)
    [| for _ in 0 .. n do
        yield mkChange () |]

  let mkLog _ : Either<DiscoError,RaftLog> =
    either {
      let! state = mkTmpDir() |> Project.ofFilePath |> mkState
      return
        LogEntry(DiscoId.Create(), index 7, term 1, DataSnapshot(state),
          Some <| LogEntry(DiscoId.Create(), index 6, term 1, DataSnapshot(state),
            Some <| Configuration(DiscoId.Create(), index 5, term 1, [| mkMember () |],
              Some <| JointConsensus(DiscoId.Create(), index 4, term 1, mkChanges (),
                Some <| Snapshot(DiscoId.Create(), index 3, term 1, index 2, term 1, mkMembers (), DataSnapshot(state))))))
        |> Log.fromEntries
    }

  let testRepo () =
    mkTmpDir ()
    |> fun path ->
      path |> unwrap |> LibGit2Sharp.Repository.Init |> ignore
      new LibGit2Sharp.Repository(unwrap path)

  let inline binaryEncDec< ^t when ^t : (member ToBytes: unit -> byte[])
                              and ^t : (static member FromBytes: byte[] -> Either<DiscoError, ^t>)
                              and ^t : equality>
                              (thing: ^t) =
    let rething: ^t = thing |> Binary.encode |> Binary.decode |> Either.get
    expect "Should be equal" thing id rething

  let inline yamlEncDec< ^i, ^t when ^t : (member ToYaml: unit -> ^i)
                                and ^t : (static member FromYaml: ^i -> Either<DiscoError, ^t>)
                                and ^t : equality>
                                (thing: ^t) =
    let rething: ^t = thing |> Yaml.encode |> Yaml.decode |> Either.get
    expect "Should be equal" thing id rething
