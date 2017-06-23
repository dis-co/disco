namespace rec Iris.Net

// * Imports

open System
open System.IO
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

  // ** PeedId

  member request.PeerId
    with get () = Guid.toId request.ConnectionId

// * RequestBuilder

[<AllowNullLiteral>]
type IRequestBuilder =
  inherit IDisposable
  abstract IsFinished: bool
  abstract Start: data: byte array -> offset:int64 -> unit
  abstract Append: data: byte array -> offset:int64 -> count:int64 -> unit
  abstract Position: int64
  abstract BodyLength: int64
  abstract Finish: unit -> Request

// * RequestBuilder module

module RequestBuilder =

  // ** Constants

  [<Literal>]
  let MsgLengthSize = 8L      // int64 is 8 bytes long

  [<Literal>]
  let IdSize = 16L             // Guid has 16 bytes

  // length of body  | length of client id | length of request id
  // ------------------------------------------------------------
  // MSG_LENGTH_SIZE +       ID_SIZE       +      ID_SIZE
  // ------------------------------------------------------------
  let HeaderSize = MsgLengthSize + IdSize + IdSize

  // ** parseLength

  let private parseLength (data: byte array)  (offset: int64) =
    BitConverter.ToInt64(data, int offset)

  // ** unparseLength

  let private unparseLength (request: Request) =
    int64 request.Body.Length
    |> BitConverter.GetBytes

  // ** parseIdData

  let private parseIdData (data: byte array) (offset: int64) =
    use stream = new MemoryStream()
    stream.Write(data, int offset, int IdSize)
    stream.ToArray()

  // ** parseGuid

  let private parseGuid (data: byte array) (offset: int64) =
    try
      parseIdData data offset |> Guid |> Some
    with
      | exn ->
        printfn "[EXN] RequestBuilder.parseGuid: %s" exn.Message
        None

  // ** serialize

  let serialize (request: Request) : byte array =
    let totalSize = int RequestBuilder.HeaderSize + request.Body.Length
    let destination = Array.zeroCreate totalSize
    // copy the encoded message length to destination
    Array.Copy(
      RequestBuilder.unparseLength request,
      destination,
      MsgLengthSize)
    // copy the connection id (peer id) to destination
    Array.Copy(
      request.ConnectionId.ToByteArray(),
      0L,
      destination,
      MsgLengthSize,
      IdSize)
    // copy the request id to destination
    Array.Copy(
      request.RequestId.ToByteArray(),
      0L,
      destination,
      MsgLengthSize + IdSize,
      IdSize)
    // copy the message body to destination
    Array.Copy(
      request.Body,
      0L,
      destination,
      HeaderSize,
      int64 request.Body.Length)
    destination                         // done!

  // ** create

  let create () =
    let body = new MemoryStream()

    let mutable position = 0L
    let mutable bodyLength = 0L
    let mutable clientId = None
    let mutable requestId = None

    { new IRequestBuilder with
        member builder.Position
          with get () = position

        member builder.BodyLength
          with get () = bodyLength

        member builder.IsFinished
          with get () =
            Option.isSome requestId
            && Option.isSome clientId
            && body.Position = bodyLength

        member builder.Start (data: byte array) (offset: int64) =
          match clientId, requestId, bodyLength with
          | None, None, 0L ->
            if int64 data.Length < (HeaderSize + int64 offset) then
              "Buffer not big enough to complete this operation"
              |> InvalidOperationException
              |> raise
            else
              bodyLength <- parseLength data offset
              clientId   <- parseGuid data (offset + MsgLengthSize)
              requestId  <- parseGuid data (offset + MsgLengthSize + IdSize)
          | _ ->
            "This builder was already started"
            |> InvalidOperationException
            |> raise

        member builder.Append (data: byte array) (offset: int64) (count: int64) =
          match clientId, requestId with
          | Some client, Some id when body.Position < bodyLength ->
            body.Write(data, int offset, int count)
          | Some client, Some id -> ()
          | _ ->
            "You must call Start before appending data"
            |> InvalidOperationException
            |> raise

        member builder.Finish() =
          match clientId, requestId, builder.IsFinished with
          | Some client, Some id, true ->
            { RequestId = id
              ConnectionId = client
              Body = body.ToArray() }
          | _ ->
            "Request not done yet"
            |> InvalidOperationException
            |> raise

        member builder.Dispose() =
          body.Close()
          body.Dispose()
      }

// * Request module

module Request =

  let create (id: Guid) (body: byte array) : Request =
    { RequestId = Guid.NewGuid()
      ConnectionId = id
      Body = body }

  let serialize = RequestBuilder.serialize

// * Response

type Response = Request

// * IResponseBuilder

type IResponseBuilder = IRequestBuilder

// * ResponseBuilder module

module ResponseBuilder =

  let create () : IResponseBuilder = RequestBuilder.create()

// * Response module

module Response =

  let fromRequest (request: Request) (body: byte array) =
    { RequestId = request.RequestId
      ConnectionId = request.ConnectionId
      Body = body }

  let serialize (response: Response) = RequestBuilder.serialize response

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
  | Connected    of peerid:Id
  | Disconnected of peerid:Id
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
  | Request of peer:Id * data:byte array

type IPubSub =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Send: byte array -> unit
  abstract Subscribe: (PubSubEvent -> unit) -> IDisposable

// * Core module

module Core =

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
