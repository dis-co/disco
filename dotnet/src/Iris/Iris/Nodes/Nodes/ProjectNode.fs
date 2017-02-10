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

//  ____            _           _
// |  _ \ _ __ ___ (_) ___  ___| |_
// | |_) | '__/ _ \| |/ _ \/ __| __|
// |  __/| | | (_) | |  __/ (__| |_
// |_|   |_|  \___// |\___|\___|\__|
//               |__/

[<PluginInfo(Name="Project", Category="Iris", AutoEvaluate=true)>]
type ProjectNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Project", IsSingle = true)>]
  val mutable InProject: ISpread<IrisProject>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: ISpread<bool>

  [<DefaultValue>]
  [<Output("Id", IsSingle = true)>]
  val mutable OutId: ISpread<string>

  [<DefaultValue>]
  [<Output("Name", IsSingle = true)>]
  val mutable OutName: ISpread<string>

  [<DefaultValue>]
  [<Output("Path", IsSingle = true)>]
  val mutable OutPath: ISpread<string>

  [<DefaultValue>]
  [<Output("CreatedOn", IsSingle = true)>]
  val mutable OutCreatedOn: ISpread<string>

  [<DefaultValue>]
  [<Output("LastSaved", IsSingle = true)>]
  val mutable OutLastSaved: ISpread<string>

  [<DefaultValue>]
  [<Output("Config", IsSingle = true)>]
  val mutable OutConfig: ISpread<IrisConfig>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InUpdate.[0] && not (Util.isNull self.InProject.[0]) then
        let project = self.InProject.[0]
        let lastSaved =
          match project.LastSaved with
          | Some str -> str
          | None -> ""

        self.OutId.[0] <- string project.Id
        self.OutName.[0] <- project.Name
        self.OutPath.[0] <- project.Path
        self.OutCreatedOn.[0] <- sprintf "%A" project.CreatedOn
        self.OutLastSaved.[0] <- lastSaved
        self.OutConfig.[0] <- project.Config
