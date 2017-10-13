module Iris.Web.PinView

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Types

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

let rec findWithClassUpwards (className: string) (el: Browser.Element) =
  if el.classList.contains(className)
  then el
  else
    match el.parentElement with
    | null -> failwithf "Couldn't find any element with class %s in parent hierarchy" className
    | el -> findWithClassUpwards className el

type [<Pojo>] PinState =
  { isOpen: bool }

type [<Pojo>] PinProps =
  { key: string
    pin: Pin
    output: bool
    slices: Slices option
    model: Model
    updater: IUpdater option
    onDragStart: (Browser.Element -> bool -> unit) option
    onSelect: bool -> unit
  }

type PinView(props) =
  inherit React.Component<PinProps, PinState>(props)
  do base.setInitState({ isOpen = false })

  member this.ValueAt(i) =
    match this.props.slices with
    | Some slices -> slices.[index i].Value
    | None -> this.props.pin.Slices.[index i].Value

  member inline this.RenderRows(rowCount: int, useRightClick: bool, updater: IUpdater option) =
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
          createElement("div", options, this.ValueAt(0))
          this.RenderArrow()
        ]
      else
        td [] [createElement("div", options, this.ValueAt(0))]
    let head =
      tr [ClassName "iris-pin-child"] [
        td [
          OnMouseDown (fun ev ->
            ev.stopPropagation()
            match this.props.onDragStart with
            | Some onDragStart ->
              let el = findWithClassUpwards "iris-pin" !!ev.target
              // TODO: Use another key for multiple selections? Make it configurable? (See below too)
              onDragStart el ev.ctrlKey
            | None -> ()
            this.props.onSelect(ev.ctrlKey)
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
            td [] [createElement("div", options, this.ValueAt(i))]
          ]
      ]
    else tbody [] [head]

  member this.RenderArrow() =
    span [
      ClassName ("iris-icon icon-control " + (if this.state.isOpen then "icon-less" else "icon-more"))
      OnClick (fun ev ->
        ev.stopPropagation()
        this.setState({ this.state with isOpen = not this.state.isOpen}))
    ] []

  member this.render() =
    let pin = this.props.pin
    let useRightClick =
      this.props.model.userConfig.useRightClick
    let isSelected =
      match this.props.onDragStart with
      | Some _ -> this.props.model.selectedPins |> Seq.exists ((=) pin.Id)
      | None -> false
    let rowCount =
      match this.props.slices with
      | Some slices -> slices.Length
      | None -> pin.Slices.Length
    let classes =
      ["iris-pin", true
       "iris-pin-output",    this.props.output
       "iris-dirty",         not this.props.output && pin.Dirty
       "iris-non-persisted", not pin.Persisted
       "iris-offline",       pin.Persisted && not pin.Online
       "iris-selected",      isSelected
       ]
    div [classList classes] [
      table [] [this.RenderRows(rowCount, useRightClick, props.updater)]
    ]
