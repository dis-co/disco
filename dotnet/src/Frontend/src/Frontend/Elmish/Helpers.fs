module Iris.Web.Helpers

open System
open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Iris.Core
open Types

// jQuery
let [<Global("$")>] jQuery(arg: obj): obj = jsNative

// Syntactic sugar
let inline Class x = ClassName x
let inline (~%) o = createObj o
let inline (=>) x y = x ==> y
let inline equalsRef x y = obj.ReferenceEquals(x, y)
let inline distinctRef x y = not(obj.ReferenceEquals(x, y))

// Math
type Point = { x: float; y: float}

let distance (p1: Point) (p2: Point) =
  sqrt ((pown (p2.x - p1.x) 2) + (pown (p2.y - p1.y) 2))

// Same as Elmish LazyView with hooks
// for mount and unmount events
type [<Pojo>] HookProps<'model> = {
    model:'model
    render:unit->ReactElement
    equal:'model->'model->bool
    onMount: unit->unit
    onUnMount: unit->unit
}

type HookView<'model>(props) =
  inherit React.Component<HookProps<'model>, obj>(props)
  member this.componentDidMount() =
    this.props.onMount()
  member this.componentWillUnmount() =
    this.props.onUnMount()
  member this.shouldComponentUpdate(nextProps, nextState, nextContext) =
      not(this.props.equal this.props.model nextProps.model)
  member this.render () =
      this.props.render()

let inline hookViewWith
        (equal:'model->'model->bool)
        (onMount:unit->unit)
        (onUnMount:unit->unit)
        (view:'model->ReactElement)
        (state:'model) =
    com<HookView<_>,_,_>
      { model = state
        render = fun () -> view state
        equal = equal
        onMount = onMount
        onUnMount = onUnMount }
        []

type ElmishView = (Msg->unit)->Model->React.ReactElement

// Widget view
let widget (id: Guid) (name: string)
           (titleBar: ElmishView option) (body: ElmishView)
           (dispatch: Msg->unit) (model: Model) =
  div [Class "iris-widget"; Key (string id)] [
    div [Class "iris-draggable-handle"] [
      span [] [str name]
      div [Class "iris-title-bar"] [
        titleBar
        |> Option.map (fun titleBar -> titleBar dispatch model)
        |> opt
      ]
      div [Class "iris-window-control"] [
        button [
          Class "iris-button iris-icon icon-control icon-resize"
          OnClick(fun ev ->
            ev.stopPropagation()
            failwith "TODO" // AddTab id |> dispatch
          )
        ] []
        button [
          Class "iris-button iris-icon icon-control icon-close"
          OnClick(fun ev ->
            ev.stopPropagation()
            RemoveWidget id |> dispatch
          )
        ] []
      ]
    ]
    div [Class "iris-widget-body"] [
      body dispatch model
    ]
  ]

let [<Literal>] private mdir = "../../js/modals/"

let makeModal<'T> autoHide dispatch modal: JS.Promise<Choice<'T,unit>> =
  Fable.PowerPack.Promise.create (fun onSuccess _ ->
    let data, com =
      match modal with
      | Modal.AddMember          -> None,           importDefault (mdir+"AddMember")
      | Modal.CreateProject      -> None,           importDefault (mdir+"CreateProject")
      | Modal.LoadProject        -> None,           importDefault (mdir+"LoadProject")
      | Modal.AvailableProjects data     -> Some(box data), importDefault (mdir+"AvailableProjects")
      | Modal.ProjectConfig data -> Some(box data), importDefault (mdir+"ProjectConfig")
    let props =
      createObj ["data" ==> data
                 "onSubmit" ==> fun x ->
                  // Non-autoHide modals must take
                  // care of hiding themselves
                  if autoHide then
                    UpdateModal None |> dispatch
                  Choice1Of2 x |> onSuccess ]
    let view =
      div [ClassName "modal is-active"] [
        div [
          ClassName "modal-background"
          OnClick (fun ev ->
            ev.stopPropagation()
            if autoHide then
              UpdateModal None |> dispatch
              Choice2Of2 () |> onSuccess)
        ] []
        div [ClassName "modal-content"] [
          div [ClassName "box"] [from com props []]
        ]
      ]
    Some { modal = modal; view = view } |> UpdateModal |> dispatch)

module Promise =
  open Fable.PowerPack

  [<Emit("$2.then($0,$1)")>]
  let iterOrError (resolve: 'T->unit) (reject: Exception->unit) (pr: JS.Promise<'T>): unit = jsNative

  let race (p1: JS.Promise<'a>) (p2: JS.Promise<'b>) =
    let mutable fin = false
    let onSuccess resolve choice result =
      if not fin then
        fin <- true
        choice result |> resolve
    let onError reject er =
      if not fin then
        fin <- true
        reject er
    Promise.create(fun resolve reject ->
      p1 |> iterOrError (onSuccess resolve Choice1Of2) (onError reject)
      p2 |> iterOrError (onSuccess resolve Choice2Of2) (onError reject)
    )

module Array =
  open Iris.Core

  let inline replaceById< ^t when ^t : (member Id : IrisId)> (newItem : ^t) (ar: ^t[]) =
    Array.map (fun (x: ^t) -> if (^t : (member Id : IrisId) newItem) = (^t : (member Id : IrisId) x) then newItem else x) ar

  let insertAfter (i: int) (x: 't) (xs: 't[]) =
    let len = xs.Length
    if len = 0 (* && i = 0 *) then
      [|x|]
    elif i >= len then
      failwith "Index out of array bounds"
    elif i < 0 then
      Array.append [|x|] xs
    elif i = (len - 1) then
      Array.append xs [|x|]
    else
      let xs2 = Array.zeroCreate<'t> (len + 1)
      for j = 0 to len do
        if j <= i then
          xs2.[j] <- xs.[j]
        elif j = (i + 1) then
          xs2.[j] <- x
        else
          xs2.[j] <- xs.[j - 1]
      xs2
