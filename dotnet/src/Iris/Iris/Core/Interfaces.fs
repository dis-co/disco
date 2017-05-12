[<AutoOpen>]
module Iris.Core.Interfaces

// * Imports

open System
open Iris.Core
open Iris.Raft
open Hopac

// * IAgentStore

type IAgentStore<'t when 't : not struct> =
  abstract State: 't
  abstract Update: 't -> unit

// * AgentStore module

module AgentStore =
  open System.Threading

  let create<'t when 't : not struct> (initial: 't) =
    let mutable state = initial

    { new IAgentStore<'t> with
        member self.State with get () = state
        member self.Update update =
          Interlocked.CompareExchange<'t>(&state, update, state)
          |> ignore }

// * GitEvent

type GitEvent =
  | Started of pid:int
  | Exited  of code:int
  | Pull    of pid:int * address:string * port:Port

// * RaftEvent

[<RequireQualifiedAccess;NoComparison;NoEquality>]
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
  | CreateSnapshot   of Ch<State option>
  | RetrieveSnapshot of Ch<RaftLogEntry option>
  | PersistSnapshot  of RaftLogEntry
  | RaftError        of IrisError

// * SocketEvent

[<NoComparison;NoEquality>]
type WebSocketEvent =
  | SessionAdded    of Id
  | SessionRemoved  of Id
  | OnMessage       of Id * StateMachine
  | OnError         of Id * Exception

// * ApiEvent

[<RequireQualifiedAccess>]
type ApiEvent =
  | Update        of StateMachine
  | ServiceStatus of ServiceStatus
  | ClientStatus  of IrisClient
  | Register      of IrisClient
  | UnRegister    of IrisClient

// * IrisEvent

[<NoComparison;NoEquality>]
type IrisEvent =
  | Git    of GitEvent
  | Socket of WebSocketEvent
  | Raft   of RaftEvent
  | Log    of LogEvent
  | Api    of ApiEvent
  | Status of ServiceStatus
