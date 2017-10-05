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

  [<RequireQualifiedAccess>]
  type Msg =
    | PinAdded               of pin:IPin2 * parsed:Pin
    | PinRemoved             of group:PinGroupId * pin:PinId
    | PinValueChange         of group:PinGroupId * slices:Slices
    | PinSubTypeChange       of string       // node id
    | PinVecSizeChange       of group:PinGroupId * pin:PinId * VecSize
    | PinNameChange          of group:PinGroupId * pin:PinId * Name
    | PinTagChange           of group:PinGroupId * pin:PinId * Property array
    | PinConfigurationChange of group:PinGroupId * pin:PinId * path:string * PinConfiguration

  // ** PinGroupMapping

  type PinGroupMapping =
    { PinGroupId: PinGroupId
      NodePath: string
      Pins: Dictionary<PinId, IPin2> }

    override mapping.ToString() =
      let pins =
        Seq.map
          (function KeyValue(id, pin) -> sprintf "Id: %A Type: %O" id pin)
          mapping.Pins
        |> Seq.toList
      sprintf "Group: %A Path: %A"
        mapping.PinGroupId
        mapping.NodePath

  // ** PluginState

  type PluginState =
    { Frame: uint64
      Initialized: bool
      Update: bool ref
      TmpPins: Dictionary<PinGroupId,PinGroupMapping>
      Pins: Dictionary<PinGroupId,PinGroup>
      Patches: Dictionary<PatchPath,Patch>
      Commands: ResizeArray<StateMachine>
      NodeMappings: Dictionary<PinId,NodeMapping>
      Hashing: SHA1Managed
      Events: ConcurrentQueue<Msg>
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
      { Frame = 0UL
        Initialized = false
        Update = ref false
        TmpPins = Dictionary()
        Pins = new Dictionary<PinId,PinGroup>()
        Patches = Dictionary()
        Commands = new ResizeArray<StateMachine>()
        NodeMappings = new Dictionary<PinId,NodeMapping>()
        Hashing = new SHA1Managed()
        Events = new ConcurrentQueue<Msg>()
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
        dispose self.Hashing
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

  // ** composeId

  let private composeId (state: PluginState) (path: string) (id: IrisId) : IrisId =
    string id + path
    |> Encoding.UTF8.GetBytes
    |> state.Hashing.ComputeHash
    |> fun bytes -> bytes.[..15]
    |> IrisId.FromByteArray

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

  let private parsePinId (state: PluginState) (pin: IPin2) =
    let node = pin.ParentNode
    match node.FindPin Settings.TAG_PIN with
    | null ->
      let error = "Unable to find Tag pin on node"
      Logger.err "parsePinId" error
      failwith error
    | tagPin ->
      match IrisId.TryParse tagPin.[0] with
      | Right id -> id
      | Left _ ->
        let id = IrisId.Create()
        do patchGraph state {
          PatchId = node.Parent.ID
          FileName = node.Parent.NodeInfo.Filename
          NodePatches = [{ NodeId = node.ID; Content = string id }]
        }
        id
      |> composeId state pin.Name

  // ** parsePinGroupId

  let private parsePinGroupId (state: PluginState) (node: INode2) =
    let parent = node.Parent
    match parent.FindPin Settings.TAG_PIN with
    | null ->
      let error = "Unable to find Tag pin on node"
      Logger.err "parsePinGroupId" error
      failwith "error"
    | tagPin ->
      match IrisId.TryParse tagPin.[0] with
      | Right id -> id
      | Left _ ->
        let id = IrisId.Create()
        do patchGraph state {
          PatchId = parent.Parent.ID
          FileName = parent.Parent.NodeInfo.Filename
          NodePatches = [{ NodeId = parent.ID; Content = string id }]
        }
        id
      |> composeId state ""

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

  let private parsePinIds (state: PluginState) (pins: IPin2 seq) =
    Seq.fold
      (fun lst pin ->
        let pinid = parsePinId state pin
        let grpid = parsePinGroupId state pin.ParentNode
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
          |> Error.asParseError "parsePinType"
          |> Either.fail
    }

  // ** parseINode2Ids

  let private parseINode2Ids (state: PluginState) (node: INode2)  =
    node.Pins
    |> visibleInputPins
    |> parsePinIds state

  // ** registerHandlers

  let private registerHandlers (state: PluginState) (pin: IPin2) (parsed: Pin) =
    let path = parseNodePath pin
    let np = pin.ParentNode.FindPin Settings.DESCRIPTIVE_NAME_PIN
    let scmp = pin.ParentNode.FindPin Settings.SLICECOUNT_MODE_PIN
    let cp = pin.ParentNode.FindPin Settings.COLUMNS_PIN
    let rp = pin.ParentNode.FindPin Settings.ROWS_PIN
    let pp = pin.ParentNode.FindPin Settings.PAGES_PIN
    let tp = pin.ParentNode.FindPin Settings.TAG_PIN
    let tipe = parsePinType pin |> Either.get // !!!
    let props = parseEnumProperties pin

    let vecsizeUpdate _ _ =
      match parseVecSize pin with
      | Right vecsize ->
        (parsed.PinGroupId, parsed.Id, vecsize)
        |> Msg.PinVecSizeChange
        |> state.Events.Enqueue
      | Left error ->
        error
        |> string
        |> Logger.err "registerHandlers"

    let vecsizeHandler = new EventHandler(vecsizeUpdate)
    let columnsHandler = new EventHandler(vecsizeUpdate)
    let rowsHandler = new EventHandler(vecsizeUpdate)
    let pagesHandler = new EventHandler(vecsizeUpdate)

    let nameHandler = new EventHandler(fun _ _ ->
      (parsed.PinGroupId, parsed.Id, if isNull np.[0] then name "" else name np.[0])
      |> Msg.PinNameChange
      |> state.Events.Enqueue)

    let tagHandler = new EventHandler(fun _ _ ->
      (parsed.PinGroupId, parsed.Id, pin |> parseTags |> addDefaultTags path)
      |> Msg.PinTagChange
      |> state.Events.Enqueue)

    let changedHandler = new EventHandler(fun _ _ ->
      let slices = parsePinValueWith tipe parsed.Id props pin
      (parsed.PinGroupId,slices)
      |> Msg.PinValueChange
      |> state.Events.Enqueue)

    let directionUpdate _ _ =
      (parsed.PinGroupId, parsed.Id, path, parseConfiguration pin)
      |> Msg.PinConfigurationChange
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
          pin.SubtypeChanged.RemoveHandler(subtypeHandler) }

  // ** parseValuePin

  let private parseValuePin state group (pin: IPin2) : Either<IrisError,Pin> =
    either {
      let path  = parseNodePath pin
      let pinId = parsePinId state pin
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
          PinGroupId       = group
          ClientId         = state.ClientId
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
          PinGroupId       = group
          ClientId         = state.ClientId
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
          PinGroupId       = group
          ClientId         = state.ClientId
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

  let private parseValuePins state group (pins: IPin2 seq) : (IPin2 * Pin) list =
    parseSeqWith (parseValuePin state group) pins

  // ** parseValueBox

  let private parseValueBox state group (node: INode2) : (IPin2 * Pin) list =
    node.Pins
    |> visibleInputPins
    |> parseValuePins state group

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

  let private parseStringPin state group (pin: IPin2) : Either<IrisError,Pin> =
    either {
      let path = parseNodePath pin
      let id = parsePinId state pin
      let cnf = parseConfiguration pin
      let! st = parseStringType pin
      let! pinName = parseName pin
      let! vc = parseVecSize pin
      let! maxchars = parseMaxChars pin
      let tags = pin |> parseTags |> addDefaultTags path
      return StringPin {
        Id               = id
        Name             = name pinName
        PinGroupId       = group
        ClientId         = state.ClientId
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

  let private parseStringPins state group (pins: IPin2 seq) =
    parseSeqWith (parseStringPin state group) pins

  // ** parseStringBox

  let private parseStringBox state group (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseStringPins state group

  // ** parseEnumPin

  let private parseEnumPin state group (pin: IPin2) : Either<IrisError,Pin> =
    either {
      let path = parseNodePath pin
      let id = parsePinId state pin
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
        PinGroupId       = group
        ClientId         = state.ClientId
        PinConfiguration = cnf
        VecSize          = vc
        Properties       = props
        Tags             = tags
        Labels           = [| |]
        Values           = parseEnumValues props pin
      }
    }

  // ** parseEnumPins

  let private parseEnumPins state group (pins: IPin2 seq) =
    parseSeqWith (parseEnumPin state group) pins

  // ** parseEnumBox

  let private parseEnumBox state group (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseEnumPins state group

  // ** parseColorPin

  let private parseColorPin state group (pin: IPin2) : Either<IrisError,Pin> =
    either {
      let path = parseNodePath pin
      let id = parsePinId state pin
      let cnf = parseConfiguration pin
      let tags = pin |> parseTags |> addDefaultTags path
      let! pinName = parseName pin
      let! vc = parseVecSize pin
      return ColorPin {
        Id               = id
        Name             = name pinName
        PinGroupId       = group
        ClientId         = state.ClientId
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

  let private parseColorPins state group (pins: IPin2 seq) =
    parseSeqWith (parseColorPin state group) pins

  // ** parseColorBox

  let private parseColorBox state group (node: INode2) =
    node.Pins
    |> visibleInputPins
    |> parseColorPins state group

  // ** parseINode2

  let private parseINode2 (state: PluginState) (node: INode2) =
    let group = parsePinGroupId state node
    if state.Pins.ContainsKey(group) then
      do Logger.err "parseINode2" "Group already exists"
    either {
      let! boxtype = IOBoxType.TryParse (node.NodeInfo.ToString())
      match boxtype with
      | IOBoxType.Value  -> return parseValueBox  state group node
      | IOBoxType.String -> return parseStringBox state group node
      | IOBoxType.Enum   -> return parseEnumBox   state group node
      | IOBoxType.Color  -> return parseColorBox  state group node
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

  // ** onGroupRename

  let private onGroupRename (state: PluginState) (node: INode2) id _ _ =
    match state.Pins.TryGetValue(id) with
    | true, group ->
      { group with
          Name = parseGroupName node
          Path = parseGroupPath node }
      |> UpdatePinGroup
      |> state.Commands.Add
    | _,_ -> ()

  // ** addPin

  let private addPin (state: PluginState) (pin: IPin2) (parsed: Pin) =
    /// register a disposable for the variaous callbacks that need to be tracked
    let id = parsePinId state pin

    /// dispose of previously registered disposable for this pin id
    state.Disposables
    |> Seq.choose (function KeyValue(pid, disp) -> if pid = id then Some disp else None)
    |> Seq.iter dispose

    /// register event handlers for this pin and track them
    registerHandlers state pin parsed
    |> fun disposable -> state.Disposables.Add(id, disposable)
    |> ignore

    /// create a NodeMapping, tracking the connection between Iris Pin and VVVV Pin
    let _, nm = makeNodeMapping state pin
    if not (state.NodeMappings.ContainsKey id)
    then state.NodeMappings.Add(id, nm) |> ignore
    else state.NodeMappings.[id] <- nm

    /// patch the VVVV graph
    let node = pin.ParentNode
    let parent = node.Parent

    /// add the pin an existing group
    if state.Pins.ContainsKey parsed.PinGroupId then
      let group = state.Pins.[parsed.PinGroupId]
      state.Pins.[group.Id] <- { group with Pins = Map.add parsed.Id parsed group.Pins }
      state.Commands.Add (AddPin parsed)
    else
      /// no group found for pin, hence we just create it
      parent.add_Renamed(new RenamedHandler(onGroupRename state parent parsed.PinGroupId))
      /// BUG: this callback never fires
      /// node.FindPin(Settings.DESCRIPTIVE_NAME_PIN).add_Changed(new EventHandler(onGroupRename state node parsed.PinGroupId))
      let group: PinGroup =
        { Id = parsed.PinGroupId
          Name = parseGroupName parent
          Path = parseGroupPath parent
          ClientId = state.ClientId
          RefersTo = None
          Pins = Map.ofList [ (parsed.Id, parsed) ] }
      state.Commands.Add (AddPinGroup group)
      state.Pins.Add(group.Id, group) |> ignore

  // ** updatePinWith

  type private Updater = Pin -> Pin

  let private updatePinWith (state: PluginState)
                            (groupid: PinGroupId)
                            (pinid: PinId)
                            (updater: Updater) =
    match state.Pins.TryGetValue(groupid) with
    | true, group ->
      match Map.tryFind pinid group.Pins with
      | Some pin ->
        state.Pins.[groupid] <- { group with Pins = Map.add pinid (updater pin) group.Pins }
      | _ -> ()
    | _ -> ()

  // ** updatePinValues

  let private updatePinValues (state: PluginState) (group: PinGroupId) (slices: Slices) =
    updatePinWith state group slices.PinId <| fun oldpin ->
      Pin.setSlices slices oldpin
    [ (slices.PinId, slices) ]
    |> Map.ofList
    |> SlicesMap
    |> UpdateSlices
    |> state.Commands.Add

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

  let private removePin (state: PluginState) (groupid: PinGroupId) (pinid: PinId) =
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

  // ** removeDisposable

  let private removeDisposable (state: PluginState) (id: PinId) =
    try
      state.Disposables.Remove(id)
      |> ignore
    with _ -> ()

  // ** makeNodeMapping

  let private makeNodeMapping (state: PluginState) (pin: IPin2) =
    let id = parsePinId state pin
    let gid = parsePinGroupId state pin.ParentNode
    let cp = pin.ParentNode.FindPin Settings.CHANGED_PIN
    let cnf = parseConfiguration pin
    let tipe, props =
      match parsePinType pin with
      | Right PinType.Enum -> PinType.Enum, Some (parseEnumProperties pin)
      | Right tipe         -> tipe, None
      | Left  _            -> PinType.String, None /// default is string
    let nm =
      { PinId = id
        GroupId = gid
        Pin = pin
        Type = tipe
        PinConfiguration = cnf
        Properties = props
        ChangedNode = cp }
    (id, nm)

  // ** updateChangedPin

  let private updateChangedPin (state: PluginState) (pin: IPin2) =
    let id, nm = makeNodeMapping state pin
    if state.NodeMappings.ContainsKey id then
      state.NodeMappings.[id] <- nm
    else
      state.NodeMappings.Add(id, nm) |> ignore

  // ** removeChangedPin

  let private removeChangedPin (state: PluginState) (id: PinId) =
    try
      state.NodeMappings.Remove(id)
      |> ignore
    with _ -> ()

  // ** printGraph

  let private printGraph (state: PluginState) (node: INode2) =
    let log str = state.Logger.Log(LogType.Debug, str)

    let toGuid (input: string) =
      use sha1 = new SHA1Managed()
      input
      |> Encoding.UTF8.GetBytes
      |> sha1.ComputeHash
      |> fun hash -> Guid hash.[..15]

    let patchNode (node: INode2) content =
      sprintf "Tagging %s with %s (parent: %d node: %d)"
        (node.GetNodePath(false))
        content
        node.Parent.ID
        node.ID
      |> log
      do patchGraph state {
        PatchId = node.Parent.ID
        FileName = node.Parent.NodeInfo.Filename
        NodePatches = [{ NodeId = node.ID; Content = content }]
      }

    let parseParentPath (node: INode2) =
      node.Parent.GetNodePath(false)

    let parseParentId (node: INode2) =
      match node.Parent with
      | null ->
        log "parseParentId: INode2 is null. Generating Id."
        IrisId.Create()
      | parent ->
        let defaultId () =
          parent.NodeInfo.Filename
          |> toGuid
          |> IrisId.FromGuid
        match parent.FindPin Settings.TAG_PIN with
        | null ->
          log "parseParentId: Tag pin is null. Using default Id."
          defaultId ()
        | tagPin when isNull tagPin.[0] ->
          log "parseParentId: Tag pin has no value. Using default Id."
          defaultId ()
        | tagPin ->
          try
            log "parseParentId: Tag pin has a value. Parsing."
            IrisId.Parse tagPin.[0]
          with exn ->
            log (sprintf "parseParentId: Parsing %A failed. Using default Id." tagPin.[0])
            defaultId ()

    let parseNodeId (node: INode2) =
      match node.FindPin Settings.TAG_PIN with
      | null ->
        log "parseNodeId: Tag pin is null. Generating Id."
        IrisId.Create()
      | tagPin when isNull tagPin.[0] ->
        log "parseNodeId: Tag pin is empty. Generating Id."
        IrisId.Create()
      | tagPin ->
        try
          log "parseNodeId: Tag pin has a value. Parsing."
          IrisId.Parse tagPin.[0]
        with exn ->
          log (sprintf "parseNodeId: Parsing %A failed. Generating." tagPin.[0])
          IrisId.Create()

    let parentPath = parseParentPath node
    let parentId = parseParentId node
    let nodeId = parseNodeId node
    let pin = node.FindPin "Y Input Value"

    try
      /// the mapping is already in our cache but the newly added pin is in the same patch
      let mapping = state.TmpPins.[parentId]
      if mapping.NodePath = parentPath then
        do patchNode node.Parent (string parentId)
        do mapping.Pins.Add(nodeId, pin)
        do state.TmpPins.[parentId] <- mapping
      else
        /// mapping is already present, so we
        let newGroupId = IrisId.Create()
        /// patch parent with this id instead
        do patchNode node.Parent (string newGroupId)
        let dict = Dictionary()
        dict.Add(nodeId, pin)
        let mapping =
          { PinGroupId = newGroupId
            NodePath = parentPath
            Pins = dict }
        do state.TmpPins.Add(newGroupId, mapping)
    with exn ->
      let dict = Dictionary()
      dict.Add(nodeId, pin)
      let mapping =
        { PinGroupId = parentId
          NodePath = parentPath
          Pins = dict }
      do state.TmpPins.Add(parentId, mapping)
      do patchNode node.Parent (string parentId)

    /// patch the node itself
    do patchNode node (string nodeId)

    if node.Parent.Parent.NodeInfo.Name = "root" then
      log "Is in root, not tagging containing module."
    else
      log "Is *NOT* in root"

    if File.Exists(node.Parent.NodeInfo.Filename)
    then log ("Has Filename: " + node.Parent.NodeInfo.Filename)

    log "----------------------Mapping--------------------"

    for KeyValue(_,mapping) in state.TmpPins do
      mapping
      |> sprintf "%O"
      |> log

    log "---------------------------------------------------------------"

  // ** onNodeExposed

  let private onNodeExposed (state: PluginState) (node: INode2) =
    /// match parseINode2 state node with
    /// | Right [] -> ()
    /// | Right pins -> List.iter (Msg.PinAdded >> state.Events.Enqueue) pins
    /// | Left error ->
    ///   error
    ///   |> string
    ///   |> Logger.err "onNodeExposed"
    printGraph state node

  let private onNodeUnExposed (state: PluginState) (node: INode2) =
    /// parseINode2Ids state node
    /// |> List.iter (Msg.PinRemoved >> state.Events.Enqueue)
    ()

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

  // ** processing

  let private processing (state: PluginState) =
    while state.Events.Count > 0 do
      match state.Events.TryDequeue() with
      | true, msg ->
        match msg with
        | Msg.PinAdded (pin, parsed) -> addPin state pin parsed

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

        | Msg.PinConfigurationChange (group, id, path, dir) ->
          updatePinConfiguration state group id dir
          path
          |> findPinById state
          |> Option.iter (updateChangedPin state)

        | Msg.PinVecSizeChange (group, id, vecsize) ->
          updatePinVecSize state group id vecsize

        | Msg.PinSubTypeChange nodeid ->
          let node = state.V2Host.GetNodeFromPath(nodeid)
          if not (isNull node) then
            match parseINode2 state node with
            | Right []     -> ()
            | Left error   ->
              error
              |> string
              |> Logger.err "processing"
            | Right parsed ->
              List.iter
                (fun (pin,parsed) ->
                  updateChangedPin state pin
                  updatePin state parsed)
                parsed

        requestUpdate state |> ignore
      | _ -> ()

    for KeyValue(id,nm) in state.NodeMappings do
      if nm.IsSource && nm.ChangedNode.[0] = "1" then
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

  // ** evaluate

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

  [<Input("Client ID", IsSingle = true);DefaultValue>]
  val mutable InClientId: ISpread<ClientId>

  [<Output("Commands");DefaultValue>]
  val mutable OutCommands: ISpread<StateMachine>

  [<Output("PinGroups");DefaultValue>]
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
