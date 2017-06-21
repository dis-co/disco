module rec Iris.Web.Widgets.PinView

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props

let [<Literal>] BASE_HEIGHT = 25
let [<Literal>] ROW_HEIGHT = 17
let [<Literal>] MIN_WIDTH = 100

type [<Pojo>] InputState =
  { isOpen: bool }

let addInputView(index: int, value: obj, useRigthClick: bool, update: int -> obj -> unit): React.ReactElement =
  importMember "../../../src/behaviors/input.tsx"

let formatValue(value: obj): string =
  importMember "../../../src/behaviors/input.tsx"

[<Global>]
let jQuery(el: obj): obj = jsNative

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

type [<Pojo>] PinProps =
  { key: string
    ``global``: IGlobalModel
    pin: Pin
    slices: Slices option
    update: (int->obj->unit) option
    onDragStart: (unit -> unit) option }

type PinView(props) =
  inherit React.Component<PinProps, InputState>(props)
  do base.setInitState({ isOpen = false })

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

  member this.RenderRows(rowCount: int, useRightClick: bool) =
    let name = 
      if String.IsNullOrEmpty(this.props.pin.Name)
      then "--"
      else this.props.pin.Name
    let firstRowValue =
      if rowCount > 1
      then span [] [str (sprintf "%s (%d)" (formatValue(this.ValueAt(0))) rowCount)]
      else addInputView(0, this.ValueAt(0), useRightClick, (fun i v -> this.UpdateValue(0,v)))
    [
      yield div [Key "-1"; ClassName "iris-pin-child"] [
        span [
          Style [!!("cursor", "move")]
          OnMouseDown (fun ev ->
            ev.stopPropagation()
            match this.props.onDragStart with
            | Some onDragStart -> onDragStart()
            | None -> ())
        ] [str name]
        firstRowValue
      ]
      if this.state.isOpen then
        for i=0 to rowCount - 1 do
          let label =
            // The Labels array can be shorter than Values'
            match Array.tryItem i this.props.pin.Labels with
            | None | Some(NullOrEmpty) -> "Label"
            | Some label -> label          
          yield div [Key (string i); ClassName "iris-pin-child"] [
            span [Key (string i)] [str label]
            addInputView(i, this.ValueAt(i), useRightClick, (fun i v -> this.UpdateValue(i,v)))
          ]
    ]

  member this.RenderArrow() =
    let arrowRotation = if this.state.isOpen then 90 else 0
    img [
      Src "/lib/img/more.png"
      Style [CSSProp.Transform (sprintf "rotate(%ideg)" arrowRotation)]
      OnClick (fun ev ->
        ev.stopPropagation()
        this.setState({ this.state with isOpen = not this.state.isOpen}))
    ]

  // member this.onMounted(el: Browser.Element) =
  //   if el <> null then
  //     !!jQuery(el)?resizable(
  //       createObj [
  //         "minWidth" ==> MIN_WIDTH
  //         "handles" ==> "e"
  //         "resize" ==> fun event ui ->
  //             !!ui?size?height = !!ui?originalSize?height
  //       ])

  member this.render() =
    let rowCount =
      match this.props.slices with
      | Some slices -> slices.Length
      | None -> this.props.pin.Values.Length
    // let height = if this.state.isOpen then this.RecalculateHeight(rowCount) else BASE_HEIGHT
    div [
      ClassName "iris-pin"
      // Ref (fun el -> this.onMounted(el))
    ] (this.RenderRows(rowCount, this.props.``global``.state.useRightClick))
      // div [
      //   ClassName "iris-pin-end"
      // ] (if rowCount > 1 then [this.RenderArrow()] else [])

type [<Pojo>] PinGroupProps =
  { ``global``: IGlobalModel
    pinGroup: PinGroup
    pinAndSlices: (int * Pin * Slices)[]
    update: (int->int->obj->unit) option }
