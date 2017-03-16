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

  // ** visibleInputPins

  let private visibleInputPins (pins: IPin2 seq) =
    pins
    |> Seq.filter (fun pin -> pin.Direction = PinDirection.Input)
    |> Seq.filter (fun pin -> pin.Visibility = PinVisibility.True)

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
        let! value =
          try
            uint16 pin.SliceCount
            |> Either.succeed
          with | _ -> Right 1us
        return VecSize.Fixed value
    }

  // ** parseBoolValues

  let private parseBoolValues (pin: IPin2) =
    let result = new ResizeArray<bool>()
    for i in 0 .. pin.SliceCount - 1 do
      match pin.[i] with
      | "1" -> result.Add true
      | _   -> result.Add false
    result.ToArray()

  // ** parseDoubleValues

  let private parseDoubleValues (pin: IPin2) : double array =
    let result = new ResizeArray<double>()
    for i in 0 .. pin.SliceCount - 1 do
      try double pin.[i]
      with | _ -> 0.0
      |> result.Add
    result.ToArray()

  // ** parseStringValues

  let private parseStringValues (pin: IPin2) =
    let result = new ResizeArray<string>()
    for i in 0 .. pin.SliceCount - 1 do
      let value = if isNull pin.[i] then "" else pin.[i]
      result.Add value
    result.ToArray()

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

  // ** parseSeqWith

  let private parseSeqWith (parse: IPin2 -> Either<IrisError,Pin>) (state: PluginState) (pins: IPin2 seq) =
    Seq.fold
      (fun lst pin ->
        match parse pin with
        | Right parsed -> parsed :: lst
        | Left error ->
          error
          |> string
          |> Util.error state
          lst)
      []
      pins

  // ** parseValuesPins

  let private parseValuePins (state: PluginState) (pins: IPin2 seq) =
    parseSeqWith parseValuePin state pins

  // ** parseValueBox

  let private parseValueBox (state: PluginState) (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseValuePins state

  // ** parseStringType

  let private parseStringType (pin: IPin2) =
    either {
      let! st = findPin Settings.STRING_TYPE_PIN pin.ParentNode.Pins
      return! Iris.Core.Behavior.TryParse st.[0]
    }

  // ** parseMaxChars

  let private parseMaxChars (pin: IPin2) =
    either {
      let! mc = findPin Settings.MAXCHAR_PIN pin.ParentNode.Pins
      let! value =
        try
          mc.[0]
          |> Int32.Parse
          |> Either.succeed
        with
          | _ -> Either.succeed -1
      return value
    }

  // ** parseStringPin

  let private parseStringPin (pin: IPin2) =
    either {
      let id = parseNodePath pin
      let dir = parseDirection pin
      let grp = parsePinGroupId pin
      let! st = parseStringType pin
      let! name = parseName pin
      let! vc = parseVecSize pin
      let! maxchars = parseMaxChars pin

      return StringPin {
        Id = Id id
        Name = name
        PinGroup = grp
        Tags = [| |]
        Direction = dir
        Behavior = st
        MaxChars = maxchars
        VecSize = vc
        Labels = [| |]
        Values = parseStringValues pin
      }
    }

  // ** parseStringPins

  let private parseStringPins (state: PluginState) (pins: IPin2 seq) =
    parseSeqWith parseStringPin state pins

  // ** parseStringBox

  let private parseStringBox (state: PluginState) (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseStringPins state

  // ** parseEnumProperties

  let private parseEnumProperties (pin: IPin2) =
    let properties = new ResizeArray<Property>()
    match pin.ParentNode.FindPin Settings.INPUT_ENUM_PIN with
    | null -> properties.ToArray()
    | pin ->
      let name =
        let start = pin.SubType.IndexOf(',') + 2
        let len = pin.SubType.LastIndexOf(',') - pin.SubType.IndexOf(',') - 2
        pin.SubType.Substring(start, len)
      let mutable count = EnumManager.GetEnumEntryCount name
      while count > 0 do
        count <- count - 1
        (name, count)
        |> EnumManager.GetEnumEntry
        |> (fun (ety: EnumEntry) -> { Key = string ety.Index; Value = ety.Name })
        |> properties.Add
      properties.ToArray()

  // ** parseEnumValues

  let private parseEnumValues (props: Property array) (pin: IPin2) =
    let valueToProp (str: string) =
      Array.fold
        (fun m prop ->
          match m with
          | Some _ -> m
          | None ->
            if prop.Value = str then
              Some prop
            else None)
        None
        props
    let result = new ResizeArray<Property>()
    let values = parseStringValues pin
    for value in values do
      match valueToProp value with
      | Some prop -> result.Add prop
      | None -> ()
    result.ToArray()

  // ** parseEnumPin

  let private parseEnumPin (pin: IPin2) =
    either {
      let id = parseNodePath pin
      let dir = parseDirection pin
      let grp = parsePinGroupId pin
      let! name = parseName pin
      let! vc = parseVecSize pin
      let props = parseEnumProperties pin

      return EnumPin {
        Id = Id id
        Name = name
        PinGroup = grp
        Direction = dir
        VecSize = vc
        Properties = props
        Tags = [| |]
        Labels = [| |]
        Values = parseEnumValues props pin
      }
    }

  // ** parseEnumPins

  let private parseEnumPins (state: PluginState) (pins: IPin2 seq) =
    parseSeqWith parseEnumPin state pins

  // ** parseEnumBox

  let private parseEnumBox (state: PluginState) (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseEnumPins state

  // ** parseColorValues

  let private parseColorValues (pin: IPin2) =
    let result = new ResizeArray<ColorSpace>()
    for i in 0 .. pin.SliceCount - 1 do
      match String.split [| ',' |] pin.[i] with
      | [| red; green; blue; alpha |] ->
        try
          { Red = uint8 (float red * 255.0)
            Green = uint8 (float green * 255.0)
            Blue = uint8 (float blue * 255.0)
            Alpha = uint8 (float alpha * 255.0) }
          |> RGBA
          |> result.Add
        with
          | _ -> result.Add ColorSpace.Black
      | _ -> result.Add ColorSpace.Black

    result.ToArray()

  // ** parseColorPin

  let private parseColorPin (pin: IPin2) =
    either {
      let id = parseNodePath pin
      let dir = parseDirection pin
      let grp = parsePinGroupId pin
      let! name = parseName pin
      let! vc = parseVecSize pin

      return ColorPin {
        Id = Id id
        Name = name
        PinGroup = grp
        Direction = dir
        VecSize = vc
        Tags = [| |]
        Labels = [| |]
        Values = parseColorValues pin
      }
    }

  // ** parseColorPins

  let private parseColorPins (state: PluginState) (pins: IPin2 seq) =
    parseSeqWith parseColorPin state pins

  // ** parseColorBox

  let private parseColorBox (state: PluginState) (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseColorPins state

  // ** parseINode2

  let private parseINode2 (state: PluginState) (node: INode2) : Either<IrisError,Pin list> =
    for pin in visibleInputPins node.Pins do
      // sprintf "name: %s direction: %A visible: %A type: %s subtype: %s value: %A"
      //    pin.Name
      //    pin.Direction
      //    pin.Visibility
      //    pin.Type
      //    pin.SubType
      //    pin.[0]
      Util.debug state (sprintf "%s <==> %A" pin.Name pin.Spread)
      for n in 0 .. pin.SliceCount - 1 do
        sprintf "  [%d] %A" n pin.[n]
        |> Util.debug state

    either {
      let! boxtype = IOBoxType.TryParse node.Name
      match boxtype with
      | IOBoxType.Value ->
        return parseValueBox state node
      | IOBoxType.String ->
        return parseStringBox state node
      | IOBoxType.Enum ->
        return parseEnumBox state node
      | IOBoxType.Color ->
        return parseColorBox state node
      | x ->
        return!
          sprintf "unsupported type %A" x
          |> Error.asParseError "parseINode2"
          |> Either.fail
    }

  let private parsePinIds (pins: IPin2 seq) =
    Seq.fold
      (fun lst pin ->
        let pinid = Id (parseNodePath pin)
        let grpid = parsePinGroupId pin
        (grpid,pinid) :: lst)
      []
      pins

  let private parseINode2Ids (_: PluginState) (node: INode2)  =
    node.Pins
    |> visibleInputPins
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
