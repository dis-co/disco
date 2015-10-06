[<ReflectedDefinition>]
module Iris.Web.Types.Store

open FSharp.Html

open FunScript.TypeScript
open FunScript.VirtualDom

open Iris.Web.Types.IOBox
open Iris.Web.Types.Patch
open Iris.Web.Types.Events

(* Listener callback. *)
type Listener = (AppEvent -> unit)

(*   ____  _        _       
    / ___|| |_ __ _| |_ ___ 
    \___ \| __/ _` | __/ _ \
     ___) | || (_| | ||  __/
    |____/ \__\__,_|\__\___|

    Record type containing all the actual data that gets passed around in our
    application.
*)

type State =
  { Patches  : Patch list }
  static member empty = { Patches  = [] }

(* Reducers are take a state, an action, acts and finally return the new state *)
type Reducer = (AppEvent -> State -> State)

(*
     ____  _                 
    / ___|| |_ ___  _ __ ___ 
    \___ \| __/ _ \| '__/ _ \
     ___) | || (_) | | |  __/
    |____/ \__\___/|_|  \___|

    The store centrally manages all state changes and notifies interested
    parties of changes to the carried state (e.g. views, socket transport).

*)

type Store (rdcr : Reducer) =
  let         reducer   : Reducer       = rdcr
  let mutable state     : State         = State.empty
  let mutable listeners : Listener list = []

  let mutable locked : bool = false

  let notify ev =
    List.map (fun l -> l ev) listeners

  member self.Dispatch (ev : AppEvent) =
    if locked
    then Globals.console.log("oops store locked! what now?")
    else locked <- true
         state  <- reducer ev state
         locked <- false
    notify ev |> ignore

  member self.Subscribe (listener : AppEvent -> unit) =
    listeners <- listener :: listeners

  member self.GetState
    with get () = state

