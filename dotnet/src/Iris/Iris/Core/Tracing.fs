namespace Iris.Core

module Tracing =
  open System.Diagnostics

  let mutable private on = false

  let enable() = on <- true
  let disable() = on <- true

  let trace (tag: string) (f: unit -> 'b) =
    #if !FABLE_COMPILER
    if on then
      let stop = new Stopwatch()
      stop.Start()
      let result = f()
      stop.Stop()
      Logger.trace tag ("took " + string stop.ElapsedMilliseconds + "ms")
      result
    else f()
    #else
    f()
    #endif
