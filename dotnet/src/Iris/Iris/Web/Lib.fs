module Iris.Web.Lib

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open System
open Iris.Raft
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.PowerPack
open Fable.PowerPack.Fetch.Fetch_types

let login(info: StateInfo, username: string, password: string) =
  { info.session with Status = { StatusType=Login; Payload=username+"\n"+password}}
  |> UpdateSession
  |> info.context.Post

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

let loadProject(info: StateInfo, dir: string) =
  Fetch.fetch Constants.COMMAND_ENDPOINT
    [ RequestProperties.Method HttpMethod.POST
    ; RequestProperties.Body (BodyInit.Case3 ("load " + dir)) ]
  |> Promise.bind (fun _ ->
    // TODO: The server should indicate somehow if the server has been loaded
    // This is a cheap trick
    Promise.sleep 500)
  |> Promise.bind (fun _ ->
    info.context.ConnectWithWebSocket())

let createProject(_info: StateInfo, dir: string, bind, git, ws, web, raft) =
  let dir = if dir.Contains(" ") then "\"" + dir + "\"" else dir
  let cmd = sprintf "create dir:%s bind:%s git:%s ws:%s web:%s raft:%s"
                    dir bind git ws web raft
  Fetch.fetch Constants.COMMAND_ENDPOINT
    [ RequestProperties.Method HttpMethod.POST
    ; RequestProperties.Body (BodyInit.Case3 cmd) ]
