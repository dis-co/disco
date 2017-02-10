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

//  ____            _
// |  _ \ ___  __ _(_) ___  _ __
// | |_) / _ \/ _` | |/ _ \| '_ \
// |  _ <  __/ (_| | | (_) | | | |
// |_| \_\___|\__, |_|\___/|_| |_|
//            |___/

[<PluginInfo(Name="Region", Category="Iris", AutoEvaluate=true)>]
type RegionNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Region")>]
  val mutable InRegion: ISpread<Region>

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
  [<Output("Source Position")>]
  val mutable OutSrcPos: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Source Size")>]
  val mutable OutSrcSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Output Size")>]
  val mutable OutOutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Output Position")>]
  val mutable OutOutPos: ISpread<ISpread<int>>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InRegion.[n]) then
            let config = self.InRegion.[n]
            self.OutId.[n] <- string config.Id
            self.OutName.[n] <- config.Name
            self.OutSrcSize.[n].[0] <- config.SrcSize.X
            self.OutSrcSize.[n].[1] <- config.SrcSize.Y
            self.OutSrcPos.[n].[0] <- config.SrcPosition.X
            self.OutSrcPos.[n].[1] <- config.SrcPosition.Y
            self.OutOutSize.[n].[0] <- config.OutputSize.X
            self.OutOutSize.[n].[1] <- config.OutputSize.Y
            self.OutOutPos.[n].[0] <- config.OutputPosition.X
            self.OutOutPos.[n].[1] <- config.OutputPosition.Y
