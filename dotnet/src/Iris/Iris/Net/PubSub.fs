namespace Iris.Net

// * Imports

open System
open System.Net
open System.Net.Sockets
open System.Collections.Concurrent

open Iris.Core

// * PubSub

module rec PubSub =

  // ** tag

  let private tag (str: string) = String.format "PubSub.{0}" str

  // ** defaultAddress

  let defaultAddress =
    IPAddress.Parse Constants.MCAST_ADDRESS

  // ** IState

  type private IState =
    abstract ConnectionId: Guid
    abstract LocalEndPoint: IPEndPoint
    abstract RemoteEndPoint: IPEndPoint
    abstract Client: UdpClient
    abstract Subscriptions: ConcurrentDictionary<Guid,IObserver<PubSubEvent>>

  // ** receiveCallback

  let private receiveCallback () =
    AsyncCallback(fun (ar: IAsyncResult) ->
      let state = ar.AsyncState :?> IState
      let raw = state.Client.EndReceive(ar, &state.LocalEndPoint)

      let guid =
        let intermediate = Array.zeroCreate 16
        Array.blit raw 0 intermediate 0 16
        Guid intermediate

      if guid <> state.ConnectionId then
        let payload =
          let intermedate = raw.Length - 16 |> Array.zeroCreate
          Array.blit raw 16 intermedate 0 (raw.Length - 16)
          intermedate

        (guid, payload)
        |> PubSubEvent.Request
        |> Observable.onNext state.Subscriptions
      beginReceive state)

  // ** beginReceive

  let private beginReceive (state: IState) =
    state.Client.BeginReceive(receiveCallback(), state)
    |> ignore

  let private sendCallback (ar: IAsyncResult) =
    try
      let state = ar.AsyncState :?> IState
      state.Client.EndSend(ar)
      |> String.format "%d bytes sent"
      |> Logger.debug (tag "sendCallback")
    with
      | exn ->
        exn.Message
        |> printfn "exn: %s"

  let private beginSend (state: IState) (data: byte array) =
    let id = state.ConnectionId
    let payload = Array.append (id.ToByteArray()) data
    state.Client.BeginSend(
      payload,
      payload.Length,
      state.RemoteEndPoint,
      AsyncCallback(sendCallback),
      state)
    |> ignore

  let create (multicastAddress: IPAddress) (port: int) =
    let id = Guid.NewGuid()

    let subscriptions = ConcurrentDictionary<Guid,IObserver<PubSubEvent>>()

    let client = new UdpClient()
    client.ExclusiveAddressUse <- false

    let remoteEp = IPEndPoint(multicastAddress, port)
    let localEp = IPEndPoint(IPAddress.Any, port)

    let state =
      { new IState with
          member state.ConnectionId
            with get () = id

          member state.LocalEndPoint
            with get () = localEp

          member state.RemoteEndPoint
            with get () = remoteEp

          member state.Client
            with get () = client

          member state.Subscriptions
            with get () = subscriptions }

    { new IPubSub with
        member pubsub.Start() =
          try
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
            client.Client.Bind(localEp)
            client.JoinMulticastGroup(multicastAddress)
            beginReceive state
            Either.nothing
          with
            | exn ->
              exn.Message
              |> Error.asSocketError (tag "Start")
              |> Either.fail

        member pubsub.Send(bytes: byte array) =
          beginSend state bytes

        member pubsub.Subscribe (callback: PubSubEvent -> unit) =
          Observable.subscribe callback subscriptions

        member pubsub.Dispose () =
          client.Dispose() }
