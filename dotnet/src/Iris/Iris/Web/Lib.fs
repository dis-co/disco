module Iris.Web.Lib

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
open Fable.PowerPack.Fetch.Fetch_types
open Fable.Core.JsInterop
open Fable.Import
open Iris.Core.Discovery

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

let removeMember(info: StateInfo, memId: Id) =
  match Config.findMember info.state.Project.Config memId with
  | Right mem ->
    RemoveMember mem
    |> ClientContext.Singleton.Post
  | Left error ->
    printfn "%O" error

let createMemberInfo() =
  let m = Id.Create() |> Member.create
  string m.Id, m.HostName, string m.IpAddr, string m.Port, string m.WsPort, string m.GitPort, string m.ApiPort

let addMember(id, host, ip, port: string, wsPort: string, gitPort: string, apiPort: string) =
  try
    { Member.create (Id id) with
        HostName = host
        IpAddr = IPv4Address ip
        Port = uint16 port
        WsPort = uint16 wsPort
        GitPort = uint16 gitPort
        ApiPort = uint16 apiPort }
    |> AddMember
    |> ClientContext.Singleton.Post
  with
  | exn -> printfn "Couldn't create mem: %s" exn.Message

let alert msg (_: Exception) =
  Browser.window.alert("ERROR: " + msg)

/// Works like function composition but the second operand
/// is a value, and the result of the first function is ignored
let (&>) fst v =
  fun x -> fst x; v

let private postCommandPrivate (cmd: Command) =
  GlobalFetch.fetch(
    RequestInfo.Url Constants.WEP_API_COMMAND,
    !![ RequestProperties.Method HttpMethod.POST
        RequestProperties.Headers [ContentType "application/json"]
        RequestProperties.Body (toJson cmd |> U3.Case3) ])

let postCommand onSuccess onFail (cmd: Command) =
  postCommandPrivate cmd
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.map onFail
    else res.text() |> Promise.map onSuccess)

let postCommandAndBind onSuccess onFail (cmd: Command) =
  postCommandPrivate cmd
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.bind onFail
    else res.text() |> Promise.bind onSuccess)

let postCommandWithErrorNotifier defValue onSuccess cmd =
  postCommand onSuccess (fun msg -> notify msg; defValue) cmd

let postCommandAndForget cmd =
  postCommand ignore notify cmd

let listProjects() =
  ListProjects
  |> postCommandWithErrorNotifier [||] (String.split [|','|])

let shutdown() =
  Shutdown |> postCommand (fun _ -> notify "The service has been shut down") notify

let unloadProject() =
  UnloadProject |> postCommand (fun _ -> notify "The project has been unloaded") notify

let nullify _: 'a = null
  
let rec loadProject(project, username, password, site) =
  LoadProject(project, username, password, site)
  |> postCommandPrivate
  |> Promise.bind (fun res ->
    if res.Ok
    then
      ClientContext.Singleton.ConnectWithWebSocket()
      |> Promise.map (fun _msg -> // TODO: Check message?
        notify "The project has been loaded successfully" |> nullify)
    else
      res.text() |> Promise.map (fun msg ->    
        if msg.Contains(ErrorMessages.PROJECT_NO_ACTIVE_CONFIG)
          || msg.Contains(ErrorMessages.PROJECT_MISSING_CLUSTER)
          || msg.Contains(ErrorMessages.PROJECT_MISSING_MEMBER)
        then msg
        // We cannot deal with the error, just notify it
        else notify msg |> nullify
      )
  )

let getProjectSites(project, username, password) =
  GetProjectSites(project, username, password)
  |> postCommand ofJson<string[]> (fun msg -> notify msg; [||])

let createProject(info: obj) =
  { name          = !!info?name
  ; ipAddress     = !!info?ipAddress
  ; apiPort       = !!info?apiPort
  ; raftPort      = !!info?raftPort
  ; webSocketPort = !!info?webSocketPort
  ; gitPort       = !!info?gitPort }
  |> CreateProject
  |> postCommand (fun _ -> notify "The project has been created successfully") notify

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
    [| leaf ("MachineId: " + string c.MachineId)
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
  ;  leaf ("Name: " + p.Name)
  ;  leaf ("Path: " + p.Path)
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
