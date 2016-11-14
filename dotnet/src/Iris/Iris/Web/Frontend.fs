module Iris.Web.Frontend

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Import
module R = Fable.Helpers.React

let context = new ClientContext()

type AppView(props, ctx) as this =
    inherit React.Component<obj, State>(props, ctx)
    //let dispatch = context.Trigger
    do context.Subscribe(fun _ state ->
      printfn "%A" state
      this.setState state)

    member this.render() =
        R.div [] [R.str "foo"]

ReactDom.render(
  R.com<AppView,_,_> None [],
  Browser.document.getElementById "app") |> ignore

context.Start()

registerKeyHandlers context
