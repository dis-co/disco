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
open Types

// jQuery
let [<Global("$")>] jQuery(arg: obj): obj = jsNative

// Syntactic sugar
let inline Class x = ClassName x
let inline (~%) o = createObj o
let inline (=>) x y = x ==> y
let inline equalsRef x y = obj.ReferenceEquals(x, y)

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

// Widget view
let widget (id: Guid) (name: string)
           (titleBar: _ option) (body: (Msg->unit)->Model->React.ReactElement)
           (dispatch: Msg->unit) (model: Model) =
  div [Class "iris-widget"] [
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
