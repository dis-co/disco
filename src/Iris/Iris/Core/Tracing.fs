namespace Iris.Core

module Tracing =
  open System
  open System.Diagnostics

  let mutable private on = false
  let private lockobj = Object()

  let enable() = on <- true
  let disable() = on <- false

  let trace (tag: string) (f: unit -> 'b) =
    #if !FABLE_COMPILER
    let env =
      match Environment.GetEnvironmentVariable "IRIS_TRACING" with
      | "true" -> true
      | _ -> false

    if on || env then
      let stop = Stopwatch()
      stop.Start()
      let result = f()
      stop.Stop()
      lock lockobj <| fun _ ->
        printfn "[%s] %s" tag (sprintf "took %dms" stop.ElapsedMilliseconds)
      result
    else f()
    #else
    f()
    #endif
