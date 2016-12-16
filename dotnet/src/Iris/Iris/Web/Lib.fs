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

let login(info: StateInfo, username: string, password: string) =
  { info.session with Status = { StatusType=Login; Payload=username+"\n"+password}}
  |> UpdateSession
  |> info.context.Post

let subscribeToLogs(ctx: ClientContext, f:ClientLog->unit): IDisposable =
  ctx.OnClientLog.Subscribe(f)

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
