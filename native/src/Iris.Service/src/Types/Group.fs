namespace Iris.Service.Types

open System
open Vsync

[<AutoOpen>]
module Groups =

  type Intable<'a> =
    abstract ToInt : unit -> int

  type Handler<'a> = delegate of 'a -> unit
 
  type IrisGroup<'a,'b>(name : string) =
    inherit Vsync.Group(name)

    member self.AddViewHandler(handler : Vsync.View -> unit) =
      self.ViewHandlers <- self.ViewHandlers + new Vsync.ViewHandler(handler)

    member self.AddHandler(i : Intable<'a>, v : Handler<'b>) =
      self.Handlers.[i.ToInt()] <- self.Handlers.[i.ToInt()] + v

    member self.Send(i : Intable<'a>, thing : 'b) =
      self.Send(i.ToInt(), thing)
