(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace VVVV.Nodes

open System
open System.Text
open System.Threading
open System.Collections.Concurrent
open System.ComponentModel.Composition
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.PluginInterfaces.V2.Graph
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Disco.Raft
open Disco.Core
open Disco.Nodes
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

  let mutable frame = 0UL
  let bumpFrame () = frame <- frame + 1UL

  let resetEvents = ConcurrentQueue<uint64 * NodeMapping>()

  let makeSpread (count:int) =
    let max = count - 1
    let builder = StringBuilder()
    builder.Append '|' |> ignore
    for n in 0 .. max do
      builder.Append '0' |> ignore
      if n < max then builder.Append ',' |> ignore
    builder.Append '|' |> ignore
    string builder

  let processResets () =
    for _ in 0 .. resetEvents.Count - 1 do
      match resetEvents.TryPeek() with
      | true, (prevFrame, mapping) when prevFrame < frame ->
        mapping.Pin.Spread <- makeSpread mapping.Pin.SliceCount
        do resetEvents.TryDequeue() |> ignore
      | _ -> ()

  interface IPluginEvaluate with
    member self.Evaluate (_: int) : unit =
      if self.InUpdate.[0] then
        for mapping in self.InNodeMappings do
          for cmd in self.InCommands do
            match cmd with
            /// update the name property and slices
            | UpdatePin pin when pin.Id = mapping.PinId ->
              match mapping.Pin.ParentNode.FindPin Settings.DESCRIPTIVE_NAME_PIN with
              | null -> ()
              | ipin -> ipin.[0] <- unwrap pin.Name
              mapping.Pin.Spread <- pin.Slices.ToSpread()

            /// process updates to pin slices
            | UpdateSlices map when Map.containsKey mapping.PinId map.Slices ->
              match map.Slices.[mapping.PinId] with
              // ignore resets
              | BoolSlices (_,_,trig,values) when trig && not (Array.contains true values) -> ()
              | slices ->
                mapping.Pin.Spread <- map.Slices.[mapping.PinId].ToSpread()
                if mapping.Trigger then
                  // schedule frame accurate reset
                  resetEvents.Enqueue(frame, mapping)
            | _ -> ()
      do processResets()
      do bumpFrame()
