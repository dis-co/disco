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

//  _____                     _        _     _
// | ____|_  _____  ___ _   _| |_ __ _| |__ | | ___
// |  _| \ \/ / _ \/ __| | | | __/ _` | '_ \| |/ _ \
// | |___ >  <  __/ (__| |_| | || (_| | |_) | |  __/
// |_____/_/\_\___|\___|\__,_|\__\__,_|_.__/|_|\___|

[<PluginInfo(Name="VvvvExe", Category="Iris", AutoEvaluate=true)>]
type VvvvExeNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("VvvvExe")>]
  val mutable InExe: ISpread<VvvvExe>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Executable")>]
  val mutable OutExecutable: ISpread<string>

  [<DefaultValue>]
  [<Output("Version")>]
  val mutable OutVersion: ISpread<string>

  [<DefaultValue>]
  [<Output("Required")>]
  val mutable OutRequired: ISpread<bool>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutExecutable.SliceCount <- self.InExe.SliceCount
        self.OutVersion.SliceCount <- self.InExe.SliceCount
        self.OutRequired.SliceCount <- self.InExe.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InExe.[n]) then
            let config = self.InExe.[n]
            self.OutExecutable.[n] <- config.Executable
            self.OutVersion.[n] <- config.Version
            self.OutRequired.[n] <- config.Required

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
