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

[<PluginInfo(Name="VvvvConfig", Category="Iris", AutoEvaluate=true)>]
type VvvvConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Vvvv", IsSingle = true)>]
  val mutable InVvvv: ISpread<VvvvConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Executables")>]
  val mutable OutExecutables: ISpread<VvvvExe>

  [<DefaultValue>]
  [<Output("Plugins")>]
  val mutable OutPlugins: ISpread<VvvvPlugin>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InVvvv.[0]) then
        let config = self.InVvvv.[0]
        self.OutExecutables.AssignFrom config.Executables
        self.OutExecutables.AssignFrom config.Executables
