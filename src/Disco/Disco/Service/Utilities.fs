(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service

module Utilities =

  open System.Threading

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
