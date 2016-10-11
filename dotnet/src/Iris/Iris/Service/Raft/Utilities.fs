module Iris.Service.Raft.Utilities

open System
open System.IO
open System.Threading
open FSharpx.Functional
open Iris.Service
open Iris.Raft
open Iris.Core
open Zmq
open Db


// ------------------------------------------------------------------------------------- //
//                            _   _ _   _ _ _ _   _                                      //
//                           | | | | |_(_) (_) |_(_) ___  ___                            //
//                           | | | | __| | | | __| |/ _ \/ __|                           //
//                           | |_| | |_| | | | |_| |  __/\__ \                           //
//                            \___/ \__|_|_|_|\__|_|\___||___/                           //
// ------------------------------------------------------------------------------------- //

/// ## Create a new Raft state
///
/// Create a new initial Raft state value from the passed-in options.
///
/// ### Signature:
/// - options: RaftOptions
///
/// Returns: Raft<StateMachine,IrisNode>
let createRaft (options: Config) : Either<IrisError, Raft> =
  getNodeId ()
  |> Either.bind (tryFindNode options)
  |> Either.map mkRaft
  |> Either.map (options.ClusterConfig.Nodes |> Array.ofList |> addNodes)

let loadRaft (options: Config) =
  let db =
    options.RaftConfig.DataDir </> DB_NAME
    |> openDB

  let node =
    getNodeId ()
    |> Either.bind (tryFindNode options)

  let nodes =
    options.ClusterConfig.Nodes
    |> List.map (fun node -> node.Id, node)
    |> Map.ofList

  match db, node with
  | Right database, Right node -> loadRaft node nodes database
  | Left error,     _          -> Either.fail error
  | _,              Left error -> Either.fail error

let getRaft (options: Config) =
  match loadRaft options with
    | Right raft -> Either.succeed raft
    | _          -> createRaft options

/// ## Create an RaftAppState value
///
/// Given the `RaftOptions`, create or load data and construct a new `RaftAppState` for the
/// `RaftServer`.
///
/// ### Signature:
/// - context: `ZeroMQ` `Context`
/// - options: `RaftOptions`
///
/// Returns: RaftAppState
let mkState (context: ZeroMQ.ZContext) (options: Config) : Either<IrisError,RaftAppState> =
  getRaft options
  |> Either.map
      (fun raft ->
        { Raft      = raft
        ; Context   = context
        ; Options   = options })

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

  exn.Message
  |> Other
  |> Error.exitWith

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
let nodeUri (data: RaftNode) =
  formatUri data.IpAddr (int data.Port)

/// ## Make a new client socket with correct settings
///
/// Creates a new req type socket with correct settings, connects and returns it.
///
/// ### Signature:
/// - uri: string uri of peer to connect to
/// - state: current app state
///
/// Returns: fszmq.Socket
let mkClientSocket (uri: string) (state: RaftAppState) =
  let timeout = 2000 // FIXME: this request timeout value should be settable
  let socket = new Req(uri, state.Context, timeout)
  socket.Start()
  socket

// let send (socket: ZSocket) (bytes: byte array) =
//   use msg = new ZFrame(bytes)
//   socket.Send(msg)

// let recv (socket: ZSocket) : byte array =
//   use frame = socket.ReceiveFrame()
//   frame.Read()

/// ## getSocket for Member
///
/// Gets the socket we memoized for given MemberId, else creates one and instantiates a
/// connection.
///
/// ### Signature:
/// - appState: current TVar<AppState>
///
/// Returns: Socket
let getSocket (node: RaftNode) (state: RaftAppState) (connections: Map<Id,Zmq.Req>) =
  match Map.tryFind node.Id connections with
  | Some client -> (client, connections)
  | _  ->
    let addr = nodeUri node
    let socket = mkClientSocket addr state
    (socket, Map.add node.Id socket connections)

/// ## Dispose of a client socket
///
/// Dispose of a client socket that we don't need anymore.
///
/// ### Signature:
/// - node: Node whose socket should be disposed of.
/// - appState: RaftAppState TVar
///
/// Returns: unit
let disposeSocket (node: RaftNode) (connections: Map<Id,Zmq.Req>) =
  match Map.tryFind node.Id connections with
  | Some client ->
    dispose client
    Map.remove node.Id connections
  | _  -> connections

/// ## Perform a raw request cycle on a request socket
///
/// Request a resource and return its response.
///
/// ### Signature:
/// - request: RaftRequest to perform
/// - client: Req socket object
/// - state: RaftAppState to perform request against
///
/// Returns: RaftResponse option
let rawRequest (request: RaftRequest) (client: Req) =
  request
  |> Binary.encode
  |> client.Request
  |> Option.bind Binary.decode<RaftResponse>

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
let performRequest (request: RaftRequest) (node: RaftNode) (state: RaftAppState) (connections: Map<Id,Zmq.Req>) =
  let client, connections = getSocket node state connections

  try
    let response = rawRequest request client
    (response, connections)
  with
    | :? TimeoutException ->
      printfn "Operation timed out"
      None, disposeSocket node connections
    | exn ->
      printfn "performRequest exception: %s" exn.Message
      None, disposeSocket node connections
