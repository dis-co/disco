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
    Via: WorkerId
    Body: byte array }

  // ** ToString

  override self.ToString() =
    sprintf "[Request] from %O via: %O bytes: %d"
      self.From
      self.Via
      (Array.length self.Body)

// * RawServerResponse

type RawServerResponse =
  { RequestId: Guid
    Via: WorkerId
    Body: byte array }

// * RawClientRequest

type RawClientRequest =
  { RequestId: Guid
    Body: byte array }

// * RawClientResponse

type RawClientResponse =
  { PeerId: Id
    RequestId: Guid
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

// * WorkerConfig

[<NoComparison>]
type WorkerConfig =
  { Id: WorkerId
    Backend: Url
    Context: ZContext
    RequestTimeout: Timeout }

// * IWorker

type private IWorker =
  abstract Id: WorkerId
  abstract Respond: RawServerResponse -> unit
  abstract Subscribe: (RawServerRequest -> unit) -> IDisposable

// * BrokerConfig

type BrokerConfig =
  { Id: Id
    MinWorkers: uint8
    MaxWorkers: uint8
    Frontend: Url
    Backend: Url
    RequestTimeout: Timeout }

// * IBroker

type IBroker =
  inherit IDisposable
  abstract Subscribe: (RawServerRequest -> unit) -> IDisposable
  abstract Respond: RawServerResponse -> unit

// * RawServerResponse

module RawServerResponse =

  let fromRequest (request: RawServerRequest) (body: byte array) =
    { RequestId = request.RequestId
      Via = request.Via
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
                msg.Add(new ZFrame(request.RequestId.ToByteArray())) // add the request id
                msg.Add(new ZFrame(request.Body))                    // add the request body
                state.Socket.Send(msg, ZSocketFlags.DontWait)        // send that shit
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
            { RequestId = Guid (incoming.[0].Read())
              PeerId = state.Id
              Body = Right (incoming.[1].Read()) }
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

// * Worker module

// __        __         _
// \ \      / /__  _ __| | _____ _ __
//  \ \ /\ / / _ \| '__| |/ / _ \ '__|
//   \ V  V / (_) | |  |   <  __/ |
//    \_/\_/ \___/|_|  |_|\_\___|_|

[<RequireQualifiedAccess>]
module private Worker =
  open Utils

  // ** tag

  let private tag (id: WorkerId) (str: string) =
    String.Format("Worker-{0}.{1}", id, str)

  // ** LocalThreadState

  [<NoComparison;NoEquality>]
  type private LocalThreadState (args: WorkerConfig) as self =

    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Disposed: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Socket: ZSocket
    [<DefaultValue>] val mutable Error: Either<IrisError,unit>
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent
    [<DefaultValue>] val mutable Responder: ConcurrentQueue<RawServerResponse>

    do
      self.Initialized <- false
      self.Disposed <- false
      self.Started <- false
      self.Run <- true
      self.Error <- Right ()
      self.Subscriptions <- new Subscriptions()
      self.Starter <- new AutoResetEvent(false)
      self.Stopper <- new AutoResetEvent(false)
      self.Responder <- new ConcurrentQueue<RawServerResponse>()

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
        self.Socket <- new ZSocket(args.Context, ZSocketType.DEALER)
        self.Socket.Connect(unwrap args.Backend)
        self.Initialized <- true
        self.Started <- true
        self.Starter.Set() |> ignore
      with
        | exn ->
          dispose self

          exn.Message + exn.StackTrace
          |> Error.asSocketError (tag args.Id "Start")
          |> Either.fail
          |> fun error -> self.Error <- error

          self.Initialized <- true
          self.Started <- false
          self.Run <- false

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
        Logger.debug (tag args.Id "worker") "disposed"

  // ** spin

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  // ** timedOut

  let private timedOut (state: LocalThreadState) (timer: Stopwatch) =
    timer.ElapsedMilliseconds > int64 state.Timeout

  // ** workerLoop

  let private workerLoop (state: LocalThreadState) () =
    state.Start()

    Logger.debug (tag state.Id "initialize") "startup done"

    while spin state do
      use mutable request = Unchecked.defaultof<ZMessage>
      let mutable error = Unchecked.defaultof<ZError>

      if state.Socket.ReceiveMessage(&request, &error) then
        Tracing.trace (tag state.Id "request") <| fun () ->
          let clientId = request.[1].Read()
          let reqid = request.[2].Read()
          let body = request.[3].Read()

          // blow out the request to the backend process
          { RequestId = Guid reqid
            From = Guid clientId
            Via = state.Id
            Body = body }
          |> notify state.Subscriptions

          let timer = Stopwatch()
          timer.Start()

          let mutable response = Unchecked.defaultof<RawServerResponse>

          while not (state.Responder.TryDequeue(&response)) && not (timedOut state timer) && state.Run do
            Thread.Sleep(TimeSpan.FromTicks 1L)

          timer.Stop()

          if not (timedOut state timer) && state.Run then // backend did not time out
            use reply = new ZMessage()
            reply.Add(new ZFrame(clientId));
            reply.Add(new ZFrame(reqid));
            reply.Add(new ZFrame(response.Body));

            try
              state.Socket.Send(reply)
            with
              | :? ZException as exn ->
                String.Format("error sending reply to proxy: {0}", exn.Message)
                |> Logger.err (tag state.Id "worker")
              | exn ->
                String.Format("error during send: {0}", exn.Message)
                |> Logger.err (tag state.Id "worker")
                state.Run <- false
          elif state.Run then           // backend timed out, but we are still running
            timer.ElapsedMilliseconds   // by re-registering it
            |> sprintf "Backend Response Timeout: %d"
            |> Logger.err (tag state.Id "worker")
          else
            state.Run
            |> sprintf "Stop requested: %b"
            |> Logger.err (tag state.Id "worker")
      else
        match error with
        | err when err = ZError.EAGAIN -> () // don't do anything if we just didn't receive
        | err when err = ZError.ETERM ->     // context was killed, so we exit
          "shutting down"
          |> Logger.err (tag state.Id "worker")
          state.Run <- false
        | other ->                           // something else happened
          other
          |> sprintf "error in worker: %O"
          |> Logger.err (tag state.Id "worker")
          state.Run <- false

    dispose state
    state.Stopper.Set() |> ignore

  // ** create

  let create (args: WorkerConfig)  =
    let state = new LocalThreadState(args)

    let thread = Thread(workerLoop state)
    thread.Name <- sprintf "broker-worker-%d" state.Id
    thread.Start()

    if state.Starter.WaitOne(TimeSpan.FromMilliseconds 1000.0) then
      Either.map
        (fun _ ->
          { new IWorker with
              member worker.Id
                with get () = state.Id

              member worker.Respond(response: RawServerResponse) =
                state.Responder.Enqueue(response)

              member worker.Subscribe(callback: RawServerRequest -> unit) =
                let guid = Guid.NewGuid()
                let listener = createListener guid state.Subscriptions
                { new IObserver<RawServerRequest> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe })
        state.Error
    else
      thread.Abort()
      dispose state
      let msg = "timeout: starting worker failed"
      Logger.err (tag state.Id "create") msg
      msg |> Error.asSocketError (tag state.Id "create") |> Either.fail

// * Broker module

//  ____            _
// | __ ) _ __ ___ | | _____ _ __
// |  _ \| '__/ _ \| |/ / _ \ '__|
// | |_) | | | (_) |   <  __/ |
// |____/|_|  \___/|_|\_\___|_|

[<RequireQualifiedAccess>]
module Broker =
  open Utils

  // ** Workers

  type private Workers = ConcurrentDictionary<WorkerId,IWorker>

  // ** ResponseActor

  type private ResponseActor = MailboxProcessor<RawServerResponse>

  let private tag (str: string) =
    String.Format("Broker.{0}", str)

  // ** loop

  let private loop (workers: Workers) (agent: ResponseActor) =
    let rec impl () =
      async {
        let! response = agent.Receive()
        match workers.TryGetValue response.Via with
        | true, worker ->
          try
            Tracing.trace (tag "ResponseActor") <| fun _ ->
              worker.Respond response     // try to respond
          with
            | exn ->
              agent.Post response       // re-cue the response
        | _ ->
          sprintf "no worker found for %O" response.Via
          |> Logger.err (tag "ResponseActor")
        return! impl ()
      }
    impl ()

  // ** LocalThreadState

  [<NoComparison;NoEquality>]
  type private LocalThreadState private (args: BrokerConfig, ctx: ZContext) as self =

    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Disposed: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Frontend: ZSocket
    [<DefaultValue>] val mutable Backend: ZSocket
    [<DefaultValue>] val mutable Workers: Workers
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent

    let disposables = new ResizeArray<IDisposable>()

    do
      self.Initialized <- false
      self.Started <- false
      self.Run <- true
      self.Disposed <- false
      self.Starter <- new AutoResetEvent(false)
      self.Stopper <- new AutoResetEvent(false)
      self.Workers <- new Workers()
      self.Subscriptions <- new Subscriptions()

    // *** Init

    static member Create(args, ctx) =
      either {
        let state = new LocalThreadState(args,ctx)

        let frontend = new ZSocket(ctx, ZSocketType.ROUTER)
        let backend  = new ZSocket(ctx, ZSocketType.DEALER)

        let mutable error = ZError.None

        let fresult = frontend.Bind(unwrap args.Frontend, &error)
        let bresult = backend.Bind(unwrap args.Backend, &error)

        if fresult && bresult then
          state.Frontend <- frontend
          state.Backend <- backend
          return state
        else
          frontend.Close()
          backend.Close()
          return!
            "failed to initialize sockets"
            |> Error.asSocketError (tag "Create")
            |> Either.fail
      }

    // *** Start

    member self.Start () =
      for _ in 1uy .. args.MinWorkers do
        self.AddWorker()

      self.Initialized <- true
      self.Started <- true

    // *** AddWorker

    member self.AddWorker() =
      let count = self.Workers.Count
      if count < int args.MaxWorkers then
        Logger.debug (tag "AddWorker") "creating new worker"

        let result = Worker.create {
            Id = uint16 count + 1us
            Backend = args.Backend
            Context = ctx
            RequestTimeout = args.RequestTimeout
          }

        match result with
        | Right worker ->
          notify self.Subscriptions
          |> worker.Subscribe
          |> disposables.Add

          while not (self.Workers.TryAdd(worker.Id, worker)) do
            Thread.Sleep(TimeSpan.FromTicks 1L)

          Logger.debug (tag "AddWorker") "successfully added new worker"

        | Left error ->
          error
          |> string
          |> Logger.err (tag "AddWorker")

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        for disp in disposables do
          tryDispose disp
        self.Frontend.Linger <- TimeSpan.FromMilliseconds 0.0
        self.Backend.Linger <- TimeSpan.FromMilliseconds 0.0
        self.Frontend.Close()
        self.Backend.Close()
        tryDispose self.Frontend
        tryDispose self.Backend
        self.Disposed <- true

  // ** proxy

  let private proxy (state: LocalThreadState) () =
    state.Start()
    state.Starter.Set() |> ignore
    try
      Logger.debug (tag "proxy") "starting ZMQ proxy"
      let mutable error = ZError.None
      if not (ZContext.Proxy(state.Frontend, state.Backend, &error)) then
        Logger.err (tag "proxy") (string error)
    with
      | exn -> Logger.err (tag "proxy") exn.Message
    state.Stopper.Set() |> ignore
    Logger.err (tag "proxy") "exiting"

  // ** create

  let create (ctx: ZContext) (args: BrokerConfig) = either {
      let! state = LocalThreadState.Create(args, ctx)

      let cts = new CancellationTokenSource()
      let responder = ResponseActor.Start(loop state.Workers, cts.Token)

      let proxy = Thread(proxy state)
      proxy.Name <- sprintf "BrokerProxy"
      proxy.Start()

      if state.Starter.WaitOne(TimeSpan.FromMilliseconds 1000.0) then
        return
          { new IBroker with
              member self.Subscribe(callback: RawServerRequest -> unit) =
                let guid = Guid.NewGuid()
                let listener = createListener guid state.Subscriptions
                { new IObserver<RawServerRequest> with
                    member self.OnCompleted() = ()
                    member self.OnError (error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Respond (response: RawServerResponse) =
                responder.Post response

              member self.Dispose() =
                dispose cts
                dispose state }
      else
        proxy.Abort()
        dispose state
        let msg = "timeout: failed to start broker"
        Logger.err (tag "create") msg
        return! msg |> Error.asSocketError (tag "create") |> Either.fail
    }
