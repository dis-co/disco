namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Diagnostics
open System.Collections.Concurrent
open Iris.Zmq
open Iris.Core

// * Types

type ClockEvent =
  { Frame: int64<frame>
    Deviation: int64<ns> }

type IClock =
  inherit IDisposable
  abstract Subscribe: (ClockEvent -> unit) -> IDisposable
  abstract Start: unit -> unit
  abstract Stop: unit -> unit
  abstract Running: bool with get
  abstract Fps: int16<fps>  with get, set
  abstract Frame: int64<frame>

// * Clock module

module Clock =

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<int, IObserver<ClockEvent>>

  // ** Listener

  type private Listener = IObservable<ClockEvent>

  // ** createListener

  let private createListener (subscriptions: Subscriptions) =
    { new Listener with
        member self.Subscribe(obs) =
          while not (subscriptions.TryAdd(obs.GetHashCode(), obs)) do
            Thread.Sleep(1)

          { new IDisposable with
              member self.Dispose() =
                match subscriptions.TryRemove(obs.GetHashCode()) with
                | true, _  -> ()
                | _ -> subscriptions.TryRemove(obs.GetHashCode())
                      |> ignore } }

  // ** secPerFrame

  let private secPerFrame (fps: int16<fps>) = 1. / float fps

  // ** μsPerFrame

  let private μsPerFrame (fps: int16<fps>) =
    secPerFrame fps
    * 1000. (* ms *)
    * 1000. (* μs *)
    * 1000. (* ns *)
    |> int64

  // ** μsPerTick

  let μsPerTick =
    (1000L (* ms *) * 1000L (* μs *) * 1000L (* ns *))
    / Stopwatch.Frequency
    |> int64

  // ** ticksPerFrame

  let ticksPerFrame (fps: int16<fps>) =
    μsPerFrame fps / μsPerTick

  // ** calculateTimeout

  let private calculateTimeout (fps: int16<fps>) =
    fps * 8s                            // sample the time 8x more often than required
    |> ticksPerFrame                     // to achive relative high accuracy
    |> TimeSpan.FromTicks

  // ** ClockState

  type private ClockState(ip: IpAddress) =
    let addr =
      Uri.epgmUri
        ip
        (IPv4Address Constants.CLOCK_MCAST_ADDRESS)
        Constants.CLOCK_MCAST_PORT

    let socket = new Pub(addr, Constants.CLOCK_MCAST_PREFIX)

    let subscriptions = Subscriptions()
    let stopwatch = Stopwatch.StartNew()

    let mutable run = true
    let mutable publish = true
    let mutable disposed = false
    let mutable previous = 0L
    let mutable frame = 0L<frame>
    let mutable fps = 60s<fps>
    let mutable timeout = calculateTimeout fps

    do
      printfn "starting socket"
      socket.Start()
      |> Either.mapError (string >> failwith)
      |> ignore

    member state.Run
      with get ()  = run && not disposed
      and set run' =
        if not run' then
          stopwatch.Stop()
          stopwatch.Reset()
        run <- run'

    member state.Publish
      with get ()  = publish && not disposed
      and set pub  = publish <- pub

    member state.Socket
      with get () = socket

    member state.Timeout
      with get () = timeout

    member state.Subscriptions
      with get () = subscriptions

    member state.Stopwatch
      with get () = stopwatch

    member state.Disposed
      with get () = disposed

    member state.Frame
      with get () = frame

    member state.Previous
      with get () = previous
      and set current = previous <- current

    member state.Fps
      with get () = fps
      and set fps' =
        fps <- fps'
        timeout <- calculateTimeout fps'

    member state.Tick() =
      frame <- frame + 1L<frame>

    interface IDisposable with
      member self.Dispose() =
        disposed <- true
        tryDispose socket ignore
        subscriptions.Clear()

  // ** notify

  let private notify (state: ClockState) (ev: ClockEvent) =
    for KeyValue(_,obs) in state.Subscriptions do
      obs.OnNext(ev)

  // ** worker

  let private worker (state: ClockState) () =
    while state.Run do
      if state.Publish && state.Run then
        let elapsed = state.Stopwatch.ElapsedTicks
        let diff = elapsed - state.Previous
        if diff >= ticksPerFrame state.Fps then // fire another clock event
          state.Previous <- elapsed
          state.Tick()

          let ev = { Frame = state.Frame
                     Deviation = (diff / μsPerTick) * 1L<ns> }

          notify state ev

          state.Frame
          |> uint32
          |> UpdateClock
          |> Binary.encode
          |> state.Socket.Publish
          |> Either.mapError (string >> Logger.err "Clock")
          |> ignore

      Thread.Sleep state.Timeout

  // ** create

  let create (ip: IpAddress) =
    let state = new ClockState(ip)
    let listener = createListener state.Subscriptions

    if not Stopwatch.IsHighResolution then
      Logger.warn "Clock" "internal timer is not using high resolution clock"

    let thread = Thread(worker state)
    thread.Start()

    { new IClock with
        member clock.Start() =
          if not state.Disposed then
            state.Publish <- true

        member clock.Stop() =
          if not state.Disposed then
            state.Publish <- false

        member clock.Running
          with get () = state.Run && state.Publish && not state.Disposed

        member clock.Frame
          with get () = state.Frame

        member clock.Fps
          with get () = state.Fps
          and set fps = if not state.Disposed then state.Fps <- fps

        member clock.Subscribe (callback: ClockEvent -> unit) =
          { new IObserver<ClockEvent> with
              member self.OnCompleted() = ()
              member self.OnError(error) = ()
              member self.OnNext(value) = callback value }
          |> listener.Subscribe

        member clock.Dispose() =
          dispose state
      }
