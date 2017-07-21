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
  [<Output("Id", IsSingle = true)>]
  val mutable OutId: ISpread<Id>

  [<DefaultValue>]
  [<Output("ID String", IsSingle = true)>]
  val mutable OutIdStr: ISpread<string>

  let mutable initialized = false

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if not initialized then
        let id =
          try
            match Environment.GetEnvironmentVariable IRIS_CLIENT_ID_ENV_VAR with
            | null | "" -> Id.Create()
            | str -> str |> Guid.Parse |> string |> Id
          with
            | exn ->
              Logger.err "ClientId (Iris)" exn.Message
              Logger.err "ClientId (Iris)" exn.StackTrace
              Id.Create()

        Logger.initialize {
          Id = id
          Tier = Tier.Client
          UseColors = false
          Level = LogLevel.Debug
        }

        self.OutId.SliceCount <- 1
        self.OutIdStr.SliceCount <- 1

        self.OutId.[0] <- id
        self.OutIdStr.[0] <- string id

        initialized <- true
