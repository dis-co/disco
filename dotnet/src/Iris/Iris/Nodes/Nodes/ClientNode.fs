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

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

[<PluginInfo(Name="Client", Category="Iris", AutoEvaluate=true)>]
type ClientNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Client")>]
  val mutable InClient: ISpread<IrisClient>

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
  [<Output("Role")>]
  val mutable OutRole: ISpread<string>

  [<DefaultValue>]
  [<Output("Status")>]
  val mutable OutStatus: ISpread<string>

  [<DefaultValue>]
  [<Output("IpAddress")>]
  val mutable OutIpAddress: ISpread<string>

  [<DefaultValue>]
  [<Output("Port")>]
  val mutable OutPort: ISpread<int>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InClient.[n]) then
            let config = self.InClient.[n]
            self.OutId.[n] <- string config.Id
            self.OutName.[n] <- config.Name
            self.OutRole.[n] <- string config.Role
            self.OutStatus.[n] <- string config.Status
            self.OutIpAddress.[n] <- string config.IpAddress
            self.OutPort.[n] <- int config.Port

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
