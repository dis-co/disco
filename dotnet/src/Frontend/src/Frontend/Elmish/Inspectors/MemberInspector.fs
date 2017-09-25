namespace Iris.Web.Inspectors

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
open Iris.Raft
open Iris.Core
open Iris.Web.Core
open Iris.Web.Helpers
open Iris.Web.Types
open State

module MemberInspector =
  let private buildClient dispatch (client: IrisClient) =
    li [] [
      Common.link
        (string client.Name)
        (fun _ -> Select.client dispatch client)
    ]

  let private renderClients tag dispatch (model: Model) (mem: RaftMember) =
    match model.state with
    | None -> Common.row tag []
    | Some state ->
      state.Clients
      |> Map.filter (fun _ client -> client.ServiceId = mem.Id)
      |> Map.toList
      |> List.map (snd >> buildClient dispatch)
      |> ul []
      |> fun list -> Common.row tag [ list ]

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
          Common.stringRow "Raft State"     (string mem.State)
          Common.stringRow "IP Address"     (string mem.IpAddr)
          Common.stringRow "Raft Port"      (string mem.Port)
          Common.stringRow "API Port"       (string mem.ApiPort)
          Common.stringRow "Git Port"       (string mem.GitPort)
          Common.stringRow "WebSocket Port" (string mem.WsPort)
          renderClients    "Clients"        dispatch model mem
        ]
