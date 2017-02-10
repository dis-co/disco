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

//   ____ _           _
//  / ___| |_   _ ___| |_ ___ _ __
// | |   | | | | / __| __/ _ \ '__|
// | |___| | |_| \__ \ ||  __/ |
//  \____|_|\__,_|___/\__\___|_|

[<PluginInfo(Name="ClusterConfig", Category="Iris", AutoEvaluate=true)>]
type ClusterConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Cluster")>]
  val mutable InCluster: ISpread<ClusterConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Members")>]
  val mutable OutMembers: ISpread<ISpread<RaftMember>>

  [<DefaultValue>]
  [<Output("Groups")>]
  val mutable OutGroups: ISpread<ISpread<HostGroup>>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InCluster.[n]) then
            let config = self.InCluster.[n]
            self.OutName.[n] <- config.Name
            self.OutMembers.[n].AssignFrom (config.Members |> Map.toArray |> Array.map snd)
            self.OutGroups.[n].AssignFrom config.Groups
