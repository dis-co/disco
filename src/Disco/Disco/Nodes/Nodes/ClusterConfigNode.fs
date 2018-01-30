(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

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

//   ____ _           _
//  / ___| |_   _ ___| |_ ___ _ __
// | |   | | | | / __| __/ _ \ '__|
// | |___| | |_| \__ \ ||  __/ |
//  \____|_|\__,_|___/\__\___|_|

[<PluginInfo(Name="ClusterConfig", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type ClusterConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Cluster")>]
  val mutable InCluster: ISpread<ClusterConfig>

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
  [<Output("Members")>]
  val mutable OutMembers: ISpread<ISpread<ClusterMember>>

  [<DefaultValue>]
  [<Output("Groups")>]
  val mutable OutGroups: ISpread<ISpread<HostGroup>>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutName.SliceCount <- self.InCluster.SliceCount
        self.OutMembers.SliceCount <- self.InCluster.SliceCount
        self.OutGroups.SliceCount <- self.InCluster.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InCluster.[n]) then
            let config = self.InCluster.[n]
            let members = config.Members |> Map.toArray |> Array.map snd

            self.OutName.[n] <- unwrap config.Name
            self.OutMembers.[n].SliceCount <- Array.length members
            self.OutMembers.[n].AssignFrom members
            self.OutGroups.[n].SliceCount <- Array.length config.Groups
            self.OutGroups.[n].AssignFrom config.Groups

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
