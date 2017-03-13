namespace VVVV.Nodes

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Raft
open Iris.Core
open Iris.Nodes

// __     __
// \ \   / /_   ____   ____   __
//  \ \ / /\ \ / /\ \ / /\ \ / /
//   \ V /  \ V /  \ V /  \ V /
//    \_/    \_/    \_/    \_/

[<PluginInfo(Name="VvvvConfig", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type VvvvConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Vvvv", IsSingle = true)>]
  val mutable InVvvv: ISpread<VvvvConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Executables")>]
  val mutable OutExecutables: ISpread<VvvvExe>

  [<DefaultValue>]
  [<Output("Plugins")>]
  val mutable OutPlugins: ISpread<VvvvPlugin>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if self.InUpdate.[0] && not (Util.isNullReference self.InVvvv.[0]) then
        let config = self.InVvvv.[0]
        self.OutExecutables.SliceCount <- (Array.length config.Executables)
        self.OutPlugins.SliceCount <- (Array.length config.Executables)
        self.OutExecutables.AssignFrom config.Executables
        self.OutPlugins.AssignFrom config.Plugins

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
