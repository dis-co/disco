module Iris.Web.Frontend

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Iris.Web.Lib

type ReactApp =
  abstract mount: StateInfo*((StateInfo->unit)->unit)->unit

let reactApp: ReactApp = importDefault "ReactApp"
let context = ClientContext.Start()

let awaitOnce (observable: IObservable<'T>)=
  let mutable disp = Unchecked.defaultof<IDisposable>
  Async.FromContinuations(fun (cont,_,_) ->
      disp <- observable.Subscribe(fun ev ->
        disp.Dispose()
        cont(ev)
  ))

async {
  let! initInfo = awaitOnce context.OnRender
    
  let initInfo =
    { initInfo with state = State.Empty } // Send empty state until session is authorized

  reactApp.mount(initInfo, fun f ->
    context.OnRender.Subscribe(f)
    |> ignore
  )

  registerKeyHandlers context
}
|> Async.StartImmediate



