namespace Iris.Service.Types

open System
open Vsync

[<AutoOpen>]
module Groups =

  type Intable =
    abstract ToInt : unit -> int

  type Handler<'a,'b> = delegate of 'a -> 'b
 
  type IrisGroup<'a,'b>(name : string) =
    let group = new Vsync.Group(name)

    member self.AddHandler(i : Intable, v : Handler<'a,'b>) =
      group.Handlers.[i.ToInt()] <- group.Handlers.[i.ToInt()] + v

    member self.Join() =
      group.Join()

    member self.Send(i : Intable, thing : 'a) =
      group.Send(i.ToInt(), thing)
