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
    | PinAdded           of pin:IPin2 * parsed:Pin
    | PinRemoved         of group:Id * pin:Id
    | PinValueChange     of group:Id * slices:Slices
    | PinSubTypeChange   of string       // node id
    | PinVecSizeChange   of group:Id * pin:Id * VecSize
    | PinNameChange      of group:Id * pin:Id * string
    | PinTagChange       of group:Id * pin:Id * Tag array
    | PinDirectionChange of group:Id * pin:Id * ConnectionDirection

  // ** PluginState

  type PluginState =
    { Frame: uint64
      Initialized: bool
      Update: bool ref
      Pins: Dictionary<Id,PinGroup>
      Commands: ResizeArray<StateMachine>
      NodeMappings: Dictionary<Id,NodeMapping>
      Events: ConcurrentQueue<Msg>
      Logger: ILogger
      V1Host: IPluginHost
      V2Host: IHDEHost
      InDebug: ISpread<bool>
      InClientId: ISpread<string>
      OutPinGroups: ISpread<PinGroup>
      OutCommands: ISpread<StateMachine>
      OutNodeMappings: ISpread<NodeMapping>
      OutUpdate: ISpread<bool>
      Disposables: Dictionary<Id,IDisposable> }

    static member Create () =
      { Frame = 0UL
        Initialized = false
        Update = ref false
        Pins = new Dictionary<Id,PinGroup>()
        Commands = new ResizeArray<StateMachine>()
        NodeMappings = new Dictionary<Id,NodeMapping>()
        Events = new ConcurrentQueue<Msg>()
        Logger = null
        V1Host = null
        V2Host = null
        InDebug = null
        InClientId = null
        OutPinGroups = null
        OutCommands = null
        OutUpdate = null
        OutNodeMappings = null
        Disposables = new Dictionary<Id,IDisposable>() }

    interface IDisposable with
      member self.Dispose() =
        Seq.iter (fun (kv: KeyValuePair<Id,IDisposable>) -> dispose kv.Value) self.Disposables
        self.Disposables.Clear()
        self.NodeMappings.Clear()

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
    |> Seq.filter
      (fun pin ->
        pin.Visibility = PinVisibility.True ||
        pin.Visibility = PinVisibility.Hidden)

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
    |> Id

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

  // ** parseTags

  let private parseTags (str: string) =
     str.Split [| ',' |]

  // ** parsePinGroupId

  let private parsePinGroupId (pin: IPin2) =
    let id = pin.ParentNode.Parent.GetNodePath(false)
    Id id

  // ** parseDirection

  let private parseDirection (pin: IPin2) =
    if pin.IsConnected() then
      ConnectionDirection.Output
    else
      ConnectionDirection.Input

  // ** parseVecSize

  let private parseVecSize (pin: IPin2) =
    either {
      let mp = pin.ParentNode.FindPin Settings.SLICECOUNT_MODE_PIN
      match mp.[0] with
      | "Input" -> return VecSize.Dynamic
      | _ ->
        let cols =
          try
            pin.ParentNode.FindPin(Settings.COLUMNS_PIN).[0]
            |> uint16
          with | _ -> 1us

        let rows =
          try
            pin.ParentNode.FindPin(Settings.ROWS_PIN).[0]
            |> uint16
          with | _ -> 1us

        let pages =
          try
            pin.ParentNode.FindPin(Settings.PAGES_PIN).[0]
            |> uint16
          with | _ -> 1us

        return VecSize.Fixed (cols * rows * pages)
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

  // ** parsePinId

  let private parsePinId (id: Id) =
    let parsed = string id |> String.split [| '/' |]
    let idx = Array.length parsed - 1
    match Array.splitAt idx parsed with
    | nodepath, [| name |] ->
      (String.Join("/", nodepath), name)
    | _ -> failwithf "wrong format: %s" (string id)

  // ** findPinById

  let private findPinById (state: PluginState) (id: Id) =
    let path, name = parsePinId id
    let node = state.V2Host.GetNodeFromPath(path)
    if not (isNull node) then
      let pin = node.FindPin name
      if not (isNull pin) then Some pin else None
    else None

  // ** parsePinValues

  let private parsePinValues (pin: IPin2) =
    either {
      let node = pin.ParentNode
      let id = parseNodePath pin
      let! bt = IOBoxType.TryParse (node.NodeInfo.ToString())
      match bt with
      | IOBoxType.Value ->
        let! vt =  parseValueType pin
        match vt with
        | ValueType.Boolean ->
          return BoolSlices(id, parseBoolValues pin)
        | ValueType.Integer | ValueType.Real ->
          return NumberSlices(id, parseDoubleValues pin)
      | IOBoxType.String ->
        return StringSlices(id, parseStringValues pin)
      | IOBoxType.Color ->
        return ColorSlices(id, parseColorValues pin)
      | IOBoxType.Enum ->
        let props = parseEnumProperties pin
        return EnumSlices(id, parseEnumValues props pin)
      | x ->
        return!
          x
          |> sprintf "unsupported type: %A"
          |> Error.asParseError "parsePinValues"
          |> Either.fail
    }

  // ** parsePinValueWith

  let private parsePinValueWith (tipe: PinType) (pid: Id) (props: Property array) (pin: IPin2) =
    match tipe with
    | PinType.Boolean -> BoolSlices(pid, parseBoolValues pin)
    | PinType.Number -> NumberSlices(pid, parseDoubleValues pin)
    | PinType.String -> StringSlices(pid, parseStringValues pin)
    | PinType.Color -> ColorSlices(pid, parseColorValues pin)
    | PinType.Enum -> EnumSlices(pid, parseEnumValues props pin)

  // ** parsePinIds

  let private parsePinIds (pins: IPin2 seq) =
    Seq.fold
      (fun lst pin ->
        let pinid = parseNodePath pin
        let grpid = parsePinGroupId pin
        (grpid,pinid) :: lst)
      []
      pins

  // ** parsePintype

  let private parsePinType (pin: IPin2) =
    either {
      let node = pin.ParentNode
      let! boxtype = IOBoxType.TryParse (node.NodeInfo.ToString())
      match boxtype with
      | IOBoxType.Value ->
        let! vt = parseValueType pin
        match vt with
        | ValueType.Boolean ->
          return PinType.Boolean
        | ValueType.Integer | ValueType.Real ->
          return PinType.Number
      | IOBoxType.String -> return PinType.String
      | IOBoxType.Enum -> return PinType.Enum
      | IOBoxType.Color -> return PinType.Color
      | x ->
        return!
          sprintf "unsupported type %A" x
          |> Error.asParseError "parseINode2"
          |> Either.fail
    }

  // ** parseINode2Ids

  let private parseINode2Ids (_: PluginState) (node: INode2)  =
    node.Pins
    |> visibleInputPins
    |> parsePinIds

  // ** registerHandlers

  let private registerHandlers (state: PluginState) (pin: IPin2) =
    let group = parsePinGroupId pin
    let id = parseNodePath pin
    let np = pin.ParentNode.FindPin Settings.DESCRIPTIVE_NAME_PIN
    let tp = pin.ParentNode.FindPin Settings.TAG_PIN
    let scmp = pin.ParentNode.FindPin Settings.SLICECOUNT_MODE_PIN
    let cp = pin.ParentNode.FindPin Settings.COLUMNS_PIN
    let rp = pin.ParentNode.FindPin Settings.ROWS_PIN
    let pp = pin.ParentNode.FindPin Settings.PAGES_PIN
    let tipe = parsePinType pin |> Either.get // !!!
    let props = parseEnumProperties pin

    let vecsizeUpdate _ _ =
      match parseVecSize pin with
      | Right vecsize ->
        (group, id, vecsize)
        |> Msg.PinVecSizeChange
        |> state.Events.Enqueue
      | Left error ->
        error |> string |> Util.error state

    let vecsizeHandler = new EventHandler(vecsizeUpdate)
    let columnsHandler = new EventHandler(vecsizeUpdate)
    let rowsHandler = new EventHandler(vecsizeUpdate)
    let pagesHandler = new EventHandler(vecsizeUpdate)

    let nameHandler = new EventHandler(fun _ _ ->
      (group, id, np.[0])
      |> Msg.PinNameChange
      |> state.Events.Enqueue)

    let tagHandler = new EventHandler(fun _ _ ->
      (group, id, parseTags tp.[0])
      |> Msg.PinTagChange
      |> state.Events.Enqueue)

    let changedHandler = new EventHandler(fun _ _ ->
      let slices = parsePinValueWith tipe id props pin
      (group,slices)
      |> Msg.PinValueChange
      |> state.Events.Enqueue)

    let directionUpdate _ _ =
      (group, id, parseDirection pin)
      |> Msg.PinDirectionChange
      |> state.Events.Enqueue

    let connectedHandler = new PinConnectionEventHandler(directionUpdate)
    let disconnectedHandler = new PinConnectionEventHandler(directionUpdate)

    let subtypeHandler = new EventHandler(fun _  _ ->
      pin.ParentNode.GetNodePath(false)
      |> Msg.PinSubTypeChange
      |> state.Events.Enqueue)

    np.Changed.AddHandler(nameHandler)
    tp.Changed.AddHandler(tagHandler)
    scmp.Changed.AddHandler(vecsizeHandler)
    cp.Changed.AddHandler(columnsHandler)
    rp.Changed.AddHandler(rowsHandler)
    pp.Changed.AddHandler(pagesHandler)

    pin.Changed.AddHandler(changedHandler)
    pin.Connected.AddHandler(connectedHandler)
    pin.Disconnected.AddHandler(disconnectedHandler)
    pin.SubtypeChanged.AddHandler(subtypeHandler)

    { new IDisposable with
        member disp.Dispose () =
          tp.Changed.RemoveHandler(tagHandler)
          np.Changed.RemoveHandler(nameHandler)
          cp.Changed.RemoveHandler(columnsHandler)
          rp.Changed.RemoveHandler(rowsHandler)
          pp.Changed.RemoveHandler(pagesHandler)
          scmp.Changed.RemoveHandler(vecsizeHandler)

          pin.Changed.RemoveHandler(changedHandler)
          pin.Connected.RemoveHandler(connectedHandler)
          pin.Disconnected.RemoveHandler(disconnectedHandler)
          pin.SubtypeChanged.RemoveHandler(subtypeHandler)
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
          Id = id
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
          Id = id
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
          Id = id
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

  type private Parser = IPin2 -> Either<IrisError,Pin>

  let private parseSeqWith (parse: Parser) (state: PluginState) (pins: IPin2 seq) =
    Seq.fold
      (fun lst pin ->
        match parse pin with
        | Right parsed ->
          (pin, parsed) :: lst
        | Left error ->
          error
          |> string
          |> Util.debug state
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
        Id = id
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
        Id = id
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

  // ** parseColorPin

  let private parseColorPin (pin: IPin2) =
    either {
      let id = parseNodePath pin
      let dir = parseDirection pin
      let grp = parsePinGroupId pin
      let! name = parseName pin
      let! vc = parseVecSize pin
      return ColorPin {
        Id = id
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

  let private parseINode2 (state: PluginState) (node: INode2) =
    // for pin in node.Pins do
    //   sprintf "name: %s direction: %A visible: %A type: %s subtype: %s value: %A"
    //      pin.Name
    //      pin.Direction
    //      pin.Visibility
    //      pin.Type
    //      pin.SubType
    //      pin.[0]
    //   |> Util.debug state
      // for n in 0 .. pin.SliceCount - 1 do
      //   sprintf "  [%d] %A" n pin.[n]
      //   |> Util.debug state

    either {
      let! boxtype = IOBoxType.TryParse (node.NodeInfo.ToString())
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

  // ** addPin

  let private addPin (state: PluginState) (pin: Pin) =
    if state.Pins.ContainsKey pin.PinGroup then
      let group = state.Pins.[pin.PinGroup]
      state.Pins.[group.Id] <- { group with Pins = Map.add pin.Id pin group.Pins }
      state.Commands.Add (AddPin pin)
    else
      let node = state.V2Host.GetNodeFromPath(string pin.PinGroup)
      let group: PinGroup =
        { Id = pin.PinGroup
          Name = node.GetNodePath(true)
          Client = Id state.InClientId.[0]
          Pins = Map.ofList [ (pin.Id, pin) ] }
      state.Commands.Add (AddPinGroup group)
      state.Commands.Add (AddPin pin)
      state.Pins.Add(group.Id, group) |> ignore

  // ** updatePinWith

  type private Updater = Pin -> Pin

  let private updatePinWith (state: PluginState) (groupid: Id) (pinid: Id) (updater: Updater) =
    match state.Pins.TryGetValue(groupid) with
    | true, group ->
      match Map.tryFind pinid group.Pins with
      | Some pin ->
        state.Pins.[groupid] <- { group with Pins = Map.add pinid (updater pin) group.Pins }
      | _ -> ()
    | _ -> ()

  // ** updatePinValues

  let private updatePinValues (state: PluginState) (group: Id) (slices: Slices) =
    updatePinWith state group slices.Id <| fun oldpin ->
      oldpin.SetSlices slices
    state.Commands.Add (UpdateSlices slices)

  // ** updatePinName

  let private updatePinName (state: PluginState) (group: Id) (pin: Id) (name: string) =
    updatePinWith state group pin <| fun oldpin ->
      let updated = oldpin.SetName name
      state.Commands.Add (UpdatePin updated)
      updated

  // ** updatePinTags

  let private updatePinTags (state: PluginState) (group: Id) (pin: Id) (tags: Tag array) =
    updatePinWith state group pin <| fun oldpin ->
      let updated = oldpin.SetTags tags
      state.Commands.Add (UpdatePin updated)
      updated

  // ** updatePinDirection

  let private updatePinDirection (state: PluginState) (group: Id) (pin: Id) dir =
    updatePinWith state group pin <| fun oldpin ->
      let updated = oldpin.SetDirection dir
      state.Commands.Add (UpdatePin updated)
      updated

  // ** updatePinVecSize

  let private updatePinVecSize (state: PluginState) (group: Id) (pin: Id) vecsize =
    updatePinWith state group pin <| fun oldpin ->
      let updated = oldpin.SetVecSize vecsize
      state.Commands.Add (UpdatePin updated)
      updated

  // ** updatePin

  let private updatePin (state: PluginState) (pin: Pin) =
    updatePinWith state pin.PinGroup pin.Id (konst pin)

  // ** removePin

  let private removePin (state: PluginState) (groupid: Id) (pinid: Id) =
    match state.Pins.TryGetValue(groupid) with
    | true, group ->
      match Map.tryFind pinid group.Pins with
      | Some pin ->
        let updated = Map.remove pinid group.Pins
        let length = Map.fold (fun count _ _ -> count + 1) 0 updated
        state.Commands.Add (RemovePin pin)
        if length = 0 then
          state.Commands.Add (RemovePinGroup group)
          state.Pins.Remove(groupid) |> ignore
        else
          state.Pins.[groupid] <- { group with Pins = updated }
      | _ -> ()
    | _ -> ()

  // ** addDisposable

  let private addDisposable (state: PluginState) (pin: IPin2) =
    let id = parseNodePath pin
    let disposable = registerHandlers state pin
    if state.Disposables.ContainsKey id then
      dispose disposable                // should not happen, and if it does prevent it from working
    else
      state.Disposables.Add(id, disposable)
      |> ignore

  // ** removeDisposable

  let private removeDisposable (state: PluginState) (id: Id) =
    try
      state.Disposables.Remove(id)
      |> ignore
    with
      | _ -> ()

  // ** makeNodeMapping

  let private makeNodeMapping (pin: IPin2) =
    let id = parseNodePath pin
    let gid = parsePinGroupId pin
    let cp = pin.ParentNode.FindPin Settings.CHANGED_PIN
    let dir = parseDirection pin
    let tipe, props =
      match parsePinType pin with
      | Right PinType.Enum ->
        PinType.Enum, Some (parseEnumProperties pin)
      | Right tipe -> tipe, None
      | Left error -> failwith (string error)
    let nm =
      { PinId = id
        GroupId = gid
        Pin = pin
        Type = tipe
        Direction = dir
        Properties = props
        ChangedNode = cp }
    (id, nm)

  // ** addChangedPin

  let private addChangedPin (state: PluginState) (pin: IPin2) =
    let id, nm = makeNodeMapping pin
    if not (state.NodeMappings.ContainsKey id) then
      state.NodeMappings.Add(id, nm)
      |> ignore

  // ** updateChangedPin

  let private updateChangedPin (state: PluginState) (pin: IPin2) =
    let id, nm = makeNodeMapping pin
    if state.NodeMappings.ContainsKey id then
      state.NodeMappings.[id] <- nm
    else
      state.NodeMappings.Add(id, nm) |> ignore

  // ** removeChangedPin

  let private removeChangedPin (state: PluginState) (id: Id) =
    try
      state.NodeMappings.Remove(id)
      |> ignore
    with
      | _ -> ()

  // ** onNodeExposed

  let private onNodeExposed (state: PluginState) (node: INode2) =
    match parseINode2 state node with
    | Right [] -> ()
    | Right pins -> List.iter (Msg.PinAdded >> state.Events.Enqueue) pins
    | Left error -> error |> string |> Util.error state

  // ** onNodeUnExposed

  let private onNodeUnExposed (state: PluginState) (node: INode2) =
    parseINode2Ids state node
    |> List.iter (Msg.PinRemoved >> state.Events.Enqueue)

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
      state.Disposables.Add(globals,disposable) |> ignore
      { state with Initialized = true }
    else
      state

  // ** parseExposedNodes

  let private parseExposedNodes (state: PluginState) =
    for node in state.V2Host.ExposedNodeService.Nodes do
      onNodeExposed state node
    state

  // ** requestUpdate

  let private requestUpdate (state: PluginState) =
    state.Update := true
    state

  // ** updateRequested

  let private updateRequested (state: PluginState) =
    !state.Update


  // ** resetState

  let private resetState (state: PluginState) =
    state.Update := false
    state.Commands.Clear()

  // ** initialize

  let private initialize (state: PluginState) =
    if not state.Initialized then
      setupVvvv state
      |> parseExposedNodes
      |> requestUpdate
    else
      state

  // ** updateOutputs

  let private updateOutputs (state: PluginState) =
    let values = new ResizeArray<PinGroup>()

    for KeyValue(_,value) in state.Pins do
      values.Add value

    if values.Count > 0 then
      state.OutPinGroups.SliceCount <- values.Count
      state.OutPinGroups.AssignFrom values
    else
      state.OutPinGroups.SliceCount <- 1
      state.OutPinGroups.AssignFrom(new ResizeArray<PinGroup>())

    state.OutCommands.SliceCount <- state.Commands.Count
    state.OutCommands.AssignFrom state.Commands

    if state.NodeMappings.Count > 0 then
      let mappings = new ResizeArray<NodeMapping>()
      for KeyValue(_,nm) in state.NodeMappings do
        if not nm.IsOutput then
          mappings.Add nm
      state.OutNodeMappings.SliceCount <- mappings.Count
      state.OutNodeMappings.AssignFrom mappings
    else
      state.OutNodeMappings.SliceCount <- 1
      state.OutNodeMappings.AssignFrom [| |]

    state.OutUpdate.[0] <- true

  // ** processing

  let private processing (state: PluginState) =
    while state.Events.Count > 0 do
      match state.Events.TryDequeue() with
      | true, msg ->
        match msg with
        | Msg.PinAdded (pin, parsed) ->
          addDisposable state pin
          addChangedPin state pin
          addPin state parsed

        | Msg.PinRemoved (group, pin) ->
          removeDisposable state pin
          removeChangedPin state pin
          removePin state group pin

        | Msg.PinValueChange (group, slices) ->
          updatePinValues state group slices

        | Msg.PinTagChange (group, id, tags) ->
          updatePinTags state group id tags

        | Msg.PinNameChange (group, id, name) ->
          updatePinName state group id name

        | Msg.PinDirectionChange (group, id, dir) ->
          updatePinDirection state group id dir
          id
          |> findPinById state
          |> Option.iter (updateChangedPin state)

        | Msg.PinVecSizeChange (group, id, vecsize) ->
          updatePinVecSize state group id vecsize

        | Msg.PinSubTypeChange nodeid ->
          let node = state.V2Host.GetNodeFromPath(nodeid)
          if not (isNull node) then
            match parseINode2 state node with
            | Right []     -> ()
            | Left error   -> error |> string |> Util.error state
            | Right parsed ->
              List.iter
                (fun (pin,parsed) ->
                  updateChangedPin state pin
                  updatePin state parsed)
                parsed

        requestUpdate state |> ignore
      | _ -> ()

    for KeyValue(id,nm) in state.NodeMappings do
      if nm.IsOutput && nm.ChangedNode.[0] = "1" then
        let slices =
          match nm.Properties with
          | Some props -> parsePinValueWith nm.Type id props nm.Pin
          | _ ->  parsePinValueWith nm.Type id [| |] nm.Pin
        updatePinValues state nm.GroupId slices
        requestUpdate state |> ignore

    if updateRequested state then
      updateOutputs state
      resetState state
    else
      state.OutCommands.SliceCount <- 1
      state.OutCommands.AssignFrom [| |]
      state.OutUpdate.[0] <- false

    state

  let evaluate (state: PluginState) (_: int) =
    state
    |> initialize
    |> processing

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
  [<Input("Client ID", IsSingle = true)>]
  val mutable InClientId: ISpread<string>

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
  [<Output("NodeMappings")>]
  val mutable OutNodeMappings: ISpread<NodeMapping>

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
              InClientId = self.InClientId
              OutUpdate = self.OutUpdate
              OutCommands = self.OutCommands
              OutNodeMappings = self.OutNodeMappings
              OutPinGroups = self.OutPinGroups }
        state <- state'
        initialized <- true

      state <- Graph.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state
