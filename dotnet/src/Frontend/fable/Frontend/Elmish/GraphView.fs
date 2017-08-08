module Iris.Web.GraphView

open System.Collections.Generic
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Iris.Web.Widgets
open Helpers
open State
open Types

let mapi (f: int->'a->'b->'c) (map: Map<'a,'b>): 'c list =
  map |> Seq.mapi (fun i (KeyValue(k,v)) -> f i k v) |> Seq.toList

let body dispatch (model: Model) =
  let pinGroups =
    match model.state with
    | Some state -> state.PinGroups
    | None -> Map.empty
  ul [Class "iris-graphview"] (
    pinGroups |> mapi (fun i _ group ->
      li [Key (string i)] [
        yield div [] [str (unwrap group.Name)]
        yield! group.Pins |> mapi (fun _ _ pin ->
          let key = string pin.Id
          div [Key key] [
            com<PinView.PinView,_,_>
              { key = key
                model = model
                pin = pin
                slices = None
                updater = None
                onDragStart = None } []
          ]
        )
      ]
    )
  )

let view id name dispatch model =
  widget id name None body dispatch model

let createGraphViewWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = "Graph View"
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 2; maxW = 10
        minH = 2; maxH = 10 }
    member this.Render(id, dispatch, model) =
      div [Key (string id)] [
        lazyViewWith
          (fun m1 m2 ->
            printfn "Checking GraphView update..."
            match m1.state, m2.state with
            | Some s1, Some s2 ->
              equalsRef s1.PinGroups s2.PinGroups
            | None, None -> true
            | _ -> false)
          (view id this.Name dispatch)
          model
      ]
  }
