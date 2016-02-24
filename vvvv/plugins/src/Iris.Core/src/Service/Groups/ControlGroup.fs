namespace Iris.Service.Groups

open Vsync
open System.IO
open Iris.Core.Types
open Iris.Core.Utils
open Nessos.FsPickler


[<AutoOpen>]
module ControlGroup =

  type RepoJob =
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

    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Load   -> 1
          | Save   -> 2
          | Close  -> 3

  //    ____
  //   / ___|_ __ ___  _   _ _ __
  //  | |  _| '__/ _ \| | | | '_ \
  //  | |_| | | | (_) | |_| | |_) |
  //   \____|_|  \___/ \__,_| .__/
  //                        |_|
  type ControlGroup(context' : Context) as self =
    [<DefaultValue>] val mutable group   : IrisGroup<CtrlAction, RepoJob>
    [<DefaultValue>] val mutable context : Context

    let host = System.Guid.NewGuid().ToString();
    let ip = getIpAddress()

    let AddHandler(action : CtrlAction, cb : RepoJob -> unit) =
      self.group.AddHandler(action, cb)

    let AllHandlers =
      [ (CtrlAction.Load,  self.OnLoad)
      ; (CtrlAction.Save,  self.OnSave)
      ; (CtrlAction.Close, self.OnClose)
      ]

    do
      self.context <- context'
      self.group <- new IrisGroup<CtrlAction,RepoJob>("iris.control")
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    member self.Join() = self.group.Join()

    member self.Send(action : CtrlAction, raw : byte array) =
      self.group.Send((action :> IEnum).ToInt(), raw)

    member self.Initialize() =
      printfn "Initialize"

    member self.ViewChanged(view : View) =
      printfn "ViewChanged"

    member self.MakeCheckpoint(view : View) =
      printfn "MakeCheckpoint"

    member self.LoadCheckpoint(pth : RepoJob) =
      printfn "LoadCheckpoint"

    member self.OnLoad(job : RepoJob) =
      let pth = Path.Combine(Workspace(), job.Name)
      if File.Exists pth || Directory.Exists pth
      then
        match self.context.Project with
          | Some project ->
            if project.Name = job.Name
            then printfn "project already loaded."
            else
              self.context.StopDaemon()
              self.context.LoadProject(pth)
          | None -> self.context.LoadProject(pth)

      else
        printfn "address toString: %s" job.Host
        match cloneProject "localhost" job.Name with
          | Some pth -> printfn "%s" pth
          | None -> printfn "something went wrong"

    member self.OnSave(ctx : RepoJob) =
      printfn "OnSave"

    member self.OnClose(ctx : RepoJob) = 
      printfn "OnClose"
      
    (* load a project and notify everybody *)
    member self.LoadProject(name : string) =
      printfn "load: %s" name
      let pickler = FsPickler.CreateBinarySerializer()
      self.Send(CtrlAction.Load, pickler.Pickle({ Host = ip.ToString(); Name = name }))
