namespace Iris.Service

// * Imports

open LibGit2Sharp
open Iris.Core.Utils
open Iris.Core
open Iris.Raft
open Iris.Service.Zmq

// * RaftAppState

//  ____        __ _      _               ____  _        _
// |  _ \ __ _ / _| |_   / \   _ __  _ __/ ___|| |_ __ _| |_ ___
// | |_) / _` | |_| __| / _ \ | '_ \| '_ \___ \| __/ _` | __/ _ \
// |  _ < (_| |  _| |_ / ___ \| |_) | |_) |__) | || (_| | ||  __/
// |_| \_\__,_|_|  \__|_/   \_\ .__/| .__/____/ \__\__,_|\__\___|
//                            |_|   |_|
[<NoComparison;NoEquality>]
type RaftAppContext =
  { Context:     ZeroMQ.ZContext
    Connections: Map<Id,Req>
    Raft:        RaftValue
    Options:     IrisConfig }

  override self.ToString() =
    sprintf "Raft: %A" self.Raft

// * RaftContext

[<RequireQualifiedAccess>]
module RaftContext =

  // ** getRaft

  /// ## pull Raft state value out of RaftAppContext value
  ///
  /// Get Raft state value from RaftAppContext.
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  ///
  /// Returns: Raft
  let getRaft (context: RaftAppContext) =
    context.Raft

  // ** updateRaft

  /// ## Update Raft in RaftAppContext
  ///
  /// Update the Raft field of a given RaftAppContext
  ///
  /// ### Signature:
  /// - raft: new Raft value to add to RaftAppContext
  /// - state: RaftAppContext to update
  ///
  /// Returns: RaftAppContext
  let updateRaft (raft: RaftValue) (context: RaftAppContext) : RaftAppContext =
    { context with Raft = raft }

  // ** addConnection

  /// ## addConnection
  ///
  /// Add the passed socket connect to the Connections map
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  /// - client: Req
  ///
  /// Returns: RaftAppContext
  let addConnection (context: RaftAppContext) (client: Req) : RaftAppContext =
    { context with
        Connections = Map.add client.Id client context.Connections }

  // ** rmConnection

  /// ## rmConnection
  ///
  /// Remove a socket connection from Connections map
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  /// - id: Id of Node to remove connection for
  ///
  /// Returns: RaftAppContext
  let rmConnection (context: RaftAppContext) (id: Id) : RaftAppContext =
    { context with
        Connections = Map.remove id context.Connections }

  // ** getConnection

  /// ## getConnection
  ///
  /// Get a socket connection from the connections map.
  ///
  /// ### Signature:
  /// - context: RaftAppContex
  /// - id: Node Id to get connection for
  ///
  /// Returns: Req option
  let getConnection (context: RaftAppContext) (id: Id) : Req option =
    Map.tryFind id context.Connections
