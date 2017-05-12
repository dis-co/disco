[<AutoOpen>]
module Iris.Service.Interfaces

// * Imports

open System
open System.Collections.Concurrent
open Iris.Core
open Iris.Core.Commands
open Iris.Core.Discovery
open Iris.Raft
open Iris.Client
open Iris.Zmq
open Mono.Zeroconf
open Hopac

// * IDiscoveryService

type IDiscoveryService =
  inherit IDisposable
  abstract Services: Either<IrisError,Map<Id,RegisterService> * Map<Id,DiscoveredService>>
  abstract Subscribe: (DiscoveryEvent -> unit) -> IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Register: service:DiscoverableService -> Either<IrisError,IDisposable>

// * IGitServer

type IGitServer =
  inherit IDisposable

  abstract Status    : Either<IrisError,ServiceStatus>
  abstract Pid       : Either<IrisError,int>
  abstract Subscribe : (GitEvent -> unit) -> IDisposable
  abstract Start     : unit -> Either<IrisError,unit>


// * IRaftServer

type IRaftServer =
  inherit IDisposable
  abstract Start         : unit -> Either<IrisError, unit>
  abstract Member        : RaftMember
  abstract MemberId      : Id
  abstract Append        : StateMachine -> unit
  abstract ForceElection : unit -> unit
  abstract Status        : ServiceStatus
  abstract Subscribe     : (RaftEvent -> unit) -> IDisposable
  abstract Periodic      : unit -> unit
  // abstract JoinCluster   : IpAddress -> uint16 -> unit
  // abstract LeaveCluster  : unit -> unit
  abstract AddMember     : RaftMember -> unit
  abstract RemoveMember  : Id -> unit
  abstract Connections   : ConcurrentDictionary<Id,IClient>
  abstract Leader        : RaftMember option
  abstract IsLeader      : bool

// * IWsServer

type IWebSocketServer =
  inherit System.IDisposable
  abstract Send         : Id -> StateMachine -> Either<IrisError,unit>
  abstract Broadcast    : StateMachine -> Either<IrisError list,unit>
  abstract BuildSession : Id -> Session -> Either<IrisError,Session>
  abstract Subscribe    : (WebSocketEvent -> unit) -> System.IDisposable
  abstract Start        : unit -> Either<IrisError, unit>

// * IHttpServer

type IHttpServer =
  inherit System.IDisposable
  abstract Start: unit -> Either<IrisError,unit>

// * IIrisServer

/// Interface type to close over internal actors and state.
type IIrisServer =
  inherit IDisposable
  abstract Config        : Either<IrisError,IrisConfig>
  abstract Status        : Either<IrisError,ServiceStatus>
  abstract GitServer     : Either<IrisError,IGitServer>
  abstract RaftServer    : Either<IrisError,IRaftServer>
  abstract SocketServer  : Either<IrisError,IWebSocketServer>
  abstract SetConfig     : IrisConfig -> Either<IrisError,unit>
  abstract Periodic      : unit       -> Either<IrisError,unit>
  abstract ForceElection : unit       -> Either<IrisError,unit>
  abstract RmMember      : Id         -> Either<IrisError,EntryResponse>
  abstract AddMember     : RaftMember -> Either<IrisError,EntryResponse>
  abstract Subscribe     : (IrisEvent -> unit) -> IDisposable
  abstract LoadProject   : name:string * userName:string * password:Password * ?site:string -> Either<IrisError,unit>
  abstract UnloadProject : unit -> Either<IrisError,unit>
  abstract MachineStatus : Either<IrisError,MachineStatus>
  // abstract JoinCluster   : IpAddress  -> uint16 -> Either<IrisError,unit>
  // abstract LeaveCluster  : unit       -> Either<IrisError,unit>

// * IApiServer

type IApiServer =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Subscribe: (ApiEvent -> unit) -> IDisposable
  abstract Clients: Map<Id,IrisClient>
  abstract State: State with get, set
  abstract Update: sm:StateMachine -> unit
