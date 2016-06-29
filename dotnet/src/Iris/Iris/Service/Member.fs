namespace Iris.Service.Core

open System
open System.Net
open Iris.Core

type Member =
  { MemberId : Guid
  ; Name     : string
  ; IP       : IPAddress
  ; Projects : (Guid * string) array
  }

  with
    static member FormatProject (pid : Guid, name : string) : string =
      sprintf "Project Id: %s Name: %s" (pid.ToString()) name

    static member SameAs mem1 mem2 : bool =
      mem1.MemberId = mem2.MemberId

    override self.ToString() =
      Array.map Member.FormatProject self.Projects
      |> Array.fold (fun m s -> m + "\n  " + s) ""
      |> sprintf "Id: %s Name: %s IP: %s\n%s" (self.MemberId.ToString()) self.Name (self.IP.ToString())
