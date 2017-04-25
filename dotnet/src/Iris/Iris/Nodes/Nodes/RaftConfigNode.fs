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

//  ____        __ _    ____             __ _
// |  _ \ __ _ / _| |_ / ___|___  _ __  / _(_) __ _
// | |_) / _` | |_| __| |   / _ \| '_ \| |_| |/ _` |
// |  _ < (_| |  _| |_| |__| (_) | | | |  _| | (_| |
// |_| \_\__,_|_|  \__|\____\___/|_| |_|_| |_|\__, |
//                                            |___/

[<PluginInfo(Name="RaftConfig", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type RaftConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Raft")>]
  val mutable InRaft: ISpread<RaftConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Request Timeout")>]
  val mutable OutRequestTimeout: ISpread<int>

  [<DefaultValue>]
  [<Output("ElectionTimeout")>]
  val mutable OutElectionTimeout: ISpread<int>

  [<DefaultValue>]
  [<Output("MaxLogDepth")>]
  val mutable OutMaxLogDepth: ISpread<int>

  [<DefaultValue>]
  [<Output("Log Level")>]
  val mutable OutLogLevel: ISpread<string>

  [<DefaultValue>]
  [<Output("Data Directory")>]
  val mutable OutDataDir: ISpread<string>

  [<DefaultValue>]
  [<Output("MaxRetries")>]
  val mutable OutMaxRetries: ISpread<int>

  [<DefaultValue>]
  [<Output("Periodic Interval")>]
  val mutable OutPeriodicInterval: ISpread<int>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutRequestTimeout.SliceCount <- self.InRaft.SliceCount
        self.OutElectionTimeout.SliceCount <- self.InRaft.SliceCount
        self.OutMaxLogDepth.SliceCount <- self.InRaft.SliceCount
        self.OutLogLevel.SliceCount <- self.InRaft.SliceCount
        self.OutDataDir.SliceCount <- self.InRaft.SliceCount
        self.OutMaxRetries.SliceCount <- self.InRaft.SliceCount
        self.OutPeriodicInterval.SliceCount <- self.InRaft.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InRaft.[n]) then
            let config = self.InRaft.[n]
            self.OutRequestTimeout.[n] <- int config.RequestTimeout
            self.OutElectionTimeout.[n] <- int config.ElectionTimeout
            self.OutMaxLogDepth.[n] <- int config.MaxLogDepth
            self.OutLogLevel.[n] <- string config.LogLevel
            self.OutDataDir.[n] <- unwrap config.DataDir
            self.OutMaxRetries.[n] <- int config.MaxRetries
            self.OutPeriodicInterval.[n] <- int config.PeriodicInterval

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
