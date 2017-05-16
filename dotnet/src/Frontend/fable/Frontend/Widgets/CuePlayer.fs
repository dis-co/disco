module rec Iris.Web.Widgets.CuePlayer

open System
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
  // let SpreadView: RCom = importMember "../../../src/widgets/Spread"
  // let SpreadCons: JsConstructor<Pin,ISpread> = importDefault "../../../src/widgets/Spread"
  let touchesElement(el: Browser.Element, x: float, y: float): bool = importMember "../../../src/Util"

  let inline Class x = ClassName x
  let inline CustomKeyValue(k:string, v: obj):'a = !!(k,v)
  let inline (~%) x = createObj x

  // TODO: Create a cache to speed up look-ups
  let findPin (pinId: Id) (state: IGlobalState) =
    match Map.tryFindPin pinId state.pinGroups with
    | Some pin -> pin
    | None -> failwithf "Cannot find pind with Id %O in GlobalState" pinId

  let cueMockup() =
    let cue: Cue =
      { Id = Id.Create()
        Name = "MockCue"
        Slices = [||] }
    let cueList: CueList =
      { Id = Id.Create()
        Name = name "MockCueList"
        Cues = [|cue|] }
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

  module Array =
    // TODO: Fable is not able to resolve this, check
    // let inline replaceById< ^t when ^t : (member Id : Id)> (newItem : ^t) (ar: ^t[]) =
    //   Array.map (fun (x: ^t) -> if (^t : (member Id : Id) newItem) = (^t : (member Id : Id) x) then newItem else x) ar

    let inline replaceById (newItem : Cue) (ar: Cue[]) =
      Array.map (fun (x: Cue) -> if newItem.Id = x.Id then newItem else x) ar


type [<Pojo>] private CueState =
  { IsOpen: bool }

type [<Pojo>] private CueProps =
  { Global: GlobalModel
  ; Cue: Cue
  ; CueList: CueList
  ; Index: int
  ; SelectedIndex: int
  ; Select: int -> unit }

type private CueView(props) =
  inherit React.Component<CueProps, CueState>(props)
  let disposables = ResizeArray<IDisposable>()
  let mutable selfRef = Unchecked.defaultof<Browser.Element>
  do base.setInitState({IsOpen = false})

  member this.componentDidMount() =
    disposables.Add(this.props.Global.SubscribeToEvent("drag", fun (ev: IDragEvent) ->
        if selfRef <> null then
          let mutable highlight = false
          if touchesElement(selfRef, ev.x, ev.y) then
            match ev.``type`` with
            | "move" ->
              highlight <- true
            | "stop" ->
              let newCue = { this.props.Cue with Slices = Array.append this.props.Cue.Slices [|ev.model.pin.Slices|] }
              let newCueList = { this.props.CueList with Cues = Array.replaceById newCue this.props.CueList.Cues }
              UpdateCueList newCueList |> ClientContext.Singleton.Post
            | _ -> ()
          if highlight
          then selfRef.classList.add("iris-highlight-blue")
          else selfRef.classList.remove("iris-highlight-blue")
      )
    )

  member this.componentWillUnmount() =
    for d in disposables do
      d.Dispose()

  member this.render() =
    let leftIconClass =
      if this.state.IsOpen
      then "iris-icon iris-icon-caret-down-two"
      else "iris-icon iris-icon-caret-right"
    div [Ref (fun el -> selfRef <- el)] [
      yield
        div [Class "cueplayer-list-header cueplayer-cue level"] [
          div [Class "level-left"] [
            div [Class "level-item"] [
              span [
                Class leftIconClass
                OnClick (fun _ -> this.setState({IsOpen = not this.state.IsOpen}))
              ] []]
            div [Class "level-item"] [
              div [Class "cueplayer-button iris-icon cueplayer-player"] [
                span [Class "iris-icon iris-icon-play"] []
              ]
            ]
          ]
          div [Class "level-item"] [
            form [] [
              input [
                Class "cueplayer-cueDesc"
                Type "text"
                Value !^"0000"
                Name "firstname"
              ]
              br []
            ]
          ]
          div [Class "level-item"] [
            form [] [
              input [
                Class "cueplayer-cueDesc"
                Type "text"
                Value !^"Untitled"
                Name "firstname"
              ]
              br []
            ]
          ]
          div [Class "level-item"] [
            form [] [
              input [
                Class "cueplayer-cueDesc"
                Style [
                  CSSProp.Width 60.
                  MarginRight 5.
                ]
                Type "text"
                Value !^"00:00:00"
                Name "firstname"
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
                let cueList2 = { this.props.CueList with Cues = this.props.CueList.Cues |> Array.filter (fun c -> c.Id = this.props.Cue.Id) }
                UpdateCueList cueList2 |> ClientContext.Singleton.Post)
            ] [
              span [Class "iris-icon iris-icon-close"] []
            ]
          ]
        ]
      if this.state.IsOpen then
        for i=0 to this.props.Cue.Slices.Length - 1 do
          let slices = this.props.Cue.Slices.[i]
          let pin = findPin slices.Id this.props.Global.State
          yield com<SpreadView,_,_>
            { key = string i
            ; model = Spread(pin, slices, (fun valueIndex value -> this.UpdateCueValue(i, valueIndex, value)))
            ; ``global`` = this.props.Global
            ; onDragStart = None } []
    ]

  member this.UpdateCueValue(sliceIndex: int, valueIndex: int, value: obj) =
    let newSlices =
      this.props.Cue.Slices |> Array.mapi (fun i slices ->
        if i = sliceIndex then updateSlicesValue valueIndex value slices else slices)
    let newCue = { this.props.Cue with Slices = newSlices }
    let newCueList = { this.props.CueList with Cues = Array.replaceById newCue this.props.CueList.Cues }
    UpdateCueList newCueList |> ClientContext.Singleton.Post

type CuePlayerModel() =
  interface IWidgetModel with
    member __.view = typeof<CuePlayerView>
    member __.name = "Cue Player"
    member __.layout =
      {
        x = 0; y = 0;
        w = 8; h = 5;
        minW = 2; maxW = 10;
        minH = 1; maxH = 10;
      }

type [<Pojo>] CuePlayerState =
  { selectedIndex: int }

type CuePlayerView(props) =
  inherit React.Component<IWidgetProps<CuePlayerModel>, CuePlayerState>(props)
  let disposables = ResizeArray<IDisposable>()
  let globalModel = props.``global`` :?> GlobalModel
  do
    base.setInitState({ selectedIndex = 0 })
    // TODO: Mock code, create player if it doesn't exist
    if Map.count globalModel.State.cuePlayers = 0 then
      let cueList, cuePlayer = cueMockup()
      AddCueList cueList |> ClientContext.Singleton.Post
      AddCuePlayer cuePlayer |> ClientContext.Singleton.Post

  member this.componentDidMount() =
    let state = globalModel.State
    disposables.Add(globalModel.Subscribe(!^[|nameof(state.cueLists); nameof(state.cuePlayers)|], fun _ -> this.forceUpdate()))

  member this.componentWillUnmount() =
    for d in disposables do
      d.Dispose()

  member this.render() =
    let cueList =
      // TODO: Use a dropdown to choose the player
      Seq.tryHead globalModel.State.cuePlayers
      |> Option.bind (fun kv -> kv.Value.CueList)
      |> Option.bind (fun id -> Map.tryFind id globalModel.State.cueLists)
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
              match cueList with
              | None -> printfn "There is no cue list available to add the cue"
              | Some cueList ->
                let newCue = { Id = Id.Create(); Name = "Untitled"; Slices = [||] }
                let cueList2 = { cueList with Cues = Array.append cueList.Cues [|newCue|] }
                UpdateCueList cueList2 |> ClientContext.Singleton.Post)
          ] [str "Add Cue"]
          div [Class "cueplayer-button"] [str "Add Group"]
          div [Style [Clear "both"]] []
        ]
      // CUES
      match cueList with
      | None -> ()
      | Some cueList ->
        for i=0 to (cueList.Cues.Length-1) do
          yield com<CueView,_,_>
            { Global = globalModel
            ; Cue = cueList.Cues.[i]
            ; CueList = cueList
            ; Index = i
            ; SelectedIndex = this.state.selectedIndex
            ; Select = fun i -> this.setState({selectedIndex = i}) }
            []
    ]

  // member this.render() =
  //   let header =
  //     div [Class "level"]
  //       [div [Class "level-left"]
  //         [button [Class "button level-item"
  //                  Style [Margin 5]
  //                  OnClick(fun ev -> this.UpdateSource())]
  //           [str "Fire!"]
  //         ]
  //       ;div [Class "level-right"]
  //         [div [Class "level-item"]
  //           [from Clock %["global"==>this.props.``global``] []]
  //         ]
  //     ]
  //   let rows =
  //     this.props.model.cues
  //     |> Seq.mapi (fun i cue ->
  //       let foo = from SpreadView %["model"==>cue; "global"==>this.props.``global``] []
  //       div [Key (string i)] [from SpreadView %["model"==>cue; "global"==>this.props.``global``] []])
  //     |> Seq.toList
  //   // Return value
  //   div [Class "iris-cuelist"; Ref(fun el' -> el <- el')] (header::rows)