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

[<PluginInfo(Name="CueList", Category="Iris", AutoEvaluate=true)>]
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
  val mutable OutCues: ISpread<Cue>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InCueList.[n]) then
            let config = self.InCueList.[n]
            self.OutId.[n] <- string config.Id
            self.OutName.[n] <- config.Name
            self.OutCues.AssignFrom config.Cues

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
