namespace Iris.Service.Types

open System
open Vsync

[<AutoOpen>]
module Groups =

  type Handler = delegate of byte[] -> unit

  let mkHandler (f : byte[] -> unit) = new Handler(f)

  type IrisGroup(name : string) =
    inherit Vsync.Group(name)

    member self.AddViewHandler(handler : Vsync.View -> unit) =
      self.ViewHandlers <- self.ViewHandlers + new Vsync.ViewHandler(handler)

    member self.AddHandler(action : int, v : Handler) =
      self.Handlers.[action] <- self.Handlers.[action] + v
