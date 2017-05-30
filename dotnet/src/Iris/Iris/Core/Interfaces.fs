[<AutoOpen>]
module Iris.Core.Interfaces

// * Imports

open System
open Iris.Core
open Iris.Raft

// * IAgentStore

type IAgentStore<'t when 't : not struct> =
  abstract State: 't
  abstract Update: 't -> unit

// * AgentStore module

module AgentStore =
  open System.Threading

  let create<'t when 't : not struct> () =
    let mutable state = Unchecked.defaultof<'t>

    { new IAgentStore<'t> with
        member self.State with get () = state
        member self.Update update =
          Interlocked.CompareExchange<'t>(&state, update, state)
          |> ignore }

// * Origin

type Origin =
  | Raft
  | Web     of session:Id
  | Service of service:Id
  | Client  of client:Id

// * DispatchStrategy

type DispatchStrategy =
  | Replicate
  | Publish
  | Ignore

// * GitEvent

type GitEvent =
  | Started of pid:int
  | Exited  of code:int
  | Failed  of reason:string
  | Pull    of pid:int * address:string * port:Port

  // ** DispatchEvent

  member ev.DispatchStrategy
    with get () = Publish

// * RaftEvent

[<NoComparison;NoEquality>]
type RaftEvent =

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () = Publish

// * DiscoveryEvent

type DiscoveryEvent =
  | Status       of ServiceStatus
  | Registering  of DiscoverableService
  | UnRegistered of DiscoverableService
  | Registered   of DiscoverableService
  | Appeared     of DiscoveredService
  | Updated      of DiscoveredService
  | Vanished     of DiscoveredService

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () =
      match ev with
      | Status       _ -> Publish
      | Registering  _ -> Publish
      | Registered   _ -> Publish
      | UnRegistered _ -> Publish
      | Appeared     _ -> Replicate
      | Updated      _ -> Replicate
      | Vanished     _ -> Replicate

// * ClockEvent

type ClockEvent =
  { Frame: int64<frame>
    Deviation: int64<ns> }

  member tick.DispatchStrategy
    with get () = Publish

// * IrisEvent

[<NoComparison;NoEquality>]
type IrisEvent =
  | Configured      of members:RaftMember array
  | StateChanged    of oldstate:RaftState * newstate:RaftState
  | PersistSnapshot of log:RaftLogEntry
  | RaftError       of error:IrisError
  | Status          of ServiceStatus
  | Append          of origin:Origin * cmd:StateMachine
  | SessionOpened   of session:Id
  | SessionClosed   of session:Id
  | Git             of ev:GitEvent

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () =
      match ev with
      | Status _                                             -> Publish

      | Git _                                                -> Ignore

      | Configured      _
      | StateChanged    _                                    -> Publish
      | PersistSnapshot _
      | RaftError       _                                    -> Ignore

      | SessionOpened   _
      | SessionClosed   _                                    -> Replicate

      | Append (Origin.Raft, _)                              -> Publish // all raft events get published

      | Append (Origin.Web _, UnloadProject)                 -> Replicate
      | Append (Origin.Client _, UnloadProject)              -> Ignore
      | Append (Origin.Service _, UnloadProject)             -> Replicate

      | Append (Origin.Web _, UpdateProject _)
      | Append (Origin.Client _, UpdateProject _)
      | Append (Origin.Service _, UpdateProject _)           -> Replicate

      | Append (Origin.Web _, AddMember _)                   -> Replicate
      | Append (Origin.Web _, UpdateMember _)                -> Ignore
      | Append (Origin.Web _, RemoveMember _)                -> Replicate
      | Append (Origin.Client _, AddMember _)
      | Append (Origin.Client _, UpdateMember _)
      | Append (Origin.Client _, RemoveMember _)             -> Ignore // ignore all member ops from clients
      | Append (Origin.Service _, AddMember _)
      | Append (Origin.Service _, UpdateMember _)
      | Append (Origin.Service _, RemoveMember _)            -> Replicate

      | Append (Origin.Web _, AddClient _)
      | Append (Origin.Web _, UpdateClient _)
      | Append (Origin.Web _, RemoveClient _)
      | Append (Origin.Client _, AddClient _)
      | Append (Origin.Client _, UpdateClient _)
      | Append (Origin.Client _, RemoveClient _)             -> Ignore
      | Append (Origin.Service _, AddClient _)
      | Append (Origin.Service _, UpdateClient _)
      | Append (Origin.Service _, RemoveClient _)            -> Replicate

      | Append (Origin.Web _, AddPinGroup _)
      | Append (Origin.Web _, UpdatePinGroup _)
      | Append (Origin.Web _, RemovePinGroup _)              -> Ignore // ignore modifications from web
      | Append (Origin.Client _, AddPinGroup _)
      | Append (Origin.Client _, UpdatePinGroup _)
      | Append (Origin.Client _, RemovePinGroup _)           -> Replicate // replicate modifications
      | Append (Origin.Service _, AddPinGroup _)
      | Append (Origin.Service _, UpdatePinGroup _)
      | Append (Origin.Service _, RemovePinGroup _)          -> Ignore

      | Append (Origin.Web _, AddPin _)                      -> Ignore
      | Append (Origin.Web _, UpdatePin _)                   -> Replicate
      | Append (Origin.Web _, RemovePin _)                   -> Ignore
      | Append (Origin.Client _, AddPin _)
      | Append (Origin.Client _, UpdatePin _)
      | Append (Origin.Client _, RemovePin _)                -> Replicate
      | Append (Origin.Service _, AddPin _)
      | Append (Origin.Service _, UpdatePin _)
      | Append (Origin.Service _, RemovePin _)               -> Ignore

      | Append (Origin.Web _, AddCue _)
      | Append (Origin.Web _, UpdateCue _)
      | Append (Origin.Web _, RemoveCue _)
      | Append (Origin.Client _, AddCue _)
      | Append (Origin.Client _, UpdateCue _)
      | Append (Origin.Client _, RemoveCue _)                -> Replicate
      | Append (Origin.Service _, AddCue _)
      | Append (Origin.Service _, UpdateCue _)
      | Append (Origin.Service _, RemoveCue _)               -> Ignore


      | Append (Origin.Web _, AddCueList _)
      | Append (Origin.Web _, UpdateCueList _)
      | Append (Origin.Web _, RemoveCueList _)
      | Append (Origin.Client _, AddCueList _)
      | Append (Origin.Client _, UpdateCueList _)
      | Append (Origin.Client _, RemoveCueList _)            -> Replicate
      | Append (Origin.Service _, AddCueList _)
      | Append (Origin.Service _, UpdateCueList _)
      | Append (Origin.Service _, RemoveCueList _)           -> Ignore

      | Append (Origin.Web _, AddCuePlayer _)
      | Append (Origin.Web _, UpdateCuePlayer _)
      | Append (Origin.Web _, RemoveCuePlayer _)
      | Append (Origin.Client _, AddCuePlayer _)
      | Append (Origin.Client _, UpdateCuePlayer _)
      | Append (Origin.Client _, RemoveCuePlayer _)          -> Replicate
      | Append (Origin.Service _, AddCuePlayer _)
      | Append (Origin.Service _, UpdateCuePlayer _)
      | Append (Origin.Service _, RemoveCuePlayer _)         -> Ignore

      | Append (Origin.Web _, AddUser _)
      | Append (Origin.Web _, UpdateUser _)
      | Append (Origin.Web _, RemoveUser _)                  -> Replicate
      | Append (Origin.Client _, AddUser _)
      | Append (Origin.Client _, UpdateUser _)
      | Append (Origin.Client _, RemoveUser _)
      | Append (Origin.Service _, AddUser _)
      | Append (Origin.Service _, UpdateUser _)
      | Append (Origin.Service _, RemoveUser _)              -> Ignore

      | Append (Origin.Web _, AddSession _)
      | Append (Origin.Web _, UpdateSession _)
      | Append (Origin.Web _, RemoveSession _)               -> Replicate
      | Append (Origin.Client _, AddSession _)
      | Append (Origin.Client _, UpdateSession _)
      | Append (Origin.Client _, RemoveSession _)
      | Append (Origin.Service _, AddSession _)
      | Append (Origin.Service _, UpdateSession _)
      | Append (Origin.Service _, RemoveSession _)           -> Ignore

      | Append (Origin.Web _, AddDiscoveredService _)
      | Append (Origin.Web _, UpdateDiscoveredService _)
      | Append (Origin.Web _, RemoveDiscoveredService _)
      | Append (Origin.Client _, AddDiscoveredService _)
      | Append (Origin.Client _, UpdateDiscoveredService _)
      | Append (Origin.Client _, RemoveDiscoveredService _)  -> Ignore
      | Append (Origin.Service _, AddDiscoveredService _)
      | Append (Origin.Service _, UpdateDiscoveredService _)
      | Append (Origin.Service _, RemoveDiscoveredService _) -> Replicate

      | Append (Origin.Service _, UpdateClock _)             -> Publish
      | Append (_, UpdateClock _)                            -> Ignore

      | Append (Origin.Web _, Command _)
      | Append (Origin.Client _, Command _)
      | Append (Origin.Service _, Command _)                 -> Replicate

      | Append (Origin.Web _, DataSnapshot _)
      | Append (Origin.Client _, DataSnapshot _)
      | Append (Origin.Service _, DataSnapshot _)            -> Ignore

      | Append (_, SetLogLevel _)                            -> Replicate
      | Append (_, LogMsg _)                                 -> Publish
      | Append (_, UpdateSlices _)                           -> Publish
      | Append (_, CallCue _)                                -> Publish
