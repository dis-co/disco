namespace Iris.Web.Tests

open System

[<AutoOpen>]
module Mocha =

  open Fable.Core
  open Fable.Core.JsInterop
  open Fable.Import
  open Fable.Import.Browser

  let success (cb : unit -> unit) : unit = cb ()

  let inline equals (expectation: 'a) (value: 'a) : unit =
    Fable.Core.Testing.Assert.AreEqual(expectation, value)

  [<Emit "window.suite($0)">]
  let suite (desc : string) : unit = failwith "JS only"

  [<Emit "window.test($0,$1)">]
  let test (str : string) (t : (unit -> unit) -> unit) : unit = failwith "JS only"

  [<Emit "window.test($0,$1)">]
  let testSync (str : string) (t : unit -> unit) : unit = failwith "JS only"

  [<Emit "window.test($0)">]
  let pending (str : string) : unit = failwith "JS only"

  [<Emit("window.resetPlugins()")>]
  let resetPlugins () = failwith "JS only"

  [<Emit("window.simpleString1()")>]
  let addString1Plug () = failwith "JS only"

  [<Emit("window.simpleString2()")>]
  let addString2Plug () = failwith "JS only"

  [<Emit("window.numberPlugin()")>]
  let addNumberPlug () = failwith "JS only"
