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

  /// ## Create an RaftAppState value
  ///
  /// Given the `RaftOptions`, create or load data and construct a new `RaftAppState` for the
  /// `RaftServer`.
  ///
  /// ### Signature:
  /// - context: `ZeroMQ` `Context`
  /// - options: `RaftOptions`
  ///
  /// Returns: RaftAppState
  let mkState (context: ZeroMQ.ZContext) (options: IrisConfig) : Either<IrisError,RaftAppContext> =
    getRaft options
    |> Either.map
        (fun raft ->
          { Raft      = raft
          ; Context   = context
          ; Options   = options })

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

    exn.Message
    |> Other
    |> Error.exitWith
