module Main =

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Views

  let patchesMain _ =
    let widget = new Patches.Root()
    let ctrl = new ViewController<State, ClientContext> (widget)

    let context = new ClientContext()

    context.Controller <- ctrl
    context.Start()

    registerKeyHandlers context

  (*   __  __       _
      |  \/  | __ _(_)_ __
      | |\/| |/ _` | | '_ \
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)
  let main _ : unit =
    printfn "from main"


Main.main ()
