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
let [<Literal>] MIN_WIDTH = 100

type [<Pojo>] InputState =
  { editIndex: int
    editText: string
    isOpen: bool }

let addInputView
  ( index: int
  , value: obj
  , useRigthClick: bool
  , parent: React.Component<#obj, InputState>
  , update: int -> obj -> unit
  , generate: obj -> obj -> React.ReactElement
  ): React.ReactElement = importMember "../../../src/behaviors/input.tsx"

let formatValue(value: obj): string = importMember "../../../src/behaviors/input.tsx"

[<Global>]
let jQuery(el: obj): obj = jsNative

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

type [<Pojo>] SpreadProps =
  { key: string
    ``global``: IGlobalModel
    pin: Pin
    slices: Slices option
    update: (int->obj->unit) option
    onDragStart: (unit -> unit) option }

type SpreadView(props) =
  inherit React.Component<SpreadProps, InputState>(props)
  do base.setInitState({ editIndex = -1; editText = ""; isOpen = false })

  member this.RecalculateHeight(rowCount: int) =
    BASE_HEIGHT + (ROW_HEIGHT * rowCount)

  member this.ValueAt(i) =
    match this.props.slices with
    | Some slices -> slices.[index i].Value
    | None -> this.props.pin.Values.[index i].Value

  member this.UpdateValue(index: int, value: obj) =
    match this.props.update with
    | Some update -> update index value
    | None -> updatePinValue(this.props.pin, index, value)

  member this.RenderRowLabels(rowCount: int) = [
    yield span [
      Key "-1"
      Style [!!("cursor", "move")]
      OnMouseDown (fun ev ->
        ev.stopPropagation()
        match this.props.onDragStart with
        | Some onDragStart -> onDragStart()
        | None -> ())
    ] [str <| if String.IsNullOrEmpty(this.props.pin.Name) then "--" else this.props.pin.Name]
    if rowCount > 1 then
      for i=0 to rowCount - 1 do
        let label =
          // The Labels array can be shorter than Values'
          match Array.tryItem i this.props.pin.Labels with
          | None | Some(NullOrEmpty) -> "Label"
          | Some label -> label
        yield span [Key (string i)] [str label]
  ]

  member this.RenderRowValues(rowCount: int, useRightClick: bool) = [
    if rowCount > 1 then
      yield span [
        Key "-1"
      ] [str (sprintf "%s (%d)" (formatValue(this.ValueAt(0))) rowCount)]
    let mutable i = 0
    for i=0 to rowCount - 1 do
      let value = this.ValueAt(i)
      yield addInputView
        (i, value, useRightClick, this,
         (fun i v -> this.UpdateValue(i,v)),
         (fun value props -> createEl("span", props, value)))
  ]

  member this.RenderArrow() =
    let arrowRotation = if this.state.isOpen then 90 else 0
    img [
      Src "/img/more.png"
      Style [CSSProp.Transform (sprintf "rotate(%ideg)" arrowRotation)]
      OnClick (fun ev ->
        ev.stopPropagation()
        this.setState({ this.state with isOpen = not this.state.isOpen}))
    ]

  member this.onMounted(el: Browser.Element) =
    if el <> null then
      !!jQuery(el)?resizable(
        createObj [
          "minWidth" ==> MIN_WIDTH
          "handles" ==> "e"
          "resize" ==> fun event ui ->
              !!ui?size?height = !!ui?originalSize?height
        ])

  member this.render() =
    let rowCount =
      match this.props.slices with
      | Some slices -> slices.Length
      | None -> this.props.pin.Values.Length
    let height = if this.state.isOpen then this.RecalculateHeight(rowCount) else BASE_HEIGHT
    div [
      ClassName "iris-spread"
      Ref (fun el -> this.onMounted(el))
    ] [
      div [
        ClassName "iris-spread-child iris-flex-1"
        Style [Height height]
      ] (this.RenderRowLabels(rowCount))
      div [
        ClassName "iris-spread-child iris-flex-2"
        Style [Height height]
      ] (this.RenderRowValues(rowCount, this.props.``global``.state.useRightClick))
      div [
        ClassName "iris-spread-child iris-spread-end"
        Style [Height height]
      ] (if rowCount > 1 then [this.RenderArrow()] else [])
    ]


