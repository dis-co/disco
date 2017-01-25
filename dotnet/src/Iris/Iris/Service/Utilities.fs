namespace Iris.Service

module Utilities =

  open System
  open System.IO
  open System.Threading
  open FSharpx.Functional
  open Iris.Raft
  open Iris.Core

  /// ## cancelToken
  ///
  /// Cancel a CancellationToken and capture the exception.
  ///
  /// ### Signature:
  /// - token: CancellationToken
  ///
  /// Returns: unit
  let cancelToken (token: CancellationTokenSource) =
    try
      token.Cancel()
    with
      | _ -> ()

  /// ## maybe cancel a CancellationTokenSource
  ///
  /// Cancels a ref to an CancellationTokenSource. Assign None when done.
  ///
  /// ### Signature:
  /// - cts: CancellationTokenSource option ref
  ///
  /// Returns: unit
  let maybeCancelToken (cts: CancellationTokenSource option ref) =
    match !cts with
    | Some token ->
      cancelToken token
      cts := None
    | _ -> ()
