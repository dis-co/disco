namespace Iris.Service.Groups

open Vsync
open System.IO
open System.Net
open Iris.Core.Types
open Iris.Core.Utils
open Iris.Service.Core
open Iris.Service.Contexts
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
    | Close
    | MemberAdd
    | MemberUpdate
    | MemberRemove

    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Load         -> 1
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
  type ControlGroup(context' : ServiceContext) as self =
    let tag = "ControlGroup"

    [<DefaultValue>] val mutable group   : IrisGroup<CtrlAction>
    [<DefaultValue>] val mutable context : ServiceContext

    let host = System.Guid.NewGuid().ToString();
    let ip = Option.get <| getIpAddress()

    let AddHandler (action : CtrlAction, cb : 'data -> unit) =
      self.group.AddHandler<'data>(action, cb)

    let ProjHandlers =
      [ (CtrlAction.Load,  self.OnLoad)
      ; (CtrlAction.Close, self.OnClose)
      ]

    let MemHandlers =
      [ (CtrlAction.MemberAdd,    self.OnMemberAdd)
      ; (CtrlAction.MemberUpdate, self.OnMemberUpdate)
      ; (CtrlAction.MemberRemove, self.OnMemberRemove)
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

    member self.Join()  =
      self.group.Join()
      self.group.Send<Member>(CtrlAction.MemberAdd, { Name = host; IP = ip })

    member self.Leave() =
      self.group.Send<Member>(CtrlAction.MemberRemove, { Name = host; IP = ip })
      self.group.Leave()

    member self.Initialize() =
      logger tag "Initialize"

    member self.ViewChanged(view : View) =
      logger tag "ViewChanged"

    member self.MakeCheckpoint(view : View) =
      logger tag "MakeCheckpoint"
      for mem in self.GetMembers() do
        self.group.SendCheckpoint<Member>(mem)
      self.group.DoneCheckpoint()

    member self.LoadCheckpoint(mem : Member) =
      logger tag "LoadCheckpoint"
      self.context.AddMember(mem)

    member self.OnLoad(job : ProjectMsg) =
      let pth = Path.Combine(Workspace(), job.Name)
      if File.Exists pth || Directory.Exists pth
      then
        if not <| self.context.ProjectLoaded(job.Name)
        then self.context.LoadProject(pth)
      else
        logger tag <| sprintf "address toString: %s" job.Host
        match cloneProject "localhost" job.Name with
          | Some pth -> logger tag <| sprintf "%s" pth
          | None -> logger tag"something went wrong"

    member self.OnClose(msg : ProjectMsg) = 
      self.context.CloseProject(msg.Name)

    member self.OnMemberAdd(mem : Member) =
      self.context.AddMember(mem)
      
    member self.OnMemberUpdate(mem : Member) =
      self.context.UpdateMember(mem)

    member self.OnMemberRemove(mem : Member) =
      self.context.RemoveMember(mem)

    member self.GetMembers() : Member array =
      self.context.GetMembers()
      
    (* load a project and notify everybody *)
    member self.LoadProject(name : string) =
      logger tag <| sprintf "load: %s" name
      self.group.Send<ProjectMsg>(CtrlAction.Load, { Host = ip.ToString(); Name = name })
