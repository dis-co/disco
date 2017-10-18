module Iris.Web.CuePlayerView

open System
open System.Collections.Generic
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Elmish.React
open Types
open Helpers

let [<Literal>] SELECTION_COLOR = "lightblue"

module private PrivateHelpers =
  type RCom = React.ComponentClass<obj>
  let ContentEditable: RCom = importDefault "../../js/widgets/ContentEditable"
  let touchesElement(el: Browser.Element option, x: float, y: float): bool = importMember "../../js/Util"

  let tryDic key value (dic: IDictionary<string, obj>) =
    match dic.TryGetValue(key) with
    | true, v when v = value -> true
    | _ -> false

  let cueListMockup() =
    let cueGroup =
      { Id = IrisId.Create()
        Name = name "MockCueGroup"
        CueRefs = [||] }
    { Id = IrisId.Create()
      Name = name "MockCueList"
      Items = [| CueGroup cueGroup |] }
    // let cuePlayer =
    //   CuePlayer.create (name "MockCuePlayer") (Some cueList.Id)

  let private castValue<'a> arr idx (value: obj) =
    Array.mapi (fun i el -> if i = idx then value :?> 'a else el) arr

  let updateSlicesValue (index: int) (value: obj) slices: Slices =
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

  let printCueList (cueList: CueList) =
    for item in cueList.Items do
      match item with
      | Headline (_, headline) ->
        printfn "Headline: %s" headline
      | CueGroup group ->
        printfn "CueGroup: %O (%O)" group.Name group.Id
        for cueRef in group.CueRefs do
          printfn "    CueRef: %O" cueRef.Id

open PrivateHelpers

let CueSortableHandle = Sortable.Handle(fun props ->
  td [Class "width-10"; Style [Cursor "move"]] [str props.value])

type [<Pojo>] private CueState =
  { IsOpen: bool
    IsHighlit: bool }

type [<Pojo>] private CueProps =
  { key: string
    Model: Model
    State: State
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

type private CueView(props) =
  inherit React.Component<CueProps, CueState>(props)
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

  member this.renderInput(widthPercentage: int, content: string, ?update: string->unit) =
    let content =
      match update with
      | Some update ->
        from ContentEditable
          %["tagName" ==> "span"
            "html" ==> content
            "onChange" ==> update] []
      | None -> span [] [str content]
    td [Class ("width-" + string widthPercentage)] [content]

  member this.render() =
    let arrowButton =
      td [Class "width-5"] [
        button [
          Class ("iris-button iris-icon icon-control " + (if this.state.IsOpen then "icon-less" else "icon-more"))
          OnClick (fun ev ->
            // Don't stop propagation to allow the item to be selected
            // ev.stopPropagation()
            this.setState({ this.state with IsOpen = not this.state.IsOpen}))
        ] []
      ]
    let playButton =
      td [Class "width-5"] [
        button [
          Class "iris-button iris-icon icon-play"
          OnClick (fun ev ->
            // Don't stop propagation to allow the item to be selected
            // ev.stopPropagation()
            CallCue this.props.Cue |> ClientContext.Singleton.Post
          )
        ] []
      ]
    let autocallButton =
      td [Class "width-10"; Style [TextAlign "center"]] [
        button [
          Class "iris-button iris-icon icon-autocall"
          OnClick (fun ev ->
            // Don't stop propagation to allow the item to be selected
            // ev.stopPropagation()
            Browser.window.alert("Auto call!")
          )
        ] []
      ]
    let removeButton =
      td [Class "width-5"] [
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
      ]
    let cueHeader =
      tr [
        OnClick (fun _ ->
          Select.cue this.props.Dispatch this.props.Cue
          if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex
            || this.props.CueIndex <> this.props.SelectedCueIndex then
            this.props.SelectCue this.props.CueGroupIndex this.props.CueIndex  )
      ] [
        arrowButton
        playButton
        from CueSortableHandle { value = String.Format("{0:0000}", this.props.CueIndex + 1)} []
        this.renderInput(25, unwrap this.props.Cue.Name, (fun txt ->
          { this.props.Cue with Name = name txt } |> UpdateCue |> ClientContext.Singleton.Post))
        this.renderInput(20, "00:00:00")
        this.renderInput(20, "shortkey")
        autocallButton
        removeButton
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
        [cueHeader; tr [] [td [ColSpan 8.] [ul [Class "iris-graphview"] pinGroups]]]
    let isSelected =
      this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
        && this.props.CueIndex = this.props.SelectedCueIndex
    let isHighlit = this.state.IsHighlit
    tr [] [
      // Set min-width so the row doesn't look too compressed when dragging
      td [ColSpan 8.; Style [MinWidth 500]] [
        table [
          classList ["iris-table", true
                     "iris-cue", true
                     "iris-cue-selected", isSelected
                     "iris-highlight", isHighlit]
          Ref (fun el -> selfRef <- Option.ofObj el)
        ] [tbody [] rows]]
    ]

  member this.updateCueValue(local: bool, sliceIndex: int, valueIndex: int, value: obj) =
    let newSlices =
      this.props.Cue.Slices |> Array.mapi (fun i slices ->
        if i = sliceIndex then updateSlicesValue valueIndex value slices else slices)
    let command =
      { this.props.Cue with Slices = newSlices } |> UpdateCue
    if local
    then ClientContext.Singleton.PostLocal command
    else ClientContext.Singleton.Post command

let private CueSortableItem = Sortable.Element <| fun props ->
  com<CueView,_,_> props.value []

let private CueSortableContainer = Sortable.Container <| fun props ->
    let items =
      props.items |> Array.mapi (fun i props ->
        from CueSortableItem { key=props.key; index=i; value=props } [])
    tbody [] (Array.toList items)

type [<Pojo>] CuePlayerProps =
  { CueList: CueList option
    Model: Model
    Dispatch: Msg->unit
    Id: Guid
    Name: string }

type [<Pojo>] CuePlayerState =
  { SelectedCueGroupIndex: int
    SelectedCueIndex: int }

type CuePlayerView(props) =
  inherit React.Component<CuePlayerProps, CuePlayerState>(props)
  do base.setInitState({ SelectedCueGroupIndex = -1; SelectedCueIndex = -1})

  member this.renderCues() =
    match this.props.CueList, this.props.Model.state with
    | Some cueList, Some state ->
      // TODO: Temporarily assume just one group
      match Seq.tryHead cueList.Items with
      | Some (CueGroup group) ->
        let cueProps =
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

  member this.shouldComponentUpdate(nextProps: CuePlayerProps, nextState: CuePlayerState) =
    this.state <> nextState ||
      match this.props.Model.state, nextProps.Model.state with
      | Some s1, Some s2 ->
        distinctRef s1.CueLists s2.CueLists
          || distinctRef s1.CuePlayers s2.CuePlayers
          || distinctRef s1.Cues s2.Cues
          || distinctRef s1.PinGroups s2.PinGroups
      | None, None -> false
      | _ -> true

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
      com<CuePlayerView,_,_>
        { CueList = cueList
          Model = model
          Dispatch = dispatch
          Id = this.Id
          Name = this.Name } []
  }
