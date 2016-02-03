namespace Iris.Service.Groups

open Vsync
open Iris.Core.Types


[<AutoOpen>]
module ControlGroup =

  //      _        _   _
  //     / \   ___| |_(_) ___  _ __  ___
  //    / _ \ / __| __| |/ _ \| '_ \/ __|
  //   / ___ \ (__| |_| | (_) | | | \__ \
  //  /_/   \_\___|\__|_|\___/|_| |_|___/
  //
  [<RequireQualifiedAccess>]
  type CtrlActions =
    | Load
    | Save
    | Clone

    interface IEnum with
      member self.ToInt() : int =
        match self with
          | Load   -> 1
          | Save   -> 2
          | Clone  -> 3

  //    ____
  //   / ___|_ __ ___  _   _ _ __
  //  | |  _| '__/ _ \| | | | '_ \
  //  | |_| | | | (_) | |_| | |_) |
  //   \____|_|  \___/ \__,_| .__/
  //                        |_|
  type ControlGroup() as self =
    [<DefaultValue>] val mutable group   : IrisGroup<CtrlActions,Context>
    [<DefaultValue>] val mutable Context : Context

    let AddHandler(action : CtrlActions, cb : Context -> unit) =
      self.group.AddHandler(action, cb)

    let AllHandlers =
      [ (CtrlActions.Load,  self.OnLoad)
      ; (CtrlActions.Save,  self.OnSave)
      ; (CtrlActions.Clone, self.OnClone)
      ]

    do
      self.group <- new IrisGroup<CtrlActions,Context>("iris.control")
      self.group.AddInitializer(self.Initialize)
      self.group.AddViewHandler(self.ViewChanged)
      self.group.CheckpointMaker(self.MakeCheckpoint)
      self.group.CheckpointLoader(self.LoadCheckpoint)
      List.iter AddHandler AllHandlers

    member self.Initialize() =
      printfn "Initialize"

    member self.ViewChanged(view : View) =
      printfn "ViewChanged"

    member self.MakeCheckpoint(view : View) =
      printfn "MakeCheckpoint"

    member self.LoadCheckpoint(ctx : Context) =
      printfn "LoadCheckpoint"

    member self.OnLoad(ctx : Context) =
      printfn "OnLoad"

    member self.OnSave(ctx : Context) =
      printfn "OnSave"

    member self.OnClone(ctx : Context) =
      printfn "OnClone"
