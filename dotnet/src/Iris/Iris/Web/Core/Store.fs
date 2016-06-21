namespace Iris.Web.Core

[<AutoOpen>]
module Store =

  open Fable.Core
  open Fable.Import
  open Iris.Core

  (* Reducers are take a state, an action, acts and finally return the new state *)
  type Reducer<'a> = (AppEvent -> 'a -> 'a)

  (* Action: Log entry for the Event occurred and the resulting state. *)
  type Action<'a> = { Event : AppEvent; State : 'a }
    with override self.ToString() : string =
                  sprintf "%s %s" (self.Event.ToString()) (self.State.ToString())

  (*  _   _ _     _                   
   * | | | (_)___| |_ ___  _ __ _   _ 
   * | |_| | / __| __/ _ \| '__| | | |
   * |  _  | \__ \ || (_) | |  | |_| |
   * |_| |_|_|___/\__\___/|_|   \__, |
   *                            |___/ 
   * Wrap up undo/redo logic.
   *) 
  type History<'a> (state : 'a) =
    let mutable depth = 10
    let mutable debug = false
    let mutable head = 1
    let mutable values = [ state ]

    (* - - - - - - - - - - Properties - - - - - - - - - - *)
    member __.Debug
      with get () = debug
      and  set b  =
        debug <- b
        if not debug then
          let n = List.length values - depth
          values <- List.take n values

    member __.Depth
      with get () = depth
       and set n  = depth <- n

    member __.Values
      with get () = values

    member __.Length
      with get () = List.length values

    (* - - - - - - - - - - Methods - - - - - - - - - - *)
    member __.Append (value : 'a) : unit =
      head <- 0
      let newvalues = value :: values
      if (not debug) && List.length newvalues > depth then
        values <- List.take depth newvalues
      else 
        values <- newvalues

    member __.Undo () : 'a option =
      let head' =
        if (head - 1) > (List.length values) then
          List.length values
        else
          head + 1

      if head <> head' then
        head <- head'

      List.tryItem head values

    member __.Redo () : 'a option =
      let head' =
        if   head - 1 < 0
        then 0
        else head - 1

      if head <> head' then
        head <- head'

      List.tryItem head values


  (*   ____  _
   *  / ___|| |_ ___  _ __ ___
   *  \___ \| __/ _ \| '__/ _ \
   *   ___) | || (_) | | |  __/
   *  |____/ \__\___/|_|  \___|
   *
   *  The store centrally manages all state changes and notifies interested
   *  parties of changes to the carried state (e.g. views, socket transport).
   *
   *  Features:
   *
   *  - time-traveleing debugger
   *  - undo/redo (to be implemented)
   *)

  type Store<'a> (reducer : Reducer<'a>, state : 'a)=
    let reducer = reducer
    let mutable state = state
    let mutable history =
      new History<Action<'a>>({ State = state; Event = AppEvent(Initialize) })

    let mutable listeners : Listener<'a> list = List.empty

    (*
     * Notify all listeners of the AppEvent change
     *)
    member private store.Notify (ev : AppEvent) =
      List.iter (fun f -> f store ev) listeners

    (*
     * Turn debugging mode on or off.
     *
     * Makes sure the current state is the first element.
     *)
    member __.Debug(debug' : bool) : unit =
      history.Debug <- debug'

    (*
     * Number of undo steps to keep around.
     *
     * Overridden in debug mode.
     *)
    member __.UndoSteps
      with get () = history.Depth
       and set n  = history.Depth <- n

    (*
       Dispatch an action (AppEvent) to be executed against the current
       version of the state to produce the next state.

       Notify all listeners of the change.

       Create a history item for this change if debugging is enabled.
     *)
    member __.Dispatch (ev : AppEvent) : unit =
      state <- reducer ev state         // 1) create new state
      __.Notify ev                      // 2) notify all listeners (render as soon as possible)
      history.Append({ Event = ev       // 3) store this action the and state it produced
                     ; State = state }) // 4) append to undo history

    (*
       Subscribe a callback to changes on the store.
     *)
    member __.Subscribe (listener : Listener<'a>) =
      listeners <- listener :: listeners

    (*
       Get the current version of the Store
     *)
    member __.State with get () = state

    member __.History with get () = history

    member __.Redo() = 
      match history.Redo() with
        | Some log ->
          state <- log.State
          __.Notify log.Event |> ignore
        | _ -> ()

    member __.Undo() =
      match history.Undo() with
        | Some log -> 
          state <- log.State
          __.Notify log.Event |> ignore
        | _ -> ()

  and Listener<'a> = (Store<'a> -> AppEvent -> unit)

