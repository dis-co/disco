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

type [<Pojo>] InputState =
  { isOpen: bool }

let addInputView(index: int, value: obj, tagName: string, useRigthClick: bool, updater: IUpdater option): React.ReactElement =
  importMember "../../js/Util"

let formatValue(value: obj): string =
  importMember "../../js/Util"

[<Global>]
let jQuery(el: obj): obj = jsNative

let (|NullOrEmpty|_|) str =
  if String.IsNullOrEmpty(str) then Some NullOrEmpty else None

let rec findWithClassUpwards (className: string) (el: Browser.Element) =
  if el.classList.contains(className)
  then el
  else
    match el.parentElement with
    | null -> failwithf "Couldn't find any element with class %s in parent hierarchy" className
    | el -> findWithClassUpwards className el

type [<Pojo>] PinProps =
  { key: string
    pin: Pin
    useRightClick: bool
    slices: Slices option
    updater: IUpdater option
    onDragStart: (Browser.Element -> unit) option }

type PinView(props) =
  inherit React.Component<PinProps, InputState>(props)
  do base.setInitState({ isOpen = false })

  member this.ValueAt(i) =
    match this.props.slices with
    | Some slices -> slices.[index i].Value
    | None -> this.props.pin.Slices.[index i].Value

  member inline this.RenderRows(rowCount: int, useRightClick: bool, updater: IUpdater option) =
    let name =
      if this.props.pin.Name |> unwrap |> String.IsNullOrEmpty
      then "--"
      else unwrap this.props.pin.Name
    let firstRowValue =
      if rowCount > 1 then
        td [ClassName "iris-flex-row"] [
          span [ClassName "iris-flex-1"] [str (sprintf "%s (%d)" (formatValue(this.ValueAt(0))) rowCount)]
          this.RenderArrow()
        ]
      else
        addInputView(0, this.ValueAt(0), "td", useRightClick, updater)
    let head =
      tr [ClassName "iris-pin-child"] [
        td [
          OnMouseDown (fun ev ->
            ev.stopPropagation()
            let el = findWithClassUpwards "iris-pin" !!ev.target
            match this.props.onDragStart with
            | Some onDragStart -> onDragStart(el)
            | None -> ())
        ] [str name]
        firstRowValue
      ]
    if rowCount > 1 && this.state.isOpen then
      let labels = this.props.pin.Labels
      tbody [] [
        yield head
        for i=0 to rowCount - 1 do
          let label =
            // The Labels array can be shorter than Values'
            match Array.tryItem i labels with
            | None | Some(NullOrEmpty) -> sprintf "Slice%i" i
            | Some label -> label
          yield tr [Key (string i); ClassName "iris-pin-child"] [
            td [] [str label]
            addInputView(i, this.ValueAt(i), "td", useRightClick, updater)
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
    let rowCount =
      match this.props.slices with
      | Some slices -> slices.Length
      | None -> this.props.pin.Slices.Length
    let dirtyClass = if this.props.pin.Dirty then " iris-dirty" else ""
    div [ClassName ("iris-pin" + dirtyClass) ] [
      table [] [this.RenderRows(rowCount, this.props.useRightClick, props.updater)]
    ]
