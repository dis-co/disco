module Iris.Web.Frontend

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open Iris.Raft
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

let context = new ClientContext()

type ReactApp =
  abstract mount: ((State->unit)->unit)->unit

let reactApp: ReactApp = importDefault "ReactApp"
reactApp.mount(fun f ->
  context.Subscribe(fun _ state -> f state)
)

context.Start()

registerKeyHandlers context
