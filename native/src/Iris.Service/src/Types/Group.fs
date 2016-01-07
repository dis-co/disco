namespace Iris.Service.Types

open System
open Vsync

[<AutoOpen>]
module Groups =

  type Intable<'a> =
    abstract ToInt : unit -> int

  type Handler = delegate of byte[] -> unit
 
  type IrisGroup<'a>(name : string) =
    inherit Vsync.Group(name)

    member self.AddViewHandler(handler : Vsync.View -> unit) =
      self.ViewHandlers <- self.ViewHandlers + new Vsync.ViewHandler(handler)

    member self.AddHandler(i : Intable<'a>, v : Handler) =
      self.Handlers.[i.ToInt()] <- self.Handlers.[i.ToInt()] + v

    member self.MySend(i : Intable<'a>, thing : byte[]) =
      self.Send(i.ToInt(), thing)
