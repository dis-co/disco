module rec Iris.Web.Lib

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open System
open System.Collections.Generic
open Iris.Raft
open Iris.Core
open Iris.Web.Core
open Iris.Core.Commands
open Fable.Core
open Fable.PowerPack
open Fable.PowerPack.Fetch
open Fable.Core.JsInterop
open Fable.Import

// TYPES -----------------------------------------------------
type GenericObservable<'T>() =
    let listeners = Dictionary<Guid,IObserver<'T>>()
    member x.Trigger v =
      for lis in listeners.Values do
        lis.OnNext v
    interface IObservable<'T> with
      member x.Subscribe w =
        let guid = Guid.NewGuid()
        listeners.Add(guid, w)
        { new IDisposable with
          member x.Dispose() = listeners.Remove(guid) |> ignore }

[<NoComparison>]
type DragEvent = {
  ``type``: string; value: obj; x: int; y: int;
}

type [<Pojo>] TreeNode =
  { ``module``: string; children: TreeNode[] option }


// VALUES ----------------------------------------------------
let EMPTY = Constants.EMPTY

// HELPERS ----------------------------------------------------
let toString (x: obj) = string x

let getClientContext() =
    ClientContext.Singleton

let private dragObservable =
    GenericObservable<DragEvent>()

let subscribeToDrags (f: DragEvent->unit) =
  Observable.subscribe f dragObservable

let triggerDragEvent(typ: string, value: obj, x: int, y: int) =
  { ``type`` = typ; value = value; x = x; y = y}
  |> dragObservable.Trigger

let notify(msg: string) =
  Browser.console.log(msg)

  match !!Browser.window?Notification with
  // Check if the browser supports notifications
  | null -> Browser.console.log msg

  // Check whether notification permissions have already been granted
  | notify when !!notify?permission = "granted" ->
    !!createNew notify msg

  // Ask the user for permission
  | notify when !!notify?permission <> "denied" ->
    !!notify?requestPermission(function
        | "granted" -> !!createNew notify msg
        | _ -> ())
  | _ -> ()

let subscribeToLogs(f:ClientLog->unit): IDisposable =
    ClientContext.Singleton.OnMessage.Subscribe (function
      | ClientMessage.ClientLog log -> f log
      | _ -> ())

let subscribeToClock(f:uint32->unit): IDisposable =
    ClientContext.Singleton.OnMessage.Subscribe (function
      | ClientMessage.ClockUpdate frames -> f frames
      | _ -> ())

let removeMember(config: IrisConfig, memId: Id) =
  match Config.findMember config memId with
  | Right mem ->
    RemoveMember mem
    |> ClientContext.Singleton.Post
  | Left error ->
    printfn "%O" error

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
let inline postCommandParseAndContinue<'T> (ipAndPort: string option) (cmd: Command) =
  postCommandPrivate ipAndPort cmd
  |> Promise.bind (fun res ->
    printfn "cmd: %A res: %A" cmd res.Ok
    if res.Ok
    then res.text() |> Promise.map ofJson<'T>
    else res.text() |> Promise.map (failwithf "%s"))

let postCommandWithErrorNotifier defValue onSuccess cmd =
  postCommand onSuccess (fun msg -> notify msg; defValue) cmd

let postCommandAndForget cmd =
  postCommand ignore notify cmd

let listProjects() =
  ListProjects
  |> postCommandWithErrorNotifier [||] (ofJson<NameAndId[]> >> Array.map (fun x -> x.Name))

let addMember(info: obj) =
  Promise.start (promise {
  // See workflow: https://bitbucket.org/nsynk/iris/wiki/md/workflows.md
  try
    let latestState = ClientContext.Singleton.LatestState

    let memberIpAndPort =
      let memberIpAddr: string = !!info?ipAddr
      let memberHttpPort: uint16 = !!info?httpPort
      sprintf "%s:%i" memberIpAddr memberHttpPort |> Some

    memberIpAndPort |> Option.iter (printfn "New member URI: %s")

    let! (status: MachineStatus) = postCommandParseAndContinue memberIpAndPort MachineStatus
    printfn "New member status: %A" status

    match status with
    | Busy (_, name) -> failwithf "Host cannot be added. Busy with project %A" name
    | _ -> ()

    // Get the added machines configuration
    let! (machine: IrisMachine) = postCommandParseAndContinue memberIpAndPort MachineConfig

    printfn "Member config details: %A" machine

    // List projects of member candidate (B)
    let! (projects: NameAndId[]) = postCommandParseAndContinue memberIpAndPort ListProjects

    printfn "New member projects: %A" projects

    // If B has leader (A) active project,
    // then **pull** project from A into B
    // else **clone** active project from A into B

    let! commandMsg =
      let projectGitUri =
        match Project.localRemote latestState.Project with
        | Some uri -> uri
        | None -> failwith "Cannot get URI of project git repository"
      match projects |> Array.tryFind (fun p -> p.Id = latestState.Project.Id) with
      | Some p -> PullProject(string p.Id, unwrap latestState.Project.Name, projectGitUri)
      | None   -> CloneProject(unwrap latestState.Project.Name, projectGitUri)
      |> postCommandParseAndContinue<string> memberIpAndPort

    notify commandMsg

    let active =
      latestState.Project.Config.ActiveSite
      |> Option.map string

    // Load active project in machine B
    // TODO: Using the admin user for now, should it be the same user as leader A?
    let! errMsg = loadProject(unwrap latestState.Project.Name, "admin", "Nsynk", active, memberIpAndPort)
    errMsg |> Option.iter (failwith "Error when loading project in member: %s")

    // Add member B to the leader (A) cluster
    { Member.create machine.MachineId with
        HostName = machine.HostName
        IpAddr   = IPv4Address machine.WebIP
        Port     = machine.RaftPort
        WsPort   = machine.WsPort
        GitPort  = machine.GitPort
        ApiPort  = machine.ApiPort }
    |> AddMember
    // TODO: Check the state machine post has been successful
    |> ClientContext.Singleton.Post
  with
  | exn ->
    sprintf "Cannot add new member: %s" exn.Message |> notify
})

let shutdown() =
  Shutdown |> postCommand (fun _ -> notify "The service has been shut down") notify

let unloadProject() =
  UnloadProject |> postCommand (fun _ -> notify "The project has been unloaded") notify

let nullify _: 'a = null

let rec loadProject(project: string, username: string, pass: string, site: string option, ipAndPort: string option): JS.Promise<string option> =
  LoadProject(project, username, password pass, site)
  |> postCommandPrivate ipAndPort
  |> Promise.bind (fun res ->
    if res.Ok
    then
      ClientContext.Singleton.ConnectWithWebSocket()
      |> Promise.map (fun _msg -> // TODO: Check message?
        notify "The project has been loaded successfully"; None)
    else
      res.text() |> Promise.map (fun msg ->
        if msg.Contains(ErrorMessages.PROJECT_NO_ACTIVE_CONFIG)
          || msg.Contains(ErrorMessages.PROJECT_MISSING_CLUSTER)
          || msg.Contains(ErrorMessages.PROJECT_MISSING_MEMBER)
        then Some msg
        // We cannot deal with the error, just notify it
        else notify msg; None
      )
  )

let getProjectSites(project, username, password) =
  GetProjectSites(project, username, password)
  |> postCommand ofJson<string[]> (fun msg -> notify msg; [||])

let createProject(info: obj) =
  Promise.start (promise {
    let! (machine: IrisMachine) = postCommandParseAndContinue None MachineConfig

    do! { name     = !!info?name
        ; ipAddr   = machine.WebIP
        ; port     = machine.RaftPort
        ; apiPort  = machine.ApiPort
        ; wsPort   = machine.WsPort
        ; gitPort  = machine.GitPort }
        |> CreateProject
        |> postCommand (fun _ -> notify "The project has been created successfully") notify
  })

let project2tree (p: IrisProject) =
  let leaf m = { ``module``=m; children=None }
  let node m c = { ``module``=m; children=Some c }
  let rec obj2tree k (o: obj) =
    Fable.Import.JS.Object.getOwnPropertyNames(o)
    |> Seq.map (fun k ->
    match box o?(k) with
      | :? (obj[]) as arr ->
        arr2tree k arr
      | :? IDictionary<obj, obj> as dic ->
        dic |> Seq.map (fun kv -> obj2tree (string kv.Key) kv.Value)
        |> Seq.toArray |> node k
      | v -> sprintf "%s: %O" k v |> leaf)
    |> Seq.toArray
    |> node k
  and arr2tree k (arr: obj[]) =
    Array.mapi (fun i v -> obj2tree (string i) v) arr
    |> node k
  let cfg2tree (c: IrisConfig) =
    [| leaf ("MachineId: " + string c.Machine.MachineId)
    ;  obj2tree "Audio" c.Audio
    ;  obj2tree "Vvvv" c.Vvvv
    ;  obj2tree "Raft" c.Raft
    ;  obj2tree "Timing" c.Timing
    ;  leaf ("ActiveSite" + string c.ActiveSite)
    ;  arr2tree "Sites" (Array.map box c.Sites)
    ;  arr2tree "ViewPorts" (Array.map box c.ViewPorts)
    ;  arr2tree "Displays" (Array.map box c.Displays)
    ;  arr2tree "Tasks" (Array.map box c.Tasks)
    |] |> node "Config"
  [| leaf ("Id: " + string p.Id)
  ;  leaf ("Name: " + unwrap p.Name)
  ;  leaf ("Path: " + unwrap p.Path)
  ;  leaf ("CreatedOn: " + p.CreatedOn)
  ;  leaf ("LastSaved: " + defaultArg p.LastSaved "unknown")
  ;  leaf ("Copyright: " + defaultArg p.Copyright "unknown")
  ;  leaf ("Author: " + defaultArg p.Author "unknown")
  ;  cfg2tree p.Config
  |] |> node "Project"

let startContext f =
  let context = ClientContext.Singleton
  context.Start()
  |> Promise.map (fun () ->
    context.OnMessage
    |> Observable.add (function
      | ClientMessage.Render(Some state) ->
        match Map.tryFind context.Session state.Sessions with
        | Some session ->
          Some { session = session; state = state } |> f
        | None -> ()
      | ClientMessage.Render None ->
          f None
      | _ -> ())
  )

let startWorkerContext() =
  GlobalContext()

let pinToKeyValuePairs (pin: Pin) =
  let zip labels values =
    let labels =
      if Array.length labels = Array.length values
      then labels
      else Array.replicate values.Length ""
    Array.zip labels values
  match pin with
  | StringPin pin -> Array.map box pin.Values |> zip pin.Labels
  | NumberPin pin -> Array.map box pin.Values |> zip pin.Labels
  | BoolPin   pin -> Array.map box pin.Values |> zip pin.Labels
  // TODO: Apply transformations to the value of this pins?
  | BytePin   pin -> Array.map box pin.Values |> zip pin.Labels
  | EnumPin   pin -> Array.map box pin.Values |> zip pin.Labels
  | ColorPin  pin -> Array.map box pin.Values |> zip pin.Labels

let updateSlices(pin: Pin, rowIndex, newValue: obj) =
  let updateArray (i: int) (v: obj) (ar: 'T[]) =
    let newArray = Array.copy ar
    newArray.[i] <- unbox v
    newArray
  match pin with
  | StringPin pin ->
    StringSlices(pin.Id, updateArray rowIndex newValue pin.Values)
  | NumberPin pin ->
    let newValue =
      match newValue with
      | :? string as v -> box(double v)
      | v -> v
    NumberSlices(pin.Id, updateArray rowIndex newValue pin.Values)
  | BoolPin pin ->
    let newValue =
      match newValue with
      | :? string as v -> box(v.ToLower() = "true")
      | v -> v
    BoolSlices(pin.Id, updateArray rowIndex newValue pin.Values)
  | BytePin   _pin -> failwith "TO BE IMPLEMENTED"
  | EnumPin   _pin -> failwith "TO BE IMPLEMENTED"
  | ColorPin  _pin -> failwith "TO BE IMPLEMENTED"
  |> UpdateSlices |> ClientContext.Singleton.Post
