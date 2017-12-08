module Disco.Web.Cues.CueGroupView

open System
open Disco.Core
open Disco.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Disco.Web
open Disco.Web.Notifications
open Types
open Helpers

// * Types

type [<Pojo>] State =
  { IsOpen: bool }

type [<Pojo>] Props =
  { key: string
    Dispatch: Elmish.Dispatch<Msg>
    Model: Model
    State: Disco.Core.State
    Locked: bool
    CueGroup: CueGroup
    CueList: CueList
    CueGroupIndex: int
    CurrentCue: CueRefId option
    SelectedCueIndex: int
    SelectedCueGroupIndex: int
    SelectCueGroup: int -> unit
    SelectCue: int -> int -> unit
  }

// * Sortable components

let private CueSortableItem = Sortable.Element <| fun props ->
  com<CueView.Component,_,_> props.value []

let private CueSortableContainer = Sortable.Container <| fun props ->
    let items =
      props.items |> Array.mapi (fun i (props: CueView.Props) ->
        from CueSortableItem { key=props.key; index=i; value=props } [])
    ul [] (Array.toList items)

// * Helpers

let private renderCues (state: State) (props: Props) =
  let cueProps: CueView.Props[] =
    if true then //state.IsOpen then
      props.CueGroup.CueRefs
      |> Array.mapi (fun i cueRef ->
        { key = string cueRef.Id
          Model = props.Model
          State = props.State
          Locked = props.Locked
          Dispatch = props.Dispatch
          Cue = Lib.findCue cueRef.CueId props.State
          CueRef = cueRef
          CueGroup = props.CueGroup
          CueList = props.CueList
          CueIndex = i
          CurrentCue = props.CurrentCue
          CueGroupIndex = props.CueGroupIndex
          SelectedCueIndex = props.SelectedCueIndex
          SelectedCueGroupIndex = props.SelectedCueGroupIndex
          SelectCue = props.SelectCue
        })
    else [||]
  from CueSortableContainer
    { items = cueProps
      useDragHandle = true
      onSortEnd = fun ev ->
        // Update the CueList with the new CueRefs order
        let newCueRefs = Sortable.arrayMove(props.CueGroup.CueRefs, ev.oldIndex, ev.newIndex)
        { props.CueGroup with CueRefs = newCueRefs }
        |> CueView.updateCueGroup props.CueList
        props.SelectCue props.CueGroupIndex ev.newIndex
    } []


let private renderNameInput (props:Props) =
  if props.Locked then
    str (props.CueGroup.Name |> Option.map unwrap |> Option.defaultValue "")
  else
    Editable.string
      (props.CueGroup.Name |> Option.map unwrap |> Option.defaultValue "&nbsp;")
      (fun txt ->
        let name =
          if String.IsNullOrWhiteSpace txt
          then None
          else Some (name txt)
        { props.CueGroup with Name = name }
        |> CueView.updateCueGroup props.CueList)

let private renderGroupIndex (props:Props) =
  let content = String.Format("{0:0000}", props.CueGroupIndex + 1)
  if props.Locked
  then str content
  else from CueView.SortableHandle { value = content } []

let private renderRemoveButton (props:Props) =
  if props.Locked
  then str ""
  else
    button [
      Class "disco-button disco-icon icon-control icon-close"
      OnClick (fun ev ->
        ev.stopPropagation()
        // Change selection if this item was selected
        if props.CueGroupIndex = props.SelectedCueGroupIndex then
          props.SelectCueGroup 0
        props.CueList
        |> CueList.filterItems (function { Id = id } -> id <> props.CueGroup.Id)
        |> UpdateCueList
        |> ClientContext.Singleton.Post)
    ] []

let private updateAutoFollow (props:Props) =
  props.CueGroup
  |> CueGroup.setAutoFollow (not props.CueGroup.AutoFollow)
  |> flip CueList.replace props.CueList
  |> UpdateCueList
  |> ClientContext.Singleton.Post

let private autocallButton (props:Props) =
  button [
    classList [
      "disco-button disco-icon icon-autocall", true
      "warning", props.CueGroup.AutoFollow
    ]
    Disabled props.Locked
    OnClick (fun _ ->
      // Don't stop propagation to allow the item to be selected
      // ev.stopPropagation()
      updateAutoFollow props)
  ] []

// * React components

type Component(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState({ IsOpen = false })

  member this.render() =
    let arrowButton =
      button [
        classList [
          "disco-button",  true
          "disco-icon",    true
          "icon-control", true
          "icon-less",    this.state.IsOpen
          "icon-more",    not this.state.IsOpen
        ]
        OnClick (fun ev ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          this.setState({ this.state with IsOpen = not this.state.IsOpen }))
      ] []
    let playButton =
      button [
        Class "disco-button disco-icon icon-play"
        OnClick (fun ev ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          Notifications.error "TODO: Call cue group!")
      ] []
    let isSelected =
      this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
    let groupHeadline name =
      div [
        classList ["disco-cuegroup-headline",true]
      ] [
        strong [] [ str (string name)]
      ]
    let groupHeader =
      div [
        classList ["disco-cuegroup-selected", isSelected]
        OnClick (fun _ ->
          if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex then
            this.props.SelectCueGroup this.props.CueGroupIndex  )
      ] [
        div [Class "width-5"] [arrowButton]
        div [Class "width-5"] [playButton]
        div [Class "width-5"] [] // offset
        div [Class "width-10"] [
          renderGroupIndex this.props
        ]
        div [Class "width-20"] [
          renderNameInput this.props
        ]
        div [Class "width-20"] [str "00:00:00"]
        div [Class "width-20"] [str "shortkey"]
        div [Class "width-10"; Style [TextAlign "center"]] [
          autocallButton this.props
        ]
        div [Class "width-5"] [
          renderRemoveButton this.props
        ]
      ]
    div [Class "disco-cuegroup"]
      (match this.state.IsOpen, this.props.CueGroup.Name with
        | false, None -> [groupHeader]
        | false, Some name -> [groupHeadline name; groupHeader]
        | true, None -> [groupHeader; renderCues this.state this.props]
        | true, Some name ->
          [ groupHeadline name
            groupHeader
            renderCues this.state this.props ])
