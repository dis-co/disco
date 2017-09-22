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

module CueListInspector =

  let render dispatch (model: Model) (cuelist: CueList) =
    Common.render dispatch model "Cue List" [
      Common.stringRow "Id"     (string cuelist.Id)
      Common.stringRow "Name"   (string cuelist.Name)
      Common.stringRow "Groups" (string cuelist.Groups)
    ]
