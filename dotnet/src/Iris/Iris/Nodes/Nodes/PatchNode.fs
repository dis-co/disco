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

//  ____       _       _
// |  _ \ __ _| |_ ___| |__
// | |_) / _` | __/ __| '_ \
// |  __/ (_| | || (__| | | |
// |_|   \__,_|\__\___|_| |_|

[<PluginInfo(Name="Patch", Category="Iris", AutoEvaluate=true)>]
type PatchNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Patch")>]
  val mutable InPatch: ISpread<Patch>

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

        self.OutId.SliceCount <- self.InPatch.SliceCount
        self.OutName.SliceCount <- self.InPatch.SliceCount
        self.OutPins.SliceCount <- self.InPatch.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InPatch.[0]) then
            let patch = self.InPatch.[n]
            let pins =
              patch.Pins
              |> Map.toArray
              |> Array.map snd
            self.OutId.[n] <- string patch.Id
            self.OutName.[n] <- patch.Name
            self.OutPins.[n].SliceCount <- Array.length pins
            self.OutPins.[n].AssignFrom pins

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
