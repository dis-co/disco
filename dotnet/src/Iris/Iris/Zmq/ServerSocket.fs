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

// * ServerConfig

type ServerConfig =
  { Id: Id
    Listen: Url }

// * IServer

type IServer =
  inherit IDisposable
  abstract Subscribe: (RawServerRequest -> unit) -> IDisposable
  abstract Respond: RawServerResponse -> unit

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

// * RawServerRequest module

module RawServerRequest =

  let create (from: Guid) (body: byte array) : RawServerRequest =
    { RequestId = Guid.NewGuid()
      From = from
      Body = body }

// * RawServerResponse module

module RawServerResponse =

  let fromRequest (request: RawServerRequest) (body: byte array) =
    { RequestId = request.RequestId
      From = request.From
      Body = body }

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

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<RawServerRequest>

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

    // *** do

    do
      self.Initialized <- false
      self.Disposed <- false
      self.Started <- false
      self.Run <- true
      self.Subscriptions <- Subscriptions()
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
        self.Socket.Unbind(unwrap args.Listen)
        self.Socket.Close()
        tryDispose self.Socket ignore
        self.Disposed <- true
        Logger.debug (tag "Dispose") "disposed"

  // ** spin

  let private spin (state: LocalThreadState) =
    state.Initialized && state.Started && state.Run

  // ** serverLoop

  let private serverLoop (state: LocalThreadState) () =
    Logger.debug (tag "serverLoop") "startup done"

    let poll = ZPollItem.CreateReceiver()
    let timeout = Nullable(TimeSpan.FromMilliseconds 1.0)

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
            Observable.onNext state.Subscriptions request
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
                Observable.subscribe callback state.Subscriptions

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
