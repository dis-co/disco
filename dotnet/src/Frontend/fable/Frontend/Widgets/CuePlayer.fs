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
  let touchesElement(el: Browser.Element, x: float, y: float): bool = importMember "../../../src/Util"

  let inline Class x = ClassName x
  let inline CustomKeyValue(k:string, v: obj):'a = !!(k,v)
  let inline (~%) x = createObj x


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

let updateSlices(pin: Pin, rowIndex, newValue: obj) =
  let updateArray (i: int) (v: obj) (ar: 'T[]) =
    let newArray = Array.copy ar
    newArray.[i] <- unbox v
    newArray
  match pin with
  | StringPin pin ->
    StringSlices(pin.Id, updateArray rowIndex newValue pin.Values)
  | NumberPin pin ->
    let newValue =
      match newValue with
      | :? string as v -> box(double v)
      | v -> v
    NumberSlices(pin.Id, updateArray rowIndex newValue pin.Values)
  | BoolPin pin ->
    let newValue =
      match newValue with
      | :? string as v -> box(v.ToLower() = "true")
      | v -> v
    BoolSlices(pin.Id, updateArray rowIndex newValue pin.Values)
  | BytePin   _pin -> failwith "TO BE IMPLEMENTED"
  | EnumPin   _pin -> failwith "TO BE IMPLEMENTED"
  | ColorPin  _pin -> failwith "TO BE IMPLEMENTED"
  |> UpdateSlices |> ClientContext.Singleton.Post

// type Cue(spread: ISpread) =
//   member val Pin = spread.pin
//   member val Rows = spread.rows

//   member this.Update(rowIndex, newValue) =
//     let oldValue = this.Rows.[rowIndex]
//     this.Rows.[rowIndex] <- fst oldValue, newValue

//   member this.UpdateSource() =
//     for i = 0 to this.Rows.Length do
//       updateSlices(this.Pin, i, snd this.Rows.[i])

type [<Pojo>] CueListProps = { ``global``: GlobalModel; cueList: CueList }
type [<Pojo>] CueListState = { isOpen: bool }

type private CueListView(props) =
  inherit React.Component<CueListProps, CueListState>(props)
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
              failwith "TODO: Add spread to cue list"
              this.forceUpdate()
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
        div [Class "cueplayer-button iris-icon cueplayer-close level-item"] [
          span [Class "iris-icon iris-icon-close"] []
        ]
      ]
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

[<Pojo>]
type CuePlayerProps =
  { id: int;
    model: CuePlayerModel
    ``global``: GlobalModel }

type CuePlayerView(props) =
  inherit React.Component<CuePlayerProps, obj>(props)

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

  member this.render() =
    let cuePlayer =
      // TODO: Use a dropdown to choose the player
      this.props.``global``.state.cuePlayers
      |> Seq.head
      |> fun kv -> kv.Value
    let cueListCom =
      cuePlayer.CueList
      |> Option.bind (fun id -> Map.tryFind id this.props.``global``.state.cueLists)
      |> Option.map (fun cueList -> com<CueListView,_,_> {``global``=this.props.``global``; cueList=cueList} [])
    div [Class "cueplayer-container"] [
      // HEADER
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
        div [Class "cueplayer-button"] [str "Add Cue"]
        div [Class "cueplayer-button"] [str "Add Group"]
        div [Style [Clear "both"]] []
      ]
      // CUE LIST
      opt cueListCom
    ]
