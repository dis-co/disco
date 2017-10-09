module Iris.Web.GraphView

open System
open System.Collections.Generic
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Helpers
open State
open Types

type [<Pojo>] PinGroupProps =
  { key: string
    Group: PinGroup
    Dispatch: Msg -> unit
    UseRightClick: bool }

type [<Pojo>] PinGroupState =
  { IsOpen: bool }

let isInput (pin: Pin) =
  match pin.PinConfiguration with
  | PinConfiguration.Preset | PinConfiguration.Sink -> true
  | PinConfiguration.Source -> false

let isNumberOrBool (pin: Pin) =
  match pin with
  | NumberPin _ | BoolPin _ -> true
  | _ -> false

let makeInputPin dispatch useRightClick (pid: PinId) (pin: Pin) =
  com<PinView.PinView,_,_>
    { key = string pid
      pin = pin
      output = false
      useRightClick = useRightClick
      slices = None
      updater = Some { new IUpdater with
                        member __.Update(_, index, value) =
                          Lib.updatePinValue(pin, index, value) }
      onSelect = fun () -> Select.pin dispatch pin
      onDragStart = Some(fun el ->
        Drag.Pin pin |> Drag.start el) } []

let makeOutputPin dispatch useRightClick (pid: PinId) (pin: Pin) =
  com<PinView.PinView,_,_>
    { key = string pid
      pin = pin
      output = true
      useRightClick = useRightClick
      slices = None
      updater = None
      onSelect = fun () -> Select.pin dispatch pin
      onDragStart = Some(fun el ->
        Drag.Pin pin |> Drag.start el) } []

type PinGroupView(props) =
  inherit React.Component<PinGroupProps, PinGroupState>(props)
  do base.setInitState({ IsOpen = false })

  member this.render() =
    let { Group = group; Dispatch = dispatch; UseRightClick = rightClick } = this.props
    li [] [
      yield div [] [
        button [
          ClassName ("iris-button iris-icon icon-control " +
            (if this.state.IsOpen then "icon-less" else "icon-more"))
          OnClick (fun ev ->
            ev.stopPropagation()
            this.setState({ this.state with IsOpen = not this.state.IsOpen}))
        ] []
        span [
            OnClick (fun _ -> Select.group dispatch group)
            Style [ Cursor "pointer" ]
          ] [str (unwrap group.Name)]
      ]
      if this.state.IsOpen then
        yield div [] (group.Pins |> Seq.choose (fun (KeyValue(pid, pin)) ->
          if isInput pin
          then makeInputPin dispatch rightClick pid pin |> Some
          else None) |> Seq.toList)
        yield div [] (group.Pins |> Seq.choose (fun (KeyValue(pid, pin)) ->
          if not(isInput pin) && isNumberOrBool pin
          then makeOutputPin dispatch rightClick pid pin |> Some
          else None) |> Seq.toList)
    ]

let body dispatch (model: Model) =
  let pinGroups =
    match model.state with
    | Some state ->
      state.PinGroups
      |> PinGroupMap.unifiedPins
      |> PinGroupMap.byGroup
    | None -> Map.empty
  ul [Class "iris-graphview"] (
    pinGroups |> Seq.map (fun (KeyValue(gid, group)) ->
      com<PinGroupView,_,_> { key = string gid
                              Group = group
                              Dispatch = dispatch
                              UseRightClick = model.userConfig.useRightClick } [])
    |> Seq.toList)

let createWidget (id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.GraphView
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 2; maxW = 10
        minH = 2; maxH = 10 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 ->
            equalsRef s1.PinGroups s2.PinGroups
          | None, None -> true
          | _ -> false)
        (widget id this.Name None body dispatch)
        model
  }
