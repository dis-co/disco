namespace Iris.Service.Zmq

open System
open System.Threading
open ZeroMQ
open Iris.Raft
open Iris.Core
open Iris.Service

[<AutoOpen>]
module ZmqUtils =

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

  /// ## execute a request and return response
  ///
  /// execucte a RaftRequest and return response
  ///
  /// ### Signature:
  /// - sock: Req socket object
  /// - req: the reqest value
  ///
  /// Returns: RaftResponse option
  let request (sock: Req) (req: RaftRequest) : Either<IrisError,RaftResponse> =
    req |> Binary.encode |> sock.Request |> Either.bind Binary.decode

  /// ## Make a new client socket with correct settings
  ///
  /// Creates a new req type socket with correct settings, connects and returns it.
  ///
  /// ### Signature:
  /// - uri: string uri of peer to connect to
  /// - state: current app state
  ///
  /// Returns: fszmq.Socket
  let mkClientSocket (uri: string) (state: RaftAppContext) =
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

  let addSocket (node: RaftNode) (state: RaftAppContext) (connections: Map<Id,Zmq.Req>) =
    let addr = nodeUri node
    let socket = mkClientSocket addr state
    (socket, Map.add node.Id socket connections)

  /// ## getSocket for Member
  ///
  /// Gets the socket we memoized for given MemberId, else creates one and instantiates a
  /// connection.
  ///
  /// ### Signature:
  /// - appState: current TVar<AppState>
  ///
  /// Returns: Req option
  let getSocket (node: RaftNode) (connections: Map<Id,Zmq.Req>) : Req option =
    Map.tryFind node.Id connections

  /// ## Dispose of a client socket
  ///
  /// Dispose of a client socket that we don't need anymore.
  ///
  /// ### Signature:
  /// - node: Node whose socket should be disposed of.
  /// - appState: RaftAppContext TVar
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
  /// - state: RaftAppContext to perform request against
  ///
  /// Returns: RaftResponse option
  let rawRequest (request: RaftRequest) (client: Req) : Either<IrisError,RaftResponse> =
    request
    |> Binary.encode
    |> client.Request
    |> Either.bind Binary.decode<IrisError,RaftResponse>

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
  let performRequest (request: RaftRequest) (node: RaftNode) (state: RaftAppContext) (connections: Map<Id,Zmq.Req>) =
    either {
      let client = getSocket node connections
      try
        let! response = rawRequest request client
        return (response, connections)
      with
        | :? TimeoutException ->
          disposeSocket node connections
          "Operation timed out"
          |> SocketError
          |> Either.fail

        | exn ->
          disposeSocket node connections
          sprintf "performRequest encountered an exception: %s" exn.Message
          |> SocketError
          |> Either.fail
    }
