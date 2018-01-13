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

//  __  __                   _
// |  \/  | __ _ _ __  _ __ (_)_ __   __ _
// | |\/| |/ _` | '_ \| '_ \| | '_ \ / _` |
// | |  | | (_| | |_) | |_) | | | | | (_| |
// |_|  |_|\__,_| .__/| .__/|_|_| |_|\__, |
//              |_|   |_|            |___/

[<PluginInfo(Name="PinMapping", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type PinMappingNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("PinMapping")>]
  val mutable InPinMapping: ISpread<PinMapping>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Source")>]
  val mutable OutSource: ISpread<string>

  [<DefaultValue>]
  [<Output("Sinks")>]
  val mutable OutSinks: ISpread<ISpread<string>>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount <- self.InPinMapping.SliceCount
        self.OutSource.SliceCount <- self.InPinMapping.SliceCount
        self.OutSinks.SliceCount <- self.InPinMapping.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InPinMapping.[0]) then
            let mapping = self.InPinMapping.[n]

            let sinks =
              mapping.Sinks
              |> Array.ofSeq
              |> Array.map string

            self.OutId.[n] <- string mapping.Id
            self.OutSource.[n] <- string mapping.Source
            self.OutSinks.[n].SliceCount <- Array.length sinks
            self.OutSinks.[n].AssignFrom sinks

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
