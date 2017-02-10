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

//   ____             __ _
//  / ___|___  _ __  / _(_) __ _
// | |   / _ \| '_ \| |_| |/ _` |
// | |__| (_) | | | |  _| | (_| |
//  \____\___/|_| |_|_| |_|\__, |
//                         |___/

[<PluginInfo(Name="Config", Category="Iris", AutoEvaluate=true)>]
type ConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Config", IsSingle = true)>]
  val mutable InConfig: ISpread<IrisConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("MachineId", IsSingle = true)>]
  val mutable OutMachineId: ISpread<string>

  [<DefaultValue>]
  [<Output("Audio", IsSingle = true)>]
  val mutable OutAudio: ISpread<AudioConfig>

  [<DefaultValue>]
  [<Output("Vvvv", IsSingle = true)>]
  val mutable OutVvvv: ISpread<VvvvConfig>

  [<DefaultValue>]
  [<Output("Raft", IsSingle = true)>]
  val mutable OutRaft: ISpread<RaftConfig>

  [<DefaultValue>]
  [<Output("Timing", IsSingle = true)>]
  val mutable OutTiming: ISpread<TimingConfig>

  [<DefaultValue>]
  [<Output("Cluster", IsSingle = true)>]
  val mutable OutCluster: ISpread<ClusterConfig>

  [<DefaultValue>]
  [<Output("Viewports")>]
  val mutable OutViewports: ISpread<ViewPort>

  [<DefaultValue>]
  [<Output("Displays")>]
  val mutable OutDisplays: ISpread<Display>

  [<DefaultValue>]
  [<Output("Tasks")>]
  val mutable OutTasks: ISpread<Task>

  [<DefaultValue>]
  [<Output("Version", IsSingle = true)>]
  val mutable OutVersion: ISpread<string>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InConfig.[0]) then
        let config = self.InConfig.[0]

        self.OutMachineId.[0] <- string config.MachineId
        self.OutAudio.[0] <- config.Audio
        self.OutVvvv.[0] <- config.Vvvv
        self.OutRaft.[0] <- config.Raft
        self.OutTiming.[0] <- config.Timing
        self.OutCluster.[0] <- config.Cluster
        self.OutViewports.AssignFrom config.ViewPorts
        self.OutDisplays.AssignFrom config.Displays
        self.OutTasks.AssignFrom config.Tasks
        self.OutVersion.[0] <- string config.Version
