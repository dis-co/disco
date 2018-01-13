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

//  _   _           _    ____
// | | | | ___  ___| |_ / ___|_ __ ___  _   _ _ __
// | |_| |/ _ \/ __| __| |  _| '__/ _ \| | | | '_ \
// |  _  | (_) \__ \ |_| |_| | | | (_) | |_| | |_) |
// |_| |_|\___/|___/\__|\____|_|  \___/ \__,_| .__/
//                                           |_|

[<PluginInfo(Name="HostGroup", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type HostGroupNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("HostGroup")>]
  val mutable InHostGroup: ISpread<HostGroup>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Members")>]
  val mutable OutMembers: ISpread<ISpread<string>>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutName.SliceCount <- self.InHostGroup.SliceCount
        self.OutMembers.SliceCount <- self.InHostGroup.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InHostGroup.[n]) then
            let group = self.InHostGroup.[n]
            self.OutName.[n] <- unwrap group.Name
            self.OutMembers.[n].SliceCount <- (Array.length group.Members)
            self.OutMembers.[n].AssignFrom (Array.map string group.Members)

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
