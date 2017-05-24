namespace Iris.Zmq

// * Imports

open ZeroMQ
open System.Threading

// * Common

[<AutoOpen>]
module Common =

  // ** setOption

  /// ## setOption
  ///
  /// Set a ZSocketOption on a socket in a more functional style.
  ///
  /// ### Signature:
  /// - sock: ZSocket to set option onb
  /// - option: ZSocketOption to set
  /// - value: int value to set on the socket
  ///
  /// Returns: unit
  let setOption (sock: ZSocket) (option: ZSocketOption) (value: int) =
    sock.SetOption(option, value)
    |> ignore

  // ** ingoreErr

  /// ## ignoreErr
  ///
  /// Determine if the error number passed is worth ignoring or not.
  ///
  /// ### Signature:
  /// - errno: int error number to check
  ///
  /// Returns: bool
  let ignoreErr (errno: int) =
    match errno with
    | x when x = ZError.ETIMEDOUT.Number -> true
    | x when x = ZError.EAGAIN.Number    -> true
    | _ -> false

  // ** bind

  /// ## bind
  ///
  /// Bind a ZSocket to an address.
  ///
  /// ### Signature:
  /// - sock: ZSocket to bind
  /// - addr: string address to bind to
  ///
  /// Returns: unit
  let bind (sock: ZSocket) (addr: string) =
    sock.Bind(addr)

  // ** tryUnbind

  /// ## tryUnbind
  ///
  /// Attempt to unbind a ZSocket from a given address safely.
  ///
  /// ### Signature:
  /// - sock: ZSocket to unbind
  /// - addr: string address to unbind from
  ///
  /// Returns: unit
  let tryUnbind (sock: ZSocket) (addr: string) =
    try
      sock.Unbind(addr)
    with
      | _ -> () // throws ENOENT on failure. We ignore that.

  // ** tryClose

  /// ## tryClose
  ///
  /// Attempt to close a ZSocket safely.
  ///
  /// ### Signature:
  /// - sock: ZSocket to close
  ///
  /// Returns: unit
  let tryClose (sock: ZSocket) =
    try
      sock.Close()
    with
      | _ -> () // ....at least we tried!

  // ** tryDispose

  /// ## tryDispose
  ///
  /// Attempt to dispose a ZSocket safely.
  ///
  /// ### Signature:
  /// - sock: ZSocket to dispose
  ///
  /// Returns: unit
  let tryDispose (sock: ZSocket) =
    try
      sock.Dispose()
    with
      | _ -> () // ....at least we tried!

// * RequestCount module

[<RequireQualifiedAccess>]
module private RequestCount =
  let mutable private id = 0L
  let increment () = Interlocked.Increment &id |> ignore
  let current () = id
