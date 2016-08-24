module Iris.Service.Raft.Zmq

open System
open System.Threading
open ZeroMQ
open Iris.Core

//  ____             _        _
// / ___|  ___   ___| | _____| |_ ___
// \___ \ / _ \ / __| |/ / _ \ __/ __|
//  ___) | (_) | (__|   <  __/ |_\__ \
// |____/ \___/ \___|_|\_\___|\__|___/ Thread-safe ZMQ Socket abstractions.


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
        ctx <- new ZContext()
        let socket = new ZSocket(ctx, ZSocketType.REP)        // create it
        socket.SetOption(ZSocketOption.RCVTIMEO, 50) |> ignore // periodic timeout to interupt loop
        socket.Bind(addr)                                     // bind to address
        sock <- socket                                         // and safe for later use
        starter.Set() |> ignore                                // signal Start that startup is done
      with
        | failure ->
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
        | :? ZException as e-> ignore e
        | failure ->
          exn <- Some failure
          starter.Set() |> ignore

    sock.SetOption(ZSocketOption.LINGER, 0) |> ignore          // loop exited, so set linger to 0
    sock.Unbind(addr)
    sock.Close()                                              // and close socket
    sock.Dispose()                                            // dispose socket
    ctx.Dispose()
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

/// ## Req
///
/// Thread-safe Req socket corresponds to ZSocketType.Req
///
/// ### Signature:
/// - addr: Address to connect to
/// - ctx: ZeroMQ context
///
/// Returns: instance of Req
type Req (addr: string, ctx: ZContext, timeout: int) =

  let mutable starter:   AutoResetEvent = null
  let mutable stopper:   AutoResetEvent = null
  let mutable requester: AutoResetEvent = null
  let mutable responder: AutoResetEvent = null

  let mutable exn: Exception option = None

  let mutable request:  byte array = [| |]
  let mutable response: byte array = [| |]

  let mutable thread = null
  let mutable run = true
  let mutable started = false
  let mutable disposed = false
  let mutable sock = null
  let mutable lokk = null

  let worker _ =                                              // thread worker function
    if isNull sock then                                       // if not yet present
      sock <- new ZSocket(ctx, ZSocketType.REQ)                // initialise the socket
      sock.SetOption(ZSocketOption.RCVTIMEO, timeout)         // set receive timeout
      |> ignore
      sock.Connect(addr)                                      // connect to server
      started <- true
      starter.Set() |> ignore                                  // signal that startup is done

    while run do
      try
        // wait for the signal that a new request is ready *or* that shutdown is reuqested
        requester.WaitOne() |> ignore

        // `run` is usually true, but shutdown first sets this to false to exit the loop
        if run then
          let frame = new ZFrame(request)                     // create a new ZFrame to send
          sock.Send(frame)                                    // and send it via sock
          response <- sock.ReceiveFrame().Read()               // block and wait for reply frame
          responder.Set() |> ignore                            // signal that response is ready
          frame.Dispose()                                     // dispose of frame
      with
        | e ->
          printfn "expception: %s timeout %d" e.Message timeout
          // save exception to be rethrown on the callers thread
          exn <- Some e
          // prevent re-entering the loop
          run <- false
          // set the responder so self.Request does not block indefinietely
          responder.Set() |> ignore

    sock.SetOption(ZSocketOption.LINGER, 0) |> ignore          // set linger to 0 to close socket quickly
    sock.Close()                                              // close the socket
    sock.Dispose()                                            // dispose of it
    disposed <- true                                           // this socket is disposed
    started <- false                                           // and not running anymore
    stopper.Set() |> ignore                                    // signal that everything was cleaned up now

  do
    lokk      <- new Object()                                  // lock object
    starter   <- new AutoResetEvent(false)                     // initialize the signals
    stopper   <- new AutoResetEvent(false)
    requester <- new AutoResetEvent(false)
    responder <- new AutoResetEvent(false)

  member self.Start() =
    if not started && not disposed then
      thread <- new Thread(new ThreadStart(worker))            // create a new Thread as context for the client socket
      thread.Start()                                          // start that thread, causing the socket to be intitialized
      starter.WaitOne() |> ignore                              // wait for the `starter` signal to indicate startup is done
    elif disposed then
      failwith "Socket disposed."

  member self.Stop() =
    if started && not disposed then
      run <- false                                             // stop the loop from iterating
      requester.Set() |> ignore                                // but signal requester one more time to exit loop
      stopper.WaitOne() |> ignore                              // wait for the stopper to signal that everything was disposed
      thread.Join()

  member self.Restart() =
    self.Stop()                                               // stop, if not stopped yet
    disposed <- false                                          // disposed reset to default
    run <- true                                                // run reset to default
    self.Start()                                              // start the socket

  member self.Request(req: byte array) : byte array option =  // synchronously request the square of `req`
    if started && not disposed then
      lock lokk  (fun _ ->                                       // lock on the `lokk` object while executing this transaction
        request <- req                                         // first set the requets
        requester.Set() |> ignore                              // then signal a request is ready for execution
        responder.WaitOne() |> ignore                          // wait for signal from the responder that execution has finished
        match exn with                                        // handle exception raised on thread
        | Some e -> raise e                                    // re-raise it on callers thread
        | _      -> Some response)                             // or return the response
    elif disposed then                                        // diposed sockets should have to be re-initialized
      failwith "Socket disposed"
    else None

  interface IDisposable with
    member self.Dispose() = self.Stop()

/// ## execute a request and return response
///
/// execucte a RaftRequest and return response
///
/// ### Signature:
/// - sock: Req socket object
/// - req: the reqest value
///
/// Returns: RaftResponse option
let request (sock: Req) (req: RaftRequest) : RaftResponse option =
  req |> encode |> sock.Request |> Option.bind decode
