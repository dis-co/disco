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

[<PluginInfo(Name="AudioConfig", Category="Iris", AutoEvaluate=true)>]
type AudioConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Audio", IsSingle = true)>]
  val mutable InAudio: ISpread<AudioConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("SampleRate", IsSingle = true)>]
  val mutable OutSampleRate: ISpread<int>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InAudio.[0]) then
        let config = self.InAudio.[0]
        self.OutSampleRate.[0] <- int config.SampleRate
