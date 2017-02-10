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

//  ____  _                   _
// / ___|(_) __ _ _ __   __ _| |
// \___ \| |/ _` | '_ \ / _` | |
//  ___) | | (_| | | | | (_| | |
// |____/|_|\__, |_| |_|\__,_|_|
//          |___/

[<PluginInfo(Name="Signal", Category="Iris", AutoEvaluate=true)>]
type SignalNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Signal")>]
  val mutable InSignal: ISpread<Signal>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Size")>]
  val mutable OutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Coordinate")>]
  val mutable OutCoordinate: ISpread<ISpread<int>>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InSignal.[0]) then
            let config = self.InSignal.[n]
            self.OutSize.[n].[0] <- config.Size.X
            self.OutSize.[n].[1] <- config.Size.Y
            self.OutCoordinate.[n].[0] <- config.Position.X
            self.OutCoordinate.[n].[1] <- config.Position.Y
