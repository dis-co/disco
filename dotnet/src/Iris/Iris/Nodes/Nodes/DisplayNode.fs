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

//  ____  _           _
// |  _ \(_)___ _ __ | | __ _ _   _
// | | | | / __| '_ \| |/ _` | | | |
// | |_| | \__ \ |_) | | (_| | |_| |
// |____/|_|___/ .__/|_|\__,_|\__, |
//             |_|            |___/

[<PluginInfo(Name="Display", Category="Iris", AutoEvaluate=true)>]
type DisplayNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Display")>]
  val mutable InDisplay: ISpread<Display>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Size")>]
  val mutable OutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Signals")>]
  val mutable OutSignals: ISpread<ISpread<Signal>>

  [<DefaultValue>]
  [<Output("RegionMap")>]
  val mutable OutRegionMap: ISpread<RegionMap>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InDisplay.[n]) then
            let config = self.InDisplay.[n]
            self.OutId.[n] <- string config.Id
            self.OutName.[n] <- config.Name
            self.OutSize.[n].[0] <- config.Size.X
            self.OutSize.[n].[1] <- config.Size.Y
            self.OutSignals.[n].AssignFrom config.Signals
            self.OutRegionMap.[n] <- config.RegionMap
