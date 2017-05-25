module rec Iris.Web.Widgets.CuePlayer

open System
open System.Collections.Generic
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Spread
open Helpers

importAll "../../../css/cuePlayer.css"

module private Helpers =
  type RCom = React.ComponentClass<obj>
  let Clock: RCom = importDefault "../../../src/widgets/Clock"
  let touchesElement(el: Browser.Element, x: float, y: float): bool = importMember "../../../src/Util"

  let inline Class x = ClassName x
  let inline CustomKeyValue(k:string, v: obj):'a = !!(k,v)
  let inline (~%) x = createObj x

  let tryDic key value (dic: IDictionary<string, obj>) =
    match dic.TryGetValue(key) with
    | true, v when v = value -> true
    | _ -> false

  let findPin =
    let cache = Dictionary<string, Pin>()
    fun (pinId: Id) (state: IGlobalState) ->
      let pinIdStr = string pinId
      match cache.TryGetValue(pinIdStr) with
      | true, pin -> pin
      | false, _ ->
        match Map.tryFindPin pinId state.pinGroups with
        // TODO: Clear cache when it reaches a limit?
        | Some pin -> cache.Add(pinIdStr, pin); pin
        | None -> failwithf "Cannot find pin with Id %O in GlobalState" pinId

  let findCue =
    let cache = Dictionary<string, Cue>()
    fun (cueId: Id) (state: IGlobalState) ->
      let cueIdStr = string cueId
      match cache.TryGetValue(cueIdStr) with
      | true, pin -> pin
      | false, _ ->
        match Map.tryFind cueId state.cues with
        // TODO: Clear cache when it reaches a limit?
        | Some cue -> cache.Add(cueIdStr, cue); cue
        | None -> failwithf "Cannot find cue with Id %O in GlobalState" cueId

  let cueMockup() =
    let cueList: CueList =
      { Id = Id.Create()
        Name = name "MockCueList"
        Groups = [||] }
    let cuePlayer =
      CuePlayer.create (name "MockCuePlayer") (Some cueList.Id)
    cueList, cuePlayer

  let updateSlicesValue (index: int) (value: obj) slices: Slices =
    match slices with
    | StringSlices(id, arr) -> StringSlices(id, Array.mapi (fun i el -> if i = index then value :?> string     else el) arr)
    | NumberSlices(id, arr) -> NumberSlices(id, Array.mapi (fun i el -> if i = index then value :?> double     else el) arr)
    | BoolSlices  (id, arr) -> BoolSlices  (id, Array.mapi (fun i el -> if i = index then value :?> bool       else el) arr)
    | ByteSlices  (id, arr) -> ByteSlices  (id, Array.mapi (fun i el -> if i = index then value :?> byte[]     else el) arr)
    | EnumSlices  (id, arr) -> EnumSlices  (id, Array.mapi (fun i el -> if i = index then value :?> Property   else el) arr)
    | ColorSlices (id, arr) -> ColorSlices (id, Array.mapi (fun i el -> if i = index then value :?> ColorSpace else el) arr)

  let printCueList (cueList: CueList) =
    for group in cueList.Groups do
      printfn "CueGroup: %O" group.Id
      for cueRef in group.CueRefs do
        printfn "    CueRef: %O" cueRef.Id

  module Array =
    let inline replaceById< ^t when ^t : (member Id : Id)> (newItem : ^t) (ar: ^t[]) =
      Array.map (fun (x: ^t) -> if (^t : (member Id : Id) newItem) = (^t : (member Id : Id) x) then newItem else x) ar

    // let inline replaceById (newItem : CueGroup) (ar: CueGroup[]) =
    //   Array.map (fun (x: CueGroup) -> if newItem.Id = x.Id then newItem else x) ar


type [<Pojo>] private CueState =
  { Cue: Cue
    IsOpen: bool
    Name: Name
    Offset: string
    Time: string }

type [<Pojo>] private CueProps =
  { key: string
    Global: GlobalModel
    CueRef: CueReference
    CueGroup: CueGroup
    CueList: CueList
    Index: int
    SelectedCueGroupIndex: int
    SelectedCueIndex: int
    SelectCue: int -> unit }

type private CueView(props) =
  inherit React.Component<CueProps, CueState>(props)
  let disposables = ResizeArray<IDisposable>()
  let mutable selfRef = Unchecked.defaultof<Browser.Element>
  do
    let cue = findCue props.CueRef.CueId props.Global.State
    base.setInitState({Cue = cue; IsOpen = false; Name = cue.Name; Offset = "0000"; Time = "00:00:00" })

  member this.componentDidMount() =
    let globalModel = this.props.Global
    disposables.Add(globalModel.Subscribe(!^(nameof globalModel.State.cues), fun _ dic ->
      if tryDic "Id" this.state.Cue.Id dic then
        let cue = Map.find this.state.Cue.Id globalModel.State.cues
        this.setState({this.state with Cue=cue; Name=cue.Name})
    ))
    disposables.Add(this.props.Global.SubscribeToEvent("drag", fun (ev: IDragEvent) _ ->
        if selfRef <> null then
          let mutable highlight = false
          if touchesElement(selfRef, ev.x, ev.y) then
            match ev.``type`` with
            | "move" ->
              highlight <- true
            | "stop" ->
              let newCue = { this.state.Cue with Slices = Array.append this.state.Cue.Slices [|ev.model.pin.Slices|] }
              UpdateCue newCue |> ClientContext.Singleton.Post
            | _ -> ()
          if highlight
          then selfRef.classList.add("iris-highlight-blue")
          else selfRef.classList.remove("iris-highlight-blue")
    ))

  member this.componentWillUnmount() =
    for d in disposables do
      d.Dispose()

  member this.render() =
    let arrowIconClass =
      if this.state.IsOpen
      then "iris-icon iris-icon-caret-down-two"
      else "iris-icon iris-icon-caret-right"
    div [
      Ref (fun el -> selfRef <- el)
      Style [MarginLeft 20.]
    ] [
      yield
        div [Class "cueplayer-list-header cueplayer-cue level"] [
          div [Class "level-left"] [
            div [Class "level-item"] [
              span [
                Class arrowIconClass
                OnClick (fun _ -> this.setState({this.state with IsOpen = not this.state.IsOpen}))
              ] []]
            div [Class "level-item"] [
              div [
                Class "cueplayer-button iris-icon cueplayer-player"
                OnClick (fun _ ->
                  // TODO: Edit the value of the pin directly
                  CallCue this.state.Cue |> ClientContext.Singleton.Post)
              ] [
                span [Class "iris-icon iris-icon-play"] []
              ]
            ]
          ]
          div [Class "level-item"] [
            form [OnSubmit (fun ev -> ev.preventDefault())] [
              input [
                Class "cueplayer-cueDesc"
                Type "text"
                Name "cueoffset"
                Value !^this.state.Offset
                OnChange (fun ev -> this.setState({this.state with Offset = !!ev.target?value}))
              ]
              br []
            ]
          ]
          div [Class "level-item"] [
            form [OnSubmit (fun ev -> ev.preventDefault())] [
              input [
                Class "cueplayer-cueDesc"
                Type "text"
                Name "cuename"
                Value !^(unwrap this.state.Name: string)
                OnChange (fun ev -> this.setState({this.state with Name = !!ev.target?value}))
                OnBlur (fun _ ->
                  { this.state.Cue with Name = this.state.Name } |> UpdateCue |> ClientContext.Singleton.Post)
                OnKeyUp (fun ev -> if ev.keyCode = 13. (* ENTER *) then !!ev.target?blur())
              ]
              br []
            ]
          ]
          div [Class "level-item"] [
            form [OnSubmit (fun ev -> ev.preventDefault())] [
              input [
                Class "cueplayer-cueDesc"
                Style [
                  CSSProp.Width 60.
                  MarginRight 5.
                ]
                Type "text"
                Name "cuetime"
                Value !^this.state.Time
                OnChange (fun ev -> this.setState({this.state with Time = !!ev.target?value}))
              ]
              br []
            ]
          ]
          div [Class "level-right"] [
            div [Class "cueplayer-button iris-icon level-item"] [
              span [Class "iris-icon iris-icon-duplicate"] []
            ]
            div [
              Class "cueplayer-button iris-icon cueplayer-close level-item"
              OnClick (fun _ ->
                let id = this.props.CueRef.Id
                let cueGroup2 = { this.props.CueGroup with CueRefs = this.props.CueGroup.CueRefs |> Array.filter (fun c -> c.Id <> id) }
                let msg = failwith "TODO" // UpdateCueGroup(this.props.CueList.Id, cueGroup2)
                ClientContext.Singleton.Post(msg))
            ] [
              span [Class "iris-icon iris-icon-close"] []
            ]
          ]
        ]
      if this.state.IsOpen then
        for i=0 to this.state.Cue.Slices.Length - 1 do
          let slices = this.state.Cue.Slices.[i]
          let pin = findPin slices.Id this.props.Global.State
          yield com<SpreadView,_,_>
            { key = string i
              model = Spread(pin, slices, (fun valueIndex value -> this.UpdateCueValue(i, valueIndex, value)))
              ``global`` = this.props.Global
              onDragStart = None } []
    ]

  member this.UpdateCueValue(sliceIndex: int, valueIndex: int, value: obj) =
    let newSlices =
      this.state.Cue.Slices |> Array.mapi (fun i slices ->
        if i = sliceIndex then updateSlicesValue valueIndex value slices else slices)
    { this.state.Cue with Slices = newSlices } |> UpdateCue |> ClientContext.Singleton.Post

type [<Pojo>] private CueGroupState =
  { IsOpen: bool
  ; Name: Name }

type [<Pojo>] private CueGroupProps =
  { key: string
    Global: GlobalModel
    CueGroup: CueGroup
    CueList: CueList
    CueGroupIndex: int
    SelectedCueGroupIndex: int
    SelectedCueIndex: int
    SelectCueGroup: int -> unit
    SelectCue: int -> unit }

type private CueGroupView(props) =
  inherit React.Component<CueGroupProps, CueGroupState>(props)
  do base.setInitState({IsOpen = true; Name = props.CueGroup.Name })

  member this.render() =
    let arrowIconClass =
      if this.state.IsOpen
      then "iris-icon iris-icon-caret-down-two"
      else "iris-icon iris-icon-caret-right"
    div [] [
      yield
        div [Class "cueplayer-list-header cueplayer-cue level"] [
          div [Class "level-left"] [
            div [Class "level-item"] [
              span [
                Class arrowIconClass
                OnClick (fun _ -> this.setState({this.state with IsOpen = not this.state.IsOpen}))
              ] []]
            div [Class "level-item"] [
              div [
                Class "cueplayer-button iris-icon cueplayer-player"
                // TODO: Call all cues in the group
                OnClick (fun _ -> ())
              ] [
                span [Class "iris-icon iris-icon-play"] []
              ]
            ]
            div [Class "level-item"] [
              form [OnSubmit (fun ev -> ev.preventDefault())] [
                input [
                  Class "cueplayer-cueDesc"
                  Type "text"
                  Name "cuegroupname"
                  Value !^(unwrap this.state.Name: string)
                  OnChange (fun ev -> this.setState({this.state with Name = !!ev.target?value}))
                  OnBlur (fun _ ->
                    let newGroup = { this.props.CueGroup with Name = this.state.Name }
                    { this.props.CueList with Groups = Array.replaceById newGroup this.props.CueList.Groups  }
                    |> UpdateCueList |> ClientContext.Singleton.Post)
                  OnKeyUp (fun ev -> if ev.keyCode = 13. (* ENTER *) then !!ev.target?blur())
                ]
                br []
              ]
            ]
          ]
        ]
      if this.state.IsOpen then
        for i=0 to this.props.CueGroup.CueRefs.Length - 1 do
          let cueRef = this.props.CueGroup.CueRefs.[i]
          yield com<CueView,_,_>
            { key = string cueRef.Id
              Global = this.props.Global
              CueRef = cueRef
              CueGroup = this.props.CueGroup
              CueList = this.props.CueList
              Index = i
              SelectedCueIndex = this.props.SelectedCueIndex
              SelectedCueGroupIndex = this.props.SelectedCueGroupIndex
              SelectCue = this.props.SelectCue }
            []
    ]

type CuePlayerModel() =
  interface IWidgetModel with
    member __.view = typeof<CuePlayerView>
    member __.name = "Cue Player"
    member __.layout =
      { x = 0; y = 0;
        w = 8; h = 5;
        minW = 2; maxW = 10;
        minH = 1; maxH = 10; }

type [<Pojo>] CuePlayerState =
  { CueList: CueList option
    SelectedCueGroupIndex: int
    SelectedCueIndex: int }

type CuePlayerView(props) =
  inherit React.Component<IWidgetProps<CuePlayerModel>, CuePlayerState>(props)
  let disposables = ResizeArray<IDisposable>()
  let globalModel = props.``global`` :?> GlobalModel
  do
    // TODO: Mock code, create player if it doesn't exist
    if Map.count globalModel.State.cuePlayers = 0 then
      let cueList, cuePlayer = cueMockup()
      AddCueList cueList |> ClientContext.Singleton.Post
      AddCuePlayer cuePlayer |> ClientContext.Singleton.Post
      base.setInitState({ CueList = Some cueList; SelectedCueGroupIndex = 0; SelectedCueIndex = 0 })
    else
      // TODO: Use a dropdown to choose the player/list
      let cueList =
        Seq.tryHead globalModel.State.cuePlayers
        |> Option.bind (fun kv -> kv.Value.CueList)
        |> Option.bind (fun id -> Map.tryFind id globalModel.State.cueLists)
      base.setInitState({ CueList = cueList; SelectedCueGroupIndex = 0; SelectedCueIndex = 0 })

  member this.componentDidMount() =
    let state = globalModel.State
    disposables.Add(globalModel.Subscribe(!^[|nameof(state.cueLists); nameof(state.cuePlayers)|], fun _ dic ->
      printfn "Received event"
      match this.state.CueList with
      | Some cueList ->
        if tryDic (nameof cueList.Id) cueList.Id dic then
          printfn "Ids match"
          let cueList = Map.find cueList.Id globalModel.State.cueLists
          this.setState({this.state with CueList=Some cueList})
      | None -> ()))

  member this.componentWillUnmount() =
    for d in disposables do
      d.Dispose()

  member this.render() =
    #if DEBUG
    Option.iter printCueList this.state.CueList
    #endif
    div [Class "cueplayer-container"] [
      // HEADER
      yield
        div [Class "cueplayer-list-header"] [
          div [Class "cueplayer-button cueplayer-go"] [
            span [
              Class "iris-icon"
              CustomKeyValue("data-icon", "c")
            ] [str "GO"]
          ]
          div [Class "cueplayer-button iris-icon"] [
            span [Class "iris-icon iris-icon-fast-backward"] []
          ]
          div [Class "cueplayer-button iris-icon"] [
            span [Class "iris-icon iris-icon-fast-forward"] []
          ]
          div [
            Class "cueplayer-button"
            OnClick (fun _ ->
              match this.state.CueList with
              | None -> printfn "There is no cue list available to add the cue"
              | Some cueList ->
                let cueGroup = cueList.Groups.[this.state.SelectedCueGroupIndex]
                let newCue = { Id = Id.Create(); Name = name "Untitled"; Slices = [||] }
                let newCueRef = { Id = Id.Create(); CueId = newCue.Id; AutoFollow = -1; Duration = -1; Prewait = -1 }
                // TODO: Add CueRef after selected cue
                let newCueGroup = { cueGroup with CueRefs = Array.append cueGroup.CueRefs [| newCueRef |] }
                let newCueList = { cueList with Groups = Array.append cueList.Groups [|newCueGroup|] }
                AddCue newCue |>  ClientContext.Singleton.Post
                UpdateCueList newCueList |> ClientContext.Singleton.Post
              )
          ] [str "Add Cue"]
          div [
            Class "cueplayer-button"
            OnClick (fun _ ->
              match this.state.CueList with
              | None -> printfn "There is no cue list available to add the group"
              | Some cueList ->
                let newCueGroup = { Id = Id.Create(); Name = name "Untitled"; CueRefs = [||] }
                // TODO: Add CueGroup after selected group
                let newCueList = { cueList with Groups = Array.append cueList.Groups [|newCueGroup|] }
                UpdateCueList newCueList |> ClientContext.Singleton.Post
              )
          ] [str "Add Group"]
          div [Style [Clear "both"]] []
        ]
      // CUE GROUPS
      match this.state.CueList with
      | None -> ()
      | Some cueList ->
        for i=0 to (cueList.Groups.Length-1) do
          let cueGroup = cueList.Groups.[i]
          yield com<CueGroupView,_,_>
            { key = string cueGroup.Id
              Global = globalModel
              CueGroup = cueGroup
              CueList = cueList
              CueGroupIndex = i
              SelectedCueGroupIndex = this.state.SelectedCueGroupIndex
              SelectedCueIndex = this.state.SelectedCueIndex
              SelectCueGroup = fun i -> this.setState({this.state with SelectedCueGroupIndex = i})
              SelectCue = fun i -> this.setState({this.state with SelectedCueIndex = i}) }
            []
    ]
