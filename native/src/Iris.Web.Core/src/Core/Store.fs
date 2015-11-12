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

  type Store<'a> =
    { Reducer   : Reducer<'a>
    ; State     : 'a
    ; Listeners : Listener<'a> list }

  and Listener<'a> = (Store<'a> -> AppEvent -> unit)

  let private notify (store : Store<'a>) (ev : AppEvent) =
    List.map (fun l -> l store ev) store.Listeners

  let dispatch (store : Store<'a>) (ev : AppEvent) : Store<'a> =
    let newstate = store.Reducer ev store.State
    let newstore = { store with State = newstate }
    notify newstore ev |> ignore
    newstore

  let subscribe (store : Store<'a>) (listener : Listener<'a>) =
    { store with Listeners = listener :: store.Listeners }

  let mkStore (reducer : Reducer<'a>) (state : 'a) =
    { Reducer = reducer
    ; State   = state
    ; Listeners = []
    }
