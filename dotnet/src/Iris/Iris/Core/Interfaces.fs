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
  | Started
  | JoinedCluster
  | LeftCluster
  | ApplyLog         of StateMachine
  | MemberAdded      of RaftMember
  | MemberRemoved    of RaftMember
  | MemberUpdated    of RaftMember
  | Configured       of RaftMember array
  | StateChanged     of RaftState * RaftState
  | PersistSnapshot  of RaftLogEntry
  | RaftError        of IrisError

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () = Publish

// * SocketEvent

[<NoComparison;NoEquality>]
type WebSocketEvent =
  | SessionAdded    of Id
  | SessionRemoved  of Id
  | OnMessage       of Id * StateMachine
  | OnError         of Id * Exception

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () =
      match ev with
      | SessionAdded               _            -> Replicate
      | SessionRemoved             _            -> Replicate
      | OnError                    _            -> Replicate
      | OnMessage (_,UnloadProject)             -> Replicate
      | OnMessage (_,UpdateProject _)           -> Replicate
      | OnMessage (_,AddMember _)               -> Replicate
      | OnMessage (_,UpdateMember _)            -> Replicate
      | OnMessage (_,RemoveMember _)            -> Replicate
      | OnMessage (_,AddClient _)               -> Ignore
      | OnMessage (_,UpdateClient _)            -> Ignore
      | OnMessage (_,RemoveClient _)            -> Ignore
      | OnMessage (_,AddPinGroup _)             -> Ignore
      | OnMessage (_,UpdatePinGroup _)          -> Ignore
      | OnMessage (_,RemovePinGroup _)          -> Ignore
      | OnMessage (_,AddPin _)                  -> Replicate
      | OnMessage (_,UpdatePin _)               -> Replicate
      | OnMessage (_,RemovePin _)               -> Replicate
      | OnMessage (_,AddCue _)                  -> Replicate
      | OnMessage (_,UpdateCue _)               -> Replicate
      | OnMessage (_,RemoveCue _)               -> Replicate
      | OnMessage (_,AddCueList _)              -> Replicate
      | OnMessage (_,UpdateCueList _)           -> Replicate
      | OnMessage (_,RemoveCueList _)           -> Replicate
      | OnMessage (_,AddCuePlayer _)            -> Replicate
      | OnMessage (_,UpdateCuePlayer _)         -> Replicate
      | OnMessage (_,RemoveCuePlayer _)         -> Replicate
      | OnMessage (_,AddUser _)                 -> Replicate
      | OnMessage (_,UpdateUser _)              -> Replicate
      | OnMessage (_,RemoveUser _)              -> Replicate
      | OnMessage (_,AddSession _)              -> Ignore
      | OnMessage (_,UpdateSession _)           -> Ignore
      | OnMessage (_,RemoveSession _)           -> Ignore
      | OnMessage (_,AddDiscoveredService _)    -> Ignore
      | OnMessage (_,UpdateDiscoveredService _) -> Ignore
      | OnMessage (_,RemoveDiscoveredService _) -> Ignore
      | OnMessage (_,UpdateClock _)             -> Ignore
      | OnMessage (_,Command _)                 -> Replicate
      | OnMessage (_,DataSnapshot _)            -> Ignore
      | OnMessage (_,SetLogLevel _)             -> Replicate
      | OnMessage (_,LogMsg _)                  -> Publish
      | OnMessage (_,UpdateSlices _)            -> Publish
      | OnMessage (_,CallCue _)                 -> Publish

// * ApiEvent

[<RequireQualifiedAccess>]
type ApiEvent =
  | Update        of StateMachine
  | ServiceStatus of ServiceStatus
  | ClientStatus  of IrisClient
  | Register      of IrisClient
  | UnRegister    of IrisClient

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () =
      match ev with
      | ServiceStatus _                    -> Ignore
      | ClientStatus  _                    -> Replicate
      | Register      _                    -> Replicate
      | UnRegister    _                    -> Replicate
      | Update (UnloadProject)             -> Ignore
      | Update (UpdateProject _)           -> Ignore
      | Update (AddMember _)               -> Ignore
      | Update (UpdateMember _)            -> Ignore
      | Update (RemoveMember _)            -> Ignore
      | Update (AddClient _)               -> Ignore
      | Update (UpdateClient _)            -> Ignore
      | Update (RemoveClient _)            -> Ignore
      | Update (AddPinGroup _)             -> Replicate
      | Update (UpdatePinGroup _)          -> Replicate
      | Update (RemovePinGroup _)          -> Replicate
      | Update (AddPin _)                  -> Replicate
      | Update (UpdatePin _)               -> Replicate
      | Update (RemovePin _)               -> Replicate
      | Update (AddCue _)                  -> Replicate
      | Update (UpdateCue _)               -> Replicate
      | Update (RemoveCue _)               -> Replicate
      | Update (AddCueList _)              -> Replicate
      | Update (UpdateCueList _)           -> Replicate
      | Update (RemoveCueList _)           -> Replicate
      | Update (AddCuePlayer _)            -> Replicate
      | Update (UpdateCuePlayer _)         -> Replicate
      | Update (RemoveCuePlayer _)         -> Replicate
      | Update (AddUser _)                 -> Ignore
      | Update (UpdateUser _)              -> Ignore
      | Update (RemoveUser _)              -> Ignore
      | Update (AddSession _)              -> Ignore
      | Update (UpdateSession _)           -> Ignore
      | Update (RemoveSession _)           -> Ignore
      | Update (AddDiscoveredService _)    -> Ignore
      | Update (UpdateDiscoveredService _) -> Ignore
      | Update (RemoveDiscoveredService _) -> Ignore
      | Update (UpdateClock _)             -> Ignore
      | Update (Command _)                 -> Ignore
      | Update (DataSnapshot _)            -> Ignore
      | Update (SetLogLevel _)             -> Ignore
      | Update (LogMsg _)                  -> Publish
      | Update (UpdateSlices _)            -> Publish
      | Update (CallCue _)                 -> Publish

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
  | Git          of GitEvent
  | Socket       of WebSocketEvent
  | Raft         of RaftEvent
  | Log          of LogEvent
  | Api          of ApiEvent
  | Clock        of ClockEvent
  | Status       of ServiceStatus
  | Append       of StateMachine

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () =
      match ev with
      | Git    ev                          -> ev.DispatchStrategy
      | Socket ev                          -> ev.DispatchStrategy
      | Raft   ev                          -> ev.DispatchStrategy
      | Api    ev                          -> ev.DispatchStrategy
      | Clock  ev                          -> ev.DispatchStrategy
      | Log    _                           -> Publish
      | Status _                           -> Publish
      | Append (UnloadProject)             -> Replicate
      | Append (UpdateProject _)           -> Replicate
      | Append (AddMember _)               -> Replicate
      | Append (UpdateMember _)            -> Ignore
      | Append (RemoveMember _)            -> Replicate
      | Append (AddClient _)               -> Ignore
      | Append (UpdateClient _)            -> Ignore
      | Append (RemoveClient _)            -> Ignore
      | Append (AddPinGroup _)             -> Ignore
      | Append (UpdatePinGroup _)          -> Ignore
      | Append (RemovePinGroup _)          -> Ignore
      | Append (AddPin _)                  -> Ignore
      | Append (UpdatePin _)               -> Ignore
      | Append (RemovePin _)               -> Ignore
      | Append (AddCue _)                  -> Replicate
      | Append (UpdateCue _)               -> Replicate
      | Append (RemoveCue _)               -> Replicate
      | Append (AddCueList _)              -> Replicate
      | Append (UpdateCueList _)           -> Replicate
      | Append (RemoveCueList _)           -> Replicate
      | Append (AddCuePlayer _)            -> Replicate
      | Append (UpdateCuePlayer _)         -> Replicate
      | Append (RemoveCuePlayer _)         -> Replicate
      | Append (AddUser _)                 -> Replicate
      | Append (UpdateUser _)              -> Replicate
      | Append (RemoveUser _)              -> Replicate
      | Append (AddSession _)              -> Ignore
      | Append (UpdateSession _)           -> Ignore
      | Append (RemoveSession _)           -> Ignore
      | Append (AddDiscoveredService _)    -> Replicate
      | Append (UpdateDiscoveredService _) -> Replicate
      | Append (RemoveDiscoveredService _) -> Replicate
      | Append (UpdateClock _)             -> Ignore
      | Append (Command _)                 -> Replicate
      | Append (DataSnapshot _)            -> Ignore
      | Append (SetLogLevel _)             -> Replicate
      | Append (LogMsg _)                  -> Publish
      | Append (UpdateSlices _)            -> Publish
      | Append (CallCue _)                 -> Publish
