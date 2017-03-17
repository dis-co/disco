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
            | UpdateSlices slices ->
              if slices.Id = mapping.PinId then
                let spread = slices.ToSpread()
                let id =
                  sprintf "%s/%s"
                    (mapping.Pin.ParentNode.GetNodePath(false))
                    mapping.Pin.Name
                self.Logger.Log(LogType.Debug, sprintf "pin: %s values: %A" id spread)
                mapping.Pin.Spread <-spread
            | _ -> ()


(*

//  _____         _   _   _           _
// |_   _|__  ___| |_| \ | | ___   __| | ___
//   | |/ _ \/ __| __|  \| |/ _ \ / _` |/ _ \
//   | |  __/\__ \ |_| |\  | (_) | (_| |  __/
//   |_|\___||___/\__|_| \_|\___/ \__,_|\___|

[<PluginInfo(Name="ValueSlices", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type ValuesSlicesNode() =

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Id")>]
  val mutable InId: ISpread<string>

  [<DefaultValue>]
  [<Input("Values")>]
  val mutable InValues: ISpread<double>

  [<DefaultValue>]
  [<Input("Update", IsSingle = true, IsBang = true)>]
  val mutable InUpdate: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Command")>]
  val mutable OutCommand: ISpread<StateMachine>

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      self.OutCommand.SliceCount <- 1
      if self.InUpdate.[0] then
        let values = [| for n in 0 .. self.InValues.SliceCount - 1 do
                          yield self.InValues.[n] |]

        let output = NumberSlices(Id self.InId.[0], values) |> UpdateSlices
        self.OutCommand.[0] <- output
      else
        self.OutCommand.AssignFrom [| |]

*)
