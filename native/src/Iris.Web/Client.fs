namespace Iris.Web

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views

  type WorkerEvent = { data : string }

  (* FIXME: need to factor this out into a nice abstraction *)
  let handler (store : Store<State>) (ev : MessageEvent) : unit =
    let msg = JSON.Parse(ev.Data :?> string) :?> Message

    let parsed =
      match msg.Type with
        | Action.AddPatch    -> PatchEvent (AddPatch,    msg.Payload :?> Patch)
        | Action.UpdatePatch -> PatchEvent (UpdatePatch, msg.Payload :?> Patch)
        | Action.RemovePatch -> PatchEvent (RemovePatch, msg.Payload :?> Patch)

        | Action.AddIOBox    -> IOBoxEvent (AddIOBox,    msg.Payload :?> IOBox)
        | Action.UpdateIOBox -> IOBoxEvent (UpdateIOBox, msg.Payload :?> IOBox)
        | Action.RemoveIOBox -> IOBoxEvent (RemoveIOBox, msg.Payload :?> IOBox)

        // | _            -> UnknownEvent

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
