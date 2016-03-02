namespace Iris.Service.Core

open Vsync
open System
open System.Net
open Iris.Core.Types

[<AutoOpen>]
module Member =

  let private formatProject (pid : Guid, name : string) : string =
    sprintf "Project Id: %s Name: %s" (pid.ToString()) name

  type Member =
    { MemberId : Guid
    ; Name     : string
    ; IP       : IPAddress
    ; Projects : (Guid * string) array
    }
  
    with
      override self.ToString() =
        Array.map formatProject self.Projects
        |> Array.fold (fun m s -> m + "\n  " + s) ""
        |> sprintf "Id: %s Name: %s IP: %s\n%s" (self.MemberId.ToString()) self.Name (self.IP.ToString())

  let sameAs mem1 mem2 : bool =
    mem1.MemberId = mem2.MemberId
