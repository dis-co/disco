namespace Iris.Core

[<AutoOpen>]
module Store =

#if JAVASCRIPT
  open Fable.Core
  open Fable.Import
#endif

  (* Reducers are take a state, an action, acts and finally return the new state *)
  type Reducer<'a> = (ApplicationEvent -> 'a -> 'a)

  (* Action: Log entry for the Event occurred and the resulting state. *)
  type StoreAction<'a> = { Event : ApplicationEvent; State : 'a }
    with override self.ToString() : string =
                  sprintf "%s %s" (self.Event.ToString()) (self.State.ToString())

  //  _   _ _     _
  // | | | (_)___| |_ ___  _ __ _   _
  // | |_| | / __| __/ _ \| '__| | | |
  // |  _  | \__ \ || (_) | |  | |_| |
  // |_| |_|_|___/\__\___/|_|   \__, |
  //                            |___/

  type History<'a> (state : 'a) =
    let mutable depth = 10
    let mutable debug = false
    let mutable head = 1
    let mutable values = [ state ]

    (* - - - - - - - - - - Properties - - - - - - - - - - *)
    member self.Debug
      with get () = debug
      and  set b  =
        debug <- b
        if not debug then
          values <- List.take depth values

    member self.Depth
      with get () = depth
       and set n  = depth <- n

    member self.Values
      with get () = values

    member self.Length
      with get () = List.length values

    (* - - - - - - - - - - Methods - - - - - - - - - - *)
    member self.Append (value : 'a) : unit =
      head <- 0
      let newvalues = value :: values
      if (not debug) && List.length newvalues > depth then
        values <- List.take depth newvalues
      else
        values <- newvalues

    member self.Undo () : 'a option =
      let head' =
        if (head - 1) > (List.length values) then
          List.length values
        else
          head + 1

      if head <> head' then
        head <- head'

      List.tryItem head values

    member self.Redo () : 'a option =
      let head' =
        if   head - 1 < 0
        then 0
        else head - 1

      if head <> head' then
        head <- head'

      List.tryItem head values

  //  ____  _
  // / ___|| |_ ___  _ __ ___
  // \___ \| __/ _ \| '__/ _ \
  //  ___) | || (_) | | |  __/
  // |____/ \__\___/|_|  \___|

  // The store centrally manages all state changes and notifies interested
  // parties of changes to the carried state (e.g. views, socket transport).
  //
  // Features:
  //
  // - time-traveleing debugger
  // - undo/redo

  type Store<'a> (reducer : Reducer<'a>, state : 'a)=
    let reducer = reducer
    let mutable state = state
    let mutable history =
      new History<StoreAction<'a>>({ State = state; Event = Command(AppCommand.Reset) })

    let mutable listeners : Listener<'a> list = []

    // Notify all listeners of the ApplicationEvent change
    member private store.Notify (ev : ApplicationEvent) =
      List.iter (fun f -> f store ev) listeners

    // Turn debugging mode on or off.
    member self.Debug
      with get ()  = history.Debug
       and set dbg = history.Debug <- dbg

    (*
     * Number of undo steps to keep around.
     *
     * Overridden in debug mode.
     *)
    member self.UndoSteps
      with get () = history.Depth
       and set n  = history.Depth <- n

    (*
       Dispatch an action (ApplicationEvent) to be executed against the current
       version of the state to produce the next state.

       Notify all listeners of the change.

       Create a history item for this change if debugging is enabled.
     *)
    member self.Dispatch (ev : ApplicationEvent) : unit =
      match ev with
      | Command (AppCommand.Redo)  -> self.Redo()
      | Command (AppCommand.Undo)  -> self.Undo()
      | Command (AppCommand.Reset) -> ()   // do nothing for now
      | _ ->
        state <- reducer ev state          // 1) create new state
        self.Notify(ev)                   // 2) notify all listeners (render as soon as possible)
        history.Append({ Event = ev       // 3) store this action the and state it produced
                       ; State = state }) // 4) append to undo history

    (*
       Subscribe a callback to changes on the store.
     *)
    member self.Subscribe (listener : Listener<'a>) =
      listeners <- listener :: listeners

    (*
       Get the current version of the Store
     *)
    member self.State with get () = state

    member self.History with get () = history

    member self.Redo() =
      match history.Redo() with
        | Some log ->
          state <- log.State
          self.Notify log.Event |> ignore
        | _ -> ()

    member self.Undo() =
      match history.Undo() with
        | Some log ->
          state <- log.State
          self.Notify log.Event |> ignore
        | _ -> ()

  and Listener<'a> = (Store<'a> -> ApplicationEvent -> unit)
