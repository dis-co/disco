namespace Iris.Service

module Utilities =

  open System
  open System.IO
  open System.Threading
  open FSharpx.Functional
  open Iris.Service
  open Iris.Raft
  open Iris.Core
  open Iris.Service.Persistence

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
  let mabyeCancelToken (cts: CancellationTokenSource option ref) =
    match !cts with
    | Some token ->
      cancelToken token
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
