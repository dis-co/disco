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
open Disco.Core
open Disco.Web.Core
open Disco.Web.Helpers
open Disco.Web.Types
open State

///  ____        _     _ _
/// |  _ \ _   _| |__ | (_) ___
/// | |_) | | | | '_ \| | |/ __|
/// |  __/| |_| | |_) | | | (__
/// |_|    \__,_|_.__/|_|_|\___|

module PinInspector =

  let private renderValue (tag: string) (value: string) =
    div [ Class "columns" ] [
      div [ Class "column" ] [str tag ]
      div [ Class "column" ] [str value]
    ]

  let private renderSlices (tag: string) (slices: Slices) =
    let slices: ReactElement array =
      slices.Map (function
      | StringSlice(idx, value) -> renderValue (string idx) (string value)
      | NumberSlice(idx, value) -> renderValue (string idx) (string value)
      | BoolSlice(idx, value)   -> renderValue (string idx) (string value)
      | ByteSlice(idx, value)   -> renderValue (string idx) (string value)
      | EnumSlice(idx, value)   -> renderValue (string idx) (string value)
      | ColorSlice(idx, value)  -> renderValue (string idx) (string value))
    slices
    |> List.ofArray
    |> Common.tableRow tag [ "Index"; "Value" ]

  let private renderClients (tag: string) dispatch (model: Model) (pin: Pin) =
    match model.state with
    | None -> Common.row tag [ str (string pin.ClientId) ]
    | Some state ->
      state.PinGroups
      |> PinGroupMap.findGroupBy (PinGroup.contains pin.Id)
      |> Map.toList
      |> List.map
        (fun (client,_) ->
          match Map.tryFind client state.Clients with
          | Some client ->
            Common.link
              (string client.Name)
              (fun _ -> Select.client dispatch client)
          | None ->
            match ClientConfig.tryFind client state.Project.Config.Clients with
            | Some config -> str (config.Id.Prefix())
            | None        -> str ((client.Prefix()) + " (orphaned)"))
      |> Common.row tag

  let private renderGroup (tag: string) dispatch (model: Model) (pin: Pin) =
    match model.state with
    | None -> Common.row tag [ str (string pin.PinGroupId) ]
    | Some state ->
      state.PinGroups
      |> PinGroupMap.findGroupBy (fun group -> group.Id = pin.PinGroupId)
      |> Map.toList
      |> List.map
        (fun (client, group) ->
          Common.link
            (string group.Name)
            (fun _ -> Select.group dispatch group))
      |> Common.row tag

  let private renderCues (tag: string) dispatch (model: Model) (pin: Pin) =
    match model.state with
    | None -> Common.row tag [ ]
    | Some state ->
      state.Cues
      |> Map.fold
        (fun lst _ (cue: Cue) ->
          match Array.tryFindIndex (fun (slices: Slices) -> slices.PinId = pin.Id) cue.Slices with
          | Some _ -> cue :: lst
          | None -> lst)
        List.empty
      |> List.map
        (fun (cue: Cue) ->
          Common.link
            (string cue.Name)
            (fun _ -> Select.cue dispatch cue))
      |> Common.row tag

  let private configurationRow tag dispatch (model: Model) (pin: Pin) =
    let selected config = pin.PinConfiguration = config
    let handler str =
      let parsed = str |> PinConfiguration.Parse
      pin
      |> Pin.setPinConfiguration parsed
      |> UpdatePin
      |> ClientContext.Singleton.Post
    Common.row tag [
      select [
        Value (string pin.PinConfiguration)
        OnChange (fun ev -> handler !!ev.target?value)
      ] [
        option [] [
          str (string PinConfiguration.Sink)
        ]
        option [] [
          str (string PinConfiguration.Source)
        ]
        option [] [
          str (string PinConfiguration.Preset)
        ]
      ]
    ]

  let private onlineRow tag (pin: Pin) =
    let icon =
      if pin.Online
      then "disco-status-on"
      else "disco-status-off"

    Common.row tag [
      span [] [
        span [Class ("disco-icon icon-bull " + icon)] []
      ]
    ]

  let render dispatch (model: Model) (client: ClientId) (pin: PinId) =
    let updatePin = UpdatePin >> ClientContext.Singleton.Post
    match model.state with
    | None ->
      Common.render dispatch model "Pin" [
        str (string pin + " (orphaned)")
      ]
    | Some state ->
      match PinGroupMap.findPin pin state.PinGroups |> Map.tryFind client with
      | None ->
        Common.render dispatch model "Pin" [
          str (string pin + " (orphaned)")
        ]
      | Some pin ->
        Common.render dispatch model "Pin" [
          Common.stringRow "Id"            (string pin.Id)
          Common.stringRow "Name"          (string pin.Name)
          Common.stringRow "Type"          (string pin.Type)
          configurationRow "Configuration"  dispatch model pin
          Common.stringRow "VecSize"       (string pin.VecSize)
          renderClients    "Clients"        dispatch model pin
          renderGroup      "Group"          dispatch model pin
          onlineRow        "Online"         pin
          Common.buttonRow "Persisted"      pin.Persisted (flip Pin.setPersisted pin >> updatePin)
          Common.stringRow "Dirty"         (string pin.Dirty)
          Common.stringRow "Labels"        (string pin.Labels)
          Common.stringRow "Tags"          (string pin.GetTags)
          renderSlices     "Values"         pin.Slices
          renderCues       "Cues With Pin"  dispatch model pin
        ]
