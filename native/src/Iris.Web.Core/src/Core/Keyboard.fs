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

    
  let keydownHandler (ctx : ClientContext) (ev : obj) = 
    let kev = ev :?> KeyboardEvent

    if kev.CtrlKey && not kev.ShiftKey && kev.KeyCode = 90
    then ctx.Trigger(ClientMessage.Undo)

    if kev.CtrlKey && kev.ShiftKey && kev.KeyCode = 90
    then ctx.Trigger(ClientMessage.Redo)

    if kev.CtrlKey && kev.KeyCode = 83
    then ctx.Trigger(ClientMessage.Save)

    kev.PreventDefault true 


  let registerKeyHandlers (ctx : ClientContext) = 
    JS.Window.Onkeydown <- keydownHandler ctx
