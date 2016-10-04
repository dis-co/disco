//  _____                _                 _   __  __       _
// |  ___| __ ___  _ __ | |_ ___ _ __   __| | |  \/  | __ _(_)_ __
// | |_ | '__/ _ \| '_ \| __/ _ \ '_ \ / _` | | |\/| |/ _` | | '_ \
// |  _|| | | (_) | | | | ||  __/ | | | (_| | | |  | | (_| | | | | |
// |_|  |_|  \___/|_| |_|\__\___|_| |_|\__,_| |_|  |_|\__,_|_|_| |_|

open Iris.Core
open Iris.Web.Core
open Iris.Web.Views
open Fable.Core

let widget = new Patches.Root()
let ctrl = new ViewController<State, ClientContext> (widget)

let context = new ClientContext()

[<Emit("Object.keys(buffers.Iris.Serialization.Raft)")>]
let buffers _ = failwith "ONLY JS"

context.Controller <- ctrl
context.Start()

registerKeyHandlers context
