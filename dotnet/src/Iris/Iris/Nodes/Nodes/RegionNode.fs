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

[<PluginInfo(Name="Region", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type RegionNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Region")>]
  val mutable InRegion: ISpread<Region>

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

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount <- self.InRegion.SliceCount
        self.OutName.SliceCount <- self.InRegion.SliceCount
        self.OutSrcSize.SliceCount <- self.InRegion.SliceCount
        self.OutSrcPos.SliceCount <- self.InRegion.SliceCount
        self.OutOutSize.SliceCount <- self.InRegion.SliceCount
        self.OutOutPos.SliceCount <- self.InRegion.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InRegion.[n]) then
            let region = self.InRegion.[n]
            self.OutId.[n] <- string region.Id
            self.OutName.[n] <- unwrap region.Name
            self.OutSrcSize.[n].SliceCount <- 2
            self.OutSrcSize.[n].[0] <- region.SrcSize.X
            self.OutSrcSize.[n].[1] <- region.SrcSize.Y
            self.OutSrcPos.[n].SliceCount <- 2
            self.OutSrcPos.[n].[0] <- region.SrcPosition.X
            self.OutSrcPos.[n].[1] <- region.SrcPosition.Y
            self.OutOutSize.[n].SliceCount <- 2
            self.OutOutSize.[n].[0] <- region.OutputSize.X
            self.OutOutSize.[n].[1] <- region.OutputSize.Y
            self.OutOutPos.[n].SliceCount <- 2
            self.OutOutPos.[n].[0] <- region.OutputPosition.X
            self.OutOutPos.[n].[1] <- region.OutputPosition.Y

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
