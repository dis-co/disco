namespace Iris.Zmq

// * Imports

open System
open System.Text
open System.Diagnostics
open System.Threading
open System.Collections.Generic
open System.Collections.Concurrent

open Iris.Core
open ZeroMQ

// * WorkerId

type private WorkerId = uint16

// * RequestCount module

[<RequireQualifiedAccess>]
module private RequestCount =
  let mutable private id = 0L
  let increment () = Interlocked.Increment &id |> ignore
  let current () = id

// * RawServerRequest

type RawServerRequest =
  { RequestId: Guid
    From: Guid
    Body: byte array }

  // ** ToString

  override self.ToString() =
    sprintf "[Request] requestid %O from: %O bytes: %d"
      self.RequestId
      self.From
      (Array.length self.Body)

// * RawServerResponse

type RawServerResponse =
  { RequestId: Guid
    From: Guid
    Body: byte array }

// * RawClientRequest

type RawClientRequest =
  { RequestId: Guid
    Body: byte array }

// * RawClientResponse

type RawClientResponse =
  { RequestId: Guid
    PeerId: Id
    Body: Either<IrisError,byte array> }

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

// * ServerConfig

type ServerConfig =
  { Id: Id
    Listen: Url
    RequestTimeout: Timeout }

// * IServer

type IServer =
  inherit IDisposable
  abstract Subscribe: (RawServerRequest -> unit) -> IDisposable
  abstract Respond: RawServerResponse -> unit

// * RawClientRequest

module RawClientRequest =

  let create (body: byte array) : RawClientRequest =
    { RequestId = Guid.NewGuid()
      Body = body }

// * RawServerRequest

module RawServerRequest =

  let create (from: Guid) (body: byte array) : RawServerRequest =
    { RequestId = Guid.NewGuid()
      From = from
      Body = body }

// * RawServerResponse

module RawServerResponse =

  let fromRequest (request: RawServerRequest) (body: byte array) =
    { RequestId = request.RequestId
      From = request.From
      Body = body }

// * Utils

//  _   _ _   _ _
// | | | | |_(_) |___
// | | | | __| | / __|
// | |_| | |_| | \__ \
//  \___/ \__|_|_|___/

module internal Utils =

  // ** READY

  let internal READY = [| 0uy; 0uy |] // 0us

  // ** Subscriptions

  type internal Subscriptions = ConcurrentDictionary<Guid,IObserver<RawServerRequest>>

  // ** Listener

  type internal Listener = IObservable<RawServerRequest>

  // ** tryDispose

  let internal tryDispose (disp: IDisposable) =
    try dispose disp with | _ -> ()

  // ** createListener

  let internal createListener<'t> (guid: Guid) (subs: ConcurrentDictionary<Guid,IObserver<'t>>) =
    { new IObservable<'t> with
        member self.Subscribe (obs) =
          while not (subs.TryAdd(guid, obs)) do
            Thread.Sleep(TimeSpan.FromTicks 1L)

          { new IDisposable with
              member self.Dispose() =
                match subs.TryRemove(guid) with
                | true, _  -> ()
                | _ -> subs.TryRemove(guid)
                      |> ignore } }

  // ** notify

  let internal notify (subs: Subscriptions) (request: RawServerRequest) =
    for KeyValue(_,sub) in subs do
      sub.OnNext(request)

  // ** shouldRestart

  let internal shouldRestart (no: int) =
    List.contains no [
      ZError.EAGAIN.Number
      ZError.EFSM.Number
    ]

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

  type private Subscriptions = ConcurrentDictionary<Guid,IObserver<RawClientResponse>>

  // ** notify

  let private notify (subscriptions: Subscriptions) (ev: RawClientResponse) =
    for KeyValue(_,subscription) in subscriptions do
      subscription.OnNext ev

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
                |> notify state.Subscriptions // respond to subscriber that this request has failed

        //  ____               _
        // |  _ \ ___  ___ ___(_)_   _____
        // | |_) / _ \/ __/ _ \ \ \ / / _ \
        // |  _ <  __/ (_|  __/ |\ V /  __/
        // |_| \_\___|\___\___|_| \_/ \___|
        let mutable error = ZError.None
        let mutable incoming = Unchecked.defaultof<ZMessage>

        while state.Socket.PollIn(poll, &incoming, &error, timeout) do
          try
            { RequestId = Guid (incoming.[1].Read())
              PeerId = state.Id
              Body = Right (incoming.[2].Read()) }
            |> notify state.Subscriptions
          with
            | exn ->
              exn.Message
              |> Error.asParseError (tag "clientLoop")
              |> fun error -> { RequestId = Guid.Empty; PeerId = state.Id; Body = Left error }
              |> notify state.Subscriptions

        //  ____                _ _
        // |  _ \ ___ _ __   __| (_)_ __   __ _
        // | |_) / _ \ '_ \ / _` | | '_ \ / _` |
        // |  __/  __/ | | | (_| | | | | | (_| |
        // |_|   \___|_| |_|\__,_|_|_| |_|\__, |
        //                                |___/
        let pending = state.Pending.ToArray()
        for KeyValue(reqid, ts) in pending do
          if (state.Stopwatch.ElapsedMilliseconds - ts) > int64 Constants.REQ_TIMEOUT then
            // PENDING REQUEST TIMED OUT
            { RequestId = reqid; PeerId = state.Id; Body = toerror }
            |> notify state.Subscriptions
            state.Pending.TryRemove(reqid) |> ignore // its save, because this collection is only
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

    tryDispose state
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
            let guid = Guid.NewGuid()
            let listener = createListener guid state.Subscriptions
            { new IObserver<RawClientResponse> with
                member self.OnCompleted() = ()
                member self.OnError (error) = ()
                member self.OnNext(value) = callback value }
            |> listener.Subscribe

          member self.Dispose() =
            state.Status <- ServiceStatus.Stopping
            if not (state.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
              Logger.err (tag "Dispose") "timeout: disposing client socket failed"
              thread.Abort()
              dispose state }
      |> Either.succeed

// * Server module

//  ____
// / ___|  ___ _ ____   _____ _ __
// \___ \ / _ \ '__\ \ / / _ \ '__|
//  ___) |  __/ |   \ V /  __/ |
// |____/ \___|_|    \_/ \___|_|

[<RequireQualifiedAccess>]
module Server =
  open Utils

  // ** tag

  let private tag (str: string) =
    String.Format("Server.", id, str)

  // ** LocalThreadState

  [<NoComparison;NoEquality>]
  type private LocalThreadState (ctx: ZContext, args: ServerConfig) as self =

    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Disposed: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Socket: ZSocket
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent
    [<DefaultValue>] val mutable Stopwatch: Stopwatch
    [<DefaultValue>] val mutable Pending: ConcurrentDictionary<Guid,int64>
    [<DefaultValue>] val mutable Responses: ConcurrentQueue<RawServerResponse>

    do
      self.Initialized <- false
      self.Disposed <- false
      self.Started <- false
      self.Run <- true
      self.Subscriptions <- new Subscriptions()
      self.Starter <- new AutoResetEvent(false)
      self.Stopper <- new AutoResetEvent(false)
      self.Stopwatch <- Stopwatch()
      self.Pending <- ConcurrentDictionary()
      self.Responses <- new ConcurrentQueue<RawServerResponse>()

    // *** ToString

    override state.ToString() =
      sprintf "initialized: %b started: %b run: %b"
        state.Initialized
        state.Started
        state.Run

    // *** Timeout

    member self.Timeout
      with get () = args.RequestTimeout

    // *** Start

    member self.Start() =
      try
        let id = args.Id |> string |> Guid.Parse
        self.Socket <- new ZSocket(ctx, ZSocketType.ROUTER)
        self.Socket.Identity <- id.ToByteArray()
        self.Socket.Bind(unwrap args.Listen)
        self.Initialized <- true
        self.Started <- true
        Either.succeed ()
      with
        | exn ->
          dispose self

          self.Initialized <- true
          self.Started <- false
          self.Run <- false

          exn.Message + exn.StackTrace
          |> Error.asSocketError (tag "Start")
          |> Either.fail

    // *** Id

    member self.Id
      with get () = args.Id

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        self.Subscriptions.Clear()
        self.Socket.Close()
        tryDispose self.Socket
        self.Disposed <- true
        Logger.debug (tag "Dispose") "disposed"

  // ** spin

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  // ** timedOut

  let private timedOut (state: LocalThreadState) (timer: Stopwatch) =
    timer.ElapsedMilliseconds > int64 state.Timeout

  // ** serverLoop

  let private serverLoop (state: LocalThreadState) () =
    Logger.debug (tag "serverLoop") "startup done"

    let poll = ZPollItem.CreateReceiver()
    let timeout = Nullable(TimeSpan.FromMilliseconds 1.0)

    let toerror =
      "Timeout on socket"
      |> Error.asSocketError (tag "worker")
      |> Either.fail

    state.Starter.Set() |> ignore

    while spin state do
      try
        //  ____                                 _
        // |  _ \ ___  ___ _ __   ___  _ __   __| |
        // | |_) / _ \/ __| '_ \ / _ \| '_ \ / _` |
        // |  _ <  __/\__ \ |_) | (_) | | | | (_| |
        // |_| \_\___||___/ .__/ \___/|_| |_|\__,_|
        //                |_|
        while state.Responses.Count > 0 && state.Run do
          let result, response = state.Responses.TryDequeue()
          try
            if result then
              use reply = new ZMessage()
              reply.Add(new ZFrame(response.From.ToByteArray()));
              reply.Add(new ZFrame());
              reply.Add(new ZFrame(response.RequestId.ToByteArray()));
              reply.Add(new ZFrame(response.Body));

              let mutable error = ZError.None
              let result = state.Socket.Send(reply, &error)

              if not result || error <> ZError.None then
                String.Format("error sending reply to client: {0}", error)
                |> Logger.err (tag "serverLoop")
          with
            | :? ZException as err when err.ErrNo = ZError.EAGAIN.Number -> ()
            | :? ZException as err when err.ErrNo = ZError.ETERM.Number -> // context was killed
              state.Run <- false
            | exn ->
              exn.Message + exn.StackTrace
              |> Logger.err (tag "serverLoop")

        //  ____               _
        // |  _ \ ___  ___ ___(_)_   _____
        // | |_) / _ \/ __/ _ \ \ \ / / _ \
        // |  _ <  __/ (_|  __/ |\ V /  __/
        // |_| \_\___|\___\___|_| \_/ \___|
        let mutable error = ZError.None
        use mutable incoming = new ZMessage()
        while state.Socket.PollIn(poll, &incoming, &error, timeout) && state.Run do
          try
            let request: RawServerRequest =
              { From = Guid (incoming.[1].Read())
                RequestId = Guid (incoming.[3].Read())
                Body = incoming.[4].Read() }
            notify state.Subscriptions request
          with
            | exn ->
              exn.Message + exn.StackTrace
              |> Logger.err (tag "serverLoop")

      with
      | :? ZException as err when err.ErrNo = ZError.EAGAIN.Number -> ()
      | :? ZException as err when err.ErrNo = ZError.ETERM.Number -> // context was killed
        "shutting down"
        |> Logger.err (tag "serverLoop")
        state.Run <- false
      | other ->                           // something else happened
        other
        |> sprintf "error in worker: %O"
        |> Logger.err (tag "serverLoop")
        state.Run <- false

    dispose state
    state.Stopper.Set() |> ignore

  // ** create

  let create ctx (args: ServerConfig)  = either {
      let state = new LocalThreadState(ctx, args)
      do! state.Start()

      let thread = Thread(serverLoop state)
      thread.Name <- sprintf "server-%O" args.Id
      thread.Start()

      if state.Starter.WaitOne(TimeSpan.FromMilliseconds 1000.0) then
        return
          { new IServer with
              member server.Respond(response: RawServerResponse) =
                state.Responses.Enqueue(response)

              member server.Subscribe(callback: RawServerRequest -> unit) =
                let guid = Guid.NewGuid()
                let listener = createListener guid state.Subscriptions
                { new IObserver<RawServerRequest> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member server.Dispose() =
                if not state.Disposed then
                  state.Run <- false
                  state.Disposed <- true
                  if not (state.Stopper.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
                    Logger.err (tag "Dispose") "Dispose timed out"
            }
      else
        thread.Abort()
        dispose state
        let msg = "timeout: starting worker failed"
        Logger.err (tag "create") msg
        return! msg |> Error.asSocketError (tag "create") |> Either.fail
    }
