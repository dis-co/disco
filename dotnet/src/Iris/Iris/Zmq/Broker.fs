namespace Iris.Zmq

open System
open System.Text
open System.Diagnostics
open System.Threading
open System.Collections.Generic
open System.Collections.Concurrent

open Iris.Core
open ZeroMQ

type private WorkerId = uint16

[<RequireQualifiedAccess>]
module private RequestCount =
  let mutable private id = 0L
  let increment () = Interlocked.Increment &id |> ignore
  let current () = id

type RawRequest =
  { From: Guid
    Via: WorkerId
    Body: byte array }

  override self.ToString() =
    sprintf "[Request] from %O via: %O bytes: %d"
      self.From
      self.Via
      (Array.length self.Body)

type RawResponse =
  { Via: WorkerId
    Body: byte array }

  override self.ToString() =
    sprintf "[Response] via: %O bytes: %d"
      self.Via
      (Array.length self.Body)

type IClient =
  inherit IDisposable
  abstract Id: Id
  abstract Request: byte array -> Either<IrisError,byte array>
  abstract Running: bool
  abstract Restart: unit -> unit


[<NoComparison>]
type WorkerArgs =
  { Id: WorkerId
    Backend: string
    Context: ZContext
    RequestTimeout: uint32 }

type private IWorker =
  inherit IDisposable
  abstract Id: WorkerId
  abstract Respond: RawResponse -> unit
  abstract Subscribe: (RawRequest -> unit) -> IDisposable

type BrokerArgs =
  { Id: Id
    MinWorkers: uint8
    MaxWorkers: uint8
    Frontend: string
    Backend: string
    RequestTimeout: uint32 }

type IBroker =
  inherit IDisposable
  abstract Subscribe: (RawRequest -> unit) -> IDisposable
  abstract Respond: RawResponse -> unit

module RawResponse =

  let fromRequest (request: RawRequest) (body: byte array) =
    { Via = request.Via; Body = body }

//  _   _ _   _ _
// | | | | |_(_) |___
// | | | | __| | / __|
// | |_| | |_| | \__ \
//  \___/ \__|_|_|___/

module internal Utils =

  let internal READY = [| 0uy; 0uy |] // 0us

  type internal Subscriptions = ConcurrentDictionary<int,IObserver<RawRequest>>

  type internal Listener = IObservable<RawRequest>

  let internal tryDispose (disp: IDisposable) =
    try dispose disp with | _ -> ()

  let internal createListener (subs: Subscriptions) =
    { new Listener with
        member self.Subscribe (obs) =
          while not (subs.TryAdd(obs.GetHashCode(), obs)) do
            Thread.Sleep(TimeSpan.FromTicks 1L)

          { new IDisposable with
              member self.Dispose() =
                match subs.TryRemove(obs.GetHashCode()) with
                | true, _  -> ()
                | _ -> subs.TryRemove(obs.GetHashCode())
                      |> ignore } }

  let internal notify (subs: Subscriptions) (request: RawRequest) =
    for KeyValue(_,sub) in subs do
      sub.OnNext(request)

  let internal shouldRestart (no: int) =
    List.contains no [
      ZError.EAGAIN.Number
      ZError.EFSM.Number
    ]

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

[<RequireQualifiedAccess>]
module Client =
  open Utils

  let rand = new System.Random()

  type private LocalThreadState(id: Id, frontend: string, timeout: float) as self =
    let clid = string id |> Guid.Parse

    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Disposed: bool
    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Socket: ZSocket
    [<DefaultValue>] val mutable Context: ZContext
    [<DefaultValue>] val mutable Request: byte array
    [<DefaultValue>] val mutable Response: Either<IrisError, byte array>
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent
    [<DefaultValue>] val mutable Requester: AutoResetEvent
    [<DefaultValue>] val mutable Responder: AutoResetEvent

    do
      self.Run <- true
      self.Started <- false
      self.Disposed <- false
      self.Initialized <- false
      self.Request <- [| |]
      self.Response <- Right [| |]
      self.Starter <- new AutoResetEvent(false)
      self.Stopper <- new AutoResetEvent(false)
      self.Requester <- new AutoResetEvent(false)
      self.Responder <- new AutoResetEvent(false)

    member self.Id
      with get () = id

    member self.Start() =
      try
        self.Context <- new ZContext()
        self.StartSocket()
        self.Initialized <- true
        self.Started     <- true
      with
        | exn ->
          dispose self
          self.Initialized <- true
          self.Started <- false
          self.Run <- false
          exn.Message + exn.StackTrace
          |> Error.asRaftError "Client.LocalThreadState.Start"
          |> Either.fail
          |> fun error -> self.Response <- error

    member self.StartSocket() =
      self.Socket <- new ZSocket(self.Context, ZSocketType.REQ)
      self.Socket.ReceiveTimeout <- TimeSpan.FromMilliseconds timeout
      self.Socket.Linger <- TimeSpan.FromMilliseconds 1.0
      self.Socket.Identity <- clid.ToByteArray()
      self.Socket.Connect(frontend)

    member self.RestartSocket() =
      Logger.debug "Client" "restarting socket"
      dispose self.Socket
      self.StartSocket()

    member self.Reset() =
      if not self.Disposed then
        dispose self

      self.Run <- true
      self.Started <- false
      self.Disposed <- false
      self.Initialized <- false
      self.Request <- [| |]
      self.Response <- Right [| |]

    interface IDisposable with
      member self.Dispose() =
        tryDispose self.Socket
        tryDispose self.Context
        self.Disposed <- true

  let private initialize (state: LocalThreadState) =
    if not state.Initialized && not state.Started then
      state.Start()
      state.Starter.Set() |> ignore

  let private started (state: LocalThreadState) =
    state.Initialized && state.Started

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  let private worker (state: LocalThreadState) () =
    initialize state

    while spin state do
      state.Requester.WaitOne() |> ignore

      Tracing.trace "IClient.Request" <| fun () ->
        if spin state then
          try
            use msg = new ZMessage()
            msg.Add(new ZFrame(state.Request))
            state.Socket.Send(msg)

            use reply = state.Socket.ReceiveMessage()
            // let worker = reply.[0].ReadInt64()

            state.Response <- reply.[1].Read() |> Either.succeed
            state.Responder.Set() |> ignore
          with
            | :? ZException as exn when shouldRestart exn.ErrNo ->
              state.RestartSocket()
              state.Response <-
                String.Format("{0} encountered sending request", exn.Message)
                |> Error.asSocketError "Client.worker"
                |> Either.fail
              state.Responder.Set() |> ignore
            | :? ZException as exn when exn.ErrNo = ZError.ETERM.Number ->
              state.Response <-
                exn.Message + exn.StackTrace
                |> Error.asSocketError "IClient.worker"
                |> Either.fail
              state.Run <- false
              state.Started <- false
              state.Responder.Set() |> ignore

    tryDispose state
    state.Stopper.Set() |> ignore

  let create (id: Id) (frontend: string) (timeout: float) =
    let state = new LocalThreadState(id = id, frontend = frontend, timeout = timeout)
    let mutable thread = new Thread(new ThreadStart(worker state))
    thread.Name <- sprintf "Client %O" state.Id
    thread.Start()

    let locker = new Object()

    state.Starter.WaitOne() |> ignore

    { new IClient with
        member self.Request(body: byte array) =
          if state.Disposed then
            "Socket already disposed"
            |> Error.asSocketError "IClient.Dispose"
            |> Either.fail
          else
            lock locker <| fun _ ->
              // Tracer.trace "Client.Request" str <| fun _ ->
              state.Request <- body
              state.Requester.Set() |> ignore
              state.Responder.WaitOne() |> ignore
              state.Response

        member self.Running
          with get () =
            state.Initialized && state.Run && state.Started

        member self.Restart () =
          self.Dispose()
          state.Reset()
          thread <- new Thread(new ThreadStart(worker state))
          thread.Name <- sprintf "Client %O" state.Id
          thread.Start()
          state.Starter.WaitOne() |> ignore

        member self.Id
          with get () = state.Id

        member self.Dispose() =
          lock state <| fun _ ->
            state.Run <- false
          state.Requester.Set() |> ignore
          state.Stopper.WaitOne() |> ignore }

// __        __         _
// \ \      / /__  _ __| | _____ _ __
//  \ \ /\ / / _ \| '__| |/ / _ \ '__|
//   \ V  V / (_) | |  |   <  __/ |
//    \_/\_/ \___/|_|  |_|\_\___|_|

[<RequireQualifiedAccess>]
module private Worker =
  open Utils

  let private tag (id: WorkerId) (str: string) =
    String.Format("Worker-{0}.{1}", id, str)

  [<NoComparison;NoEquality>]
  type private LocalThreadState (args: WorkerArgs) as self =

    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Disposed: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Socket: ZSocket
    [<DefaultValue>] val mutable Error: Either<IrisError,unit>
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent
    [<DefaultValue>] val mutable Responder: ConcurrentStack<RawResponse>

    do
      self.Initialized <- false
      self.Disposed <- false
      self.Started <- false
      self.Run <- true
      self.Error <- Right ()
      self.Subscriptions <- new Subscriptions()
      self.Starter <- new AutoResetEvent(false)
      self.Stopper <- new AutoResetEvent(false)
      self.Responder <- new ConcurrentStack<RawResponse>()

    override state.ToString() =
      sprintf "initialized: %b started: %b run: %b"
        state.Initialized
        state.Started
        state.Run

    member self.Timeout
      with get () = args.RequestTimeout

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

    member self.StartSocket () =
      self.Socket <- new ZSocket(args.Context, ZSocketType.REQ)
      self.Socket.Identity <- BitConverter.GetBytes args.Id
      self.Socket.ReceiveTimeout <- TimeSpan.FromMilliseconds 10.0
      self.Socket.Linger <- TimeSpan.FromMilliseconds 1.0
      self.Socket.Connect(args.Backend)

    member self.RestartSocket() =
      Logger.debug (tag args.Id "RestartSocket") "restarting socket"
      dispose self.Socket
      self.StartSocket()

    member self.Register() =
      use hello = new ZFrame(READY)
      self.Socket.Send(hello)

    member self.Id
      with get () = args.Id

    interface IDisposable with
      member self.Dispose() =
        tryDispose self.Socket
        self.Disposed <- true

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  let private timedOut (state: LocalThreadState) (timer: Stopwatch) =
    timer.ElapsedMilliseconds > int64 state.Timeout

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

          let mutable response = Unchecked.defaultof<RawResponse>

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

  let create (args: WorkerArgs)  =
    let state = new LocalThreadState(args)
    let listener = createListener state.Subscriptions

    let thread = new Thread(new ThreadStart(worker state))
    thread.Name <- sprintf "broker-worker-%d" state.Id
    thread.Start()

    state.Starter.WaitOne() |> ignore

    Either.map
      (fun _ ->
        { new IWorker with
            member worker.Id
              with get () = state.Id

            member worker.Respond(response: RawResponse) =
              state.Responder.Push(response)

            member worker.Subscribe(callback: RawRequest -> unit) =
              { new IObserver<RawRequest> with
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

//  ____            _
// | __ ) _ __ ___ | | _____ _ __
// |  _ \| '__/ _ \| |/ / _ \ '__|
// | |_) | | | (_) |   <  __/ |
// |____/|_|  \___/|_|\_\___|_|

[<RequireQualifiedAccess>]
module Broker =
  open Utils

  type private Workers = ConcurrentDictionary<WorkerId,IWorker>

  type private ResponseActor = MailboxProcessor<RawResponse>

  let private tag (str: string) =
    String.Format("Broker.{0}", str)

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

  [<NoComparison;NoEquality>]
  type private LocalThreadState (args: BrokerArgs) as self =

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

    member self.Start () =
      try
        self.Context <- new ZContext()
        self.Frontend <- new ZSocket(self.Context, ZSocketType.ROUTER)
        self.Backend <- new ZSocket(self.Context, ZSocketType.ROUTER)
        self.Frontend.Bind(args.Frontend)
        self.Backend.Bind(args.Backend)

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

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  let private worker (state: LocalThreadState) () =
    state.Start()
    state.Starter.Set() |> ignore

    Logger.debug (tag "initialize") "startup done"

    let available = new ResizeArray<WorkerId>()
    let mutable incoming = Unchecked.defaultof<ZMessage>
    let mutable error = Unchecked.defaultof<ZError>
    let poll = ZPollItem.CreateReceiver()

    while spin state do
      let timespan = Nullable(TimeSpan.FromMilliseconds(1.0))

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

  let create (args: BrokerArgs) =
    let state = new LocalThreadState(args)
    let listener = createListener state.Subscriptions
    let cts = new CancellationTokenSource()
    let responder = ResponseActor.Start(loop state.Workers, cts.Token)

    let thread = new Thread(new ThreadStart(worker state))
    thread.Name <- sprintf "Broker"
    thread.Start()

    state.Starter.WaitOne() |> ignore

    Either.map
      (fun _ ->
        { new IBroker with
            member self.Subscribe(callback: RawRequest -> unit) =
              { new IObserver<RawRequest> with
                  member self.OnCompleted() = ()
                  member self.OnError (error) = ()
                  member self.OnNext(value) = callback value }
              |> listener.Subscribe

            member self.Respond (response: RawResponse) =
              responder.Post response

            member self.Dispose() =
              state.Run <- false
              state.Stopper.WaitOne() |> ignore
              dispose cts
              state.Stopper.Dispose() })
      state.Error
