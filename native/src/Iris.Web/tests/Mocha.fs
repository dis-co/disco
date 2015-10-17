[<ReflectedDefinition>]
module FunScript.Mocha

#nowarn "1182"

open FunScript 
open FunScript.TypeScript

[<JSEmit(""" if(!{0}) { throw new Error({1}) } """)>]
let check (res : bool) (msg : string) : unit = failwith "never"

[<JSEmit(""" if(!{0}) { throw new Error({1}) } else { ({2})(); } """)>]
let check_cc (res : bool) (msg : string) (cb : unit -> unit) : unit = failwith "never"

[<JSEmit(""" suite({0}) """)>]
let suite (desc : string) : unit = failwith "never "

[<JSEmit(""" test({0}, {1}) """)>]
let test (str : string) (f : (unit -> unit) -> unit) : unit = failwith "never"

[<JSEmit(""" test({0}) """)>]
let pending (str : string) : unit = failwith "never"

[<JSEmit(""" throw new Error({1}) """)>]
let fail (msg : string) : unit = failwith "never"


[<JSEmit(""" test({0}, {1}) """)>]
let withTestImpl (str : string) (t : (unit -> unit) -> unit) : unit = failwith "never"

let withTest (name : string) (t : unit -> unit) =
  let worker (cont : unit -> unit, econt : exn -> unit, ccont : System.OperationCanceledException -> unit) : unit = 
    let wrapper (cb : unit -> unit) =
      (* error handling & order not*)
      t ()
      cb ()
      cont ()
    withTestImpl name wrapper 

  Async.FromContinuations(worker) 
