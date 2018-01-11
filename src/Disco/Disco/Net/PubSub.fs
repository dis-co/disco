(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Net

// * Imports

open System
open System.Net
open System.Net.Sockets
open System.Collections.Concurrent

open Disco.Core
open Disco.Raft

// * PubSub

module rec PubSub =

  // ** tag

  let private tag (str: string) = String.format "PubSub.{0}" str

  // ** IState

  type private IState =
    abstract Id: PeerId
    abstract LocalEndPoint: IPEndPoint
    abstract RemoteEndPoint: IPEndPoint
    abstract Client: UdpClient
    abstract Subscriptions: ConcurrentDictionary<Guid,IObserver<PubSubEvent>>

  // ** receiveCallback

  let private receiveCallback () =
    AsyncCallback(fun (ar: IAsyncResult) ->
      try
        let state = ar.AsyncState :?> IState
        let raw = state.Client.EndReceive(ar, &state.LocalEndPoint)

        let guid =
          let intermediate = Array.zeroCreate 16
          Array.blit raw 0 intermediate 0 16
          DiscoId.FromByteArray intermediate

        if guid <> state.Id then
          let payload =
            let intermedate = raw.Length - 16 |> Array.zeroCreate
            Array.blit raw 16 intermedate 0 (raw.Length - 16)
            intermedate

          (guid, payload)
          |> PubSubEvent.Request
          |> Observable.onNext state.Subscriptions
        beginReceive state
      with
        | :? ObjectDisposedException -> ()
        | exn ->
          exn.Message
          |> Logger.err (tag "receiveCallback"))

  // ** beginReceive

  let private beginReceive (state: IState) =
    try
      state.Client.BeginReceive(receiveCallback(), state)
      |> ignore
    with
      | :? ObjectDisposedException -> ()
      | exn ->
        exn.Message
        |> Logger.err (tag "beginReceive")

  // ** sendCallback

  let private sendCallback (ar: IAsyncResult) =
    try
      let state = ar.AsyncState :?> IState
      state.Client.EndSend(ar) |> ignore
    with
      | :? ObjectDisposedException -> ()
      | exn ->
        exn.Message
        |> Logger.err (tag "sendCallback")

  // ** beginSend

  let private beginSend (state: IState) (data: byte array) =
    let id = Guid.ofId state.Id
    let payload = Array.append (id.ToByteArray()) data
    state.Client.BeginSend(
      payload,
      payload.Length,
      state.RemoteEndPoint,
      AsyncCallback(sendCallback),
      state)
    |> ignore

  // ** create

  let create mem =
    let subscriptions = ConcurrentDictionary<Guid,IObserver<PubSubEvent>>()

    let client = new UdpClient()
    client.ExclusiveAddressUse <- false

    let externalAddress =
      mem
      |> Member.ipAddress
      |> string
      |> IPAddress.Parse

    let remoteAddress =
      mem
      |> Member.multicastAddress
      |> string
      |> IPAddress.Parse

    let remotePort =
      mem
      |> Member.multicastPort
      |> int

    let remoteEp = IPEndPoint(remoteAddress, remotePort)
    let localEp = IPEndPoint(IPAddress.Any, remotePort)

    let state =
      { new IState with
          member state.Id
            with get () = Member.id mem

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
            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1)
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)
            do client.Client.Bind(localEp)
            do client.JoinMulticastGroup(remoteAddress, externalAddress)
            do beginReceive state
            Either.nothing
          with
            | exn ->
              exn.Message
              |> Error.asSocketError (tag "Start")
              |> Either.fail

        member pubsub.Send(bytes: byte array) =
          try
            beginSend state bytes
          with exn ->
            Logger.err (tag "Send") exn.Message

        member pubsub.Subscribe (callback: PubSubEvent -> unit) =
          Observable.subscribe callback subscriptions

        member pubsub.Dispose () =
          try client.Dispose() with _ -> () }
