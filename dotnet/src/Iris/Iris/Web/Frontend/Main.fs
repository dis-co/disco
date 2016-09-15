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

// ------------------ 8< ------------------
[<Import("buffers","*")>]
module Serializations =
  let Bla : string = "hello"


[<Emit("flatbuffers")>]
let getit _ = failwith "ONLY JS"

[<Emit("buffers")>]
let gotit _ = failwith "ONLY JS"

open Serializations

printfn "flatbuffers: %A" (getit())
printfn "buffers: %A"     (gotit())
// ------------------ 8< ------------------

context.Controller <- ctrl
context.Start()

registerKeyHandlers context
