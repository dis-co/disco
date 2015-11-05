namespace WebSharper

#nowarn "1182"

open System
open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Mocha =

  [<Direct " throw new Error($msg) ">]
  let fail (msg : string) : unit = X<unit>

  let check (result : bool) (msg : string) : unit =
    if not result
    then fail msg

  let check_cc (result : bool) (msg : string) (cb : unit -> unit) : unit =
    if not result
    then fail msg
    else cb ()

  [<Stub>]
  [<Name "window.suite">]
  let suite (desc : string) : unit = X<unit>

  [<Stub>]
  [<Name "window.test">]
  let test (str : string) (t : (unit -> unit) -> unit) : unit = X<unit>

  [<Stub>]
  [<Name "window.test">]
  let pending (str : string) : unit = X<unit>

