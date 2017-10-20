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
    Model: Model
    Dispatch: Msg -> unit
  }

type [<Pojo>] PinGroupState =
  { IsOpen: bool }

let onDragStart (model: Model) pin el multiple =
  match multiple, model.state with
  | true, Some state ->
    let previousPins = model.selectedPins |> Seq.map (fun id -> Lib.findPin id state)
    pin::(Seq.toList previousPins)
  | _ -> [pin]
  |> Drag.Pin |> Drag.start el

let makeInputPin dispatch model (pid: PinId) (pin: Pin) =
  com<PinView.PinView,_,_>
    { key = string pid
      pin = pin
      output = false
      slices = None
      model = model
      updater = Some { new IUpdater with
                        member __.Update(_, index, value) =
                          Lib.updatePinValue(pin, index, value) }
      onSelect = fun multiple -> Select.pin dispatch multiple pin
      onDragStart = Some(onDragStart model pin)
    } []

let makeOutputPin dispatch model (pid: PinId) (pin: Pin) =
  com<PinView.PinView,_,_>
    { key = string pid
      pin = pin
      output = true
      slices = None
      model = model
      updater = None
      onSelect = fun multiple -> Select.pin dispatch multiple pin
      onDragStart = Some(onDragStart model pin)
    } []

type PinGroupView(props) =
  inherit React.Component<PinGroupProps, PinGroupState>(props)
  do base.setInitState({ IsOpen = false })

  member this.render() =
    let { Group = group; Dispatch = dispatch; Model = model } = this.props
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
          if not(Lib.isOutputPin pin)
          then makeInputPin dispatch model pid pin |> Some
          else None) |> Seq.toList)
        yield div [] (group.Pins |> Seq.choose (fun (KeyValue(pid, pin)) ->
          if Lib.isOutputPin pin
          then makeOutputPin dispatch model pid pin |> Some
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
                              Model = model } [])
    |> Seq.toList)

let createWidget (id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.GraphView
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 2; maxW = 20
        minH = 2; maxH = 20 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 ->
            equalsRef s1.PinGroups s2.PinGroups
              && equalsRef m1.selectedPins m2.selectedPins
          | None, None -> true
          | _ -> false)
        (widget id this.Name None body dispatch)
        model
  }
