namespace VVVV.Nodes

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Disco.Raft
open Disco.Core
open Disco.Nodes

//  ____                _
// / ___|  ___  ___ ___(_) ___  _ __
// \___ \ / _ \/ __/ __| |/ _ \| '_ \
//  ___) |  __/\__ \__ \ | (_) | | | |
// |____/ \___||___/___/_|\___/|_| |_|

[<PluginInfo(Name="Session", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
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

        self.OutId.SliceCount <- self.InSession.SliceCount
        self.OutIpAddress.SliceCount <- self.InSession.SliceCount
        self.OutUserAgent.SliceCount <- self.InSession.SliceCount

        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNullReference self.InSession.[n]) then
            let session = self.InSession.[n]
            self.OutId.[n] <- string session.Id
            self.OutIpAddress.[n] <- string session.IpAddress
            self.OutUserAgent.[n] <- session.UserAgent

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
