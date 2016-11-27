namespace Iris.Service

// * Imports
open LibGit2Sharp

open System
open System.Collections.Concurrent
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Zmq

// * Connections

type private Connections = ConcurrentDictionary<Id,Req>

// * RaftAppState

//  ____        __ _      _               ____  _        _
// |  _ \ __ _ / _| |_   / \   _ __  _ __/ ___|| |_ __ _| |_ ___
// | |_) / _` | |_| __| / _ \ | '_ \| '_ \___ \| __/ _` | __/ _ \
// |  _ < (_| |  _| |_ / ___ \| |_) | |_) |__) | || (_| | ||  __/
// |_| \_\__,_|_|  \__|_/   \_\ .__/| .__/____/ \__\__,_|\__\___|
//                            |_|   |_|

[<NoComparison;NoEquality>]
type RaftAppContext =
  { Status:      ServiceStatus
    Raft:        RaftValue
    Options:     IrisConfig
    Connections: Connections
    Callbacks:   IRaftCallbacks }

  override self.ToString() =
    sprintf "Raft: %A" self.Raft

  interface IDisposable with
    member self.Dispose () =
      for KeyValue(_, connection) in self.Connections do
        dispose connection
      self.Connections.Clear()

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

  // ** getNode

  /// ## getNode
  ///
  /// Return the current node.
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  ///
  /// Returns: RaftNode
  let getNode (context: RaftAppContext) =
    context
    |> getRaft
    |> Raft.getSelf

  // ** getNodeId

  /// ## getNodeId
  ///
  /// Return the current node Id.
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  ///
  /// Returns: Id
  let getNodeId (context: RaftAppContext) =
    context
    |> getRaft
    |> Raft.getSelf
    |> Node.getId

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
  let updateRaft (context: RaftAppContext) (raft: RaftValue) : RaftAppContext =
    { context with Raft = raft }
