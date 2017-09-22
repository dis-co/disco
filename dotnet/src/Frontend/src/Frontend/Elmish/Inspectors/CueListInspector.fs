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

module CueInspector =

  let render dispatch (model: Model) (cue: Cue) =
    Common.render dispatch model "Cue" [
      Common.stringRow "Id"     (string cue.Id)
      Common.stringRow "Name"   (string cue.Name)
      Common.stringRow "Values" (string cue.Slices)
    ]
