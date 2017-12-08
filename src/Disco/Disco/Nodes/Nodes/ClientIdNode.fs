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
open FSharp.Reflection

//   ____ _ _            _   ___    _
//  / ___| (_) ___ _ __ | |_|_ _|__| |
// | |   | | |/ _ \ '_ \| __|| |/ _` |
// | |___| | |  __/ | | | |_ | | (_| |
//  \____|_|_|\___|_| |_|\__|___\__,_|

[<PluginInfo(Name="ClientId", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type ClientIdNode() =

  [<DefaultValue>]
  [<Input("Guid", IsSingle = true)>]
  val mutable InStr: ISpread<string>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Id", IsSingle = true)>]
  val mutable OutClientId: ISpread<ClientId>

  [<DefaultValue>]
  [<Output("ID String", IsSingle = true)>]
  val mutable OutIdStr: ISpread<string>

  let mutable initialized = false

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if not initialized then
        let id =
          self.InStr.[0]
          |> DiscoId.TryParse
          |> function
          | Right id -> id
          | Left _ ->
            DISCO_CLIENT_ID_ENV_VAR
            |> Environment.GetEnvironmentVariable
            |> DiscoId.TryParse
            |> function
            | Right id -> id
            | Left _ -> DiscoId.Create()

        do Logger.initialize {
          MachineId = id
          Tier = Tier.Client
          UseColors = false
          Level = LogLevel.Debug
        }

        self.OutClientId.[0] <- id
        self.OutIdStr.[0] <- string id
        initialized <- true

      if self.InUpdate.[0] then
        initialized <- false
