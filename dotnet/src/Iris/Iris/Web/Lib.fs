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

let EMPTY = Constants.EMPTY

let notify(msg: string) =
  match box Browser.window?Notification with
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

let subscribeToLogs(ctx: ClientContext, f:ClientLog->unit): IDisposable =
    ctx.OnMessage.Subscribe (function
      | ClientMessage.ClientLog log -> f log
      | _ -> ())

let removeMember(info: StateInfo, memId: Id) =
  match Map.tryFind memId info.state.Project.Config.Cluster.Members with
  | Some mem ->
    RemoveMember mem
    |> info.context.Post
  | None ->
    printfn "Couldn't find mem with Id %O" memId

let addMember(info: StateInfo, host: string, ip: string, port: string) =
  try
    let mem = Id.Create() |> Member.create
    { mem with HostName = host; IpAddr = IPv4Address ip; Port = uint16 port }
    |> AddMember
    |> info.context.Post
  with
  | exn -> printfn "Couldn't create mem: %s" exn.Message

let alert msg (_: Exception) =
  Browser.window.alert("ERROR: " + msg)

/// Works like function composition but the second operand
/// is a value, and the result of the first function is ignored
let (&>) fst v =
  fun x -> fst x; v

let postCommand defValue success (cmd: Command) =
  GlobalFetch.fetch(
    RequestInfo.Url Constants.WEP_API_COMMAND,
    !![ RequestProperties.Method HttpMethod.POST
        RequestProperties.Headers [ContentType "application/json"]
        RequestProperties.Body (toJson cmd |> U3.Case3) ])
  |> Promise.bind (fun res ->
    if not res.Ok
    then res.text() |> Promise.map (fun msg -> notify msg; defValue)
    else res.text() |> Promise.map success)

let postCommandAndForget cmd =
  postCommand () ignore cmd

let listProjects() =
  ListProjects
  |> postCommand [||] (String.split [|','|])

let loadProject(info: StateInfo, project, username, password) =
  LoadProject(project, username, password)
  |> postCommand () (fun _ -> info.context.ConnectWithWebSocket() |> ignore)

let createProject(_info: StateInfo, projectName: string, ipAddress, gitPort, webSocketPort, raftPort) =
  { name = projectName
  ; ipAddress = ipAddress
  ; gitPort = gitPort
  ; webSocketPort = webSocketPort
  ; raftPort = raftPort }
  |> CreateProject
  |> postCommandAndForget

type [<Pojo>] TreeNode =
  { ``module``: string; children: TreeNode[] option }

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
    ;  obj2tree "Cluster" c.Cluster
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
