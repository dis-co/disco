namespace Iris.Service.Core

open Vsync
open System.Net

[<AutoOpen>]
module Member =

  type Member =
    { Name    : string
    ; Address : Address
    ; IP      : IPAddress
    }
  
