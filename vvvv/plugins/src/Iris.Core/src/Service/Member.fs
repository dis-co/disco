namespace Iris.Service.Core

open Vsync
open System.Net
open Iris.Core.Types

[<AutoOpen>]
module Member =

  let private formatProject (project : ProjectData) : string =
    project.Path.ToString()
    |> sprintf "Name: %s Path: %s" project.Name

  type Member =
    { Name     : string
    ; IP       : IPAddress
    ; Projects : ProjectData array
    }
  
    with
      override self.ToString() =
        Array.map formatProject self.Projects
        |> Array.fold (fun m s -> m + "\n  " + s) ""
        |> sprintf "Name: %s IP: %s\n%s" self.Name (self.IP.ToString())
      
