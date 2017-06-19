[<AutoOpen>]
module Iris.Service.Interfaces

// * Imports

open System
open System.Collections.Concurrent
open Iris.Core
open Iris.Raft
open Iris.Zmq
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
  abstract Services: Map<Id,RegisterService> * Map<Id,DiscoveredService>
  abstract Subscribe: (DiscoveryEvent -> unit) -> IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Register: service:DiscoverableService -> IDisposable

// * IResolver

type IResolver =
  inherit IDisposable
  abstract Pending: Map<Frame,Cue>
  abstract Update: StateMachine -> unit
  abstract Subscribe: (IrisEvent -> unit) -> IDisposable

// * IClock

type IClock =
  inherit IDisposable
  abstract Subscribe: (IrisEvent -> unit) -> IDisposable
  abstract Start: unit -> unit
  abstract Stop: unit -> unit
  abstract Running: bool with get
  abstract Fps: int16<fps>  with get, set
  abstract Frame: int64<frame>

// * IGitServer

type IGitServer =
  inherit IDisposable
  abstract Status    : ServiceStatus
  abstract Subscribe : (IrisEvent -> unit) -> IDisposable
  abstract Start     : unit -> Either<IrisError,unit>

// * IRaftSnapshotCallbacks

type IRaftSnapshotCallbacks =
  abstract PrepareSnapshot: unit -> State option
  abstract RetrieveSnapshot: unit -> RaftLogEntry option

// * IRaftServer

type IRaftServer =
  inherit IDisposable
  inherit ISink<IrisEvent>
  abstract Start         : unit -> Either<IrisError, unit>
  abstract Member        : RaftMember
  abstract MemberId      : Id
  abstract Append        : StateMachine -> unit
  abstract ForceElection : unit -> unit
  abstract Status        : ServiceStatus
  abstract Subscribe     : (IrisEvent -> unit) -> IDisposable
  abstract Periodic      : unit -> unit
  abstract AddMember     : RaftMember -> unit
  abstract RemoveMember  : Id -> unit
  abstract Connections   : ConcurrentDictionary<Id,IClient>
  abstract Leader        : RaftMember option
  abstract IsLeader      : bool
  abstract Raft          : RaftValue
  // abstract JoinCluster   : IpAddress -> uint16 -> unit
  // abstract LeaveCluster  : unit -> unit

// * IWebSocketServer

type IWebSocketServer =
  inherit IDisposable
  inherit ISink<IrisEvent>
  abstract Send         : Id -> StateMachine -> Either<IrisError,unit>
  abstract Broadcast    : StateMachine -> Either<IrisError list,unit>
  abstract Multicast    : except:Id -> StateMachine -> Either<IrisError list,unit>
  abstract BuildSession : Id -> Session -> Either<IrisError,Session>
  abstract Subscribe    : (IrisEvent -> unit) -> System.IDisposable
  abstract Start        : unit -> Either<IrisError, unit>

// * IApiServerCallbacks

type IApiServerCallbacks =
  abstract PrepareSnapshot: unit -> State

// * IApiServer

type IApiServer =
  inherit IDisposable
  inherit ISink<IrisEvent>
  abstract Start: unit -> Either<IrisError,unit>
  abstract Subscribe: (IrisEvent -> unit) -> IDisposable
  abstract Clients: Map<Id,IrisClient>
  abstract SendSnapshot: unit -> unit
  abstract Update: origin:Origin -> sm:StateMachine -> unit

// * IHttpServer

type IHttpServer =
  inherit System.IDisposable
  abstract Start: unit -> Either<IrisError,unit>

// * IrisServiceOptions

[<NoComparison;NoEquality>]
type IrisServiceOptions =
  { Machine: IrisMachine
    ProjectName: Name
    UserName: Name
    Password: Password
    SiteId: Id option }

// * IIrisService

/// Interface type to close over internal actors and state.
type IIrisService =
  inherit IDisposable
  abstract AddMember:     RaftMember -> unit
  abstract Append:        StateMachine -> unit
  abstract Config:        IrisConfig with get, set
  abstract ForceElection: unit       -> unit
  abstract GitServer:     IGitServer
  abstract Machine:       IrisMachine
  abstract Periodic:      unit       -> unit
  abstract Project:       IrisProject
  abstract RaftServer:    IRaftServer
  abstract RemoveMember:  Id         -> unit
  abstract SocketServer:  IWebSocketServer
  abstract Start:         unit -> Either<IrisError,unit>
  abstract Status:        ServiceStatus
  abstract Subscribe:     (IrisEvent -> unit) -> IDisposable
  // abstract JoinCluster   : IpAddress  -> uint16 -> Either<IrisError,unit>
  // abstract LeaveCluster  : unit       -> Either<IrisError,unit>

// * IrisOptions

type IrisOptions =
  { Machine: IrisMachine
    FrontendPath: FilePath option
    ProjectPath: FilePath option }

// * IIris

type IIris =
  inherit IDisposable
  abstract Machine: IrisMachine
  abstract HttpServer: IHttpServer
  abstract DiscoveryService: IDiscoveryService option
  abstract IrisService: IIrisService option
  abstract SaveProject: unit -> Either<IrisError,unit>
  abstract LoadProject: Name * UserName * Password * Id option -> Either<IrisError,unit>
  abstract UnloadProject: unit -> Either<IrisError,unit>
