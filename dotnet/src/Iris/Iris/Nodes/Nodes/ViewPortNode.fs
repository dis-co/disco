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

// __     ___               ____            _
// \ \   / (_) _____      _|  _ \ ___  _ __| |_
//  \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
//   \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
//    \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|

[<PluginInfo(Name="ViewPort", Category="Iris", AutoEvaluate=true)>]
type ViewPortNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("ViewPort")>]
  val mutable InViewPort: ISpread<ViewPort>

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
  [<Output("Position")>]
  val mutable OutPosition: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Size")>]
  val mutable OutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Output Position")>]
  val mutable OutOutPosition: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Output Size")>]
  val mutable OutOutSize: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Overlap")>]
  val mutable OutOverlap: ISpread<ISpread<int>>

  [<DefaultValue>]
  [<Output("Description")>]
  val mutable OutDescription: ISpread<string>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount <- self.InViewPort.SliceCount
        self.OutName.SliceCount <- self.InViewPort.SliceCount
        self.OutPosition.SliceCount <- self.InViewPort.SliceCount
        self.OutSize.SliceCount <- self.InViewPort.SliceCount
        self.OutOutPosition.SliceCount <- self.InViewPort.SliceCount
        self.OutOutSize.SliceCount <- self.InViewPort.SliceCount
        self.OutOverlap.SliceCount <- self.InViewPort.SliceCount
        self.OutDescription.SliceCount <- self.InViewPort.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InViewPort.[n]) then
            let config = self.InViewPort.[n]
            self.OutId.[n] <- string config.Id
            self.OutName.[n] <- config.Name
            self.OutPosition.[n].SliceCount <- 2
            self.OutPosition.[n].[0] <- config.Position.X
            self.OutPosition.[n].[1] <- config.Position.Y
            self.OutSize.[n].SliceCount <- 2
            self.OutSize.[n].[0] <- config.Size.X
            self.OutSize.[n].[1] <- config.Size.Y
            self.OutOutPosition.[n].SliceCount <- 2
            self.OutOutPosition.[n].[0] <- config.OutputPosition.X
            self.OutOutPosition.[n].[1] <- config.OutputPosition.Y
            self.OutOutSize.[n].SliceCount <- 2
            self.OutOutSize.[n].[0] <- config.OutputSize.X
            self.OutOutSize.[n].[1] <- config.OutputSize.Y
            self.OutOverlap.[n].SliceCount <- 2
            self.OutOverlap.[n].[0] <- config.Overlap.X
            self.OutOverlap.[n].[1] <- config.Overlap.Y
            self.OutDescription.[n] <- config.Description

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
