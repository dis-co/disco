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

//   ____
//  / ___|_ __ ___  _   _ _ __
// | |  _| '__/ _ \| | | | '_ \
// | |_| | | | (_) | |_| | |_) |
//  \____|_|  \___/ \__,_| .__/
//                       |_|

[<PluginInfo(Name="PinGroup", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type PinGroupNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("PinGroup")>]
  val mutable InPinGroup: ISpread<PinGroup>

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
  [<Output("Pins")>]
  val mutable OutPins: ISpread<ISpread<Pin>>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount <- self.InPinGroup.SliceCount
        self.OutName.SliceCount <- self.InPinGroup.SliceCount
        self.OutPins.SliceCount <- self.InPinGroup.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InPinGroup.[0]) then
            let group = self.InPinGroup.[n]
            let pins =
              group.Pins
              |> Map.toArray
              |> Array.map snd
            self.OutId.[n] <- string group.Id
            self.OutName.[n] <- unwrap group.Name
            self.OutPins.[n].SliceCount <- Array.length pins
            self.OutPins.[n].AssignFrom pins

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
