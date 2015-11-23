namespace Iris.Web.Worker

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views

  type WorkerEvent = { ports : MessagePort array }

  [<Direct "void (onconnect = $handler)">]
  let onConnect (handler: WorkerEvent -> unit) = ()


  (*   __  __       _
      |  \/  | __ _(_)_ __
      | |\/| |/ _` | | '_ \
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)

  let Main : unit =
    onConnect (fun ev ->
                   let port = ev.ports.[0]
                   port.Onmessage <- (fun msg ->
                       port.PostMessage(msg.Data, Array.empty))
                   port.Start())
