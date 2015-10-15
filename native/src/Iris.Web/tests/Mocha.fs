[<ReflectedDefinition>]
module FunScript.Mocha

#nowarn "1182"

open FunScript 
open FunScript.TypeScript

[<JSEmit(""" if(!{0}) { throw new Error({1}) } else { ({2})(); } """)>]
let check (res : bool) (msg : string) (cb : unit -> unit) : unit = failwith "never"

[<JSEmit(""" suite({0}) """)>]
let suite (desc : string) : unit = failwith "never "

[<JSEmit(""" test({0}, {1}) """)>]
let test (str : string) (f : (unit -> unit) -> unit) : unit = failwith "never"
