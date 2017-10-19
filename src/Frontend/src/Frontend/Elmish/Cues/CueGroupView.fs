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
    SelectGroup: int -> unit
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
          let newCueGroup = { props.CueGroup with CueRefs = Sortable.arrayMove(props.CueGroup.CueRefs, ev.oldIndex, ev.newIndex) }
          let newCueList = CueList.replace (CueGroup newCueGroup) props.CueList
          UpdateCueList newCueList |> ClientContext.Singleton.Post
          props.SelectCue props.CueGroupIndex ev.newIndex
      } []

// ** React components

type Component(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState({ IsOpen = false })

  member this.render() =
    renderCues this.state this.props