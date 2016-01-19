namespace WebSharper

#nowarn "1182"

open System
open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Mocha =

  [<Direct " throw new Error($msg) ">]
  let fail (msg : string) : unit = X<unit>

  let success (cb : unit -> unit) : unit = cb ()

  let check (result : bool) (msg : string) : unit =
    if not result
    then fail msg

  let check_cc (result : bool) (msg : string) (cb : unit -> unit) : unit =
    if not result
    then fail msg
    else cb ()

  let (==>>) a b cb =
    check_cc (a = b) (sprintf "expected to be equal but %O /= %O" a b) cb

  let (/=>>) a b cb =
    check_cc (a <> b) (sprintf "expected to be different but %O == %O" a b) cb

  let (|==|) a b =
    check (a = b) (sprintf "expected to be equal but %O /= %O" a b)

  let (|/=|) a b =
    check (a <> b) (sprintf "expected to be different but %O == %O" a b)

  [<Stub>]
  [<Name "window.suite">]
  let suite (desc : string) : unit = X<unit>

  [<Stub>]
  [<Name "window.test">]
  let test (str : string) (t : (unit -> unit) -> unit) : unit = X<unit>

  [<Stub>]
  [<Name "window.test">]
  let pending (str : string) : unit = X<unit>

