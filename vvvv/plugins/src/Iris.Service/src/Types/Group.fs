namespace Iris.Service.Types

open System
open Nessos.FsPickler
open Vsync

[<AutoOpen>]
module Groups =

  type Handler<'a> = 'a -> unit

  type private RawHandler = delegate of byte[] -> unit

  let private mkRawHandler (f : byte[] -> unit) = new RawHandler(f)

  type IEnum =
    abstract member ToInt : unit -> int
    
  type IrisGroup<'action,'data when 'action :> IEnum>(name : string) =
    inherit Vsync.Group(name)

    let pickler = FsPickler.CreateBinarySerializer()

    member self.Send(action : 'action, thing : 'data) : unit =
      self.Send(action.ToInt(), pickler.Pickle(thing))

    member self.ToBytes(thing : 'data) : byte[] =
      pickler.Pickle(thing)

    member self.FromBytes(data : byte[]) : 'data =
      pickler.UnPickle<'data>(data)

    member self.CheckpointMaker(handler : Vsync.View -> unit) =
      self.RegisterMakeChkpt(new Vsync.ChkptMaker(handler))

    member self.CheckpointLoader(handler : 'data -> unit) =
      let wrapped = fun data -> self.FromBytes(data) |> handler
       in self.RegisterLoadChkpt(mkRawHandler wrapped)

    member self.SendCheckpoint(thing : 'data) =
      self.SendChkpt(self.ToBytes(thing))

    member self.DoneCheckpoint() =
      self.EndOfChkpt()

    member self.AddViewHandler(handler : Vsync.View -> unit) =
      self.ViewHandlers <- self.ViewHandlers + new Vsync.ViewHandler(handler)

    member self.AddInitializer(handler : unit -> unit) =
      self.RegisterInitializer(new Vsync.Initializer(handler))

    member self.AddHandler(action : 'action, handler : Handler<'data>) =
      let wrapped = mkRawHandler <| fun data ->
        handler <| self.FromBytes(data)
      in self.Handlers.[action.ToInt()] <- self.Handlers.[action.ToInt()] + wrapped
