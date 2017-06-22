namespace rec Iris.Net

// * Imports

open System
open System.Net
open System.Net.Sockets
open System.Text
open Iris.Core
open System.Collections.Concurrent

// * Request

type Request =
  { RequestId: Guid
    ConnectionId: Guid
    Body: byte array }

// * Request module

module Request =

  let create (id: Guid) (body: byte array) : Request =
    { RequestId = Guid.NewGuid()
      ConnectionId = id
      Body = body }

// * Response

type Response = Request

// * Response module

module Response =

  let fromRequest (request: Request) (body: byte array) =
    { RequestId = request.RequestId
      ConnectionId = request.ConnectionId
      Body = body }

// * Client

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

// ** ClientConfig

type ClientConfig =
  { PeerId: Id
    PeerAddress: IpAddress
    PeerPort: Port
    Timeout: Timeout }

// ** TcpClientEvent

type TcpClientEvent =
  | Connected    of peerid:Id * connectionId:Guid
  | Disconnected of peerid:Id * connectionId:Guid
  | Response     of Response

// ** IClient

type IClient =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract PeerId: Id
  abstract ConnectionId: Guid
  abstract Request: Request -> unit
  abstract Subscribe: (TcpClientEvent -> unit) -> IDisposable

// * Server

//  ____
// / ___|  ___ _ ____   _____ _ __
// \___ \ / _ \ '__\ \ / / _ \ '__|
//  ___) |  __/ |   \ V /  __/ |
// |____/ \___|_|    \_/ \___|_|

// ** ServerConfig

type ServerConfig =
  { Id: Id
    Listen: IpAddress
    Port: Port }

// ** TcpServerEvent

[<NoComparison;NoEquality>]
type TcpServerEvent =
  | Connect    of connectionId:Guid * ip:IPAddress * port:int
  | Disconnect of connectionId:Guid
  | Request    of Request

// ** IServer

type IServer =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Subscribe: (TcpServerEvent -> unit) -> IDisposable
  abstract Respond: Response -> unit

// * PubSub

type PubSubEvent =
  | Request of connectionId:Guid * data:byte array

type IPubSub =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Send: byte array -> unit
  abstract Subscribe: (PubSubEvent -> unit) -> IDisposable

// * Core module

module Core =

  [<Literal>]
  let MSG_LENGTH_OFFSET = 4             // int32 has 4 bytes

  [<Literal>]
  let ID_LENGTH_OFFSET = 16             // Guid has 16 bytes

  [<Literal>]
  let HEADER_OFFSET = 20                // total offset, e.g. MSG_LENGTH_OFFSET + ID_LENGTH_OFFSET

  [<Literal>]
  let BUFFER_SIZE = 4048

// * Socket module

module Socket =

  // ** isAlive

  let isAlive (socket:Socket) =
    not (socket.Poll(1, SelectMode.SelectRead) && socket.Available = 0)

  // ** dispose

  let dispose (socket:Socket) =
    try
      socket.Shutdown(SocketShutdown.Both)
      socket.Close()
    finally
      socket.Dispose()

  // ** checkState

  let rec checkState<'t>
    (socket: Socket)
    (subscriptions: ConcurrentDictionary<Guid,IObserver<'t>>)
    (ev: 't) =
    async {
      do! Async.Sleep(1000)             // check socket liveness ever second
      try
        if isAlive socket then
          return! checkState socket subscriptions ev
        else
          Observable.onNext subscriptions ev
      with
        | _ -> Observable.onNext subscriptions ev
    }
