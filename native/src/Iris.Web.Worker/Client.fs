namespace Iris.Web.Worker

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views

  type WorkerEvent = { data : string }

  [<Direct "void (onmessage = $handler)">]
  let setMessageHandler (handler: WorkerEvent -> unit) = ()

  (*   __  __       _
      |  \/  | __ _(_)_ __
      | |\/| |/ _` | | '_ \
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)

  let Main : unit = setMessageHandler (fun ev -> Console.Log("Aww yeah"))
