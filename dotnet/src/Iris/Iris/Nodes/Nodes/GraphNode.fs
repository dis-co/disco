namespace VVVV.Nodes

// * Imports

open System
open System.Web
open System.ComponentModel.Composition
open System.Collections.Generic
open System.Collections.Concurrent
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.PluginInterfaces.V2.Graph
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open Iris.Raft
open Iris.Core
open Iris.Nodes
open Newtonsoft.Json
open FSharpx.Functional

// * Graph

[<RequireQualifiedAccess>]
module Graph =

  // ** Msg

  [<RequireQualifiedAccess>]
  type Msg =
    | AddPin of Pin
    | RemovePin of Pin

  // ** PluginState

  type PluginState =
    { Frame: uint64
      Initialized: bool
      Events: ConcurrentQueue<obj>
      Logger: ILogger
      V1Host: IPluginHost
      V2Host: IHDEHost
      InCommands: IDiffSpread<StateMachine>
      InDebug: ISpread<bool>
      OutState: ISpread<State>
      OutCommands: ISpread<StateMachine>
      Disposables: Map<Id,IDisposable> }

    static member Create () =
      { Frame = 0UL
        Initialized = false
        Events = new ConcurrentQueue<obj>()
        Logger = null
        V1Host = null
        V2Host = null
        InCommands = null
        InDebug = null
        OutState = null
        OutCommands = null
        Disposables = Map.empty }

    interface IDisposable with
      member self.Dispose() =
        Map.iter (konst dispose) self.Disposables

  // ** pin

  let private getPinByName (node: INode2) (name: string) =
    Seq.fold
      (fun (m: IPin2 option) (pin: IPin2) ->
        match m with
        | Some _ -> m
        | None ->
          if pin.Name = name then
            Some pin
          else
            None)
      None
      node.Pins

  // ** parseINode2

  let private parseINode2 (_: INode2) : Either<IrisError,Pin> =
    Pin.Toggle(Id.Create(),"Hello",Id.Create(), [| |], [| |])
    |> Either.succeed

  // ** onNodeExposed

  let private onNodeExposed (state: PluginState) (node: INode2) =
    match parseINode2 node with
    | Right pin -> state.Events.Enqueue (Msg.AddPin pin)
    | Left error -> error |> string |> Util.error state

  // ** onNodeUnExposed

  let private onNodeUnExposed (state: PluginState) (node: INode2) =
    match parseINode2 node with
    | Right pin -> state.Events.Enqueue (Msg.RemovePin pin)
    | Left error ->
      error
      |> string
      |> Util.error state

  // ** setupVvvv

  let private setupVvvv (state: PluginState) =
    let globals = Id "globals"
    if not (Map.containsKey globals state.Disposables) then
      let onNodeAdded = new NodeEventHandler(onNodeExposed state)
      let onNodeRemoved = new NodeEventHandler(onNodeUnExposed state)
      state.V2Host.ExposedNodeService.add_NodeAdded(onNodeAdded)
      state.V2Host.ExposedNodeService.add_NodeRemoved(onNodeRemoved)
      let disposable =
        { new IDisposable with
            member self.Dispose () =
              state.V2Host.ExposedNodeService.remove_NodeAdded(onNodeAdded)
              state.V2Host.ExposedNodeService.remove_NodeRemoved(onNodeRemoved) }
      { state with
          Initialized = true
          Disposables = Map.add globals disposable state.Disposables }
    else
      state

  // ** initialize

  let private initialize (state: PluginState) =
    if not state.Initialized then
      setupVvvv state
    else
      state

  // ** processing

  let private processing (state: PluginState) =
    state

  let evaluate (state: PluginState) (_: int) =
    state
    |> initialize
    |> processing

  // ------------  Call Graph -------------------
  //
  // Evaluate
  //    |
  //    Process (update our world)
  //    |  |
  //    |  CallCue &&  UpdatePin
  //    |      |
  //    |      pin.Update (either values, or entire pin)
  //    |      |
  //    |      MkQueueJob value
  //    |
  //    Tick  (now flush it to vvvv)
  //    |  |
  //    |  VVVVGraph.FrameCount <= CurrentFrame
  //    |  |
  //    |  ProcessGraphWrites
  //    |         |
  //    |         IPin2.Spread = "|val|"
  //    |         |
  //    |         MkQueueJob value (Reset with current frame + 1)
  //    |
  //    Cleanup

// * GraphNode

[<PluginInfo(Name="Graph", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type GraphNode() =

  [<Import();DefaultValue>]
  val mutable V1Host: IPluginHost

  [<Import();DefaultValue>]
  val mutable V2Host: IHDEHost

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<DefaultValue>]
  [<Input("Commands")>]
  val mutable InCommands: IDiffSpread<StateMachine>

  [<DefaultValue>]
  [<Input("Debug", IsSingle = true, DefaultValue = 0.0)>]
  val mutable InDebug: IDiffSpread<bool>

  [<DefaultValue>]
  [<Output("Commands")>]
  val mutable OutCommands: ISpread<StateMachine>

  [<DefaultValue>]
  [<Output("State", IsSingle = true)>]
  val mutable OutState: ISpread<Iris.Core.State>

  [<DefaultValue>]
  [<Output("Update", IsSingle = true, IsBang = true)>]
  val mutable OutUpdate: ISpread<bool>

  let mutable initialized = false
  let mutable state = Unchecked.defaultof<Graph.PluginState>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if not initialized then
        let state' =
          { Graph.PluginState.Create() with
              V1Host = self.V1Host
              V2Host = self.V2Host
              Logger = self.Logger
              InDebug = self.InDebug
              OutState = self.OutState }
        state <- state'
        initialized <- true

      state <- Graph.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state
