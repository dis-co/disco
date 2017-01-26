namespace VVVV.Nodes

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging

[<PluginInfo(Name = "GraphApi", Category = "Awesome", Version = "0.0.1", Help = "RTFM")>]
type GraphApi() =

  [<Input("Input", DefaultValue = 1.0);DefaultValue>]
  val mutable FInput : ISpread<double>

  [<Output("Output");DefaultValue>]
  val mutable FOutput : ISpread<double>

  [<Import();DefaultValue>]
  val mutable FLogger : ILogger

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax : int) : unit =
      self.FLogger.Log(LogType.Debug, "hi tty!");
