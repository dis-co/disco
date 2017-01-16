module Iris.Service.Interfaces

open System
open System.Collections.Concurrent
open Iris.Core
open Iris.Raft
open Iris.Service.Zmq

type CommandAgent = string -> Async<Either<IrisError,string>>

// ** GitEvent

type GitEvent =
  | Started of pid:int
  | Exited  of code:int
  | Pull    of pid:int * address:string * port:uint16

// ** IGitServer

type IGitServer =
  inherit IDisposable

  abstract Status    : Either<IrisError,ServiceStatus>
  abstract Pid       : Either<IrisError,int>
  abstract Subscribe : (GitEvent -> unit) -> IDisposable
  abstract Start     : unit -> Either<IrisError,unit>

  // ** RaftEvent

  type RaftEvent =
    | ApplyLog       of StateMachine
    | MemberAdded    of RaftMember
    | MemberRemoved  of RaftMember
    | MemberUpdated  of RaftMember
    | Configured     of RaftMember array
    | StateChanged   of RaftState * RaftState
    | CreateSnapshot of string

  // ** RaftAppContext

  [<NoComparison;NoEquality>]
  type RaftAppContext =
    { Status:      ServiceStatus
      Raft:        RaftValue
      Options:     IrisConfig
      Callbacks:   IRaftCallbacks
      Server:      Zmq.Rep
      Periodic:    IDisposable
      Connections: ConcurrentDictionary<Id,Req> }

    interface IDisposable with
      member self.Dispose() =
        dispose self.Periodic
        for KeyValue(_,connection) in self.Connections do
          dispose connection
        self.Connections.Clear()
        dispose self.Server

  // ** RaftServer

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
    abstract Connections   : Either<IrisError, ConcurrentDictionary<Id,Req>>

  // ** SocketEvent

  [<NoComparison;NoEquality>]
  type SocketEvent =
    | OnOpen    of Id
    | OnClose   of Id
    | OnMessage of Id * StateMachine
    | OnError   of Id * Exception

  // ** IWsServer

  type IWebSocketServer =
    inherit System.IDisposable
    abstract Send         : Id -> StateMachine -> Either<IrisError,unit>
    abstract Broadcast    : StateMachine -> Either<IrisError list,unit>
    abstract BuildSession : Id -> Session -> Either<IrisError,Session>
    abstract Subscribe    : (SocketEvent -> unit) -> System.IDisposable
    abstract Start        : unit -> Either<IrisError, unit>

  // ** IHttpServer

  type IHttpServer =
    inherit System.IDisposable
    abstract Start: unit -> Either<IrisError,unit>

  // ** IrisEvent

  [<NoComparison;NoEquality>]
  type IrisEvent =
    | Git    of GitEvent
    | Socket of SocketEvent
    | Raft   of RaftEvent
    | Log    of LogEvent
    | Status of ServiceStatus

  // ** IIrisServer

  /// Interface type to close over internal actors and state.
  type IIrisServer =
    inherit IDisposable
    abstract Config        : Either<IrisError,IrisConfig>
    abstract Status        : Either<IrisError,ServiceStatus>
    abstract GitServer     : Either<IrisError,IGitServer>
    abstract RaftServer    : Either<IrisError,IRaftServer>
    abstract SocketServer  : Either<IrisError,IWebSocketServer>
    abstract SetConfig     : IrisConfig -> Either<IrisError,unit>
    abstract Load          : FilePath   -> Either<IrisError,unit>
    abstract Periodic      : unit       -> Either<IrisError,unit>
    abstract ForceElection : unit       -> Either<IrisError,unit>
    abstract LeaveCluster  : unit       -> Either<IrisError,unit>
    abstract RmMember        : Id         -> Either<IrisError,EntryResponse>
    abstract AddMember       : RaftMember -> Either<IrisError,EntryResponse>
    abstract JoinCluster   : IpAddress  -> uint16 -> Either<IrisError,unit>
    abstract Subscribe     : (IrisEvent -> unit) -> IDisposable
