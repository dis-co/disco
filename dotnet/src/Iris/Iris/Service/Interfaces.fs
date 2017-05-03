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

// * GitEvent

type GitEvent =
  | Started of pid:int
  | Exited  of code:int
  | Pull    of pid:int * address:string * port:Port

// * IGitServer

type IGitServer =
  inherit IDisposable

  abstract Status    : Either<IrisError,ServiceStatus>
  abstract Pid       : Either<IrisError,int>
  abstract Subscribe : (GitEvent -> unit) -> IDisposable
  abstract Start     : unit -> Either<IrisError,unit>

// * RaftEvent

[<NoComparison;NoEquality>]
type RaftEvent =
  | ApplyLog         of StateMachine
  | MemberAdded      of RaftMember
  | MemberRemoved    of RaftMember
  | MemberUpdated    of RaftMember
  | Configured       of RaftMember array
  | StateChanged     of RaftState * RaftState
  | CreateSnapshot   of Ch<State option>
  | RetrieveSnapshot of Ch<RaftLogEntry option>
  | PersistSnapshot  of RaftLogEntry

// * RaftAppContext

[<NoComparison;NoEquality>]
type RaftAppContext =
  { Status:      ServiceStatus
    Raft:        RaftValue
    Options:     IrisConfig
    Callbacks:   IRaftCallbacks
    Server:      IBroker
    Disposables: IDisposable list
    Connections: ConcurrentDictionary<Id,IClient> }

  interface IDisposable with
    member self.Dispose() =
      List.iter dispose self.Disposables
      for KeyValue(_,connection) in self.Connections do
        dispose connection
      self.Connections.Clear()
      dispose self.Server

// * IRaftServer

type IRaftServer =
  inherit IDisposable

  abstract Member        : Either<IrisError,RaftMember>
  abstract MemberId      : Either<IrisError,Id>
  abstract Load          : IrisConfig -> Either<IrisError, unit>
  abstract Unload        : unit -> Either<IrisError, unit>
  abstract Append        : StateMachine -> Either<IrisError, EntryResponse>
  abstract ForceElection : unit -> Either<IrisError, unit>
  abstract State         : Either<IrisError, RaftAppContext>
  abstract Status        : Either<IrisError, ServiceStatus>
  abstract Subscribe     : (RaftEvent -> unit) -> IDisposable
  abstract Start         : unit -> Either<IrisError, unit>
  abstract Periodic      : unit -> Either<IrisError, unit>
  abstract JoinCluster   : IpAddress -> uint16 -> Either<IrisError, unit>
  abstract LeaveCluster  : unit -> Either<IrisError, unit>
  abstract AddMember     : RaftMember -> Either<IrisError, EntryResponse>
  abstract RmMember      : Id -> Either<IrisError, EntryResponse>
  abstract Connections   : Either<IrisError, ConcurrentDictionary<Id,IClient>>
  abstract Leader        : Either<IrisError, RaftMember option>
  abstract IsLeader      : bool

// * SocketEvent

[<NoComparison;NoEquality>]
type WebSocketEvent =
  | SessionAdded    of Id
  | SessionRemoved  of Id
  | OnMessage       of Id * StateMachine
  | OnError         of Id * Exception

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
  abstract LeaveCluster  : unit       -> Either<IrisError,unit>
  abstract RmMember      : Id         -> Either<IrisError,EntryResponse>
  abstract AddMember     : RaftMember -> Either<IrisError,EntryResponse>
  abstract JoinCluster   : IpAddress  -> uint16 -> Either<IrisError,unit>
  abstract Subscribe     : (IrisEvent -> unit) -> IDisposable
  abstract LoadProject   : name:string * userName:string * password:Password * ?options:ProjectOptions -> Either<IrisError,unit>
  abstract UnloadProject : unit -> Either<IrisError,unit>

// * IApiServer

type IApiServer =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Subscribe: (ApiEvent -> unit) -> IDisposable
  abstract Clients: Either<IrisError,Map<Id,IrisClient>>
  abstract State: Either<IrisError,State>
  abstract Update: sm:StateMachine -> unit
  abstract SetState: state:State -> Either<IrisError,unit>
