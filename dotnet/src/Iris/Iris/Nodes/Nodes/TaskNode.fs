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

[<PluginInfo(Name="Task", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
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
  [<Output("Argument Keys")>]
  val mutable OutArgumentKeys: ISpread<ISpread<string>>

  [<DefaultValue>]
  [<Output("Argument Values")>]
  val mutable OutArgumentValues: ISpread<ISpread<string>>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount <- self.InTask.SliceCount
        self.OutDisplayId.SliceCount <- self.InTask.SliceCount
        self.OutDescription.SliceCount <- self.InTask.SliceCount
        self.OutAudioStream.SliceCount <- self.InTask.SliceCount
        self.OutArgumentKeys.SliceCount <- self.InTask.SliceCount
        self.OutArgumentValues.SliceCount <- self.InTask.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InTask.[n]) then
            let task = self.InTask.[n]
            let keys = Array.map fst task.Arguments
            let len = Array.length keys
            let vals = Array.map snd task.Arguments

            self.OutId.[n] <- string task.Id
            self.OutDisplayId.[n] <- string task.DisplayId
            self.OutDescription.[n] <- task.Description
            self.OutAudioStream.[n] <- task.AudioStream
            self.OutArgumentKeys.[n].SliceCount <- len
            self.OutArgumentValues.[n].SliceCount <- len
            self.OutArgumentKeys.[n].AssignFrom keys
            self.OutArgumentValues.[n].AssignFrom vals

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
