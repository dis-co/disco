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
  abstract Services: Map<Id,RegisterService> * Map<Id,DiscoveredService>
  abstract Subscribe: (DiscoveryEvent -> unit) -> IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Register: service:DiscoverableService -> IDisposable

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
  abstract AddMember     : RaftMember -> unit
  abstract RemoveMember  : Id -> unit
  abstract Connections   : ConcurrentDictionary<Id,IClient>
  abstract Leader        : RaftMember option
  abstract IsLeader      : bool
  abstract Raft          : RaftValue
  // abstract JoinCluster   : IpAddress -> uint16 -> unit
  // abstract LeaveCluster  : unit -> unit

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

// * IApiServer

type IApiServer =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Subscribe: (ApiEvent -> unit) -> IDisposable
  abstract Clients: Map<Id,IrisClient>
  abstract State: State with get, set
  abstract Update: sm:StateMachine -> unit

// * IrisServiceOptions

[<NoComparison;NoEquality>]
type IrisServiceOptions =
  { Machine: IrisMachine
    ProjectName: Name
    UserName: Name
    Password: Password
    SiteName: Name option }

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
  abstract LoadProject: Name * UserName * Password * Name option -> Either<IrisError,unit>
  abstract UnloadProject: unit -> Either<IrisError,unit>
