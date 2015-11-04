namespace WebSharper

#nowarn "1182"

open System
open WebSharper
open WebSharper.JavaScript

[<ReflectedDefinition>]
module Mocha =

  [<Direct " if(!$res) { throw new Error($msg) } ">]
  let check (res : bool) (msg : string) : unit = X<unit>

  [<Direct " if(!$res) { throw new Error($res) } else { ($cb)(); } ">]
  let check_cc (res : bool) (msg : string) (cb : unit -> unit) : unit = X<unit>

  [<Direct " suite($desc) ">]
  let suite (desc : string) : unit = X<unit>

  [<Direct " test($str, $t) ">]
  let test (str : string) (t : (unit -> unit) -> unit) : unit = X<unit>

  [<Direct " test($str) ">]
  let pending (str : string) : unit = X<unit>

  [<Direct " throw new Error($msg) ">]
  let fail (msg : string) : unit = X<unit>

  (*----------------------------------------------------------------------------*)

  [<Direct(""" test($str, $t) """)>]
  let withTestImpl (str : string) (t : (unit -> unit) -> unit) : unit = X<unit>

  let withTest (name : string) (t : unit -> unit) =
    let worker (cont : unit -> unit, econt : exn -> unit, ccont : OperationCanceledException -> unit) : unit =
      let wrapper (cb : unit -> unit) =
        t () // test
        cb () // continue if successful
        cont () // call continuation
      withTestImpl name wrapper
    Async.FromContinuations(worker)
