namespace VVVV.Nodes

open System
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.PluginInterfaces.V2.Graph
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Raft
open Iris.Core
open Iris.Nodes
open FSharp.Reflection

//  ____  _    __        __    _ _
// |  _ \(_)_ _\ \      / / __(_) |_ ___
// | |_) | | '_ \ \ /\ / / '__| | __/ _ \
// |  __/| | | | \ V  V /| |  | | ||  __/
// |_|   |_|_| |_|\_/\_/ |_|  |_|\__\___|


[<PluginInfo(Name="PinWrite", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type PinWriteNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Commands")>]
  val mutable InCommands: ISpread<StateMachine>

  [<DefaultValue>]
  [<Input("NodeMappings")>]
  val mutable InNodeMappings: ISpread<NodeMapping>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if self.InUpdate.[0] then

        for mapping in self.InNodeMappings do
          for cmd in self.InCommands do
            match cmd with
            | UpdatePin pin ->
              if pin.Id = mapping.PinId then
                match mapping.Pin.ParentNode.FindPin Settings.DESCRIPTIVE_NAME_PIN with
                | null -> ()
                | ipin -> ipin.[0] <- unwrap pin.Name
                mapping.Pin.Spread <- pin.Slices.ToSpread()
            | UpdateSlices map ->
              if Map.containsKey mapping.PinId map.Slices then
                mapping.Pin.Spread <- map.Slices.[mapping.PinId].ToSpread()
            | _ -> ()
