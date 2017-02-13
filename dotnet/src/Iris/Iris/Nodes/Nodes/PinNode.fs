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

[<PluginInfo(Name="Pin", Category="Iris", AutoEvaluate=true)>]
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
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then

        self.OutId.SliceCount   <- self.InPin.SliceCount
        self.OutName.SliceCount <- self.InPin.SliceCount
        self.OutType.SliceCount <- self.InPin.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InPin.[0]) then
            let pin = self.InPin.[n]
            let name =
              let case, _ = FSharpValue.GetUnionFields(pin, pin.GetType())
              case.Name

            self.OutId.[n] <- string pin.Id
            self.OutName.[n] <- pin.Name
            self.OutType.[n] <- name

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
