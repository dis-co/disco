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
      Id = Guid.NewGuid() |> string |> Id
      Tag = oneOf tags
      LogLevel = oneOf levels
      Message = oneOf loremIpsum }

let (|IsJsArray|_|) (o: obj) =
    if JS.Array.isArray(o) then Some(o :?> ResizeArray<obj>) else None

let failParse (gid: Id) (pk: string) (x: obj) =
    printfn "Unexpected value %A when parsing %s in PinGroup %O" x pk gid; None

let inline forcePin<'T> gid pk (values: obj seq) =
    let labels = ResizeArray()
    let values =
        values
        |> Seq.choose (function
            | IsJsArray ar when ar.Count = 2 ->
                match ar.[1] with
                | :? 'T as x -> labels.Add(string ar.[0] |> astag); Some x
                | x -> failParse gid pk x
            | :? 'T as x -> Some x
            | x -> failParse gid pk x)
        |> Seq.toArray
    Seq.toArray labels, values

let makeNumberPin gid pid pk values =
    let labels, values = forcePin<float> gid pk values
    Pin.number pid (name pk) gid labels values |> Some

let makeTogglePin gid pid pk values =
    let labels, values = forcePin<bool> gid pk values
    Pin.toggle pid (name pk) gid labels values |> Some

let makeStringPin gid pid pk values =
    let labels, values = forcePin<string> gid pk values
    Pin.string pid (name pk) gid labels values |> Some

let makePin gid pk (v: obj) =
    let pid = Id (sprintf "%O::%s" gid pk)
    match v with
    | IsJsArray ar ->
        match Seq.tryHead ar with
        | Some (IsJsArray ar2) when ar2.Count = 2 ->
            match ar2.[1] with
            | :? float -> makeNumberPin gid pid pk ar
            | :? bool -> makeTogglePin gid pid pk ar
            | :? string -> makeStringPin gid pid pk ar
            | _ -> failParse gid pk ar
        | Some(:? float) -> makeNumberPin gid pid pk ar
        | Some(:? bool) -> makeTogglePin gid pid pk ar
        | Some(:? string) -> makeStringPin gid pid pk ar
        | _ -> failParse gid pk ar
    | :? float as x ->
        Pin.number pid (name pk) gid [||] [|x|] |> Some
    | :? bool as x ->
        Pin.toggle pid (name pk) gid [||] [|x|] |> Some
    | :? string as x ->
        Pin.string pid (name pk) gid [||] [|x|] |> Some
    | x -> failParse gid pk x

let pinGroups: Map<Id, PinGroup> =
    let pinGroups: obj = Node.Globals.require.Invoke("../data/pingroups.json")
    JS.Object.keys(pinGroups)
    |> Seq.map (fun gk ->
        let g = box pinGroups?(gk)
        let gid = Id gk
        let pins =
            JS.Object.keys(g)
            |> Seq.choose (fun pk ->
                box g?(pk) |> makePin gid pk)
            |> Seq.map (fun pin -> pin.Id, pin)
            |> Map
        let pinGroup =
            { Id = gid
              Name = name gk
              Client = Id "mockupclient"
              Pins = pins
              Path = None }
        pinGroup.Id, pinGroup)
    |> Map

let project =
    let memb =
        let memb = Id.Create() |> Iris.Raft.Member.create
        { memb with HostName = name "Wilhelm" }
    let clusterConfig =
      { Id = Id.Create()
        Name = name "mockcluster"
        Members = Map[memb.Id, memb]
        Groups = [||] }
    let machine =
      { MachineId    = Id.Create()
        HostName     = name "mockmachine"
        WorkSpace    = filepath "/Iris"
        LogDirectory = filepath "/Iris"
        BindAddress  = IPv4Address "127.0.0.1"
        WebPort      = port Constants.DEFAULT_WEB_PORT
        RaftPort     = port Constants.DEFAULT_RAFT_PORT
        WsPort       = port Constants.DEFAULT_WEB_SOCKET_PORT
        GitPort      = port Constants.DEFAULT_GIT_PORT
        ApiPort      = port Constants.DEFAULT_API_PORT
        Version      = version "0.0.0" }
    let irisConfig =
        { Machine    = machine
          ActiveSite = Some clusterConfig.Id
          Version   = "0.0.0"
          Vvvv      = VvvvConfig.Default
          Audio     = AudioConfig.Default
          Raft      = RaftConfig.Default
          Timing    = TimingConfig.Default
          ViewPorts = [| |]
          Displays  = [| |]
          Tasks     = [| |]
          Sites     = [|clusterConfig|] }
    { Id        = Id.Create()
      Name      = name "mockproject"
      Path      = filepath "/Iris/mockproject"
      CreatedOn = Time.createTimestamp()
      LastSaved = Some (Time.createTimestamp ())
      Copyright = None
      Author    = None
      Config    = irisConfig  }

let _1of3 (x,_,_) = x
let _2of3 (_,x,_) = x
let _3of3 (_,_,x) = x

let cuesAndListsAndPlayers =
    let makeCue() =
        // Create new Cue and CueReference
        let cue = { Id = Id.Create(); Name = name "Untitled"; Slices = [||] }
        let cueRef = { Id = Id.Create(); CueId = cue.Id; AutoFollow = -1; Duration = -1; Prewait = -1 }
        cue, cueRef
    let cue1, cueRef1 = makeCue()
    let cue2, cueRef2 = makeCue()
    let cue3, cueRef3 = makeCue()
    let cueGroup = { Id = Id "mockcuegroup"; Name = name "mockcuegroup"; CueRefs = [|cueRef1; cueRef2; cueRef3|] }
    let cueList = { Id=Id "mockcuelist"; Name=name "mockcuelist"; Groups=[|cueGroup|]}
    let cuePlayer = CuePlayer.create (name "mockcueplayer") (Some cueList.Id)
    Map[cue1.Id, cue1; cue2.Id, cue2; cue3.Id, cue3],
    Map[cueList.Id, cueList],
    Map[cuePlayer.Id, cuePlayer]

let getMockState() =
    { State.Empty with
        Project = project
        PinGroups = pinGroups
        Cues = _1of3 cuesAndListsAndPlayers
        CueLists = _2of3 cuesAndListsAndPlayers
        CuePlayers = _3of3 cuesAndListsAndPlayers }
