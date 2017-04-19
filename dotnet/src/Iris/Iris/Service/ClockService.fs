namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Diagnostics
open System.Collections.Concurrent

open Iris.Core

// * Types

type ClockEvent =
  { Frame: uint64
    Deviation: int64 }

type IClock =
  inherit IDisposable
  abstract Subscribe: (ClockEvent -> unit) -> IDisposable
  abstract Start: unit -> unit
  abstract Stop: unit -> unit
  abstract Running: bool with get
  abstract Fps: uint16   with get, set
  abstract Frame: uint64

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

  let private secPerFrame (fps: uint16) = 1. / float fps

  let private μsPerFrame (fps: uint16) =
    secPerFrame fps
    * 1000. (* ms *)
    * 1000. (* μs *)
    * 1000. (* ns *)
    |> int64

  let μsPerTick =
    (1000L (* ms *) * 1000L (* μs *) * 1000L (* ns *))
    / Stopwatch.Frequency
    |> int64

  let ticksPerFrame (fps: uint16) =
    μsPerFrame fps / μsPerTick

  // ** calculateTimeout

  let private calculateTimeout (fps: uint16) =
    fps * 8us                           // sample the time 8x more often than required
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
    let mutable frame = 0UL
    let mutable fps = 60us
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
      frame <- frame + 1UL

    interface IDisposable with
      member self.Dispose() =
        disposed <- true
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
          notify state { Frame = state.Frame
                         Deviation = diff / μsPerTick }
      Thread.Sleep state.Timeout

  // ** create

  let create () =
    let state = new ClockState()
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

// * Playground

#if INTERACTIVE

let clock = Clock.create()
let disp = clock.Subscribe (fun (ev: ClockEvent) -> printfn "t: %O" ev.Frame)

clock.Start()
clock.Stop()
clock.Dispose()



#endif
