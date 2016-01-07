namespace Iris.Service.Types

open System
open Vsync

[<AutoOpen>]
module Groups =

  type Intable<'a> =
    abstract ToInt : unit -> int

  type Handler<'a> = delegate of 'a -> unit
 
  type IrisGroup<'a,'b>(name : string) =
    let group = new Vsync.Group(name)

    member self.AddHandler(i : Intable<'a>, v : Handler<'b>) =
      group.Handlers.[i.ToInt()] <- group.Handlers.[i.ToInt()] + v

    member self.Join() =
      group.Join()

    member self.Send(i : Intable<'a>, thing : 'b) =
      group.Send(i.ToInt(), thing)
