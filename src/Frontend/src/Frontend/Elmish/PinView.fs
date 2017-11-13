module Iris.Web.PinView

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Iris.Web.Types

type [<Pojo>] ElProps =
  { index: int
    precision: uint32 option
    useRightClick: bool
    updater: IUpdater option
    classes: string array
    suffix: string option
  }

let createElement(tagName: string, opts: ElProps, value: obj): React.ReactElement =
  importMember "../../js/Util"

let (|NullOrEmpty|_|) str =
  if String.IsNullOrEmpty(str) then Some NullOrEmpty else None

type [<Pojo>] PinState =
  { isOpen: bool }

type [<Pojo>] PinProps =
  { key: string
    pin: Pin
    output: bool
    selected: bool
    slices: Slices option
    model: Model
    updater: IUpdater option
    onDragStart: (bool -> unit) option
    onSelect: bool -> unit
  }

// * PinView

type PinView(props) =
  inherit React.Component<PinProps, PinState>(props)
  do base.setInitState({ isOpen = false })

  // ** valueAt

  member this.valueAt(i) =
    match this.props.slices with
    | Some slices -> slices.[index i].Value
    | None -> this.props.pin.Slices.[index i].Value

  // ** renderRows

  member inline this.renderRows(rowCount: int, useRightClick: bool, updater: IUpdater option) =
    let pin = this.props.pin
    let name =
      if pin.Name |> unwrap |> String.IsNullOrEmpty
      then "--"
      else unwrap pin.Name
    let precision =
      match pin with
      | NumberPin pin -> Some pin.Precision
      | _ -> None
    let firstRowValue =
      let options =
        { index = 0
          precision = precision
          useRightClick = useRightClick
          updater = if rowCount > 1 then None else updater
          classes = if rowCount > 1 then [|"iris-flex-1"|] else [||]
          suffix  = if rowCount > 1 then Some(" (" + string rowCount + ")") else None
        }
      if rowCount > 1 then
        td [ClassName "iris-flex-row"] [
          createElement("div", options, this.valueAt(0))
          this.renderArrow()
        ]
      else
        td [] [createElement("div", options, this.valueAt(0))]
    let head =
      tr [ClassName "iris-pin-child"] [
        td [
          OnMouseDown (fun ev ->
            ev.stopPropagation()
            let ev = ev :?> Browser.MouseEvent
            match this.props.onDragStart with
            | Some onDragStart ->
              Keyboard.isMultiSelection ev |> onDragStart
            | None -> ()
            this.props.onSelect(Keyboard.isMultiSelection ev)
          )
        ] [str name]
        firstRowValue
      ]
    if rowCount > 1 && this.state.isOpen then
      tbody [] [
        yield head
        for i=0 to rowCount - 1 do
          let label =
            // The Labels array can be shorter than Values'
            match Array.tryItem i pin.Labels with
            | None | Some(NullOrEmpty) -> sprintf "Slice%i" i
            | Some label -> label
          let options =
            { index = i
              precision = precision
              useRightClick = useRightClick
              updater = updater
              classes = [||]
              suffix  = None
            }
          yield tr [Key (string i); ClassName "iris-pin-child"] [
            td [] [str label]
            td [] [createElement("div", options, this.valueAt(i))]
          ]
      ]
    else tbody [] [head]

  // ** renderArrow

  member this.renderArrow() =
    span [
      classList [
        "iris-icon icon-control",true
        "icon-less", this.state.isOpen
        "icon-more", not this.state.isOpen
      ]
      OnClick (fun ev ->
        ev.stopPropagation()
        this.setState({ this.state with isOpen = not this.state.isOpen}))
    ] []

  // ** render

  member this.render() =
    let pin = this.props.pin
    let useRightClick =
      this.props.model.userConfig.useRightClick
    let rowCount =
      match this.props.slices with
      | Some slices -> slices.Length
      | None -> pin.Slices.Length
    let isOffline =
      (pin.Persisted && not pin.Online)
      // Make placeholder pins (with empty Ids) look as if they were offline
      || Lib.isMissingPin pin
    let classes =
      [ "iris-pin",           true
        "iris-pin-output",    this.props.output
        "iris-dirty",         not this.props.output && pin.Dirty
        "iris-non-persisted", not pin.Persisted
        "iris-offline",       isOffline
        "iris-pin-selected",  this.props.selected ]
    div [classList classes] [
      table [] [this.renderRows(rowCount, useRightClick, props.updater)]
    ]
