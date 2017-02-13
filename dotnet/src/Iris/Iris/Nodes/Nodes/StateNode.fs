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

//  ____  _        _
// / ___|| |_ __ _| |_ ___
// \___ \| __/ _` | __/ _ \
//  ___) | || (_| | ||  __/
// |____/ \__\__,_|\__\___|

[<PluginInfo(Name="State", Category="Iris", AutoEvaluate=true)>]
type StateNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("State", IsSingle = true)>]
  val mutable InState: ISpread<Iris.Core.State>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Project", IsSingle = true)>]
  val mutable OutProject: ISpread<IrisProject>

  [<DefaultValue>]
  [<Output("Patches")>]
  val mutable OutPatches: ISpread<Patch>

  [<DefaultValue>]
  [<Output("Cues")>]
  val mutable OutCues: ISpread<Cue>

  [<DefaultValue>]
  [<Output("CueLists")>]
  val mutable OutCueLists: ISpread<CueList>

  [<DefaultValue>]
  [<Output("Sessions")>]
  val mutable OutSessions: ISpread<Session>

  [<DefaultValue>]
  [<Output("Users")>]
  val mutable OutUsers: ISpread<User>

  [<DefaultValue>]
  [<Output("Clients")>]
  val mutable OutClients: ISpread<IrisClient>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if self.InUpdate.[0] (* && not (Util.isNull self.InState.[0]) *) then
        let state = self.InState.[0]
        self.OutProject.[0] <- state.Project

        let patches =
          state.Patches
          |> Map.toArray
          |> Array.map snd

        let cues =
          state.Cues
          |> Map.toArray
          |> Array.map snd

        let cuelists =
          state.CueLists
          |> Map.toArray
          |> Array.map snd

        let sessions =
          state.Sessions
          |> Map.toArray
          |> Array.map snd

        let users =
          state.Users
          |> Map.toArray
          |> Array.map snd

        let clients =
          state.Clients
          |> Map.toArray
          |> Array.map snd

        self.OutPatches.SliceCount <- Array.length patches
        self.OutCues.SliceCount <- Array.length cues
        self.OutCueLists.SliceCount <- Array.length cuelists
        self.OutSessions.SliceCount <- Array.length sessions
        self.OutUsers.SliceCount <- Array.length users
        self.OutClients.SliceCount <- Array.length clients

        self.OutPatches.AssignFrom patches
        self.OutCues.AssignFrom cues
        self.OutCueLists.AssignFrom cuelists
        self.OutSessions.AssignFrom sessions
        self.OutUsers.AssignFrom users
        self.OutClients.AssignFrom clients

      if self.InUpdate.IsChanged then
        self.OutUpdate.[0] <- self.InUpdate.[0]
