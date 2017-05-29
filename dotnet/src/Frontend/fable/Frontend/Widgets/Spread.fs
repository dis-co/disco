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

let private updatePinValue(pin: Pin, index: int, value: obj) =
  let updateArray (i: int) (v: obj) (ar: 'T[]) =
    let newArray = Array.copy ar
    newArray.[i] <- unbox v
    newArray
  match pin with
  | StringPin pin ->
    StringSlices(pin.Id, updateArray index value pin.Values)
  | NumberPin pin ->
    let value =
      match value with
      | :? string as v -> box(double v)
      | v -> v
    NumberSlices(pin.Id, updateArray index value pin.Values)
  | BoolPin pin ->
    let value =
      match value with
      | :? string as v -> box(v.ToLower() = "true")
      | v -> v
    BoolSlices(pin.Id, updateArray index value pin.Values)
  | BytePin   _pin -> failwith "TO BE IMPLEMENTED"
  | EnumPin   _pin -> failwith "TO BE IMPLEMENTED"
  | ColorPin  _pin -> failwith "TO BE IMPLEMENTED"
  |> UpdateSlices |> ClientContext.Singleton.Post

let (|NullOrEmpty|_|) str =
  if String.IsNullOrEmpty(str) then Some NullOrEmpty else None

type Spread(pin: Pin, ?slices: Slices, ?update: int->obj->unit) =
  let length =
    match slices with
    | Some slices -> slices.Length
    | None -> pin.Values.Length
  member val IsOpen = false with get, set
  member __.Pin = pin
  member __.Length = length
  member __.ValueAt(i) =
    match slices with
    | Some slices -> slices.[index i].Value
    | None -> pin.Values.[index i].Value

  member __.Update(index: int, value: obj) =
    match update with
    | Some update -> update index value
    | None -> updatePinValue(pin, index, value)


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
    ] [str <| if String.IsNullOrEmpty(model.Pin.Name) then "--" else model.Pin.Name]
    for i=0 to model.Length - 1 do
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
    ] [str (sprintf "%O (%d)" (model.ValueAt(0)) model.Length)]
    let mutable i = 0
    for i=0 to model.Length - 1 do
      let value = model.ValueAt(0)
      yield addInputView
        (i, value, useRightClick, this,
         (fun i v -> model.Update(i,v)),
         (fun value props -> createEl("span", props, value)))
  ]

  member this.render() =
    let model = this.props.model
    let arrowRotation = if model.IsOpen then 90 else 0
    let height = if model.IsOpen then this.recalculateHeight(model.Length) else BASE_HEIGHT
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


