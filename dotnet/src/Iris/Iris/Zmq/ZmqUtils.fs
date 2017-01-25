namespace Iris.Zmq

// * Imports

open System
open System.Threading
open ZeroMQ
open Iris.Raft
open Iris.Core

// * ZmqUtils

[<AutoOpen>]
module ZmqUtils =

  // ** formatUri

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

  // ** nodeUri

  /// ## Format ZeroMQ URI for passed RaftMember
  ///
  /// Formates the given RaftMember's host metadata into a ZeroMQ compatible resource string.
  ///
  /// ### Signature:
  /// - data: RaftMember
  ///
  /// Returns: string
  let memUri (data: RaftMember) =
    formatUri data.IpAddr (int data.Port)
