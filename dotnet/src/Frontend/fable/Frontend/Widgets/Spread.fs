module rec Iris.Web.Widgets.Spread

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props

importAll "../../../css/Spread.less"
importAll "../../../css/cuePlayer.css"

let [<Literal>] BASE_HEIGHT = 25
let [<Literal>] ROW_HEIGHT = 17

type [<Pojo>] InputState =
  { editIndex: int
  ; editText: string }

let addInputView
  ( index: int
  , value: obj
  , useRigthClick: bool
  , parent: React.Component<#obj, InputState>
  , update: int -> obj -> unit
  , generate: obj -> obj -> React.ReactElement
  ): React.ReactElement = importMember "../../../src/behaviors/input.tsx"

let [<Global>] jQuery(el: obj): obj = jsNative

let private pinToKeyValuePairs (pin: Pin) =
  let zip labels values =
    let labels =
      if Array.length labels = Array.length values
      then labels
      else Array.replicate values.Length ""
    Array.zip labels values
  match pin with
  | StringPin pin -> Array.map box pin.Values |> zip pin.Labels
  | NumberPin pin -> Array.map box pin.Values |> zip pin.Labels
  | BoolPin   pin -> Array.map box pin.Values |> zip pin.Labels
  // TODO: Apply transformations to the value of this pins?
  | BytePin   pin -> Array.map box pin.Values |> zip pin.Labels
  | EnumPin   pin -> Array.map box pin.Values |> zip pin.Labels
  | ColorPin  pin -> Array.map box pin.Values |> zip pin.Labels

let private updateSlices(pin: Pin, rowIndex, newValue: obj) =
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


type Spread(pin: Pin) =
  member __.view = typeof<SpreadView>
  member __.pin = pin
  member val ``open`` = false with get, set
  member val rows = pinToKeyValuePairs(pin)

  member __.update(rowIndex, newValue) =
    updateSlices(pin, rowIndex, newValue)


type [<Pojo>] SpreadProps =
  abstract model: Spread
  abstract ``global``: IGlobalModel
  abstract onDragStart: unit -> unit

type SpreadView(props) =
  inherit React.Component<SpreadProps, InputState>(props)
  do base.setInitState({ editIndex = -1; editText = "" })

  member this.recalculateHeight(rowCount: int) =
    BASE_HEIGHT + (ROW_HEIGHT * rowCount)

  member this.onMounted(el: Browser.Element) =
    if el <> null then
      !!jQuery(el)?resizable(
        createObj [
          "minWidth" ==> 150
          "handles" ==> "e"
          "resize" ==> fun event ui ->
              !!ui?size?height = !!ui?originalSize?height
        ])

  member this.renderRowLabels(model: Spread) = [
    yield span [
      Key "-1"
      Style [!!("cursor", "move")]
      OnMouseDown (fun ev -> ev.stopPropagation(); this.props.onDragStart())
    ] [str model.pin.Name]
    let mutable i = 0
    for i=0 to model.rows.Length - 1 do
      let key, _ = model.rows.[i]
      let key = if String.IsNullOrEmpty(key) then "Label" else key
      yield span [Key (string i)] [str key]
  ]

  member this.renderRowValues(model: Spread, useRightClick: bool) = [
    yield span [
      Key "-1"
    ] [str (sprintf "%O (%d)" (snd model.rows.[0]) model.rows.Length)]
    let mutable i = 0
    for i=0 to model.rows.Length - 1 do
      let _, value = model.rows.[i]
      yield addInputView
        (i, value, useRightClick, this,
         (fun i v -> model.update(i,v)),
         (fun value props -> span [] [!!value])) // TODO {...props}
  ]

  member this.render() =
    let model = this.props.model
    let arrowRotation = if model.``open`` then 90 else 0
    let height = if model.``open`` then this.recalculateHeight(model.rows.Length) else BASE_HEIGHT
    div [
      ClassName "iris-spread"
      Ref (fun el -> this.onMounted(el))
    ] [
      div [
        ClassName "iris-spread-child iris-flex-1"
        Style [Height height]
      ] (this.renderRowLabels(model))
      div [
        ClassName "iris-spread-child iris-flex-2"
        Style [Height height]
      ] (this.renderRowValues(model, this.props.``global``.state.useRightClick))
      div [
        ClassName "iris-spread-child iris-spread-end"
        Style [Height height]
      ] [
        img [
          Src "/img/more.png"
          Style [CSSProp.Transform (sprintf "rotate(%ideg)" arrowRotation)]
          OnClick (fun ev ->
            ev.stopPropagation()
            model.``open`` <- not model.``open``
            this.forceUpdate())
        ]
      ]
    ]


