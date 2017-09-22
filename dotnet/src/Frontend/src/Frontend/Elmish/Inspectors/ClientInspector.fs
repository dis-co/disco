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
open Iris.Core
open Iris.Web.Core
open Iris.Web.Helpers
open Iris.Web.Types
open State

///  ____        _     _ _
/// |  _ \ _   _| |__ | (_) ___
/// | |_) | | | | '_ \| | |/ __|
/// |  __/| |_| | |_) | | | (__
/// |_|    \__,_|_.__/|_|_|\___|

module ClientInspector =

  let private renderMachine tag dispatch (model: Model) (client: IrisClient) =
    match model.state with
    | None -> Common.stringRow tag (string client.ServiceId)
    | Some state ->
      match Config.tryFindMember state.Project.Config client.ServiceId with
      | None -> Common.stringRow tag (string client.ServiceId)
      | Some mem ->
        Common.row tag [
          Common.link
            (string mem.HostName)
            (fun _ -> Select.clusterMember dispatch mem.Id)
        ]

  let buildGroup dispatch (group: PinGroup) =
    li [] [
      Common.link
        (string group.Name)
        (fun _ -> Select.group dispatch group.ClientId group.Id)
    ]

  let private renderGroups tag dispatch (model: Model) (client: IrisClient) =
    match model.state with
    | None -> Common.row tag []
    | Some state ->
      state.PinGroups
      |> PinGroupMap.findGroupBy (fun group -> group.ClientId = client.Id)
      |> Map.toList
      |> List.map (snd >> buildGroup dispatch)
      |> ul []
      |> fun list -> Common.row tag [ list ]

  let render dispatch (model: Model) (client: ClientId) =
    match model.state with
    | None -> Common.render dispatch model "Client" []
    | Some state ->
      match Map.tryFind client state.Clients with
      | None ->
        Common.render dispatch model "Client" [
          Common.stringRow "Id" (string client + " (orphaned)")
        ]
      | Some client ->
        Common.render dispatch model "Client" [
          Common.stringRow "Id"         (string client.Id)
          Common.stringRow "Name"       (string client.Name)
          Common.stringRow "Role"       (string client.Role)
          Common.stringRow "Status"     (string client.Status)
          Common.stringRow "IP Address" (string client.IpAddress)
          Common.stringRow "Port"       (string client.Port)
          renderMachine    "Machine"     dispatch model client
          renderGroups     "Pin Groups"  dispatch model client
        ]
