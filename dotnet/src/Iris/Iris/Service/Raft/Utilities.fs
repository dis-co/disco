module Iris.Service.Raft.Utilities

open System
open System.Threading
open Iris.Core
open Iris.Service
open Pallet.Core
open fszmq
open Db
open FSharpx.Functional

// ------------------------------------------------------------------------------------- //
//                            _   _ _   _ _ _ _   _                                      //
//                           | | | | |_(_) (_) |_(_) ___  ___                            //
//                           | | | | __| | | | __| |/ _ \/ __|                           //
//                           | |_| | |_| | | | |_| |  __/\__ \                           //
//                            \___/ \__|_|_|_|\__|_|\___||___/                           //
// ------------------------------------------------------------------------------------- //

/// ## Get the current machine's host name
///
/// Get the current machine's host name.
///
/// ### Signature:
/// - unit: unit
///
/// Returns: string
let getHostName () =
  System.Net.Dns.GetHostName()

/// ## Create a new Raft state
///
/// Create a new initial Raft state value from the passed-in options.
///
/// ### Signature:
/// - options: RaftOptions
///
/// Returns: Raft<StateMachine,IrisNode>
let createRaft (options: RaftOptions) =
  let node =
    { MemberId = createGuid()
    ; HostName = getHostName()
    ; IpAddr   = IpAddress.Parse options.IpAddr
    ; Port     = options.RaftPort
    ; TaskId   = None
    ; Status   = IrisNodeStatus.Running }
    |> Node.create (RaftId options.RaftId)
  Raft.create node

let loadRaft (options: RaftOptions) =
  let dir = options.DataDir </> DB_NAME
  match IO.Directory.Exists dir with
    | true -> openDB dir |> Option.bind loadRaft
    | _    -> None

let mkRaft (options: RaftOptions) =
  match loadRaft options with
    | Some raft -> raft
    | _         -> createRaft options

/// ## Create an AppState value
///
/// Given the `RaftOptions`, create or load data and construct a new `AppState` for the
/// `RaftServer`.
///
/// ### Signature:
/// - context: `ZeroMQ` `Context`
/// - options: `RaftOptions`
///
/// Returns: AppState
let mkState (context: Context) (options: RaftOptions) : AppState =
  { Clients     = []
  ; Sessions    = []
  ; Projects    = Map.empty
  ; Peers       = Map.empty
  ; Connections = Map.empty
  ; Raft        = mkRaft options
  ; Context     = context
  ; Options     = options
  }

/// ## idiomatically cancel a CancellationTokenSource
///
/// Cancels a ref to an CancellationTokenSource. Assign None when done.
///
/// ### Signature:
/// - cts: CancellationTokenSource option ref
///
/// Returns: unit
let cancelToken (cts: CancellationTokenSource option ref) =
  match !cts with
  | Some token ->
    try
      token.Cancel()
    finally
      cts := None
  | _ -> ()

/// ## Print debug information and exit
///
/// Print debug information and exit
///
/// ### Signature:
/// - tag: string tag to attach for easier debugging
/// - exn: the Exception
///
/// Returns: unit
let handleException (tag: string) (exn: 't when 't :> Exception) =
  printfn "[%s]"          tag
  printfn "Exception: %s" exn.Message
  printfn "Source: %s"    exn.Source
  printfn "StackTrace:"
  printfn "%s"            exn.StackTrace
  printfn "Aborting."
  exit 1

// ----------------------------------------------------------------------------------------- //
//                   _   _      _                      _    _                                //
//                  | \ | | ___| |___      _____  _ __| | _(_)_ __   __ _                    //
//                  |  \| |/ _ \ __\ \ /\ / / _ \| '__| |/ / | '_ \ / _` |                   //
//                  | |\  |  __/ |_ \ V  V / (_) | |  |   <| | | | | (_| |                   //
//                  |_| \_|\___|\__| \_/\_/ \___/|_|  |_|\_\_|_| |_|\__, |                   //
//                                                                  |___/                    //
// ----------------------------------------------------------------------------------------- //

/// ## Format ZeroMQ URI
///
/// Formates the given IP and port into a ZeroMQ compatible resource string.
///
/// ### Signature:
/// - ip: IpAddress
/// - port: Port
///
/// Returns: string
let formatUri (ip: IpAddress) (port: int) =
  sprintf "tcp://%s:%d" (string ip) port

/// ## Format ZeroMQ URI for passed NodeInfo
///
/// Formates the given IrisNode's host metadata into a ZeroMQ compatible resource string.
///
/// ### Signature:
/// - data: IrisNode
///
/// Returns: string
let nodeUri (data: IrisNode) =
  formatUri data.IpAddr data.Port

/// ## Make a new client socket with correct settings
///
/// Creates a new req type socket with correct settings, connects and returns it.
///
/// ### Signature:
/// - uri: string uri of peer to connect to
/// - state: current app state
///
/// Returns: fszmq.Socket
let mkClientSocket (uri: string) (state: AppState) =
  let socket = Context.req state.Context
  Socket.setOption socket (ZMQ.RCVTIMEO,int state.Raft.RequestTimeout)
  Socket.connect socket uri
  socket

/// ## getSocket for Member
///
/// Gets the socket we memoized for given MemberId, else creates one and instantiates a
/// connection.
///
/// ### Signature:
/// - appState: current TVar<AppState>
///
/// Returns: Socket
let getSocket (node: Node) (state: AppState) =
  match Map.tryFind node.Data.MemberId state.Connections with
  | Some client ->
    Thread.CurrentThread.ManagedThreadId
    |> printfn "[Raft: %A] Found Socket for %s on thread %d" state.Raft.Node.Id (nodeUri node.Data)

    (client, state)
  | _  ->
    let addr = nodeUri node.Data
    let socket = mkClientSocket addr state

    Thread.CurrentThread.ManagedThreadId
    |> printfn "[Raft: %A] Created Socket for %s on thread %d" state.Raft.Node.Id addr

    let newstate =
      { state with
          Connections = Map.add node.Data.MemberId socket state.Connections }

    (socket, newstate)

/// ## Dispose of a client socket
///
/// Dispose of a client socket that we don't need anymore.
///
/// ### Signature:
/// - node: Node whose socket should be disposed of.
/// - appState: AppState TVar
///
/// Returns: unit
let disposeSocket (node: Node) state =
  match Map.tryFind node.Data.MemberId state.Connections with
  | Some client ->

    Thread.CurrentThread.ManagedThreadId
    |> printfn "[Raft: %A] Disposing Socket for %s on thread %d" state.Raft.Node.Id (nodeUri node.Data)

    dispose client

    { state with Connections = Map.remove node.Data.MemberId state.Connections }
  | _  -> state

let performRawRequest (request: RaftRequest) (client: Socket) (state: AppState)=
  Thread.CurrentThread.ManagedThreadId
  |> printfn "[Raft: %A] performRawRequest: before request. thread: %d" state.Raft.Node.Id

  // SEND THE REQUEST
  request |> encode |> Socket.send client

  Thread.CurrentThread.ManagedThreadId
  |> printfn "[Raft: %A] performRawRequest: after request. thread: %d" state.Raft.Node.Id

  // BLOCK FOR RESPONSE AND DECODE
  let msg = new Message()
  Message.recv msg client

  Thread.CurrentThread.ManagedThreadId
  |> printfn "[Raft: %A] performRawRequest: got response. thread: %d" state.Raft.Node.Id

  let response = Message.data msg |> decode<RaftResponse>
  dispose msg
  response

/// ## Send RaftRequest to node
///
/// Sends given RaftRequest to node. If the request times out, None is return to indicate
/// failure. Otherwise the de-serialized RaftResponse is returned, wrapped in option to
/// indicate whether de-serialization was successful.
///
/// ### Signature:
/// - thing:    RaftRequest to send
/// - node:     node to send the message to
/// - appState: application state TVar
///
/// Returns: RaftResponse option
let performRequest (request: RaftRequest) (node: Node<IrisNode>) (state: AppState) =
    let client, state = getSocket node state
    try
      client
      |> flip (performRawRequest request) state
      |> fun response ->
        (response, state)
    with
      | :? TimeoutException ->
        let state = disposeSocket node state
        None, state
      | exn -> handleException "receiveReply" exn
