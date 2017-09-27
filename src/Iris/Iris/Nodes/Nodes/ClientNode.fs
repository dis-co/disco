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

[<PluginInfo(Name="Client", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
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

        self.OutId.SliceCount <- self.InClient.SliceCount
        self.OutName.SliceCount <- self.InClient.SliceCount
        self.OutRole.SliceCount <- self.InClient.SliceCount
        self.OutStatus.SliceCount <- self.InClient.SliceCount
        self.OutIpAddress.SliceCount <- self.InClient.SliceCount
        self.OutPort.SliceCount <- self.InClient.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InClient.[n]) then
            let client = self.InClient.[n]
            self.OutId.[n] <- string client.Id
            self.OutName.[n] <- unwrap client.Name
            self.OutRole.[n] <- string client.Role
            self.OutStatus.[n] <- string client.Status
            self.OutIpAddress.[n] <- string client.IpAddress
            self.OutPort.[n] <- int client.Port

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
