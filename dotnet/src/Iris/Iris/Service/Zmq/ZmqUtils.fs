namespace Iris.Service.Zmq

// * Imports

open System
open System.Threading
open ZeroMQ
open Iris.Raft
open Iris.Core
open Iris.Service

[<AutoOpen>]
module ZmqUtils =

  // * formatUri

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

  // * nodeUri

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

  // * request

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

  // * mkReqSocket

  /// ## Make a new client socket with correct settings
  ///
  /// Creates a new req type socket with correct settings, connects and returns it.
  ///
  /// ### Signature:
  /// - uri: string uri of peer to connect to
  /// - state: current app state
  ///
  /// Returns: fszmq.Socket
  let mkReqSocket (node: RaftNode) =
    let timeout = 2000 // FIXME: this request timeout value should be settable
    let addr = nodeUri node
    let socket = new Req(node.Id, addr, timeout)
    socket.Start()
    socket

  // * getSocket

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

  // * disposeSocket

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

  // * rawRequest

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

  // * performRequest

  /// ## Send RaftRequest to node
  ///
  /// Sends given RaftRequest to node. If the request times out, None is return to indicate
  /// failure. Otherwise the de-serialized RaftResponse is returned, wrapped in option to
  /// indicate whether de-serialization was successful.
  ///
  /// ### Signature:
  /// - request:    RaftRequest to send
  /// - client:     client socket to use
  ///
  /// Returns: Either<IrisError,RaftResponse>
  let performRequest (request: RaftRequest) (client: Req) =
    either {
      try
        let! response = rawRequest request client
        return response
      with
        | :? TimeoutException ->
          return!
            "Operation timed out"
            |> SocketError
            |> Either.fail
        | exn ->
          return!
            exn.Message
            |> sprintf "performRequest encountered an exception: %s"
            |> SocketError
            |> Either.fail
    }
