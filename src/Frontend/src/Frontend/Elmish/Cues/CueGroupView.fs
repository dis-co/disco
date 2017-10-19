module Iris.Web.Cues.CueGroupView

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

// ** Types

type [<Pojo>] State =
  { IsOpen: bool }

type [<Pojo>] Props =
  { key: string
    Dispatch: Elmish.Dispatch<Msg>
    Model: Model
    State: Iris.Core.State
    CueGroup: CueGroup
    CueList: CueList
    CueGroupIndex: int
    SelectedCueIndex: int
    SelectedCueGroupIndex: int
    SelectCueGroup: int -> unit
    SelectCue: int -> int -> unit
  }

// ** Sortable components

let private CueSortableItem = Sortable.Element <| fun props ->
  com<CueView.Component,_,_> props.value []

let private CueSortableContainer = Sortable.Container <| fun props ->
    let items =
      props.items |> Array.mapi (fun i (props: CueView.Props) ->
        from CueSortableItem { key=props.key; index=i; value=props } [])
    ul [] (Array.toList items)

// ** Helpers

let private renderCues (state: State) (props: Props) =
    let cueProps: CueView.Props[] =
      if true then //state.IsOpen then
        props.CueGroup.CueRefs
        |> Array.mapi (fun i cueRef ->
          { key = string cueRef.Id
            Model = props.Model
            State = props.State
            Dispatch = props.Dispatch
            Cue = Lib.findCue cueRef.CueId props.State
            CueRef = cueRef
            CueGroup = props.CueGroup
            CueList = props.CueList
            CueIndex = i
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

// ** React components

type Component(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState({ IsOpen = false })

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
          Browser.window.alert("Call cue group!"))
      ] []
    let autocallButton =
      button [
        Class "iris-button iris-icon icon-autocall"
        OnClick (fun ev ->
          // Don't stop propagation to allow the item to be selected
          // ev.stopPropagation()
          Browser.window.alert("Auto call cue group!"))
      ] []
    let removeButton =
      button [
        Class "iris-button iris-icon icon-control icon-close"
        OnClick (fun ev ->
          ev.stopPropagation()
          // Change selection if this item was selected
          if this.props.CueGroupIndex = this.props.SelectedCueGroupIndex then
            this.props.SelectCueGroup 0
          let gid = this.props.CueGroup.Id
          this.props.CueList |> CueList.filterItems (function
            | CueGroup g -> g.Id <> gid
            | _ -> false)
          |> UpdateCueList
          |> ClientContext.Singleton.Post)
      ] []
    let isSelected =
      this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
    let groupHeader =
      div [
        classList ["iris-cuegroup-selected", isSelected]
        OnClick (fun _ ->
          if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex then
            this.props.SelectCueGroup this.props.CueGroupIndex  )
      ] [
        div [Class "width-5"] [arrowButton]
        div [Class "width-5"] [playButton]
        div [Class "width-5"] [] // offset
        div [Class "width-10"] [
          from CueView.SortableHandle
            { value = String.Format("{0:0000}", this.props.CueGroupIndex + 1) } []]
        div [Class "width-20"] [
          CueView.renderInput (unwrap this.props.CueGroup.Name) (fun txt ->
            assert false
            { this.props.CueGroup with Name = name txt }
            |> CueView.updateCueGroup this.props.CueList)
        ]
        div [Class "width-20"] [str "00:00:00"]
        div [Class "width-20"] [str "shortkey"]
        div [Class "width-10"; Style [TextAlign "center"]] [autocallButton]
        div [Class "width-5"] [removeButton]
      ]
    div [Class "iris-cuegroup"]
      (if not this.state.IsOpen
       then [groupHeader]
       else [groupHeader; renderCues this.state this.props])
