namespace Iris.Zmq

// * Imports

open System
open System.Threading
open ZeroMQ
open Iris.Core

// * Pub

/// ## Pub
///
/// Thread-safe Pub socket corresponds to ZSocketType.PUB
///
/// ### Signature:
/// - addr: Address to connect to
///
/// Returns: instance of Pub
type Pub (id: Id, addr: string) =

  let tag = "Pub"

  let mutable starter:   AutoResetEvent = null
  let mutable stopper:   AutoResetEvent = null
  let mutable requester: AutoResetEvent = null
  let mutable responder: AutoResetEvent = null

  let mutable exn: Exception option = None

  let mutable request:  byte array = [| |]

  let mutable thread = null
  let mutable run = true
  let mutable started = false
  let mutable disposed = false
  let mutable sock = null
  let mutable lokk = null
  let mutable ctx = null

  // ** worker

  let worker _ =                                              // thread worker function
    if isNull sock then                                       // if not yet present
      try
        Logger.debug id tag "initializing context and socket"
        ctx <- new ZContext()
        sock <- new ZSocket(ctx, ZSocketType.PUB)                // initialise the socket

        sprintf "connecting to %A" addr
        |> Logger.debug id tag

        sock.Bind(addr)                                         // connect to server
        started <- true
        starter.Set() |> ignore                                  // signal that startup is done
      with
        | ex ->
          run <- false
          exn <- Some ex
          starter.Set() |> ignore

    Logger.debug id tag "entering publish loop"

    while run do
      try
        // wait for the signal that a new request is ready *or* that shutdown is reuqested
        Logger.debug id tag "waiting for a publish"
        requester.WaitOne() |> ignore

        // `run` is usually true, but shutdown first sets this to false to exit the loop
        if run then
          let frame = new ZFrame(request)                     // create a new ZFrame to send
          sock.Send(frame)                                    // and send it via sock

          responder.Set() |> ignore                            // signal that response is ready
          frame.Dispose()                                     // dispose of frame
        else
          responder.Set() |> ignore
      with
        | e ->
          sprintf "exception: %s" e.Message
          |> Logger.err id tag

          // save exception to be rethrown on the callers thread
          exn <- Some e
          // prevent re-entering the loop
          run <- false
          // set the responder so self.Publish does not block indefinietely
          responder.Set() |> ignore

    Logger.debug id tag "exited loop. disposing."

    sock.SetOption(ZSocketOption.LINGER, 0) |> ignore  // set linger to 0 to close socket quickly
    sock.Close()                                      // close the socket
    sock.Dispose()                                    // dispose of it
    ctx.Dispose()
    disposed <- true                                   // this socket is disposed
    started <- false                                   // and not running anymore
    stopper.Set() |> ignore                            // signal that everything was cleaned up now

    Logger.debug id tag "thread-local shutdown done"

  // ** Constructor

  do
    lokk      <- new Object()                       // lock object
    starter   <- new AutoResetEvent(false)          // initialize the signals
    stopper   <- new AutoResetEvent(false)
    requester <- new AutoResetEvent(false)
    responder <- new AutoResetEvent(false)

  // ** Id

  member self.Id
    with get () = id

  // ** Start

  member self.Start() =
    if not started && not disposed then
      Logger.debug id tag "starting socket thread"
      thread <- new Thread(new ThreadStart(worker)) // new Thread as context for the client socket
      thread.Start()                               // start thread & intialize socket
      starter.WaitOne() |> ignore                   // wait for signal to indicate startup is done
    elif disposed then
      Logger.err id tag "already disposed"

  // ** Stop

  member self.Stop() =
    if started && not disposed then
      Logger.debug id tag "stopping stocket thread"
      run <- false                                  // stop the loop from iterating
      requester.Set() |> ignore                     // signal requester one more time to exit loop
      stopper.WaitOne() |> ignore                   // wait for stopper to signal disposed done
      thread.Join()
      Logger.debug id tag "socket shutdown complete"
    else
      Logger.err id tag "refusing to stop. wrong state"

  // ** Restart

  member self.Restart() =
    Logger.debug id tag "restarting socket"
    self.Stop()                                    // stop, if not stopped yet
    disposed <- false                               // disposed reset to default
    run <- true                                     // run reset to default
    self.Start()                                   // start the socket

  // ** Publish

  member self.Publish(req: byte array) : Either<IrisError,unit> =
    if started && not disposed then        // synchronously request the square of `req-`
      Logger.debug id tag "requesting"
      lock lokk <| fun _ ->                 // lock while executing transaction
        request <- req                   // first set the requets
        requester.Set() |> ignore        // then signal a request is ready for execution
        responder.WaitOne() |> ignore    // wait for signal that execution has finished
        match exn with                  // handle exception raised on thread
        | Some e ->                      // re-raise it on callers thread
          Logger.err id tag e.Message
          sprintf "Exception thrown on socket thread: %s" e.Message
          |> Error.asSocketError "Pub.Publish"
          |> Either.fail
        | _  ->
          Logger.debug id tag "request successful"
          Either.succeed ()             // return the response
    elif disposed then                  // disposed sockets need to be re-initialized
      Logger.err id tag "refusing request. already disposed"
      "Socket disposed"
      |> Error.asSocketError "Pub.Publish"
      |> Either.fail
    else
      Logger.err id tag "refusing request. socket has not been started"
      "Socket not started"
      |> Error.asSocketError "Pub.Publish"
      |> Either.fail

  member self.Running
    with get () = started && not disposed

  // ** Dispose

  interface IDisposable with
    member self.Dispose() = self.Stop()
