module Iris.Web.Frontend

//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open Iris.Core
open Iris.Web.Core
open Iris.Web.Core.DomDelegator
open Iris.Web.Views
open Fable.Core

let delegator = Delegator.Create()
delegator.ListenTo "play"

let widget = new Patches.Root()
let ctrl = new ViewController<State, ClientContext> (widget)

let context = new ClientContext()

context.Controller <- ctrl
context.Start()

registerKeyHandlers context
