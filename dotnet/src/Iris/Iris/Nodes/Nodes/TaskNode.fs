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

//  _____         _
// |_   _|_ _ ___| | __
//   | |/ _` / __| |/ /
//   | | (_| \__ \   <
//   |_|\__,_|___/_|\_\

[<PluginInfo(Name="Task", Category="Iris", AutoEvaluate=true)>]
type TaskNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Task")>]
  val mutable InTask: ISpread<Task>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Description")>]
  val mutable OutDescription: ISpread<string>

  [<DefaultValue>]
  [<Output("DisplayId")>]
  val mutable OutDisplayId: ISpread<string>

  [<DefaultValue>]
  [<Output("AudioStream")>]
  val mutable OutAudioStream: ISpread<string>

  [<DefaultValue>]
  [<Output("Arguments")>]
  val mutable OutArguments: ISpread<ISpread<string>>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InTask.[n]) then
            let config = self.InTask.[n]
            let keys = Array.map fst config.Arguments
            let vals = Array.map snd config.Arguments
            self.OutId.[n] <- string config.Id
            self.OutDisplayId.[n] <- string config.DisplayId
            self.OutDescription.[n] <- config.Description
            self.OutAudioStream.[n] <- config.AudioStream
            self.OutArguments.[n].AssignFrom keys

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
