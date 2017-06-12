namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Diagnostics
open System.Collections.Concurrent
open Iris.Zmq
open Iris.Core
open ZeroMQ

// * Clock module

module Clock =

  // ** Subscriptions

  type private Subscriptions = Subscriptions<IrisEvent>

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

  type private ClockState() =
    let subscriptions = Subscriptions()
    let stopwatch = Stopwatch.StartNew()

    let mutable run = true
    let mutable publish = true
    let mutable disposed = false
    let mutable previous = 0L
    let mutable frame = 0L<frame>
    let mutable fps = 60s<fps>
    let mutable timeout = calculateTimeout fps

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
        subscriptions.Clear()

  // ** worker

  let private worker (state: ClockState) () =
    while state.Run do
      if state.Publish && state.Run then
        let elapsed = state.Stopwatch.ElapsedTicks
        let diff = elapsed - state.Previous
        if diff >= ticksPerFrame state.Fps then // fire another clock event
          state.Previous <- elapsed
          state.Tick()

          // let deviation = (diff / μsPerTick) * 1L<ns>
          let ev = state.Frame |> uint32 |> UpdateClock

          let subscriptions = state.Subscriptions.ToArray()
          for KeyValue(_,obs) in subscriptions do
            (Origin.Service, ev) |> IrisEvent.Append |> obs.OnNext

      Thread.Sleep state.Timeout

  // ** create

  let create () =
    let state = new ClockState()

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

        member clock.Subscribe (callback: IrisEvent -> unit) =
          let listener = Observable.createListener state.Subscriptions
          { new IObserver<IrisEvent> with
              member self.OnCompleted() = ()
              member self.OnError(error) = ()
              member self.OnNext(value) = callback value }
          |> listener.Subscribe

        member clock.Dispose() =
          if not state.Disposed then
            dispose state }
