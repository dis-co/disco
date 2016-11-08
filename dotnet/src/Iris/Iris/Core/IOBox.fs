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
    | "toggle" -> Right Toggle
    | "bang"   -> Right Bang
    | _  ->
      sprintf "Invalid Behavior value: %s" str
      |> ParseError
      |> Either.fail

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
    | x when x = BehaviorFB.ToggleFB -> Right Toggle
    | x when x = BehaviorFB.BangFB   -> Right Bang
    | x ->
      sprintf "Could not parse Behavior: %A" x
      |> ParseError
      |> Either.fail

#else

  static member FromFB (fb: BehaviorFB) =
    match fb with
    | BehaviorFB.ToggleFB -> Right Toggle
    | BehaviorFB.BangFB   -> Right Bang
    | x  ->
      sprintf "Could not parse Behavior: %A" x
      |> ParseError
      |> Either.fail

#endif

  member self.ToOffset(_: FlatBufferBuilder) : BehaviorFB =
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
    | "simple"    -> Right Simple
    | "multiline" -> Right MultiLine
    | "filename"  -> Right FileName
    | "directory" -> Right Directory
    | "url"       -> Right Url
    | "ip"        -> Right IP
    | _ ->
      sprintf "Invalid StringType value: %s" str
      |> ParseError
      |> Either.fail

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
    | x when x = StringTypeFB.SimpleFB    -> Right Simple
    | x when x = StringTypeFB.MultiLineFB -> Right MultiLine
    | x when x = StringTypeFB.FileNameFB  -> Right FileName
    | x when x = StringTypeFB.DirectoryFB -> Right Directory
    | x when x = StringTypeFB.UrlFB       -> Right Url
    | x when x = StringTypeFB.IPFB        -> Right IP
    | x ->
      sprintf "Cannot parse StringType. Unknown type: %A" x
      |> ParseError
      |> Either.fail

#else

    match fb with
    | StringTypeFB.SimpleFB    -> Right Simple
    | StringTypeFB.MultiLineFB -> Right MultiLine
    | StringTypeFB.FileNameFB  -> Right FileName
    | StringTypeFB.DirectoryFB -> Right Directory
    | StringTypeFB.UrlFB       -> Right Url
    | StringTypeFB.IPFB        -> Right IP
    | x ->
      sprintf "Cannot parse StringType. Unknown type: %A" x
      |> ParseError
      |> Either.fail

#endif

  member self.ToOffset(_: FlatBufferBuilder) : StringTypeFB =
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

  member self.ToStringSliceD() : Either<IrisError,StringSliceD> =
    Either.tryWith ParseError "StringSlice" <| fun _ ->
      StringSliceD.Create
        self.Index
        (string self.Value)

  member self.ToIntSliceD() : Either<IrisError,IntSliceD> =
    Either.tryWith ParseError "IntSlice" <| fun _ ->
      IntSliceD.Create
        self.Index
        (Int32.Parse (string self.Value))

  member self.ToFloatSliceD() : Either<IrisError, FloatSliceD> =
    Either.tryWith ParseError "FloatSlice" <| fun _ ->
      FloatSliceD.Create
        self.Index
        (Double.Parse (string self.Value))

  member self.ToDoubleSliceD() : Either<IrisError, DoubleSliceD> =
    Either.tryWith ParseError "DoubleSlice" <| fun _ ->
      DoubleSliceD.Create
        self.Index
        (Double.Parse (string self.Value))

  member self.ToBoolSliceD() : Either<IrisError, BoolSliceD> =
    Either.tryWith ParseError "BoolSlice" <| fun _ ->
      BoolSliceD.Create
        self.Index
        (Boolean.Parse (string self.Value))

  member self.ToByteSliceD() : Either<IrisError, ByteSliceD> =
    Either.tryWith ParseError "ByteSlice" <| fun _ ->
      ByteSliceD.Create
        self.Index
        (Convert.FromBase64String(string self.Value))

  member self.ToEnumSliceD() : Either<IrisError,EnumSliceD> =
    Either.tryWith ParseError "EnumSlice" <| fun _ ->
      let pyml = self.Value :?> PropertyYaml
      { Key = pyml.Key; Value = pyml.Value }
      |> EnumSliceD.Create self.Index

  member self.ToColorSliceD() : Either<IrisError,ColorSliceD> =
    Either.tryWith ParseError "ColorSlice" <| fun _ ->
      match Yaml.fromYaml(self.Value :?> ColorYaml) with
      | Right color ->
        ColorSliceD.Create self.Index color
      | Left (ParseError error) ->
        failwith error
      | other ->
        failwithf "Encountered unexpected error: %A" other

  member self.ToCompoundSliceD() : Either<IrisError,CompoundSliceD>  =
    Either.tryWith ParseError "CompoundSlice" <| fun _ ->
      let n = (self.Value :?> IOBoxYaml array).Length
      let ioboxes =
        Array.fold
          (fun (m: Either<IrisError,int * IOBox array>) box -> either {
              let! inner = m
              let! iobox = Yaml.fromYaml box
              (snd inner).[fst inner] <- iobox
              return (fst inner + 1, snd inner)
            })
          (Right (0, Array.zeroCreate n))
          (self.Value :?> IOBoxYaml array)
      match ioboxes with
      | Right (_, boxes) ->
        CompoundSliceD.Create self.Index boxes
      | Left (ParseError error) ->
        failwith error
      | error ->
        failwithf "Encountered unexpected error: %A" error

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
        /// in JavaScript an array> will re-allocate automatically under the hood
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

  static member FromFB(fb: IOBoxFB) : Either<IrisError,IOBox> =
#if JAVASCRIPT
    match fb.IOBoxType with
    | x when x = IOBoxTypeFB.StringBoxFB ->
      StringBoxFB.Create()
      |> fb.IOBox
      |> StringBoxD.FromFB
      |> Either.map StringBox

    | x when x = IOBoxTypeFB.IntBoxFB ->
      IntBoxFB.Create()
      |> fb.IOBox
      |> IntBoxD.FromFB
      |> Either.map IntBox

    | x when x = IOBoxTypeFB.FloatBoxFB ->
      FloatBoxFB.Create()
      |> fb.IOBox
      |> FloatBoxD.FromFB
      |> Either.map FloatBox

    | x when x = IOBoxTypeFB.DoubleBoxFB ->
      DoubleBoxFB.Create()
      |> fb.IOBox
      |> DoubleBoxD.FromFB
      |> Either.map DoubleBox

    | x when x = IOBoxTypeFB.BoolBoxFB ->
      BoolBoxFB.Create()
      |> fb.IOBox
      |> BoolBoxD.FromFB
      |> Either.map BoolBox

    | x when x = IOBoxTypeFB.ByteBoxFB ->
      ByteBoxFB.Create()
      |> fb.IOBox
      |> ByteBoxD.FromFB
      |> Either.map ByteBox

    | x when x = IOBoxTypeFB.EnumBoxFB ->
      EnumBoxFB.Create()
      |> fb.IOBox
      |> EnumBoxD.FromFB
      |> Either.map EnumBox

    | x when x = IOBoxTypeFB.ColorBoxFB ->
      ColorBoxFB.Create()
      |> fb.IOBox
      |> ColorBoxD.FromFB
      |> Either.map ColorBox

    | x when x = IOBoxTypeFB.CompoundBoxFB ->
      CompoundBoxFB.Create()
      |> fb.IOBox
      |> CompoundBoxD.FromFB
      |> Either.map Compound

    | x ->
      sprintf "%A is not a valid IOBoxTypeFB" x
      |> ParseError
      |> Either.fail

#else

    match fb.IOBoxType with
    | IOBoxTypeFB.StringBoxFB ->
      let v = fb.IOBox<StringBoxFB>()
      if v.HasValue then
        v.Value
        |> StringBoxD.FromFB
        |> Either.map StringBox
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | IOBoxTypeFB.IntBoxFB ->
      let v = fb.IOBox<IntBoxFB>()
      if v.HasValue then
        v.Value
        |> IntBoxD.FromFB
        |> Either.map IntBox
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | IOBoxTypeFB.FloatBoxFB ->
      let v = fb.IOBox<FloatBoxFB>()
      if v.HasValue then
        v.Value
        |> FloatBoxD.FromFB
        |> Either.map FloatBox
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | IOBoxTypeFB.DoubleBoxFB ->
      let v = fb.IOBox<DoubleBoxFB>()
      if v.HasValue then
        v.Value
        |> DoubleBoxD.FromFB
        |> Either.map DoubleBox
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | IOBoxTypeFB.BoolBoxFB ->
      let v = fb.IOBox<BoolBoxFB>()
      if v.HasValue then
        v.Value
        |> BoolBoxD.FromFB
        |> Either.map BoolBox
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | IOBoxTypeFB.ByteBoxFB ->
      let v = fb.IOBox<ByteBoxFB>()
      if v.HasValue then
        v.Value
        |> ByteBoxD.FromFB
        |> Either.map ByteBox
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | IOBoxTypeFB.EnumBoxFB ->
      let v = fb.IOBox<EnumBoxFB>()
      if v.HasValue then
        v.Value
        |> EnumBoxD.FromFB
        |> Either.map EnumBox
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | IOBoxTypeFB.ColorBoxFB ->
      let v = fb.IOBox<ColorBoxFB>()
      if v.HasValue then
        v.Value
        |> ColorBoxD.FromFB
        |> Either.map ColorBox
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | IOBoxTypeFB.CompoundBoxFB ->
      let v = fb.IOBox<CompoundBoxFB>()
      if v.HasValue then
        v.Value
        |> CompoundBoxD.FromFB
        |> Either.map Compound
      else
        "IOBoxFB has no value"
        |> ParseError
        |> Either.fail

    | x ->
      sprintf "%A is not a valid IOBoxTypeFB" x
      |> ParseError
      |> Either.fail

#endif

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,IOBox> =
    Binary.createBuffer bytes
    |> IOBoxFB.GetRootAsIOBoxFB
    |> IOBox.FromFB

// __   __              _
// \ \ / /_ _ _ __ ___ | |
//  \ V / _` | '_ ` _ \| |
//   | | (_| | | | | | | |
//   |_|\__,_|_| |_| |_|_|

#if !JAVASCRIPT
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


  /// ## Parse all SliceYamls for a given IOBox data type
  ///
  /// Takes an array> of SliceYaml and folds over it, parsing the
  /// slices. If an error occurs, it will be returned in the left-hand
  /// side.
  ///
  /// ### Signature:
  /// - slices: SliceYaml array>
  ///
  /// Returns: Either<IrisError,^a>
  static member inline ParseSliceYamls< ^t when ^t : (static member FromYamlObject : SliceYaml -> Either<IrisError, ^t>)>
                                           (slices: SliceYaml array)
                                           : Either<IrisError, ^t array> =
    Array.fold
      (fun (m: Either<IrisError,int * ^t array>) yml -> either {
        let! arr = m
        let! parsed = Yaml.fromYaml yml
        (snd arr).[fst arr] <- parsed
        return (fst arr + 1, snd arr)
      })
      (Right (0, Array.zeroCreate slices.Length))
      slices
    |> Either.map snd

#endif

  /// ## Parse all tags in a Flatbuffer-serialized type
  ///
  /// Parses all tags in a given IOBox inner data type.
  ///
  /// ### Signature:
  /// - fb: the inner IOBox data type (BoolBoxD, StringBoxD, etc.)
  ///
  /// Returns: Either<IrisError, Tag array>
  static member inline ParseTagsFB< ^a when ^a : (member TagsLength : int)
                                       and  ^a : (member Tags : int -> Tag)>
                                       (fb: ^a)
                                       : Either<IrisError, Tag array> =
    let len = (^a : (member TagsLength : int) fb)
    let arr = Array.zeroCreate len
    for i = 0 to (arr.Length-1) do
      let x = (^a : (member Tags : int -> Tag) (fb, i))
      arr.[i] <- x
    Either.succeed arr
    //        Array.fold
    //  (fun (result: Either<IrisError,int * Tag array>) _ -> either {
    //      let! (i, tags) = result
    //      tags.[i] <- (^a : (member Tags : int -> Tag) (fb, i))
    //      return (i + 1, tags)
    //    })
    //  (Right (0, arr))
    //  arr
    //|> Either.map snd


#if JAVASCRIPT

  static member inline ParseSlicesFB< ^a, ^b, ^t when ^t : (static member FromFB : ^a -> Either<IrisError, ^t>)
                                                 and ^b : (member SlicesLength : int)
                                                 and ^b : (member Slices : int -> ^a)>
                                                 (fb: ^b)
                                                 : Either<IrisError, ^t array> =
    let len = (^b : (member SlicesLength : int) fb)
    let arr = Array.zeroCreate len
    for i = 0 to (arr.Length-1) do
      let value = (^b : (member Slices : int -> ^a) (fb, i))
      let slice = (^t : (static member FromFB : ^a -> Either<IrisError, ^t>) value)
      arr.[i] <- Either.get slice
    Either.succeed arr
    //Array.fold
    //  (fun (result: Either<IrisError,int * ^t array>) _ -> either {

    //      let! (i, slices) = result

    //      // In Javascript, Flatbuffer types are not modeled as nullables,
    //      // hence parsing code is much simpler
    //      let! slice =
    //        let value = (^b : (member Slices : int -> ^a) (fb, i))
    //        (^t : (static member FromFB : ^a -> Either<IrisError, ^t>) value)

    //      // add the slice to the array> at its correct position
    //      slices.[i] <- slice
    //      return (i + 1, slices)
    //  })
    //  (Right (0, arr))
    //  arr
    //|> Either.map snd

#else

  static member inline ParseSlicesFB< ^a, ^b, ^t when ^t : (static member FromFB : ^a -> Either<IrisError, ^t>)
                                                 and ^b : (member SlicesLength : int)
                                                 and ^b : (member Slices : int -> Nullable< ^a >)>
                                                 (fb: ^b)
                                                 : Either<IrisError, ^t array> =
    let len = (^b : (member SlicesLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<IrisError,int * ^t array>) _ -> either {
          let! (i, slices) = result

          // In .NET, Flatbuffers are modelled with nullables, hence
          // parsing is slightly more elaborate
          let! slice =
            let value = (^b : (member Slices : int -> Nullable< ^a >) (fb, i))
            if value.HasValue then
              (^t : (static member FromFB : ^a -> Either<IrisError, ^t>) value.Value)
            else
              "Could not parse empty slice"
              |> ParseError
              |> Either.fail

          // add the slice to the array> at its correct position
          slices.[i] <- slice
          return (i + 1, slices)
      })
      (Right (0, arr))
      arr
    |> Either.map snd

#endif

#if !JAVASCRIPT
  static member FromYamlObject(yml: IOBoxYaml) =
    try
      match yml.BoxType with
      | "StringBox" -> either {
          let! strtype = StringType.TryParse yml.StringType
          let! slices  = IOBox.ParseSliceYamls yml.Slices

          return StringBox {
            Id         = Id yml.Id
            Name       = yml.Name
            Patch      = Id yml.Patch
            Tags       = yml.Tags
            FileMask   = if isNull yml.FileMask then None else Some yml.FileMask
            MaxChars   = yml.MaxChars
            StringType = strtype
            Slices     = slices
          }
        }

      | "IntBox" -> either {
          let! slices = IOBox.ParseSliceYamls yml.Slices

          return IntBox {
            Id       = Id yml.Id
            Name     = yml.Name
            Patch    = Id yml.Patch
            Tags     = yml.Tags
            VecSize  = yml.VecSize
            Min      = yml.Min
            Max      = yml.Max
            Unit     = yml.Unit
            Slices   = slices
          }
        }

      | "FloatBox" -> either {
          let! slices = IOBox.ParseSliceYamls yml.Slices

          return FloatBox {
            Id        = Id yml.Id
            Name      = yml.Name
            Patch     = Id yml.Patch
            Tags      = yml.Tags
            VecSize   = yml.VecSize
            Min       = yml.Min
            Max       = yml.Max
            Unit      = yml.Unit
            Precision = yml.Precision
            Slices    = slices
          }
        }

      | "DoubleBox" -> either {
          let! slices = IOBox.ParseSliceYamls yml.Slices
          return DoubleBox {
            Id        = Id yml.Id
            Name      = yml.Name
            Patch     = Id yml.Patch
            Tags      = yml.Tags
            VecSize   = yml.VecSize
            Min       = yml.Min
            Max       = yml.Max
            Unit      = yml.Unit
            Precision = yml.Precision
            Slices    = slices
          }
        }

      | "BoolBox" -> either {
          let! behavior = Behavior.TryParse yml.Behavior
          let! slices = IOBox.ParseSliceYamls yml.Slices
          return BoolBox {
            Id       = Id yml.Id
            Name     = yml.Name
            Patch    = Id yml.Patch
            Tags     = yml.Tags
            Behavior = behavior
            Slices   = slices
          }
        }

      | "ByteBox" -> either {
          let! slices = IOBox.ParseSliceYamls yml.Slices
          return ByteBox {
            Id     = Id yml.Id
            Name   = yml.Name
            Patch  = Id yml.Patch
            Tags   = yml.Tags
            Slices = slices
          }
        }

      | "EnumBox" -> either {
          let! properties =
            Array.fold
              (fun (m: Either<IrisError, int * Property array>) yml ->
                either {
                  let! state = m
                  let! parsed = Yaml.fromYaml yml
                  (snd state).[fst state] <- parsed
                  return (fst state + 1, snd state)
                })
              (Right (0, Array.zeroCreate yml.Properties.Length))
              yml.Properties
            |> Either.map snd

          let! slices = IOBox.ParseSliceYamls yml.Slices

          return EnumBox {
            Id         = Id yml.Id
            Name       = yml.Name
            Patch      = Id yml.Patch
            Tags       = yml.Tags
            Properties = properties
            Slices     = slices
          }
        }

      | "ColorBox" -> either {
          let! slices = IOBox.ParseSliceYamls yml.Slices
          return ColorBox {
            Id     = Id yml.Id
            Name   = yml.Name
            Patch  = Id yml.Patch
            Tags   = yml.Tags
            Slices = slices
          }
        }

      | "Compound" -> either {
          let! slices = IOBox.ParseSliceYamls yml.Slices
          return Compound {
            Id     = Id yml.Id
            Name   = yml.Name
            Patch  = Id yml.Patch
            Tags   = yml.Tags
            Slices = slices
          }
        }

      | x ->
        sprintf "Could not parse IOBoxYml type: %s" x
        |> ParseError
        |> Either.fail

    with
      | exn ->
        sprintf "Could not parse IOBoxYml: %s" exn.Message
        |> ParseError
        |> Either.fail

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

  static member FromFB(fb: BoolBoxFB) : Either<IrisError,BoolBoxD> =
    either {
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb
      let! behavior = Behavior.FromFB fb.Behavior

      return { Id         = Id fb.Id
               Name       = fb.Name
               Patch      = Id fb.Patch
               Tags       = tags
               Behavior   = behavior
               Slices     = slices }
    }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,BoolBoxD> =
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

  static member Create (idx: Index) (value: bool) =
    { Index = idx
      Value = value }

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

  static member FromFB(fb: BoolSliceFB) : Either<IrisError,BoolSliceD> =
    Either.tryWith ParseError "BoolSlice" <| fun _ ->
      { Index = fb.Index; Value = fb.Value }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,BoolSliceD> =
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
    | x ->
      sprintf "Could not parse SliceType: %s" x
      |> ParseError
      |> Either.fail

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

  static member FromFB(fb: IntBoxFB) : Either<IrisError,IntBoxD> =
    either {
      let unit = if isNull fb.Unit then "" else fb.Unit
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb

      return { Id      = Id fb.Id
               Name    = fb.Name
               Patch   = Id fb.Patch
               Tags    = tags
               VecSize = fb.VecSize
               Min     = fb.Min
               Max     = fb.Max
               Unit    = unit
               Slices  = slices }
    }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,IntBoxD> =
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

  static member Create (idx: Index) (value: int) =
    { Index = idx
      Value = value }

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

  static member FromFB(fb: IntSliceFB) : Either<IrisError,IntSliceD> =
    Either.tryWith ParseError "IntSliceFB" <| fun _ ->
      { Index = fb.Index
        Value = fb.Value }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,IntSliceD> =
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
    | x ->
      sprintf "Could not parse %s as InSlice" x
      |> ParseError
      |> Either.fail

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

  static member FromFB(fb: FloatBoxFB) : Either<IrisError,FloatBoxD> =
    either {
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb
      let unit = if isNull fb.Unit then "" else fb.Unit

      return { Id         = Id fb.Id
               Name       = fb.Name
               Patch      = Id fb.Patch
               Tags       = tags
               VecSize    = fb.VecSize
               Min        = fb.Min
               Max        = fb.Max
               Unit       = unit
               Precision  = fb.Precision
               Slices     = slices }
    }


  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,FloatBoxD> =
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

  static member Create (idx: Index) (value: float) =
    { Index = idx
      Value = value }

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

  static member FromFB(fb: FloatSliceFB) : Either<IrisError,FloatSliceD> =
    Either.tryWith ParseError "FloatSliceFB" <| fun _ ->
      { Index = fb.Index
        Value = float fb.Value }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,FloatSliceD> =
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
    | x ->
      sprintf "Cannot parse %s as FloatSlice" x
      |> ParseError
      |> Either.fail

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

  static member FromFB(fb: DoubleBoxFB) : Either<IrisError,DoubleBoxD> =
    either {
      let unit = if isNull fb.Unit then "" else fb.Unit
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb

      return { Id        = Id fb.Id
               Name      = fb.Name
               Patch     = Id fb.Patch
               Tags      = tags
               VecSize   = fb.VecSize
               Min       = fb.Min
               Max       = fb.Max
               Unit      = unit
               Precision = fb.Precision
               Slices    = slices }
    }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,DoubleBoxD> =
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

  static member Create (idx: Index) (value: double) =
    { Index = idx
      Value = value }

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

  static member FromFB(fb: DoubleSliceFB) : Either<IrisError,DoubleSliceD> =
    Either.tryWith ParseError "DoubleSliceD" <| fun _ ->
      { Index = fb.Index
        Value = fb.Value }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,DoubleSliceD> =
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
    | x ->
      sprintf "Could not parse %s as DoubleSliceD" x
      |> ParseError
      |> Either.fail

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

  static member FromFB(fb: ByteBoxFB) : Either<IrisError,ByteBoxD> =
    either {
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb

      return { Id         = Id fb.Id
               Name       = fb.Name
               Patch      = Id fb.Patch
               Tags       = tags
               Slices     = slices }
    }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,ByteBoxD> =
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

  static member Create (idx: Index) (value: Binary.Buffer) =
    { Index = idx
      Value = value }

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
    | x ->
      sprintf "Cannot parse %s as ByteSliceD" x
      |> ParseError
      |> Either.fail

#endif

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let encode (bytes: Binary.Buffer) =
#if JAVASCRIPT
      let mutable str = ""
      let arr = Fable.Import.JS.Uint8Array.Create(bytes)
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

  static member FromFB(fb: ByteSliceFB) : Either<IrisError,ByteSliceD> =
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

    Either.tryWith ParseError "ByteSliceD" <| fun _ ->
      { Index = fb.Index
        Value = decode fb.Value }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,ByteSliceD> =
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

  static member FromFB(fb: EnumBoxFB) : Either<IrisError,EnumBoxD> =
    either {
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb

      let! properties =
        let properties = Array.zeroCreate fb.PropertiesLength
        Array.fold
          (fun (m: Either<IrisError, int * Property array>) _ -> either {
            let! (i, arr) = m
#if JAVASCRIPT
            let prop = fb.Properties(i)
#else
            let! prop =
              let nullable = fb.Properties(i)
              if nullable.HasValue then
                Either.succeed nullable.Value
              else
                "Cannot parse empty property"
                |> ParseError
                |> Either.fail
#endif
            arr.[i] <- { Key = prop.Key; Value = prop.Value }
            return (i + 1, arr)
          })
          (Right (0, properties))
          properties
        |> Either.map snd

      return { Id         = Id fb.Id
               Name       = fb.Name
               Patch      = Id fb.Patch
               Tags       = tags
               Properties = properties
               Slices     = slices }
    }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromEnums(bytes: Binary.Buffer) : Either<IrisError,EnumBoxD> =
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

  static member Create (idx: Index) (value: Property) =
    { Index = idx
      Value = value }

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

  static member FromFB(fb: EnumSliceFB) : Either<IrisError,EnumSliceD> =
    Either.tryWith ParseError "EnumSliceD" <| fun _ ->
#if JAVASCRIPT
      let prop = fb.Value
      { Index = fb.Index
        Value = { Key = prop.Key; Value = prop.Value } }
#else
      let nullable = fb.Value
      if nullable.HasValue then
        let prop = nullable.Value
        { Index = fb.Index
          Value = { Key = prop.Key; Value = prop.Value } }
      else
        failwith "Cannot parse empty property value"
#endif

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromEnums(bytes: Binary.Buffer) : Either<IrisError,EnumSliceD> =
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
    | x ->
      sprintf "Cannot parse %s as EnumSlice" x
      |> ParseError
      |> Either.fail

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

  static member FromFB(fb: ColorBoxFB) : Either<IrisError,ColorBoxD> =
    either {
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb
      return { Id     = Id fb.Id
               Name   = fb.Name
               Patch  = Id fb.Patch
               Tags   = tags
               Slices = slices }
    }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromColors(bytes: Binary.Buffer) : Either<IrisError,ColorBoxD> =
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

  static member Create (idx: Index) (value: ColorSpace) =
    { Index = idx
      Value = value }

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

  static member FromFB(fb: ColorSliceFB) : Either<IrisError,ColorSliceD> =
    Either.tryWith ParseError "ColorSliceD" <| fun _ ->
#if JAVASCRIPT
      match fb.Value |> ColorSpace.FromFB with
      | Right color             -> { Index = fb.Index; Value = color }
      | Left (ParseError error) -> failwith error
      | Left error ->
        failwithf "Unexpected error: %A" error
#else
      let nullable = fb.Value
      if nullable.HasValue then
        match ColorSpace.FromFB nullable.Value with
        | Right color             -> { Index = fb.Index; Value = color }
        | Left (ParseError error) -> failwith error
        | Left error ->
          failwithf "Unexpected error: %A" error
      else
        failwith "Cannot parse empty ColorSpaceFB"
#endif

  member self.ToColors() : Binary.Buffer = Binary.buildBuffer self

  static member FromColors(bytes: Binary.Buffer) : Either<IrisError,ColorSliceD> =
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
    | x ->
      sprintf "Cannot parse %s as ColorSlice" x
      |> ParseError
      |> Either.fail

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

  static member FromFB(fb: StringBoxFB) : Either<IrisError,StringBoxD> =
    either {
      let mask = if isNull fb.FileMask then None else Some fb.FileMask
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb
      let! tipe = StringType.FromFB fb.StringType

      return { Id         = Id fb.Id
               Name       = fb.Name
               Patch      = Id fb.Patch
               Tags       = tags
               StringType = tipe
               FileMask   = mask
               MaxChars   = fb.MaxChars
               Slices     = slices }
    }


  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromStrings(bytes: Binary.Buffer) : Either<IrisError,StringBoxD> =
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

  static member Create (idx: Index) (value: string) =
    { Index = idx
      Value = value }

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

  static member FromFB(fb: StringSliceFB) : Either<IrisError,StringSliceD> =
    Either.tryWith ParseError "StringSliceD" <| fun _ ->
      { Index = fb.Index
        Value = fb.Value }

  member self.ToStrings() : Binary.Buffer = Binary.buildBuffer self

  static member FromStrings(bytes: Binary.Buffer) : Either<IrisError,StringSliceD> =
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
    | x ->
      sprintf "Cannot parse %s as StringSlice" x
      |> ParseError
      |> Either.fail

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

  static member FromFB(fb: CompoundBoxFB) : Either<IrisError,CompoundBoxD> =
    either {
      let! tags = IOBox.ParseTagsFB fb
      let! slices = IOBox.ParseSlicesFB fb

      return { Id     = Id fb.Id
               Name   = fb.Name
               Patch  = Id fb.Patch
               Tags   = tags
               Slices = slices }
    }

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromCompounds(bytes: Binary.Buffer) : Either<IrisError,CompoundBoxD> =
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

  static member Create (idx: Index) (value: IOBox array) =
    { Index = idx
      Value = value }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let ioboxoffsets = Array.map (Binary.toOffset builder) self.Value
    let ioboxes = CompoundSliceFB.CreateValueVector(builder, ioboxoffsets)
    CompoundSliceFB.StartCompoundSliceFB(builder)
    CompoundSliceFB.AddIndex(builder, self.Index)
    CompoundSliceFB.AddValue(builder, ioboxes)
    CompoundSliceFB.EndCompoundSliceFB(builder)

  static member FromFB(fb: CompoundSliceFB) : Either<IrisError,CompoundSliceD> =
    either {
      let! ioboxes =
        let arr = Array.zeroCreate fb.ValueLength
        Array.fold
          (fun (m: Either<IrisError,int * IOBox array>) _ -> either {
              let! (i, arr) = m

  #if JAVASCRIPT
              let! iobox = i |> fb.Value |> IOBox.FromFB
  #else
              let! iobox =
                let nullable = fb.Value(i)
                if nullable.HasValue then
                  nullable.Value
                  |> IOBox.FromFB
                else
                  "Could not parse empty IOBoxFB"
                  |> ParseError
                  |> Either.fail
  #endif

              arr.[i] <- iobox
              return (i + 1, arr)
            })
          (Right (0, arr))
          arr
        |> Either.map snd

      return { Index = fb.Index
               Value = ioboxes }
    }

  member self.ToCompounds() : Binary.Buffer = Binary.buildBuffer self

  static member FromCompounds(bytes: Binary.Buffer) : Either<IrisError,CompoundSliceD> =
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
    | x ->
      sprintf "Could not parse %s as CompoundSlice" x
      |> ParseError
      |> Either.fail

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

  static member FromFB(fb: SliceFB) : Either<IrisError,Slice>  =
    match fb.SliceType with
#if JAVASCRIPT
    | x when x = SliceTypeFB.StringSliceFB ->
      StringSliceFB.Create()
      |> fb.Slice
      |> StringSliceD.FromFB
      |> Either.map StringSlice

    | x when x = SliceTypeFB.IntSliceFB ->
      IntSliceFB.Create()
      |> fb.Slice
      |> IntSliceD.FromFB
      |> Either.map IntSlice

    | x when x = SliceTypeFB.FloatSliceFB ->
      FloatSliceFB.Create()
      |> fb.Slice
      |> FloatSliceD.FromFB
      |> Either.map FloatSlice

    | x when x = SliceTypeFB.DoubleSliceFB ->
      DoubleSliceFB.Create()
      |> fb.Slice
      |> DoubleSliceD.FromFB
      |> Either.map DoubleSlice

    | x when x = SliceTypeFB.BoolSliceFB ->
      BoolSliceFB.Create()
      |> fb.Slice
      |> BoolSliceD.FromFB
      |> Either.map BoolSlice

    | x when x = SliceTypeFB.ByteSliceFB ->
      ByteSliceFB.Create()
      |> fb.Slice
      |> ByteSliceD.FromFB
      |> Either.map ByteSlice

    | x when x = SliceTypeFB.EnumSliceFB ->
      EnumSliceFB.Create()
      |> fb.Slice
      |> EnumSliceD.FromFB
      |> Either.map EnumSlice

    | x when x = SliceTypeFB.ColorSliceFB ->
      ColorSliceFB.Create()
      |> fb.Slice
      |> ColorSliceD.FromFB
      |> Either.map ColorSlice

    | x when x = SliceTypeFB.CompoundSliceFB ->
      CompoundSliceFB.Create()
      |> fb.Slice
      |> CompoundSliceD.FromFB
      |> Either.map CompoundSlice

    | x ->
      sprintf "Could not parse slice. Unknown slice type %A" x
      |> ParseError
      |> Either.fail

#else

    | SliceTypeFB.StringSliceFB   ->
      let slice = fb.Slice<StringSliceFB>()
      if slice.HasValue then
        slice.Value
        |> StringSliceD.FromFB
        |> Either.map StringSlice
      else
        "Could not parse StringSlice"
        |> ParseError
        |> Either.fail

    | SliceTypeFB.IntSliceFB      ->
      let slice = fb.Slice<IntSliceFB>()
      if slice.HasValue then
        slice.Value
        |> IntSliceD.FromFB
        |> Either.map IntSlice
      else
        "Could not parse IntSlice"
        |> ParseError
        |> Either.fail

    | SliceTypeFB.FloatSliceFB    ->
      let slice = fb.Slice<FloatSliceFB>()
      if slice.HasValue then
        slice.Value
        |> FloatSliceD.FromFB
        |> Either.map FloatSlice
      else
        "Could not parse FloatSlice"
        |> ParseError
        |> Either.fail

    | SliceTypeFB.DoubleSliceFB   ->
      let slice = fb.Slice<DoubleSliceFB>()
      if slice.HasValue then
        slice.Value
        |> DoubleSliceD.FromFB
        |> Either.map DoubleSlice
      else
        "Could not parse DoubleSlice"
        |> ParseError
        |> Either.fail

    | SliceTypeFB.BoolSliceFB     ->
      let slice = fb.Slice<BoolSliceFB>()
      if slice.HasValue then
        slice.Value
        |> BoolSliceD.FromFB
        |> Either.map BoolSlice
      else
        "Could not parse BoolSlice"
        |> ParseError
        |> Either.fail

    | SliceTypeFB.ByteSliceFB     ->
      let slice = fb.Slice<ByteSliceFB>()
      if slice.HasValue then
        slice.Value
        |> ByteSliceD.FromFB
        |> Either.map ByteSlice
      else
        "Could not parse ByteSlice"
        |> ParseError
        |> Either.fail

    | SliceTypeFB.EnumSliceFB     ->
      let slice = fb.Slice<EnumSliceFB>()
      if slice.HasValue then
        slice.Value
        |> EnumSliceD.FromFB
        |> Either.map EnumSlice
      else
        "Could not parse EnumSlice"
        |> ParseError
        |> Either.fail

    | SliceTypeFB.ColorSliceFB    ->
      let slice = fb.Slice<ColorSliceFB>()
      if slice.HasValue then
        slice.Value
        |> ColorSliceD.FromFB
        |> Either.map ColorSlice
      else
        "Could not parse ColorSlice"
        |> ParseError
        |> Either.fail

    | SliceTypeFB.CompoundSliceFB ->
      let slice = fb.Slice<CompoundSliceFB>()
      if slice.HasValue then
        slice.Value
        |> CompoundSliceD.FromFB
        |> Either.map CompoundSlice
      else
        "Could not parse CompoundSlice"
        |> ParseError
        |> Either.fail

    | x ->
      sprintf "Cannot parse slice. Unknown slice type: %A" x
      |> ParseError
      |> Either.fail

#endif

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,Slice> =
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
    | "StringSlice"   -> yaml.ToStringSliceD()   |> Either.map StringSlice
    | "IntSlice"      -> yaml.ToIntSliceD()      |> Either.map IntSlice
    | "FloatSlice"    -> yaml.ToFloatSliceD()    |> Either.map FloatSlice
    | "DoubleSlice"   -> yaml.ToDoubleSliceD()   |> Either.map DoubleSlice
    | "BoolSlice"     -> yaml.ToBoolSliceD()     |> Either.map BoolSlice
    | "ByteSlice"     -> yaml.ToByteSliceD()     |> Either.map ByteSlice
    | "EnumSlice"     -> yaml.ToEnumSliceD()     |> Either.map EnumSlice
    | "ColorSlice"    -> yaml.ToColorSliceD()    |> Either.map ColorSlice
    | "CompoundSlice" -> yaml.ToCompoundSliceD() |> Either.map CompoundSlice
    | x ->
      sprintf "Cannot parse slice type: %s" x
      |> ParseError
      |> Either.fail

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
