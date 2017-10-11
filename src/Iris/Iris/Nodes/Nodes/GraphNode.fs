namespace VVVV.Nodes

// * Imports

open System
open System.IO
open System.Web
open System.Text
open System.Threading
open System.ComponentModel.Composition
open System.Security.Cryptography
open System.Collections.Generic
open System.Collections.Concurrent
open VVVV.PluginInterfaces.V1
open VVVV.PluginInterfaces.V2
open VVVV.PluginInterfaces.V2.Graph
open VVVV.Utils.VColor
open VVVV.Utils.VMath
open VVVV.Core.Logging
open VVVV.Core
open Iris.Raft
open Iris.Core
open Iris.Nodes

// * Graph

[<RequireQualifiedAccess>]
module rec Graph =

  // ** tag

  let private tag (str:string) = String.format "Graph.{0}" str

  // ** Aliases

  type NodePath = string
  type PatchPath = string

  // ** NodePatch

  type NodePatch =
    { NodeId: int
      Content: string }

    member patch.Render () =
      String.Format(
        @"<NODE id=""{0}"">
          <PIN pinname=""Tag"" slicecount=""1"" values=""{1}""/>
        </NODE>",
        patch.NodeId,
        HttpUtility.HtmlEncode(patch.Content))

  // ** Patch

  type Patch =
    { PatchId: int
      FileName: PatchPath
      NodePatches: NodePatch list }

    member patch.Render() =
      String.Format(
        @"<PATCH id=""{0}"">
          {1}
        </PATCH>",
        patch.PatchId,
        Seq.fold (fun m (node: NodePatch) -> m + node.Render()) "" patch.NodePatches)

  // ** Patch module

  module private Patch =

    // *** append

    let append (fragment: NodePatch) patch =
      { patch with NodePatches = fragment :: patch.NodePatches }

  // ** Msg

  type Msg =
    | NodeAdded              of frame:uint64 * node:INode2
    | NodeRemoved            of frame:uint64 * node:INode2
    | UpdateGroup            of frame:uint64 * node:INode2
    | PinVecSizeChange       of groupId:PinGroupId * pinId:PinId * pin:IPin2
    | PinNameChange          of groupId:PinGroupId * pinId:PinId * pin:IPin2
    | PinSubTypeChange       of groupId:PinGroupId * pinId:PinId * pin:IPin2
    | PinConfigurationChange of groupId:PinGroupId * pinId:PinId * pin:IPin2
    | PinTagChange           of groupId:PinGroupId * pinId:PinId * pin:IPin2
    | PinValueChange         of groupId:PinGroupId * pinId:PinId * pin:IPin2

    member msg.Frame
      with get () =
        match msg with
        | NodeAdded              (frame,_) -> Some frame
        | NodeRemoved            (frame,_) -> Some frame
        | UpdateGroup            (frame,_) -> Some frame
        | PinVecSizeChange              _  -> None
        | PinNameChange                 _  -> None
        | PinSubTypeChange              _  -> None
        | PinConfigurationChange        _  -> None
        | PinTagChange                  _  -> None
        | PinValueChange                _  -> None

  // ** PluginState

  type PluginState =
    { Frame: uint64 ref
      Initialized: bool
      Update: bool ref
      Events: ConcurrentQueue<Msg>
      PinGroups: Map<PinGroupId,PinGroup>
      NodeMappings: Map<PinId,NodeMapping>
      Commands: ResizeArray<StateMachine>
      Logger: ILogger
      V1Host: IPluginHost
      V2Host: IHDEHost
      InClientId: ISpread<ClientId>
      OutPinGroups: ISpread<PinGroup>
      OutCommands: ISpread<StateMachine>
      OutNodeMappings: ISpread<NodeMapping>
      OutUpdate: ISpread<bool>
      Disposables: Dictionary<IrisId,IDisposable> }

    static member Create () =
      { Frame = ref 0UL
        Initialized = false
        Update = ref false
        Commands = ResizeArray()
        Events = ConcurrentQueue()
        PinGroups = Map.empty
        NodeMappings = Map.empty
        Logger = null
        V1Host = null
        V2Host = null
        InClientId = null
        OutPinGroups = null
        OutCommands = null
        OutUpdate = null
        OutNodeMappings = null
        Disposables = new Dictionary<IrisId,IDisposable>() }

    member state.ClientId
      with get () = state.InClientId.[0]

    interface IDisposable with
      member self.Dispose() =
        Seq.iter (fun (KeyValue(_,disposable)) -> dispose disposable) self.Disposables
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
      | Value  -> "IOBox (Value Advanced)"
      | String -> "IOBox (String)"
      | Node   -> "IOBox (Node)"
      | Color  -> "IOBox (Color)"
      | Enum   -> "IOBox (Enumerations)"

    static member Parse (str: string) =
      match str with
      | "IOBox (Value Advanced)" -> Value
      | "IOBox (String)"         -> String
      | "IOBox (Node)"           -> Node
      | "IOBox (Color)"          -> Color
      | "IOBox (Enumerations)"   -> Enum
      | _ -> failwithf "unknown type: %s" str

    static member TryParse (str: string) =
      try
        str
        |> IOBoxType.Parse
        |> Either.succeed
      with exn ->
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
      with exn ->
        exn.Message
        |> Error.asParseError "Behavior.TryParse"
        |> Either.fail

    static member IsTrigger (bh: Behavior) =
      match bh with
      | Bang -> true
      | _ -> false

  // ** toGuid

  let private toGuid (str: string) =
    use sha1 = new SHA1Managed()
    str
    |> Encoding.UTF8.GetBytes
    |> sha1.ComputeHash
    |> fun hash -> Guid(hash.[..15])

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

  // ** parseNodeId

  let private parseNodeId (node: INode2) =
    let pin = node.FindPin Settings.TAG_PIN
    match pin.[0] with
    | null | "" -> None
    | content ->
      try IrisId.Parse content |> Some
      with exn -> None

  // ** parseNodePath

  let private parseNodePath (pin: IPin2) =
    sprintf "%s/%s" (pin.ParentNode.GetNodePath(false)) pin.Name

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
      return if isNull np.[0] then "" else np.[0]
    }

  // ** parseTags

  let private parseTags (pin: IPin2) =
    let tp = pin.ParentNode.FindPin Settings.TAG_PIN
    try
      match tp.[0] with
      | null | "" -> Array.empty
      | str ->
        Array.map
          (fun (pair:string) ->
            match pair.Split('=') with
            | [| key; value |] -> { Key = key; Value = value }
            | _ -> { Key = "<no key>"; Value = pair })
          (str.Split ',')
    with _ -> Array.empty

  // ** addDefaultTags

  let private addDefaultTags (path: string) props =
    let path, name = parseIOBoxPath path
    [| { Key = Settings.PIN_NAME_PROP; Value = name }
       { Key = Settings.PIN_PATH_PROP; Value = path } |]
    |> Array.append props

  // ** parsePinId

  let private parsePinId nodeId (pin: IPin2) =
    string nodeId + pin.Name
    |> toGuid
    |> IrisId.FromGuid

  // ** parseConfiguration

  let private parseConfiguration (pin: IPin2) : PinConfiguration =
    if pin.IsConnected()
    then PinConfiguration.Source
    else PinConfiguration.Sink

  // ** parseVecSize

  let private parseVecSize (pin: IPin2) =
      Settings.SLICECOUNT_MODE_PIN
      |> pin.ParentNode.FindPin
      |> fun pin -> pin.[0]
      |> function
      | "Input" -> Either.succeed VecSize.Dynamic
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

        VecSize.Fixed (cols * rows * pages)
        |> Either.succeed

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
        with  _ ->
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
        with _ ->
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
        with _ ->
          Either.succeed 4ul
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
        with _ ->
          result.Add ColorSpace.Black
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

  // ** parseIOBoxPath

  let private parseIOBoxPath (path: string) =
    let parsed = String.split [| '/' |] path
    let idx = Array.length parsed - 1
    match Array.splitAt idx parsed with
    | nodepath, [| name |] -> String.Join("/", nodepath), name
    | _ -> failwithf "wrong format: %s" (string id)

  // ** findPinById

  let private findPinById (state: PluginState) (path: string) =
    let path, name = parseIOBoxPath path
    let node = state.V2Host.GetNodeFromPath(path)
    if not (isNull node) then
      let pin = node.FindPin name
      if not (isNull pin) then Some pin else None
    else None

  // ** parsePinValueWith

  let private parsePinValueWith (tipe: PinType) (pid: PinId) (props: Property array) (pin: IPin2) =
    match tipe with
    | PinType.Boolean -> BoolSlices(pid, None, parseBoolValues pin)
    | PinType.Number  -> NumberSlices(pid, None, parseDoubleValues pin)
    | PinType.String  -> StringSlices(pid, None, parseStringValues pin)
    | PinType.Color   -> ColorSlices(pid, None, parseColorValues pin)
    | PinType.Enum    -> EnumSlices(pid, None, parseEnumValues props pin)

  // ** parsePinIds

  let private parsePinIds (pins: IPin2 seq): (PinGroupId * PinId) list =
    Seq.fold
      (fun lst (pin: IPin2) ->
        let node = pin.ParentNode
        let nodeId = parseNodeId node
        let groupId = parseNodeId node.Parent
        match nodeId, groupId with
        | Some nodeId, Some groupId ->
          let pinId = parsePinId nodeId pin
          (groupId,pinId) :: lst
        | _ -> lst)
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
          |> Error.asParseError "parsePinType"
          |> Either.fail
    }

  // ** parseINode2Ids

  let private parseINode2Ids (node: INode2)  =
    node.Pins
    |> visibleInputPins
    |> parsePinIds

  // ** registerPinHandlers

  let private registerPinHandlers (state: PluginState) (pin: IPin2) (parsed:Pin) =
    let np    = pin.ParentNode.FindPin Settings.DESCRIPTIVE_NAME_PIN
    let scmp  = pin.ParentNode.FindPin Settings.SLICECOUNT_MODE_PIN
    let cp    = pin.ParentNode.FindPin Settings.COLUMNS_PIN
    let rp    = pin.ParentNode.FindPin Settings.ROWS_PIN
    let pp    = pin.ParentNode.FindPin Settings.PAGES_PIN
    let tp    = pin.ParentNode.FindPin Settings.TAG_PIN

    let tipe  = parsePinType pin |> Either.get // !!!
    let props = parseEnumProperties pin

    let vecsizeUpdate _ _ =
      (parsed.PinGroupId, parsed.Id, pin)
      |> Msg.PinVecSizeChange
      |> state.Events.Enqueue

    let vecsizeHandler = new EventHandler(vecsizeUpdate)
    let columnsHandler = new EventHandler(vecsizeUpdate)
    let rowsHandler = new EventHandler(vecsizeUpdate)
    let pagesHandler = new EventHandler(vecsizeUpdate)

    let nameHandler = new EventHandler(fun _ _ ->
      (parsed.PinGroupId, parsed.Id, pin)
      |> Msg.PinNameChange
      |> state.Events.Enqueue)

    let tagHandler = new EventHandler(fun _ _ ->
      (parsed.PinGroupId, parsed.Id, pin)
      |> Msg.PinTagChange
      |> state.Events.Enqueue)

    let changedHandler = new EventHandler(fun _ _ ->
      (parsed.PinGroupId, parsed.Id, pin)
      |> Msg.PinValueChange
      |> state.Events.Enqueue)

    let directionUpdate _ _ =
      (parsed.PinGroupId, parsed.Id, pin)
      |> Msg.PinConfigurationChange
      |> state.Events.Enqueue

    let connectedHandler = new PinConnectionEventHandler(directionUpdate)
    let disconnectedHandler = new PinConnectionEventHandler(directionUpdate)

    let subtypeHandler = new EventHandler(fun _  _ ->
      (parsed.PinGroupId, parsed.Id, pin)
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
          pin.SubtypeChanged.RemoveHandler(subtypeHandler) }

  // ** registerNodeHandlers

  let private registerNodeHandlers (state:PluginState) (node: INode2) =
    let onNodeRename _ _ =
      (!state.Frame,node)
      |> Msg.UpdateGroup
      |> state.Events.Enqueue

    let renamedHandler = new RenamedHandler(onNodeRename)
    node.add_Renamed(renamedHandler)
    { new IDisposable with
        member self.Dispose() =
          node.remove_Renamed(renamedHandler) }

  // ** parseValuePin

  let private parseValuePin clientId nodeId groupId (pin: IPin2) =
    either {
      let path  = parseNodePath pin
      let pinId = parsePinId nodeId pin
      let cnf = parseConfiguration pin
      let! vt = parseValueType pin
      let! bh = parseBehavior pin
      let! pinName = parseName pin
      let! vc = parseVecSize pin
      let tags = pin |> parseTags |> addDefaultTags path
      match vt with
      | ValueType.Boolean ->
        return BoolPin {
          Id               = pinId
          Name             = name pinName
          PinGroupId       = groupId
          ClientId         = clientId
          Tags             = tags
          PinConfiguration = cnf
          Persisted        = false
          Online           = true
          Dirty            = false
          IsTrigger        = Behavior.IsTrigger bh
          VecSize          = vc
          Labels           = [| |]
          Values           = parseBoolValues pin
        }
      | ValueType.Integer ->
        let! min = parseMin pin
        let! max = parseMax pin
        let! unit = parseUnits pin
        return NumberPin {
          Id               = pinId
          Name             = name pinName
          PinGroupId       = groupId
          ClientId         = clientId
          Tags             = tags
          Min              = min
          Max              = max
          Unit             = unit
          Persisted        = false
          Online           = true
          Dirty            = false
          Precision        = 0ul
          PinConfiguration = cnf
          VecSize          = vc
          Labels           = [| |]
          Values           = parseDoubleValues pin
        }
      | ValueType.Real ->
        let! min = parseMin pin
        let! max = parseMax pin
        let! unit = parseUnits pin
        let! prec = parsePrecision pin
        return NumberPin {
          Id               = pinId
          Name             = name pinName
          PinGroupId       = groupId
          ClientId         = clientId
          Min              = min
          Max              = max
          Unit             = unit
          Persisted        = false
          Online           = true
          Dirty            = false
          Precision        = prec
          PinConfiguration = cnf
          VecSize          = vc
          Tags             = tags
          Labels           = [| |]
          Values           = parseDoubleValues pin
        }
    }

  // ** parseSeqWith

  type private Parser = IPin2 -> Either<IrisError,Pin>

  let private parseSeqWith (parse: Parser) (pins: IPin2 seq) : (IPin2 * Pin) list =
    Seq.fold
      (fun lst pin -> parse pin |> function
        | Right parsed -> (pin, parsed) :: lst
        | Left error ->
          error
          |> string
          |> Logger.err "parseSeqWith"
          lst)
      []
      pins

  // ** parseValuePins

  let private parseValuePins clientId nodeId groupId (pins: IPin2 seq) : (IPin2 * Pin) list =
    parseSeqWith (parseValuePin clientId nodeId groupId) pins

  // ** parseValueBox

  let private parseValueBox clientId nodeId groupId (node: INode2) : (IPin2 * Pin) list =
    node.Pins
    |> visibleInputPins
    |> parseValuePins clientId nodeId groupId

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
        with  _ ->
          Either.succeed -1
      return value
    }

  // ** parseStringPin

  let private parseStringPin clientId nodeId groupId (pin: IPin2) : Either<IrisError,Pin> =
    either {
      let path = parseNodePath pin
      let id = parsePinId nodeId pin
      let cnf = parseConfiguration pin
      let! st = parseStringType pin
      let! pinName = parseName pin
      let! vc = parseVecSize pin
      let! maxchars = parseMaxChars pin
      let tags = pin |> parseTags |> addDefaultTags path
      return StringPin {
        Id               = id
        Name             = name pinName
        PinGroupId       = groupId
        ClientId         = clientId
        Tags             = tags
        Persisted        = false
        Online           = true
        Dirty            = false
        PinConfiguration = cnf
        Behavior         = st
        MaxChars         = 1<chars> * maxchars
        VecSize          = vc
        Labels           = [| |]
        Values           = parseStringValues pin
      }
    }

  // ** parseStringPins

  let private parseStringPins clientId nodeId groupId (pins: IPin2 seq) =
    parseSeqWith (parseStringPin clientId nodeId groupId) pins

  // ** parseStringBox

  let private parseStringBox clientId nodeId groupId (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseStringPins clientId nodeId groupId

  // ** parseEnumPin

  let private parseEnumPin clientId nodeId groupId (pin: IPin2) : Either<IrisError,Pin> =
    either {
      let path = parseNodePath pin
      let id = parsePinId nodeId pin
      let cnf = parseConfiguration pin
      let! pinName = parseName pin
      let! vc = parseVecSize pin
      let tags = pin |> parseTags |> addDefaultTags path
      let props = parseEnumProperties pin
      return EnumPin {
        Id               = id
        Name             = name pinName
        Persisted        = false
        Online           = true
        Dirty            = false
        PinGroupId       = groupId
        ClientId         = clientId
        PinConfiguration = cnf
        VecSize          = vc
        Properties       = props
        Tags             = tags
        Labels           = [| |]
        Values           = parseEnumValues props pin
      }
    }

  // ** parseEnumPins

  let private parseEnumPins clientId nodeId groupId (pins: IPin2 seq) =
    parseSeqWith (parseEnumPin clientId nodeId groupId) pins

  // ** parseEnumBox

  let private parseEnumBox clientId nodeId groupId (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseEnumPins clientId nodeId groupId

  // ** parseColorPin

  let private parseColorPin clientId nodeId groupId (pin: IPin2) : Either<IrisError,Pin> =
    either {
      let path = parseNodePath pin
      let id = parsePinId nodeId pin
      let cnf = parseConfiguration pin
      let tags = pin |> parseTags |> addDefaultTags path
      let! pinName = parseName pin
      let! vc = parseVecSize pin
      return ColorPin {
        Id               = id
        Name             = name pinName
        PinGroupId       = groupId
        ClientId         = clientId
        PinConfiguration = cnf
        Persisted        = false
        Online           = true
        Dirty            = false
        VecSize          = vc
        Tags             = tags
        Labels           = [| |]
        Values           = parseColorValues pin
      }
    }

  // ** parseColorPins

  let private parseColorPins clientId nodeId groupId (pins: IPin2 seq) =
    parseSeqWith (parseColorPin clientId nodeId groupId) pins

  // ** parseColorBox

  let private parseColorBox clientId nodeId groupId (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseColorPins clientId nodeId groupId

  // ** parseINode2

  let private parseINode2 clientId nodeId groupId (node: INode2) =
    either {
      let! boxtype = IOBoxType.TryParse (string node.NodeInfo)
      match boxtype with
      | IOBoxType.Value  -> return parseValueBox  clientId nodeId groupId node
      | IOBoxType.String -> return parseStringBox clientId nodeId groupId node
      | IOBoxType.Enum   -> return parseEnumBox   clientId nodeId groupId node
      | IOBoxType.Color  -> return parseColorBox  clientId nodeId groupId node
      | x ->
        return!
          sprintf "unsupported type %A" x
          |> Error.asParseError "parseINode2"
          |> Either.fail
    }

  // ** parseGroupName

  let private parseGroupName (node: INode2) =
    let nodeName = node.NodeInfo.Name
    match node.FindPin(Settings.DESCRIPTIVE_NAME_PIN).[0] with
    | null | ""   -> name nodeName
    | description -> name (sprintf "%s - %s" nodeName description)

  // ** parseGroupPath

  let private parseGroupPath (node: INode2) =
    node.NodeInfo.Filename
    |> filepath
    |> Some

  // ** addPin

  let private addPin (state: PluginState) nodeId (pin: IPin2) (parsed: Pin) : PluginState =
    /// dispose of previously registered disposable for this pin id
    state.Disposables
    |> Seq.choose (function KeyValue(pid, disp) -> if pid = parsed.Id then Some disp else None)
    |> Seq.iter dispose

    /// register event handlers for this pin and track them
    do registerPinHandlers state pin parsed
       |> fun disposable -> state.Disposables.Add(parsed.Id, disposable)
       |> ignore

    match Map.tryFind parsed.PinGroupId state.PinGroups with
    /// the pin does not exist in this group yet, so we simply add it
    | Some group when not (PinGroup.contains parsed.Id group) ->
      state.Commands.Add (AddPin parsed)

      /// create a NodeMapping, tracking the connection between Iris Pin and VVVV Pin
      let nm = makeNodeMapping nodeId parsed.PinGroupId pin
      let mappings = Map.add parsed.Id nm state.NodeMappings

      { state with
          PinGroups = Map.add group.Id (PinGroup.addPin parsed group) state.PinGroups
          NodeMappings = mappings }
    /// the pin already exists, so we ignore it, since this might just
    | Some _ -> state
    /// the group does not exist yet
    | None ->
      let parent = pin.ParentNode.Parent

      /// register event handlers for this node to track them
      do registerNodeHandlers state parent
          |> fun disposable -> state.Disposables.Add(parsed.PinGroupId, disposable)
          |> ignore

      let group: PinGroup =
        { Id = parsed.PinGroupId
          Name = parseGroupName parent
          Path = parseGroupPath parent
          ClientId = state.ClientId
          RefersTo = None
          Pins = Map.ofList [ (parsed.Id, parsed) ] }

      state.Commands.Add (AddPinGroup group)

      /// create a NodeMapping, tracking the connection between Iris Pin and VVVV Pin
      let nm = makeNodeMapping nodeId parsed.PinGroupId pin
      let mappings = Map.add parsed.Id nm state.NodeMappings

      { state with
          PinGroups = Map.add group.Id (PinGroup.addPin parsed group) state.PinGroups
          NodeMappings = mappings }

  // ** nodeAdded

  let private nodeAdded (state: PluginState) (node: INode2) =
    let clientId = state.ClientId

    let nodeId =
      match parseNodeId node with
      | Some id -> id
      | None ->
        let id = IrisId.Create()
        do patchNode state node (string id)
        id

    let groupId =
      if node.Parent.NodeInfo.Name = "root"
      then Settings.TOP_LEVEL_GROUP_ID else
        match parseNodeId node.Parent with
        | Some id -> id
        | None ->
          let id = IrisId.Create()
          do patchNode state node (string id)
          id

    /// parse all visibile pin on this node and
    match parseINode2 clientId nodeId groupId node with
    | Left error ->
      Logger.err (tag "nodeAdded") error.Message
      state
    | Right pins ->
      Seq.fold
        (fun (state: PluginState) (pin:IPin2, parsed: Pin) ->
          addPin state nodeId pin parsed)
        state
        pins

  // ** nodeRemoved

  let private nodeRemoved (state: PluginState) (node: INode2) =
    match parseNodeId node with
    | None        -> state
    | Some nodeId ->
      /// parse all IDs for pins in this node
      let ids = parseINode2Ids node
      /// remove them all by aggregating a new state
      Seq.fold
        (fun (state: PluginState) (groupId, pinId) ->
          removePin state groupId pinId)
        state
        ids

  // ** groupUpdated

  let private groupUpdated (state: PluginState) (parent: INode2) =
    let name = parseGroupName parent
    let path = parseGroupPath parent
    if parent.Parent.NodeInfo.Name = "root" then
      let groupId = Settings.TOP_LEVEL_GROUP_ID
      match Map.tryFind groupId state.PinGroups with
      | Some group ->
        let group = { group with Name = name; Path = path }
        state.Commands.Add (UpdatePinGroup group)
        { state with PinGroups = Map.add groupId group state.PinGroups }
      | None -> state
    else
      match parseNodeId parent with
      | Some groupId ->
        match Map.tryFind groupId state.PinGroups with
        | Some group ->
          let group = { group with Name = name; Path = path }
          state.Commands.Add (UpdatePinGroup group)
          { state with PinGroups = Map.add groupId group state.PinGroups }
        | None -> state
      | None -> state

  // ** pinValueChange

  let private pinValueChange (state: PluginState) groupId pinId (pin: IPin2) =
    state

  // ** pinTagChange

  let private pinTagChange (state: PluginState) groupId pinId (pin: IPin2) =
    state

  // ** pinNameChange

  let private pinNameChange (state: PluginState) groupId pinId (pin: IPin2) =
    state


  // ** pinConfigurationChange

  let private pinConfigurationChange (state: PluginState) groupId pinId (pin: IPin2) =
    state

  // ** pinVecSizeChange

  let private pinVecSizeChange (state: PluginState) groupId pinId (pin: IPin2) =
    state

  // ** pinSubTypeChange

  let private pinSubTypeChange (state: PluginState) groupId pinId (pin: IPin2) =
    state

  // ** updatePinWith

  type private Updater = Pin -> Pin

  let private updatePinWith (state: PluginState)
                            (groupId: PinGroupId)
                            (pinId: PinId)
                            (updater: Updater) =
    match Map.tryFind groupId state.PinGroups with
    | Some group ->
      match Map.tryFind pinId group.Pins with
      | Some pin ->
        let group = PinGroup.updatePin (updater pin) group
        { state with PinGroups = Map.add groupId group state.PinGroups }
      | _ -> state
    | _ -> state

  // ** updatePinValues

  let private updatePinValues (state: PluginState) (group: PinGroupId) (slices: Slices) =
    [ (slices.PinId, slices) ]
    |> Map.ofList
    |> SlicesMap
    |> UpdateSlices
    |> state.Commands.Add
    updatePinWith state group slices.PinId <| fun oldpin ->
      Pin.setSlices slices oldpin

  // ** updatePinName

  let private updatePinName (state: PluginState) (group: PinGroupId) (pin: PinId) (name: Name) =
    updatePinWith state group pin <| fun oldpin ->
      let updated = Pin.setName name oldpin
      state.Commands.Add (UpdatePin updated)
      updated

  // ** updatePinTags

  let private updatePinTags (state: PluginState)
                            (group: PinGroupId)
                            (pin: PinId)
                            (tags: Property array) =
    updatePinWith state group pin <| fun oldpin ->
      let updated = Pin.setTags tags oldpin
      state.Commands.Add (UpdatePin updated)
      updated

  // ** updatePinConfiguration

  let private updatePinConfiguration (state: PluginState)
                                     (group: PinGroupId)
                                     (pin: PinId)
                                     cnf =
    updatePinWith state group pin <| fun oldpin ->
      let updated = Pin.setPinConfiguration cnf oldpin
      state.Commands.Add (UpdatePin updated)
      updated

  // ** updatePinVecSize

  let private updatePinVecSize (state: PluginState)
                               (group: PinGroupId)
                               (pin: PinId)
                               vecsize =
    updatePinWith state group pin <| fun oldpin ->
      let updated = Pin.setVecSize vecsize oldpin
      state.Commands.Add (UpdatePin updated)
      updated

  // ** updatePin

  let private updatePin (state: PluginState) (pin: Pin) =
    updatePinWith state pin.PinGroupId pin.Id (konst pin)

  // ** removePin

  let private removePin (state: PluginState) (groupId: PinGroupId) (pinId: PinId) =
    match Map.tryFind groupId state.PinGroups with
    | Some group ->
      match Map.tryFind pinId group.Pins with
      | Some _ when group.Pins.Count = 1 ->
        /// communicate the removal of this group to the host service
        state.Commands.Add (RemovePinGroup group)
        /// dispose and remove registrations for group
        if state.Disposables.ContainsKey groupId then
          dispose state.Disposables.[groupId]
          state.Disposables.Remove(groupId) |> ignore
        /// dispose and remove registrations for pin
        if state.Disposables.ContainsKey pinId then
          dispose state.Disposables.[pinId]
          state.Disposables.Remove(pinId) |> ignore
        /// remove the group from the state
        { state with PinGroups = Map.remove groupId state.PinGroups }
      | Some pin ->
        /// communicate the disappearance of this pin to the host service
        state.Commands.Add (RemovePin pin)
        /// dispose and remove registrations for pin
        if state.Disposables.ContainsKey pinId then
          dispose state.Disposables.[pinId]
          state.Disposables.Remove(pinId) |> ignore
        /// remove pin from group
        let group = PinGroup.removePin pin group
        /// update state with updated group
        { state with PinGroups = Map.add groupId group state.PinGroups }
      | _ -> state
    | _ -> state

  // ** makeNodeMapping

  let private makeNodeMapping nodeId groupId (pin: IPin2) =
    let id = parsePinId nodeId pin
    let cp = pin.ParentNode.FindPin Settings.CHANGED_PIN
    let cnf = parseConfiguration pin
    let tipe, props =
      match parsePinType pin with
      | Right PinType.Enum -> PinType.Enum, Some (parseEnumProperties pin)
      | Right tipe         -> tipe, None
      | Left  _            -> PinType.String, None /// default is string
    { PinId = id
      GroupId = groupId
      Pin = pin
      Type = tipe
      PinConfiguration = cnf
      Properties = props
      ChangedNode = cp }

  // ** updateChangedPin

  let private updateChangedPin (state: PluginState) (pin: IPin2) =
    let node = pin.ParentNode
    match parseNodeId node with
    | None        -> state
    | Some nodeId ->
      if node.Parent.NodeInfo.Name = "root" then
        let groupId = Settings.TOP_LEVEL_GROUP_ID
        let nm = makeNodeMapping nodeId groupId pin
        { state with NodeMappings = Map.add nm.PinId nm state.NodeMappings }
      else
        match parseNodeId node.Parent with
        | Some groupId ->
          let nm = makeNodeMapping nodeId groupId pin
          { state with NodeMappings = Map.add nm.PinId nm state.NodeMappings }
        | None -> state

  // ** removeChangedPin

  let private removeChangedPin (state: PluginState) (id: PinId) =
    { state with NodeMappings = Map.remove id state.NodeMappings }

  // ** onNodeUnExposed2

  (*

  let private onNodeUnExposed2 (state: PluginState) (node: INode2) =
    let log str = state.Logger.Log(LogType.Debug, str)

    let nodeId =
      let pin = node.FindPin Settings.TAG_PIN
      match pin.[0] with
      | null | "" -> None
      | content ->
        try IrisId.Parse content |> Some
        with exn -> None

    match nodeId with
    | None -> log "Node has Id, ignoring"
    | Some nodeId ->
      if node.Parent.NodeInfo.Name = "root" then
        if state.TmpPins.ContainsKey Settings.TOP_LEVEL_GROUP_ID &&
           state.TmpPins.[Settings.TOP_LEVEL_GROUP_ID].Pins.ContainsKey nodeId
        then
          state.TmpPins.[Settings.TOP_LEVEL_GROUP_ID].Pins.Remove nodeId |> ignore
      else
        let pin = node.Parent.FindPin Settings.TAG_PIN

        /// A sub-patch contains this module
        let parentId =
          match pin.[0] with
          | null | "" -> None
          | content ->
            /// something is present, so we try parse it. if it fails, we generate again and patch it
            /// into the graph
            try IrisId.Parse content |> Some
            with exn -> None

        match parentId with
        | None -> log "Parent has no Id, ignoring"
        | Some parentId ->
          if state.TmpPins.ContainsKey parentId then
            if state.TmpPins.[parentId].Pins.ContainsKey nodeId then
              if state.TmpPins.[parentId].Pins.Count = 1
              then state.TmpPins.Remove parentId |> ignore
              else state.TmpPins.[parentId].Pins.Remove nodeId |> ignore

    log "----------------------(Removed) Mappings--------------------"

    for KeyValue(_,mapping) in state.TmpPins do
      log (sprintf "PinGroup: %A" mapping.PinGroupId)
      for KeyValue(pinid,np) in mapping.Pins do
        log (sprintf "    Pin: %A - %s" pinid np)

    log "---------------------------------------------------------------"

    *)

  // ** onNodeUnExposedOLD

    /// parseINode2Ids state node
    /// |> List.iter (Msg.PinRemoved >> state.Events.Enqueue)

  // ** onNodeExposed

  let private onNodeExposed (state: PluginState) (node: INode2) =
    (!state.Frame, node) |> Msg.NodeAdded |> state.Events.Enqueue

  let private onNodeUnExposed (state: PluginState) (node: INode2) =
    (!state.Frame, node) |> Msg.NodeRemoved |> state.Events.Enqueue

  // ** setupVvvv

  let private setupVvvv (state: PluginState) =
    let globals = IrisId.Create()
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

    for KeyValue(_,value) in state.PinGroups do
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
        if not nm.IsSource then
          mappings.Add nm
      state.OutNodeMappings.SliceCount <- mappings.Count
      state.OutNodeMappings.AssignFrom mappings
    else
      state.OutNodeMappings.SliceCount <- 1
      state.OutNodeMappings.AssignFrom [| |]

    state.OutUpdate.[0] <- true

  // ** patchGraph

  let private patchGraph (state: PluginState) (patch: Patch) =
    state.V2Host.SendXMLSnippet(patch.FileName, patch.Render(), false);

  // ** patchNode

  let patchNode state (node: INode2) content =
    do patchGraph state {
      PatchId = node.Parent.ID
      FileName = node.Parent.NodeInfo.Filename
      NodePatches = [{ NodeId = node.ID; Content = content }]
    }

  // ** processEvents

  let private processEvents (state: PluginState) =
    if state.Events.Count > 0 then
      let msgs = ResizeArray()

      /// process message from one frame ago so all graph information is available to us. when they
      /// are ready, they'll be dequeued and added to the intermediate list of msgs to be processed
      /// next. if they are not ready (i.e. they occurred in the same frame), leave them in the queue
      /// and process them in the next frame.
      /// when the frame property is None, this means its OK to consume the message right away.
      for _ in 1 .. state.Events.Count do
        match state.Events.TryPeek() with
        | true, peeked ->
          match peeked.Frame with
          | Some frame when frame < !state.Frame ->
            match state.Events.TryDequeue() with
            | true, msg -> msgs.Add msg
            | false, _  -> ()
          | None ->
            match state.Events.TryDequeue() with
            | true, msg -> msgs.Add msg
            | false, _  -> ()
          | _ -> ()
        | _ -> ()

      if msgs.Count > 0 then
        let updated =
          /// fold over all messages that are ready to be processed and produce new state
          /// followed by the request to update the output pins of the node in the next iteration
          Seq.fold
            (fun (state: PluginState) (msg: Msg) ->
              match msg with
              | Msg.NodeAdded   (_, node) -> nodeAdded    state node
              | Msg.NodeRemoved (_, node) -> nodeRemoved  state node
              | Msg.UpdateGroup (_, node) -> groupUpdated state node
              | Msg.PinValueChange (groupId, pinId, pin) ->
                pinValueChange state groupId pinId pin

              | Msg.PinTagChange (groupId, pinId, pin) ->
                pinTagChange state groupId pinId pin

              | Msg.PinNameChange (groupId, pinId, pin) ->
                pinNameChange state groupId pinId pin

              | Msg.PinConfigurationChange (groupId, pinId, pin) ->
                pinConfigurationChange state groupId pinId pin

              | Msg.PinVecSizeChange (groupId, pinId, pin) ->
                pinVecSizeChange state groupId pinId pin

              | Msg.PinSubTypeChange (groupId, pinId, pin) ->
                pinSubTypeChange state groupId pinId pin
              |> requestUpdate)
            state
            msgs
        msgs.Clear()
        updated                         /// return updated state
      else state                        /// there were no "ready" events
    else state                          /// there were no events at all

  // ** processing

  let private processing (state: PluginState) =
    let state = processEvents state

    (*
    for KeyValue(id,nm) in state.NodeMappings do
      if nm.IsSource && nm.ChangedNode.[0] = "1" then
        let slices =
          match nm.Properties with
          | Some props -> parsePinValueWith nm.Type id props nm.Pin
          | _ ->  parsePinValueWith nm.Type id [| |] nm.Pin
        updatePinValues state nm.GroupId slices
        requestUpdate state |> ignore
    *)

    if updateRequested state then
      updateOutputs state
      resetState state
    else
      state.OutCommands.SliceCount <- 1
      state.OutCommands.AssignFrom [| |]
      state.OutUpdate.[0] <- false

    state

  // ** bumpFrame

  let private bumpFrame (state: PluginState) =
    state.Frame := !state.Frame + 1UL
    state

  // ** evaluate

  let evaluate (state: PluginState) (_: int) =
    state
    |> initialize
    |> processing
    |> bumpFrame

// * GraphNode

[<PluginInfo(Name="Graph", Category=Settings.NODES_CATEGORY, AutoEvaluate=true)>]
type GraphNode() =

  [<Import();DefaultValue>]
  val mutable V1Host: IPluginHost

  [<Import();DefaultValue>]
  val mutable V2Host: IHDEHost

  [<Import();DefaultValue>]
  val mutable Logger: ILogger

  [<Input("Client ID", IsSingle = true);DefaultValue>]
  val mutable InClientId: ISpread<ClientId>

  [<Output("Commands");DefaultValue>]
  val mutable OutCommands: ISpread<StateMachine>

  [<Output("Local PinGroups");DefaultValue>]
  val mutable OutPinGroups: ISpread<PinGroup>

  [<Output("NodeMappings");DefaultValue>]
  val mutable OutNodeMappings: ISpread<NodeMapping>

  [<Output("Update", IsSingle = true, IsBang = true);DefaultValue>]
  val mutable OutUpdate: ISpread<bool>

  let mutable initialized = false
  let mutable state = Unchecked.defaultof<Graph.PluginState>

  interface IPluginEvaluate with
    member self.Evaluate (spreadMax: int) : unit =
      if not initialized then
        state <-
          { Graph.PluginState.Create() with
              V1Host = self.V1Host
              V2Host = self.V2Host
              Logger = self.Logger
              InClientId = self.InClientId
              OutUpdate = self.OutUpdate
              OutCommands = self.OutCommands
              OutNodeMappings = self.OutNodeMappings
              OutPinGroups = self.OutPinGroups }
        initialized <- true

      state <- Graph.evaluate state spreadMax

  interface IDisposable with
    member self.Dispose() =
      dispose state
