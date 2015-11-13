namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Store =

  open Iris.Web.Core.Events

  (* Reducers are take a state, an action, acts and finally return the new state *)
  type Reducer<'a> = (AppEvent -> 'a -> 'a)

  (*
       ____  _
      / ___|| |_ ___  _ __ ___
      \___ \| __/ _ \| '__/ _ \
       ___) | || (_) | | |  __/
      |____/ \__\___/|_|  \___|

      The store centrally manages all state changes and notifies interested
      parties of changes to the carried state (e.g. views, socket transport).

  *)

  type Store<'a> (reducer : Reducer<'a>, state : 'a)=
    let mutable listeners : Listener<'a> list = List.empty
    let mutable state = state
    let reducer = reducer

    member private self.Notify (ev : AppEvent) =
        List.map (fun l -> l self ev) listeners

    member self.Dispatch (ev : AppEvent) : unit =
      state <- reducer ev state
      self.Notify ev |> ignore

    member self.Subscribe (listener : Listener<'a>) =
      listeners <- listener :: listeners

    member self.State with get ()= state

  and Listener<'a> = (Store<'a> -> AppEvent -> unit)
