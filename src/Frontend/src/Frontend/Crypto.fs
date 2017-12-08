namespace Disco.Web.Core

open Fable.Core
open Fable.Import

[<AutoOpen>]
module Crypto =

  [<Emit "asmCrypto.SHA1.hex($input)">]
  let sha1sum (_: string) : string = failwith "JS Only"

  [<Emit "asmCrypto.SHA256.hex($input)">]
  let sha256sum (_: string) : string = failwith "JS Only"
