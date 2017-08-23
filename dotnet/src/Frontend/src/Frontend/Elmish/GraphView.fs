module Iris.Web.GraphView

open System
open System.Collections.Generic
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Iris.Web.Widgets
open Helpers
open State
open Types

let body dispatch (model: Model) =
  let pinGroups =
    match model.state with
    | Some state -> state.PinGroups
    | None -> Map.empty
  ul [Class "iris-graphview"] (
    pinGroups |> Seq.map (fun (KeyValue(gid, group)) ->
      li [Key (string gid)] [
        yield div [] [str (unwrap group.Name)]
        yield! group.Pins |> Seq.map (fun (KeyValue(pid, pin)) ->
          com<PinView.PinView,_,_>
            { key = string pid
              pin = pin
              useRightClick = model.userConfig.useRightClick
              slices = None
              updater = None
              onDragStart = Some(fun el ->
                Drag.Pin pin |> Drag.start el) } [])
      ]) |> Seq.toList)

let createWidget (id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.GraphView
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 2; maxW = 10
        minH = 2; maxH = 10 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 ->
            equalsRef s1.PinGroups s2.PinGroups
          | None, None -> true
          | _ -> false)
        (widget id this.Name None body dispatch)
        model
  }
