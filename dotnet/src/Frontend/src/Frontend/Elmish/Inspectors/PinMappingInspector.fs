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

module PinMappingInspector =

  let render dispatch (model: Model) (mapping: PinMapping) =
    Common.render dispatch model "Pin Mapping" [
      Common.stringRow "Id"     (string mapping.Id)
      Common.stringRow "Source" (string mapping.Source)
      Common.stringRow "Sinks"  (string mapping.Sinks)
    ]
