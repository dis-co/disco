module Disco.Web.PinView

open System
open Disco.Core
open Disco.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Disco.Web.Types
open Disco.Web.Tooltips

type [<Pojo>] ElProps =
  { index: int
    precision: uint32 option
    min: int32
    max: int32
    useRightClick: bool
    updater: IUpdater option
    handleExternally: bool
    onDoubleClick: (unit -> unit) option
    properties: (string * string) array
    classes: string array
    suffix: string option
    title: string
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
    dispatch: Msg -> unit
  }

// * PinView

type PinView(props) =
  inherit React.Component<PinProps, PinState>(props)
  do base.setInitState({ isOpen = false })

  // ** valueAt

  member this.valueAt(i) =
    match this.props.slices with
    | Some slices -> slices.[i].Value
    | None -> this.props.pin.Slices.[i].Value

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
    let properties =
      match pin with
      | EnumPin pin -> pin.Properties |> Array.map (fun prop -> prop.Value,prop.Key)
      | _ -> Array.empty
    let min =
      match pin with
      | NumberPin data -> data.Min
      | _ -> 0
    let max =
      match pin with
      | NumberPin data -> data.Max
      | _ -> 0
    let firstRowValue =
      let options =
        { index = 0
          min = min
          max = max
          precision = precision
          useRightClick = useRightClick
          properties = properties
          handleExternally =
            match pin with
            | StringPin { Behavior = behavior } ->
              behavior = Behavior.FileName || behavior = Behavior.Directory
            | _ -> false
          onDoubleClick =
            match pin with
            | StringPin data when data.Behavior = Behavior.FileName ->
              Some (fun _ -> Modal.showFileChooser pin this.props.model this.props.dispatch)
            | StringPin data when data.Behavior = Behavior.Directory ->
              Some (fun _ -> Modal.showDirectoryChooser pin this.props.model this.props.dispatch)
            | _ -> None
          updater = if rowCount > 1 then None else updater
          classes = if rowCount > 1 then [|"disco-flex-1"|] else [||]
          suffix  = if rowCount > 1 then Some(" (" + string rowCount + ")") else None
          title = Pin.tooltip this.props.pin
        }
      if rowCount > 1 then
        td [ClassName "disco-flex-row"] [
          createElement("div", options, this.valueAt(0<index>))
          this.renderArrow()
        ]
      else
        td [] [createElement("div", options, this.valueAt(0<index>))]
    let head =
      tr [ClassName "disco-pin-child"] [
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
          let min =
            match pin with
            | NumberPin data -> data.Min
            | _ -> 0
          let max =
            match pin with
            | NumberPin data -> data.Max
            | _ -> 0
          let options =
            { index = i
              precision = precision
              min = min
              max = max
              properties = properties
              useRightClick = useRightClick
              handleExternally =
                match pin with
                | StringPin { Behavior = behavior } ->
                  behavior = Behavior.FileName || behavior = Behavior.Directory
                | _ -> false
              onDoubleClick =
                match pin with
                | StringPin data when data.Behavior = Behavior.FileName ->
                  Some (fun _ -> Modal.showFileChooser pin this.props.model this.props.dispatch)
                | StringPin data when data.Behavior = Behavior.Directory ->
                  Some (fun _ -> Modal.showDirectoryChooser pin this.props.model this.props.dispatch)
                | _ -> None
              updater = updater
              classes = [||]
              suffix  = None
              title = Pin.tooltip this.props.pin
            }
          yield tr [Key (string i); ClassName "disco-pin-child"] [
            td [] [str label]
            td [] [createElement("div", options, this.valueAt(1<index> * i))]
          ]
      ]
    else tbody [] [head]

  // ** renderArrow

  member this.renderArrow() =
    span [
      classList [
        "disco-icon icon-control",true
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
      [ "disco-pin",           true
        "disco-pin-output",    this.props.output
        "disco-dirty",         not this.props.output && pin.Dirty
        "disco-non-persisted", not pin.Persisted
        "disco-offline",       isOffline
        "disco-pin-selected",  this.props.selected ]
    div [classList classes] [
      table [] [this.renderRows(rowCount, useRightClick, props.updater)]
    ]
