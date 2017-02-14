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

  let private toUri (proto: ZmqTransport) (prefix: IpAddress option) (ip: IpAddress) (port: int) =
    match prefix with
    | Some prefix -> sprintf "%s://%s;%s:%d" (string proto) (string prefix) (string ip) port
    | _ -> sprintf "%s://%s:%d" (string proto) (string ip) port

  // ** formatPGMUri

  /// ## Format ZeroMQ PGM URI
  ///
  /// Formates the given IP and port into a ZeroMQ compatible PGM resource string.
  ///
  /// ### Signature:
  /// - ip: IpAddress
  /// - port: Port
  ///
  /// Returns: string
  let formatPGMUri (ip: IpAddress) (mcast: IpAddress) (port: int) =
    toUri PGM (Some ip) mcast port

  // ** formatEPGMUri

  /// ## Format ZeroMQ EPGM URI
  ///
  /// Formates the given IP and port into a ZeroMQ compatible PGM resource string.
  ///
  /// ### Signature:
  /// - ip: IpAddress
  /// - port: Port
  ///
  /// Returns: string
  let formatEPGMUri (ip: IpAddress) (mcast: IpAddress) (port: int) =
    toUri EPGM (Some ip) mcast port

  // ** formatTCPUri

  /// ## Format ZeroMQ TCO URI
  ///
  /// Formates the given IP and port into a ZeroMQ compatible resource string.
  ///
  /// ### Signature:
  /// - ip: IpAddress
  /// - port: Port
  ///
  /// Returns: string
  let formatTCPUri (ip: IpAddress) (port: int) =
    toUri TCP None ip port

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
    formatTCPUri data.IpAddr (int data.Port)
