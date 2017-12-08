namespace Disco.Web.Inspectors

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
open Disco.Core
open Disco.Web.Core
open Disco.Web.Helpers
open Disco.Web.Types
open State

module PinMappingInspector =

  let render dispatch (model: Model) (mapping: PinMappingId) =
    match model.state with
    | None ->
      Common.render dispatch model "Pin Mapping" [
        str (string mapping + " (orphaned)")
      ]
    | Some state ->
      match Map.tryFind mapping state.PinMappings with
      | None ->
        Common.render dispatch model "Pin Mapping" [
          str (string mapping + " (orphaned)")
        ]
      | Some mapping ->
        Common.render dispatch model "Pin Mapping" [
          Common.stringRow "Id"     (string mapping.Id)
          Common.stringRow "Source" (string mapping.Source)
          Common.stringRow "Sinks"  (string mapping.Sinks)
        ]
