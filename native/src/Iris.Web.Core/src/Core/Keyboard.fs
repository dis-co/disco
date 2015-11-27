namespace Iris.Web.Core

open WebSharper
open WebSharper.JQuery
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module Keyboard = 

  open Iris.Core.Types
  open Iris.Web.Core

  [<Stub>]
  type KeyboardEvent() =
    [<DefaultValue>]
    [<Name "ctrlKey">]
    val mutable CtrlKey  : bool

    [<DefaultValue>]
    [<Name "shiftKey">]
    val mutable ShiftKey : bool
    
    [<DefaultValue>]
    [<Name "keyCode">]
    val mutable KeyCode  : int

    [<Stub>]
    [<Name "preventDefault">]
    member __.PreventDefault (arg : bool) : unit = X

  type KeyBinding = (bool * bool * int * ClientMessage<State>)

  let knownActions : KeyBinding array =
    //  ctrl, shift, key, action
    [| (true, false, 90,  ClientMessage.Undo)
     ; (true, true,  90,  ClientMessage.Redo)
     ; (true, false, 83,  ClientMessage.Save)
     ; (true, false, 79,  ClientMessage.Open)
     |]
    
  let matches (ctx : ClientContext) (kev : KeyboardEvent) ((ctrl, shift, key, msg) : KeyBinding) : unit =
    if kev.KeyCode  = key   &&
       kev.ShiftKey = shift &&
       kev.CtrlKey  = ctrl
    then
       ctx.Trigger(msg)
       kev.PreventDefault true 

  let keydownHandler (ctx : ClientContext) (ev : obj) = 
    let kev = ev :?> KeyboardEvent
    Array.iter (matches ctx kev) knownActions

  let registerKeyHandlers (ctx : ClientContext) = 
    JS.Window.Onkeydown <- keydownHandler ctx
