namespace Iris.Zmq

open System
open System.Text
open System.Threading
open System.Collections.Generic
open System.Collections.Concurrent

open Iris.Core
open ZeroMQ

type private Id = int64

module private Id =
  let mutable private id: Id = 0L
  let next () = Interlocked.Increment &id

type RawRequest =
  { From: Id
    Via: Id
    Body: byte array }

  override self.ToString() =
    sprintf "[Request] from %d via: %d bytes: %d"
      self.From
      self.Via
      (Array.length self.Body)

type RawResponse =
  { Via: Id
    Body: byte array }

  override self.ToString() =
    sprintf "[Response] via: %d bytes: %d"
      self.Via
      (Array.length self.Body)

type private IWorker =
  inherit IDisposable
  abstract Respond: RawResponse -> unit
  abstract Subscribe: (RawRequest -> unit) -> IDisposable

type IBroker =
  inherit IDisposable
  abstract Subscribe: (RawRequest -> unit) -> IDisposable
  abstract Respond: RawResponse -> unit

module internal Utils =

  [<Literal>]
  let internal READY = 0

  type internal Subscriptions = ConcurrentDictionary<int,IObserver<RawRequest>>

  type internal Listener = IObservable<RawRequest>

  let internal createListener (subs: Subscriptions) =
    { new Listener with
        member self.Subscribe (obs) =
          while not (subs.TryAdd(obs.GetHashCode(), obs)) do
            Thread.Sleep(1)

          { new IDisposable with
              member self.Dispose() =
                match subs.TryRemove(obs.GetHashCode()) with
                | true, _  -> ()
                | _ -> subs.TryRemove(obs.GetHashCode())
                      |> ignore } }

  let internal notify (subs: Subscriptions) (request: RawRequest) =
    for KeyValue(_,sub) in subs do
      sub.OnNext(request)

// __        __         _
// \ \      / /__  _ __| | _____ _ __
//  \ \ /\ / / _ \| '__| |/ / _ \ '__|
//   \ V  V / (_) | |  |   <  __/ |
//    \_/\_/ \___/|_|  |_|\_\___|_|

[<RequireQualifiedAccess>]
module internal Worker =
  open Utils

  [<NoComparison;NoEquality>]
  type private LocalThreadState () as self =
    let id = Id.next()

    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Context: ZContext
    [<DefaultValue>] val mutable Socket: ZSocket
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Starter: AutoResetEvent
    [<DefaultValue>] val mutable Stopper: AutoResetEvent
    [<DefaultValue>] val mutable Responder: ConcurrentStack<RawResponse>

    do
      self.Initialized <- false
      self.Started <- false
      self.Run <- true
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
      Context <- new ZContext()
      Socket <- new ZSocket(ZSocketType.REQ)

    member self.Id
      with get () = id

    interface IDisposable with
      member self.Dispose() =
        try
          self.Socket.Dispose()
        with
          | _ -> ()
        try
          self.Context.Dispose()
        with
          | _ -> ()

  let private initialize (state: LocalThreadState) =
    try
      let ctx = new ZContext()
      let socket = new ZSocket(ZSocketType.REQ)

      socket.IdentityString <- state.Id
      socket.SetOption(ZSocketOption.RCVTIMEO, 50) |> ignore
      socket.Connect(BACKEND_ADDRESS)

      use hello = new ZFrame(READY)
      socket.Send(hello)

      state.Initialized <- true
      state.Started <- true
      state.Context <- ctx
      state.Socket <- socket
    with
      | exn ->
        exn.Message
        |> sprintf "Exception trying to create worker: %s"
        |> Logger.debug (Id state.Id) ("Worker.initialize")
        state.Initialized <- true
        state.Started <- false
        state.Run <- false

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  let private started (state: LocalThreadState) =
    state.Initialized && state.Started

  let private worker (state: LocalThreadState) () =
    if not (started state) then
      initialize state

    let mutable error = Unchecked.defaultof<ZError>

    state.Starter.Set() |> ignore

    while spin state do
      use mutable request = Unchecked.defaultof<ZMessage>
      let result = state.Socket.ReceiveMessage(&request, &error)

      if result then
        let clientId = request.[0].ReadString()
        let body = request.[2].ReadString()

        { From = clientId
          Via = state.Id
          Body = body }
        |> notify state.Subscriptions

        let mutable response = Unchecked.defaultof<Response>

        // Tracer.trace "Worker.Thread.TryPop" body <| fun _ ->
        //   // BLOCK UNTIL RESPONSE IS SET
        while not (state.Responder.TryPop(&response)) do
          Thread.Sleep(1)

        use reply = new ZMessage()
        reply.Add(new ZFrame(clientId));
        reply.Add(new ZFrame());
        reply.Add(new ZFrame(response.Via));
        reply.Add(new ZFrame(response.Body));

        state.Socket.Send(reply)
        |> ignore

      else
        match error with
        | err when err = ZError.ETERM ->
          Logger.debug (Id state.Id) "Worker.Thread.worker" "context was terminated, shutting down"
          state.Run <- false
        | _ -> ()

    (state :> IDisposable).Dispose()
    state.Stopper.Set() |> ignore
    state.Stopper.Dispose()

  let create (id: string) =
    let subscriptions = new Subscriptions()
    let listener = createListener subscriptions
    let state =
      { Id = id
        Initialized = false
        Started = false
        Run = true
        Context = Unchecked.defaultof<ZContext>
        Socket = Unchecked.defaultof<ZSocket>
        Subscriptions = subscriptions
        Starter = new AutoResetEvent(false)
        Stopper = new AutoResetEvent(false)
        Responder = new ConcurrentStack<Response>() }

    let thread = new Thread(new ThreadStart(worker state))
    thread.Name <- sprintf "Worker %s" id
    thread.Start()

    state.Starter.WaitOne() |> ignore

    { new IWorker with
        member worker.Respond(response: Response) =
          // Tracer.trace "Worker.Respond" response.Body <| fun _ ->
          state.Responder.Push(response)

        member worker.Subscribe(callback: Request -> unit) =
          { new IObserver<Request> with
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

  type private Workers = ConcurrentDictionary<string,IWorker>

  type private ResponseActor = MailboxProcessor<Response>

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
          sprintf "no worker found for %s" response.Via
          |> Logger.debug (Id "Broker") "ResponseActor"
        return! impl ()
      }
    impl ()

  [<NoComparison;NoEquality>]
  type private LocalThreadState =
    { Num: int
      mutable Initialized: bool
      mutable Started: bool
      mutable Run: bool
      mutable Context: ZContext
      mutable Frontend: ZSocket
      mutable Backend: ZSocket
      mutable Workers: Workers
      Subscriptions: Subscriptions
      Stopper: AutoResetEvent }

    interface IDisposable with
      member self.Dispose() =
        try
          self.Frontend.Dispose()
        with | _ -> ()
        try
          self.Backend.Dispose()
        with | _ -> ()
        for KeyValue(_, worker) in self.Workers do
          try
            dispose worker
          with | _ -> ()
        try
          self.Context.Dispose()
        with | _ -> ()

  let private initialize (state: LocalThreadState) =
    let context = new ZContext()
    let frontend = new ZSocket(ZSocketType.ROUTER)
    let backend = new ZSocket(ZSocketType.ROUTER)

    frontend.Bind(FRONTEND_ADDRESS)
    backend.Bind(BACKEND_ADDRESS)

    let workers =
      [| for n in 0 .. (state.Num - 1) do
          let id = Guid.NewGuid()
          let worker = Worker.create (string id)

          worker.Subscribe (notify state.Subscriptions)
          |> ignore

          while not (state.Workers.TryAdd(string id, worker)) do
            Thread.Sleep(1) |]

    state.Initialized <- true
    state.Started <- true
    state.Context <- context
    state.Frontend <- frontend
    state.Backend <- backend

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  let private worker (state: LocalThreadState) () =
    if not state.Initialized then
      initialize state

    let workers = new ResizeArray<string>()
    let mutable incoming = Unchecked.defaultof<ZMessage>
    let mutable error = Unchecked.defaultof<ZError>
    let poll = ZPollItem.CreateReceiver()

    while spin state do
      let timespan = Nullable(TimeSpan.FromMilliseconds(4.0))

      if state.Backend.PollIn(poll, &incoming, &error, timespan) then
        let workerId = incoming.[0].ReadString()
        let clientId = incoming.[2].ReadString()

        workers.Add(workerId)

        if clientId <> READY then
          let from = incoming.[4].ReadString()
          let reply = incoming.[5].ReadString()

          // Tracer.trace "Broker.Backend.Poll" reply <| fun _ ->
          use outgoing = new ZMessage()
          outgoing.Add(new ZFrame(clientId))
          outgoing.Add(new ZFrame())
          outgoing.Add(new ZFrame(from))
          outgoing.Add(new ZFrame(reply))

          state.Frontend.Send(outgoing)

      if workers.Count > 0 then
        if state.Frontend.PollIn(poll, &incoming, &error, timespan) then
          let clientId = incoming.[0].ReadString()
          let request = incoming.[2].ReadString()

          // Tracer.trace "Broker.Frontend.Poll" request <| fun _ ->
          use outgoing = new ZMessage()
          outgoing.Add(new ZFrame(workers.[0])) // worker ID
          outgoing.Add(new ZFrame())
          outgoing.Add(new ZFrame(clientId))
          outgoing.Add(new ZFrame())
          outgoing.Add(new ZFrame(request))

          state.Backend.Send(outgoing)
          workers.RemoveAt(0)

    (state :> IDisposable).Dispose()
    state.Stopper.Set() |> ignore

  let create (num: int) =
    let subscriptions = new Subscriptions()
    let listener = createListener subscriptions

    let cts = new CancellationTokenSource()
    let workers = new Workers()
    let responder = ResponseActor.Start(loop workers, cts.Token)

    let state =
      { Initialized = false
        Started = false
        Run = true
        Num = num
        Context = Unchecked.defaultof<ZContext>
        Frontend = Unchecked.defaultof<ZSocket>
        Backend = Unchecked.defaultof<ZSocket>
        Subscriptions = subscriptions
        Workers = workers
        Stopper = new AutoResetEvent(false) }

    let thread = new Thread(new ThreadStart(worker state))
    thread.Name <- sprintf "Broker"
    thread.Start()

    { new IBroker with
        member self.Subscribe(callback: Request -> unit) =
          { new IObserver<Request> with
              member self.OnCompleted() = ()
              member self.OnError (error) = ()
              member self.OnNext(value) = callback value }
          |> listener.Subscribe

        member self.Respond (response: Response) =
          responder.Post response

        member self.Dispose() =
          state.Run <- false
          state.Stopper.WaitOne() |> ignore
          dispose cts
          state.Stopper.Dispose()
        }
