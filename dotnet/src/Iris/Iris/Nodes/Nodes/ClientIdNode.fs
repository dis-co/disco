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
open FSharp.Reflection

//   ____ _ _            _   ___    _
//  / ___| (_) ___ _ __ | |_|_ _|__| |
// | |   | | |/ _ \ '_ \| __|| |/ _` |
// | |___| | |  __/ | | | |_ | | (_| |
//  \____|_|_|\___|_| |_|\__|___\__,_|

[<PluginInfo(Name="ClientId", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type ClientIdNode() =

  [<DefaultValue>]
  [<Input("ID String", IsSingle = true)>]
  val mutable InIdStr: ISpread<string>

  [<DefaultValue>]
  [<Output("Id")>]
  val mutable OutId: ISpread<Id>

  [<DefaultValue>]
  [<Output("ID String", IsSingle = true)>]
  val mutable OutIdStr: ISpread<string>

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      let id =
        if self.InIdStr.[0] = null then
          try
            Environment.GetEnvironmentVariable IRIS_CLIENT_ID_ENV_VAR
            |> Id
          with
            | _ -> Id.Create()
        else Id self.InIdStr.[0]

      self.OutId.[0] <- id
      self.OutIdStr.[0] <- string id
