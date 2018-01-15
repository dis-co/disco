(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Net

// * Imports

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open Disco.Core

// * TcpClient

module rec TcpClient =

  // ** tag

  let private tag (str: string) = String.format "TcpClient.{0}" str

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<Guid,IObserver<TcpClientEvent>>

  // ** PendingRequests

  type private RequestId = Guid

  type private PendingRequests = ConcurrentDictionary<RequestId,Request>

  // ** IState

  type private IState =
    inherit IDisposable
    abstract Disposed: bool
    abstract Status: ServiceStatus with get, set
    abstract ClientId: ClientId
    abstract ConnectionId: byte array
    abstract Socket: Socket
    abstract Stream: NetworkStream
    abstract EndPoint: IPEndPoint
    abstract PendingRequests: PendingRequests
    abstract ResponseBuilder: IResponseBuilder
    abstract Subscriptions: Subscriptions
    abstract Request: Request -> unit
    abstract Respond: Response -> unit
    abstract StartReceiving: unit -> unit

  // ** handleError

  let private handleError (state:IState) (error: DiscoError) =
    do Logger.debug error.Location error.Message
    state.Status <- ServiceStatus.Failed error
    (state.ClientId, error)
    |> TcpClientEvent.Disconnected
    |> Observable.onNext state.Subscriptions

  //                     _ _
  //  ___  ___ _ __   __| (_)_ __   __ _
  // / __|/ _ \ '_ \ / _` | | '_ \ / _` |
  // \__ \  __/ | | | (_| | | | | | (_| |
  // |___/\___|_| |_|\__,_|_|_| |_|\__, |
  //                               |___/
  let private sendLoop (state: IState) inbox msg = async {
      try
        do state.Stream.Write(msg, 0, msg.Length)
      with exn ->
        exn.Message
        |> Error.asSocketError (tag "sendLoop")
        |> handleError state
    }

  //                    _       _
  //  _ __ ___  ___ ___(_)_   _(_)_ __   __ _
  // | '__/ _ \/ __/ _ \ \ \ / / | '_ \ / _` |
  // | | |  __/ (_|  __/ |\ V /| | | | | (_| |
  // |_|  \___|\___\___|_| \_/ |_|_| |_|\__, |
  //                                    |___/
  let private receiveLoop (state: IState) =
    async {
      let mutable run = true
      while run do
        try
          let result = state.Stream.ReadByte()
          if result <> -1
          then result |> byte |> state.ResponseBuilder.Write
          else
            "Reached end of Stream. Disconnected"
            |> Error.asSocketError (tag "receiveLoop")
            |> handleError state
            run <- false
        with
          | :? IOException -> run <- false
          | exn ->
            exn.Message
            |> Error.asSocketError (tag "receiveLoop")
            |> handleError state
            run <- false
    }

  // ** makeState

  let private makeState (options: TcpClientSettings) (subscriptions: Subscriptions) =
    let guid =
      options.ClientId
      |> Guid.ofId
      |> fun guid -> guid.ToByteArray()

    let pending = PendingRequests()
    let cts = new CancellationTokenSource()
    let endpoint = IPEndPoint(options.PeerAddress.toIPAddress(), int options.PeerPort)
    let client = Socket.createTcp()
    let mutable stream = Unchecked.defaultof<NetworkStream>
    let mutable status = ServiceStatus.Stopped

    let builder = ResponseBuilder.create <| fun request client body  ->
      if pending.ContainsKey request then
        pending.TryRemove(request) |> ignore
        body
        |> Response.create request client
        |> TcpClientEvent.Response
        |> Observable.onNext subscriptions
      else
        body
        |> Request.make request client
        |> TcpClientEvent.Request
        |> Observable.onNext subscriptions

    let mutable sender = Unchecked.defaultof<IActor<byte[]>>

    { new IState with
      member state.Status
        with get () = status
        and set st = status <- st
      member state.ClientId = options.ClientId
      member state.ConnectionId = guid
      member state.Socket = client
      member state.Stream = stream
      member state.EndPoint = endpoint
      member state.PendingRequests = pending
      member state.Disposed = cts.IsCancellationRequested
      member state.ResponseBuilder = builder
      member state.Subscriptions = subscriptions

      member state.Request (request: Request) =
        // this socket is asking something, so we need to track this in pending requests
        do request.RequestId
            |> sprintf "sending to %A (id: %A)" request.PeerId
            |> Logger.debug (tag "Request")
        do pending.TryAdd(request.RequestId, request) |> ignore
        do request |> RequestBuilder.serialize |> sender.Post

      member state.Respond (response: Response) =
        do response.RequestId
            |> sprintf "sending to %A (id: %A)" response.PeerId
            |> Logger.debug (tag "Respond")
        do response |> RequestBuilder.serialize |> sender.Post

      member state.StartReceiving() =
        stream <- new NetworkStream(client)
        sender <- Actor.create (sendLoop state)
        sender.Start()
        Async.Start(receiveLoop state, cts.Token)

      member state.Dispose() =
        if not cts.IsCancellationRequested then
          try cts.Cancel() with | _ -> ()
          for KeyValue(id,_) in pending.ToArray() do
            pending.TryRemove(id) |> ignore
          dispose builder
          tryDispose sender ignore
          tryDispose stream ignore
          dispose client
          status <- ServiceStatus.Disposed
      }

  // ** onError

  let private onError location (args: SocketAsyncEventArgs) =
    let listener, state = args.UserToken :?> IDisposable * IState
    do dispose listener
    args.SocketError
    |> String.format "{0} in socket operation"
    |> Error.asSocketError (tag location)
    |> handleError state

  // ** onSend

  let private onSend (args: SocketAsyncEventArgs) =
    let listener, state = args.UserToken :?> IDisposable * IState
    do dispose listener
    if args.SocketError <> SocketError.Success then
      args.SocketError
      |> String.format "{0} in socket operation"
      |> Error.asSocketError (tag "onSend")
      |> handleError state
    else
      args.BytesTransferred
      |> String.format "sent {0} bytes"
      |> Logger.debug (tag "onSend")

  // ** sendAsync

  let private sendAsync (state: IState) (bytes: byte array) =
    let args = new SocketAsyncEventArgs()
    do args.SetBuffer(bytes, 0, bytes.Length)
    let listener = args.Completed.Subscribe onSend
    args.RemoteEndPoint <- state.EndPoint
    args.UserToken <- listener, state
    try
      match state.Socket.SendAsync(args) with
      | true -> ()
      | false -> onSend args
    with
      | :? ObjectDisposedException -> ()

  // ** onInitialize

  let private onInitialize (args: SocketAsyncEventArgs) =
    if args.SocketError = SocketError.Success then
      let listener, state = args.UserToken :?> IDisposable * IState
      do dispose listener
      if args.Buffer = Core.CONNECTED then
        state.Status <- ServiceStatus.Running
        state.StartReceiving()
        do state.ClientId
          |> TcpClientEvent.Connected
          |> Observable.onNext state.Subscriptions
      else
        "Incorrect response from server, disconnected"
        |> Error.asSocketError (tag "onInitialize")
        |> handleError state
    else onError "onInitialize" args

  // ** initializeAsync

  let private initializeAsync (state: IState) =
    do sendAsync state state.ConnectionId
    let args = new SocketAsyncEventArgs()
    let buffer = Array.zeroCreate Core.CONNECTED.Length
    let listener = args.Completed.Subscribe onInitialize
    args.UserToken <- listener, state
    args.SetBuffer(buffer, 0, buffer.Length)
    if not state.Disposed then
      match state.Socket.ReceiveAsync(args) with
      | true -> ()
      | false -> onInitialize args

  // ** onConnected

  let private onConnected (args: SocketAsyncEventArgs) =
    if args.SocketError = SocketError.Success then
      let listener, state = args.UserToken :?> IDisposable * IState
      do initializeAsync state
      do dispose listener
      do dispose args
    else onError "onConnected" args

  // ** connectAsync

  let private connectAsync (state: IState) =
    state.Status <- ServiceStatus.Starting
    let args = new SocketAsyncEventArgs()
    let listener = args.Completed.Subscribe onConnected
    args.RemoteEndPoint <- state.EndPoint
    args.UserToken <- listener, state
    match state.Socket.ConnectAsync args with
    | true  -> ()
    | false -> onConnected args

  // ** create

  let create (options: TcpClientSettings) =
    let subscriptions = Subscriptions()
    let mutable state = makeState options subscriptions

    let listener =
      flip Observable.subscribe subscriptions <| function
        | TcpClientEvent.Disconnected _ ->
          dispose state
          Async.Start(async {
            do! Async.Sleep(500);
            string options.PeerAddress + string options.PeerPort
            |> String.format "Reconnecting to {0}"
            |> Logger.info (tag "listener")
            state <- makeState options subscriptions
            connectAsync state
          })
        | _ -> ()

    { new ITcpClient with
        member socket.Status
          with get () = state.Status

        member socket.Connect() =
          connectAsync state

        member socket.Request(request: Request) =
          if Service.isRunning state.Status then
            do state.Request request
          else
            string state.Status
            |> String.format "not sending, wrong state {0}"
            |> Logger.err (tag "Request")

        member socket.Respond(response: Response) =
          if Service.isRunning state.Status then
            do state.Respond response
          else
            string state.Status
            |> String.format "not sending, wrong state {0}"
            |> Logger.err (tag "Respond")

        member socket.Disconnect() =
          do Socket.disconnect state.Socket

        member socket.ClientId
          with get () = options.ClientId

        member socket.Subscribe (callback: TcpClientEvent -> unit) =
          Observable.subscribe callback subscriptions

        member socket.LocalEndPoint = state.Socket.LocalEndPoint :?> IPEndPoint
        member socket.RemoteEndPoint = state.Socket.RemoteEndPoint :?> IPEndPoint

        member socket.Dispose () =
          listener.Dispose()
          state.Dispose() }
