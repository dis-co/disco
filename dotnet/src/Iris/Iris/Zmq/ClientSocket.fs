namespace rec Iris.Zmq

// * Imports

open System
open System.Text
open System.Diagnostics
open System.Threading
open System.Collections.Generic
open System.Collections.Concurrent

open Iris.Core
open ZeroMQ

// * IClient

type IClient =
  inherit IDisposable
  abstract PeerId: Id
  abstract Request: RawClientRequest -> Either<IrisError,unit>
  abstract Running: bool
  abstract Subscribe: (RawClientResponse -> unit) -> IDisposable

// * ClientConfig

type ClientConfig =
  { PeerId: Id
    Frontend: Url
    Timeout: Timeout }

// * RawClientRequest

type RawClientRequest =
  { RequestId: Guid
    Body: byte array }

// * RawClientResponse

type RawClientResponse =
  { RequestId: Guid
    PeerId: Id
    Body: Either<IrisError,byte array> }

// * RawClientRequest module

module RawClientRequest =

  let create (body: byte array) : RawClientRequest =
    { RequestId = Guid.NewGuid()
      Body = body }

// * Client module

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

[<RequireQualifiedAccess>]
module Client =
  open Utils

  // ** tag

  let private tag (str: string) = String.Format("Client.{0}",str)

  // ** rand

  let private rand = new System.Random()

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<RawClientResponse>

  // ** LocalThreadState

  type private LocalThreadState(options: ClientConfig, context: ZContext) as self =
    let timeout = options.Timeout |> float |> TimeSpan.FromMilliseconds
    let clid = options.PeerId |> string |> Guid.Parse

    [<DefaultValue>] val mutable Status: ServiceStatus
    [<DefaultValue>] val mutable Socket: ZSocket
    [<DefaultValue>] val mutable Requests: ConcurrentQueue<RawClientRequest>
    [<DefaultValue>] val mutable Pending: ConcurrentDictionary<Guid,int64>
    [<DefaultValue>] val mutable Stopwatch: Stopwatch
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent

    // *** do

    do
      self.Status <- ServiceStatus.Stopped
      self.Requests <- ConcurrentQueue()
      self.Pending <- ConcurrentDictionary()
      self.Stopwatch <- Stopwatch()
      self.Subscriptions <- Subscriptions()
      self.Starter <- new AutoResetEvent(false)
      self.Stopper <- new AutoResetEvent(false)

    // *** Id

    member self.Id
      with get () = options.PeerId

    // *** Timeout

    member self.Timeout with get () = Nullable(timeout)

    // *** Start

    member self.Start() =
      try
        self.Socket <- new ZSocket(context, ZSocketType.DEALER)
        self.Socket.Identity <- clid.ToByteArray()
        self.Socket.Connect(unwrap options.Frontend)
        self.Status <- ServiceStatus.Running
        self.Stopwatch.Start()
      with
        | exn ->
          dispose self
          self.Status <- ServiceStatus.Disposed
          exn.Message + exn.StackTrace
          |> sprintf "Exception creating socket for %O: %s" options.PeerId
          |> Logger.err "IClient"

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        if not (Service.isDisposed self.Status) then
          Logger.debug (tag "Dispose") "disposing client socket"
          self.Subscriptions.Clear()
          try
            self.Socket.Linger <- TimeSpan.FromMilliseconds 0.0
            self.Socket.Close()
          with | exn -> Logger.err (tag "Dispose") exn.Message
          self.Status <- ServiceStatus.Disposed
          Logger.debug (tag "Dispose") "client socket disposed"

  // ** started

  let private started (state: LocalThreadState) =
    state.Status = ServiceStatus.Running

  // ** spin

  let private spin (state: LocalThreadState) =
    state.Status = ServiceStatus.Running

  // ** clientLoop

  let private clientLoop (state: LocalThreadState) () =
    state.Start()

    let poll = ZPollItem.CreateReceiver()
    let timeout = Nullable(TimeSpan.FromMilliseconds 1.0)

    let toerror =
      "Timeout on socket"
      |> Error.asSocketError (tag "worker")
      |> Either.fail

    state.Starter.Set() |> ignore

    try
      while spin state do
        //  ____                 _
        // / ___|  ___ _ __   __| |
        // \___ \ / _ \ '_ \ / _` |
        //  ___) |  __/ | | | (_| |
        // |____/ \___|_| |_|\__,_|
        while state.Requests.Count > 0 do
          let result, request = state.Requests.TryDequeue()
          if result then
            try
              use msg = new ZMessage()
              msg.Add(new ZFrame(state.Socket.Identity))           // add this sockets identit
              msg.Add(new ZFrame())                                // add empty frame
              msg.Add(new ZFrame(request.RequestId.ToByteArray())) // add the request id
              msg.Add(new ZFrame(request.Body))                    // add the request body
              state.Socket.Send(msg)                               // send that shit
              state.Pending.TryAdd(request.RequestId, state.Stopwatch.ElapsedMilliseconds)
              |> function
                | true  -> ()
                | false -> Logger.err (tag "clientLoop") "could not add request to pending queue"
            with
              | exn ->
                exn.Message
                |> sprintf "error sending request: %s"
                |> Error.asSocketError (tag "clientLoop")
                |> Either.fail
                |> fun body -> { RequestId = request.RequestId; PeerId = state.Id; Body = body }
                |> Observable.onNext state.Subscriptions // respond to subscriber that this request
                                                         // has failed

        //  ____               _
        // |  _ \ ___  ___ ___(_)_   _____
        // | |_) / _ \/ __/ _ \ \ \ / / _ \
        // |  _ <  __/ (_|  __/ |\ V /  __/
        // |_| \_\___|\___\___|_| \_/ \___|
        let mutable error = ZError.None
        let mutable incoming = Unchecked.defaultof<ZMessage>

        while state.Socket.PollIn(poll, &incoming, &error, timeout) do
          try
            let reqid = Guid (incoming.[1].Read())
            { RequestId = reqid
              PeerId = state.Id
              Body = Right (incoming.[2].Read()) }
            |> Observable.onNext state.Subscriptions
            state.Pending.TryRemove(reqid) |> ignore
          with
            | exn ->
              exn.Message
              |> Error.asParseError (tag "clientLoop")
              |> fun error -> { RequestId = Guid.Empty; PeerId = state.Id; Body = Left error }
              |> Observable.onNext state.Subscriptions

        //  ____                _ _
        // |  _ \ ___ _ __   __| (_)_ __   __ _
        // | |_) / _ \ '_ \ / _` | | '_ \ / _` |
        // |  __/  __/ | | | (_| | | | | | (_| |
        // |_|   \___|_| |_|\__,_|_|_| |_|\__, |
        //                                |___/
        let pending = state.Pending.ToArray()
        for KeyValue(reqid, reqtime) in pending do
          let elapsed = state.Stopwatch.ElapsedMilliseconds - reqtime
          let timedout = elapsed > int64 Constants.REQ_TIMEOUT
          if timedout then
            // PENDING REQUEST TIMED OUT
            { RequestId = reqid; PeerId = state.Id; Body = toerror }
            |> Observable.onNext state.Subscriptions
            state.Pending.TryRemove(reqid) |> ignore // its safe, because this collection is only
                                                    // ever modified in one place

    with
    | :? ZException as exn when exn.ErrNo = ZError.ETERM.Number ->
      "Encountered ETERM on thread. Disposing"
      |> Logger.err (tag "clientLoop")
      state.Status <- ServiceStatus.Stopping
    | :? ZException -> ()
    | exn ->
      let msg = String.Format("Exception: {0}\nStackTrace:{1}",exn.Message, exn.StackTrace)
      let error = Error.asSocketError (tag "clientLoop") msg
      Logger.err (tag "clientLoop") msg
      state.Status <- ServiceStatus.Failed error

    tryDispose state ignore
    state.Stopper.Set() |> ignore

  // ** create

  let create (ctx: ZContext) (options: ClientConfig) =
    let state = new LocalThreadState(options, ctx)

    let mutable thread = Thread(clientLoop state)
    thread.Name <- sprintf "Client %O" state.Id
    thread.Start()

    if not (state.Starter.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
      let msg = "timeout: starting client failed"
      Logger.debug (tag "create") msg
      thread.Abort()
      dispose state
      msg |> Error.asSocketError (tag "create") |> Either.fail
    else
      { new IClient with
          member self.PeerId
            with get () = state.Id

          member self.Request(request: RawClientRequest) =
            if state.Status <> ServiceStatus.Disposed then
              request
              |> state.Requests.Enqueue
              |> Either.succeed
            else
              "Socket already disposed"
              |> Error.asSocketError (tag "create")
              |> Either.fail

          member self.Running
            with get () =
              state.Status = ServiceStatus.Running

          member self.Subscribe (callback: RawClientResponse -> unit) =
            Observable.subscribe callback state.Subscriptions

          member self.Dispose() =
            state.Status <- ServiceStatus.Stopping
            if not (state.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
              Logger.err (tag "Dispose") "timeout: disposing client socket failed"
              thread.Abort()
              dispose state }
      |> Either.succeed
