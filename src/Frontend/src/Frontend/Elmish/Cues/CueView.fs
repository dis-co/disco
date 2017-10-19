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

// ** Helpers

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

// ** Types

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

// ** Sortable components

let SortableHandle = Sortable.Handle(fun props ->
  div [Style [Cursor "move"]] [str props.value])

// ** React components

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
        | Drag.Moved(x,y,Drag.Pin pins) -> Some(pins,x,y,false)
        | Drag.Stopped(x,y,Drag.Pin pins) -> Some(pins,x,y,true))
      |> Observable.subscribe(fun (pins,x,y,stopped) ->
        let isHighlit, isOpen =
          if touchesElement(selfRef, x, y) then
            if not stopped then
              true, this.state.IsOpen
            else
              // Filter out output pins and pins already contained by the cue
              let sliceses =
                pins |> Seq.choose (fun pin ->
                  if isOutputPin pin then
                    None
                  else
                    let id = pin.Id
                    let existing = this.props.Cue.Slices |> Array.exists (fun slices -> slices.PinId = id)
                    if existing then
                      printfn "The cue already contains pin %O" id
                      None
                    else Some pin.Slices)
                |> Seq.toArray
              let newCue = { this.props.Cue with Slices = Array.append this.props.Cue.Slices sliceses }
              UpdateCue newCue |> ClientContext.Singleton.Post
              false, true
          else
            false, this.state.IsOpen
        if isHighlit <> this.state.IsHighlit || isOpen <> this.state.IsOpen then
          this.setState({ this.state with IsOpen = isOpen; IsHighlit = isHighlit })
      ) |> Some

  member this.renderInput(content: string, ?update: string->unit) =
    match update with
    | Some update ->
      from ContentEditable
        %["tagName" ==> "span"
          "html" ==> content
          "onChange" ==> update] []
    | None -> span [] [str content]

  member this.render() =
    let arrowButton =
      button [
        Class ("iris-button iris-icon icon-control " + (if this.state.IsOpen then "icon-less" else "icon-more"))
        OnClick (fun ev ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          this.setState({ this.state with IsOpen = not this.state.IsOpen}))
      ] []
    let playButton =
      button [
        Class "iris-button iris-icon icon-play"
        OnClick (fun ev ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          CallCue this.props.Cue |> ClientContext.Singleton.Post
        )
      ] []
    let autocallButton =
      button [
        Class "iris-button iris-icon icon-autocall"
        OnClick (fun ev ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          Browser.window.alert("Auto call!")
        )
      ] []
    let removeButton =
      button [
        Class "iris-button iris-icon icon-control icon-close"
        OnClick (fun ev ->
          ev.stopPropagation()
          let id = this.props.CueRef.Id
          // Change selection if this item was selected
          if this.props.CueGroupIndex = this.props.SelectedCueGroupIndex then
            this.props.SelectCue this.props.CueGroupIndex 0
          let cueGroup = {
            this.props.CueGroup with
              CueRefs =
                this.props.CueGroup.CueRefs
                |> Array.filter (fun c -> c.Id <> id)
          }
          this.props.CueList
          |> CueList.replace (CueGroup cueGroup)
          |> UpdateCueList
          |> ClientContext.Singleton.Post)
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
          from SortableHandle { value = String.Format("{0:0000}", this.props.CueIndex + 1)} []
        ]
        div [Class "width-20"] [
          this.renderInput(unwrap this.props.Cue.Name, (fun txt ->
            { this.props.Cue with Name = name txt } |> UpdateCue |> ClientContext.Singleton.Post))
        ]
        div [Class "width-20"] [this.renderInput("00:00:00")]
        div [Class "width-20"] [this.renderInput("shortkey")]
        div [Class "width-10"; Style [TextAlign "center"]] [autocallButton]
        div [Class "width-5"] [removeButton]
      ]
    let rows =
      if not this.state.IsOpen then
        [cueHeader]
      else
        let pinGroups =
          this.props.Cue.Slices
          |> Array.mapi (fun i slices -> i, Lib.findPin slices.PinId this.props.State, slices)
          |> Array.groupBy (fun (_, pin, _) -> pin.PinGroupId)
          |> Array.map(fun (pinGroupId, pinAndSlices) ->
            let pinGroup = Lib.findPinGroup pinGroupId this.props.State
            li [Key (string pinGroupId)] [
              div [] [str (unwrap pinGroup.Name)]
              // Use iris-wrap class to cancel the effects of iris-table wrapping CSS rules
              div [Class "iris-wrap"] (pinAndSlices |> Seq.map (fun (i, pin, slices) ->
                com<PinView.PinView,_,_>
                  { key = string pin.Id
                    pin = pin
                    output = false
                    slices = Some slices
                    model = this.props.Model
                    updater =
                      if Lib.isMissingPin pin
                      then None
                      else Some { new IUpdater with
                                      member __.Update(dragging, valueIndex, value) =
                                        this.updateCueValue(dragging, i, valueIndex, value) }
                    onSelect = fun multiple -> Select.pin this.props.Dispatch multiple pin
                    onDragStart = None
                  } []) |> Seq.toList)
            ])
          |> Array.toList
        [cueHeader; div [] [ul [Class "iris-graphview"] pinGroups]]
    let isSelected =
      this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
        && this.props.CueIndex = this.props.SelectedCueIndex
    let isHighlit = this.state.IsHighlit
    // Set min-width so the row doesn't look too compressed when dragging
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
