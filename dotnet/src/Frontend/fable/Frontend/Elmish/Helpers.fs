module Iris.Web.Helpers

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser

open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props

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

let inline Class x = ClassName x