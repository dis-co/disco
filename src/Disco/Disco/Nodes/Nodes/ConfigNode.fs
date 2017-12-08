namespace VVVV.Nodes

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Disco.Raft
open Disco.Core
open Disco.Nodes

//   ____             __ _
//  / ___|___  _ __  / _(_) __ _
// | |   / _ \| '_ \| |_| |/ _` |
// | |__| (_) | | | |  _| | (_| |
//  \____\___/|_| |_|_| |_|\__, |
//                         |___/

[<PluginInfo(Name="Config", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type ConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Config", IsSingle = true)>]
  val mutable InConfig: ISpread<DiscoConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Machine", IsSingle = true)>]
  val mutable OutMachine: ISpread<DiscoMachine>

  [<DefaultValue>]
  [<Output("ActiveSite", IsSingle = true)>]
  val mutable OutActiveSite: ISpread<string>

  [<DefaultValue>]
  [<Output("Audio", IsSingle = true)>]
  val mutable OutAudio: ISpread<AudioConfig>

  [<DefaultValue>]
  [<Output("Clients", IsSingle = true)>]
  val mutable OutClients: ISpread<ClientConfig>

  [<DefaultValue>]
  [<Output("Raft", IsSingle = true)>]
  val mutable OutRaft: ISpread<RaftConfig>

  [<DefaultValue>]
  [<Output("Timing", IsSingle = true)>]
  val mutable OutTiming: ISpread<TimingConfig>

  [<DefaultValue>]
  [<Output("Sites")>]
  val mutable OutSites: ISpread<ClusterConfig>

  [<DefaultValue>]
  [<Output("Version", IsSingle = true)>]
  val mutable OutVersion: ISpread<string>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if self.InUpdate.[0] && not (Util.isNullReference self.InConfig.[0]) then
        let config = self.InConfig.[0]

        self.OutMachine.[0] <- config.Machine
        self.OutAudio.[0] <- config.Audio
        self.OutClients.[0] <- config.Clients
        self.OutRaft.[0] <- config.Raft
        self.OutTiming.[0] <- config.Timing
        self.OutSites.SliceCount <- Array.length config.Sites
        self.OutSites.AssignFrom config.Sites
        self.OutVersion.[0] <- string config.Version

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
