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

//  ____                _
// / ___|  ___  ___ ___(_) ___  _ __
// \___ \ / _ \/ __/ __| |/ _ \| '_ \
//  ___) |  __/\__ \__ \ | (_) | | | |
// |____/ \___||___/___/_|\___/|_| |_|

[<PluginInfo(Name="Session", Category="Iris", AutoEvaluate=true)>]
type SessionNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Session")>]
  val mutable InSession: ISpread<Session>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("IP Address")>]
  val mutable OutIpAddress: ISpread<string>

  [<DefaultValue>]
  [<Output("UserAgent")>]
  val mutable OutUserAgent: ISpread<string>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InSession.[n]) then
            let config = self.InSession.[n]
            self.OutId.[n] <- string config.Id
            self.OutIpAddress.[n] <- string config.IpAddress
            self.OutUserAgent.[n] <- config.UserAgent

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
