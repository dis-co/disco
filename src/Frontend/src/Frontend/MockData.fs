module Iris.Web.Core.MockData

open System
open System.Collections.Generic
open Fable.Core
open Fable.Import
open Fable.Core.JsInterop
open Fable.PowerPack
open Iris.Core

let loremIpsum =
    [|"Lorem ipsum dolor sit amet"
      "consectetur adipiscing elit"
      "sed do eiusmod tempor incididunt"
      "ut labore et dolore magna aliqua"
      "Ut enim ad minim veniam"
      "quis nostrud exercitation ullamco laboris"
      "nisi ut aliquip ex ea commodo consequat"
      "Duis aute irure dolor in reprehenderit"
      "in voluptate velit esse cillum dolore eu fugiat nulla pariatur"
      "Excepteur sint occaecat cupidatat non proident"
      "sunt in culpa qui officia deserunt mollit anim id est laborum"|]

let tags = [| "raft"; "iris"; "remote"; "git"; "store"; "persistence"; "frontend"; "yaml" |]
let tiers = [| Tier.FrontEnd; Tier.Client; Tier.Service |]
let levels = [| LogLevel.Debug; LogLevel.Info; LogLevel.Warn; LogLevel.Err; LogLevel.Trace |]

let rnd = Random()
let oneOf (ar: 'T[]) =
    ar.[rnd.Next(ar.Length)]

let genLog() =
    { Time = rnd.Next() |> uint32
      Thread = rnd.Next()
      Tier = oneOf tiers
      MachineId = IrisId.Create()
      Tag = oneOf tags
      LogLevel = oneOf levels
      Message = oneOf loremIpsum }

let (|IsJsArray|_|) (o: obj) =
    if JS.Array.isArray(o) then Some(o :?> ResizeArray<obj>) else None

let failParse (gid: IrisId) (pk: string) (x: obj) =
    printfn "Unexpected value %A when parsing %s in PinGroup %O" x pk gid; None

let inline forcePin<'T> gid pk (values: obj seq) =
    let values =
        values
        |> Seq.choose (function
            | IsJsArray ar when ar.Count = 2 ->
                match ar.[1] with
                | :? 'T as x -> Some x
                | x -> failParse gid pk x
            | :? 'T as x -> Some x
            | x -> failParse gid pk x)
        |> Seq.toArray
    Seq.toArray values

let makeNumberPin clientId gid pid pk values =
    let values = forcePin<double> gid pk values
    if rnd.Next() % 2 = 0
    // Using Sink makes the pin editable
    then Pin.Sink.number pid (name pk) gid clientId values |> Some
    else Pin.Source.number pid (name pk) gid clientId values |> Some

let makeTogglePin clientId gid pid pk values =
    let values = forcePin<bool> gid pk values
    // Using Sink makes the pin editable
    Pin.Sink.toggle pid (name pk) gid clientId values |> Some

let makeStringPin clientId gid pid pk values =
    let values = forcePin<string> gid pk values
    // Using Sink makes the pin editable
    Pin.Sink.string pid (name pk) gid clientId values |> Some

let makePin gid clientId pk (v: obj) =
    let pid = IrisId.Create()
    match v with
    | IsJsArray ar ->
        match Seq.tryHead ar with
        | Some (IsJsArray ar2) when ar2.Count = 2 ->
            match ar2.[1] with
            | :? float -> makeNumberPin clientId gid pid pk ar
            | :? bool -> makeTogglePin clientId gid pid pk ar
            | :? string -> makeStringPin clientId gid pid pk ar
            | _ -> failParse gid pk ar
        | Some(:? float) -> makeNumberPin clientId gid pid pk ar
        | Some(:? bool) -> makeTogglePin clientId gid pid pk ar
        | Some(:? string) -> makeStringPin clientId gid pid pk ar
        | _ -> failParse gid pk ar
    | :? float as x ->
        makeNumberPin clientId gid pid pk [|x|]
    | :? bool as x ->
        makeTogglePin clientId gid pid pk [|x|]
    | :? string as x ->
        makeStringPin clientId gid pid pk [|x|]
    | x -> failParse gid pk x

let pinGroups clientId : seq<PinGroup> =
  let pinGroups: obj = Node.Globals.require.Invoke("../data/pingroups.json")
  JS.Object.keys(pinGroups)
  |> Seq.map (fun gk ->
      let g = box pinGroups?(gk)
      let gid = IrisId.Create()
      let pins =
          JS.Object.keys(g)
          |> Seq.choose (fun pk ->
              box g?(pk) |> makePin gid clientId pk)
          |> Seq.map (fun pin -> pin.Id, pin)
          |> Map
      { Id = gid
        Name = name gk
        ClientId = clientId
        RefersTo = None
        Pins = pins
        Path = None })

let makeClient service (name: Name) : IrisClient =
  { Id = IrisId.Create()
    Name = name
    Role = Role.Renderer
    ServiceId = service
    Status = ServiceStatus.Running
    IpAddress = IpAddress.Localhost
    Port = port 5000us }

let machines =
  List.map (fun idx ->
    { MachineId    = IrisId.Create()
      HostName     = name ("mockmachine-" + string idx)
      WorkSpace    = filepath "/Iris"
      LogDirectory = filepath "/Iris"
      BindAddress  = IPv4Address "127.0.0.1"
      WebPort      = port Constants.DEFAULT_WEB_PORT
      RaftPort     = port Constants.DEFAULT_RAFT_PORT
      WsPort       = port Constants.DEFAULT_WEB_SOCKET_PORT
      GitPort      = port Constants.DEFAULT_GIT_PORT
      ApiPort      = port Constants.DEFAULT_API_PORT
      Version      = version "0.0.0" })
    [ 0 .. 3 ]

let clients =
  Seq.map
    (fun (service:IrisMachine) ->
      makeClient service.MachineId (sprintf "%A Client" service.HostName |> name))
    machines
  |> List.ofSeq

let project =
    let members =
      List.map
        (fun (machine: IrisMachine) ->
          let mem = { Iris.Raft.Member.create machine.MachineId with HostName = machine.HostName }
          (mem.Id, mem))
        machines
      |> Map.ofList
    let machine = machines.[0]
    let clusterConfig =
      { Id = IrisId.Create()
        Name = name "mockcluster"
        Members = members
        Groups = [||] }
    let irisConfig =
        { Machine    = machine
          ActiveSite = Some clusterConfig.Id
          Version   = "0.0.0"
          Audio     = AudioConfig.Default
          Clients   = ClientConfig.Default
          Raft      = RaftConfig.Default
          Timing    = TimingConfig.Default
          Sites     = [| clusterConfig |] }
    { Id        = IrisId.Create()
      Name      = name "mockproject"
      Path      = filepath "/Iris/mockproject"
      CreatedOn = Time.createTimestamp()
      LastSaved = Some (Time.createTimestamp ())
      Copyright = None
      Author    = None
      Config    = irisConfig  }

let _1of4 (x,_,_,_) = x
let _2of4 (_,x,_,_) = x
let _3of4 (_,_,x,_) = x
let _4of4 (_,_,_,x) = x

let cuesAndListsAndPlayers =
  let makeCue i =
    // Create new Cue and CueReference
    let cue = Cue.create ("Cue " + string i) [| |]
    let cueRef = CueReference.create cue
    cue, cueRef
  let cue1, cueRef1 = makeCue 1
  let cue2, cueRef2 = makeCue 2
  let cue3, cueRef3 = makeCue 3
  let cueGroup =
    CueGroup.create [|
      cueRef1
      cueRef2
      cueRef3
    |]
  let cueList =
    CueList.create "mockcuelist" [|
      cueGroup
    |]
  let cuePlayer = CuePlayer.create "mockcueplayer" (Some cueList.Id)
  let playerGroup = PinGroup.ofPlayer cuePlayer
  Map[cue1.Id, cue1; cue2.Id, cue2; cue3.Id, cue3],
  Map[cueList.Id, cueList],
  Map[cuePlayer.Id, cuePlayer],
  playerGroup

let getMockState() =
  let groups =
    clients
    |> List.collect (fun client -> pinGroups client.Id |> List.ofSeq)
    |> PinGroupMap.ofSeq
  let clients =
    clients
    |> List.map (fun client -> client.Id, client)
    |> Map.ofList
  { State.Empty with
      Project = project
      Clients = clients
      PinGroups = PinGroupMap.add (_4of4 cuesAndListsAndPlayers) groups
      Cues = _1of4 cuesAndListsAndPlayers
      CueLists = _2of4 cuesAndListsAndPlayers
      CuePlayers = _3of4 cuesAndListsAndPlayers}
