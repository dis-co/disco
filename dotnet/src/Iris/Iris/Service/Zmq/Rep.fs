namespace Iris.Service.Zmq

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
type Rep (addr: string, handle: byte array -> byte array) =
  let uuid = Guid.NewGuid()

  let mutable error : Exception option = None
  let mutable started = false
  let mutable disposed = false
  let mutable run = true
  let mutable sock: ZSocket = null
  let mutable thread: Thread = null
  let mutable starter: AutoResetEvent = null
  let mutable stopper: AutoResetEvent = null
  let mutable ctx : ZContext = null

  let ignoreErr (errno: int) =
    match errno with
    | x when x = ZError.ETIMEDOUT.Number -> true
    | x when x = ZError.EAGAIN.Number    -> true
    | _ -> false

  let setOption (sock: ZSocket) (option: ZSocketOption) (value: int) =
    sock.SetOption(option, value)
    |> ignore                            // FIXME: maybe I should do something with this result

  let bind (sock: ZSocket) (addr: string) =
    sock.Bind(addr)

  let tryUnbind (sock: ZSocket) (addr: string) =
    try
      sock.Unbind(addr)
    with
      | _ -> () // throws ENOENT on failure. We ignore that.

  let tryClose (sock: ZSocket) =
    try
      sock.Close()
    with
      | _ -> () // ....at least we tried!

  let worker () =                                             // thread worker function
    if not started && isNull sock then                           // if the socket is null
      try
        ctx  <- new ZContext()
        sock <- new ZSocket(ctx, ZSocketType.REP)            // create it
        setOption sock ZSocketOption.RCVTIMEO 50            // periodic timeout to interupt loop
        bind sock addr                                      // bind to address
        starter.Set() |> ignore
      with
        | exn ->
          run <- false
          error <- Some exn
          starter.Set() |> ignore

    while run do
      try
        let frame = sock.ReceiveFrame()                       // block to receive a request
        let bytes = frame.Read()                              // read the number as byte buffer

        let reply = new ZFrame(handle bytes)                  // handle request & create response
        sock.Send(reply)                                      // send response back

        dispose frame                                         // dispose of frame
        dispose reply                                         // dispose of reply

      with
        /// ignore timeouts, since they are our way to ensure we can
        /// cancel close and dispose the socket in time
        | :? ZException as e when ignoreErr e.ErrNo ->
          ignore e

        | exn ->
          run <- false
          error <- Some exn
          starter.Set() |> ignore

    setOption sock ZSocketOption.LINGER 0                      // loop exited, so set linger to 0
    tryUnbind sock addr                                        // try to unbind the socket from addr
    tryClose  sock                                             // and close socket
    tryDispose sock ignore                                     // dispose socket
    tryDispose ctx ignore                                      // dispose the context

    stopper.Set() |> ignore                                     // signal that Stop is done

  do
    starter <- new AutoResetEvent(false)                       // initialise the signals
    stopper <- new AutoResetEvent(false)

  member self.Stop () =
    run <- false                                               // break loop by setting to false
    stopper.WaitOne() |> ignore                                // wait for signal that stopping done

  member self.Start () =
    thread <- new Thread(new ThreadStart(worker))              // create worker thread
    thread.Start()                                            // start worker thread
    starter.WaitOne() |> ignore                                // wait for startup done signal

    match error with
    | Some exn -> raise exn                                    // re-raise the exception on the
    | _ -> ()                                                  // parents thread, so it can be
                                                              // caught and handled synchronously

  interface IDisposable with
    member self.Dispose() =
      self.Stop()
