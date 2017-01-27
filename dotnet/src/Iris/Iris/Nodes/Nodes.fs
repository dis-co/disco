namespace VVVV.Nodes

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Core
open Iris.Nodes

[<PluginInfo(Name="Iris", Category="Iris", AutoEvaluate=true)>]
type IrisVvvvClient() =

  //  ___                            _
  // |_ _|_ __ ___  _ __   ___  _ __| |_ ___
  //  | || '_ ` _ \| '_ \ / _ \| '__| __/ __|
  //  | || | | | | | |_) | (_) | |  | |_\__ \
  // |___|_| |_| |_| .__/ \___/|_|   \__|___/
  //               |_|

  [<Import();DefaultValue>]
  val mutable V1Host: IPluginHost

  [<Import();DefaultValue>]
  val mutable V2Host: IHDEHost

  [<Import();DefaultValue>]
  val mutable FLogger: ILogger

  //  ___                   _
  // |_ _|_ __  _ __  _   _| |_ ___
  //  | || '_ \| '_ \| | | | __/ __|
  //  | || | | | |_) | |_| | |_\__ \
  // |___|_| |_| .__/ \__,_|\__|___/
  //           |_|

  [<DefaultValue>]
  [<Input("Server", IsSingle = true)>]
  val mutable InServer: IDiffSpread<string>

  [<DefaultValue>]
  [<Input("Port", IsSingle = true)>]
  val mutable InPort: IDiffSpread<uint16>

  [<DefaultValue>]
  [<Input("Debug", IsSingle = true, DefaultValue = 0.0)>]
  val mutable InDebug: IDiffSpread<bool>

  //   ___        _               _
  //  / _ \ _   _| |_ _ __  _   _| |_ ___
  // | | | | | | | __| '_ \| | | | __/ __|
  // | |_| | |_| | |_| |_) | |_| | |_\__ \
  //  \___/ \__,_|\__| .__/ \__,_|\__|___/
  //                 |_|

  [<DefaultValue>]
  [<Output("State")>]
  val mutable OutState: ISpread<State>

  [<DefaultValue>]
  [<Output("Connected", IsSingle = true, DefaultValue = 0.0)>]
  val mutable OutConnected: ISpread<bool>

  [<DefaultValue>]
  [<Output("Status", IsSingle = true)>]
  val mutable OutStatus: ISpread<string>

  //  _                    _
  // | |    ___   ___ __ _| |
  // | |   / _ \ / __/ _` | |
  // | |__| (_) | (_| (_| | |
  // |_____\___/ \___\__,_|_|

  let mutable initialized = false
  let mutable state = Unchecked.defaultof<GraphApi.PluginState>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if not initialized then
        let state' =
          { GraphApi.PluginState.Create() with
              V1Host = self.V1Host
              V2Host = self.V2Host
              Logger = self.FLogger
              InServer = self.InServer
              InPort = self.InPort
              InDebug = self.InDebug
              OutState = self.OutState
              OutConnected = self.OutConnected
              OutStatus = self.OutStatus }
        state <- state'
        initialized <- true

      state <- GraphApi.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state
