module Iris.Web.Lib

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open Iris.Core
open Iris.Web.Core
open Fable.Core

type [<Pojo; NoComparison>] StateInfo =
  { context: ClientContext; session: Session; state: State }

let login(info: StateInfo, username: string, password: string) =
  { info.session with Status = { StatusType=Login; Payload=username+"\n"+password}}
  |> UpdateSession
  |> info.context.Post

let removeNode(info: StateInfo, nodeId: Id) =
  match Map.tryFind nodeId info.state.Nodes with
  | Some node ->
    RemoveNode node
    |> info.context.Post
  | None ->
    printfn "Couldn't find node with Id %O" nodeId

let addNode(info: StateInfo, host: string, ip: string, port: string) =
  try
    let node = Id.Create() |> Iris.Raft.Node.create
    { node with HostName = host; IpAddr = IPv4Address ip; Port = uint16 port }
    |> AddNode
    |> info.context.Post
  with
  | exn -> printfn "Couldn't create node: %s" exn.Message

