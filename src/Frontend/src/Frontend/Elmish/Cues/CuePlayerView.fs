[<RequireQualifiedAccess>]
module Disco.Web.Cues.CuePlayerView

open System
open Disco.Core
open Disco.Core.Commands
open Disco.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Disco.Web
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
    Player: CuePlayer option
    CurrentCue: CueRefId option }

type [<Pojo>] State =
  { ContextMenuActive: bool
    SelectedCueGroupIndex: int
    SelectedCueIndex: int }

let private defaultState =
  { ContextMenuActive = false
    SelectedCueGroupIndex = -1
    SelectedCueIndex = -1 }

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

let private toggleLocked (player: CuePlayer) =
  player
  |> CuePlayer.setLocked (not player.Locked)
  |> UpdateCuePlayer
  |> ClientContext.Singleton.Post

let private updateCueList (player: CuePlayer option) (str:string) =
  match player with
  | None -> ()
  | Some player ->
    str |> DiscoId.TryParse |> function
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

let private nextItem (props:Props) =
  match props.Model.state, props.Player, props.CueList with
  | Some state, Some player, Some cueList ->
    let nextIdx = int (player.Selected + 1<index>)
    if nextIdx < CueList.cueCount cueList then
      try
        cueList
        |> CueList.cueRefs
        |> Array.item nextIdx
        |> CueReference.cueId
        |> flip State.cue state
        |> Option.map (flip CuePlayer.next player)
        |> Option.iter (CommandBatch.toList >> List.iter ClientContext.Singleton.Post)
      with _ -> ()
  | _ -> ()

let private previousItem (props:Props) =
  match props.Model.state, props.Player, props.CueList with
  | Some state, Some player, Some cueList ->
    let previousIdx = int (player.Selected - 1<index>)
    if 0 <= previousIdx then
      try
        cueList
        |> CueList.cueRefs
        |> Array.item previousIdx
        |> CueReference.cueId
        |> flip State.cue state
        |> Option.map (flip CuePlayer.previous player)
        |> Option.iter (CommandBatch.toList >> List.iter ClientContext.Singleton.Post)
      with _ -> ()
  | _ -> ()

let private withGivenState (props:Props) f =
  match props.Model.state, props.Player, props.CueList with
  | Some state, Some player, Some cueList when not player.Locked ->
    f state player cueList
  | _ -> None

let private createContextMenu active onOpen (state:State) (props:Props) =
  let addGroup =
    withGivenState props <| fun _ player cueList ->
      Some("Add Group", fun () -> addGroup cueList state.SelectedCueGroupIndex)

  let createCue =
    withGivenState props <| fun _ player cueList ->
      Some("Create Cue",
           fun () -> Lib.groupCreateCue cueList state.SelectedCueGroupIndex state.SelectedCueIndex)

  let addCue =
    withGivenState props <| fun globalState player cueList ->
      let cues =
        globalState
        |> State.cues
        |> Map.toArray
        |> Array.map snd
        |> Array.sortBy (fun { Name = name } -> name)
      Some("Add Cues",
           fun () ->
            Modal.InsertCues(cues, cueList, state.SelectedCueGroupIndex, state.SelectedCueIndex)
            :> IModal
            |> OpenModal
            |> props.Dispatch)

  let duplicateCue =
    withGivenState props <| fun globalState player cueList ->
      Some("Duplicate Cue",
           fun () ->
            Lib.duplicateCue
              globalState
              cueList
              state.SelectedCueGroupIndex
              state.SelectedCueIndex)

  let toggleLocked =
    match props.Player with
    | Some player when player.Locked ->
      Some("Unlock Player", fun _ -> toggleLocked player)
    | Some player ->
      Some("Lock Player", fun _ -> toggleLocked player)
    | None -> None

  ContextMenu.create active onOpen
    (List.choose id [
      toggleLocked
      addGroup
      createCue
      addCue
      duplicateCue
     ])

// * React components
// ** EmptyComponent

type EmptyComponent(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState(defaultState)

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
    CurrentCue = None
  } []

// ** Component

type Component(props) =
  inherit React.Component<Props, State>(props)
  do base.setInitState(defaultState)

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
            CG.CurrentCue = this.props.CurrentCue
            CG.SelectedCueIndex = this.state.SelectedCueIndex
            CG.SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
            CG.SelectCueGroup = fun g -> this.setState({ this.state with SelectedCueGroupIndex = g; SelectedCueIndex = -1 })
            CG.SelectCue = fun g c -> this.setState({ this.state with SelectedCueGroupIndex = g; SelectedCueIndex = c })
          } |> Some)
        |> Array.choose id
      from CueGroupSortableContainer
        { items = itemProps
          useDragHandle = true
          onSortEnd = fun ev ->
            // Update the CueList with the new order
            Sortable.arrayMove(cueList.Items, ev.oldIndex, ev.newIndex)
            |> flip CueList.setItems cueList
            |> UpdateCueList
            |> ClientContext.Singleton.Post
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

      ///  _               _
      /// | |    ___   ___| | __
      /// | |   / _ \ / __| |/ /
      /// | |__| (_) | (__|   <
      /// |_____\___/ \___|_|\_\
      button [
        classList [
          "warning",locked
          "disco-button", true
        ]
        Disabled true
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
        Class "disco-control disco-select"
        Value current
        Disabled locked
        OnChange (fun ev -> updateCueList this.props.Player !!ev.target?value)
      ] cueLists

      createContextMenu
        this.state.ContextMenuActive
        this.toggleMenu
        this.state
        this.props

      ///  _   _           _
      /// | \ | | _____  _| |_
      /// |  \| |/ _ \ \/ / __|
      /// | |\  |  __/>  <| |_
      /// |_| \_|\___/_/\_\\__|
      button [
        Class "disco-button pull-right"
        Disabled (Option.isNone this.props.CueList)
        OnClick (fun _ -> nextItem this.props)
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
        Class "disco-button pull-right"
        Disabled (Option.isNone this.props.CueList)
        OnClick (fun _ -> previousItem this.props)
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

  // *** toggleMenu

  member this.toggleMenu() =
    this.setState({ this.state with ContextMenuActive = not this.state.ContextMenuActive })

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
        minW = 4
        minH = 4 }
    member this.Render(dispatch, model) =
      match model.state with
      | None -> createEmpty dispatch model this.Id this.Name
      | Some state ->
        match Map.tryFind (DiscoId.FromGuid id) state.CuePlayers with
        | None -> createEmpty dispatch model this.Id this.Name
        | Some player ->
          let cueList =
            Option.bind
              (flip State.cueList state)
              player.CueListId
          let currentCue =
            Option.bind
              (CueList.items >> flip CuePlayer.findSelected player)
              cueList
          com<Component,_,_>
            { CueList = cueList
              Model = model
              Dispatch = dispatch
              Id = this.Id
              Name = this.Name
              Player = Some player
              CurrentCue = currentCue } []
  }
