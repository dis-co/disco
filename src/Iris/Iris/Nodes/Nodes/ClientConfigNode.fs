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

///   ____ _ _            _    ____             __ _
///  / ___| (_) ___ _ __ | |_ / ___|___  _ __  / _(_) __ _
/// | |   | | |/ _ \ '_ \| __| |   / _ \| '_ \| |_| |/ _` |
/// | |___| | |  __/ | | | |_| |__| (_) | | | |  _| | (_| |
///  \____|_|_|\___|_| |_|\__|\____\___/|_| |_|_| |_|\__, |
///                                                  |___/

[<PluginInfo(Name="ClientConfig", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type ClientConfigNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Client", IsSingle = true)>]
  val mutable InClient: ISpread<ClientConfig>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Client Id")>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Executable")>]
  val mutable OutExecutable: ISpread<string>

  [<DefaultValue>]
  [<Output("Version")>]
  val mutable OutVersion: ISpread<string>

  [<DefaultValue>]
  [<Output("Required")>]
  val mutable OutRequired: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if self.InUpdate.[0]then

        if not (Util.isNullReference self.InClient.[0]) then
          let config = ClientConfig.executables self.InClient.[0]

          self.OutId.SliceCount <- config.Length
          self.OutExecutable.SliceCount <- config.Length
          self.OutVersion.SliceCount <- config.Length
          self.OutRequired.SliceCount <- config.Length

          for idx in 0 .. config.Length - 1 do
            let exe = config.[idx]
            self.OutId.[idx]         <- string exe.Id
            self.OutExecutable.[idx] <- string exe.Executable
            self.OutVersion.[idx]    <- string exe.Version
            self.OutRequired.[idx]   <- exe.Required
