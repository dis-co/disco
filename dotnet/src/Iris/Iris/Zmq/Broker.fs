namespace Iris.Zmq

open System
open System.Text
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

type private IWorker =
  inherit IDisposable
  abstract Id: WorkerId
  abstract Respond: RawResponse -> unit
  abstract Subscribe: (RawRequest -> unit) -> IDisposable

type IBroker =
  inherit IDisposable
  abstract Subscribe: (RawRequest -> unit) -> IDisposable
  abstract Respond: RawResponse -> unit

[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
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

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

[<RequireQualifiedAccess>]
module Client =
  open Utils

  let rand = new System.Random()

  type private LocalThreadState(id: Id, frontend: string) as self =
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
        self.Socket <- new ZSocket(ZSocketType.REQ)
        self.Socket.ReceiveTimeout <- TimeSpan.FromMilliseconds 1000.0
        self.Socket.Identity <- clid.ToByteArray()
        self.Socket.Connect(frontend)
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
            let body = reply.[1].Read()

            state.Response <- Right body
            state.Responder.Set() |> ignore
          with
            | exn ->
              let error =
                exn.Message + exn.StackTrace
                |> Error.asSocketError "IClient.worker"
              state.Response <- Left error
              state.Run <- false
              state.Started <- false
              state.Responder.Set() |> ignore

    dispose state
    state.Stopper.Set() |> ignore

  let create (id: Id) (frontend: string) =
    let state = new LocalThreadState(id = id, frontend = frontend)
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

  [<NoComparison;NoEquality>]
  type private LocalThreadState (id: WorkerId, address: string) as self =

    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Disposed: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Context: ZContext
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

    member self.Start() =
      try
        self.Context <- new ZContext()
        self.Socket <- new ZSocket(ZSocketType.REQ)
        self.Socket.Identity <- BitConverter.GetBytes id
        self.Socket.SetOption(ZSocketOption.RCVTIMEO, 500) |> ignore
        self.Socket.Connect(address)

        use hello = new ZFrame(READY)
        self.Socket.Send(hello)

        self.Initialized <- true
        self.Started <- true
        self.Starter.Set() |> ignore
      with
        | exn ->
          dispose self

          exn.Message + exn.StackTrace
          |> Error.asSocketError "Worker.LocalThreadState.Start"
          |> Either.fail
          |> fun error -> self.Error <- error

          self.Initialized <- true
          self.Started <- false
          self.Run <- false

    member self.Id
      with get () = id

    interface IDisposable with
      member self.Dispose() =
        tryDispose self.Socket
        tryDispose self.Context
        self.Disposed <- true

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  let private worker (state: LocalThreadState) () =
    state.Start()

    Logger.debug "Worker.initialize" "startup done"

    let mutable error = Unchecked.defaultof<ZError>

    while spin state do
      use mutable request = Unchecked.defaultof<ZMessage>

      let result = state.Socket.ReceiveMessage(&request, &error)

      if result then
        Tracing.trace "IWorker.Request" <| fun () ->
          let clientId = request.[0].Read()
          let body = request.[2].Read()

          { From = Guid clientId
            Via = state.Id
            Body = body }
          |> notify state.Subscriptions

          let mutable response = Unchecked.defaultof<RawResponse>

          // Tracer.trace "Worker.Thread.TryPop" body <| fun _ ->
          //   // BLOCK UNTIL RESPONSE IS SET
          while not (state.Responder.TryPop(&response)) do
            Thread.Sleep(TimeSpan.FromTicks 1L)

          use reply = new ZMessage()
          reply.Add(new ZFrame(clientId));
          reply.Add(new ZFrame());
          reply.Add(new ZFrame(response.Via));
          reply.Add(new ZFrame(response.Body));

          state.Socket.Send(reply)
          |> ignore
      else
        match error with
        | err when err = ZError.EAGAIN -> ()
        | err when err = ZError.ETERM ->
          "context was terminated, shutting down"
          |> Logger.err "Worker.Thread.worker"
          state.Run <- false
        | other ->
          other
          |> sprintf "error in worker: %O"
          |> Logger.err "Worker.Thread.worker"
          state.Run <- false

    dispose state
    state.Stopper.Set() |> ignore
    dispose state.Stopper

  let create (id: WorkerId) (backend: string)=
    let state = new LocalThreadState(id = id, address = backend)
    let listener = createListener state.Subscriptions

    let thread = new Thread(new ThreadStart(worker state))
    thread.Name <- sprintf "broker-worker-%d" state.Id
    thread.Start()

    state.Starter.WaitOne() |> ignore

    match state.Error with
    | Left error -> Either.fail error
    | Right _ ->
      Either.succeed
        { new IWorker with
            member worker.Id
              with get () = state.Id

            member worker.Respond(response: RawResponse) =
              // Tracer.trace "Worker.Respond" response.Body <| fun _ ->
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
              thread.Join()
          }

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

  let private loop (workers: Workers) (agent: ResponseActor) =
    let rec impl () =
      async {
        let! response = agent.Receive()
        match workers.TryGetValue response.Via with
        | true, worker ->
          try
            // Tracer.trace "ResponseActor" response.Body <| fun _ ->
            worker.Respond response     // try to respond
          with
            | exn ->
              agent.Post response       // re-cue the response
        | _ ->
          sprintf "no worker found for %O" response.Via
          |> Logger.err "ResponseActor"
        return! impl ()
      }
    impl ()

  [<NoComparison;NoEquality>]
  type private LocalThreadState (num: int, frontend: string, backend: string) as self =

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
        self.Frontend <- new ZSocket(ZSocketType.ROUTER)
        self.Backend <- new ZSocket(ZSocketType.ROUTER)
        self.Frontend.Bind(frontend)
        self.Backend.Bind(backend)

        for n in 1 .. num do
          match Worker.create (uint16 n) backend with
          | Right worker ->
            notify self.Subscriptions
            |> worker.Subscribe
            |> disposables.Add

            while not (self.Workers.TryAdd(worker.Id, worker)) do
              Thread.Sleep(TimeSpan.FromTicks 1L)
          | Left error ->
            error
            |> sprintf "unable to create worker: %O"
            |> Logger.err "IBroker.Start"

        self.Initialized <- true
        self.Started <- true
      with
        | exn ->
          exn.Message + exn.StackTrace
          |> Error.asSocketError "Broker.LocalThreadState.Start"
          |> Either.fail
          |> fun error -> self.Error <- error
          dispose self

    interface IDisposable with
      member self.Dispose() =
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

    Logger.debug "IBroker.initialize" "startup done"

    let available = new ResizeArray<WorkerId>()
    let mutable incoming = Unchecked.defaultof<ZMessage>
    let mutable error = Unchecked.defaultof<ZError>
    let poll = ZPollItem.CreateReceiver()

    while spin state do
      let timespan = Nullable(TimeSpan.FromMilliseconds(0.1))

      if state.Backend.PollIn(poll, &incoming, &error, timespan) then
        let workerId = incoming.[0].ReadUInt16()
        let clientId = incoming.[2].Read()

        available.Add(workerId)

        if clientId <> READY then
          let from = incoming.[4].Read()
          let reply = incoming.[5].Read()

          Tracing.trace "Broker Reply To Frontend" <| fun _ ->
            use outgoing = new ZMessage()
            outgoing.Add(new ZFrame(clientId))
            outgoing.Add(new ZFrame())
            outgoing.Add(new ZFrame(from))
            outgoing.Add(new ZFrame(reply))
            state.Frontend.Send(outgoing)
        else
          workerId
          |> sprintf "registered worker %A"
          |> Logger.debug "IBroker.Thread"

      if available.Count > 0 then
        if state.Frontend.PollIn(poll, &incoming, &error, timespan) then
          let clientId = incoming.[0].Read()
          let request = incoming.[2].Read()
          let workerId = available.[0]

          Tracing.trace "Broker Forward To Backend" <| fun _ ->
            use outgoing = new ZMessage()
            outgoing.Add(new ZFrame(workerId)) // worker ID
            outgoing.Add(new ZFrame())
            outgoing.Add(new ZFrame(clientId))
            outgoing.Add(new ZFrame())
            outgoing.Add(new ZFrame(request))
            state.Backend.Send(outgoing)

          available.RemoveAt(0)

    dispose state
    state.Stopper.Set() |> ignore

  let create (id: Id) (num: int) (frontend: string) (backend: string) =
    let state = new LocalThreadState(num = num, frontend = frontend, backend = backend)
    let listener = createListener state.Subscriptions
    let cts = new CancellationTokenSource()
    let responder = ResponseActor.Start(loop state.Workers, cts.Token)

    let thread = new Thread(new ThreadStart(worker state))
    thread.Name <- sprintf "Broker"
    thread.Start()

    state.Starter.WaitOne() |> ignore

    match state.Error with
    | Left error -> Either.fail error
    | Right _ ->
      Either.succeed
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
              state.Stopper.Dispose()
            }
