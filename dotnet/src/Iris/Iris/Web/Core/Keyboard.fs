namespace Iris.Web.Core

[<AutoOpen>]
module Keyboard =

  open Fable.Core
  open Fable.Import
  open Fable.Import.Browser
  open Iris.Core
  open Iris.Web.Core

  type KeyBinding = (bool * bool * float * ClientMessage<State>)

  let knownActions : KeyBinding array =
    //  ctrl, shift, key, action
    [| (true, false, 90.0,  ClientMessage.Undo)
     ; (true, true,  90.0,  ClientMessage.Redo)
     ; (true, false, 83.0,  ClientMessage.Save)
     ; (true, false, 79.0,  ClientMessage.Open)
     |]

  let matches (ctx : ClientContext) (kev : KeyboardEvent) ((ctrl, shift, key, msg) : KeyBinding) =
    if kev.keyCode  = key   &&
       kev.shiftKey = shift &&
       kev.ctrlKey  = ctrl
    then
       ctx.Trigger(msg)
       kev.preventDefault()

  let keydownHandler (ctx : ClientContext) (ev : KeyboardEvent) =
    Array.iter (matches ctx ev) knownActions

  let registerKeyHandlers (ctx : ClientContext) =
    Browser.window.onkeydown <- fun e ->
      keydownHandler ctx e
      failwith "oh ho ho ho"
