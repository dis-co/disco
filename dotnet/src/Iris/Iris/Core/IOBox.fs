namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.Text
open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

#endif

//  ____       _                 _
// | __ )  ___| |__   __ ___   _(_) ___  _ __
// |  _ \ / _ \ '_ \ / _` \ \ / / |/ _ \| '__|
// | |_) |  __/ | | | (_| |\ V /| | (_) | |
// |____/ \___|_| |_|\__,_| \_/ |_|\___/|_|

[<RequireQualifiedAccess>]
type Behavior =
  | Toggle
  | Bang

  static member TryParse (str: string) =
    match toLower str with
    | "toggle" -> Some Toggle
    | "bang"   -> Some Bang
    | _        -> None

  override self.ToString() =
    match self with
    | Toggle  -> "Toggle"
    | Bang    -> "Bang"

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

#if JAVASCRIPT
  static member FromFB (fb: BehaviorFB) =
    match fb with
    | x when x = BehaviorFB.ToggleFB -> Some Toggle
    | x when x = BehaviorFB.BangFB   -> Some Bang
    | _                              -> None
#else
  static member FromFB (fb: BehaviorFB) =
    match fb with
    | BehaviorFB.ToggleFB -> Some Toggle
    | BehaviorFB.BangFB   -> Some Bang
    | _                       -> None
#endif

  member self.ToOffset(builder: FlatBufferBuilder) : BehaviorFB =
    match self with
    | Toggle -> BehaviorFB.ToggleFB
    | Bang   -> BehaviorFB.BangFB

//  ____  _        _            _____
// / ___|| |_ _ __(_)_ __   __ |_   _|   _ _ __   ___
// \___ \| __| '__| | '_ \ / _` || || | | | '_ \ / _ \
//  ___) | |_| |  | | | | | (_| || || |_| | |_) |  __/
// |____/ \__|_|  |_|_| |_|\__, ||_| \__, | .__/ \___|
//                         |___/     |___/|_|

type StringType =
  | Simple
  | MultiLine
  | FileName
  | Directory
  | Url
  | IP

  static member TryParse (str: string) =
    match toLower str with
    | "simple"    -> Some Simple
    | "multiline" -> Some MultiLine
    | "filename"  -> Some FileName
    | "directory" -> Some Directory
    | "url"       -> Some Url
    | "ip"        -> Some IP
    | _           -> None

  override self.ToString() =
    match self with
    | Simple    -> "Simple"
    | MultiLine -> "MultiLine"
    | FileName  -> "FileName"
    | Directory -> "Directory"
    | Url       -> "Url"
    | IP        -> "IP"

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: StringTypeFB) =
#if JAVASCRIPT
    match fb with
    | x when x = StringTypeFB.SimpleFB    -> Some Simple
    | x when x = StringTypeFB.MultiLineFB -> Some MultiLine
    | x when x = StringTypeFB.FileNameFB  -> Some FileName
    | x when x = StringTypeFB.DirectoryFB -> Some Directory
    | x when x = StringTypeFB.UrlFB       -> Some Url
    | x when x = StringTypeFB.IPFB        -> Some IP
    | _                                   -> None
#else
    match fb with
    | StringTypeFB.SimpleFB    -> Some Simple
    | StringTypeFB.MultiLineFB -> Some MultiLine
    | StringTypeFB.FileNameFB  -> Some FileName
    | StringTypeFB.DirectoryFB -> Some Directory
    | StringTypeFB.UrlFB       -> Some Url
    | StringTypeFB.IPFB        -> Some IP
    | _                        -> None
#endif

  member self.ToOffset(builder: FlatBufferBuilder) : StringTypeFB =
    match self with
    | Simple    -> StringTypeFB.SimpleFB
    | MultiLine -> StringTypeFB.MultiLineFB
    | FileName  -> StringTypeFB.FileNameFB
    | Directory -> StringTypeFB.DirectoryFB
    | Url       -> StringTypeFB.UrlFB
    | IP        -> StringTypeFB.IPFB

//  ___ ___  ____
// |_ _/ _ \| __ )  _____  __
//  | | | | |  _ \ / _ \ \/ /
//  | | |_| | |_) | (_) >  <
// |___\___/|____/ \___/_/\_\

#if JAVASCRIPT

type IOBox =

#else

type SliceYaml(tipe, idx, value: obj) as self =
  [<DefaultValue>] val mutable SliceType : string
  [<DefaultValue>] val mutable Index     : Index
  [<DefaultValue>] val mutable Value     : obj

  new () = new SliceYaml(null, 0u, null)

  do
    self.SliceType <- tipe
    self.Index     <- idx
    self.Value     <- value

  static member StringSlice(idx, value) =
    new SliceYaml("StringSlice", idx, value)

  static member IntSlice(idx, value) =
    new SliceYaml("IntSlice", idx, value)

  static member FloatSlice(idx, value) =
    new SliceYaml("FloatSlice", idx, value)

  static member DoubleSlice(idx, value) =
    new SliceYaml("DoubleSlice", idx, value)

  static member BoolSlice(idx, value) =
    new SliceYaml("BoolSlice", idx, value)

  static member ByteSlice(idx, value) =
    new SliceYaml("ByteSlice", idx, value)

  static member EnumSlice(idx, value) =
    new SliceYaml("EnumSlice", idx, value)

  static member ColorSlice(idx, value) =
    new SliceYaml("ColorSlice", idx, value)

  static member CompoundSlice(idx, value) =
    new SliceYaml("CompoundSlice", idx, value)

  member self.ToStringSliceD() : StringSliceD option =
    try
      let result : StringSliceD =
        { Index = self.Index; Value = string self.Value }
      Some result
    with
      | exn ->
        printfn "Could not de-serialize StringSlice yaml: %s" exn.Message
        None

  member self.ToIntSliceD() : IntSliceD option =
    try
      let result : IntSliceD =
        { Index = self.Index; Value = Int32.Parse (string self.Value) }
      Some result
    with
      | exn ->
        printfn "Could not de-serialize IntSlice yaml: %s" exn.Message
        None

  member self.ToFloatSliceD() : FloatSliceD option =
    try
      let result : FloatSliceD =
        { Index = self.Index; Value = Double.Parse (string self.Value) }
      Some result
    with
      | exn ->
        printfn "Could not de-serialize FloatSlice yaml: %s" exn.Message
        None

  member self.ToDoubleSliceD() : DoubleSliceD option =
    try
      let result : DoubleSliceD =
        { Index = self.Index; Value = Double.Parse (string self.Value) }
      Some result
    with
      | exn ->
        printfn "Could not de-serialize DoubleSlice yaml: %s" exn.Message
        None

  member self.ToBoolSliceD() : BoolSliceD option =
    try
      let result : BoolSliceD =
        { Index = self.Index; Value = Boolean.Parse (string self.Value) }
      Some result
    with
      | exn ->
        printfn "Could not de-serialize BoolSlice yaml: %s" exn.Message
        None

  member self.ToByteSliceD() : ByteSliceD option =
    try
      let result : ByteSliceD =
        { Index = self.Index; Value = Convert.FromBase64String(string self.Value) }
      Some result
    with
      | exn ->
        printfn "Could not de-serialize ByteSlice yaml: %s" exn.Message
        None

  member self.ToEnumSliceD() : EnumSliceD option =
    try
      let property =
        let pyml = self.Value :?> PropertyYaml
        { Key = pyml.Key; Value = pyml.Value }
      let result : EnumSliceD =
        { Index = self.Index; Value = property }
      Some result
    with
      | exn ->
        printfn "Could not de-serialize EnumSlice yaml: %s" exn.Message
        None

  member self.ToColorSliceD() : ColorSliceD option =
    try
      match Yaml.fromYaml(self.Value :?> ColorYaml) with
      | Some color  ->
        let result : ColorSliceD =
          { Index = self.Index; Value = color }
        Some result
      | _ -> None
    with
      | exn ->
        printfn "Could not de-serialize ColorSlice yaml: %s" exn.Message
        None

  member self.ToCompoundSliceD() : CompoundSliceD option =
    try
      let ioboxes =
        Array.fold
          (fun m box ->
            match Yaml.fromYaml box with
            | Some thing -> Array.append m [| thing |]
            | _          -> m)
          [| |]
          (self.Value :?> IOBoxYaml array)

      let result : CompoundSliceD =
        { Index = self.Index; Value = ioboxes }
      Some result
    with
      | exn ->
        printfn "Could not de-serialize CompoundSlice yaml: %s" exn.Message
        None

and IOBoxYaml() =
  [<DefaultValue>] val mutable BoxType    : string
  [<DefaultValue>] val mutable Id         : string
  [<DefaultValue>] val mutable Name       : string
  [<DefaultValue>] val mutable Patch      : string
  [<DefaultValue>] val mutable Tags       : string array
  [<DefaultValue>] val mutable Behavior   : string
  [<DefaultValue>] val mutable StringType : string
  [<DefaultValue>] val mutable FileMask   : string
  [<DefaultValue>] val mutable MaxChars   : int
  [<DefaultValue>] val mutable VecSize    : uint32
  [<DefaultValue>] val mutable Precision  : uint32
  [<DefaultValue>] val mutable Min        : int
  [<DefaultValue>] val mutable Max        : int
  [<DefaultValue>] val mutable Unit       : string
  [<DefaultValue>] val mutable Properties : PropertyYaml array
  [<DefaultValue>] val mutable Slices     : SliceYaml array

and IOBox =
#endif
  | StringBox of StringBoxD
  | IntBox    of IntBoxD
  | FloatBox  of FloatBoxD
  | DoubleBox of DoubleBoxD
  | BoolBox   of BoolBoxD
  | ByteBox   of ByteBoxD
  | EnumBox   of EnumBoxD
  | ColorBox  of ColorBoxD
  | Compound  of CompoundBoxD

  member self.Id
    with get () =
      match self with
        | StringBox data -> data.Id
        | IntBox    data -> data.Id
        | FloatBox  data -> data.Id
        | DoubleBox data -> data.Id
        | BoolBox   data -> data.Id
        | ByteBox   data -> data.Id
        | EnumBox   data -> data.Id
        | ColorBox  data -> data.Id
        | Compound  data -> data.Id

  member self.Name
    with get () =
      match self with
        | StringBox data -> data.Name
        | IntBox    data -> data.Name
        | FloatBox  data -> data.Name
        | DoubleBox data -> data.Name
        | BoolBox   data -> data.Name
        | ByteBox   data -> data.Name
        | EnumBox   data -> data.Name
        | ColorBox  data -> data.Name
        | Compound  data -> data.Name

  member self.SetName name =
    match self with
    | StringBox data -> StringBox { data with Name = name }
    | IntBox    data -> IntBox    { data with Name = name }
    | FloatBox  data -> FloatBox  { data with Name = name }
    | DoubleBox data -> DoubleBox { data with Name = name }
    | BoolBox   data -> BoolBox   { data with Name = name }
    | ByteBox   data -> ByteBox   { data with Name = name }
    | EnumBox   data -> EnumBox   { data with Name = name }
    | ColorBox  data -> ColorBox  { data with Name = name }
    | Compound  data -> Compound  { data with Name = name }

  member self.Patch
    with get () =
      match self with
        | StringBox data -> data.Patch
        | IntBox    data -> data.Patch
        | FloatBox  data -> data.Patch
        | DoubleBox data -> data.Patch
        | BoolBox   data -> data.Patch
        | ByteBox   data -> data.Patch
        | EnumBox   data -> data.Patch
        | ColorBox  data -> data.Patch
        | Compound  data -> data.Patch

  member self.Slices
    with get () =
      match self with
        | StringBox data -> StringSlices   data.Slices
        | IntBox    data -> IntSlices      data.Slices
        | FloatBox  data -> FloatSlices    data.Slices
        | DoubleBox data -> DoubleSlices   data.Slices
        | BoolBox   data -> BoolSlices     data.Slices
        | ByteBox   data -> ByteSlices     data.Slices
        | EnumBox   data -> EnumSlices     data.Slices
        | ColorBox  data -> ColorSlices    data.Slices
        | Compound  data -> CompoundSlices data.Slices

  //  ____       _   ____  _ _
  // / ___|  ___| |_/ ___|| (_) ___ ___
  // \___ \ / _ \ __\___ \| | |/ __/ _ \
  //  ___) |  __/ |_ ___) | | | (_|  __/
  // |____/ \___|\__|____/|_|_|\___\___|

  member self.SetSlice (value: Slice) =
    let update (arr : 'a array) (data: 'a) =

      if int value.Index > Array.length arr then
#if JAVASCRIPT
        /// Rationale:
        ///
        /// in JavaScript an array will re-allocate automatically under the hood
        /// hence we don't need to worry about out-of-bounds errors.
        let newarr = Array.copy arr
        newarr.[int value.Index] <- data
        newarr
#else
        /// Rationale:
        ///
        /// in .NET, we need to worry about out-of-bounds errors, and we
        /// detected that we are about to run into one, hence re-alloc, copy
        /// and finally set the value at the correct index.
        let newarr = Array.zeroCreate (int value.Index + 1)
        arr.CopyTo(newarr, 0)
        newarr.[int value.Index] <- data
        newarr
#endif
      else
        Array.mapi (fun i d -> if i = int value.Index then data else d) arr

    match self with
    | StringBox data as current ->
      match value with
        | StringSlice slice     -> StringBox { data with Slices = update data.Slices slice }
        | _                     -> current

    | IntBox data as current    ->
      match value with
        | IntSlice slice        -> IntBox { data with Slices = update data.Slices slice }
        | _                     -> current

    | FloatBox data as current  ->
      match value with
        | FloatSlice slice      -> FloatBox { data with Slices = update data.Slices slice }
        | _                     -> current

    | DoubleBox data as current ->
      match value with
        | DoubleSlice slice     -> DoubleBox { data with Slices = update data.Slices slice }
        | _                     -> current

    | BoolBox data as current   ->
      match value with
        | BoolSlice slice       -> BoolBox { data with Slices = update data.Slices slice }
        | _                     -> current

    | ByteBox data as current   ->
      match value with
        | ByteSlice slice       -> ByteBox { data with Slices = update data.Slices slice }
        | _                     -> current

    | EnumBox data as current   ->
      match value with
        | EnumSlice slice       -> EnumBox { data with Slices = update data.Slices slice }
        | _                     -> current

    | ColorBox data as current  ->
      match value with
        | ColorSlice slice      -> ColorBox { data with Slices = update data.Slices slice }
        | _                     -> current

    | Compound data as current  ->
      match value with
        | CompoundSlice slice   -> Compound { data with Slices = update data.Slices slice }
        | _                     -> current

  member self.SetSlices slices =
    match self with
    | StringBox data as value ->
      match slices with
      | StringSlices arr -> StringBox { data with Slices = arr }
      | _ -> value

    | IntBox data as value ->
      match slices with
      | IntSlices arr -> IntBox { data with Slices = arr }
      | _ -> value

    | FloatBox data as value ->
      match slices with
      | FloatSlices  arr -> FloatBox { data with Slices = arr }
      | _ -> value

    | DoubleBox data as value ->
      match slices with
      | DoubleSlices arr -> DoubleBox { data with Slices = arr }
      | _ -> value

    | BoolBox data as value ->
      match slices with
      | BoolSlices arr -> BoolBox { data with Slices = arr }
      | _ -> value

    | ByteBox data as value ->
      match slices with
      | ByteSlices arr -> ByteBox { data with Slices = arr }
      | _ -> value

    | EnumBox data as value ->
      match slices with
      | EnumSlices arr -> EnumBox { data with Slices = arr }
      | _ -> value

    | ColorBox data as value ->
      match slices with
      | ColorSlices arr -> ColorBox { data with Slices = arr }
      | _ -> value

    | Compound data as value ->
      match slices with
      | CompoundSlices arr -> Compound { data with Slices = arr }
      | _ -> value


  static member Toggle(id, name, patch, tags, values) =
    BoolBox { Id         = id
            ; Name       = name
            ; Patch      = patch
            ; Tags       = tags
            ; Behavior   = Behavior.Toggle
            ; Slices     = values }

  static member Bang(id, name, patch, tags, values) =
    BoolBox { Id         = id
            ; Name       = name
            ; Patch      = patch
            ; Tags       = tags
            ; Behavior   = Behavior.Bang
            ; Slices     = values }

  static member String(id, name, patch, tags, values) =
    StringBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; StringType = Simple
              ; FileMask   = None
              ; MaxChars   = sizeof<int>
              ; Slices     = values }

  static member MultiLine(id, name, patch, tags, values) =
    StringBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; StringType = MultiLine
              ; FileMask   = None
              ; MaxChars   = sizeof<int>
              ; Slices     = values }

  static member FileName(id, name, patch, tags, filemask, values) =
    StringBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; StringType = FileName
              ; FileMask   = Some filemask
              ; MaxChars   = sizeof<int>
              ; Slices     = values }

  static member Directory(id, name, patch, tags, filemask, values) =
    StringBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; StringType = Directory
              ; FileMask   = Some filemask
              ; MaxChars   = sizeof<int>
              ; Slices     = values }

  static member Url(id, name, patch, tags, values) =
    StringBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; StringType = Url
              ; FileMask   = None
              ; MaxChars   = sizeof<int>
              ; Slices     = values }

  static member IP(id, name, patch, tags, values) =
    StringBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; StringType = Url
              ; FileMask   = None
              ; MaxChars   = sizeof<int>
              ; Slices     = values }

  static member Float(id, name, patch, tags, values) =
    FloatBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; VecSize    = 1u
              ; Min        = 0
              ; Max        = sizeof<float>
              ; Unit       = ""
              ; Precision  = 4u
              ; Slices     = values }

  static member Double(id, name, patch, tags, values) =
    DoubleBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; VecSize    = 1u
              ; Min        = 0
              ; Max        = sizeof<double>
              ; Unit       = ""
              ; Precision  = 4u
              ; Slices     = values }

  static member Bytes(id, name, patch, tags, values) =
    ByteBox { Id         = id
            ; Name       = name
            ; Patch      = patch
            ; Tags       = tags
            ; Slices     = values }

  static member Color(id, name, patch, tags, values) =
    ColorBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; Slices     = values }

  static member Enum(id, name, patch, tags, properties, values) =
    EnumBox { Id         = id
            ; Name       = name
            ; Patch      = patch
            ; Tags       = tags
            ; Properties = properties
            ; Slices     = values }

  static member CompoundBox(id, name, patch, tags, values) =
    Compound { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; Slices     = values }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<IOBoxFB> =
    let inline build (data: ^t) tipe =
      let offset = Binary.toOffset builder data
      IOBoxFB.StartIOBoxFB(builder)
#if JAVASCRIPT
      IOBoxFB.AddIOBox(builder, offset)
#else
      IOBoxFB.AddIOBox(builder, offset.Value)
#endif
      IOBoxFB.AddIOBoxType(builder, tipe)
      IOBoxFB.EndIOBoxFB(builder)

    match self with
    | StringBox data -> build data IOBoxTypeFB.StringBoxFB
    | IntBox    data -> build data IOBoxTypeFB.IntBoxFB
    | FloatBox  data -> build data IOBoxTypeFB.FloatBoxFB
    | DoubleBox data -> build data IOBoxTypeFB.DoubleBoxFB
    | BoolBox   data -> build data IOBoxTypeFB.BoolBoxFB
    | ByteBox   data -> build data IOBoxTypeFB.ByteBoxFB
    | EnumBox   data -> build data IOBoxTypeFB.EnumBoxFB
    | ColorBox  data -> build data IOBoxTypeFB.ColorBoxFB
    | Compound  data -> build data IOBoxTypeFB.CompoundBoxFB

  static member FromFB(fb: IOBoxFB) : IOBox option =
#if JAVASCRIPT
    match fb.IOBoxType with
    | x when x = IOBoxTypeFB.StringBoxFB ->
      StringBoxFB.Create()
      |> fb.IOBox
      |> StringBoxD.FromFB
      |> Option.map StringBox

    | x when x = IOBoxTypeFB.IntBoxFB ->
      IntBoxFB.Create()
      |> fb.IOBox
      |> IntBoxD.FromFB
      |> Option.map IntBox

    | x when x = IOBoxTypeFB.FloatBoxFB ->
      FloatBoxFB.Create()
      |> fb.IOBox
      |> FloatBoxD.FromFB
      |> Option.map FloatBox

    | x when x = IOBoxTypeFB.DoubleBoxFB ->
      DoubleBoxFB.Create()
      |> fb.IOBox
      |> DoubleBoxD.FromFB
      |> Option.map DoubleBox

    | x when x = IOBoxTypeFB.BoolBoxFB ->
      BoolBoxFB.Create()
      |> fb.IOBox
      |> BoolBoxD.FromFB
      |> Option.map BoolBox

    | x when x = IOBoxTypeFB.ByteBoxFB ->
      ByteBoxFB.Create()
      |> fb.IOBox
      |> ByteBoxD.FromFB
      |> Option.map ByteBox

    | x when x = IOBoxTypeFB.EnumBoxFB ->
      EnumBoxFB.Create()
      |> fb.IOBox
      |> EnumBoxD.FromFB
      |> Option.map EnumBox

    | x when x = IOBoxTypeFB.ColorBoxFB ->
      ColorBoxFB.Create()
      |> fb.IOBox
      |> ColorBoxD.FromFB
      |> Option.map ColorBox

    | x when x = IOBoxTypeFB.CompoundBoxFB ->
      CompoundBoxFB.Create()
      |> fb.IOBox
      |> CompoundBoxD.FromFB
      |> Option.map Compound

    | _ -> None
#else
    match fb.IOBoxType with
    | IOBoxTypeFB.StringBoxFB ->
      let v = fb.IOBox<StringBoxFB>()
      if v.HasValue then
        v.Value
        |> StringBoxD.FromFB
        |> Option.map StringBox
      else None

    | IOBoxTypeFB.IntBoxFB ->
      let v = fb.IOBox<IntBoxFB>()
      if v.HasValue then
        v.Value
        |> IntBoxD.FromFB
        |> Option.map IntBox
      else None

    | IOBoxTypeFB.FloatBoxFB ->
      let v = fb.IOBox<FloatBoxFB>()
      if v.HasValue then
        v.Value
        |> FloatBoxD.FromFB
        |> Option.map FloatBox
      else None

    | IOBoxTypeFB.DoubleBoxFB ->
      let v = fb.IOBox<DoubleBoxFB>()
      if v.HasValue then
        v.Value
        |> DoubleBoxD.FromFB
        |> Option.map DoubleBox
      else None

    | IOBoxTypeFB.BoolBoxFB ->
      let v = fb.IOBox<BoolBoxFB>()
      if v.HasValue then
        v.Value
        |> BoolBoxD.FromFB
        |> Option.map BoolBox
      else None

    | IOBoxTypeFB.ByteBoxFB ->
      let v = fb.IOBox<ByteBoxFB>()
      if v.HasValue then
        v.Value
        |> ByteBoxD.FromFB
        |> Option.map ByteBox
      else None

    | IOBoxTypeFB.EnumBoxFB ->
      let v = fb.IOBox<EnumBoxFB>()
      if v.HasValue then
        v.Value
        |> EnumBoxD.FromFB
        |> Option.map EnumBox
      else None

    | IOBoxTypeFB.ColorBoxFB ->
      let v = fb.IOBox<ColorBoxFB>()
      if v.HasValue then
        v.Value
        |> ColorBoxD.FromFB
        |> Option.map ColorBox
      else None

    | IOBoxTypeFB.CompoundBoxFB ->
      let v = fb.IOBox<CompoundBoxFB>()
      if v.HasValue then
        v.Value
        |> CompoundBoxD.FromFB
        |> Option.map Compound
      else None

    | _ -> None
#endif

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : IOBox option =
    Binary.createBuffer bytes
    |> IOBoxFB.GetRootAsIOBoxFB
    |> IOBox.FromFB

// __   __              _
// \ \ / /_ _ _ __ ___ | |
//  \ V / _` | '_ ` _ \| |
//   | | (_| | | | | | | |
//   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    let yaml = new IOBoxYaml()
    match self with
    | StringBox data ->
      let mask =
        match data.FileMask with
        | Some mask -> mask
        | _ -> null

      yaml.BoxType    <- "StringBox"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.FileMask   <- mask
      yaml.MaxChars   <- data.MaxChars
      yaml.StringType <- string data.StringType
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | IntBox data ->
      yaml.BoxType    <- "IntBox"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.VecSize    <- data.VecSize
      yaml.Min        <- data.Min
      yaml.Max        <- data.Max
      yaml.Unit       <- data.Unit
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | FloatBox data ->
      yaml.BoxType    <- "FloatBox"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.VecSize    <- data.VecSize
      yaml.Precision  <- data.Precision
      yaml.Min        <- data.Min
      yaml.Max        <- data.Max
      yaml.Unit       <- data.Unit
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | DoubleBox data ->
      yaml.BoxType    <- "DoubleBox"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.VecSize    <- data.VecSize
      yaml.Precision  <- data.Precision
      yaml.Min        <- data.Min
      yaml.Max        <- data.Max
      yaml.Unit       <- data.Unit
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | BoolBox data ->
      yaml.BoxType    <- "BoolBox"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Behavior   <- string data.Behavior
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | ByteBox data ->
      yaml.BoxType    <- "ByteBox"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | EnumBox data ->
      yaml.BoxType    <- "EnumBox"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Properties <- Array.map Yaml.toYaml data.Properties
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | ColorBox  data ->
      yaml.BoxType    <- "ColorBox"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | Compound data ->
      yaml.BoxType    <- "Compound"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    yaml

  static member FromYamlObject(yml: IOBoxYaml) =
    let inline parseSlices (slices: SliceYaml array) =
      Array.fold
        (fun m yml ->
           match Yaml.fromYaml yml with
           | Some slice -> Array.append m [| slice |]
           | _          -> m)
        [| |]
        slices

    try
      match yml.BoxType with
      | "StringBox" ->
        match StringType.TryParse yml.StringType with
        | Some strtype ->
          StringBox {
            Id         = Id yml.Id
            Name       = yml.Name
            Patch      = Id yml.Patch
            Tags       = yml.Tags
            FileMask   = if isNull yml.FileMask then None else Some yml.FileMask
            MaxChars   = yml.MaxChars
            StringType = strtype
            Slices     = parseSlices yml.Slices
          } |> Some
        | _ ->
          printfn "Could not parse StringType from yml: %s" yml.StringType
          None
      | "IntBox" ->
        IntBox {
          Id       = Id yml.Id
          Name     = yml.Name
          Patch    = Id yml.Patch
          Tags     = yml.Tags
          VecSize  = yml.VecSize
          Min      = yml.Min
          Max      = yml.Max
          Unit     = yml.Unit
          Slices   = parseSlices yml.Slices
        } |> Some

      | "FloatBox" ->
        FloatBox {
          Id        = Id yml.Id
          Name      = yml.Name
          Patch     = Id yml.Patch
          Tags      = yml.Tags
          VecSize   = yml.VecSize
          Min       = yml.Min
          Max       = yml.Max
          Unit      = yml.Unit
          Precision = yml.Precision
          Slices    = parseSlices yml.Slices
        } |> Some

      | "DoubleBox" ->
        DoubleBox {
          Id        = Id yml.Id
          Name      = yml.Name
          Patch     = Id yml.Patch
          Tags      = yml.Tags
          VecSize   = yml.VecSize
          Min       = yml.Min
          Max       = yml.Max
          Unit      = yml.Unit
          Precision = yml.Precision
          Slices    = parseSlices yml.Slices
        } |> Some

      | "BoolBox"   ->
        match Behavior.TryParse yml.Behavior with
        | Some behavior ->
          BoolBox {
            Id       = Id yml.Id
            Name     = yml.Name
            Patch    = Id yml.Patch
            Tags     = yml.Tags
            Behavior = behavior
            Slices   = parseSlices yml.Slices
          } |> Some
        | _ ->
          printfn "Could not parse Behavior from yml: %s" yml.Behavior
          None

      | "ByteBox" ->
        ByteBox {
          Id     = Id yml.Id
          Name   = yml.Name
          Patch  = Id yml.Patch
          Tags   = yml.Tags
          Slices = parseSlices yml.Slices
        } |> Some

      | "EnumBox"   ->
        let properties =
          Array.fold
            (fun m yml ->
              match Yaml.fromYaml yml with
              | Some prop -> Array.append m [| prop |]
              | _         -> m)
            [| |]
            yml.Properties

        EnumBox {
          Id         = Id yml.Id
          Name       = yml.Name
          Patch      = Id yml.Patch
          Tags       = yml.Tags
          Properties = properties
          Slices     = parseSlices yml.Slices
        } |> Some

      | "ColorBox"  ->
        ColorBox {
          Id     = Id yml.Id
          Name   = yml.Name
          Patch  = Id yml.Patch
          Tags   = yml.Tags
          Slices = parseSlices yml.Slices
        } |> Some

      | "Compound" ->
        Compound {
          Id     = Id yml.Id
          Name   = yml.Name
          Patch  = Id yml.Patch
          Tags   = yml.Tags
          Slices = parseSlices yml.Slices
        } |> Some
      | _ -> None
    with
      | exn ->
        printfn "Could not parse IOBoxYml: %s" exn.Message
        None

  member self.ToYaml(serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYaml(str: string) =
    let serializer = new Serializer()
    serializer.Deserialize<IOBoxYaml>(str)
    |> IOBox.FromYamlObject

#endif
//  ____              _ ____
// | __ )  ___   ___ | | __ )  _____  __
// |  _ \ / _ \ / _ \| |  _ \ / _ \ \/ /
// | |_) | (_) | (_) | | |_) | (_) >  <
// |____/ \___/ \___/|_|____/ \___/_/\_\

and BoolBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; Behavior   : Behavior
  ; Slices     : BoolSliceD array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let behavior = self.Behavior.ToOffset builder
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let tags = BoolBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = BoolBoxFB.CreateSlicesVector(builder, sliceoffsets)
    BoolBoxFB.StartBoolBoxFB(builder)
    BoolBoxFB.AddId(builder, id)
    BoolBoxFB.AddName(builder, name)
    BoolBoxFB.AddPatch(builder, patch)
    BoolBoxFB.AddBehavior(builder, behavior)
    BoolBoxFB.AddTags(builder, tags)
    BoolBoxFB.AddSlices(builder, slices)
    BoolBoxFB.EndBoolBoxFB(builder)

  static member FromFB(fb: BoolBoxFB) : BoolBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> BoolSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> BoolSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    Behavior.FromFB fb.Behavior
    |> Option.map
      (fun behavior ->
        { Id         = Id fb.Id
        ; Name       = fb.Name
        ; Patch      = Id fb.Patch
        ; Tags       = tags
        ; Behavior   = behavior
        ; Slices     = slices })

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : BoolBoxD option =
    Binary.createBuffer bytes
    |> BoolBoxFB.GetRootAsBoolBoxFB
    |> BoolBoxD.FromFB

//  ____              _ ____  _ _
// | __ )  ___   ___ | / ___|| (_) ___ ___
// |  _ \ / _ \ / _ \| \___ \| | |/ __/ _ \
// | |_) | (_) | (_) | |___) | | | (_|  __/
// |____/ \___/ \___/|_|____/|_|_|\___\___|

and BoolSliceD =
  { Index: Index
  ; Value: bool }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    BoolSliceFB.StartBoolSliceFB(builder)
    BoolSliceFB.AddIndex(builder, self.Index)
    BoolSliceFB.AddValue(builder, self.Value)
    BoolSliceFB.EndBoolSliceFB(builder)

  static member FromFB(fb: BoolSliceFB) : BoolSliceD option =
    try
      { Index = fb.Index
      ; Value = fb.Value }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : BoolSliceD option =
    Binary.createBuffer bytes
    |> BoolSliceFB.GetRootAsBoolSliceFB
    |> BoolSliceD.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.BoolSlice(self.Index, self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "BoolSlice" -> yaml.ToBoolSliceD()
    | _           -> None

#endif

//  ___       _   ____
// |_ _|_ __ | |_| __ )  _____  __
//  | || '_ \| __|  _ \ / _ \ \/ /
//  | || | | | |_| |_) | (_) >  <
// |___|_| |_|\__|____/ \___/_/\_\

and IntBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; VecSize    : uint32
  ; Min        : int
  ; Max        : int
  ; Unit       : string
  ; Slices     : IntSliceD array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let unit = self.Unit |> builder.CreateString
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let tags = IntBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = IntBoxFB.CreateSlicesVector(builder, sliceoffsets)
    IntBoxFB.StartIntBoxFB(builder)
    IntBoxFB.AddId(builder, id)
    IntBoxFB.AddName(builder, name)
    IntBoxFB.AddPatch(builder, patch)
    IntBoxFB.AddTags(builder, tags)
    IntBoxFB.AddVecSize(builder, self.VecSize)
    IntBoxFB.AddMin(builder, self.Min)
    IntBoxFB.AddMax(builder, self.Max)
    IntBoxFB.AddUnit(builder, unit)
    IntBoxFB.AddSlices(builder, slices)
    IntBoxFB.EndIntBoxFB(builder)

  static member FromFB(fb: IntBoxFB) : IntBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength
    let unit = if isNull fb.Unit then "" else fb.Unit

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> IntSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> IntSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    try
      { Id         = Id fb.Id
      ; Name       = fb.Name
      ; Patch      = Id fb.Patch
      ; Tags       = tags
      ; VecSize    = fb.VecSize
      ; Min        = fb.Min
      ; Max        = fb.Max
      ; Unit       = unit
      ; Slices     = slices }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : IntBoxD option =
    Binary.createBuffer bytes
    |> IntBoxFB.GetRootAsIntBoxFB
    |> IntBoxD.FromFB

//  ___       _   ____  _ _
// |_ _|_ __ | |_/ ___|| (_) ___ ___
//  | || '_ \| __\___ \| | |/ __/ _ \
//  | || | | | |_ ___) | | | (_|  __/
// |___|_| |_|\__|____/|_|_|\___\___|

and IntSliceD =
  { Index: Index
  ; Value: int }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    IntSliceFB.StartIntSliceFB(builder)
    IntSliceFB.AddIndex(builder, self.Index)
    IntSliceFB.AddValue(builder, self.Value)
    IntSliceFB.EndIntSliceFB(builder)

  static member FromFB(fb: IntSliceFB) : IntSliceD option =
    try
      { Index = fb.Index
      ; Value = fb.Value }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : IntSliceD option =
    Binary.createBuffer bytes
    |> IntSliceFB.GetRootAsIntSliceFB
    |> IntSliceD.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.IntSlice(self.Index, self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "IntSlice" -> yaml.ToIntSliceD()
    | _          -> None

#endif

//  _____ _             _   ____
// |  ___| | ___   __ _| |_| __ )  _____  __
// | |_  | |/ _ \ / _` | __|  _ \ / _ \ \/ /
// |  _| | | (_) | (_| | |_| |_) | (_) >  <
// |_|   |_|\___/ \__,_|\__|____/ \___/_/\_\

and FloatBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; VecSize    : uint32
  ; Min        : int
  ; Max        : int
  ; Unit       : string
  ; Precision  : uint32
  ; Slices     : FloatSliceD array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let unit = self.Unit |> builder.CreateString
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let tags = FloatBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = FloatBoxFB.CreateSlicesVector(builder, sliceoffsets)
    FloatBoxFB.StartFloatBoxFB(builder)
    FloatBoxFB.AddId(builder, id)
    FloatBoxFB.AddName(builder, name)
    FloatBoxFB.AddPatch(builder, patch)
    FloatBoxFB.AddTags(builder, tags)
    FloatBoxFB.AddVecSize(builder, self.VecSize)
    FloatBoxFB.AddMin(builder, self.Min)
    FloatBoxFB.AddMax(builder, self.Max)
    FloatBoxFB.AddUnit(builder, unit)
    FloatBoxFB.AddPrecision(builder, self.Precision)
    FloatBoxFB.AddSlices(builder, slices)
    FloatBoxFB.EndFloatBoxFB(builder)

  static member FromFB(fb: FloatBoxFB) : FloatBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength
    let unit = if isNull fb.Unit then "" else fb.Unit

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> FloatSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> FloatSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    try
      { Id         = Id fb.Id
      ; Name       = fb.Name
      ; Patch      = Id fb.Patch
      ; Tags       = tags
      ; VecSize    = fb.VecSize
      ; Min        = fb.Min
      ; Max        = fb.Max
      ; Unit       = unit
      ; Precision  = fb.Precision
      ; Slices     = slices }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : FloatBoxD option =
    Binary.createBuffer bytes
    |> FloatBoxFB.GetRootAsFloatBoxFB
    |> FloatBoxD.FromFB

//  _____ _             _   ____  _ _
// |  ___| | ___   __ _| |_/ ___|| (_) ___ ___
// | |_  | |/ _ \ / _` | __\___ \| | |/ __/ _ \
// |  _| | | (_) | (_| | |_ ___) | | | (_|  __/
// |_|   |_|\___/ \__,_|\__|____/|_|_|\___\___|

and FloatSliceD =
  { Index: Index
  ; Value: float }


  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    FloatSliceFB.StartFloatSliceFB(builder)
    FloatSliceFB.AddIndex(builder, self.Index)
    FloatSliceFB.AddValue(builder, float32 self.Value)
    FloatSliceFB.EndFloatSliceFB(builder)

  static member FromFB(fb: FloatSliceFB) : FloatSliceD option =
    try
      { Index = fb.Index
      ; Value = float fb.Value }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : FloatSliceD option =
    Binary.createBuffer bytes
    |> FloatSliceFB.GetRootAsFloatSliceFB
    |> FloatSliceD.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.FloatSlice(self.Index, self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "FloatSlice" -> yaml.ToFloatSliceD()
    | _            -> None

#endif

//  ____              _     _      ____
// |  _ \  ___  _   _| |__ | | ___| __ )  _____  __
// | | | |/ _ \| | | | '_ \| |/ _ \  _ \ / _ \ \/ /
// | |_| | (_) | |_| | |_) | |  __/ |_) | (_) >  <
// |____/ \___/ \__,_|_.__/|_|\___|____/ \___/_/\_\

and DoubleBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; VecSize    : uint32
  ; Min        : int
  ; Max        : int
  ; Unit       : string
  ; Precision  : uint32
  ; Slices     : DoubleSliceD array }


  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let unit = self.Unit |> builder.CreateString
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let tags = DoubleBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = DoubleBoxFB.CreateSlicesVector(builder, sliceoffsets)
    DoubleBoxFB.StartDoubleBoxFB(builder)
    DoubleBoxFB.AddId(builder, id)
    DoubleBoxFB.AddName(builder, name)
    DoubleBoxFB.AddPatch(builder, patch)
    DoubleBoxFB.AddTags(builder, tags)
    DoubleBoxFB.AddVecSize(builder, self.VecSize)
    DoubleBoxFB.AddMin(builder, self.Min)
    DoubleBoxFB.AddMax(builder, self.Max)
    DoubleBoxFB.AddUnit(builder, unit)
    DoubleBoxFB.AddPrecision(builder, self.Precision)
    DoubleBoxFB.AddSlices(builder, slices)
    DoubleBoxFB.EndDoubleBoxFB(builder)

  static member FromFB(fb: DoubleBoxFB) : DoubleBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength
    let unit = if isNull fb.Unit then "" else fb.Unit

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> DoubleSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> DoubleSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    try
      { Id         = Id fb.Id
      ; Name       = fb.Name
      ; Patch      = Id fb.Patch
      ; Tags       = tags
      ; VecSize    = fb.VecSize
      ; Min        = fb.Min
      ; Max        = fb.Max
      ; Unit       = unit
      ; Precision  = fb.Precision
      ; Slices     = slices }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : DoubleBoxD option =
    Binary.createBuffer bytes
    |> DoubleBoxFB.GetRootAsDoubleBoxFB
    |> DoubleBoxD.FromFB

//  ____              _     _      ____  _ _
// |  _ \  ___  _   _| |__ | | ___/ ___|| (_) ___ ___
// | | | |/ _ \| | | | '_ \| |/ _ \___ \| | |/ __/ _ \
// | |_| | (_) | |_| | |_) | |  __/___) | | | (_|  __/
// |____/ \___/ \__,_|_.__/|_|\___|____/|_|_|\___\___|

and DoubleSliceD =
  { Index: Index
  ; Value: double }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    DoubleSliceFB.StartDoubleSliceFB(builder)
    DoubleSliceFB.AddIndex(builder, self.Index)
    DoubleSliceFB.AddValue(builder, self.Value)
    DoubleSliceFB.EndDoubleSliceFB(builder)

  static member FromFB(fb: DoubleSliceFB) : DoubleSliceD option =
    try
      { Index = fb.Index
      ; Value = fb.Value }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : DoubleSliceD option =
    Binary.createBuffer bytes
    |> DoubleSliceFB.GetRootAsDoubleSliceFB
    |> DoubleSliceD.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.DoubleSlice(self.Index, self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "DoubleSlice" -> yaml.ToDoubleSliceD()
    | _             -> None

#endif

//  ____        _       ____
// | __ ) _   _| |_ ___| __ )  _____  __
// |  _ \| | | | __/ _ \  _ \ / _ \ \/ /
// | |_) | |_| | ||  __/ |_) | (_) >  <
// |____/ \__, |\__\___|____/ \___/_/\_\
//        |___/

and ByteBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag        array
  ; Slices     : ByteSliceD array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let tags = ByteBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = ByteBoxFB.CreateSlicesVector(builder, sliceoffsets)
    ByteBoxFB.StartByteBoxFB(builder)
    ByteBoxFB.AddId(builder, id)
    ByteBoxFB.AddName(builder, name)
    ByteBoxFB.AddPatch(builder, patch)
    ByteBoxFB.AddTags(builder, tags)
    ByteBoxFB.AddSlices(builder, slices)
    ByteBoxFB.EndByteBoxFB(builder)

  static member FromFB(fb: ByteBoxFB) : ByteBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> ByteSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> ByteSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    try
      { Id         = Id fb.Id
      ; Name       = fb.Name
      ; Patch      = Id fb.Patch
      ; Tags       = tags
      ; Slices     = slices }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : ByteBoxD option =
    Binary.createBuffer bytes
    |> ByteBoxFB.GetRootAsByteBoxFB
    |> ByteBoxD.FromFB

//  ____        _       ____  _ _
// | __ ) _   _| |_ ___/ ___|| (_) ___ ___
// |  _ \| | | | __/ _ \___ \| | |/ __/ _ \
// | |_) | |_| | ||  __/___) | | | (_|  __/
// |____/ \__, |\__\___|____/|_|_|\___\___|
//        |___/

and [<CustomEquality;CustomComparison>] ByteSliceD =
  { Index: Index
  ; Value: Binary.Buffer }

  override self.Equals(other) =
    match other with
    | :? ByteSliceD as slice ->
      (self :> System.IEquatable<ByteSliceD>).Equals(slice)
    | _ -> false

  override self.GetHashCode() =
    let mutable hash = 42
#if JAVASCRIPT
    hash <- (hash * 7) + hashCode (string self.Index)
    hash <- (hash * 7) + hashCode (string self.Value.byteLength)
#else
    hash <- (hash * 7) + self.Index.GetHashCode()
    hash <- (hash * 7) + self.Value.GetHashCode()
#endif
    hash

  interface System.IComparable with
    member self.CompareTo other =
      match other with
      | :? ByteSliceD as slice -> compare self.Index slice.Index
      | _ -> invalidArg "other" "cannot compare value of different types"

  interface System.IEquatable<ByteSliceD> with
    member self.Equals(slice: ByteSliceD) =
      let mutable contentsEqual = false
      let lengthEqual =
#if JAVASCRIPT
        let result = self.Value.byteLength = slice.Value.byteLength
        if result then
          let me = Fable.Import.JS.Uint8Array.Create(self.Value)
          let it = Fable.Import.JS.Uint8Array.Create(slice.Value)
          let mutable contents = true
          let mutable i = 0
          while i < int self.Value.byteLength do
            if contents then
              contents <- me.[i] = it.[i]
            i <- i + 1
          contentsEqual <- contents
        result
#else
        let result = Array.length self.Value = Array.length slice.Value
        if result then
          let mutable contents = true
          for i in 0 .. (Array.length self.Value - 1) do
            if contents then
              contents <- self.Value.[i] = slice.Value.[i]
          contentsEqual <- contents
        result
#endif
      slice.Index = self.Index &&
      lengthEqual &&
      contentsEqual

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.ByteSlice(self.Index,  Convert.ToBase64String self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "ByteSlice" -> yaml.ToByteSliceD()
    | _           -> None

#endif

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let encode bytes =
#if JAVASCRIPT
      let mutable str = ""
      let arr = Fable.Import.JS.Uint8Array.Create(self.Value)
      for i in 0 .. (int arr.length - 1) do
        str <- str + Fable.Import.JS.String.fromCharCode arr.[i]
      Fable.Import.Browser.window.btoa str
#else
      Convert.ToBase64String(bytes)
#endif

    let encoded = encode self.Value
    let bytes = builder.CreateString encoded
    ByteSliceFB.StartByteSliceFB(builder)
    ByteSliceFB.AddIndex(builder, self.Index)
    ByteSliceFB.AddValue(builder, bytes)
    ByteSliceFB.EndByteSliceFB(builder)

  static member FromFB(fb: ByteSliceFB) : ByteSliceD option =
    let decode str =
#if JAVASCRIPT
      let binary = Fable.Import.Browser.window.atob str
      let bytes = Fable.Import.JS.Uint8Array.Create(float binary.Length)
      for i in 0 .. (binary.Length - 1) do
        bytes.[i] <- charCodeAt binary i
      bytes.buffer
#else
      Convert.FromBase64String(str)
#endif

    try
      let values = decode fb.Value
      { Index = fb.Index
      ; Value = values }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : ByteSliceD option =
    Binary.createBuffer bytes
    |> ByteSliceFB.GetRootAsByteSliceFB
    |> ByteSliceD.FromFB

//  _____                       ____
// | ____|_ __  _   _ _ __ ___ | __ )  _____  __
// |  _| | '_ \| | | | '_ ` _ \|  _ \ / _ \ \/ /
// | |___| | | | |_| | | | | | | |_) | (_) >  <
// |_____|_| |_|\__,_|_| |_| |_|____/ \___/_/\_\

and EnumBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag        array
  ; Properties : Property   array
  ; Slices     : EnumSliceD array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let propoffsets =
      Array.map (fun (prop: Property) ->
                 let key, value =
                    builder.CreateString prop.Key, builder.CreateString prop.Value
                 EnumPropertyFB.StartEnumPropertyFB(builder)
                 EnumPropertyFB.AddKey(builder, key)
                 EnumPropertyFB.AddValue(builder, value)
                 EnumPropertyFB.EndEnumPropertyFB(builder))
        self.Properties
    let tags = EnumBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = EnumBoxFB.CreateSlicesVector(builder, sliceoffsets)
    let properties = EnumBoxFB.CreatePropertiesVector(builder, propoffsets)
    EnumBoxFB.StartEnumBoxFB(builder)
    EnumBoxFB.AddId(builder, id)
    EnumBoxFB.AddName(builder, name)
    EnumBoxFB.AddPatch(builder, patch)
    EnumBoxFB.AddTags(builder, tags)
    EnumBoxFB.AddProperties(builder, properties)
    EnumBoxFB.AddSlices(builder, slices)
    EnumBoxFB.EndEnumBoxFB(builder)

  static member FromFB(fb: EnumBoxFB) : EnumBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength
    let properties = Array.zeroCreate fb.PropertiesLength

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.PropertiesLength do
      let prop = fb.Properties(i)
      properties.[i] <- { Key = prop.Key; Value = prop.Value }
      i <- i + 1
#else
    for i in 0 .. (fb.PropertiesLength - 1) do
      let prop = fb.Properties(i)
      if prop.HasValue then
        let value = prop.Value
        properties.[i] <- { Key = value.Key; Value = value.Value }
#endif

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> EnumSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> EnumSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    try
      { Id         = Id fb.Id
      ; Name       = fb.Name
      ; Patch      = Id fb.Patch
      ; Tags       = tags
      ; Properties = properties
      ; Slices     = slices }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromEnums(bytes: Binary.Buffer) : EnumBoxD option =
    Binary.createBuffer bytes
    |> EnumBoxFB.GetRootAsEnumBoxFB
    |> EnumBoxD.FromFB

//  _____                       ____  _ _
// | ____|_ __  _   _ _ __ ___ / ___|| (_) ___ ___
// |  _| | '_ \| | | | '_ ` _ \\___ \| | |/ __/ _ \
// | |___| | | | |_| | | | | | |___) | | | (_|  __/
// |_____|_| |_|\__,_|_| |_| |_|____/|_|_|\___\___|

and EnumSliceD =
  { Index : Index
  ; Value : Property }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let property =
      let key, value =
        builder.CreateString self.Value.Key, builder.CreateString self.Value.Value

      EnumPropertyFB.StartEnumPropertyFB(builder)
      EnumPropertyFB.AddKey(builder, key)
      EnumPropertyFB.AddValue(builder, value)
      EnumPropertyFB.EndEnumPropertyFB(builder)

    EnumSliceFB.StartEnumSliceFB(builder)
    EnumSliceFB.AddIndex(builder, self.Index)
    EnumSliceFB.AddValue(builder, property)
    EnumSliceFB.EndEnumSliceFB(builder)

  static member FromFB(fb: EnumSliceFB) : EnumSliceD option =
#if JAVASCRIPT
    let prop = fb.Value
    try
      { Index = fb.Index
      ; Value = { Key = prop.Key; Value = prop.Value } }
      |> Some
    with
      | _ -> None
#else
    let nullable = fb.Value
    if nullable.HasValue then
      let prop = nullable.Value
      try
        { Index = fb.Index
        ; Value = { Key = prop.Key; Value = prop.Value } }
        |> Some
      with
        | _ -> None
    else None
#endif

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromEnums(bytes: Binary.Buffer) : EnumSliceD option =
    Binary.createBuffer bytes
    |> EnumSliceFB.GetRootAsEnumSliceFB
    |> EnumSliceD.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.EnumSlice(self.Index, Yaml.toYaml self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "EnumSlice" -> yaml.ToEnumSliceD()
    | _           -> None

#endif

//   ____      _            ____
//  / ___|___ | | ___  _ __| __ )  _____  __
// | |   / _ \| |/ _ \| '__|  _ \ / _ \ \/ /
// | |__| (_) | | (_) | |  | |_) | (_) >  <
//  \____\___/|_|\___/|_|  |____/ \___/_/\_\

and ColorBoxD =
  { Id     : Id
  ; Name   : string
  ; Patch  : Id
  ; Tags   : Tag         array
  ; Slices : ColorSliceD array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let tags = ColorBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = ColorBoxFB.CreateSlicesVector(builder, sliceoffsets)
    ColorBoxFB.StartColorBoxFB(builder)
    ColorBoxFB.AddId(builder, id)
    ColorBoxFB.AddName(builder, name)
    ColorBoxFB.AddPatch(builder, patch)
    ColorBoxFB.AddTags(builder, tags)
    ColorBoxFB.AddSlices(builder, slices)
    ColorBoxFB.EndColorBoxFB(builder)

  static member FromFB(fb: ColorBoxFB) : ColorBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> ColorSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> ColorSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    try
      { Id     = Id fb.Id
      ; Name   = fb.Name
      ; Patch  = Id fb.Patch
      ; Tags   = tags
      ; Slices = slices }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromColors(bytes: Binary.Buffer) : ColorBoxD option =
    Binary.createBuffer bytes
    |> ColorBoxFB.GetRootAsColorBoxFB
    |> ColorBoxD.FromFB

//   ____      _            ____  _ _
//  / ___|___ | | ___  _ __/ ___|| (_) ___ ___
// | |   / _ \| |/ _ \| '__\___ \| | |/ __/ _ \
// | |__| (_) | | (_) | |   ___) | | | (_|  __/
//  \____\___/|_|\___/|_|  |____/|_|_|\___\___|

and ColorSliceD =
  { Index: Index
  ; Value: ColorSpace }


  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let offset = self.Value.ToOffset(builder)
    ColorSliceFB.StartColorSliceFB(builder)
    ColorSliceFB.AddIndex(builder, self.Index)
    ColorSliceFB.AddValue(builder, offset)
    ColorSliceFB.EndColorSliceFB(builder)

  static member FromFB(fb: ColorSliceFB) : ColorSliceD option =
#if JAVASCRIPT
    fb.Value
    |> ColorSpace.FromFB
    |> Option.map (fun color -> { Index = fb.Index; Value = color })
#else
    let nullable = fb.Value
    if nullable.HasValue then
      ColorSpace.FromFB nullable.Value
      |> Option.map (fun color -> { Index = fb.Index; Value = color })
    else None
#endif

  member self.ToColors() : Binary.Buffer = Binary.buildBuffer self

  static member FromColors(bytes: Binary.Buffer) : ColorSliceD option =
    Binary.createBuffer bytes
    |> ColorSliceFB.GetRootAsColorSliceFB
    |> ColorSliceD.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.ColorSlice(self.Index, Yaml.toYaml self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "ColorSlice" -> yaml.ToColorSliceD()
    | _            -> None

#endif

//  ____  _        _             ____
// / ___|| |_ _ __(_)_ __   __ _| __ )  _____  __
// \___ \| __| '__| | '_ \ / _` |  _ \ / _ \ \/ /
//  ___) | |_| |  | | | | | (_| | |_) | (_) >  <
// |____/ \__|_|  |_|_| |_|\__, |____/ \___/_/\_\
//                         |___/

and StringBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; StringType : StringType
  ; FileMask   : FileMask
  ; MaxChars   : MaxChars
  ; Slices     : StringSliceD array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let tipe = self.StringType.ToOffset(builder)
    let mask = self.FileMask |> Option.map builder.CreateString
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let tags = StringBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = StringBoxFB.CreateSlicesVector(builder, sliceoffsets)

    StringBoxFB.StartStringBoxFB(builder)
    StringBoxFB.AddId(builder, id)
    StringBoxFB.AddName(builder, name)
    StringBoxFB.AddPatch(builder, patch)
    StringBoxFB.AddTags(builder, tags)
    StringBoxFB.AddStringType(builder, tipe)

    Option.map (fun mask -> StringBoxFB.AddFileMask(builder, mask)) mask |> ignore

    StringBoxFB.AddMaxChars(builder, self.MaxChars)
    StringBoxFB.AddSlices(builder, slices)
    StringBoxFB.EndStringBoxFB(builder)

  static member FromFB(fb: StringBoxFB) : StringBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength
    let mask = if isNull fb.FileMask then None else Some fb.FileMask

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> StringSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> StringSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    StringType.FromFB fb.StringType
    |> Option.map
      (fun tipe ->
        { Id         = Id fb.Id
        ; Name       = fb.Name
        ; Patch      = Id fb.Patch
        ; Tags       = tags
        ; StringType = tipe
        ; FileMask   = mask
        ; MaxChars   = fb.MaxChars
        ; Slices     = slices })

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromStrings(bytes: Binary.Buffer) : StringBoxD option =
    Binary.createBuffer bytes
    |> StringBoxFB.GetRootAsStringBoxFB
    |> StringBoxD.FromFB

//  ____  _        _             ____  _ _
// / ___|| |_ _ __(_)_ __   __ _/ ___|| (_) ___ ___
// \___ \| __| '__| | '_ \ / _` \___ \| | |/ __/ _ \
//  ___) | |_| |  | | | | | (_| |___) | | | (_|  __/
// |____/ \__|_|  |_|_| |_|\__, |____/|_|_|\___\___|
//                         |___/

and StringSliceD =
  { Index : Index
  ; Value : string }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let value = builder.CreateString self.Value
    StringSliceFB.StartStringSliceFB(builder)
    StringSliceFB.AddIndex(builder, self.Index)
    StringSliceFB.AddValue(builder, value)
    StringSliceFB.EndStringSliceFB(builder)

  static member FromFB(fb: StringSliceFB) : StringSliceD option =
    try
      { Index = fb.Index
      ; Value = fb.Value }
      |> Some
    with
      | _ -> None

  member self.ToStrings() : Binary.Buffer = Binary.buildBuffer self

  static member FromStrings(bytes: Binary.Buffer) : StringSliceD option =
    Binary.createBuffer bytes
    |> StringSliceFB.GetRootAsStringSliceFB
    |> StringSliceD.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.StringSlice(self.Index, self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "StringSlice" -> yaml.ToStringSliceD()
    | _             -> None

#endif

//   ____                                            _ ____
//  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| | __ )  _____  __
// | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` |  _ \ / _ \ \/ /
// | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| | |_) | (_) >  <
//  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/ \___/_/\_\
//                      |_|

and CompoundBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag   array
  ; Slices     : CompoundSliceD array }


  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let patch = string self.Patch |> builder.CreateString
    let tagoffsets = Array.map builder.CreateString self.Tags
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let tags = CompoundBoxFB.CreateTagsVector(builder, tagoffsets)
    let slices = CompoundBoxFB.CreateSlicesVector(builder, sliceoffsets)
    CompoundBoxFB.StartCompoundBoxFB(builder)
    CompoundBoxFB.AddId(builder, id)
    CompoundBoxFB.AddName(builder, name)
    CompoundBoxFB.AddPatch(builder, patch)
    CompoundBoxFB.AddTags(builder, tags)
    CompoundBoxFB.AddSlices(builder, slices)
    CompoundBoxFB.EndCompoundBoxFB(builder)

  static member FromFB(fb: CompoundBoxFB) : CompoundBoxD option =
    let tags = Array.zeroCreate fb.TagsLength
    let slices = Array.zeroCreate fb.SlicesLength

    let mutable i = 0
    while i < fb.TagsLength do
      tags.[i] <- fb.Tags(i)
      i <- i + 1

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.SlicesLength do
      fb.Slices(i)
      |> CompoundSliceD.FromFB
      |> Option.map (fun slice -> slices.[i] <- slice)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.SlicesLength - 1) do
      let slice = fb.Slices(i)
      if slice.HasValue then
        slice.Value
        |> CompoundSliceD.FromFB
        |> Option.map (fun slice -> slices.[i] <- slice)
        |> ignore
#endif

    try
      { Id         = Id fb.Id
      ; Name       = fb.Name
      ; Patch      = Id fb.Patch
      ; Tags       = tags
      ; Slices     = slices }
      |> Some
    with
      | _ -> None

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromCompounds(bytes: Binary.Buffer) : CompoundBoxD option =
    Binary.createBuffer bytes
    |> CompoundBoxFB.GetRootAsCompoundBoxFB
    |> CompoundBoxD.FromFB

//   ____                                            _ ____  _ _
//  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| / ___|| (_) ___ ___
// | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` \___ \| | |/ __/ _ \
// | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| |___) | | | (_|  __/
//  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/|_|_|\___\___|
//                      |_|

and CompoundSliceD =
  { Index      : Index
  ; Value      : IOBox array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let ioboxoffsets = Array.map (fun (iobox: IOBox) -> iobox.ToOffset(builder)) self.Value
    let ioboxes = CompoundSliceFB.CreateValueVector(builder, ioboxoffsets)
    CompoundSliceFB.StartCompoundSliceFB(builder)
    CompoundSliceFB.AddIndex(builder, self.Index)
    CompoundSliceFB.AddValue(builder, ioboxes)
    CompoundSliceFB.EndCompoundSliceFB(builder)

  static member FromFB(fb: CompoundSliceFB) : CompoundSliceD option =
    let ioboxes = Array.zeroCreate fb.ValueLength

#if JAVASCRIPT
    let mutable i = 0
    while i < fb.ValueLength do
      fb.Value(i)
      |> IOBox.FromFB
      |> Option.map (fun iobox -> ioboxes.[i] <- iobox)
      |> ignore
      i <- i + 1
#else
    for i in 0 .. (fb.ValueLength - 1) do
      let nullable = fb.Value(i)
      if nullable.HasValue then
        nullable.Value
        |> IOBox.FromFB
        |> Option.map (fun iobox -> ioboxes.[i] <- iobox)
        |> ignore
#endif

    try
      { Index = fb.Index
      ; Value = ioboxes }
      |> Some
    with
      | _ -> None

  member self.ToCompounds() : Binary.Buffer = Binary.buildBuffer self

  static member FromCompounds(bytes: Binary.Buffer) : CompoundSliceD option =
    Binary.createBuffer bytes
    |> CompoundSliceFB.GetRootAsCompoundSliceFB
    |> CompoundSliceD.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    SliceYaml.CompoundSlice(self.Index, Array.map Yaml.toYaml self.Value)

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "CompoundSlice" -> yaml.ToCompoundSliceD()
    | _               -> None

#endif

//  ____  _ _
// / ___|| (_) ___ ___
// \___ \| | |/ __/ _ \
//  ___) | | | (_|  __/
// |____/|_|_|\___\___|

and Slice =
  | StringSlice   of StringSliceD
  | IntSlice      of IntSliceD
  | FloatSlice    of FloatSliceD
  | DoubleSlice   of DoubleSliceD
  | BoolSlice     of BoolSliceD
  | ByteSlice     of ByteSliceD
  | EnumSlice     of EnumSliceD
  | ColorSlice    of ColorSliceD
  | CompoundSlice of CompoundSliceD

  member self.Index
    with get () =
      match self with
      | StringSlice   data -> data.Index
      | IntSlice      data -> data.Index
      | FloatSlice    data -> data.Index
      | DoubleSlice   data -> data.Index
      | BoolSlice     data -> data.Index
      | ByteSlice     data -> data.Index
      | EnumSlice     data -> data.Index
      | ColorSlice    data -> data.Index
      | CompoundSlice data -> data.Index

  member self.Value
    with get () =
      match self with
      | StringSlice   data -> data.Value :> obj
      | IntSlice      data -> data.Value :> obj
      | FloatSlice    data -> data.Value :> obj
      | DoubleSlice   data -> data.Value :> obj
      | BoolSlice     data -> data.Value :> obj
      | ByteSlice     data -> data.Value :> obj
      | EnumSlice     data -> data.Value :> obj
      | ColorSlice    data -> data.Value :> obj
      | CompoundSlice data -> data.Value :> obj

  member self.StringValue
    with get () =
      match self with
      | StringSlice data -> Some data.Value
      | _                -> None

  member self.StringData
    with get () =
      match self with
      | StringSlice data -> Some data
      | _                -> None

  member self.IntValue
    with get () =
      match self with
      | IntSlice data -> Some data.Value
      | _             -> None

  member self.IntData
    with get () =
      match self with
      | IntSlice data -> Some data
      | _             -> None

  member self.FloatValue
    with get () =
      match self with
      | FloatSlice data -> Some data.Value
      | _               -> None

  member self.FloatData
    with get () =
      match self with
      | FloatSlice data -> Some data
      | _               -> None

  member self.DoubleValue
    with get () =
      match self with
      | DoubleSlice data -> Some data.Value
      | _                -> None

  member self.DoubleData
    with get () =
      match self with
      | DoubleSlice data -> Some data
      | _                -> None

  member self.BoolValue
    with get () =
      match self with
      | BoolSlice data -> Some data.Value
      | _              -> None

  member self.BoolData
    with get () =
      match self with
      | BoolSlice data -> Some data
      | _              -> None

  member self.ByteValue
    with get () =
      match self with
      | ByteSlice data -> Some data.Value
      | _              -> None

  member self.ByteData
    with get () =
      match self with
      | ByteSlice data -> Some data
      | _              -> None

  member self.EnumValue
    with get () =
      match self with
      | EnumSlice data -> Some data.Value
      | _              -> None

  member self.EnumData
    with get () =
      match self with
      | EnumSlice data -> Some data
      | _              -> None

  member self.ColorValue
    with get () =
      match self with
      | ColorSlice data -> Some data.Value
      | _               -> None

  member self.ColorData
    with get () =
      match self with
      | ColorSlice data -> Some data
      | _               -> None

  member self.CompoundValue
    with get () =
      match self with
      | CompoundSlice data -> Some data.Value
      | _                  -> None

  member self.CompoundData
    with get () =
      match self with
      | CompoundSlice data -> Some data
      | _                  -> None


  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let build tipe (offset: Offset<_>) =
      SliceFB.StartSliceFB(builder)
      SliceFB.AddSliceType(builder, tipe)
#if JAVASCRIPT
      SliceFB.AddSlice(builder, offset)
#else
      SliceFB.AddSlice(builder, offset.Value)
#endif
      SliceFB.EndSliceFB(builder)

    match self with
    | StringSlice   data -> data.ToOffset(builder) |> build SliceTypeFB.StringSliceFB
    | IntSlice      data -> data.ToOffset(builder) |> build SliceTypeFB.IntSliceFB
    | FloatSlice    data -> data.ToOffset(builder) |> build SliceTypeFB.FloatSliceFB
    | DoubleSlice   data -> data.ToOffset(builder) |> build SliceTypeFB.DoubleSliceFB
    | BoolSlice     data -> data.ToOffset(builder) |> build SliceTypeFB.BoolSliceFB
    | ByteSlice     data -> data.ToOffset(builder) |> build SliceTypeFB.ByteSliceFB
    | EnumSlice     data -> data.ToOffset(builder) |> build SliceTypeFB.EnumSliceFB
    | ColorSlice    data -> data.ToOffset(builder) |> build SliceTypeFB.ColorSliceFB
    | CompoundSlice data -> data.ToOffset(builder) |> build SliceTypeFB.CompoundSliceFB

  static member FromFB(fb: SliceFB) : Slice option =
    match fb.SliceType with
#if JAVASCRIPT
    | x when x = SliceTypeFB.StringSliceFB ->
      StringSliceFB.Create()
      |> fb.Slice
      |> StringSliceD.FromFB
      |> Option.map StringSlice

    | x when x = SliceTypeFB.IntSliceFB ->
      IntSliceFB.Create()
      |> fb.Slice
      |> IntSliceD.FromFB
      |> Option.map IntSlice

    | x when x = SliceTypeFB.FloatSliceFB ->
      FloatSliceFB.Create()
      |> fb.Slice
      |> FloatSliceD.FromFB
      |> Option.map FloatSlice

    | x when x = SliceTypeFB.DoubleSliceFB ->
      DoubleSliceFB.Create()
      |> fb.Slice
      |> DoubleSliceD.FromFB
      |> Option.map DoubleSlice

    | x when x = SliceTypeFB.BoolSliceFB ->
      BoolSliceFB.Create()
      |> fb.Slice
      |> BoolSliceD.FromFB
      |> Option.map BoolSlice

    | x when x = SliceTypeFB.ByteSliceFB ->
      ByteSliceFB.Create()
      |> fb.Slice
      |> ByteSliceD.FromFB
      |> Option.map ByteSlice

    | x when x = SliceTypeFB.EnumSliceFB ->
      EnumSliceFB.Create()
      |> fb.Slice
      |> EnumSliceD.FromFB
      |> Option.map EnumSlice

    | x when x = SliceTypeFB.ColorSliceFB ->
      ColorSliceFB.Create()
      |> fb.Slice
      |> ColorSliceD.FromFB
      |> Option.map ColorSlice

    | x when x = SliceTypeFB.CompoundSliceFB ->
      CompoundSliceFB.Create()
      |> fb.Slice
      |> CompoundSliceD.FromFB
      |> Option.map CompoundSlice

    | _ -> None
#else
    | SliceTypeFB.StringSliceFB   ->
      let slice = fb.Slice<StringSliceFB>()
      if slice.HasValue then
        slice.Value
        |> StringSliceD.FromFB
        |> Option.map StringSlice
      else None

    | SliceTypeFB.IntSliceFB      ->
      let slice = fb.Slice<IntSliceFB>()
      if slice.HasValue then
        slice.Value
        |> IntSliceD.FromFB
        |> Option.map IntSlice
      else None

    | SliceTypeFB.FloatSliceFB    ->
      let slice = fb.Slice<FloatSliceFB>()
      if slice.HasValue then
        slice.Value
        |> FloatSliceD.FromFB
        |> Option.map FloatSlice
      else None

    | SliceTypeFB.DoubleSliceFB   ->
      let slice = fb.Slice<DoubleSliceFB>()
      if slice.HasValue then
        slice.Value
        |> DoubleSliceD.FromFB
        |> Option.map DoubleSlice
      else None

    | SliceTypeFB.BoolSliceFB     ->
      let slice = fb.Slice<BoolSliceFB>()
      if slice.HasValue then
        slice.Value
        |> BoolSliceD.FromFB
        |> Option.map BoolSlice
      else None

    | SliceTypeFB.ByteSliceFB     ->
      let slice = fb.Slice<ByteSliceFB>()
      if slice.HasValue then
        slice.Value
        |> ByteSliceD.FromFB
        |> Option.map ByteSlice
      else None

    | SliceTypeFB.EnumSliceFB     ->
      let slice = fb.Slice<EnumSliceFB>()
      if slice.HasValue then
        slice.Value
        |> EnumSliceD.FromFB
        |> Option.map EnumSlice
      else None

    | SliceTypeFB.ColorSliceFB    ->
      let slice = fb.Slice<ColorSliceFB>()
      if slice.HasValue then
        slice.Value
        |> ColorSliceD.FromFB
        |> Option.map ColorSlice
      else None

    | SliceTypeFB.CompoundSliceFB ->
      let slice = fb.Slice<CompoundSliceFB>()
      if slice.HasValue then
        slice.Value
        |> CompoundSliceD.FromFB
        |> Option.map CompoundSlice
      else None
    | _ -> None
#endif

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Slice option =
    Binary.createBuffer bytes
    |> SliceFB.GetRootAsSliceFB
    |> Slice.FromFB

#if JAVASCRIPT
#else

  member self.ToYaml(serializer: Serializer) =
    let yaml =
      match self with
      | StringSlice slice ->
        SliceYaml.StringSlice(slice.Index, slice.Value)

      | IntSlice slice ->
        SliceYaml.IntSlice(slice.Index, slice.Value)

      | FloatSlice slice ->
        SliceYaml.FloatSlice(slice.Index, slice.Value)

      | DoubleSlice slice ->
        SliceYaml.DoubleSlice(slice.Index, slice.Value)

      | BoolSlice slice ->
        SliceYaml.BoolSlice(slice.Index, slice.Value)

      | ByteSlice slice ->
        SliceYaml.ByteSlice(slice.Index, Convert.ToBase64String(slice.Value))

      | EnumSlice slice ->
        SliceYaml.EnumSlice(slice.Index, Yaml.toYaml slice.Value)

      | ColorSlice slice ->
        SliceYaml.ColorSlice(slice.Index, Yaml.toYaml slice.Value)

      | CompoundSlice slice ->
        let ioboxes = Array.map Yaml.toYaml slice.Value
        SliceYaml.CompoundSlice(self.Index, ioboxes)

    serializer.Serialize yaml

  static member FromYaml(str: string) =
    let serializer = new Serializer()
    let yaml = serializer.Deserialize<SliceYaml>(str)

    match yaml.SliceType with
    | "StringSlice"   -> yaml.ToStringSliceD()   |> Option.map StringSlice
    | "IntSlice"      -> yaml.ToIntSliceD()      |> Option.map IntSlice
    | "FloatSlice"    -> yaml.ToFloatSliceD()    |> Option.map FloatSlice
    | "DoubleSlice"   -> yaml.ToDoubleSliceD()   |> Option.map DoubleSlice
    | "BoolSlice"     -> yaml.ToBoolSliceD()     |> Option.map BoolSlice
    | "ByteSlice"     -> yaml.ToByteSliceD()     |> Option.map ByteSlice
    | "EnumSlice"     -> yaml.ToEnumSliceD()     |> Option.map EnumSlice
    | "ColorSlice"    -> yaml.ToColorSliceD()    |> Option.map ColorSlice
    | "CompoundSlice" -> yaml.ToCompoundSliceD() |> Option.map CompoundSlice
    | _               -> None

#endif

//  ____  _ _
// / ___|| (_) ___ ___  ___
// \___ \| | |/ __/ _ \/ __|
//  ___) | | | (_|  __/\__ \
// |____/|_|_|\___\___||___/

and Slices =
  | StringSlices   of StringSliceD   array
  | IntSlices      of IntSliceD      array
  | FloatSlices    of FloatSliceD    array
  | DoubleSlices   of DoubleSliceD   array
  | BoolSlices     of BoolSliceD     array
  | ByteSlices     of ByteSliceD     array
  | EnumSlices     of EnumSliceD     array
  | ColorSlices    of ColorSliceD    array
  | CompoundSlices of CompoundSliceD array

  with
    member self.IsString
      with get () =
        match self with
        | StringSlices _ -> true
        |              _ -> false

    member self.IsInt
      with get () =
        match self with
        | IntSlices _ -> true
        |           _ -> false

    member self.IsFloat
      with get () =
        match self with
        | FloatSlices _ -> true
        |           _ -> false

    member self.IsDouble
      with get () =
        match self with
        | DoubleSlices _ -> true
        |              _ -> false

    member self.IsBool
      with get () =
        match self with
        | BoolSlices _ -> true
        |            _ -> false

    member self.IsByte
      with get () =
        match self with
        | ByteSlices _ -> true
        |            _ -> false

    member self.IsEnum
      with get () =
        match self with
        | EnumSlices _ -> true
        |            _ -> false

    member self.IsColor
      with get () =
        match self with
        | ColorSlices _ -> true
        |             _ -> false

    member self.IsCompound
      with get () =
        match self with
        | CompoundSlices _ -> true
        |                _ -> false

    //  ___ _
    // |_ _| |_ ___ _ __ ___
    //  | || __/ _ \ '_ ` _ \
    //  | || ||  __/ | | | | |
    // |___|\__\___|_| |_| |_|

    member self.Item (idx: int) =
      match self with
      | StringSlices    arr -> StringSlice   arr.[idx]
      | IntSlices       arr -> IntSlice      arr.[idx]
      | FloatSlices     arr -> FloatSlice    arr.[idx]
      | DoubleSlices    arr -> DoubleSlice   arr.[idx]
      | BoolSlices      arr -> BoolSlice     arr.[idx]
      | ByteSlices      arr -> ByteSlice     arr.[idx]
      | EnumSlices      arr -> EnumSlice     arr.[idx]
      | ColorSlices     arr -> ColorSlice    arr.[idx]
      | CompoundSlices  arr -> CompoundSlice arr.[idx]

    member self.At (idx: int) = self.Item idx

    //  __  __
    // |  \/  | __ _ _ __
    // | |\/| |/ _` | '_ \
    // | |  | | (_| | |_) |
    // |_|  |_|\__,_| .__/
    //              |_|

    member self.Map (f: Slice -> 'a) : 'a array =
      match self with
      | StringSlices    arr -> Array.map (StringSlice   >> f) arr
      | IntSlices       arr -> Array.map (IntSlice      >> f) arr
      | FloatSlices     arr -> Array.map (FloatSlice    >> f) arr
      | DoubleSlices    arr -> Array.map (DoubleSlice   >> f) arr
      | BoolSlices      arr -> Array.map (BoolSlice     >> f) arr
      | ByteSlices      arr -> Array.map (ByteSlice     >> f) arr
      | EnumSlices      arr -> Array.map (EnumSlice     >> f) arr
      | ColorSlices     arr -> Array.map (ColorSlice    >> f) arr
      | CompoundSlices  arr -> Array.map (CompoundSlice >> f) arr

    //  _   _      _
    // | | | | ___| |_ __   ___ _ __ ___
    // | |_| |/ _ \ | '_ \ / _ \ '__/ __|
    // |  _  |  __/ | |_) |  __/ |  \__ \
    // |_| |_|\___|_| .__/ \___|_|  |___/
    //              |_|

    member __.CreateString (idx: Index) (value: string) =
      StringSlice { Index = idx; Value = value }

    member __.CreateInt (idx: Index) (value: int) =
      IntSlice { Index = idx; Value = value }

    member __.CreateFloat (idx: Index) (value: float) =
      FloatSlice { Index = idx; Value = value }

    member __.CreateDouble (idx: Index) (value: double) =
      DoubleSlice { Index = idx; Value = value }

    member __.CreateBool (idx: Index) (value: bool) =
      BoolSlice { Index = idx; Value = value }

    member __.CreateByte (idx: Index) (value: Binary.Buffer) =
      ByteSlice { Index = idx; Value = value }

    member __.CreateEnum (idx: Index) (value: Property) =
      EnumSlice { Index = idx; Value = value }

    member __.CreateColor (idx: Index) (value: ColorSpace) =
      ColorSlice { Index = idx; Value = value }

    member __.CreateCompound (idx: Index) (value: IOBox array) =
      CompoundSlice { Index = idx; Value = value }

//  ____  _     _____
// |  _ \(_)_ _|_   _|   _ _ __   ___
// | |_) | | '_ \| || | | | '_ \ / _ \
// |  __/| | | | | || |_| | |_) |  __/
// |_|   |_|_| |_|_| \__, | .__/ \___| 4 IOBox Plugins
//                   |___/|_|

#if JAVASCRIPT

[<StringEnum>]
type PinType =
  | [<CompiledName("ValuePin")>]    ValuePin
  | [<CompiledName("StringPin")>]   StringPin
  | [<CompiledName("ColorPin")>]    ColorPin
  | [<CompiledName("EnumPin")>]     EnumPin
  | [<CompiledName("NodePin")>]     NodePin
  | [<CompiledName("BytePin")>]     BytePin
  | [<CompiledName("CompoundPin")>] CompoundPin

//  _   _ _   _ _
// | | | | |_(_) |___
// | | | | __| | / __|
// | |_| | |_| | \__ \
//  \___/ \__|_|_|___/

[<AutoOpen>]
module IOBoxUtils =

  let getType = function
    | IntBox _ | FloatBox  _ | DoubleBox _ | BoolBox _ -> ValuePin
    | StringBox                                      _ -> StringPin
    | ByteBox                                        _ -> BytePin
    | EnumBox                                        _ -> EnumPin
    | ColorBox                                       _ -> ColorPin
    | Compound                                       _ -> CompoundPin

#endif
