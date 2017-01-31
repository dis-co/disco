namespace Iris.Zmq

// * Imports

open ZeroMQ

// * Common

[<AutoOpen>]
module Common =

  // ** ZmqTransport

  type ZmqTransport =
    | TCP
    | UDP
    | PGM
    | EPGM

    override transport.ToString() =
      match transport with
      | TCP  -> "tcp"
      | UDP  -> "udp"
      | PGM  -> "pgm"
      | EPGM -> "epgm"

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
