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
  { context: ClientContext; state: State }

let getCurrentSession(info: StateInfo) =
  info.state.Sessions
  |> Map.tryFind info.context.Session

let login(info: StateInfo, username: string, password: string) =
  getCurrentSession info
  |> Option.iter (fun curSession ->
    { curSession with Status = { StatusType=Login; Payload=username+"\n"+password}}
    |> UpdateSession
    |> info.context.Post)

let removeNode(info: StateInfo, nodeId: Id) =
  match Map.tryFind nodeId info.state.Nodes with
  | Some node ->
    RemoveNode node
    |> info.context.Post
  | None ->
    printfn "Couldn't find node with Id %O" nodeId
