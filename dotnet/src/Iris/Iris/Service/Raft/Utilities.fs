module Iris.Service.Raft.Utilities

open System
open System.Threading
open FSharpx.Functional
open Iris.Service
open Pallet.Core
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
let indent (num: int) (str: string) =
  let spaces = Array.fold (fun m _ -> m + " ") "" [| 1 .. num |]
  str.Split('\n')
  |> Array.map (fun line -> spaces + line)
  |> Array.fold (fun m line -> sprintf "%s\n%s" m line) ""

let unixTime (date: DateTime) =
  let epoch = new DateTime(1970, 1, 1)
  (date.Ticks - epoch.Ticks) / TimeSpan.TicksPerMillisecond

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
let mkState (context: ZeroMQ.ZContext) (options: RaftOptions) : AppState =
  { Clients     = []
  ; Sessions    = []
  ; Projects    = Map.empty
  ; Peers       = Map.empty
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
  let socket = new Req(uri, state.Context, int state.Raft.RequestTimeout)
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
let getSocket (node: Node) (state: AppState) (connections: Map<MemberId,Zmq.Req>) =
  match Map.tryFind node.Data.MemberId connections with
  | Some client -> (client, connections)
  | _  ->
    let addr = nodeUri node.Data
    let socket = mkClientSocket addr state
    (socket, Map.add node.Data.MemberId socket connections)

/// ## Dispose of a client socket
///
/// Dispose of a client socket that we don't need anymore.
///
/// ### Signature:
/// - node: Node whose socket should be disposed of.
/// - appState: AppState TVar
///
/// Returns: unit
let disposeSocket (node: Node) (connections: Map<MemberId,Zmq.Req>) =
  match Map.tryFind node.Data.MemberId connections with
  | Some client ->
    dispose client
    Map.remove node.Data.MemberId connections
  | _  -> connections

/// ## Perform a raw request cycle on a request socket
///
/// Request a resource and return its response.
///
/// ### Signature:
/// - request: RaftRequest to perform
/// - client: Req socket object
/// - state: AppState to perform request against
///
/// Returns: RaftResponse option
let rawRequest (request: RaftRequest) (client: Req) =
  request
  |> encode
  |> client.Request
  |> Option.bind decode<RaftResponse>

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
let performRequest (request: RaftRequest) (node: Node<IrisNode>) (state: AppState) (connections: Map<MemberId,Zmq.Req>) =
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
