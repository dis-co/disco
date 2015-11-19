namespace Iris.Web

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Web.Core
  open Iris.Web.Views

  (* FIXME: need to factor this out into a nice abstraction *)
  let handler (store : Store<State>) (ev : MessageEvent) : unit =
    let msg = JSON.Parse(ev.Data :?> string) :?> Message

    let parsed =
      match msg.Type with
        | "iris.patch.add"    -> PatchEvent (AddPatch,    msg.Payload :?> Patch)
        | "iris.patch.update" -> PatchEvent (UpdatePatch, msg.Payload :?> Patch)
        | "iris.patch.remove" -> PatchEvent (RemovePatch, msg.Payload :?> Patch)

        | "iris.iobox.add"    -> IOBoxEvent (AddIOBox,    msg.Payload :?> IOBox)
        | "iris.iobox.update" -> IOBoxEvent (UpdateIOBox, msg.Payload :?> IOBox)
        | "iris.iobox.remove" -> IOBoxEvent (RemoveIOBox, msg.Payload :?> IOBox)

        | _                   -> UnknownEvent

    in store.Dispatch parsed

  let onClose _ = Console.Log("closing")

  (*   __  __       _
      |  \/  | __ _(_)_ __
      | |\/| |/ _` | | '_ \
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)

  let Main : unit =
    let store  = new Store<State>(reducer, State.Empty)
    let widget = new Patches.Root()
    let ctrl   = new ViewController<State> (widget)

    ctrl.Render store
    store.Subscribe (fun store' _ -> ctrl.Render store')

    let socket = new WebSocket("ws://localhost:8080")

    socket.Onopen    <- (fun _   -> Console.Log("on open"))
    socket.Onerror   <- (fun err -> Console.Log("error:", err))
    socket.Onmessage <- (fun msg -> handler store msg)
