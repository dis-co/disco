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

// * Types

type CG = CueGroupView.Props

type [<Pojo>] Props =
  { CueList: CueList option
    Model: Model
    Dispatch: Msg->unit
    Id: Guid
    Name: string
    Player: CuePlayer option }

type [<Pojo>] State =
  { SelectedCueGroupIndex: int
    SelectedCueIndex: int }

// * Sortable components

let private CueGroupSortableItem = Sortable.Element <| fun props ->
  com<CueGroupView.Component,_,_> props.value []

let private CueGroupSortableContainer = Sortable.Container <| fun props ->
    let items =
      props.items |> Array.mapi (fun i (props: CueGroupView.Props) ->
        from CueGroupSortableItem { key=props.key; index=i; value=props } [])
    ul [] (Array.toList items)

// * Helpers

let private addGroup (cueList: CueList) (cueGroupIndex: int) =
  let cueGroup = CueGroup.create [| |]
  cueList
  |> CueList.insertAfter cueGroupIndex cueGroup
  |> UpdateCueList
  |> ClientContext.Singleton.Post

let private toggleLocked (player: CuePlayer option) =
  match player with
  | None -> ()
  | Some player ->
    player
    |> CuePlayer.setLocked (not player.Locked)
    |> UpdateCuePlayer
    |> ClientContext.Singleton.Post

let private updateCueList (player: CuePlayer option) (str:string) =
  match player with
  | None -> ()
  | Some player ->
    str |> IrisId.TryParse |> function
      | Either.Left _ ->
        CuePlayer.unsetCueList player
        |> UpdateCuePlayer
        |> ClientContext.Singleton.Post
      | Either.Right id ->
        CuePlayer.setCueList id player
        |> UpdateCuePlayer
        |> ClientContext.Singleton.Post

let private isLocked (player: CuePlayer option) =
  player
  |> Option.map CuePlayer.locked
  |> Option.defaultValue false

let private spacer space =
  div [ Style [ Width (string space + "px")
                Display "inline-block" ] ]
      []

let private nextItem cueList =
  printfn "Next Item please"

let private previousItem cueList =
  printfn "Previous Item please"

// * React components
// ** EmptyComponent

type EmptyComponent(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState({ SelectedCueGroupIndex = -1; SelectedCueIndex = -1})

  member this.render() =
    let sadString =
      div [ Style [ TextAlign "center"; PaddingTop "20px" ] ] [
        strong [] [
          str (sprintf "Could not find %A." this.props.Id)
        ]
      ]
    widget this.props.Id this.props.Name
      None
      (fun _ _ -> sadString)
      this.props.Dispatch
      this.props.Model

// ** createEmpty

let private createEmpty dispatch model id name =
  com<EmptyComponent,_,_> {
    CueList = None
    Model = model
    Dispatch = dispatch
    Id = id
    Name = name
    Player = None
  } []

// ** Component

type Component(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState({ SelectedCueGroupIndex = -1; SelectedCueIndex = -1})

  // *** renderGroups

  member this.renderGroups() =
    match this.props.CueList, this.props.Model.state with
    | Some cueList, Some state ->
      let itemProps =
        cueList.Items
        |> Array.mapi (fun i group ->
          { CG.key = string group.Id
            CG.Dispatch = this.props.Dispatch
            CG.Model = this.props.Model
            CG.Locked = isLocked this.props.Player
            CG.State = state
            CG.CueGroup = group
            CG.CueList = cueList
            CG.CueGroupIndex = i
            CG.SelectedCueIndex = this.state.SelectedCueIndex
            CG.SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
            CG.SelectCueGroup = fun g -> this.setState({ SelectedCueGroupIndex = g; SelectedCueIndex = -1 })
            CG.SelectCue = fun g c -> this.setState({ SelectedCueGroupIndex = g; SelectedCueIndex = c })
          } |> Some)
        |> Array.choose id
      from CueGroupSortableContainer
        { items = itemProps
          useDragHandle = true
          onSortEnd = fun ev ->
            // Update the CueList with the new order
            let newItems = Sortable.arrayMove(cueList.Items, ev.oldIndex, ev.newIndex)
            { cueList with Items = newItems }
            |> UpdateCueList |> ClientContext.Singleton.Post
            // Select the dragged group
            { this.state with SelectedCueGroupIndex = ev.newIndex }
            |> this.setState
        } [] |> Some
    | _ -> None

  // *** renderBody

  member this.renderBody() =
    ul [] [
      li [Key "header"] [
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
      opt <| this.renderGroups()
    ]

  // *** renderTitleBar

  member this.renderTitleBar() =
    let empty = "--"
    let current =
      this.props.CueList
      |> Option.map (CueList.id >> string)
      |> Option.defaultValue empty

    let makeOpt (id,name) =
      option [ Value id ] [ str name ]

    let cueLists =
      this.props.Model.state
      |> Option.map
        (State.cueLists
         >> Map.toList
         >> List.map (fun (id,cueList) -> string id,string cueList.Name))
      |> Option.defaultValue List.empty
      |> List.map makeOpt
      |> fun list -> makeOpt (empty,empty) :: list

    let locked = isLocked this.props.Player

    div [] [
      ///     _       _     _    ____
      ///    / \   __| | __| |  / ___|_ __ ___  _   _ _ __
      ///   / _ \ / _` |/ _` | | |  _| '__/ _ \| | | | '_ \
      ///  / ___ \ (_| | (_| | | |_| | | | (_) | |_| | |_) |
      /// /_/   \_\__,_|\__,_|  \____|_|  \___/ \__,_| .__/
      ///                                            |_|
      button [
        Class "iris-button"
        Disabled (Option.isNone this.props.CueList || locked)
        OnClick (fun _ ->
          match this.props.CueList with
          | Some cueList -> addGroup cueList this.state.SelectedCueGroupIndex
          | None -> ())
      ] [str "Add Group"]

      ///     _       _     _    ____
      ///    / \   __| | __| |  / ___|   _  ___
      ///   / _ \ / _` |/ _` | | |  | | | |/ _ \
      ///  / ___ \ (_| | (_| | | |__| |_| |  __/
      /// /_/   \_\__,_|\__,_|  \____\__,_|\___|
      button [
        Class "iris-button"
        Disabled (Option.isNone this.props.CueList || locked)
        OnClick (fun _ ->
          match this.props.CueList with
          | Some cueList ->
            // TODO: Open group automatically
            Lib.addCue cueList this.state.SelectedCueGroupIndex this.state.SelectedCueIndex
          | None -> ())
      ] [str "Add Cue"]

      ///  _               _
      /// | |    ___   ___| | __
      /// | |   / _ \ / __| |/ /
      /// | |__| (_) | (__|   <
      /// |_____\___/ \___|_|\_\
      button [
        classList [
          "iris-button",true
          "warning", locked
        ]
        OnClick (fun _ -> toggleLocked this.props.Player)
      ] [
        i [
          classList [
            "fa fa-lg", true
            "fa-lock", locked
            "fa-unlock", not locked
          ]
          Style [
            LineHeight "14px"
            FontSize "1.11111111em"
          ]
        ] [ ]
      ]

      ///   ____           _     _     _
      ///  / ___|   _  ___| |   (_)___| |_
      /// | |  | | | |/ _ \ |   | / __| __|
      /// | |__| |_| |  __/ |___| \__ \ |_
      ///  \____\__,_|\___|_____|_|___/\__|
      select [
        Class "iris-control iris-select"
        Value current
        Disabled locked
        OnChange (fun ev -> updateCueList this.props.Player !!ev.target?value)
      ] cueLists

      ///  _   _           _
      /// | \ | | _____  _| |_
      /// |  \| |/ _ \ \/ / __|
      /// | |\  |  __/>  <| |_
      /// |_| \_|\___/_/\_\\__|
      button [
        Class "iris-button pull-right"
        Disabled (Option.isNone this.props.CueList)
        OnClick (fun _ ->
          match this.props.CueList with
          | Some cueList -> nextItem cueList
          | None -> ())
      ] [
        str "Next"
        spacer 5
        i [
          Class "fa fa-lg fa-forward"
          Style [
            LineHeight "14px"
            FontSize "1.11111111em"
          ]
        ] [ ]
      ]

      ///  ____                 _
      /// |  _ \ _ __ _____   _(_) ___  _   _ ___
      /// | |_) | '__/ _ \ \ / / |/ _ \| | | / __|
      /// |  __/| | |  __/\ V /| | (_) | |_| \__ \
      /// |_|   |_|  \___| \_/ |_|\___/ \__,_|___/
      button [
        Class "iris-button pull-right"
        Disabled (Option.isNone this.props.CueList)
        OnClick (fun _ ->
          match this.props.CueList with
          | Some cueList -> previousItem cueList
          | None -> ())
      ] [
        i [
          Class "fa fa-lg fa-backward"
          Style [
            LineHeight "14px"
            FontSize "1.11111111em"
          ]
        ] [ ]
        spacer 5
        str "Previous"
      ]
    ]

  // *** render

  member this.render() =
    widget this.props.Id this.props.Name
      (Some (fun _ _ -> this.renderTitleBar()))
      (fun _ _ -> this.renderBody()) this.props.Dispatch this.props.Model

  // *** shouldComponentUpdate

  member this.shouldComponentUpdate(nextProps: Props, nextState: State) =
    this.state <> nextState
      || distinctRef this.props.Model.selectedDragItems nextProps.Model.selectedDragItems
      || match this.props.Model.state, nextProps.Model.state with
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
      match model.state with
      | None -> createEmpty dispatch model this.Id this.Name
      | Some state ->
        match Map.tryFind (IrisId.FromGuid id) state.CuePlayers with
        | None -> createEmpty dispatch model this.Id this.Name
        | Some player ->
          let cueList =
            Option.bind
              (flip Map.tryFind state.CueLists)
              player.CueListId
          com<Component,_,_>
            { CueList = cueList
              Model = model
              Dispatch = dispatch
              Id = this.Id
              Name = this.Name
              Player = Some player } []
  }
