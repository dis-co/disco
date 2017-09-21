namespace Iris.Web.Inspectors

open System
open System.Collections.Generic
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Iris.Web.Helpers
open Iris.Web.Types
open State

///  ____        _     _ _
/// |  _ \ _   _| |__ | (_) ___
/// | |_) | | | | '_ \| | |/ __|
/// |  __/| |_| | |_) | | | (__
/// |_|    \__,_|_.__/|_|_|\___|

module PinGroupInspector =
  let private renderPins (tag: string) dispatch (model: Model) (group: PinGroup) =
    tr [] [
      td [] [ str "pins go here..." ]
    ]

  let private renderClients tag dispatch model group =
    tr [] [
      td [] [ str "pins go here..." ]
    ]

  let render dispatch (model: Model) (group: PinGroup) =
    Common.render "Pin Group" [
      Common.stringRow "Id"      (string group.Id)
      Common.stringRow "Name"    (string group.Name)
      renderClients    "Clients"  dispatch model group
      renderPins       "Pins"     dispatch model group
    ]
