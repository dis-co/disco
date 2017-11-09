module Iris.Web.InspectorView

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
open Iris.Web.Inspectors
open Helpers
open State
open Types

///  ____       _            _
/// |  _ \ _ __(_)_   ____ _| |_ ___
/// | |_) | '__| \ \ / / _` | __/ _ \
/// |  __/| |  | |\ V / (_| | ||  __/
/// |_|   |_|  |_| \_/ \__,_|\__\___|

let private empty dispatch model =
  div [] [
    Common.bar dispatch model
    div [
      Style [
        PaddingTop "10px"
        TextAlign "center"
      ]
    ] [
      str "Nothing selected."
    ]
  ]

let render dispatch (model: Model) =
  match model.history.selected with
  | Pin      (_,client,pin)   -> PinInspector.render        dispatch model client pin
  | PinGroup (_,client,group) -> PinGroupInspector.render   dispatch model client group
  | Client   (_,client)       -> ClientInspector.render     dispatch model client
  | Member   (_,mem)          -> MemberInspector.render     dispatch model mem
  | Cue      (_,cue)          -> CueInspector.render        dispatch model cue
  | CueList  (_,cuelist)      -> CueListInspector.render    dispatch model cuelist
  | Player   (_,player)       -> CuePlayerInspector.render  dispatch model player
  | Mapping  mapping          -> PinMappingInspector.render dispatch model mapping
  | Session  session          -> SessionInspector.render    dispatch model session
  | User     (_,user)         -> UserInspector.render       dispatch model user
  | Nothing                   -> empty dispatch model
