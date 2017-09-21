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

module PinInspector =

  let private renderValue (tag: string) (value: string) =
    tr [Key tag] [
      td [Class "width-5";  Common.leftSub  ] [str (tag + ":")]
      td [Class "width-30"; Common.rightSub ] [str value]
    ]

  let private renderSlices (tag: string) (slices: Slices) =
    slices.Map (function
    | StringSlice(idx, value) -> renderValue (string idx) (string value)
    | NumberSlice(idx, value) -> renderValue (string idx) (string value)
    | BoolSlice(idx, value)   -> renderValue (string idx) (string value)
    | ByteSlice(idx, value)   -> renderValue (string idx) (string value)
    | EnumSlice(idx, value)   -> renderValue (string idx) (string value)
    | ColorSlice(idx, value)  -> renderValue (string idx) (string value))
    |> List.ofArray
    |> Common.tableRow tag [ "Index"; "Value" ]

  let private renderClients (tag: string) dispatch (model: Model) (pin: Pin) =
    match model.state with
    | None -> Common.row tag [ str (string pin.ClientId) ]
    | Some state ->
      let clients =
        state.PinGroups
        |> PinGroupMap.findGroupBy (PinGroup.contains pin.Id)
        |> Map.toList
        |> List.map
          (fun (client,_) ->
            match Map.tryFind client state.Clients with
            | Some client ->
              tr [ Key (string pin.Id) ] [
                td [ Common.leftSub ] [
                  Common.link
                    (string client.Name)
                    (fun _ -> Select.client dispatch client)
                ]
                td [ Common.rightSub ] [ str (string client.Status) ]
              ]
            | None ->
              match ClientConfig.tryFind client state.Project.Config.Clients with
              | Some config ->
                tr [ Key (string pin.Id) ] [
                  td [ Common.leftSub  ] [ str (config.Id.Prefix())   ]
                  td [ Common.rightSub ] [ str "Offline" ]
                ]
              | None ->
                tr [ Key (string pin.Id) ] [
                  td [ Common.leftSub  ] [ str ((client.Prefix()) + " (orphaned)") ]
                  td [ Common.rightSub ] [ str "Offline" ]
                ])
      Common.tableRow tag [ "Name/Id"; "Status" ] clients

  let private renderGroup (tag: string) dispatch (model: Model) (pin: Pin) =
    match model.state with
    | None -> Common.row tag [ str (string pin.PinGroupId) ]
    | Some state ->
      state.PinGroups
      |> PinGroupMap.findGroupBy (fun group -> group.Id = pin.PinGroupId)
      |> Map.toList
      |> List.map
        (fun (client, group) ->
          tr [ Key (string client) ] [
            td [
              Style [ Cursor "pointer" ]
              OnClick (fun _ -> Select.group dispatch group)
            ] [
              str (string group.Name + " on " + (group.ClientId.Prefix()))
            ]
          ])
      |> Common.tableRow tag [ "Group on Host" ]

  let render dispatch (model: Model) (pin: Pin) =
    Common.render "Pin" [
      Common.stringRow "Id"            (string pin.Id)
      Common.stringRow "Name"          (string pin.Name)
      Common.stringRow "Type"          (string pin.Type)
      Common.stringRow "Configuration" (string pin.PinConfiguration)
      Common.stringRow "VecSize"       (string pin.VecSize)
      renderClients    "Clients"        dispatch model pin
      renderGroup      "Group"          dispatch model pin
      Common.stringRow "Online"        (string pin.Online)
      Common.stringRow "Persisted"     (string pin.Persisted)
      Common.stringRow "Dirty"         (string pin.Dirty)
      Common.stringRow "Labels"        (string pin.Labels)
      Common.stringRow "Tags"          (string pin.GetTags)
      renderSlices     "Values"         pin.Slices
    ]
