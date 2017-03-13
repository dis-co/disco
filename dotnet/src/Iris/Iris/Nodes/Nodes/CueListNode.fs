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

//   ____           _     _     _
//  / ___|   _  ___| |   (_)___| |_
// | |  | | | |/ _ \ |   | / __| __|
// | |__| |_| |  __/ |___| \__ \ |_
//  \____\__,_|\___|_____|_|___/\__|

[<PluginInfo(Name="CueList", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type CueListNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("CueList")>]
  val mutable InCueList: ISpread<CueList>

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
  [<Output("Cues")>]
  val mutable OutCues: ISpread<ISpread<Cue>>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount <- self.InCueList.SliceCount
        self.OutName.SliceCount <- self.InCueList.SliceCount
        self.OutCues.SliceCount <- self.InCueList.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InCueList.[n]) then
            let cuelist = self.InCueList.[n]
            self.OutId.[n] <- string cuelist.Id
            self.OutName.[n] <- cuelist.Name
            self.OutCues.[n].SliceCount <- Array.length cuelist.Cues
            self.OutCues.[n].AssignFrom cuelist.Cues

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
