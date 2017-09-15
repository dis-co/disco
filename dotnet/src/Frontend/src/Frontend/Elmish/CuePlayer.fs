module rec Iris.Web.CuePlayer

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
open PrivateHelpers

type RCom = React.ComponentClass<obj>

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
      Groups = [|cueGroup|] }
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

  // TODO: Temporary solution, we should actually just call AddCue and the operation be done in the
  // backend
  let updatePins (cue: Cue) (state: State) =
    for slices in cue.Slices do
      let pin = Lib.findPin slices.PinId state
      match slices with
      | StringSlices (_, client, values) -> StringSlices(pin.Id, client, values)
      | NumberSlices (_, client, values) -> NumberSlices(pin.Id, client, values)
      | BoolSlices   (_, client, values) -> BoolSlices(pin.Id, client, values)
      | ByteSlices   (_, client, values) -> ByteSlices(pin.Id, client, values)
      | EnumSlices   (_, client, values) -> EnumSlices(pin.Id, client, values)
      | ColorSlices  (_, client, values) -> ColorSlices(pin.Id, client, values)
      |> UpdateSlices.ofSlices
      |> ClientContext.Singleton.Post

  let printCueList (cueList: CueList) =
    for group in cueList.Groups do
      printfn "CueGroup: %O (%O)" group.Name group.Id
      for cueRef in group.CueRefs do
        printfn "    CueRef: %O" cueRef.Id

type [<Pojo>] private CueState =
  { IsOpen: bool
    IsHighlit: bool }

type [<Pojo>] private CueProps =
  { key: string
    State: State
    UseRightClick: bool
    Cue: Cue
    CueRef: CueReference
    CueGroup: CueGroup
    CueList: CueList
    CueIndex: int
    CueGroupIndex: int
    SelectedCueIndex: int
    SelectedCueGroupIndex: int
    SelectCue: int -> int -> unit }

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
        | Drag.Moved(x,y,Drag.Pin pin) -> Some(pin,x,y,false)
        | Drag.Stopped(x,y,Drag.Pin pin) -> Some(pin,x,y,true))
      |> Observable.subscribe(fun (pin,x,y,stopped) ->
        let isHighlit, isOpen =
          if touchesElement(selfRef, x, y) then
            if not stopped then
              true, this.state.IsOpen
            else
              if this.props.Cue.Slices |> Array.exists (fun slices -> slices.PinId = pin.Id) then
                printfn "The cue already contains this pin"
              else
                let newCue = { this.props.Cue with Slices = Array.append this.props.Cue.Slices [|pin.Slices|] }
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
    td [ClassName ("width-" + string widthPercentage)] [content]

  member this.render() =
    let arrowButton =
      td [ClassName "width-5"] [
        button [
          ClassName ("iris-button iris-icon icon-control " + (if this.state.IsOpen then "icon-less" else "icon-more"))
          OnClick (fun ev ->
            ev.stopPropagation()
            this.setState({ this.state with IsOpen = not this.state.IsOpen}))
        ] []
      ]
    let playButton =
      td [ClassName "width-5"] [
        button [
          ClassName "iris-button iris-icon icon-play"
          OnClick (fun ev ->
            ev.stopPropagation()
            updatePins this.props.Cue this.props.State // TODO: Send CallCue event instead
          )
        ] []
      ]
    let autocallButton =
      td [ClassName "width-10"; Style [TextAlign "center"]] [
        button [
          ClassName "iris-button iris-icon icon-autocall"
          OnClick (fun ev ->
            ev.stopPropagation()
            // Browser.window.alert("Auto call!")
          )
        ] []
      ]
    let removeButton =
      td [ClassName "width-5"] [
        button [
          ClassName "iris-button iris-icon icon-control icon-close"
          OnClick (fun ev ->
            ev.stopPropagation()
            let id = this.props.CueRef.Id
            // Change selection if this item was selected
            if this.props.CueGroupIndex = this.props.SelectedCueGroupIndex then
              this.props.SelectCue this.props.CueGroupIndex 0
            let cueGroup = { this.props.CueGroup with CueRefs = this.props.CueGroup.CueRefs |> Array.filter (fun c -> c.Id <> id) }
            { this.props.CueList with Groups = Array.replaceById cueGroup this.props.CueList.Groups }
            |> UpdateCueList |> ClientContext.Singleton.Post)
        ] []
      ]
    let cueHeader =
      tr [
        OnClick (fun _ ->
          if this.props.CueGroupIndex <> this.props.SelectedCueGroupIndex
            || this.props.CueIndex <> this.props.SelectedCueIndex then
            this.props.SelectCue this.props.CueGroupIndex this.props.CueIndex  )
      ] [
        arrowButton
        playButton
        this.renderInput(10, String.Format("{0:0000}", this.props.CueIndex + 1))
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
              yield div [] [str (unwrap pinGroup.Name)]
              for i, pin, slices in pinAndSlices do
                yield com<PinView.PinView,_,_>
                  { key = string pin.Id
                    pin = pin
                    useRightClick = this.props.UseRightClick
                    slices = Some slices
                    updater =
                      Some { new IUpdater with
                              member __.Update(dragging, valueIndex, value) =
                                this.updateCueValue(dragging, i, valueIndex, value) }
                    onDragStart = None } []
            ])
          |> Array.toList
        [cueHeader; tr [] [td [ColSpan 8.] [ul [ClassName "iris-graphview"] pinGroups]]]
    let isSelected =
      this.props.CueGroupIndex = this.props.SelectedCueGroupIndex
        && this.props.CueIndex = this.props.SelectedCueIndex
    let isHighlit = this.state.IsHighlit
    tr [] [
      td [ColSpan 8.] [
        table [
          classList ["iris-table", true
                     "iris-cue", true
                     "iris-selected", isSelected
                     "iris-highlight", isHighlit
                     "iris-blue", isHighlit]
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
      match Seq.tryHead cueList.Groups with
      | Some group ->
        group.CueRefs
        |> Array.mapi (fun i cueRef ->
          com<CueView,_,_>
            { key = string cueRef.Id
              State = state
              UseRightClick = this.props.Model.userConfig.useRightClick
              Cue = Lib.findCue cueRef.CueId state
              CueRef = cueRef
              CueGroup = group
              CueList = cueList
              CueIndex = i
              CueGroupIndex = 0 //this.props.CueGroupIndex
              SelectedCueIndex = this.state.SelectedCueIndex
              SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
              SelectCue = fun g c -> this.setState({this.state with SelectedCueGroupIndex = g; SelectedCueIndex = c }) }
            [])
        |> Array.toList
      | None -> []
    | _ -> []

  member this.renderBody() =
    table [ClassName "iris-table"] [
      thead [Key "header"] [
        tr [] [
          th [ClassName "width-5"] [str ""]
          th [ClassName "width-5"] [str ""]
          th [ClassName "width-10"] [str "Nr."]
          th [ClassName "width-25"] [str "Cue name"]
          th [ClassName "width-20"] [str "Delay"]
          th [ClassName "width-20"] [str "Shortkey"]
          th [ClassName "width-10"; Style [TextAlign "center"]] [str "Autocall"]
          th [ClassName "width-5"] [str ""]
        ]
      ]
      tbody [] (this.renderCues())
    ]

  member this.renderTitleBar() =
    // TODO: Use a dropdown to choose the player/list
    button [
      ClassName "iris-button"
      Disabled (Option.isNone this.props.CueList)
      OnClick (fun _ ->
        this.props.CueList |> Option.iter (fun cueList ->
          AddCueUI(cueList, this.state.SelectedCueGroupIndex, this.state.SelectedCueIndex) |> this.props.Dispatch))
    ] [str "Add Cue"]

  member this.render() =
    widget this.props.Id this.props.Name
      (Some (fun _ _ -> this.renderTitleBar()))
      (fun _ _ -> this.renderBody()) this.props.Dispatch this.props.Model

  member this.shouldComponentUpdate(nextProps, nextState, nextContext) =
    match this.props.Model.state, nextProps.Model.state with
    | Some s1, Some s2 ->
      distinctRef s1.CueLists s2.CueLists
        || distinctRef s1.CuePlayers s2.CuePlayers
        || distinctRef s1.Cues s2.Cues
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
        minW = 4; maxW = 10;
        minH = 4; maxH = 10; }
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
