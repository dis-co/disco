module rec Disco.Web.Lib

// * Imports

// Helper methods to be used from JS

open System
open System.Collections.Generic
open Disco.Raft
open Disco.Core
open Disco.Web.Notifications
open Disco.Web.Core
open Disco.Core.Commands
open Fable.Core
open Fable.PowerPack
open Fable.PowerPack.Fetch
open Fable.Core.JsInterop
open Fable.Import

// * alert

let alert msg (_: Exception) =
  Browser.window.alert("ERROR: " + msg)

// * (&>)

/// Works like function composition but the second operand
/// is a value, and the result of the first function is ignored
let (&>) fst v =
  fun x -> fst x; v

// * postCommandPrivate

let private postCommandPrivate (ipAndPort: string option) (cmd: Command) =
  let url =
    match ipAndPort with
    | Some ipAndPort -> sprintf "http://%s%s" ipAndPort Constants.WEP_API_COMMAND
    | None -> Constants.WEP_API_COMMAND
  GlobalFetch.fetch(
    RequestInfo.Url url,
    requestProps
      [ RequestProperties.Method HttpMethod.POST
        requestHeaders [ContentType "application/json"]
        RequestProperties.Body (toJson cmd |> U3.Case3) ])

// * postCommand

let postCommand onSuccess onFail (cmd: Command) =
  postCommandPrivate None cmd
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.map onFail
    else res.text() |> Promise.map onSuccess)

// * postCommandAndBind

let postCommandAndBind onSuccess onFail (cmd: Command) =
  postCommandPrivate None cmd
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.bind onFail
    else res.text() |> Promise.bind onSuccess)

// * postCommandParseAndContinue<'T>

/// Posts a command, parses the JSON response returns a promise (can fail)
[<PassGenerics>]
let postCommandParseAndContinue<'T> (ipAndPort: string option) (cmd: Command) =
  postCommandPrivate ipAndPort cmd
  |> Promise.bind (fun res ->
    if res.Ok
    then res.text() |> Promise.map ofJson<'T>
    else res.text() |> Promise.map (failwithf "%s"))

// * postCommandWithErrorNotifier

let postCommandWithErrorNotifier defValue onSuccess cmd =
  postCommand onSuccess (fun msg -> Notifications.error msg; defValue) cmd

// * postCommandAndForget

let postCommandAndForget cmd =
  postCommand ignore Notifications.error cmd

// * listProjects

let listProjects() =
  ListProjects
  |> postCommandWithErrorNotifier [||] (ofJson<NameAndId[]> >> Array.map (fun x -> x.Name))

// * addMember

let addMember(memberIpAddr: string, memberHttpPort: uint16) =
  Promise.start (promise {
  // See workflow: https://bitbucket.org/nsynk/disco/wiki/md/workflows.md
  try
    let latestState =
      match ClientContext.Singleton.Store with
      | Some store -> store.State
      | None -> failwith "The client store is not initialized"

    let memberIpAndPort =
      sprintf "%s:%i" memberIpAddr memberHttpPort |> Some

    memberIpAndPort |> Option.iter (printfn "New member URI: %s")

    let! status =
      postCommandParseAndContinue<MachineStatus>
        memberIpAndPort
        MachineStatus

    match status with
    | Busy (_, name) -> failwithf "Host cannot be added. Busy with project %A" name
    | _ -> ()

    // Get the added machines configuration
    let! machine =
      postCommandParseAndContinue<DiscoMachine>
        memberIpAndPort
        MachineConfig

    let current = latestState.Project.Config.Machine
    if machine.MulticastAddress <> current.MulticastAddress
       || machine.MulticastPort <> current.MulticastPort
    then failwith "Host cannot be added: Multicast group mismatch"

    // List projects of member candidate (B)
    let! projects =
      postCommandParseAndContinue<NameAndId[]>
        memberIpAndPort
        ListProjects

    // If B has leader (A) active project,
    // then **pull** project from A into B
    // else **clone** active project from A into B

    let! commandMsg =
      let projectGitUri =
        match Project.localRemote latestState.Project with
        | Some uri -> uri
        | None -> failwith "Cannot get URI of project git repository"
      match projects |> Array.tryFind (fun p -> p.Id = latestState.Project.Id) with
      | Some p ->
        PullProject(string latestState.Project.Config.Machine.MachineId,
                    latestState.Project.Name,
                    projectGitUri)
      | None   -> CloneProject(latestState.Project.Name, projectGitUri)
      |> postCommandParseAndContinue<string> memberIpAndPort

    Notifications.info commandMsg

    let active =
      latestState.Project.Config.ActiveSite
      |> Option.map (fun id -> { Id = id; Name = name "<unknown>" })

    // Load active project in machine B
    // Note that we don't use loadProject from below, since that function
    // restarts the ClientContextn and thus disconnects us from the service.

    // TODO: Using the admin user for now, should it be the same user as leader A?
    let! loadResult =
      LoadProject(latestState.Project.Name, active)
      |> postCommandPrivate memberIpAndPort

    printfn "response: %A" loadResult

    // Add member B to the leader (A) cluster
    { Member.create machine.MachineId with
        IpAddress = machine.BindAddress
        RaftPort  = machine.RaftPort }
    |> AddMachine
    |> ClientContext.Singleton.Post
  with
  | exn ->
    exn.Message
    |> sprintf "Cannot add new member: %s"
    |> Notifications.error
})

// * undo

let undo () =
  AppCommand.Undo |> Command |> ClientContext.Singleton.Post

// * redo

let redo () =
  AppCommand.Redo |> Command |> ClientContext.Singleton.Post

// * shutdown

let shutdown() =
  Shutdown |> postCommand
    (fun _ -> Notifications.info "The service has been shut down")
    Notifications.error
  |> Promise.start

// * saveProject

let saveProject() =
  SaveProject |> postCommand
    (fun _ -> Notifications.success "The project has been saved")
    Notifications.error
  |> Promise.start

// * unloadProject

let unloadProject() =
  UnloadProject |> postCommand
    (fun _ ->
      Notifications.success "The project has been unloaded"
      Browser.location.reload())
    Notifications.error
  |> Promise.start

// * setLogLevel

let setLogLevel(lv) =
  LogLevel.Parse(lv) |> SetLogLevel |> ClientContext.Singleton.Post

// * nullify

let nullify _: 'a = null

// * loadProject

let loadProject(project: Name, site: NameAndId option, ipAndPort: string option): JS.Promise<string option> =
  LoadProject(project, site)
  |> postCommandPrivate ipAndPort
  |> Promise.bind (fun res ->
    if res.Ok
    then
      ClientContext.Singleton.ConnectWithWebSocket()
      |> Promise.map (fun _msg -> // TODO: Check message?
        Notifications.success "The project has been loaded successfully"
        Browser.location.reload()
        None)
    else
      res.text() |> Promise.map (fun msg ->
        if msg.Contains(ErrorMessages.PROJECT_NO_ACTIVE_CONFIG)
          || msg.Contains(ErrorMessages.PROJECT_MISSING_CLUSTER)
          || msg.Contains(ErrorMessages.PROJECT_MISSING_MEMBER)
          || msg.Contains(ErrorMessages.PROJECT_MEMBER_MISMATCH)
        then Some msg
        // We cannot deal with the error, just notify it
        else
          Notifications.error msg
          None
      )
  )

// * getProjectSites

let getProjectSites(project) =
  GetProjectSites(project) |> postCommand
    ofJson<NameAndId[]>
    (fun msg -> Notifications.error msg; [||])

// * createProject

let createProject(projectName: string): JS.Promise<Name option> = promise {
  let! (machine: DiscoMachine) = postCommandParseAndContinue None MachineConfig
  let! result =
    { name     = projectName
      ipAddr   = string machine.BindAddress
      port     = unwrap machine.RaftPort
      apiPort  = unwrap machine.ApiPort
      wsPort   = unwrap machine.WsPort
      gitPort  = unwrap machine.GitPort }
    |> CreateProject
    |> postCommand
      (fun _ ->
        Notifications.success "The project has been created successfully"
        Some (name projectName))
      (fun error ->
        Notifications.error error
        None)
  return result
}

// * postStateCommands

let postStateCommands (cmds: StateMachine list) =
  if not (List.isEmpty cmds) then
    cmds
    |> CommandBatch.ofList
    |> ClientContext.Singleton.Post

// * updatePinValue

let updatePinValue(pin: Pin, index: int, value: obj) =
  let tryUpdateArray (i: int) (v: obj) (ar: 'T[]) =
    if i >= 0 && i < ar.Length && box ar.[i] <> v then
      let newArray = Array.copy ar
      newArray.[i] <- unbox v
      Some newArray
    else None
  let client = if Pin.isPreset pin then Some pin.ClientId else None
  match pin with
  | StringPin pin ->
    tryUpdateArray index value pin.Values
    |> Option.map (fun values -> StringSlices(pin.Id, client, values))
  | NumberPin pin ->
    let value =
      match value with
      | :? string as v -> box(double v)
      | v -> v
    tryUpdateArray index value pin.Values
    |> Option.map (fun values -> NumberSlices(pin.Id, client, values))
  | BoolPin pin ->
    let value =
      match value with
      | :? string as v -> box(v.ToLower() = "true")
      | v -> v
    tryUpdateArray index value pin.Values
    |> Option.map (fun values -> BoolSlices(pin.Id, client, pin.IsTrigger, values))
  | EnumPin pin ->
    let prop =
      Array.tryPick
        (fun prop -> if prop.Key = unbox value then Some prop else None)
        pin.Properties
    match prop with
    | Some value ->
      tryUpdateArray index value pin.Values
      |> Option.map (fun values -> EnumSlices(pin.Id, client, values))
    | _ -> None
  | ColorPin pin ->
    match ColorSpace.TryParse(unbox value) with
    | Right color ->
      tryUpdateArray index color pin.Values
      |> Option.map (fun values -> ColorSlices(pin.Id, client, values))
    | _ -> None
  | BytePin   _pin -> failwith "TO BE IMPLEMENTED: Update byte pins"
  |> Option.iter (UpdateSlices.ofSlices >> ClientContext.Singleton.Post)

// * findPin

let findPin (pinId: PinId) (state: State) : Pin =
  let groups = state.PinGroups |> PinGroupMap.unifiedPins |> PinGroupMap.byGroup
  match Map.tryFindPin pinId groups with
  | Some pin -> pin
  | None ->
    // failwithf "Cannot find pin with Id %O in GlobalState" pinId
    // Placeholder pin
    let emptyId = DiscoId.FromGuid(Guid.Empty)
    Pin.Sink.string pinId (name "MISSING") emptyId emptyId [|""|]

// * findPinGroup

let findPinGroup (pinGroupId: PinGroupId) (state: State) =
  let groups = state.PinGroups |> PinGroupMap.unifiedPins |> PinGroupMap.byGroup
  match Map.tryFind pinGroupId groups with
  | Some pinGroup -> pinGroup
  | None ->
    // failwithf "Cannot find pin group with Id %O in GlobalState" pinGroupId
    // Placeholder pin group
    let emptyId = DiscoId.FromGuid(Guid.Empty)
    { Id = emptyId
      Name = name "MISSING"
      ClientId = emptyId
      RefersTo = None
      Pins = Map.empty
      Path = None }

// * isMissingPin

let isMissingPin (pin: Pin) =
  pin.PinGroupId.Guid = Guid.Empty

// * findCue

let findCue (cueId: CueId) (state: State) =
  match Map.tryFind cueId state.Cues with
  | Some cue -> cue
  | None -> failwithf "Cannot find cue with Id %O in GlobalState" cueId

// * groupCreateCue

let groupCreateCue (cueList:CueList) (cueGroupIndex:int) (cueIndex:int) =
  if cueList.Items.Length = 0 then
    failwith "A Cue Group must be added first"
  // Create new Cue and CueReference
  let newCue = Cue.create "Untitled" [| |]

  // create a reference to the constructed cue
  let newCueRef = CueReference.ofCue newCue

  // Insert new CueRef in the selected CueGroup after the selected cue
  let cueGroup =
    let idx = max cueGroupIndex 0
    cueList.Items.[idx]

  let idx = if cueIndex < 0 then cueGroup.CueRefs.Length - 1 else cueIndex

  /// Update CueGroup by adding the created CueReference to it
  let newCueGroup = CueGroup.insertAfter idx newCueRef cueGroup

  // Update the CueList
  let newCueList = CueList.replace newCueGroup cueList

  // Send messages to backend
  [AddCue newCue; UpdateCueList newCueList]
  |> postStateCommands

// * groupAddCues

let groupAddCues
  (selected:CueId[])
  (cues:Cue[])
  (cueList:CueList)
  (cueGroupIndex:int)
  (cueIndex:int) =

  // Insert new CueRef in the selected CueGroup after the selected cue
  let cueGroup =
    let idx = max cueGroupIndex 0
    cueList.Items.[idx]

  let idx = if cueIndex < 0 then cueGroup.CueRefs.Length - 1 else cueIndex

  cues
  |> Array.filter (fun cue -> Array.contains (Cue.id cue) selected)
  |> Array.map CueReference.ofCue
  |> Array.rev
  |> Array.fold (fun group cueRef -> CueGroup.insertAfter idx cueRef group) cueGroup
  |> flip CueList.replace cueList
  |> UpdateCueList
  |> ClientContext.Singleton.Post

// * createCue

let createCue (title:string) (pins: Pin list) =
  pins
  |> List.fold (fun map pin -> pin |> Pin.slices |> SlicesMap.add map) SlicesMap.empty
  |> SlicesMap.toArray
  |> Cue.create title
  |> AddCue
  |> ClientContext.Singleton.Post

// * updateCues

let updateCues (selected:CueId[]) (pins: Pin list) (cues:Cue[]) =
  let slices = List.map Pin.slices pins
  cues
  |> Array.filter (fun cue -> Array.contains (Cue.id cue) selected)
  |> Array.map (fun cue -> List.fold (flip Cue.addSlices) cue slices |> UpdateCue)
  |> Array.toList
  |> postStateCommands

// * duplicateCue

let duplicateCue (state:State) (cueList:CueList) (cueGroupIndex:int) (cueIndex:int) =
  try
    let cueGroup =
      let idx = max cueGroupIndex 0
      cueList.Items.[idx]

    let cueRef =  cueGroup.CueRefs.[cueIndex]

    // Find selected Cue and duplicate it
    match State.cue (CueReference.cueId cueRef) state with
    | None -> ()
    | Some cue ->
      let newCue = Cue.duplicate cue

      // create a reference to the constructed cue
      let newCueRef = CueReference.ofCue newCue

      let idx = if cueIndex < 0 then cueGroup.CueRefs.Length - 1 else cueIndex

      /// Update CueGroup by adding the created CueReference to it
      let newCueGroup = CueGroup.insertAfter idx newCueRef cueGroup

      // Update the CueList
      let newCueList = CueList.replace newCueGroup cueList

      // Send messages to backend
      [AddCue newCue; UpdateCueList newCueList]
      |> postStateCommands
  with _ -> ()

// * resetDirty

let resetDirty (state:State) =
  state
  |> State.pinGroupMap
  |> PinGroupMap.dirtyPins
  |> PinGroupMap.toList
  |> List.collect (PinGroup.pins >> Map.toList >> List.map (snd >> Pin.setDirty false >> UpdatePin))
  |> CommandBatch.ofList
  |> ClientContext.Singleton.Post

// * persistAll

let persistAll (state: State) =
  state
  |> State.pinGroupMap
  |> PinGroupMap.unpersistedPins
  |> PinGroupMap.toList
  |> List.collect (PinGroup.pins >> Map.toList >> List.map (snd >> Pin.setPersisted true >> UpdatePin))
  |> CommandBatch.ofList
  |> ClientContext.Singleton.Post

// * persistPins

let persistPins (pins: Pin list) (state:State) =
  pins
  |> List.map (Pin.setPersisted true >> UpdatePin)
  |> CommandBatch.ofList
  |> ClientContext.Singleton.Post

// * addSlicesToCue

/// Returns the list of state machine commands to add the slices to the cue
let addSlicesToCue (cue: Cue) (pins: Pin seq) =
  // Filter out output pins and pins already contained by the cue
  let persistPins, updatedCue =
    Seq.fold
      (fun (persistedPins, cue) pin ->
        if Pin.isSource pin
        then persistedPins, cue
        else
          let cue = Cue.addSlices pin.Slices cue
          match pin.Persisted with
          // the pin already is persisted, do nothing
          | true  -> persistedPins, cue
          | false -> pin :: persistedPins,cue)
      (List.empty, cue)
      pins
  let cueUpdate = UpdateCue updatedCue
  if List.isEmpty persistPins then
    [cueUpdate]
  else
    let pinUpdates = List.map (Pin.setPersisted true >> UpdatePin) persistPins
    cueUpdate :: pinUpdates

// * removeSlicesFromCue

/// Returns the state machine command to remove the slices from the cue
let removeSlicesFromCue (cue: Cue) (pinIds: PinId seq) =
  // Create a set for faster comparison
  let pinIds = set pinIds
  cue.Slices |> Array.filter (fun slices ->
    Set.contains slices.PinId pinIds |> not)
  |> flip Cue.setSlices cue
  |> UpdateCue

// * mayAlterCue

let mayAlterCue (state:State) (cue:Cue) =
  Map.fold
    (fun forbidden _ player ->
      if not forbidden
      then
        if CuePlayer.locked player then
          player
          |> CuePlayer.cueListId
          |> Option.bind (flip Map.tryFind state.CueLists)
          |> Option.map (CueList.contains cue.Id)
          |> Option.defaultValue false
        else false
      else forbidden)
    false
    state.CuePlayers
  |> not

// * toggleInspector

let toggleInspector () =
  !!jQuery("#ui-layout-container")?layout()?toggle("east")

// * workspaceDimensions

let workspaceDimensions (): int * int =
  let len:int = unbox (!!jQuery("#ui-layout-container")?length)
  if len > 0                            /// check if the element exists
  then
    let state = !!jQuery("#ui-layout-container")?layout()?center?state
    unbox state?innerWidth, unbox state?innerHeight
  else 0, 0
