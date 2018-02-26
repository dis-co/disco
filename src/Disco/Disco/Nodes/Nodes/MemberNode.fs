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

//  __  __                _
// |  \/  | ___ _ __ ___ | |__   ___ _ __
// | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
// | |  | |  __/ | | | | | |_) |  __/ |
// |_|  |_|\___|_| |_| |_|_.__/ \___|_|

[<PluginInfo(Name="Member", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type MemberNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Member")>]
  val mutable InMember: ISpread<RaftMember>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("IpAddress")>]
  val mutable OutIpAddress: ISpread<string>

  [<DefaultValue>]
  [<Output("Raft Port")>]
  val mutable OutRaftPort: ISpread<int>

  [<DefaultValue>]
  [<Output("State")>]
  val mutable OutState: ISpread<string>

  [<DefaultValue>]
  [<Output("Status")>]
  val mutable OutStatus: ISpread<string>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        self.OutId.SliceCount        <- self.InMember.SliceCount
        self.OutIpAddress.SliceCount <- self.InMember.SliceCount
        self.OutState.SliceCount     <- self.InMember.SliceCount
        self.OutStatus.SliceCount    <- self.InMember.SliceCount
        self.OutRaftPort.SliceCount  <- self.InMember.SliceCount
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InMember.[n]) then
            let mem = self.InMember.[n]
            self.OutId.[n]        <- string mem.Id
            self.OutIpAddress.[n] <- string mem.IpAddress
            self.OutStatus.[n]    <- string mem.Status
            self.OutState.[n]     <- string mem.State
            self.OutRaftPort.[n]  <- int mem.RaftPort
      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
