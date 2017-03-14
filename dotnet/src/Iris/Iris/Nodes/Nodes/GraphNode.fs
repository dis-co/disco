namespace VVVV.Nodes

// * Imports

open System
open System.Web
open System.Threading
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
    | GraphUpdate

  // ** PluginState

  type PluginState =
    { Frame: uint64
      Initialized: bool
      Pins: ConcurrentDictionary<Id,PinGroup>
      Events: ConcurrentQueue<Msg>
      Logger: ILogger
      V1Host: IPluginHost
      V2Host: IHDEHost
      InCommands: IDiffSpread<StateMachine>
      InDebug: ISpread<bool>
      OutPinGroups: ISpread<PinGroup>
      OutCommands: ISpread<StateMachine>
      OutUpdate: ISpread<bool>
      Disposables: ConcurrentDictionary<Id,IDisposable> }

    static member Create () =
      { Frame = 0UL
        Initialized = false
        Pins = new ConcurrentDictionary<Id,PinGroup>()
        Events = new ConcurrentQueue<Msg>()
        Logger = null
        V1Host = null
        V2Host = null
        InCommands = null
        InDebug = null
        OutPinGroups = null
        OutCommands = null
        OutUpdate = null
        Disposables = new ConcurrentDictionary<Id,IDisposable>() }

    interface IDisposable with
      member self.Dispose() =
        Seq.iter (fun (kv: KeyValuePair<Id,IDisposable>) -> dispose kv.Value) self.Disposables
        self.Disposables.Clear()


  // ** IOBoxType

  [<RequireQualifiedAccess>]
  type private IOBoxType =
    | Value
    | String
    | Node
    | Enum
    | Color

    override tipe.ToString() =
      match tipe with
      | Value -> "IOBox (Value Advanced)"
      | String -> "IOBox (String)"
      | Node -> "IOBox (Node)"
      | Color -> "IOBox (Color)"
      | Enum -> "IOBox (Enumerations)"

    static member Parse (str: string) =
      match str with
      | "IOBox (Value Advanced)" -> Value
      | "IOBox (String)" -> String
      | "IOBox (Node)" -> Node
      | "IOBox (Color)" -> Color
      | "IOBox (Enumerations)" -> Enum
      | _ -> failwithf "unknown type: %s" str

    static member TryParse (str: string) =
      try
        str
        |> IOBoxType.Parse
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asParseError "IOBoxType"
          |> Either.fail

  // ** PinType

  [<RequireQualifiedAccess>]
  type private PinType =
    | Value
    | String

  // ** ValueType

  [<RequireQualifiedAccess>]
  type private ValueType =
    | Boolean
    | Integer
    | Real

    override tipe.ToString() =
      match tipe with
      | Boolean -> "Boolean"
      | Integer -> "Integer"
      | Real -> "Real"

    static member Parse (str: string) =
      match str with
      | "Boolean" -> Boolean
      | "Integer" -> Integer
      | "Real" -> Real
      | _ -> failwithf "unknown type: %s" str

    static member TryParse (str: string) =
      try
        str
        |> ValueType.Parse
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asParseError "ValueType.TryParse"
          |> Either.fail

    static member IsBool (vt: ValueType) =
      match vt with
      | Boolean -> true
      | _ -> false

  // ** Behavior

  type private Behavior =
    | Toggle
    | Press
    | Bang

    override behavior.ToString() =
      match behavior with
      | Toggle -> "Toggle"
      | Press -> "Press"
      | Bang -> "Bang"

    static member Parse (str: string) =
      match str with
      | "Toggle" -> Toggle
      | "Press" -> Press
      | "Bang" -> Bang
      | _ -> failwithf "unknown behavior %s" str

    static member TryParse (str: string) =
      try
        str
        |> Behavior.Parse
        |> Either.succeed
      with
        | exn ->
          exn.Message
          |> Error.asParseError "Behavior.TryParse"
          |> Either.fail

    static member IsTrigger (bh: Behavior) =
      match bh with
      | Bang -> true
      | _ -> false

  // ** findPin

  let private findPin (name: string) (pins: IPin2 seq) =
    Seq.fold
      (fun (m: Either<IrisError,IPin2>) (pin: IPin2) ->
        match m with
        | Right _ -> m
        | Left error ->
          if pin.Name = name then
            Right pin
          else
            Left error)
      (Left (Other("findPin", (sprintf "could not find pin %A" name))))
      pins

  // ** visibleOutputPins

  let private visibleOutputPins (pins: IPin2 seq) =
    pins
    |> Seq.filter (fun pin -> pin.Direction = PinDirection.Output)
    |> Seq.filter (fun pin -> pin.Visibility = PinVisibility.True)

  // ** parseNodePath

  let private parseNodePath (pin: IPin2) =
    sprintf "%s/%s"
      (pin.ParentNode.GetNodePath(false))
      pin.Name

  // ** parseDescriptivePath

  let private parseDescriptivePath (pin: IPin2) =
    sprintf "%s/%s"
      (pin.ParentNode.GetNodePath(true))
      pin.Name

  // ** parseValueType

  let private parseValueType (pin: IPin2) =
    either {
      let! vtp = findPin Settings.VALUE_TYPE_PIN pin.ParentNode.Pins
      return! ValueType.TryParse vtp.[0]
    }

  // ** parseBehavior

  let private parseBehavior (pin: IPin2) =
    either {
      let! bhp = findPin Settings.BEHAVIOR_PIN pin.ParentNode.Pins
      return! Behavior.TryParse bhp.[0]
    }

  // ** parseName

  let private parseName (pin: IPin2) =
    either {
      let! np = findPin Settings.DESCRIPTIVE_NAME_PIN pin.ParentNode.Pins
      return np.[0]
    }

  // ** parsePinGroupId

  let private parsePinGroupId (pin: IPin2) =
    let id = pin.ParentNode.Parent.GetNodePath(false)
    Id id

  // ** parseDirection

  let private parseDirection (pin: IPin2) =
    match pin.Direction with
    | PinDirection.Input -> ConnectionDirection.Input
    | PinDirection.Output -> ConnectionDirection.Output
    | _ -> ConnectionDirection.Input

  // ** parseVecSize

  let private parseVecSize (pin: IPin2) =
    either {
      let! mp = findPin Settings.SLICECOUNT_MODE_PIN pin.ParentNode.Pins
      match mp.[0] with
      | "Input" ->
        return VecSize.Dynamic
      | _ ->
        let! vsp = findPin Settings.VECSIZE_PIN pin.ParentNode.Pins
        let! value =
          try
            UInt16.Parse vsp.[0]
            |> Either.succeed
          with | _ -> Right 1us
        return VecSize.Fixed value
    }

  // ** parseBoolValues

  let private parseBoolValues (pin: IPin2) =
    pin.Spread
    |> String.split [| ',' |]
    |> Array.map (fun v -> try Boolean.Parse v with | _ -> false)

  // ** parseDoubleValues

  let private parseDoubleValues (pin: IPin2) : double array =
    pin.Spread
    |> String.split [| ',' |]
    |> Array.map (fun v -> try Double.Parse v with | _ -> 0.0)

  // ** parseMin

  let private parseMin (pin: IPin2) =
    either {
      let! min = findPin Settings.MIN_PIN pin.ParentNode.Pins
      let! value =
        try
          min.[0]
          |> Int32.Parse
          |> Either.succeed
        with
          | _ ->
            Either.succeed -99999999
      return value
    }

  // ** parseMax

  let private parseMax (pin: IPin2) =
    either {
      let! max = findPin Settings.MAX_PIN pin.ParentNode.Pins
      let! value =
        try
          max.[0]
          |> Int32.Parse
          |> Either.succeed
        with
          | _ ->
            Either.succeed 99999999
      return value
    }

  // ** parseUnits

  let private parseUnits (pin: IPin2) =
    either {
      let! units = findPin Settings.UNITS_PIN pin.ParentNode.Pins
      return if isNull units.[0] then "" else units.[0]
    }

  // ** parsePrecision

  let private parsePrecision (pin: IPin2) =
    either {
      let! precision = findPin Settings.PRECISION_PIN pin.ParentNode.Pins
      let! value =
        try
          precision.[0]
          |> UInt32.Parse
          |> Either.succeed
        with
          | _ -> Either.succeed 4ul
      return value
    }

  // ** parseValuePin

  let private parseValuePin (pin: IPin2) =
    either {
      let id = parseNodePath pin
      let dir = parseDirection pin
      let grp = parsePinGroupId pin
      let! vt = parseValueType pin
      let! bh = parseBehavior pin
      let! name = parseName pin
      let! vc = parseVecSize pin

      match vt with
      | ValueType.Boolean ->
        return BoolPin {
          Id = Id id
          Name = name
          PinGroup = grp
          Tags = [| |]
          Direction = dir
          IsTrigger = Behavior.IsTrigger bh
          VecSize = vc
          Labels = [| |]
          Values = parseBoolValues pin
        }
      | ValueType.Integer ->
        let! min = parseMin pin
        let! max = parseMax pin
        let! unit = parseUnits pin
        return NumberPin {
          Id = Id id
          Name = name
          PinGroup = grp
          Tags = [| |]
          Min = min
          Max = max
          Unit = unit
          Precision = 0ul
          Direction = dir
          VecSize = vc
          Labels = [| |]
          Values = parseDoubleValues pin
        }
      | ValueType.Real ->
        let! min = parseMin pin
        let! max = parseMax pin
        let! unit = parseUnits pin
        let! prec = parsePrecision pin
        return NumberPin {
          Id = Id id
          Name = name
          PinGroup = grp
          Tags = [| |]
          Min = min
          Max = max
          Unit = unit
          Precision = prec
          Direction = dir
          VecSize = vc
          Labels = [| |]
          Values = parseDoubleValues pin
        }
    }

  // ** parseValuesPins

  let private parseValuePins (pins: IPin2 seq) =
    Seq.fold
      (fun lst pin ->
        match parseValuePin pin with
        | Right parsed -> parsed :: lst
        | _ -> lst)
      []
      pins

  // ** parseValueBox

  let private parseValueBox (node: INode2) =
    node.Pins
    |> visibleOutputPins
    |> parseValuePins

  // ** parseINode2

  let private parseINode2 (state: PluginState) (node: INode2) : Either<IrisError,Pin list> =
    for pin in node.Pins do
      sprintf "name: %s direction: %A visible: %A type: %s subtype: %s value: %A"
        pin.Name
        pin.Direction
        pin.Visibility
        pin.Type
        pin.SubType
        pin.[0]
      |> Util.debug state

    either {
      let! boxtype = IOBoxType.TryParse node.Name
      match boxtype with
      | IOBoxType.Value -> return parseValueBox node
      | _ -> return (failwith "never")
    }

  let private parsePinIds (pins: IPin2 seq) =
    Seq.fold
      (fun lst pin ->
        let pinid = Id (parseNodePath pin)
        let grpid = parsePinGroupId pin
        (grpid,pinid) :: lst)
      []
      pins

  let private parseINode2Ids (state: PluginState) (node: INode2)  =
    node.Pins
    |> visibleOutputPins
    |> parsePinIds

  // ** addPin

  let private addPin (state: PluginState) (pin: Pin) =
    let group =
      if state.Pins.ContainsKey pin.PinGroup then
        pin.PinGroup
        |> string
        |> sprintf "Group already exists, adding pin to group: %s"
        |> Util.debug state
        let group = state.Pins.[pin.PinGroup]
        { group with Pins = Map.add pin.Id pin group.Pins }
      else
        pin.PinGroup
        |> string
        |> sprintf "Group not found, creating: %s"
        |> Util.debug state
        let node = state.V2Host.GetNodeFromPath(string pin.PinGroup)
        { Id = pin.PinGroup
          Name = node.GetNodePath(true)
          Pins = Map.ofList [ (pin.Id,pin) ] }
    state.Pins.AddOrUpdate(group.Id, group, group |> konst >> konst)
    |> ignore

  // ** removePin

  let private removePin (state: PluginState) (groupid, pinid) =
    match state.Pins.TryGetValue(groupid) with
    | true, group ->
      if Map.containsKey pinid group.Pins then
        let updated = Map.remove pinid group.Pins
        let length = Map.fold (fun count _ _ -> count + 1) 0 updated
        if length = 0 then
          while not (state.Pins.TryRemove(groupid) |> fst) do
            Thread.Sleep(TimeSpan.FromTicks(1L))
          groupid
          |> string
          |> sprintf "Group empty, removed: %s"
          |> Util.debug state
        else
          let group = { group with Pins = updated }
          state.Pins.AddOrUpdate(groupid, group, group |> konst >> konst)
          |> ignore
          pinid
          |> string
          |> sprintf "Group updated, removed: %s"
          |> Util.debug state
        state.Events.Enqueue Msg.GraphUpdate
    | _ -> ()

  // ** onNodeExposed

  let private onNodeExposed (state: PluginState) (node: INode2) =
    match parseINode2 state node with
    | Right [] -> ()
    | Right pins ->
      List.iter (addPin state) pins
      state.Events.Enqueue Msg.GraphUpdate
    | Left error ->
      error
      |> string
      |> Util.error state

  // ** onNodeUnExposed

  let private onNodeUnExposed (state: PluginState) (node: INode2) =
    parseINode2Ids state node
    |> List.iter (removePin state)

  // ** setupVvvv

  let private setupVvvv (state: PluginState) =
    let globals = Id "globals"
    if not (state.Disposables.ContainsKey globals) then
      let onNodeAdded = new NodeEventHandler(onNodeExposed state)
      let onNodeRemoved = new NodeEventHandler(onNodeUnExposed state)
      state.V2Host.ExposedNodeService.add_NodeAdded(onNodeAdded)
      state.V2Host.ExposedNodeService.add_NodeRemoved(onNodeRemoved)
      let disposable =
        { new IDisposable with
            member self.Dispose () =
              state.V2Host.ExposedNodeService.remove_NodeAdded(onNodeAdded)
              state.V2Host.ExposedNodeService.remove_NodeRemoved(onNodeRemoved) }
      while not (state.Disposables.TryAdd(globals,disposable)) do
        Thread.Sleep(TimeSpan.FromTicks(1L))
      { state with Initialized = true }
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
    let update = state.Events.Count > 0
    while state.Events.Count > 0 do
      match state.Events.TryDequeue() with
      | true, msg ->
        match msg with
        | Msg.GraphUpdate ->
          let values = new ResizeArray<PinGroup>()
          for KeyValue(_,value) in state.Pins.ToArray() do
            values.Add value
          if values.Count > 0 then
            state.OutPinGroups.SliceCount <- values.Count
            state.OutPinGroups.AssignFrom values
          else
            state.OutPinGroups.SliceCount <- 1
            state.OutPinGroups.AssignFrom(new ResizeArray<PinGroup>())
      | _ -> ()

    if update then
      state.OutUpdate.[0] <- true
    else
      state.OutUpdate.[0] <- false

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

[<PluginInfo(Name="PinGroupTest", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type PinGroupTstNode() =

  let groups =
    let arr = new ResizeArray<PinGroup>()
    arr.Add { Id = Id.Create(); Name = "Group 1"; Pins = Map.empty }
    arr.Add { Id = Id.Create(); Name = "Group 2"; Pins = Map.empty }
    arr

  [<DefaultValue>]
  [<Output("PinGroups")>]
  val mutable OutPinGroups: ISpread<PinGroup>

  [<DefaultValue>]
  [<Input("On", IsSingle = true)>]
  val mutable InOn: ISpread<bool>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if self.InOn.[0] then
        self.OutPinGroups.SliceCount <- 2
        self.OutPinGroups.AssignFrom groups
      else
        self.OutPinGroups.SliceCount <- 1
        self.OutPinGroups.AssignFrom(new ResizeArray<PinGroup>())

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
  [<Output("PinGroups")>]
  val mutable OutPinGroups: ISpread<PinGroup>

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
              OutUpdate = self.OutUpdate
              OutCommands = self.OutCommands
              OutPinGroups = self.OutPinGroups }
        state <- state'
        initialized <- true

      state <- Graph.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state
