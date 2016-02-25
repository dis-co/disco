namespace Iris.Service.Groups

open Vsync
open System.IO
open System.Net
open Iris.Core.Types
open Iris.Core.Utils
open Iris.Service.Core
open Nessos.FsPickler


[<AutoOpen>]
module ControlGroup =

  type ProjectMsg =
    { Host : string
    ; Name : string
    }

  //      _        _   _
  //     / \   ___| |_(_) ___  _ __  ___
  //    / _ \ / __| __| |/ _ \| '_ \/ __|
  //   / ___ \ (__| |_| | (_) | | | \__ \
  //  /_/   \_\___|\__|_|\___/|_| |_|___/
  //
  [<RequireQualifiedAccess>]
  type CtrlAction =
    | Load
    | Save
    | Close
    | MemberAdd
    | MemberUpdate
    | MemberRemove

    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Load         -> 1
          | Save         -> 2
          | Close        -> 3
          | MemberAdd    -> 4
          | MemberUpdate -> 5
          | MemberRemove -> 6

  //    ____
  //   / ___|_ __ ___  _   _ _ __
  //  | |  _| '__/ _ \| | | | '_ \
  //  | |_| | | | (_) | |_| | |_) |
  //   \____|_|  \___/ \__,_| .__/
  //                        |_|
  type ControlGroup(context' : Context) as self =
    let tag = "ControlGroup"

    [<DefaultValue>] val mutable group   : IrisGroup<CtrlAction>
    [<DefaultValue>] val mutable context : Context

    let host = System.Guid.NewGuid().ToString();
    let ip = getIpAddress()

    let AddHandler (action : CtrlAction, cb : 'data -> unit) =
      self.group.AddHandler<'data>(action, cb)

    let ProjHandlers =
      [ (CtrlAction.Load,  self.OnLoad)
      ; (CtrlAction.Save,  self.OnSave)
      ; (CtrlAction.Close, self.OnClose)
      ]

    let MemHandlers =
      [ (CtrlAction.MemberAdd,    self.OnMemberAdd)
      ; (CtrlAction.MemberUpdate, self.OnMemberUpdate)
      ; (CtrlAction.MemberAdd,    self.OnMemberRemove)
      ]

    do
      self.context <- context'
      self.group <- new IrisGroup<CtrlAction>("iris.control")
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler ProjHandlers
      List.iter AddHandler MemHandlers

    member self.Join()  = self.group.Join()
    member self.Leave() = self.group.Leave()

    member self.Send(action : CtrlAction, raw : byte array) =
      self.group.Send((action :> IEnum).ToInt(), raw)

    member self.Initialize() =
      logger tag "Initialize"

    member self.ViewChanged(view : View) =
      logger tag "ViewChanged"

    member self.MakeCheckpoint(view : View) =
      logger tag "MakeCheckpoint"

    member self.LoadCheckpoint(pth : ProjectMsg) =
      logger tag "LoadCheckpoint"

    member self.OnLoad(job : ProjectMsg) =
      let pth = Path.Combine(Workspace(), job.Name)
      if File.Exists pth || Directory.Exists pth
      then
        match self.context.Project with
          | Some project ->
            if project.Name = job.Name
            then logger tag"project already loaded."
            else
              self.context.StopDaemon()
              self.context.LoadProject(pth)
          | None -> self.context.LoadProject(pth)
      else
        logger tag <| sprintf "address toString: %s" job.Host
        match cloneProject "localhost" job.Name with
          | Some pth -> logger tag <| sprintf "%s" pth
          | None -> logger tag"something went wrong"

    member self.OnSave(ctx : ProjectMsg) =
      logger tag "OnSave"

    member self.OnClose(ctx : ProjectMsg) = 
      logger tag "OnClose"

    member self.OnMemberAdd(mem : Member) =
      logger tag <| sprintf "OnMemberAdd %s" mem.Name
      
    member self.OnMemberUpdate(mem : Member) =
      logger tag <| sprintf "OnMemberUpdate %s" mem.Name

    member self.OnMemberRemove(mem : Member) =
      logger tag <| sprintf "OnMemberRemove %s" mem.Name

    member self.GetMembers() : Member array =
      Array.empty
      
    (* load a project and notify everybody *)
    member self.LoadProject(name : string) =
      logger tag <| sprintf "load: %s" name
      let pickler = FsPickler.CreateBinarySerializer()
      self.Send(CtrlAction.Load, pickler.Pickle({ Host = ip.ToString(); Name = name }))
