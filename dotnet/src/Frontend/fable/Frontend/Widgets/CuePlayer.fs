module rec Iris.Web.Widgets.CuePlayer

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Helpers

importAll "../../../css/cuePlayer.css"

module private Helpers =
  type RCom = React.ComponentClass<obj>
  let Clock: RCom = importDefault "../../../src/widgets/Clock"
  let SpreadView: RCom = importMember "../../../src/widgets/Spread"
  let SpreadCons: JsConstructor<Pin,obj> = importDefault "../../../src/widgets/Spread"
  let touchesElement(el: Browser.Element, x: float, y: float): bool = importMember "../../../src/Util"

  let inline Class x = ClassName x
  let inline CustomKeyValue(k:string, v: obj):'a = !!(k,v)
  let inline (~%) x = createObj x

  module Array =
    // TODO: Fable is not able to resolve this, check
    // let inline replaceById< ^t when ^t : (member Id : Id)> (newItem : ^t) (ar: ^t[]) =
    //   Array.map (fun (x: ^t) -> if (^t : (member Id : Id) newItem) = (^t : (member Id : Id) x) then newItem else x) ar

    let inline replaceById (newItem : Cue) (ar: Cue[]) =
      Array.map (fun (x: Cue) -> if newItem.Id = x.Id then newItem else x) ar


type Layout =
  {
    x: int; y: int;
    w: int; h: int;
    minW: int; maxW: int;
    minH: int; maxH: int;
  }

type ISpread =
  abstract member pin: Pin
  abstract member rows: (string*obj)[]

type IWidgetModel =
  abstract name: string
  abstract layout: Layout
  abstract view: System.Type

type IDragEvent =
  abstract origin: int
  abstract x: float
  abstract y: float
  abstract ``type``: string
  abstract model: ISpread

type [<Pojo>] CueState =
  { isOpen: bool }

type [<Pojo>] CueProps =
  { ``global``: GlobalModel
  ; cue: Cue
  ; cueList: CueList
  ; index: int
  ; selectedIndex: int
  ; select: int -> unit }

// TODO: Create a cache to speed up look ups
let findPin (pinId: Id) (state: IGlobalState) =
  match Map.tryFindPin pinId state.pinGroups with
  | Some pin -> pin
  | None -> failwithf "Cannot find pind with Id %O in GlobalState" pinId

type private CueView(props) =
  inherit React.Component<CueProps, CueState>(props)
  let disposables = ResizeArray<IDisposable>()
  let mutable selfRef = Unchecked.defaultof<Browser.Element>
  do base.setInitState({isOpen = false})

  member this.componentDidMount() =
    disposables.Add(this.props.``global``.subscribeToEvent("drag", fun (ev: IDragEvent) ->
        if selfRef <> null then
          let mutable highlight = false
          if touchesElement(selfRef, ev.x, ev.y) then
            match ev.``type`` with
            | "move" ->
              highlight <- true
            | "stop" ->
              let newCue = { this.props.cue with Slices = Array.append this.props.cue.Slices [|ev.model.pin.Slices|] }
              let newCueList = { this.props.cueList with Cues = Array.replaceById newCue this.props.cueList.Cues }
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
      if this.state.isOpen
      then "iris-icon iris-icon-caret-down-two"
      else "iris-icon iris-icon-caret-right"
    div [] [
      yield
        div [
          Class "cueplayer-list-header cueplayer-cue level"
          Ref (fun el -> selfRef <- el)
        ] [
          div [Class "level-left"] [
            div [Class "level-item"] [
              span [
                Class leftIconClass
                OnClick (fun _ -> this.setState({isOpen = not this.state.isOpen}))
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
                let cueList2 = { this.props.cueList with Cues = this.props.cueList.Cues |> Array.filter (fun c -> c.Id = this.props.cue.Id) }
                UpdateCueList cueList2 |> ClientContext.Singleton.Post)
            ] [
              span [Class "iris-icon iris-icon-close"] []
            ]
          ]
        ]
      if this.state.isOpen then
        for slice in this.props.cue.Slices do
          let pin: Pin = findPin slice.Id this.props.``global``.state
          let spreadModel = SpreadCons.Create(pin) // TODO: Use slice values instead of pin's
          yield from SpreadView %["key"==>i; "model"==>spreadModel; "global"==>this.props.``global``] []
    ]

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

type [<Pojo>] CuePlayerProps =
  { id: int;
    model: CuePlayerModel
    ``global``: GlobalModel }

type [<Pojo>] CuePlayerState =
  { selectedIndex: int }

let private cueMockup() =
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

type CuePlayerView(props) =
  inherit React.Component<CuePlayerProps, CuePlayerState>(props)
  let disposables = ResizeArray<IDisposable>()
  do
    base.setInitState({ selectedIndex = 0 })
    // TODO: Mock code, create player if it doesn't exist
    if Map.count props.``global``.state.cuePlayers = 0 then
      let cueList, cuePlayer = cueMockup()
      AddCueList cueList |> ClientContext.Singleton.Post
      AddCuePlayer cuePlayer |> ClientContext.Singleton.Post

  member this.componentDidMount() =
    let state = this.props.``global``.state
    disposables.Add(this.props.``global``.subscribe(!^[|nameof(state.cueLists); nameof(state.cuePlayers)|], fun _ -> this.forceUpdate()))

  member this.componentWillUnmount() =
    for d in disposables do
      d.Dispose()

  member this.render() =
    let cueList =
      // TODO: Use a dropdown to choose the player
      Seq.tryHead this.props.``global``.state.cuePlayers
      |> Option.bind (fun kv -> kv.Value.CueList)
      |> Option.bind (fun id -> Map.tryFind id this.props.``global``.state.cueLists)
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
            { ``global`` = this.props.``global``
            ; cue = cueList.Cues.[i]
            ; cueList = cueList
            ; index = i
            ; selectedIndex = this.state.selectedIndex
            ; select = fun i -> this.setState({selectedIndex = i}) }
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