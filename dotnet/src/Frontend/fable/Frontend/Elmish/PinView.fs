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

type IUpdater =
  abstract Update: dragging:bool * index:int * value:obj -> unit

let addInputView(index: int, value: obj, tagName: string, useRigthClick: bool, updater: IUpdater): React.ReactElement =
  importMember "../../../src/Util.ts"

let formatValue(value: obj): string =
  importMember "../../../src/Util.ts"

[<Global>]
let jQuery(el: obj): obj = jsNative

let private updatePinValue(pin: Pin, index: int, value: obj) =
  let updateArray (i: int) (v: obj) (ar: 'T[]) =
    let newArray = Array.copy ar
    newArray.[i] <- unbox v
    newArray
  match pin with
  | StringPin pin ->
    StringSlices(pin.Id, updateArray index value pin.Values)
  | NumberPin pin ->
    let value =
      match value with
      | :? string as v -> box(double v)
      | v -> v
    NumberSlices(pin.Id, updateArray index value pin.Values)
  | BoolPin pin ->
    let value =
      match value with
      | :? string as v -> box(v.ToLower() = "true")
      | v -> v
    BoolSlices(pin.Id, updateArray index value pin.Values)
  | BytePin   _pin -> failwith "TO BE IMPLEMENTED"
  | EnumPin   _pin -> failwith "TO BE IMPLEMENTED"
  | ColorPin  _pin -> failwith "TO BE IMPLEMENTED"
  |> UpdateSlices |> ClientContext.Singleton.Post

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
    | None -> this.props.pin.Values.[index i].Value

  member inline this.RenderRows(rowCount: int, useRightClick: bool, updater: IUpdater) =
    let name =
      if String.IsNullOrEmpty(this.props.pin.Name)
      then "--"
      else this.props.pin.Name
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
      let tags = this.props.pin.GetTags
      tbody [] [
        yield head
        for i=0 to rowCount - 1 do
          let label =
            // The Labels array can be shorter than Values'
            match Array.tryItem i tags |> Option.map string with
            | None | Some(NullOrEmpty) -> "Tag"
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
    let updater =
      match props.updater with
      | Some updater -> updater
      | None ->
        { new IUpdater with
            member __.Update(_, index, value) =
              updatePinValue(this.props.pin, index, value) }
    let rowCount =
      match this.props.slices with
      | Some slices -> slices.Length
      | None -> this.props.pin.Values.Length
    div [ClassName "iris-pin"] [
      table [] [this.RenderRows(rowCount, this.props.useRightClick, updater)]
    ]
