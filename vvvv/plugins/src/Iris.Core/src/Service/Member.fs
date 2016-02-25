namespace Iris.Service.Core

open Vsync
open System.Net

[<AutoOpen>]
module Member =

  type Member =
    { Name : string
    ; IP   : IPAddress
    }
  
    with
      override self.ToString() =
        sprintf "Name: %s IP: %s" self.Name (self.IP.ToString())
      
