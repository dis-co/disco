namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module Crypto =

  [<Direct "asmCrypto.SHA1.hex($input)">]
  let sha1sum (input : string) : string = X

  [<Direct "asmCrypto.SHA256.hex($input)">]
  let sha256sum (input : string) : string = X
