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
  let mutable exn : Exception option = None
  let mutable started = false
  let mutable disposed = false
  let mutable run = true
  let mutable sock: ZSocket = null
  let mutable thread: Thread = null
  let mutable starter: AutoResetEvent = null
  let mutable stopper: AutoResetEvent = null
  let mutable ctx : ZContext = null

  let worker () =                                             // thread worker function
    if isNull sock then                                       // if the socket is null
      try
        ctx  <- new ZContext()
        sock <- new ZSocket(ctx, ZSocketType.REP)            // create it
        sock.SetOption(ZSocketOption.RCVTIMEO, 50) |> ignore // periodic timeout to interupt loop
        sock.Bind(addr)                                     // bind to address
        starter.Set() |> ignore                              // signal Start that startup is done
      with
        | failure ->
          run <- false
          exn <- Some failure
          starter.Set() |> ignore

    while run do
      try
        let frame = sock.ReceiveFrame()                       // block to receive a request
        let bytes = frame.Read()                              // read the number as byte buffer

        let reply = new ZFrame(handle bytes)                  // handle request & create response
        sock.Send(reply)                                      // send response back

        frame.Dispose()                                       // dispose of frame
        reply.Dispose()                                       // dispose of reply
      with
        | :? ZException as e ->
          ignore e                      // FIXME: should probably look at excepion type and handle
                                        // it instead of .. not.
        | failure ->
          run <- false
          exn <- Some failure
          starter.Set() |> ignore

    sock.SetOption(ZSocketOption.LINGER, 0) |> ignore          // loop exited, so set linger to 0
    sock.Unbind(addr)
    sock.Close()                                              // and close socket
    dispose sock                                              // dispose socket
    dispose ctx
    stopper.Set() |> ignore                                    // signal that Stop is done

  do
    starter <- new AutoResetEvent(false)                       // initialise the signals
    stopper <- new AutoResetEvent(false)

  member self.Stop () =
    run <- false                                               // break loop by setting this to false
    stopper.WaitOne() |> ignore                                // wait for signal that stopping done

  member self.Start () =
    thread <- new Thread(new ThreadStart(worker))              // create worker thread
    thread.Start()                                            // start worker thread
    starter.WaitOne() |> ignore                                // wait for startup done signal

    match exn with
    | Some failure -> failwith failure.Message                 // re-raise the exception on the
    | _ -> ()                                                  // parents thread, so it can be caught

  interface IDisposable with
    member self.Dispose() =
      self.Stop()
