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
  { From: Guid
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
  { Via: WorkerId
    Body: byte array }

// * RawClientRequest

type RawClientRequest =
  { Body: byte array }

// * RawClientResponse

type RawClientResponse =
  { PeerId: Id
    Body: Either<IrisError,byte array> }

// * IClient

type IClient =
  inherit IDisposable
  abstract PeerId: Id
  abstract Request: RawClientRequest -> Either<IrisError,unit>
  abstract Running: bool
  abstract Restart: unit -> unit
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
  inherit IDisposable
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
    { Via = request.Via; Body = body }

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

  type private LocalThreadState(options: ClientConfig) as self =
    let timeout = options.Timeout |> float |> TimeSpan.FromMilliseconds
    let clid = options.PeerId |> string |> Guid.Parse

    [<DefaultValue>] val mutable Status: ServiceStatus
    [<DefaultValue>] val mutable Socket: ZSocket
    [<DefaultValue>] val mutable Context: ZContext
    [<DefaultValue>] val mutable Requests: ConcurrentQueue<RawClientRequest>
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent

    // *** do

    do
      self.Status <- ServiceStatus.Stopped
      self.Requests <- ConcurrentQueue()
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
        self.Context <- new ZContext()
        self.StartSocket()
        self.Status <- ServiceStatus.Running
      with
        | exn ->
          dispose self
          self.Status <- ServiceStatus.Disposed
          exn.Message + exn.StackTrace
          |> sprintf "Exception creating socket for %O: %s" options.PeerId
          |> Logger.err "IClient"

    // *** StartSocket

    member self.StartSocket() =
      self.Socket <- new ZSocket(self.Context, ZSocketType.DEALER)
      self.Socket.Linger <- TimeSpan.FromMilliseconds 1.0
      self.Socket.Identity <- clid.ToByteArray()
      self.Socket.Connect(unwrap options.Frontend)
      self.Status <- ServiceStatus.Running

    // *** RestartSocket

    member self.RestartSocket() =
      if self.Status <> ServiceStatus.Disposed then
        sprintf "restarting socket for %O" options.PeerId
        |> Logger.debug "Client"
        dispose self.Socket
        self.StartSocket()
      else
        "Cannot restart client socket, already disposed"
        |> Logger.err "Client"

    // *** Reset

    member self.Reset() =
      if not (Service.isDisposed self.Status) then
        dispose self

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        if not (Service.isDisposed self.Status) then
          tryDispose self.Socket
          tryDispose self.Context
          self.Status <- ServiceStatus.Disposed

  // ** initialize

  let private initialize (state: LocalThreadState) =
    if state.Status = ServiceStatus.Stopped then
      state.Start()
      state.Starter.Set() |> ignore

  // ** started

  let private started (state: LocalThreadState) =
    state.Status = ServiceStatus.Running

  // ** spin

  let private spin (state: LocalThreadState) =
    state.Status = ServiceStatus.Running

  // ** worker

  let private worker (state: LocalThreadState) () =
    initialize state

    let poll = ZPollItem.CreateReceiver()
    let timeout = Nullable(TimeSpan.FromMilliseconds 1.0)
    let mutable error = ZError.None
    let mutable incoming = Unchecked.defaultof<ZMessage>

    state.Starter.Set() |> ignore

    while spin state do

      if state.Requests.Count > 0 then
        while state.Requests.Count > 0 do
          try
            // SENDING
            let result, request = state.Requests.TryDequeue()
            if result then
              use msg = new ZMessage()
              msg.Add(new ZFrame(state.Socket.Identity))
              msg.Add(new ZFrame(request.Body))
              state.Socket.Send(msg)

            // RECEIVING
            if state.Socket.PollIn(poll, &incoming, &error, state.Timeout) then
              incoming.[1].Read()
              |> Either.succeed
              |> fun body -> { PeerId = state.Id; Body = body }
              |> notify state.Subscriptions
            else // TIMEOUT
              Error.asSocketError "Client" "Timeout on socket"
              |> Either.fail
              |> fun body -> { PeerId = state.Id; Body = body }
              |> notify state.Subscriptions

          with
          | :? ZException as exn when exn.ErrNo = ZError.ETERM.Number ->
            "Encountered ETERM on thread. disposing"
            |> Logger.err "Client"
            state.Status <- ServiceStatus.Disposed
          | :? ZException -> ()
          | exn ->
            exn.Message + exn.StackTrace
            |> Logger.err "Client"
            state.Status <- ServiceStatus.Disposed
      else
        // RECEIVING
        if state.Socket.PollIn(poll, &incoming, &error, timeout) then
          incoming.[1].Read()
          |> Either.succeed
          |> fun body -> { PeerId = state.Id; Body = body }
          |> notify state.Subscriptions

    tryDispose state
    state.Stopper.Set() |> ignore

  // ** create

  let create (options: ClientConfig) =
    let state = new LocalThreadState(options)

    let mutable thread = Thread(worker state)
    thread.Name <- sprintf "Client %O" state.Id
    thread.Start()

    state.Starter.WaitOne() |> ignore

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
            |> Error.asSocketError "Client"
            |> Either.fail

        member self.Running
          with get () =
            state.Status = ServiceStatus.Running

        member self.Restart () =
          self.Dispose()
          state.Reset()
          state.Status <- ServiceStatus.Stopped
          thread <- Thread(worker state)
          thread.Name <- sprintf "Client %O" state.Id
          thread.Start()
          state.Starter.WaitOne() |> ignore

        member self.Subscribe (callback: RawClientResponse -> unit) =
          let guid = Guid.NewGuid()
          let listener = createListener guid state.Subscriptions
          { new IObserver<RawClientResponse> with
              member self.OnCompleted() = ()
              member self.OnError (error) = ()
              member self.OnNext(value) = callback value }
          |> listener.Subscribe

        member self.Dispose() =
          lock state <| fun _ ->
            state.Status <- ServiceStatus.Stopping
          state.Stopper.WaitOne() |> ignore }

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
    [<DefaultValue>] val mutable Responder: ConcurrentStack<RawServerResponse>

    do
      self.Initialized <- false
      self.Disposed <- false
      self.Started <- false
      self.Run <- true
      self.Error <- Right ()
      self.Subscriptions <- new Subscriptions()
      self.Starter <- new AutoResetEvent(false)
      self.Stopper <- new AutoResetEvent(false)
      self.Responder <- new ConcurrentStack<RawServerResponse>()

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
        self.StartSocket()
        self.Register()

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

    // *** StartSocket

    member self.StartSocket () =
      self.Socket <- new ZSocket(args.Context, ZSocketType.REQ)
      self.Socket.Identity <- BitConverter.GetBytes args.Id
      self.Socket.ReceiveTimeout <- TimeSpan.FromMilliseconds 10.0
      self.Socket.Linger <- TimeSpan.FromMilliseconds 1.0
      self.Socket.Connect(unwrap args.Backend)

    // *** RestartSocket

    member self.RestartSocket() =
      Logger.debug (tag args.Id "RestartSocket") "restarting socket"
      dispose self.Socket
      self.StartSocket()

    // *** Register

    member self.Register() =
      use hello = new ZFrame(READY)
      self.Socket.Send(hello)

    // *** Id

    member self.Id
      with get () = args.Id

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        tryDispose self.Socket
        self.Disposed <- true

  // ** spin

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  // ** timedOut

  let private timedOut (state: LocalThreadState) (timer: Stopwatch) =
    timer.ElapsedMilliseconds > int64 state.Timeout

  // ** worker

  let private worker (state: LocalThreadState) () =
    state.Start()

    Logger.debug (tag state.Id "initialize") "startup done"

    while spin state do
      use mutable request = Unchecked.defaultof<ZMessage>
      let mutable error = Unchecked.defaultof<ZError>

      if state.Socket.ReceiveMessage(&request, &error) then
        Tracing.trace (tag state.Id "request") <| fun () ->
          let clientId = request.[0].Read()
          let body = request.[2].Read()

          { From = Guid clientId
            Via = state.Id
            Body = body }
          |> notify state.Subscriptions

          let timer = new Stopwatch()
          timer.Start()

          let mutable response = Unchecked.defaultof<RawServerResponse>

          while not (state.Responder.TryPop(&response)) && not (timedOut state timer) && state.Run do
            Thread.Sleep(TimeSpan.FromTicks 1L)

          timer.Stop()

          if not (timedOut state timer) && state.Run then // backend did not time out
            use reply = new ZMessage()
            reply.Add(new ZFrame(clientId));
            reply.Add(new ZFrame());
            reply.Add(new ZFrame(response.Via));
            reply.Add(new ZFrame(response.Body));

            try
              state.Socket.Send(reply)
            with
              | :? ZException as exn when shouldRestart exn.ErrNo ->
                Logger.err (tag state.Id "worker") "sending reply error"
                state.RestartSocket()
                state.Register()
              | exn ->
                exn.Message
                |> sprintf "error during send: %O"
                |> Logger.err (tag state.Id "worker")
                state.Run <- false
          elif state.Run then           // backend timed out, but we are still running
            state.Register()            // put the socket back in business
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
        | err when err = ZError.EFSM ->      // restart the socket if in the wrong state
          Logger.err (tag state.Id "worker") "worker got EFSM"
          state.RestartSocket()
          state.Register()
        | err when err = ZError.ETERM ->     // context was killed, so we exit
          "context was terminated, shutting down"
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

    let thread = Thread(worker state)
    thread.Name <- sprintf "broker-worker-%d" state.Id
    thread.Start()

    state.Starter.WaitOne() |> ignore

    Either.map
      (fun _ ->
        { new IWorker with
            member worker.Id
              with get () = state.Id

            member worker.Respond(response: RawServerResponse) =
              state.Responder.Push(response)

            member worker.Subscribe(callback: RawServerRequest -> unit) =
              let guid = Guid.NewGuid()
              let listener = createListener guid state.Subscriptions
              { new IObserver<RawServerRequest> with
                  member self.OnCompleted() = ()
                  member self.OnError(error) = ()
                  member self.OnNext(value) = callback value }
              |> listener.Subscribe

            member worker.Dispose() =
              state.Run <- false
              state.Stopper.WaitOne() |> ignore
              dispose state.Stopper
              thread.Join() })
      state.Error

// * Broker module

//  ____            _
// | __ ) _ __ ___ | | _____ _ __
// |  _ \| '__/ _ \| |/ / _ \ '__|
// | |_) | | | (_) |   <  __/ |
// |____/|_|  \___/|_|\_\___|_|

[<RequireQualifiedAccess>]
module Broker =
  open Utils

  // ** tag

  let private tag (str: string) =
    String.Format("Broker.{0}", str)

  // ** Workers

  type private Workers = ConcurrentDictionary<WorkerId,IWorker>

  // ** ResponseActor

  type private ResponseActor = MailboxProcessor<RawServerResponse>

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
  type private LocalThreadState (args: BrokerConfig) as self =

    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Disposed: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Error: Either<IrisError,unit>
    [<DefaultValue>] val mutable Context: ZContext
    [<DefaultValue>] val mutable Frontend: ZSocket
    [<DefaultValue>] val mutable Backend: ZSocket
    [<DefaultValue>] val mutable Workers: Workers
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent

    let disposables = new ResizeArray<IDisposable>()

    // *** do

    do
      self.Initialized <- false
      self.Started <- false
      self.Run <- true
      self.Disposed <- false
      self.Error <- Right ()
      self.Starter <- new AutoResetEvent(false)
      self.Stopper <- new AutoResetEvent(false)
      self.Workers <- new Workers()
      self.Subscriptions <- new Subscriptions()

    // *** Start

    member self.Start () =
      try
        self.Context <- new ZContext()
        self.Frontend <- new ZSocket(self.Context, ZSocketType.ROUTER)
        self.Backend <- new ZSocket(self.Context, ZSocketType.ROUTER)
        self.Frontend.Bind(unwrap args.Frontend)
        self.Backend.Bind(unwrap args.Backend)

        for _ in 1uy .. args.MinWorkers do
          self.AddWorker()

        self.Initialized <- true
        self.Started <- true
      with
        | exn ->
          exn.Message + exn.StackTrace
          |> Error.asSocketError (tag "Start")
          |> Either.fail
          |> fun error -> self.Error <- error
          dispose self

    // *** AddWorker

    member self.AddWorker() =
      let count = self.Workers.Count
      if count < int args.MaxWorkers then
        Logger.debug (tag "AddWorker") "creating new worker"

        let result = Worker.create {
            Id = uint16 count + 1us
            Backend = args.Backend
            Context = self.Context
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
        lock self <| fun _ ->
          for disp in disposables do
            tryDispose disp

          tryDispose self.Frontend
          tryDispose self.Backend

          for KeyValue(_, worker) in self.Workers do
            tryDispose worker

          tryDispose self.Context
          self.Disposed <- true

  // ** spin

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  // ** worker

  let private worker (state: LocalThreadState) () =
    state.Start()
    state.Starter.Set() |> ignore

    Logger.debug (tag "initialize") "startup done"

    let available = new ResizeArray<WorkerId>()
    let mutable incoming = Unchecked.defaultof<ZMessage>
    let mutable error = Unchecked.defaultof<ZError>
    let poll = ZPollItem.CreateReceiver()
    let timespan = Nullable(TimeSpan.FromMilliseconds(1.0))

    while spin state do

      if state.Backend.PollIn(poll, &incoming, &error, timespan) then
        let workerId = incoming.[0].ReadUInt16()
        let clientId = incoming.[2].Read()

        available.Add(workerId)

        if clientId <> READY then
          let from = incoming.[4].Read()
          let reply = incoming.[5].Read()

          Tracing.trace (tag "ReplyToFrontend") <| fun _ ->
            use outgoing = new ZMessage()
            outgoing.Add(new ZFrame(clientId))
            outgoing.Add(new ZFrame())
            outgoing.Add(new ZFrame(from))
            outgoing.Add(new ZFrame(reply))
            state.Frontend.Send(outgoing)
        else
          workerId
          |> sprintf "registered worker %A"
          |> Logger.debug (tag "worker")

      if available.Count > 0 then
        if state.Frontend.PollIn(poll, &incoming, &error, timespan) then
          let clientId = incoming.[0].Read()
          let request = incoming.[2].Read()
          let workerId = available.[0]

          Tracing.trace (tag "ForwardToBackend") <| fun _ ->
            use outgoing = new ZMessage()
            outgoing.Add(new ZFrame(workerId)) // worker ID
            outgoing.Add(new ZFrame())
            outgoing.Add(new ZFrame(clientId))
            outgoing.Add(new ZFrame())
            outgoing.Add(new ZFrame(request))
            state.Backend.Send(outgoing)

          available.RemoveAt(0)
      else
        state.AddWorker()

    dispose state
    state.Stopper.Set() |> ignore

  // ** create

  let create (args: BrokerConfig) =
    let state = new LocalThreadState(args)
    let cts = new CancellationTokenSource()
    let responder = ResponseActor.Start(loop state.Workers, cts.Token)

    let thread = Thread(worker state)
    thread.Name <- sprintf "Broker"
    thread.Start()

    state.Starter.WaitOne() |> ignore

    Either.map
      (fun _ ->
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
              state.Run <- false
              state.Stopper.WaitOne() |> ignore
              dispose cts
              state.Stopper.Dispose() })
      state.Error
