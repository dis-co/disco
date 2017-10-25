[<RequireQualifiedAccess>]
module Iris.Web.Cues.CueView

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Iris.Web
open Types
open Helpers

// * Helpers

type private RCom = React.ComponentClass<obj>
let  private ContentEditable: RCom = importDefault "../../../js/widgets/ContentEditable"
let  private touchesElement(el: Browser.Element option, x: float, y: float): bool = importMember "../../../js/Util"

let private castValue<'a> arr idx (value: obj) =
  Array.mapi (fun i el -> if i = idx then value :?> 'a else el) arr

let private updateSlicesValue (index: int) (value: obj) slices: Slices =
  match slices with
  | StringSlices(id, client, arr) ->
    StringSlices(id, client, castValue<string> arr index value)
  | NumberSlices(id, client, arr) ->
    NumberSlices(id, client, castValue<double> arr index value)
  | BoolSlices  (id, client, arr) ->
    BoolSlices  (id, client, castValue<bool> arr index value)
  | ByteSlices  (id, client, arr) ->
    ByteSlices  (id, client, castValue<byte[]> arr index value)
  | EnumSlices  (id, client, arr) ->
    EnumSlices  (id, client, castValue<Property> arr index value)
  | ColorSlices (id, client, arr) ->
    ColorSlices (id, client, castValue<ColorSpace> arr index value)

let renderInput (content: string) (update: string->unit) =
  from ContentEditable
    %["tagName" ==> "div"
      "html" ==> content
      "className" ==> "iris-contenteditable"
      "onChange" ==> update] []

let updateCueGroup cueList cueGroup =
  CueList.replace cueGroup cueList
  |> UpdateCueList
  |> ClientContext.Singleton.Post

let isAtomSelected (model: Model) (cueAndPinIds: CueId * PinId) =
  match model.selectedDragItems with
  | DragItems.CueAtoms ids ->
    Seq.exists ((=) cueAndPinIds) ids
  | _ -> false

let onDragStart (model: Model) cueId pinId multiple =
  let newItems = DragItems.CueAtoms [cueId, pinId]
  if multiple then model.selectedDragItems.Append(newItems) else newItems
  |> Drag.start

// * Types

type [<Pojo>] State =
  { IsOpen: bool
    IsHighlit: bool }

type [<Pojo>] Props =
  { key: string
    Model: Model
    State: Iris.Core.State
    Cue: Cue
    CueRef: CueReference
    CueGroup: CueGroup
    CueList: CueList
    CueIndex: int
    CueGroupIndex: int
    SelectedCueIndex: int
    SelectedCueGroupIndex: int
    SelectCue: int -> int -> unit
    Dispatch: Elmish.Dispatch<Msg> }

// * Sortable components

let SortableHandle = Sortable.Handle(fun props ->
  div [Style [Cursor "move"]] [str props.value])

// * React components

type Component(props) =
  inherit React.Component<Props, State>(props)
  let mutable selfRef: Browser.Element option = None
  let mutable disposable: IDisposable option = None
  do base.setInitState({ IsOpen = false; IsHighlit = false })

  member this.componentWillUnmount() =
    disposable |> Option.iter (fun disp -> disp.Dispose())

  member this.componentDidMount() =
    disposable <-
      Drag.observe()
      |> Observable.choose(function
        | Drag.Moved(x,y,data) -> Some(data,x,y,false)
        | Drag.Stopped(x,y,data) -> Some(data,x,y,true))
      |> Observable.subscribe(fun (data,x,y,stopped) ->
        let isHighlit, isOpen =
          if touchesElement(selfRef, x, y) then
            if not stopped then
              true, this.state.IsOpen
            else
              match data with
              | DragItems.Pins pinIds ->
                Seq.map (fun id -> Lib.findPin id this.props.State) pinIds
                |> Lib.addSlicesToCue this.props.Cue
                |> Lib.postStateCommands
              | DragItems.CueAtoms ids ->
                let addCommands =
                  Seq.map (fun (_, pid) -> Lib.findPin pid this.props.State) ids
                  |> Lib.addSlicesToCue this.props.Cue
                // Group id tuples by CueId (first one)
                (addCommands, Seq.groupBy fst ids) ||> Seq.fold (fun cmds (cueId, ids) ->
                  if cueId <> this.props.Cue.Id then
                    let cue = Lib.findCue cueId this.props.State
                    Lib.removeSlicesFromCue cue (Seq.map snd ids)
                    |> cons cmds
                  else cmds)
                |> Lib.postStateCommands
              false, true
          else
            false, this.state.IsOpen
        if isHighlit <> this.state.IsHighlit || isOpen <> this.state.IsOpen then
          this.setState({ this.state with IsOpen = isOpen; IsHighlit = isHighlit })
      ) |> Some

  member this.render() =
    let arrowButton =
      button [
        Class ("iris-button iris-icon icon-control " + (if this.state.IsOpen then "icon-less" else "icon-more"))
        OnClick (fun _ ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          this.setState({ this.state with IsOpen = not this.state.IsOpen}))
      ] []
    let playButton =
      button [
        Class "iris-button iris-icon icon-play"
        OnClick (fun _ ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          CallCue this.props.Cue |> ClientContext.Singleton.Post
        )
      ] []
    let autocallButton =
      button [
        Class "iris-button iris-icon icon-autocall"
        OnClick (fun _ ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          Browser.window.alert("Auto call cue!")
        )
      ] []
    let removeButton =
      button [
        Class "iris-button iris-icon icon-control icon-close"
        OnClick (fun ev ->
          ev.stopPropagation()
          let id = this.props.CueRef.Id
          // Change selection if this item was selected
          if this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
            && this.props.CueIndex = this.props.SelectedCueIndex then
            this.props.SelectCue this.props.CueGroupIndex 0
          let newCueRefs =
            this.props.CueGroup.CueRefs
            |> Array.filter (fun c -> c.Id <> id)
          { this.props.CueGroup with CueRefs = newCueRefs }
          |> updateCueGroup this.props.CueList)
      ] []
    let cueHeader =
      div [
        OnClick (fun _ ->
          Select.cue this.props.Dispatch this.props.Cue
          if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex
            || this.props.CueIndex <> this.props.SelectedCueIndex then
            this.props.SelectCue this.props.CueGroupIndex this.props.CueIndex  )
      ] [
        div [Class "width-5"] [] // offset
        div [Class "width-5"] [arrowButton]
        div [Class "width-5"] [playButton]
        div [Class "width-10"] [
          from SortableHandle
            { value = String.Format("{0:0000}", this.props.CueIndex + 1) } []
        ]
        div [Class "width-20"] [
          renderInput (unwrap this.props.Cue.Name) (fun txt ->
            { this.props.Cue with Name = name txt }
            |> UpdateCue |> ClientContext.Singleton.Post)
        ]
        div [Class "width-20"] [str "00:00:00"]
        div [Class "width-20"] [str "shortkey"]
        div [Class "width-10"; Style [TextAlign "center"]] [autocallButton]
        div [Class "width-5"] [removeButton]
      ]
    let rows =
      if not this.state.IsOpen then
        [cueHeader]
      else
        let { Model = model
              State = state
              Dispatch = dispatch
              Cue = cue } = this.props
        let pinGroups =
          cue.Slices
          |> Array.mapi (fun i slices -> i, Lib.findPin slices.PinId state, slices)
          |> Array.groupBy (fun (_, pin, _) -> pin.PinGroupId)
          |> Array.map(fun (pinGroupId, pinAndSlices) ->
            let pinGroup = Lib.findPinGroup pinGroupId state
            li [Key (string pinGroupId)] [
              div [] [str (unwrap pinGroup.Name)]
              // Use iris-wrap class to cancel the effects of iris-table wrapping CSS rules
              div [Class "iris-wrap"] (pinAndSlices |> Seq.map (fun (i, pin, slices) ->
                com<PinView.PinView,_,_>
                  { key = string pin.Id
                    pin = pin
                    output = false
                    selected = isAtomSelected model (cue.Id, pin.Id)
                    slices = Some slices
                    model = model
                    updater =
                      if Lib.isMissingPin pin
                      then None
                      else Some {
                        new IUpdater with
                          member __.Update(dragging, valueIndex, value) =
                            this.updateCueValue(dragging, i, valueIndex, value)
                      }
                    onSelect = fun multi ->
                      Select.pin dispatch pin
                      Drag.selectCueAtom dispatch multi cue.Id pin.Id
                    onDragStart = Some(onDragStart model cue.Id pin.Id)
                  } []) |> Seq.toList)
            ])
          |> Array.toList
        [cueHeader; div [] [ul [Class "iris-graphview"] pinGroups]]
    let isSelected =
      this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
        && this.props.CueIndex = this.props.SelectedCueIndex
    let isHighlit = this.state.IsHighlit
    div [
      classList ["iris-cue", true
                 "iris-cue-selected", isSelected
                 "iris-highlight", isHighlit]
      Ref (fun el -> selfRef <- Option.ofObj el)
    ] rows

  member this.updateCueValue(local: bool, sliceIndex: int, valueIndex: int, value: obj) =
    let newSlices =
      this.props.Cue.Slices |> Array.mapi (fun i slices ->
        if i = sliceIndex then updateSlicesValue valueIndex value slices else slices)
    let command =
      { this.props.Cue with Slices = newSlices } |> UpdateCue
    if local
    then ClientContext.Singleton.PostLocal command
    else ClientContext.Singleton.Post command
