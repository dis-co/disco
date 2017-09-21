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
open Iris.Web.PinView
open Iris.Web.Core
open Iris.Web.Helpers
open Iris.Web.Types
open State

///  ____        _     _ _
/// |  _ \ _   _| |__ | (_) ___
/// | |_) | | | | '_ \| | |/ __|
/// |  __/| |_| | |_) | | | (__
/// |_|    \__,_|_.__/|_|_|\___|

module PinGroupInspector =
  let private buildPin dispatch (model: Model) (pinId, pin) =
    li [] [
      com<PinView,_,_> {
        key = string pinId
        pin = pin
        useRightClick = model.userConfig.useRightClick
        slices = None
        updater = None
        onSelect = fun () -> Select.pin dispatch pin
        onDragStart = None
      } []
    ]

  let private renderPins (tag: string) dispatch (model: Model) (group: PinGroup) =
    let pins =
      group.Pins
      |> Map.toList
      |> List.map (buildPin dispatch model)

    Common.row tag [
      ul [ Class "iris-graphview" ] pins
    ]

  let private renderClients tag dispatch model (group: PinGroup) =
    match model.state with
    | None -> Common.row tag []
    | Some state ->
      state.PinGroups
      |> PinGroupMap.findGroupBy (fun g -> g.Id = group.Id)
      |> Map.toList
      |> List.map
        (fun (clientId,group) ->
          match Map.tryFind clientId state.Clients with
          | Some client ->
            li [] [
              Common.link
                (string client.Name)
                (fun _ -> Select.client dispatch client)
            ]
          | None ->
            match ClientConfig.tryFind clientId state.Project.Config.Clients with
            | Some exe -> li [] [ str (string exe.Id) ]
            | None -> li [] [ str (string clientId + " (orphaned)") ])
      |> fun clients -> Common.row tag [ ul [] clients ]

  let private renderRefersTo tag dispatch (model: Model) (group: PinGroup) =
    tr [] []

  let render dispatch (model: Model) (group: PinGroup) =
    Common.render "Pin Group" [
      Common.stringRow "Id"         (string group.Id)
      Common.stringRow "Name"       (string group.Name)
      Common.stringRow "Path"       (string group.Path)
      Common.stringRow "Asset Path" (string group.AssetPath)
      renderRefersTo   "Belongs To"  dispatch model group
      renderClients    "Clients"     dispatch model group
      renderPins       "Pins"        dispatch model group
    ]
