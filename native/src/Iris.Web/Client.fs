namespace Iris.Web

open WebSharper
open WebSharper.JQuery
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views
    
  (*   __  __       _
      |  \/  | __ _(_)_ __
      | |\/| |/ _` | | '_ \
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)
  let Main : unit =
    let widget = new Patches.Root()
    let ctrl = new ViewController<State, ClientContext> (widget)

    let context = new ClientContext()

    context.Controller <- ctrl
    context.Start()

    registerKeyHandlers context
    
