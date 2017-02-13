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

//  ___      _
// |_ _|_ __(_)___
//  | || '__| / __|
//  | || |  | \__ \
// |___|_|  |_|___/

[<PluginInfo(Name="Iris", Category="Iris", AutoEvaluate=true)>]
type IrisNode() =

  [<Import();DefaultValue>]
  val mutable V1Host: IPluginHost

  [<Import();DefaultValue>]
  val mutable V2Host: IHDEHost

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Server", IsSingle = true)>]
  val mutable InServer: IDiffSpread<string>

  [<DefaultValue>]
  [<Input("Port", IsSingle = true)>]
  val mutable InPort: IDiffSpread<uint16>

  [<DefaultValue>]
  [<Input("Debug", IsSingle = true, DefaultValue = 0.0)>]
  val mutable InDebug: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("State", IsSingle = true)>]
  val mutable OutState: ISpread<Iris.Core.State>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Connected", IsSingle = true, DefaultValue = 0.0)>]
  val mutable OutConnected: ISpread<bool>

  [<DefaultValue>]
  [<Output("Status", IsSingle = true)>]
  val mutable OutStatus: ISpread<string>

  let mutable initialized = false
  let mutable state = Unchecked.defaultof<GraphApi.PluginState>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if not initialized then
        let state' =
          { GraphApi.PluginState.Create() with
              V1Host = self.V1Host
              V2Host = self.V2Host
              Logger = self.Logger
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
