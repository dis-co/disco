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
  let internal READY = -1L

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
  type private LocalThreadState (address: string) as self =
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
      self.Context <- new ZContext()
      self.Socket <- new ZSocket(ZSocketType.REQ)
      self.Socket.Identity <- BitConverter.GetBytes(id)
      self.Socket.SetOption(ZSocketOption.RCVTIMEO, 50) |> ignore
      self.Socket.Connect(address)

      use hello = new ZFrame(READY)
      self.Socket.Send(hello)

      self.Initialized <- true
      self.Started <- true

    member self.Id
      with get () = id

    member self.LogId
      with get () = Iris.Core.Id ("worker-" + string id)

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
      state.Start()
    with
      | exn ->
        exn.Message
        |> sprintf "Exception trying to create worker: %s"
        |> Logger.debug state.LogId "Worker.initialize"
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
        let clientId = request.[0].ReadInt64()
        let body = request.[2].Read()

        { From = clientId
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
        | err when err = ZError.ETERM ->
          Logger.debug state.LogId "Worker.Thread.worker" "context was terminated, shutting down"
          state.Run <- false
        | _ -> ()

    (state :> IDisposable).Dispose()
    state.Stopper.Set() |> ignore
    state.Stopper.Dispose()

  let create (  sBackend: string) =
    let subscriptions = new Subscriptions()
    let listener = createListener subscriptions
    let state = new LocalThreadState(address =   sBackend)

    let thread = new Thread(new ThreadStart(worker state))
    thread.Name <- string state.LogId
    thread.Start()

    state.Starter.WaitOne() |> ignore

    { new IWorker with
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

  type private Workers = ConcurrentDictionary<Id,IWorker>

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
          sprintf "no worker found for %d" response.Via
          |> Logger.debug (Iris.Core.Id "Broker") "ResponseActor"
        return! impl ()
      }
    impl ()

  [<NoComparison;NoEquality>]
  type private LocalThreadState (num: int, frontend: string, backend: string) as self =
    [<DefaultValue>] val mutable Initialized: bool
    [<DefaultValue>] val mutable Started: bool
    [<DefaultValue>] val mutable Run: bool
    [<DefaultValue>] val mutable Context: ZContext
    [<DefaultValue>] val mutable Frontend: ZSocket
    [<DefaultValue>] val mutable Backend: ZSocket
    [<DefaultValue>] val mutable Workers: Workers
    [<DefaultValue>] val mutable Subscriptions: Subscriptions
    [<DefaultValue>] val mutable Stopper: AutoResetEvent

    let disposables = new ResizeArray<IDisposable>()

    do
      self.Initialized <- false
      self.Started <- false
      self.Run <- true

    member self.Start () =
      self.Context <- new ZContext()
      self.Frontend <- new ZSocket(ZSocketType.ROUTER)
      self.Backend <- new ZSocket(ZSocketType.ROUTER)

      self.Frontend.Bind(frontend)
      self.Backend.Bind(backend)

      for n in 0 .. (num - 1) do
        let worker = Worker.create backend

        worker.Subscribe (notify self.Subscriptions)
        |> disposables.Add

        while not (self.Workers.TryAdd(worker.Id, worker)) do
          Thread.Sleep(TimeSpan.FromTicks 1L)

      self.Initialized <- true
      self.Started <- true

    interface IDisposable with
      member self.Dispose() =
        for disp in disposables do
          try
            dispose
          with | _ -> ()

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

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  let private worker (state: LocalThreadState) () =
    if not state.Initialized then
      state.Start()

    let busy = new ResizeArray<Id>()
    let mutable incoming = Unchecked.defaultof<ZMessage>
    let mutable error = Unchecked.defaultof<ZError>
    let poll = ZPollItem.CreateReceiver()

    while spin state do
      let timespan = Nullable(TimeSpan.FromMilliseconds(4.0))

      if state.Backend.PollIn(poll, &incoming, &error, timespan) then
        let workerId = incoming.[0].ReadInt64()
        let clientId = incoming.[2].ReadInt64()

        busy.Add(workerId)

        if clientId <> READY then
          let from = incoming.[4].ReadInt64()
          let reply = incoming.[5].Read()

          // Tracer.trace "Broker.Backend.Poll" reply <| fun _ ->
          use outgoing = new ZMessage()
          outgoing.Add(new ZFrame(clientId))
          outgoing.Add(new ZFrame())
          outgoing.Add(new ZFrame(from))
          outgoing.Add(new ZFrame(reply))

          state.Frontend.Send(outgoing)

      if busy.Count > 0 then
        if state.Frontend.PollIn(poll, &incoming, &error, timespan) then
          let clientId = incoming.[0].ReadInt64()
          let request = incoming.[2].Read()

          // Tracer.trace "Broker.Frontend.Poll" request <| fun _ ->
          use outgoing = new ZMessage()
          outgoing.Add(new ZFrame(busy.[0])) // worker ID
          outgoing.Add(new ZFrame())
          outgoing.Add(new ZFrame(clientId))
          outgoing.Add(new ZFrame())
          outgoing.Add(new ZFrame(request))

          state.Backend.Send(outgoing)
          busy.RemoveAt(0)

    (state :> IDisposable).Dispose()
    state.Stopper.Set() |> ignore

  let create (num: int) =
    let subscriptions = new Subscriptions()
    let listener = createListener subscriptions

    let cts = new CancellationTokenSource()
    let workers = new Workers()
    let responder = ResponseActor.Start(loop workers, cts.Token)

    let state = new LocalThreadState(num = num, frontend = frontend, backend = backend)

    let thread = new Thread(new ThreadStart(worker state))
    thread.Name <- sprintf "Broker"
    thread.Start()

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
