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

//     _             _ _
//    / \  _   _  __| (_) ___
//   / _ \| | | |/ _` | |/ _ \
//  / ___ \ |_| | (_| | | (_) |
// /_/   \_\__,_|\__,_|_|\___/

[<PluginInfo(Name="AudioConfig", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type AudioConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Audio", IsSingle = true)>]
  val mutable InAudio: ISpread<AudioConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("SampleRate", IsSingle = true)>]
  val mutable OutSampleRate: ISpread<int>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InAudio.[n]) then
            let config = self.InAudio.[n]
            self.OutSampleRate.SliceCount <- self.InAudio.SliceCount
            self.OutSampleRate.[n] <- int config.SampleRate

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
