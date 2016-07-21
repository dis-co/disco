module Iris.Service.Raft.Utilities

open System
open System.Threading
open Iris.Core
open Iris.Service
open Pallet.Core
open fszmq
open Db

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

/// ## Format ZeroMQ URI
///
/// Formates the given IrisNode's host metadata into a ZeroMQ compatible resource string.
///
/// ### Signature:
/// - data: IrisNode
///
/// Returns: string
let formatUri (data: IrisNode) =
  sprintf "tcp://%s:%d" (string data.IpAddr) data.Port

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

let handleException (tag: string) (exn: 't when 't :> Exception) =
  printfn "[%s]"          tag
  printfn "Exception: %s" exn.Message
  printfn "Source: %s"    exn.Source
  printfn "StackTrace:"
  printfn "%s"            exn.StackTrace
  printfn "Aborting."
  exit 1
