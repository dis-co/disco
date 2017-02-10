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

//  __  __                _
// |  \/  | ___ _ __ ___ | |__   ___ _ __
// | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
// | |  | |  __/ | | | | | |_) |  __/ |
// |_|  |_|\___|_| |_| |_|_.__/ \___|_|

[<PluginInfo(Name="Member", Category="Iris", AutoEvaluate=true)>]
type MemberNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Member")>]
  val mutable InMember: ISpread<RaftMember>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("HostName")>]
  val mutable OutHostName: ISpread<string>

  [<DefaultValue>]
  [<Output("IpAddress")>]
  val mutable OutIpAddress: ISpread<string>

  [<DefaultValue>]
  [<Output("Raft Port")>]
  val mutable OutRaftPort: ISpread<int>

  [<DefaultValue>]
  [<Output("WebSocket Port")>]
  val mutable OutWsPort: ISpread<int>

  [<DefaultValue>]
  [<Output("Git Port")>]
  val mutable OutGitPort: ISpread<int>

  [<DefaultValue>]
  [<Output("API Port")>]
  val mutable OutApiPort: ISpread<int>

  [<DefaultValue>]
  [<Output("Status")>]
  val mutable OutStatus: ISpread<string>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InMember.[n]) then
            let config = self.InMember.[n]
            self.OutId.[n] <- string config.Id
            self.OutHostName.[n] <- config.HostName
            self.OutIpAddress.[n] <- string config.IpAddr
            self.OutStatus.[n] <- string config.State
            self.OutRaftPort.[n] <- int config.Port
            self.OutWsPort.[n] <- int config.WsPort
            self.OutGitPort.[n] <- int config.GitPort
            self.OutApiPort.[n] <- int config.ApiPort

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
