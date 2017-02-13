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

//  _   _
// | | | |___  ___ _ __
// | | | / __|/ _ \ '__|
// | |_| \__ \  __/ |
//  \___/|___/\___|_|

[<PluginInfo(Name="User", Category="Iris", AutoEvaluate=true)>]
type UserNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("User")>]
  val mutable InUser: ISpread<User>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("User Name")>]
  val mutable OutUserName: ISpread<string>

  [<DefaultValue>]
  [<Output("First Name")>]
  val mutable OutFirstName: ISpread<string>

  [<DefaultValue>]
  [<Output("Last Name")>]
  val mutable OutLastName: ISpread<string>

  [<DefaultValue>]
  [<Output("Email")>]
  val mutable OutEmail: ISpread<string>

  [<DefaultValue>]
  [<Output("Joined")>]
  val mutable OutJoined: ISpread<string>

  [<DefaultValue>]
  [<Output("Created")>]
  val mutable OutCreated: ISpread<string>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] then
        for n in 0 .. (spreadMax - 1) do
          if not (Util.isNull self.InUser.[n]) then
            let config = self.InUser.[n]
            self.OutId.[n] <- string config.Id
            self.OutUserName.[n] <- string config.UserName
            self.OutFirstName.[n] <- config.FirstName
            self.OutLastName.[n] <- config.LastName
            self.OutEmail.[n] <- config.Email
            self.OutJoined.[n] <- string config.Joined
            self.OutCreated.[n] <- string config.Created

      if self.InUpdate.IsChanged then self.OutUpdate.[0] <- self.InUpdate.[0]
