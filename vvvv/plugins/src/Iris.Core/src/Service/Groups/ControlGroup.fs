namespace Iris.Service.Groups

open Vsync
open System
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
    ; Id   : Guid
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
  type ControlGroup(state : AppState) as self =
    let tag = "ControlGroup"

    [<DefaultValue>] val mutable group : VsyncGroup<CtrlAction>

    let host = System.Guid.NewGuid();
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
      self.group <- new VsyncGroup<CtrlAction>("iris.control")
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)

      List.iter AddHandler ProjHandlers
      List.iter AddHandler MemHandlers

    member self.Join()  =
      self.group.Join()
      let mem =
        { MemberId = host
        ; IP       = ip
        ; Name     = "Iris ControlGroup"
        ; Projects = Array.empty }
      self.group.Send<Member>(CtrlAction.MemberAdd, mem)

    member self.Leave() =
      let mem =
        { MemberId = host
        ; IP       = ip
        ; Name     = "Iris ControlGroup"
        ; Projects = Array.empty }
      self.group.Send<Member>(CtrlAction.MemberRemove, mem)
      self.group.Leave()

    member self.Initialize() =
      logger tag "Initialize"

    member self.ViewChanged(view : View) =
      logger tag "ViewChanged"

    member self.MakeCheckpoint(view : View) =
      for mem in state.Members do
        self.group.SendCheckpoint<Member>(mem)
      self.group.DoneCheckpoint()

    member self.LoadCheckpoint(mem : Member) =
      self.OnMemberAdd(mem)

    //            __
    //        ____\ \
    // Local |_____\ \  
    //       |_____/ / Remote
    //            /_/
       
    member self.Load(pid : Guid, name) =
      logger tag <| sprintf "Please load: %s %s" (pid.ToString()) name
      let msg =
        { Id   = pid
        ; Name = name
        ; Host = ip.ToString()
        }
      self.group.Send<ProjectMsg>(CtrlAction.Load, msg)

    member self.Close(pid, name) =
      logger tag <| sprintf "Please close: %s %s" (pid.ToString()) name
      let msg = 
        { Id   = pid
        ; Name = name
        ; Host = ip.ToString()
        }
      self.group.Send<ProjectMsg>(CtrlAction.Close, msg)

    //         __
    //        / /____
    // Local / /_____|
    //       \ \_____| Remote
    //        \_\

    member self.OnLoad(msg : ProjectMsg) =
      sprintf "[OnLoad] Id: %s Name: %s" (msg.Id.ToString()) msg.Name
      |> logger tag 
      // let pth = Path.Combine(Workspace(), msg.Name)
      // if File.Exists pth || Directory.Exists pth
      // then
      //   if not <| state.Loaded msg.Name
      //   then state.Load(pth) |> ignore
      // else
      //   logger tag <| sprintf "address toString: %s" msg.Host
      //   match Project.Clone("localhost", msg.Name) with
      //     | Some pth -> logger tag <| sprintf "%s" pth
      //     | None -> logger tag"something went wrong"

    member self.OnClose(msg : ProjectMsg) =
      sprintf "[OnClose] Id: %s Name: %s" (msg.Id.ToString()) msg.Name
      |> logger tag 

    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __ ___
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
    // | |  | |  __/ | | | | | |_) |  __/ |  \__ \
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|  |___/
    //
    member self.OnMemberAdd(mem : Member) =
      state.Add(mem)

    member self.OnMemberUpdate(mem : Member) =
      state.Update(mem)

    member self.OnMemberRemove(mem : Member) =
      state.Remove(mem)
