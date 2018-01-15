(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace rec Disco.Net

#nowarn "52" // "this value has been copied"

// * Imports

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading
open System.Text
open Disco.Core
open System.Collections.Concurrent

// * IBuffer

type IBuffer =
  inherit IDisposable
  abstract Length: int
  abstract Data: byte array
  abstract Id: int

// * IBufferManager

type IBufferManager =
  abstract InUse: int
  abstract Available: int
  abstract NumBuffers: int
  abstract BufferSize: int
  abstract TakeBuffer: unit -> IBuffer

// * BufferManager

module BufferManager =

  let create (num: int) (size: int) =
    let mutable num = num
    let mutable count = 0
    let mutable missing = 0

    let holding = ConcurrentQueue<byte array>()

    for n in 0 .. (num - 1) do
      size
      |> Array.zeroCreate
      |> holding.Enqueue

    { new IBufferManager with
        member manager.NumBuffers
          with get () = num

        member manager.BufferSize
          with get () =  size

        member manager.InUse
          with get () = missing

        member manager.Available
          with get () = num - missing

        member manager.TakeBuffer () =
            let buffer =
              match holding.TryDequeue() with
              | true, buffer -> buffer
              | false, _ ->
                Interlocked.Increment &num |> ignore
                Array.zeroCreate size

            Interlocked.Increment &count |> ignore
            Interlocked.Increment &missing |> ignore

            { new IBuffer with
                member buf.Data
                  with get () = buffer

                member buf.Length
                  with get () = size

                member buf.Id
                  with get () = count

                member buf.Dispose () =
                  Interlocked.Decrement &missing |> ignore
                  for n in 0 .. (size - 1) do
                    buffer.[n] <- 0uy
                  holding.Enqueue buffer }
      }

// * Core module

module Core =
  let CONNECTED = [| 79uy; 75uy |] //  "OK" in utf8

  [<Literal>]
  let MAX_CONNECTIONS = 100

  [<Literal>]
  let BUFFER_SIZE = 8192

  [<Literal>]
  let ID_SIZE = 16

  [<Literal>]
  let ACCEPT_BUFFER_OFFSET = 288 // internal offset for connection info

  [<Literal>]
  let ACCEPT_BUFFER_SIZE = 304 // ACCEPT_BUFFER_OFFSET + 16 (for Client GUID)

// * ISocketMessage

type ISocketMessage =
  abstract RequestId: Guid
  abstract PeerId: Guid
  abstract Body: byte array

// * Request (Client-side)

type Request =
  { RequestId: Guid
    PeerId: Guid
    Body: byte array }

  interface ISocketMessage with
    member self.RequestId with get () = self.RequestId
    member self.PeerId with get () = self.PeerId
    member self.Body with get () = self.Body

// * Response (Client-side)

type Response =
  { RequestId: Guid
    PeerId: Guid
    Body: byte array}

  interface ISocketMessage with
    member self.RequestId with get () = self.RequestId
    member self.PeerId with get () = self.PeerId
    member self.Body with get () = self.Body

// * IncomingRequest (Server-side)

type IncomingRequest =
  { RequestId: Guid
    PeerId: Guid
    Body: byte array}

  interface ISocketMessage with
    member self.RequestId with get () = self.RequestId
    member self.PeerId with get () = self.PeerId
    member self.Body with get () = self.Body

// * OutgoingResponse (Server-side)

type OutgoingResponse =
  { RequestId: Guid
    PeerId: Guid
    Body: byte array}

  interface ISocketMessage with
    member self.RequestId with get () = self.RequestId
    member self.PeerId with get () = self.PeerId
    member self.Body with get () = self.Body

// * SocketMessageConstructor

type SocketMessageConstructor = Guid -> Guid -> byte array -> unit

// * IRequestBuilder

[<AllowNullLiteral>]
type IRequestBuilder =
  inherit IDisposable
  abstract Write: data: byte -> unit

// * IResponseBuilder

type IResponseBuilder = IRequestBuilder

// * RequestBuilder module

module RequestBuilder =

  // ** tag

  let private tag (str: string) = String.format "RequestBuilder.{0}" str

  // ** Constants

  let private Preamble = [| 73uy; 77uy; 83uy; 71uy |] // IMSG in ASCII

  let private PreambleSize = Preamble.Length // 4 bytes in ASCII

  let private MsgLengthSize = sizeof<int64> // int64 is 8 bytes long

  let private IdSize = sizeof<Guid>     // Guid has 16 bytes

  //  preamble | length of body  | length of client id | length of request id
  // ------------------------------------------------------------------------
  //     4     +        8        +        16           +      16
  // ------------------------------------------------------------------------
  let private HeaderSize = PreambleSize + MsgLengthSize + IdSize + IdSize

  let private PreambleMsgOffset = PreambleSize + MsgLengthSize

  let private PreambleMsgIdOffset = PreambleSize + MsgLengthSize + IdSize

  // ** parseLength

  let private parseLength (data: byte array) (offset: int) =
    BitConverter.ToInt64(data, offset)

  // ** serializeLength

  let private serializeLength (request: 't when 't :> ISocketMessage) =
    int64 request.Body.Length
    |> BitConverter.GetBytes

  // ** serialize

  let serialize (request: 't when 't :> ISocketMessage) : byte array =
    let totalSize = int HeaderSize + request.Body.Length
    let destination = Array.zeroCreate totalSize
    // copy the preamble
    Array.Copy(
      Preamble,
      destination,
      PreambleSize)
    // copy the encoded message length to destination
    Array.Copy(
      RequestBuilder.serializeLength request,
      0,
      destination,
      PreambleSize,
      MsgLengthSize)
    // copy the connection id (peer id) to destination
    Array.Copy(
      request.PeerId.ToByteArray(),
      0,
      destination,
      PreambleSize + MsgLengthSize,
      IdSize)
    // copy the request id to destination
    Array.Copy(
      request.RequestId.ToByteArray(),
      0,
      destination,
      PreambleSize + MsgLengthSize + IdSize,
      IdSize)
    // copy the message body to destination
    Array.Copy(
      request.Body,
      0,
      destination,
      HeaderSize,
      request.Body.Length)
    destination                         // done!

  // ** IBoundedWriter

  type private IBoundedWriter =
    inherit IDisposable
    abstract Size: int
    abstract Position: int64
    abstract IsDone: bool
    abstract Write: byte -> unit
    abstract Buffer: byte array
    abstract Reset: unit -> unit

  // ** BoundedWriter

  module private BoundedWriter =

    let create (capacity: int) =
      let buffer = Array.zeroCreate capacity
      let stream = new MemoryStream(buffer)

      { new IBoundedWriter with
          member writer.Size
            with get () = capacity

          member writer.Position
            with get () = stream.Position

          member writer.Buffer
            with get () = buffer

          member writer.IsDone
            with get () = stream.Position = int64 capacity

          member writer.Write (data: byte) =
            try
              stream.WriteByte(data)
            with
              | exn ->
                String.format "{0}" exn
                |> Logger.err (tag "BoundedWriter.Write")

          member writer.Reset() =
            for n in 0 .. (capacity - 1) do
              buffer.[n] <- 0uy
            stream.Seek(0L, SeekOrigin.Begin) |> ignore

          member writer.Dispose() =
            stream.Dispose() }

  // ** IUnboundedWriter

  type private IUnboundedWriter =
    inherit IDisposable
    inherit IBoundedWriter
    abstract TargetSize: int64 option with get, set

  // ** UnboundedWriter

  module private UnboundedWriter =

    let create () =
      let mutable targetSize: int64 option = None
      let stream = new MemoryStream()

      { new IUnboundedWriter with
          member writer.Size
            with get () = 0

          member writer.TargetSize
            with get () = targetSize
            and set size = targetSize <- size

          member writer.Position
            with get () = stream.Position

          member writer.Buffer
            with get () = stream.ToArray()

          member writer.IsDone
            with get () =
              match targetSize with
              | Some size -> size = stream.Position
              | None -> false

          member writer.Write (data: byte) =
            stream.WriteByte(data)

          member writer.Reset() =
            targetSize <- None
            stream.Seek(0L, SeekOrigin.Begin) |> ignore
            stream.SetLength(0L)

          member writer.Dispose() =
            stream.Dispose() }

  // ** IState

  type private IState =
    inherit IDisposable
    abstract Processing: bool
    abstract Length: int64 option
    abstract IsDone: bool
    abstract Write: byte -> unit

  // ** State

  module private State =

    let create (onComplete: SocketMessageConstructor) =
      let mutable previousMatch = None
      let mutable processing = false

      let preamble = BoundedWriter.create PreambleSize
      let length = BoundedWriter.create MsgLengthSize
      let client = BoundedWriter.create IdSize
      let request = BoundedWriter.create IdSize
      let body = UnboundedWriter.create ()

      { new IState with
          member state.Length
            with get () = body.TargetSize

          member state.Processing
            with get () = processing

          member state.IsDone
            with get () =
              processing
              && length.IsDone
              && client.IsDone
              && request.IsDone
              && body.IsDone

          member state.Write (data: byte) =
            if not processing then
              match previousMatch, preamble.Position with
              // the base case is we have not had any matching data before, so we start tracking
              | None, 0L when data = Preamble.[0] ->
                previousMatch <- Some data
                preamble.Write(data)
              | Some previous, 1L when previous = Preamble.[0] && data = Preamble.[1] ->
                previousMatch <- Some data
                preamble.Write(data)
              | Some previous, 2L when previous = Preamble.[1] && data = Preamble.[2] ->
                previousMatch <- Some data
                preamble.Write(data)
              | Some previous, 3L when previous = Preamble.[2] && data = Preamble.[3] ->
                previousMatch <- None
                preamble.Write(data)
              // it can happen that the start value of the preamble is passed twice in a row
              // so, we simply restart the process in that case
              | _, _ when data = Preamble.[0] ->
                preamble.Reset()
                preamble.Write(data)
                previousMatch <- Some data
              // reset the stream parser, because its in a weird state
              | _, position when position > 0L ->
                previousMatch <- None
                preamble.Reset()
              // no match, keep on truckin'
              | _, _ -> previousMatch <- None

              if preamble.IsDone then
                processing <- true
            else
              if not length.IsDone then
                length.Write data
                if length.IsDone && Option.isNone body.TargetSize then
                  let size = parseLength length.Buffer 0
                  body.TargetSize <- Some size
              elif not client.IsDone then
                client.Write data
              elif not request.IsDone then
                request.Write data
              elif not body.IsDone then
                body.Write data

              if state.IsDone then
                onComplete (Guid request.Buffer) (Guid client.Buffer) body.Buffer
                preamble.Reset()
                length.Reset()
                request.Reset()
                client.Reset()
                body.Reset()
                processing <- false

          member state.Dispose() =
            dispose preamble
            dispose length
            dispose client
            dispose request
            dispose body }

  // ** create

  let create onComplete =
    let mutable disposed = false
    let cts = new CancellationTokenSource()
    let state = State.create onComplete

    { new IRequestBuilder with
        member parser.Write (data: byte) =
          state.Write data

        member parser.Dispose() =
          if not disposed then
            dispose state
            cts.Cancel()
            disposed <- true }

// * Request module

module Request =

  // ** make

  let make (requestId: Guid) (peerId: Guid) (body: byte array) : Request =
    { RequestId = requestId
      PeerId = peerId
      Body = body }

  // ** create

  let create (peerId: Guid) (body: byte array) : Request =
    make (Guid.NewGuid()) peerId body

  // ** serialize

  let serialize = RequestBuilder.serialize

// * ResponseBuilder module

module ResponseBuilder =

  let create callback : IResponseBuilder =
    RequestBuilder.create callback

// * Response module

module Response =

  let create (requestId: Guid) (peerId: Guid) (body: byte array) : Response =
    { RequestId = requestId
      PeerId = peerId
      Body = body }

  let fromRequest (request: Request) (body: byte array) : Response =
    create request.RequestId request.PeerId body

  let serialize (response: Response) = RequestBuilder.serialize response

// * Client

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

// ** TcpClientSettings

type TcpClientSettings =
  { ClientId: ClientId
    PeerAddress: IpAddress
    PeerPort: Port
    Timeout: Timeout }

// ** TcpClientEvent

type TcpClientEvent =
  | Connected    of peerid:PeerId
  | Disconnected of peerid:PeerId * DiscoError
  | Request      of Request
  | Response     of Response

// ** ITcpClient

type ITcpClient =
  inherit IDisposable
  abstract Connect: unit -> unit
  abstract Disconnect: unit -> unit
  abstract ClientId: ClientId
  abstract Status: ServiceStatus
  abstract Request: Request -> unit
  abstract Respond: Response -> unit
  abstract LocalEndPoint: IPEndPoint
  abstract RemoteEndPoint: IPEndPoint
  abstract Subscribe: (TcpClientEvent -> unit) -> IDisposable

// * Server

//  ____
// / ___|  ___ _ ____   _____ _ __
// \___ \ / _ \ '__\ \ / / _ \ '__|
//  ___) |  __/ |   \ V /  __/ |
// |____/ \___|_|    \_/ \___|_|

// ** TcpServerSettings

type TcpServerSettings =
  { ServerId: PeerId
    Listen: IpAddress
    Port: Port }

// ** TcpServerEvent

[<NoComparison;NoEquality>]
type TcpServerEvent =
  | Connect    of peerId:Guid * ip:IPAddress * port:int
  | Disconnect of peerId:Guid
  | Request    of Request
  | Response   of Response

// ** ITcpServer

type ITcpServer =
  inherit IDisposable
  abstract Id: PeerId
  abstract Start: unit -> Either<DiscoError,unit>
  abstract Subscribe: (TcpServerEvent -> unit) -> IDisposable
  abstract Request: client:Guid -> Request -> unit
  abstract Respond: Response -> unit

// * PubSub

type PubSubEvent =
  | Request of peer:PeerId * data:byte array

type IPubSub =
  inherit IDisposable
  abstract Start: unit -> Either<DiscoError,unit>
  abstract Send: byte array -> unit
  abstract Subscribe: (PubSubEvent -> unit) -> IDisposable

// * Socket module

module Socket =

  // ** tag

  let private tag (str: string) = String.format "Socket.{0}" str

  // ** setSocketOption

  let setSocketOption (option: SocketOptionName) (value: bool) (socket: Socket) =
    socket.SetSocketOption(SocketOptionLevel.Socket, option, value)
    socket

  // ** setNoDelay

  let setNoDelay value (socket: Socket) =
    socket.NoDelay <- value              // disable Nagle's algorithm (small packets)
    socket

  // ** createTcp

  let createTcp () =
    new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    |> setSocketOption SocketOptionName.ReuseAddress true
    |> setSocketOption SocketOptionName.DontLinger   true
    |> setNoDelay true

  // ** isAlive

  let isAlive (socket:Socket) =
    try not (socket.Poll(1, SelectMode.SelectRead) && socket.Available = 0)
    with | _ -> false

  // ** dispose

  let dispose (socket:Socket) =
    try
      socket.LingerState <- LingerOption(true,0)
      socket.Shutdown(SocketShutdown.Both)
      socket.Close()
    with
      | _ -> socket.Dispose()

  // ** disconnect

  let disconnect (socket: Socket) =
    try
      socket.Disconnect(false)
    with
      | :? ObjectDisposedException ->
        Logger.err (tag "disconnect") "socket already disposed"
      | exn ->
        Logger.err (tag "disconnect") exn.Message

  // ** checkState

  (*
  let checkState<'t> (socket: Socket)
                     (subscriptions: ConcurrentDictionary<Guid,IObserver<'t>>)
                     (good: 't option)
                     (bad: 't option) =
    let rec impl() : Async<unit> =
      async {
        do! Async.Sleep(500)            // check socket liveness every 0.5 seconds
        try
          if isAlive socket then
            Option.iter (Observable.onNext subscriptions) good
          else
            Option.iter (Observable.onNext subscriptions) bad
          return! impl()
        with
          | _ ->
            Option.iter (Observable.onNext subscriptions) bad
            return! impl()
      }
    impl()
   *)

// * Playground

#if INTERACTIVE

module Playground =

  let f () =
    let listener = Socket.createTcp()
    let endpoint = IPEndPoint(IPAddress.Loopback, 9999)

    listener.Connect(endpoint)
    listener.Send(Text.Encoding.UTF8.GetBytes("hello"))
    listener.Disconnect(true)

    listener.Connected


#endif
