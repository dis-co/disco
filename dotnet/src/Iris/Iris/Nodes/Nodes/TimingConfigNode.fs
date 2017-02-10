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

//  _____ _           _
// |_   _(_)_ __ ___ (_)_ __   __ _
//   | | | | '_ ` _ \| | '_ \ / _` |
//   | | | | | | | | | | | | | (_| |
//   |_| |_|_| |_| |_|_|_| |_|\__, |
//                            |___/

[<PluginInfo(Name="TimingConfig", Category="Iris", AutoEvaluate=true)>]
type TimingConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Timing")>]
  val mutable InTiming: ISpread<TimingConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Framebase")>]
  val mutable OutFramebase: ISpread<int>

  [<DefaultValue>]
  [<Output("Input")>]
  val mutable OutInput: ISpread<string>

  [<DefaultValue>]
  [<Output("Servers")>]
  val mutable OutServers: ISpread<ISpread<string>>

  [<DefaultValue>]
  [<Output("UDPPort")>]
  val mutable OutUDPPort: ISpread<int>

  [<DefaultValue>]
  [<Output("TCPPort")>]
  val mutable OutTCPPort: ISpread<int>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InTiming.[n]) then
            let config = self.InTiming.[n]
            self.OutFramebase.[n] <- int config.Framebase
            self.OutInput.[n] <- config.Input
            self.OutServers.[n].AssignFrom (Array.map string config.Servers)
            self.OutUDPPort.[n] <- int config.UDPPort
            self.OutTCPPort.[n] <- int config.TCPPort
