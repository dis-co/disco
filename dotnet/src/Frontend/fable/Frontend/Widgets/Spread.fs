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

[<Import("createElement", from="react")>]
let private createEl(rcom: obj, props: obj, child: obj): React.ReactElement = jsNative

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

let (|NullOrEmpty|_|) str =
  if String.IsNullOrEmpty(str) then Some NullOrEmpty else None

type Spread(pin: Pin) =
  member val Pin = pin
  member val IsOpen = false with get, set

  member __.Update(rowIndex, newValue) =
    updateSlices(pin, rowIndex, newValue)


type [<Pojo>] SpreadProps =
  { key: string
  ; model: Spread
  ; ``global``: IGlobalModel
  ; onDragStart: (unit -> unit) option }

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
      OnMouseDown (fun ev ->
        ev.stopPropagation()
        match this.props.onDragStart with
        | Some onDragStart -> onDragStart()
        | None -> ())
    ] [str model.Pin.Name]
    for i=0 to model.Pin.Values.Length - 1 do
      let label =
        // The Labels array can be shorter than Values'
        match Array.tryItem i model.Pin.Labels with
        | None | Some(NullOrEmpty) -> "Label"
        | Some label -> label
      yield span [Key (string i)] [str label]
  ]

  member this.renderRowValues(model: Spread, useRightClick: bool) = [
    yield span [
      Key "-1"
    ] [str (sprintf "%O (%d)" (model.Pin.Values.[index 0].Value) model.Pin.Values.Length)]
    let mutable i = 0
    for i=0 to model.Pin.Values.Length - 1 do
      let value = model.Pin.Values.[index i].Value
      yield addInputView
        (i, value, useRightClick, this,
         (fun i v -> model.Update(i,v)),
         (fun value props -> createEl("span", props, value)))
  ]

  member this.render() =
    let model = this.props.model
    let arrowRotation = if model.IsOpen then 90 else 0
    let height = if model.IsOpen then this.recalculateHeight(model.Pin.Values.Length) else BASE_HEIGHT
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
            model.IsOpen <- not model.IsOpen
            this.forceUpdate())
        ]
      ]
    ]


