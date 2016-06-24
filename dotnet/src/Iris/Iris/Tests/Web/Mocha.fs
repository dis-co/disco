namespace Iris.Web.Tests

open System

[<AutoOpen>]
module Mocha =

  open Fable.Core
  open Fable.Import
  open Fable.Import.Browser

  [<Emit "throw new Error($0) ">]
  let bail (msg : string) : unit = failwith "ONLY JS"

  let success (cb : unit -> unit) : unit = cb ()

  let check (result : bool) (msg : string) : unit =
    if not result
    then bail msg

  let check_cc (result : bool) (msg : string) (cb : unit -> unit) : unit =
    if not result
    then bail msg
    else cb ()

  let (==>>) a b cb =
    check_cc (a = b) (sprintf "expected to be equal but %O /= %O" a b) cb

  let (/=>>) a b cb =
    check_cc (a <> b) (sprintf "expected to be different but %O == %O" a b) cb

  let (|==|) a b =
    check (a = b) (sprintf "expected to be equal but %O /= %O" a b)

  let (|/=|) a b =
    check (a <> b) (sprintf "expected to be different but %O == %O" a b)

  [<Emit "window.suite($0)">]
  let suite (desc : string) : unit = failwith "ONLY JS"

  [<Emit "window.test($0,$1)">]
  let test (str : string) (t : (unit -> unit) -> unit) : unit = failwith "ONLY JS"

  [<Emit "window.test($0)">]
  let pending (str : string) : unit = failwith "ONLY JS"

  [<Emit("window.resetPlugins()")>]
  let resetPlugins () = failwith "OH HAY JS"

  [<Emit("window.simpleString1()")>]
  let addString1Plug () = failwith "OH HAY JS"

  [<Emit("window.simpleString2()")>]
  let addString2Plug () = failwith "OH HAY JS"

  [<Emit("window.numberPlugin()")>]
  let addNumberPlug () = failwith "OH HAY JS"
