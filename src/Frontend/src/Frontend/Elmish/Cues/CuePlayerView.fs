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

// ** Types

type CG = CueGroupView.Props

type [<Pojo>] Props =
  { CueList: CueList option
    Model: Model
    Dispatch: Msg->unit
    Id: Guid
    Name: string }

type [<Pojo>] State =
  { SelectedCueGroupIndex: int
    SelectedCueIndex: int }

// ** Helpers

let private cueListMockup() =
  let cueGroup =
    { Id = IrisId.Create()
      Name = name "MockCueGroup"
      CueRefs = [||] }
  { Id = IrisId.Create()
    Name = name "MockCueList"
    Items = [| CueGroup cueGroup |] }

let addGroup (cueList: CueList) (cueGroupIndex: int) =
  let cueGroup =
    { Id = IrisId.Create()
      Name = name "Untitled"
      CueRefs = [||] }
  // TODO: This doesn't work when selecting the last group is selected
  // CueList.insertAfter cueGroupIndex (CueGroup cueGroup) cueList
  let newItems =
    Lib.insertAfter cueGroupIndex (CueGroup cueGroup) cueList.Items
  { cueList with Items = newItems }
  |> UpdateCueList
  |> ClientContext.Singleton.Post

// ** React components

type Component(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState({ SelectedCueGroupIndex = -1; SelectedCueIndex = -1})

  member this.renderGroups() =
    match this.props.CueList, this.props.Model.state with
    | Some cueList, Some state ->
      cueList.Items |> Array.mapi (fun i item ->
        match item with
        | Headline _ ->
          printfn "TODO: Render Cue Headlines"
          None
        | CueGroup group ->
          com<CueGroupView.Component,_,_>
            { CG.key = string group.Id
              CG.Dispatch = this.props.Dispatch
              CG.Model = this.props.Model
              CG.State = state
              CG.CueGroup = group
              CG.CueList = cueList
              CG.CueGroupIndex = i
              CG.SelectedCueIndex = this.state.SelectedCueIndex
              CG.SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
              CG.SelectCueGroup = fun g -> this.setState({ SelectedCueGroupIndex = g; SelectedCueIndex = -1 })
              CG.SelectCue = fun g c -> this.setState({ SelectedCueGroupIndex = g; SelectedCueIndex = c })
            } [] |> Some
      )
      |> Array.choose id
    | _ -> [||]

  member this.renderBody() =
    ul [] [
      yield li [Key "header"] [
        // Three columns for arrow icon and play button
        // (The extra column is for the cue offset)
        div [Class "width-5"] [str ""]
        div [Class "width-5"] [str ""]
        div [Class "width-5"] [str ""]

        // Central columns: position, name...
        div [Class "width-10"] [str "Nr."]
        div [Class "width-20"] [str "Name"]
        div [Class "width-20"] [str "Delay"]
        div [Class "width-20"] [str "Trigger"]

        // Other buttons: autocall, remove
        // TODO: Add copy button
        div [Class "width-10"; Style [TextAlign "center"]] [str "Autocall"]
        div [Class "width-5"] [str ""]
      ]
      yield! this.renderGroups()
    ]

  member this.renderTitleBar() =
    // TODO: Use a dropdown to choose the CueList
    div [] [
      button [
        Class "iris-button"
        Disabled (Option.isNone this.props.CueList)
        OnClick (fun _ ->
          match this.props.CueList with
          | Some cueList -> addGroup cueList this.state.SelectedCueGroupIndex
          | None -> ())
      ] [str "Add Group"]
      button [
        Class "iris-button"
        Disabled (Option.isNone this.props.CueList)
        OnClick (fun _ ->
          match this.props.CueList with
          | Some cueList ->
            // TODO: Open group automatically
            Lib.addCue cueList this.state.SelectedCueGroupIndex this.state.SelectedCueIndex
          | None -> ())
      ] [str "Add Cue"]
    ]

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
