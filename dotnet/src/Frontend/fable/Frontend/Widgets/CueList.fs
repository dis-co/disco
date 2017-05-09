module rec Iris.Web.Widgets.CueList

open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Helpers

module private Helpers =
  type RCom = React.ComponentClass<obj>
  let Clock: RCom = importDefault "../../../src/widgets/Clock"
  let SpreadView: RCom = importMember "../../../src/widgets/Spread"

  let inline Class x = ClassName x
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

type Cue(spread: ISpread) =
  member val Pin = spread.pin
  member val Rows = spread.rows
  member val Open = false with get, set
  member val UpdateView = true

  member this.Update(rowIndex, newValue) =
    let oldValue = this.Rows.[rowIndex]
    this.Rows.[rowIndex] <- fst oldValue, newValue

  member this.UpdateSource() =
    for i = 0 to this.Rows.Length do
      updateSlices(this.Pin, i, snd this.Rows.[i])

type CueList() =
  member val view = typeof<CueListView>
  member val name = "Cue List"
  member val cues = ResizeArray()
  member val layout =
    {
      x = 0; y = 0;
      w = 8; h = 5;
      minW = 2; maxW = 10;
      minH = 1; maxH = 10;
    }

[<Pojo>]
type CueProps =
  { id: int;
    model: CueList
    globalModel: GlobalModel }

type CueListView(props) =
    inherit React.Component<CueProps, obj>(props)
    let mutable el = Unchecked.defaultof<_>

    member this.UpdateSource() =
      failwith "TODO"

    member this.render() =
      let header =
        div [Class "level"]
          [div [Class "level-left"]
            [button [Class "button level-item"
                     Style [Margin 5]
                     OnClick(fun ev -> this.UpdateSource())]
              [str "Fire!"]
            ]
          ;div [Class "level-right"]
            [div [Class "level-item"]
              [from Clock %["global"==>this.props.globalModel] []]
            ]
        ]
      let rows =
        this.props.model.cues
        |> Seq.mapi (fun i cue ->
          let foo = from SpreadView %["model"==>cue; "global"==>this.props.globalModel] []
          div [Key (string i)] [from SpreadView %["model"==>cue; "global"==>this.props.globalModel] []])
        |> Seq.toList
      // Return value
      div [Class "iris-cuelist"; Ref(fun el' -> el <- el')] (header::rows)

