module Iris.Web.Frontend

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open System
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Iris.Web.Lib

let context = ClientContext.Start()

type ReactApp =
  abstract mount: StateInfo*((StateInfo->unit)->unit)->unit

let reactApp: ReactApp = importDefault "ReactApp"
reactApp.mount({ context = context; state = Iris.Core.State.Empty }, fun f ->
  (context :> IObservable<_>).Subscribe(fun (ctx, state) ->
    f { context = ctx; state = state })
  |> ignore
)

registerKeyHandlers context
