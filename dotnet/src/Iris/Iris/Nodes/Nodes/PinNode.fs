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
open FSharp.Reflection

//  ____  _
// |  _ \(_)_ __
// | |_) | | '_ \
// |  __/| | | | |
// |_|   |_|_| |_|

[<PluginInfo(Name="Pin", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type PinNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Pin")>]
  val mutable InPin: ISpread<Pin>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Type")>]
  val mutable OutType: ISpread<string>

  [<DefaultValue>]
  [<Output("Name")>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Persisted")>]
  val mutable OutPersisted: ISpread<bool>

  [<DefaultValue>]
  [<Output("Tags")>]
  val mutable OutTags: ISpread<string>

  [<DefaultValue>]
  [<Output("Direction")>]
  val mutable OutDirection: ISpread<string>

  [<DefaultValue>]
  [<Output("VecSize")>]
  val mutable OutVecSize: ISpread<string>

  [<DefaultValue>]
  [<Output("Values")>]
  val mutable OutValues: ISpread<string>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount   <- self.InPin.SliceCount
        self.OutName.SliceCount <- self.InPin.SliceCount
        self.OutPersisted.SliceCount <- self.InPin.SliceCount
        self.OutType.SliceCount <- self.InPin.SliceCount
        self.OutTags.SliceCount <- self.InPin.SliceCount
        self.OutDirection.SliceCount <- self.InPin.SliceCount
        self.OutVecSize.SliceCount <- self.InPin.SliceCount
        self.OutValues.SliceCount <- self.InPin.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InPin.[0]) then
            let pin = self.InPin.[n]
            let tipe =
              let case, _ = FSharpValue.GetUnionFields(pin, pin.GetType())
              case.Name
            self.OutId.[n] <- string pin.Id
            self.OutName.[n] <- pin.Name
            self.OutPersisted.[n] <- pin.Persisted
            self.OutType.[n] <- tipe
            self.OutTags.[n] <- String.Join(",", pin.GetTags)
            self.OutDirection.[n] <- string pin.Direction
            self.OutVecSize.[n] <- string pin.VecSize
            self.OutValues.[n] <- pin.ToSpread()

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
