namespace Disco.Web.Inspectors

open System
open System.Collections.Generic
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Disco.Raft
open Disco.Core
open Disco.Web.Core
open Disco.Web.Helpers
open Disco.Web.Types
open State

module MemberInspector =
  let private buildClient dispatch (client: DiscoClient) =
    Common.link
      (string client.Name)
      (fun _ -> Select.client dispatch client)

  let private renderClients tag dispatch (model: Model) (mem: ClusterMember) =
    match model.state with
    | None -> Common.row tag []
    | Some state ->
      state.Clients
      |> Map.filter (fun _ client -> client.ServiceId = mem.Id)
      |> Map.toList
      |> List.map (snd >> buildClient dispatch)
      |> Common.row tag

  let render dispatch (model: Model) (mem: MemberId) =
    match model.state with
    | None ->
      Common.render dispatch model "Cluster Member" [
        str (string mem + " (orphaned)")
      ]
    | Some state ->
      match Config.tryFindMember state.Project.Config mem with
      | None ->
        Common.render dispatch model "Cluster Member" [
          str (string mem + " (orphaned)")
        ]
      | Some mem ->
        Common.render dispatch model "Cluster Member" [
          Common.stringRow "Id"             (string mem.Id)
          Common.stringRow "Host Name"      (string mem.HostName)
          Common.stringRow "Node Status"    (string mem.Status)
          Common.stringRow "IP Address"     (string mem.IpAddress)
          Common.stringRow "Raft Port"      (string mem.RaftPort)
          Common.stringRow "API Port"       (string mem.ApiPort)
          Common.stringRow "Git Port"       (string mem.GitPort)
          Common.stringRow "WebSocket Port" (string mem.WsPort)
          renderClients    "Clients"        dispatch model mem
        ]
