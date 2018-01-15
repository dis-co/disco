(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Client

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Disco.Core
open Disco.Client
open Disco.Net
open Disco.Serialization

// * ApiClient module

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

[<AutoOpen>]
module ApiClient =

  // ** tag

  let private tag (str: string) = String.Format("ApiClient.{0}",str)

  // ** TIMEOUT

  let private TIMEOUT = 2000<ms>

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<ClientEvent>

  // ** ClientState

  [<NoComparison;NoEquality>]
  type private ClientState =
    { Client: DiscoClient
      Peer: DiscoServer
      Socket: ITcpClient
      Store:  Store
      Subscriptions: Subscriptions
      SocketSubscription: IDisposable
      Stopper: AutoResetEvent }

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        dispose self.SocketSubscription
        try dispose self.Socket  with | _ -> ()
        try dispose self.Stopper with | _ -> ()

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start
    | Restart     of server:DiscoServer
    | Stop
    | Dispose
    | Notify      of ClientEvent
    | SetState    of state:State
    | SetStatus   of status:ServiceStatus
    | Update      of sm:StateMachine
    | SocketEvent of ev:TcpClientEvent

  // ** ApiAgent

  type private ApiAgent = IActor<Msg>

  // ** handleNotify

  let private handleNotify (state: ClientState) (ev: ClientEvent) =
    Observable.onNext state.Subscriptions ev
    state

  // ** requestRegister

  let private requestRegister (state: ClientState) =
    let updated =
      { state with
          Client =
            { state.Client with
                IpAddress = IpAddress.ofIPAddress state.Socket.LocalEndPoint.Address
                Port = port (uint16 state.Socket.LocalEndPoint.Port) } }

    updated.Peer.Port
    |> sprintf "registering with %O:%O" updated.Peer.IpAddress
    |> Logger.info (tag "requestRegister")

    updated.Client
    |> ApiRequest.Register
    |> Binary.encode
    |> Request.create (Guid.ofId state.Client.Id)
    |> updated.Socket.Request

    updated

  // ** requestUnRegister

  let private requestUnRegister (state: ClientState) =
    state.Peer.Port
    |> sprintf "unregistering from %O:%O" state.Peer.IpAddress
    |> Logger.info (tag "requestUnRegister")

    state.Client
    |> ApiRequest.UnRegister
    |> Binary.encode
    |> Request.create (Guid.ofId state.Client.Id)
    |> state.Socket.Request

  // ** handleStart

  let private handleStart (state: ClientState) (_: ApiAgent) =
    requestRegister state

  // ** handleSetStatus

  let private handleSetStatus (state: ClientState) (status: ServiceStatus) (agent: ApiAgent) =
    if state.Client.Status <> status then
      match status with
      | ServiceStatus.Running -> agent.Post Msg.Start
      | _ -> ()

      status
      |> ClientEvent.Status
      |> Msg.Notify
      |> agent.Post

      { state with Client = { state.Client with Status = status } }
    else
      state

  // ** handleSetState

  let private handleSetState (state: ClientState) (newstate: State) (agent: ApiAgent) =
    ClientEvent.Snapshot |> Msg.Notify |> agent.Post
    { state with Store = new Store(newstate) }

  // ** handleUpdate

  let private handleUpdate (state: ClientState) (sm: StateMachine) (agent: ApiAgent) =
    state.Store.Dispatch sm
    sm
    |> ClientEvent.Update
    |> Msg.Notify
    |> agent.Post
    state

  // ** performRequest

  let private performRequest (state: ClientState) (sm: StateMachine) =
    if Service.isRunning state.Socket.Status then
      sm
      |> ApiRequest.Update
      |> Binary.encode
      |> Request.create (Guid.ofId state.Socket.ClientId)
      |> state.Socket.Request
    else
      sm
      |> sprintf "cannot perform request: %A  - %A " state.Socket.Status
      |> Logger.err (tag "performRequest")

  // ** maybeDispatch

  let private maybeDispatch (data: ClientState) (sm: StateMachine) =
    match sm with
    | UpdateSlices _ -> data.Store.Dispatch sm
    | _ -> ()

  // ** handleRequest

  let private handleRequest (store: IAgentStore<ClientState>) (sm: StateMachine) =
    let state = store.State
    maybeDispatch state sm
    performRequest state sm

  // ** handleServerRequest

  let private handleServerRequest (state: ClientState) (req: Request) (agent: ApiAgent) =
      match req.Body |> Binary.decode with
      | Right (ApiRequest.Snapshot snapshot) ->
        state.Socket.Status
        |> String.format "received snapshot (status: {0})"
        |> Logger.info (tag "handleServerResponse")
        snapshot
        |> Msg.SetState
        |> agent.Post

      | Right (ApiRequest.Update sm) ->
        sm
        |> Msg.Update
        |> agent.Post

      | Right other ->
        string other
        |> ApiError.UnknownCommand
        |> ApiResponse.NOK
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Socket.Respond

      | Left error ->
        error
        |> string
        |> ApiError.MalformedRequest
        |> ApiResponse.NOK
        |> Binary.encode
        |> Response.fromRequest req
        |> state.Socket.Respond
      state

  // ** handleClientResponse

  let private handleClientResponse (state: ClientState) (req: Response) (agent: ApiAgent) =
    match Binary.decode req.Body with
    //  ____            _     _                    _
    // |  _ \ ___  __ _(_)___| |_ ___ _ __ ___  __| |
    // | |_) / _ \/ _` | / __| __/ _ \ '__/ _ \/ _` |
    // |  _ <  __/ (_| | \__ \ ||  __/ | |  __/ (_| |
    // |_| \_\___|\__, |_|___/\__\___|_|  \___|\__,_|
    //            |___/
    | Right ApiResponse.Registered ->
      Logger.info (tag "handleClientResponse") "registration successful"
      ClientEvent.Registered |> Msg.Notify |> agent.Post

    //  _   _       ____            _     _                    _
    // | | | |_ __ |  _ \ ___  __ _(_)___| |_ ___ _ __ ___  __| |
    // | | | | '_ \| |_) / _ \/ _` | / __| __/ _ \ '__/ _ \/ _` |
    // | |_| | | | |  _ <  __/ (_| | \__ \ ||  __/ | |  __/ (_| |
    //  \___/|_| |_|_| \_\___|\__, |_|___/\__\___|_|  \___|\__,_|
    //                        |___/
    | Right ApiResponse.Unregistered ->
      Logger.info (tag "handleClientResponse") "un-registration successful"
      ClientEvent.UnRegistered |> Msg.Notify |> agent.Post
      agent.Post Msg.Dispose

    //  _   _  ___  _  __
    // | \ | |/ _ \| |/ /
    // |  \| | | | | ' /
    // | |\  | |_| | . \
    // |_| \_|\___/|_|\_\
    | Right (ApiResponse.NOK error) -> error |> string |> Logger.err (tag "handleClientResponse")

    //  ____                     _        _____
    // |  _ \  ___  ___ ___   __| | ___  | ____|_ __ _ __ ___  _ __
    // | | | |/ _ \/ __/ _ \ / _` |/ _ \ |  _| | '__| '__/ _ \| '__|
    // | |_| |  __/ (_| (_) | (_| |  __/ | |___| |  | | | (_) | |
    // |____/ \___|\___\___/ \__,_|\___| |_____|_|  |_|  \___/|_|
    | Left error -> error |> string |> Logger.err (tag "handleClientResponse")
    state

  // ** handleSocketEvent

  let private handleSocketEvent state (ev: TcpClientEvent) agent =
    match ev with
    | TcpClientEvent.Request  request  -> handleServerRequest  state request  agent
    | TcpClientEvent.Response response -> handleClientResponse state response agent

    | TcpClientEvent.Connected _ ->
      ServiceStatus.Running
      |> Msg.SetStatus
      |> agent.Post
      state

    | TcpClientEvent.Disconnected _ ->
      "Connection to Server closed"
      |> Error.asClientError (tag "handleSocketEvent")
      |> ServiceStatus.Failed
      |> Msg.SetStatus
      |> agent.Post
      state

  // ** handleStop

  let private handleStop (state: ClientState) =
    requestUnRegister state
    state

  // ** handleDispose

  let private handleDispose (state: ClientState) =
    dispose state.SocketSubscription
    state.Stopper.Set() |> ignore
    { state with Client = { state.Client with Status = ServiceStatus.Stopping } }

  // ** makeSocket

  let private makeSocket (server: DiscoServer) (client: DiscoClient) (agent: ApiAgent) =
    let socket =
      TcpClient.create {
        Tag = "ApiClient.TcpClient"
        ClientId = client.Id
        PeerAddress = server.IpAddress
        PeerPort = server.Port
        Timeout = int Constants.REQ_TIMEOUT * 1<ms>
      }
    let subscription = socket.Subscribe (Msg.SocketEvent >> agent.Post)
    subscription, socket

  // ** handleRestart

  let private handleRestart (state: ClientState) (server: DiscoServer) (agent: ApiAgent) =
    dispose state.Socket
    dispose state.SocketSubscription

    server.Port
    |> sprintf "Connecting to server on %O:%O" server.IpAddress
    |> Logger.info (tag "start")

    let subscription, socket = makeSocket server state.Client agent

    socket.Connect()

    { state with Socket = socket; SocketSubscription = subscription }

  // ** loop

  let private loop (store: IAgentStore<ClientState>) inbox msg =
    let state = store.State
    let newstate =
      match msg with
      | Msg.Restart    server  -> handleRestart     state server   inbox
      | Msg.Notify ev          -> handleNotify      state ev
      | Msg.Dispose            -> handleDispose     state
      | Msg.Start              -> handleStart       state inbox
      | Msg.Stop               -> handleStop        state
      | Msg.SetStatus status   -> handleSetStatus   state status   inbox
      | Msg.SetState newstate  -> handleSetState    state newstate inbox
      | Msg.Update sm          -> handleUpdate      state sm       inbox
      | Msg.SocketEvent   ev   -> handleSocketEvent state ev       inbox
    store.Update newstate

  // ** ApiClient module

  [<RequireQualifiedAccess>]
  module ApiClient =

    // *** create

    let create (server: DiscoServer) (client: DiscoClient) =
      let cts = new CancellationTokenSource()
      let subscriptions = new Subscriptions()

      let store:IAgentStore<ClientState> = AgentStore.create()

      let agent = ThreadActor.create "ApiClient" (loop store)
      let subscription, socket = makeSocket server client agent

      let state =
        { Client = { client with Status = ServiceStatus.Stopped }
          Peer = server
          Socket = socket
          Store = Store(State.Empty)
          Subscriptions = subscriptions
          Stopper = new AutoResetEvent(false)
          SocketSubscription = subscription }

      store.Update state

      { new IApiClient with
          // **** Id

          member self.Id with get () = client.Id

          // **** Start

          member self.Start () =
            either {
              server.Port
              |> sprintf "Connecting to server on %O:%O" server.IpAddress
              |> Logger.info (tag "start")

              do agent.Start()
              do socket.Connect()
            }
          // **** Restart

          member self.Restart(server: DiscoServer) =
            server |> Msg.Restart |> agent.Post |> Either.succeed

          // **** State

          member self.State
            with get () = store.State.Store.State // :D

          // **** Status

          member self.Status
            with get () = store.State.Client.Status

          // **** Subscribe

          member self.Subscribe (callback: ClientEvent -> unit) =
            Observable.subscribe callback subscriptions

          // **** Dispose

          //  ____  _
          // |  _ \(_)___ _ __   ___  ___  ___
          // | | | | / __| '_ \ / _ \/ __|/ _ \
          // | |_| | \__ \ |_) | (_) \__ \  __/
          // |____/|_|___/ .__/ \___/|___/\___|
          //             |_|

          member self.Dispose () =
            agent.Post Msg.Stop
            match store.State.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0) with
            | true -> ()
            | false ->
              "attempt to un-register with server failed: timeout"
              |> Logger.err (tag "Dispose")
              ServiceStatus.Disposed |> ClientEvent.Status |> Msg.Notify |> agent.Post
              if not (store.State.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
                "attempt to dispose api client failed: timeout"
                |> Logger.info (tag "Dispose")
            dispose cts
            dispose store.State
            store.Update {
              store.State with
                Client = { client with Status = ServiceStatus.Disposed }
            }

          // **** AddCue

          //   ____
          //  / ___|   _  ___
          // | |  | | | |/ _ \
          // | |__| |_| |  __/
          //  \____\__,_|\___|

          member self.AddCue (cue: Cue) =
            cue
            |> AddCue
            |> handleRequest store

          // **** UpdateCue

          member self.UpdateCue (cue: Cue) =
            cue
            |> UpdateCue
            |> handleRequest store

          // **** RemoveCue

          member self.RemoveCue (cue: Cue) =
            cue
            |> RemoveCue
            |> handleRequest store

          // **** AddPinGroup

          member self.AddPinGroup (group: PinGroup) =
            group
            |> AddPinGroup
            |> handleRequest store

          // **** UpdatePinGroup

          member self.UpdatePinGroup (group: PinGroup) =
            group
            |> UpdatePinGroup
            |> handleRequest store

          // **** RemovePinGroup

          member self.RemovePinGroup (group: PinGroup) =
            group
            |> RemovePinGroup
            |> handleRequest store

          // **** AddCueList

          member self.AddCueList (cuelist: CueList) =
            cuelist
            |> AddCueList
            |> handleRequest store

          // **** UpdateCueList

          member self.UpdateCueList (cuelist: CueList) =
            cuelist
            |> UpdateCueList
            |> handleRequest store

          // **** RemoveCueList

          member self.RemoveCueList (cuelist: CueList) =
            cuelist
            |> RemoveCueList
            |> handleRequest store

          // **** AddPin

          member self.AddPin(pin: Pin) =
            pin
            |> AddPin
            |> handleRequest store

          // **** UpdatePin

          member self.UpdatePin(pin: Pin) =
            pin
            |> UpdatePin
            |> handleRequest store

          // **** UpdateSlices

          member self.UpdateSlices(slices: Slices list) =
            slices
            |> UpdateSlices.ofList
            |> handleRequest store

          // **** RemovePin

          member self.RemovePin(pin: Pin) =
            pin
            |> RemovePin
            |> handleRequest store

          // **** Append

          member self.Append(cmd: StateMachine) =
            handleRequest store cmd
        }
