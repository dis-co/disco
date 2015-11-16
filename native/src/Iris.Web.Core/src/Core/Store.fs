namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Store =

  open Iris.Web.Core.Events

  (* Reducers are take a state, an action, acts and finally return the new state *)
  type Reducer<'a> = (AppEvent -> 'a -> 'a)

  (*   ____  _
      / ___|| |_ ___  _ __ ___
      \___ \| __/ _ \| '__/ _ \
       ___) | || (_) | | |  __/
      |____/ \__\___/|_|  \___|

      The store centrally manages all state changes and notifies interested
      parties of changes to the carried state (e.g. views, socket transport).

      Features:

      * time-traveleing debugger
      * undo/redo (to be implemented)
  *)

  type OldState<'a> = (AppEvent * 'a)

  type Store<'a> (reducer : Reducer<'a>, state : 'a)=
    let reducer = reducer
    let mutable tick = -1 
    let mutable state = state
    let mutable debug = false
    let mutable last : OldState<'a> = (AppEvent(Initialize), state)
    let mutable history : Array<OldState<'a>> = new Array<OldState<'a>>()
    let mutable listeners : Listener<'a> list = List.empty

    member private self.Tick (newtick : int) = 
      if newtick >= 0 && newtick < history.Length
      then
        tick <- newtick
        let (ev, state') = history.[tick]
        state <- state'
        self.Notify(ev) |> ignore

    member private self.Notify (ev : AppEvent) =
      List.map (fun l -> l self ev) listeners

    member self.Debug(debug' : bool) : unit =
      debug <- debug'
      if debug then
        history.Push(last) |> ignore
      else // reset
        tick <- -1
        state <- snd history.[history.Length - 1]
        history <- new Array<OldState<'a>>()

    member self.Dump() : Array<OldState<'a>> = history

    member self.Dispatch (ev : AppEvent) : unit =
      state <- reducer ev state // 1) create new state
      last <- (ev, state)       // 2) store this action the and state it produced
      self.Notify ev |> ignore  // 3) notify all listeners
      if debug                  // 4) save to history if debug mode is on
      then history.Push((ev,state)) |> ignore

    member self.Subscribe (listener : Listener<'a>) =
      listeners <- listener :: listeners

    member self.State with get () = state

    member self.Next() =
      if debug && tick >= 0 && tick < history.Length
      then self.Tick(tick + 1)

    member self.Previous() =
      // save the last state before starting to travel in time
      if debug && tick = -1 then
        tick <- history.Length - 1

      if debug && tick > 0
      then self.Tick(tick - 1)
      

  and Listener<'a> = (Store<'a> -> AppEvent -> unit)

