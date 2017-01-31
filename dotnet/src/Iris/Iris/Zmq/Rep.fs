namespace Iris.Zmq

open System
open System.Threading
open ZeroMQ
open Iris.Core

/// ## Rep
///
/// Thread-safe Rep socket (corresponds to ZSocketType.REP)
///
/// ### Signature:
/// - addr:    Address to bind to
/// - ctx:     ZMQ Context
/// - handler: request handler
///
/// Returns: instance of Rep
type Rep (id: Id, addr: string, handle: byte array -> byte array) =
  let mutable status : ServiceStatus = ServiceStatus.Starting

  let mutable error : Exception option = None
  let mutable disposed = false
  let mutable run = true
  let mutable sock: ZSocket = null
  let mutable thread: Thread = null
  let mutable starter: AutoResetEvent = null
  let mutable stopper: AutoResetEvent = null
  let mutable ctx : ZContext = null

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
    |> ignore                            // FIXME: maybe I should do something with this result

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

  /// ## tryCloseb
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

  /// ## worker
  ///
  /// Worker function to wrap the ZSocket and ZContext. This provides a thread-safe means to run
  /// ZSockets in a multi-threaded environment.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  let worker () =

    /// ## Initialization
    ///
    /// In the beginning, there was darkness, and no server responded. So God decided, its time for
    /// some action and create the `ZContext` and a `REP`-`ZSocket` such that the universe shall
    /// respond to requests.
    ///
    /// God decided to set the `RCVTIMEO` timeout value to 50ms, in order to be able to shut the
    /// thing down in case she gets bored with it all, as calls to `ReceiveFrame` would block
    /// indefinitely. When startup was done, the `starter.Set()` call signals to the parent thread
    /// that everything is hunky dory, and allow it to pass control back to the caller.
    ///
    /// In case of an error (e.g. EADDRINUSE), the run loop is not entered and everything is
    /// disposed again.
    ///
    if isNull sock then
      try
        ctx  <- new ZContext()
        sock <- new ZSocket(ctx, ZSocketType.REP)
        setOption sock ZSocketOption.RCVTIMEO 50
        bind sock addr
        status <- ServiceStatus.Running
        starter.Set() |> ignore
      with
        | exn ->
          run <- false
          error <- Some exn
          status <- ServiceStatus.Failed (SocketError("Rep.worker", exn.Message))
          starter.Set() |> ignore

    /// ## Inner Loop
    ///
    /// This is where the magic happens.
    ///
    /// `ReceiveFrame` blocks until either one of two things happens:
    ///   - `EAGAIN` is thrown, but ignored, and the loop keeps plowing on
    ///   - a message is received
    ///
    /// When a message is received, its byte array contents are pulled out and processed with the
    /// specified `handler` function. The result is then wrapped in a new `ZFrame` and sent back to
    /// the requesting party. Both, the intial message and its response frame are finally discarded.
    ///
    /// Should any other error occur during the handling of a message, the loop is exited, the error
    /// saved in a variable and all resources are finally disposed.
    while run do
      try
        let frame = sock.ReceiveFrame()
        let bytes = frame.Read()

        let reply = new ZFrame(handle bytes)
        sock.Send(reply)

        dispose frame
        dispose reply

      with
        /// ignore timeouts, since they are our way to ensure we can
        /// cancel close and dispose the socket in time
        | :? ZException as e when ignoreErr e.ErrNo ->
          ignore e

        | exn ->
          run <- false
          status <- ServiceStatus.Failed (SocketError ("Rep.worker",exn.Message))
          error <- Some exn

    /// ## Disposal of resources
    ///
    /// First the socket option `LINGER` is set to 0, to ensure the socket can be disposed of
    /// switftly. Next we attempt to `Unbind` the socket from its address and close it. Finally, we
    /// dispose the thread-local `ZSocket` and `ZContext` and signal that we are done.
    ///
    setOption sock ZSocketOption.LINGER 0
    tryUnbind sock addr
    tryClose  sock
    tryDispose sock ignore
    tryDispose ctx ignore

    disposed <- true

    stopper.Set() |> ignore

  do
    starter <- new AutoResetEvent(false)
    stopper <- new AutoResetEvent(false)

  member self.Status
    with get () = status

  member private self.Stop () =
    if not disposed then
      run <- false                                   // break loop by setting to false
      stopper.WaitOne() |> ignore                    // wait for signal that stopping is done
                                                    // and return to caller

  member self.Start () : Either<IrisError,unit> =
    if not disposed then
      thread <- new Thread(new ThreadStart(worker))  // create worker thread
      thread.Start()                                // start worker thread
      starter.WaitOne() |> ignore                    // wait for startup-done signal

      match error with
      | Some exn ->                                  // if an exception happened on the thread
        exn.Message                                 // format it as an error and return it
        |> Error.asSocketError "Rep.Start"
        |> Either.fail
      | _ -> Right ()                                // parents thread, so it can be
                                                    // caught and handled synchronously
    else
      "already disposed"
      |> Error.asSocketError "Rep.Start"
      |> Either.fail

  interface IDisposable with
    member self.Dispose() =
      self.Stop()
