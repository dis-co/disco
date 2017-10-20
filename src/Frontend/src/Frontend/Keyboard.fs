[<AutoOpen>]
module Iris.Web.Core.Keyboard

open Iris.Core
open Iris.Web.Core
open Fable.Import
open Fable.Import.Browser

type KeyBinding = (bool * bool * float * StateMachine)

let knownActions : KeyBinding array =
  //  ctrl, shift, key, action
  [| (true, false, 90.0, StateMachine.Command AppCommand.Undo)
     (true, true,  90.0, StateMachine.Command AppCommand.Redo)
     (true, false, 83.0, StateMachine.Command AppCommand.Save)
  |]

let matches (ctx : ClientContext) (kev : KeyboardEvent) ((ctrl, shift, key, msg) : KeyBinding) =
  if kev.keyCode  = key   &&
     kev.shiftKey = shift &&
     kev.ctrlKey  = ctrl
  then
    kev.preventDefault()
    ctx.Post(msg)

let keydownHandler (ctx : ClientContext) (ev : KeyboardEvent) =
  Array.iter (matches ctx ev) knownActions

let registerKeyHandlers (ctx : ClientContext) =
  Browser.window.onkeydown <- fun e ->
    keydownHandler ctx e
    new obj()

/// Sets the modifier key used for multiple selection
let isMultiSelection (ev: Browser.MouseEvent) =
  ev.altKey // ev.ctrlKey