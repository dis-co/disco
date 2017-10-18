[<RequireQualifiedAccess>]
module Iris.Web.Cues.CuePlayerView

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

let private cueListMockup() =
  let cueGroup =
    { Id = IrisId.Create()
      Name = name "MockCueGroup"
      CueRefs = [||] }
  { Id = IrisId.Create()
    Name = name "MockCueList"
    Items = [| CueGroup cueGroup |] }

// ** Types

type [<Pojo>] Props =
  { CueList: CueList option
    Model: Model
    Dispatch: Msg->unit
    Id: Guid
    Name: string }

type [<Pojo>] State =
  { SelectedCueGroupIndex: int
    SelectedCueIndex: int }

// ** Sortable components

let private CueSortableItem = Sortable.Element <| fun props ->
  com<CueView.Component,_,_> props.value []

let private CueSortableContainer = Sortable.Container <| fun props ->
    let items =
      props.items |> Array.mapi (fun i (props: CueView.Props) ->
        from CueSortableItem { key=props.key; index=i; value=props } [])
    tbody [] (Array.toList items)

// ** React components

type Component(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState({ SelectedCueGroupIndex = -1; SelectedCueIndex = -1})

  member this.renderCues() =
    match this.props.CueList, this.props.Model.state with
    | Some cueList, Some state ->
      // TODO: Temporarily assume just one group
      match Seq.tryHead cueList.Items with
      | Some (CueGroup group) ->
        let cueProps: CueView.Props[] =
          group.CueRefs
          |> Array.mapi (fun i cueRef ->
              { key = string cueRef.Id
                Model = this.props.Model
                State = state
                Dispatch = this.props.Dispatch
                Cue = Lib.findCue cueRef.CueId state
                CueRef = cueRef
                CueGroup = group
                CueList = cueList
                CueIndex = i
                CueGroupIndex = 0 // TODO: this.props.CueGroupIndex
                SelectedCueIndex = this.state.SelectedCueIndex
                SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
                SelectCue = fun g c -> this.setState({ SelectedCueGroupIndex = g; SelectedCueIndex = c }) })
        Some(from CueSortableContainer
              { items = cueProps
                useDragHandle = true
                onSortEnd = fun ev ->
                  // Update the CueList with the new CueRefs order
                  let newCueGroup = { group with CueRefs = Sortable.arrayMove(group.CueRefs, ev.oldIndex, ev.newIndex) }
                  let newCueList = CueList.replace (CueGroup newCueGroup) cueList
                  UpdateCueList newCueList |> ClientContext.Singleton.Post
                  // TODO: CueGroupIndex
                  this.setState({ SelectedCueGroupIndex = 0; SelectedCueIndex = ev.newIndex })
              } [])
      | _ -> None
    | _ -> None

  member this.renderBody() =
    table [Class "iris-table"] [
      thead [Key "header"] [
        tr [] [
          th [Class "width-5"] [str ""]
          th [Class "width-5"] [str ""]
          th [Class "width-10"] [str "Nr."]
          th [Class "width-25"] [str "Cue name"]
          th [Class "width-20"] [str "Delay"]
          th [Class "width-20"] [str "Trigger"]
          th [Class "width-10"; Style [TextAlign "center"]] [str "Autocall"]
          th [Class "width-5"] [str ""]
        ]
      ]
      opt (this.renderCues())
    ]

  member this.renderTitleBar() =
    // TODO: Use a dropdown to choose the player/list
    button [
      Class "iris-button"
      Disabled (Option.isNone this.props.CueList)
      OnClick (fun _ ->
        match this.props.CueList with
        | Some cueList -> Lib.addCue cueList this.state.SelectedCueGroupIndex this.state.SelectedCueIndex
        | None -> ())
    ] [str "Add Cue"]

  member this.render() =
    widget this.props.Id this.props.Name
      (Some (fun _ _ -> this.renderTitleBar()))
      (fun _ _ -> this.renderBody()) this.props.Dispatch this.props.Model

  member this.shouldComponentUpdate(nextProps: Props, nextState: State) =
    this.state <> nextState ||
      match this.props.Model.state, nextProps.Model.state with
      | Some s1, Some s2 ->
        distinctRef s1.CueLists s2.CueLists
          || distinctRef s1.CuePlayers s2.CuePlayers
          || distinctRef s1.Cues s2.Cues
          || distinctRef s1.PinGroups s2.PinGroups
      | None, None -> false
      | _ -> true

// ** createWidget

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.CuePlayer
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0;
        w = 8; h = 5;
        minW = 4; maxW = 20
        minH = 4; maxH = 20 }
    member this.Render(dispatch, model) =
      let cueList =
        model.state |> Option.bind (fun state ->
          match Seq.tryHead state.CueLists with
          // TODO: Mock code, create player if it doesn't exist
          | None -> cueListMockup() |> AddCueList |> ClientContext.Singleton.Post; None
          | Some kv -> Some kv.Value)
      com<Component,_,_>
        { CueList = cueList
          Model = model
          Dispatch = dispatch
          Id = this.Id
          Name = this.Name } []
  }
