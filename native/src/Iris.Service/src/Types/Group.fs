namespace Iris.Service.Types

open System
open Vsync

[<AutoOpen>]
module Groups =

  type Handler = delegate of byte[] -> unit

  let mkHandler (f : byte[] -> unit) = new Handler(f)

  type IrisGroup(name : string) =
    inherit Vsync.Group(name)

    member self.AddCheckpointMaker(handler : Vsync.View -> unit) =
      self.RegisterMakeChkpt(new Vsync.ChkptMaker(handler))

    member self.AddCheckpointLoader(handler : byte[] -> unit) =
      self.RegisterLoadChkpt(mkHandler handler)

    member self.AddViewHandler(handler : Vsync.View -> unit) =
      self.ViewHandlers <- self.ViewHandlers + new Vsync.ViewHandler(handler)

    member self.AddHandler(action : int, v : Handler) =
      self.Handlers.[action] <- self.Handlers.[action] + v
