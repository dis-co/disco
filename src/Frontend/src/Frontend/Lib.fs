module rec Iris.Web.Lib

// Helper methods to be used from JS

open System
open System.Collections.Generic
open Iris.Raft
open Iris.Core
open Iris.Web.Notifications
open Iris.Web.Core
open Iris.Core.Commands
open Fable.Core
open Fable.PowerPack
open Fable.PowerPack.Fetch
open Fable.Core.JsInterop
open Fable.Import

let inline replaceById< ^t when ^t : (member Id : IrisId)> (newItem : ^t) (ar: ^t[]) =
  Array.map (fun (x: ^t) -> if (^t : (member Id : IrisId) newItem) = (^t : (member Id : IrisId) x) then newItem else x) ar

let insertAfter (i: int) (x: 't) (xs: 't[]) =
  let len = xs.Length
  if len = 0 (* && i = 0 *) then
    [|x|]
  elif i >= len then
    failwith "Index out of array bounds"
  elif i < 0 then
    Array.append [|x|] xs
  elif i = (len - 1) then
    Array.append xs [|x|]
  else
    let xs2 = Array.zeroCreate<'t> (len + 1)
    for j = 0 to len do
      if j <= i then
        xs2.[j] <- xs.[j]
      elif j = (i + 1) then
        xs2.[j] <- x
      else
        xs2.[j] <- xs.[j - 1]
    xs2

let alert msg (_: Exception) =
  Browser.window.alert("ERROR: " + msg)

/// Works like function composition but the second operand
/// is a value, and the result of the first function is ignored
let (&>) fst v =
  fun x -> fst x; v

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

let postCommand onSuccess onFail (cmd: Command) =
  postCommandPrivate None cmd
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.map onFail
    else res.text() |> Promise.map onSuccess)

let postCommandAndBind onSuccess onFail (cmd: Command) =
  postCommandPrivate None cmd
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.bind onFail
    else res.text() |> Promise.bind onSuccess)

/// Posts a command, parses the JSON response returns a promise (can fail)
[<PassGenerics>]
let postCommandParseAndContinue<'T> (ipAndPort: string option) (cmd: Command) =
  postCommandPrivate ipAndPort cmd
  |> Promise.bind (fun res ->
    if res.Ok
    then res.text() |> Promise.map ofJson<'T>
    else res.text() |> Promise.map (failwithf "%s"))

let postCommandWithErrorNotifier defValue onSuccess cmd =
  postCommand onSuccess (fun msg -> Notifications.error msg; defValue) cmd

let postCommandAndForget cmd =
  postCommand ignore Notifications.error cmd

let listProjects() =
  ListProjects
  |> postCommandWithErrorNotifier [||] (ofJson<NameAndId[]> >> Array.map (fun x -> x.Name))

let addMember(memberIpAddr: string, memberHttpPort: uint16) =
  Promise.start (promise {
  // See workflow: https://bitbucket.org/nsynk/iris/wiki/md/workflows.md
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
      postCommandParseAndContinue<IrisMachine>
        memberIpAndPort
        MachineConfig

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
      | Some p -> PullProject(p.Id, latestState.Project.Name, projectGitUri)
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
      LoadProject(latestState.Project.Name, name "admin", password "Nsynk", active)
      |> postCommandPrivate memberIpAndPort

    printfn "response: %A" loadResult

    // Add member B to the leader (A) cluster
    { Member.create machine.MachineId with
        HostName = machine.HostName
        IpAddr   = machine.BindAddress
        Port     = machine.RaftPort
        WsPort   = machine.WsPort
        GitPort  = machine.GitPort
        ApiPort  = machine.ApiPort }
    |> AddMember
    |> ClientContext.Singleton.Post // TODO: Check the state machine post has been successful
  with
  | exn ->
    exn.Message
    |> sprintf "Cannot add new member: %s"
    |> Notifications.error
})

let shutdown() =
  Shutdown |> postCommand
    (fun _ -> Notifications.info "The service has been shut down")
    Notifications.error

let saveProject() =
  SaveProject |> postCommand
    (fun _ -> Notifications.success "The project has been saved")
    Notifications.error

let unloadProject() =
  UnloadProject |> postCommand
    (fun _ ->
      Notifications.success "The project has been unloaded"
      Browser.location.reload())
    Notifications.error

let setLogLevel(lv) =
  LogLevel.Parse(lv) |> SetLogLevel |> ClientContext.Singleton.Post

let nullify _: 'a = null

let loadProject(project: Name, username: UserName, pass: Password, site: NameAndId option, ipAndPort: string option): JS.Promise<string option> =
  LoadProject(project, username, pass, site)
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

let getProjectSites(project, username, password) =
  GetProjectSites(project, username, password) |> postCommand
    ofJson<NameAndId[]>
    (fun msg -> Notifications.error msg; [||])

let createProject(projectName: string): JS.Promise<Name option> = promise {
  let! (machine: IrisMachine) = postCommandParseAndContinue None MachineConfig
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
    |> Option.map (fun values -> BoolSlices(pin.Id, client, values))
  | BytePin   _pin -> failwith "TO BE IMPLEMENTED: Update byte pins"
  | EnumPin   _pin -> failwith "TO BE IMPLEMENTED: Update enum pins"
  | ColorPin  _pin -> failwith "TO BE IMPLEMENTED: Update color pins"
  |> Option.iter (UpdateSlices.ofSlices >> ClientContext.Singleton.Post)

let findPin (pinId: PinId) (state: State) : Pin =
  let groups = state.PinGroups |> PinGroupMap.unifiedPins |> PinGroupMap.byGroup
  match Map.tryFindPin pinId groups with
  | Some pin -> pin
  | None ->
    // failwithf "Cannot find pin with Id %O in GlobalState" pinId
    // Placeholder pin
    let emptyId = IrisId.FromGuid(Guid.Empty)
    Pin.Sink.string pinId (name "MISSING") emptyId emptyId [|""|]

let findPinGroup (pinGroupId: PinGroupId) (state: State) =
  let groups = state.PinGroups |> PinGroupMap.unifiedPins |> PinGroupMap.byGroup
  match Map.tryFind pinGroupId groups with
  | Some pinGroup -> pinGroup
  | None ->
    // failwithf "Cannot find pin group with Id %O in GlobalState" pinGroupId
    // Placeholder pin group
    let emptyId = IrisId.FromGuid(Guid.Empty)
    { Id = emptyId
      Name = name "MISSING"
      ClientId = emptyId
      RefersTo = None
      Pins = Map.empty
      Path = None }

let isMissingPin (pin: Pin) =
  pin.PinGroupId.Guid = Guid.Empty

let isOutputPin (pin: Pin) =
  match pin.PinConfiguration with
  | PinConfiguration.Preset | PinConfiguration.Sink -> false
  | PinConfiguration.Source -> true

let findCue (cueId: CueId) (state: State) =
  match Map.tryFind cueId state.Cues with
  | Some cue -> cue
  | None -> failwithf "Cannot find cue with Id %O in GlobalState" cueId

let addCue (cueList:CueList) (cueGroupIndex:int) (cueIndex:int) =
  if cueList.Items.Length = 0 then
    failwith "A Cue Group must be added first"
  // Create new Cue and CueReference
  let newCue = {
    Id = IrisId.Create()
    Name = name "Untitled"
    Slices = [||]
  }
  // create a reference to the constructed cue
  let newCueRef = {
    Id = IrisId.Create()
    CueId = newCue.Id
    AutoFollow = -1
    Duration = -1
    Prewait = -1
  }
  // Insert new CueRef in the selected CueGroup after the selected cue
  let cueGroup =
    let idx = max cueGroupIndex 0
    match cueList.Items.[idx] with
    | CueGroup group -> group
    | _ -> failwithf "No group found at index: %d" idx

  let idx = if cueIndex < 0 then cueGroup.CueRefs.Length - 1 else cueIndex

  let newCueGroup = {
    cueGroup with CueRefs = insertAfter idx newCueRef cueGroup.CueRefs
  }

  // Update the CueList
  let newCueList = CueList.replace (CueGroup newCueGroup) cueList

  // Send messages to backend
  CommandBatch.ofList [
    AddCue newCue
    UpdateCueList newCueList
  ]
  |> ClientContext.Singleton.Post

let addSlicesToCue (cue: Cue) (pins: Pin list) =
  // Filter out output pins and pins already contained by the cue
  let persistPins, updatedCue =
    Seq.fold
      (fun (persistedPins, cue) pin ->
        if isOutputPin pin || Cue.contains pin.Id cue
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
    ClientContext.Singleton.Post cueUpdate
  else
    let pinUpdates = List.map (Pin.setPersisted true >> UpdatePin) persistPins
    cueUpdate :: pinUpdates
    |> CommandBatch.ofList
    |> ClientContext.Singleton.Post
