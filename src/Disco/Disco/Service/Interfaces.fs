(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

[<AutoOpen>]
module Disco.Service.Interfaces

// * Imports

open System
open System.Collections.Concurrent
open Disco.Core
open Disco.Raft
open Disco.Net
open Mono.Zeroconf
open Disruptor
open Disruptor.Dsl

// * PipelineEvent

type PipelineEvent<'t>() =
  let mutable cell: 't option = None

  member ev.Event
    with get () = cell
    and set value = cell <- value

  member ev.Clear() =
    cell <- None

// * ISink

type ISink<'a> =
  abstract Publish: update:'a -> unit

// * IPipeline

type IPipeline<'a> =
  inherit IDisposable
  abstract Push: 'a -> unit

// * IHandler

type IHandler<'t> = IEventHandler<PipelineEvent<'t>>

// * IHandlerGroup

type IHandlerGroup<'t> = EventHandlerGroup<PipelineEvent<'t>>

// * EventProcessor

type EventProcessor<'t> = int64 -> bool -> 't -> unit

// * IDispatcher

type IDispatcher<'t> =
  inherit IDisposable
  abstract Start: unit -> unit
  abstract Status: ServiceStatus
  abstract Dispatch: 't -> unit

// * IDiscoveryService

type IDiscoveryService =
  inherit IDisposable
  abstract Services: Map<ServiceId,DiscoveredService>
  abstract Subscribe: (DiscoveryEvent -> unit) -> IDisposable
  abstract Start: unit -> Either<DiscoError,unit>
  abstract Register: project:DiscoProject -> unit
  abstract UnRegister: unit -> unit

// * IResolver

type IResolver =
  inherit IDisposable
  abstract Pending: Map<Frame,Cue>
  abstract Update: StateMachine -> unit
  abstract Subscribe: (DiscoEvent -> unit) -> IDisposable

// * IClock

type IClock =
  inherit IDisposable
  abstract Subscribe: (DiscoEvent -> unit) -> IDisposable
  abstract Start: unit -> unit
  abstract Stop: unit -> unit
  abstract Running: bool with get
  abstract Fps: int16<fps>  with get, set
  abstract Frame: int64<frame>

// * IGitServer

type IGitServer =
  inherit IDisposable
  abstract Status    : ServiceStatus
  abstract Subscribe : (DiscoEvent -> unit) -> IDisposable
  abstract Start     : unit -> Either<DiscoError,unit>

// * IFsWatcher

type IFsWatcher =
  inherit IDisposable
  abstract Subscribe: (DiscoEvent -> unit) -> IDisposable

// * IRaftSnapshotCallbacks

type IRaftSnapshotCallbacks =
  abstract PrepareSnapshot: unit -> State option
  abstract RetrieveSnapshot: unit -> LogEntry option

// * IRaftServer

type IRaftServer =
  inherit IDisposable
  inherit ISink<DiscoEvent>
  abstract Start         : unit -> Either<DiscoError, unit>
  abstract Member        : RaftMember
  abstract MemberId      : MemberId
  abstract Append        : StateMachine -> unit
  abstract ForceElection : unit -> unit
  abstract Status        : ServiceStatus
  abstract Subscribe     : (DiscoEvent -> unit) -> IDisposable
  abstract Periodic      : unit -> unit
  abstract AddMachine    : RaftMember -> unit
  abstract RemoveMachine : MemberId -> unit
  abstract Connections   : ConcurrentDictionary<PeerId,ITcpClient>
  abstract Leader        : RaftMember option
  abstract IsLeader      : bool
  abstract RaftState     : MemberState
  abstract Raft          : RaftState
  // abstract JoinCluster   : IpAddress -> uint16 -> unit
  // abstract LeaveCluster  : unit -> unit

// * IWebSocketServer

type IWebSocketServer =
  inherit IDisposable
  inherit ISink<DiscoEvent>
  abstract Send         : PeerId -> StateMachine -> unit
  abstract Sessions     : Map<SessionId,Session>
  abstract Broadcast    : StateMachine -> unit
  abstract Multicast    : except:SessionId -> StateMachine -> unit
  abstract BuildSession : SessionId -> Session -> Either<DiscoError,Session>
  abstract Subscribe    : (DiscoEvent -> unit) -> System.IDisposable
  abstract Start        : unit -> Either<DiscoError, unit>

// * IAssetService

type IAssetService =
  inherit IDisposable
  abstract State: FsTree option
  abstract Start: unit -> Either<DiscoError, unit>
  abstract Subscribe: (DiscoEvent -> unit) -> IDisposable
  abstract Stop: unit -> Either<DiscoError, unit>

// * IApiServerCallbacks

type IApiServerCallbacks =
  abstract PrepareSnapshot: unit -> State

// * IApiServer

type IApiServer =
  inherit IDisposable
  inherit ISink<DiscoEvent>
  abstract Start: unit -> Either<DiscoError,unit>
  abstract Subscribe: (DiscoEvent -> unit) -> IDisposable
  abstract Clients: Map<ClientId,DiscoClient>
  abstract SendSnapshot: unit -> unit
  abstract Update: origin:Origin -> sm:StateMachine -> unit

// * IHttpServer

type IHttpServer =
  inherit System.IDisposable
  abstract Start: unit -> Either<DiscoError,unit>

// * DiscoServiceOptions

[<NoComparison;NoEquality>]
type DiscoServiceOptions =
  { Machine: DiscoMachine
    ProjectName: Name
    UserName: Name
    Password: Password
    SiteId: (Name * SiteId) option }

// * IDiscoService

/// Interface type to close over internal actors and state.
type IDiscoService =
  inherit IDisposable
  abstract AddMachine:    RaftMember -> unit
  abstract Append:        StateMachine -> unit
  abstract Config:        DiscoConfig with get, set
  abstract ForceElection: unit       -> unit
  abstract GitServer:     IGitServer
  abstract Machine:       DiscoMachine
  abstract Periodic:      unit       -> unit
  abstract Project:       DiscoProject
  abstract RaftServer:    IRaftServer
  abstract AssetService:  IAssetService
  abstract RemoveMachine: MemberId         -> unit
  abstract SocketServer:  IWebSocketServer
  abstract Start:         unit -> Either<DiscoError,unit>
  abstract State:         State
  abstract Status:        ServiceStatus
  abstract Subscribe:     (DiscoEvent -> unit) -> IDisposable
  // abstract JoinCluster   : IpAddress  -> uint16 -> Either<DiscoError,unit>
  // abstract LeaveCluster  : unit       -> Either<DiscoError,unit>

// * DiscoOptions

type DiscoOptions =
  { Machine: DiscoMachine
    FrontendPath: FilePath option
    ProjectPath: FilePath option }

// * IDisco

type IDisco =
  inherit IDisposable
  abstract Machine: DiscoMachine
  abstract HttpServer: IHttpServer
  abstract DiscoveryService: IDiscoveryService option
  abstract DiscoService: IDiscoService option
  abstract SaveProject: unit -> Either<DiscoError,unit>
  abstract LoadProject: Name * UserName * Password * (Name * SiteId) option -> Either<DiscoError,unit>
  abstract UnloadProject: unit -> Either<DiscoError,unit>
