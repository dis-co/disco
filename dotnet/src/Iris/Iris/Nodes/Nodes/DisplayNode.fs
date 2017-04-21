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

[<PluginInfo(Name="Display", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type DisplayNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Display")>]
  val mutable InDisplay: ISpread<Display>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

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

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount <- self.InDisplay.SliceCount
        self.OutName.SliceCount <- self.InDisplay.SliceCount
        self.OutSize.SliceCount <- self.InDisplay.SliceCount
        self.OutSignals.SliceCount <- self.InDisplay.SliceCount
        self.OutRegionMap.SliceCount <- self.InDisplay.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InDisplay.[n]) then
            let display = self.InDisplay.[n]
            self.OutId.[n] <- string display.Id
            self.OutName.[n] <- unwrap display.Name
            self.OutSize.[n].SliceCount <- 2
            self.OutSize.[n].[0] <- display.Size.X
            self.OutSize.[n].[1] <- display.Size.Y
            self.OutSignals.[n].SliceCount <- Array.length display.Signals
            self.OutSignals.[n].AssignFrom display.Signals
            self.OutRegionMap.[n] <- display.RegionMap

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
