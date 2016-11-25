[<AutoOpen>]
module Iris.Web.Core.Keyboard

open Fable.Core
open Fable.Import
open Fable.Import.Browser
open Iris.Core
open Iris.Web.Core

type KeyBinding = (bool * bool * float * StateMachine)

let knownActions : KeyBinding array =
  //  ctrl, shift, key, action
  [| (true, false, 90.0, StateMachine.Command AppCommand.Undo)
  ; (true, true,  90.0, StateMachine.Command AppCommand.Redo)
  |]

let matches (ctx : ClientContext) (kev : KeyboardEvent) ((ctrl, shift, key, msg) : KeyBinding) =
  if kev.keyCode  = key   &&
     kev.shiftKey = shift &&
     kev.ctrlKey  = ctrl
  then
    ctx.Post(msg)

let keydownHandler (ctx : ClientContext) (ev : KeyboardEvent) =
  Array.iter (matches ctx ev) knownActions

let registerKeyHandlers (ctx : ClientContext) =
  Browser.window.onkeydown <- fun e ->
    keydownHandler ctx e
    new obj()
