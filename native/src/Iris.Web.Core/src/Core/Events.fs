namespace Iris.Web.Core

open WebSharper

[<AutoOpen>]
[<JavaScript>]
module Events =

  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Patch

  (*
          _                _____                 _
         / \   _ __  _ __ | ____|_   _____ _ __ | |_ ™
        / _ \ | '_ \| '_ \|  _| \ \ / / _ \ '_ \| __|
       / ___ \| |_) | |_) | |___ \ V /  __/ | | | |_
      /_/   \_\ .__/| .__/|_____| \_/ \___|_| |_|\__|
              |_|   |_|

      The AppEventT type models all possible state-changes the app can legally
      undergo. Using this design, we have a clean understanding of how data flows
      through the system, and have the compiler assist us in handling all possible
      states with total functions.

  *)

  type IOBoxEventT =
    | AddIOBox
    | RemoveIOBox
    | UpdateIOBox

  type PatchEventT =
    | AddPatch
    | UpdatePatch
    | RemovePatch

  type AppEventT =
    | Initialize
    | SaveEvent
    | UndoEvent
    | RedoEvent

  type AppEvent =
    | AppEvent   of Kind : AppEventT
    | IOBoxEvent of Kind : IOBoxEventT * IOBox : IOBox
    | PatchEvent of Kind : PatchEventT * Patch : Patch
    | UnknownEvent
