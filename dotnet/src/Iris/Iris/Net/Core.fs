namespace rec Iris.Net

#nowarn "52" // "this value has been copied"

// * Imports

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open Iris.Core
open System.Collections.Concurrent

// * Core module

module Core =

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
  abstract Process: buffer: byte array -> read:int -> unit

// * IResponseBuilder

type IResponseBuilder = IRequestBuilder

// * RequestBuilder module

module RequestBuilder =

  // ** Constants

  let Preamble = [| 73uy; 77uy; 83uy; 71uy |] // IMSG in ASCII

  [<Literal>]
  let PreambleSize = 4                  // 4 bytes in ASCII

  [<Literal>]
  let MsgLengthSize = 8                 // int64 is 8 bytes long

  [<Literal>]
  let IdSize = 16                       // Guid has 16 bytes

  //  preamble | length of body  | length of client id | length of request id
  // ------------------------------------------------------------------------
  //     4     +        8        +        16           +      16
  // ------------------------------------------------------------------------
  let HeaderSize = PreambleSize + MsgLengthSize + IdSize + IdSize

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

  // ** findPreamble

  let private findPreamble (data: byte array) =
    Array.tryFindIndex ((=) Preamble.[0]) data

  // ** PreambleMsgOffset

  let private PreambleMsgOffset = PreambleSize + MsgLengthSize

  // ** PreambleMsgIdOffset

  let private PreambleMsgIdOffset = PreambleSize + MsgLengthSize + IdSize

  // ** create

  let create (onComplete: SocketMessageConstructor) =
    let preamble = ResizeArray()     // TODO: eventually, these should probably become BinaryWriters
    let length = ResizeArray()
    let client = ResizeArray()
    let request = ResizeArray()
    let body = ResizeArray()

    let mutable bodyLength = 0L

    let finalize () =
      onComplete
        (Guid (request.ToArray()))
        (Guid (client.ToArray()))
        (body.ToArray())
      preamble.Clear()
      length.Clear()
      client.Clear()
      request.Clear()
      body.Clear()
      bodyLength <- 0L

    let rec build (data: byte array) (read: int) =
      let rest = ResizeArray()
      let mutable addToRest = false
      // this is a fresh response, so we start off nice and neat
      if preamble.Count = 0 && length.Count = 0 && client.Count = 0 && request.Count = 0 then
        match findPreamble data with
        | Some offset when offset <= read -> // we found the possible start of a preamble
          let remaining = read - offset
          for i in 0 .. remaining - 1 do
            match i with
            | _ when i < PreambleSize                                  && not addToRest ->
              // add data to preamble
              preamble.Add data.[i + offset]

              // if the preamble is now complete, check if its valid
              if preamble.Count = PreambleSize then
                // if the preamble is not valid, the rest of the data is just
                // going to get appended to the rest array, and processed again
                addToRest <- (preamble.ToArray()) <> Preamble

            | _ when i >= PreambleSize        && i < PreambleMsgOffset   && not addToRest ->
              length.Add data.[i + offset]
              if length.Count = int MsgLengthSize then
                bodyLength <- parseLength (length.ToArray()) 0

            | _ when i >= PreambleMsgOffset   && i < PreambleMsgIdOffset && not addToRest ->
              client.Add data.[i + offset]

            | _ when i >= PreambleMsgIdOffset && i < HeaderSize          && not addToRest ->
              request.Add data.[i + offset]

            | _ when int64 body.Count < bodyLength                      && not addToRest ->
              body.Add data.[i + offset]

            | _ when int64 body.Count = bodyLength                      && not addToRest ->
              finalize()
              addToRest <- true
              rest.Add data.[i + offset]

            | _ -> rest.Add data.[i + offset]
        | _ -> () // ignore data when no preamble was found and this is the first call
      else
        // we're already working on something here, so keep at it
        for i in 0 .. read - 1 do
          match i with
          | _ when preamble.Count < PreambleSize  && not addToRest ->
            // add data to preamble
            preamble.Add data.[i]

            // if the preamble is now complete, check if its valid
            if preamble.Count = PreambleSize then
              // if the preamble is not valid, the rest of the data is just
              // going to get appended to the rest array, and processed again
              addToRest <- (preamble.ToArray()) <> Preamble

          | _ when length.Count   < MsgLengthSize && not addToRest ->
            length.Add data.[i]

            // if enough bytes have been read, parse the length
            if length.Count = MsgLengthSize then
              bodyLength <- parseLength (length.ToArray()) 0

          | _ when client.Count   < IdSize        && not addToRest ->
            client.Add data.[i]

          | _ when request.Count  < IdSize        && not addToRest ->
            request.Add data.[i]

          | _ when int64 body.Count < bodyLength  && not addToRest ->
            body.Add data.[i]

          | _ when int64 body.Count = bodyLength  && not addToRest ->
            finalize()
            addToRest <- true
            rest.Add data.[i]

          | i -> rest.Add data.[i]

      if preamble.Count       = PreambleSize
        && preamble.ToArray() = Preamble
        && length.Count       = MsgLengthSize
        && client.Count       = IdSize
        && request.Count      = IdSize
        && int64 body.Count   = bodyLength
      then finalize()

      // ever more to do, the recurse
      if rest.Count > 0 then
        build (rest.ToArray()) rest.Count

    { new IRequestBuilder with
        member state.Process (buffer: byte array) (read: int) =
          if read > 0 then
            build buffer read

        member state.Dispose() =
          length.Clear()
          client.Clear()
          request.Clear()
          body.Clear() }

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

// ** ClientConfig

type ClientConfig =
  { ClientId: Id
    PeerAddress: IpAddress
    PeerPort: Port
    Timeout: Timeout }

// ** TcpClientEvent

type TcpClientEvent =
  | Connected    of peerid:Id
  | Disconnected of peerid:Id * IrisError
  | Request      of Request
  | Response     of Response

// ** IClient

type IClient =
  inherit IDisposable
  abstract Connect: unit -> unit
  abstract Disconnect: unit -> unit
  abstract ClientId: Id
  abstract Status: ServiceStatus
  abstract Request: Request -> unit
  abstract Respond: Response -> unit
  abstract Subscribe: (TcpClientEvent -> unit) -> IDisposable

// * Server

//  ____
// / ___|  ___ _ ____   _____ _ __
// \___ \ / _ \ '__\ \ / / _ \ '__|
//  ___) |  __/ |   \ V /  __/ |
// |____/ \___|_|    \_/ \___|_|

// ** ServerConfig

type ServerConfig =
  { ServerId: Id
    Listen: IpAddress
    Port: Port }

// ** TcpServerEvent

[<NoComparison;NoEquality>]
type TcpServerEvent =
  | Connect    of peerId:Guid * ip:IPAddress * port:int
  | Disconnect of peerId:Guid
  | Request    of Request
  | Response   of Response

// ** IServer

type IServer =
  inherit IDisposable
  abstract Id: Id
  abstract Start: unit -> Either<IrisError,unit>
  abstract Subscribe: (TcpServerEvent -> unit) -> IDisposable
  abstract Request: client:Guid -> Request -> unit
  abstract Respond: Response -> unit

// * PubSub

type PubSubEvent =
  | Request of peer:Id * data:byte array

type IPubSub =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
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
      | exn ->
        Logger.err (tag "disconnect") exn.Message

  // ** checkState

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
