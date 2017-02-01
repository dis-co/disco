namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.Text
open FlatBuffers
open Iris.Serialization
open SharpYaml.Serialization

#endif

// * Behavior

//  ____       _                 _
// | __ )  ___| |__   __ ___   _(_) ___  _ __
// |  _ \ / _ \ '_ \ / _` \ \ / / |/ _ \| '__|
// | |_) |  __/ | | | (_| |\ V /| | (_) | |
// |____/ \___|_| |_|\__,_| \_/ |_|\___/|_|

[<RequireQualifiedAccess>]
type Behavior =
  | Toggle
  | Bang

  // ** TryParse

  static member TryParse (str: string) =
    match String.toLower str with
    | "toggle" -> Right Toggle
    | "bang"   -> Right Bang
    | _  ->
      sprintf "Invalid Behavior value: %s" str
      |> Error.asParseError "Behavior.TryParse"
      |> Either.fail

  // ** ToString

  override self.ToString() =
    match self with
    | Toggle  -> "Toggle"
    | Bang    -> "Bang"


  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  #if FABLE_COMPILER

  static member FromFB (fb: BehaviorFB) =
    match fb with
    | x when x = BehaviorFB.ToggleFB -> Right Toggle
    | x when x = BehaviorFB.BangFB   -> Right Bang
    | x ->
      sprintf "Could not parse Behavior: %A" x
      |> Error.asParseError "Behavior.FromFB"
      |> Either.fail

  #else

  static member FromFB (fb: BehaviorFB) =
    match fb with
    | BehaviorFB.ToggleFB -> Right Toggle
    | BehaviorFB.BangFB   -> Right Bang
    | x  ->
      sprintf "Could not parse Behavior: %A" x
      |> Error.asParseError "Behavior.FromFB"
      |> Either.fail

  #endif

  // ** ToOffset

  member self.ToOffset(_: FlatBufferBuilder) : BehaviorFB =
    match self with
    | Toggle -> BehaviorFB.ToggleFB
    | Bang   -> BehaviorFB.BangFB

// * StringType

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

  // ** TryParse

  static member TryParse (str: string) =
    match String.toLower str with
    | "simple"    -> Right Simple
    | "multiline" -> Right MultiLine
    | "filename"  -> Right FileName
    | "directory" -> Right Directory
    | "url"       -> Right Url
    | "ip"        -> Right IP
    | _ ->
      sprintf "Invalid StringType value: %s" str
      |> Error.asParseError "StringType.TryParse"
      |> Either.fail

  // ** ToString

  override self.ToString() =
    match self with
    | Simple    -> "Simple"
    | MultiLine -> "MultiLine"
    | FileName  -> "FileName"
    | Directory -> "Directory"
    | Url       -> "Url"
    | IP        -> "IP"

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: StringTypeFB) =
    #if FABLE_COMPILER
    match fb with
    | x when x = StringTypeFB.SimpleFB    -> Right Simple
    | x when x = StringTypeFB.MultiLineFB -> Right MultiLine
    | x when x = StringTypeFB.FileNameFB  -> Right FileName
    | x when x = StringTypeFB.DirectoryFB -> Right Directory
    | x when x = StringTypeFB.UrlFB       -> Right Url
    | x when x = StringTypeFB.IPFB        -> Right IP
    | x ->
      sprintf "Cannot parse StringType. Unknown type: %A" x
      |> Error.asParseError "StringType.FromFB"
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
      |> Error.asParseError "StringType.FromFB"
      |> Either.fail

    #endif

  // ** ToOffset

  member self.ToOffset(_: FlatBufferBuilder) : StringTypeFB =
    match self with
    | Simple    -> StringTypeFB.SimpleFB
    | MultiLine -> StringTypeFB.MultiLineFB
    | FileName  -> StringTypeFB.FileNameFB
    | Directory -> StringTypeFB.DirectoryFB
    | Url       -> StringTypeFB.UrlFB
    | IP        -> StringTypeFB.IPFB


// * SliceYaml

#if !FABLE_COMPILER

//  ____  _ _        __   __              _
// / ___|| (_) ___ __\ \ / /_ _ _ __ ___ | |
// \___ \| | |/ __/ _ \ V / _` | '_ ` _ \| |
//  ___) | | | (_|  __/| | (_| | | | | | | |
// |____/|_|_|\___\___||_|\__,_|_| |_| |_|_|

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
    Either.tryWith (Error.asParseError "SliceYaml.ToStringSliceD") <| fun _ ->
      StringSliceD.Create
        self.Index
        (string self.Value)

  member self.ToIntSliceD() : Either<IrisError,IntSliceD> =
    Either.tryWith (Error.asParseError "SliceYaml.ToIntSliceD") <| fun _ ->
      IntSliceD.Create
        self.Index
        (Int32.Parse (string self.Value))

  member self.ToFloatSliceD() : Either<IrisError, FloatSliceD> =
    Either.tryWith (Error.asParseError "SliceYaml.ToFloatSliceD") <| fun _ ->
      FloatSliceD.Create
        self.Index
        (Double.Parse (string self.Value))

  member self.ToDoubleSliceD() : Either<IrisError, DoubleSliceD> =
    Either.tryWith (Error.asParseError "SliceYaml.ToDoubleSliceD") <| fun _ ->
      DoubleSliceD.Create
        self.Index
        (Double.Parse (string self.Value))

  member self.ToBoolSliceD() : Either<IrisError, BoolSliceD> =
    Either.tryWith (Error.asParseError "SliceYaml.ToBoolSliceD") <| fun _ ->
      BoolSliceD.Create
        self.Index
        (Boolean.Parse (string self.Value))

  member self.ToByteSliceD() : Either<IrisError, ByteSliceD> =
    Either.tryWith (Error.asParseError "SliceYaml.ToByteSliceD") <| fun _ ->
      ByteSliceD.Create
        self.Index
        (Convert.FromBase64String(string self.Value))

  member self.ToEnumSliceD() : Either<IrisError,EnumSliceD> =
    Either.tryWith (Error.asParseError "SliceYaml.ToEnumSliceD") <| fun _ ->
      let pyml = self.Value :?> PropertyYaml
      { Key = pyml.Key; Value = pyml.Value }
      |> EnumSliceD.Create self.Index

  member self.ToColorSliceD() : Either<IrisError,ColorSliceD> =
    Either.tryWith (Error.asParseError "SliceYaml.ToColorSliceD") <| fun _ ->
      match Yaml.fromYaml(self.Value :?> ColorYaml) with
      | Right color ->
        ColorSliceD.Create self.Index color
      | Left (ParseError (_,error)) ->
        failwith error                  // this is safe (albeit quirky) because its being caught
                                        // internally
      | other ->
        failwithf "Encountered unexpected error: %A" other // same here.

  member self.ToCompoundSliceD() : Either<IrisError,CompoundSliceD>  =
    Either.tryWith (Error.asParseError "SliceYaml.ToCompoundSliceD") <| fun _ ->
      let n = (self.Value :?> PinYaml array).Length
      let pins =
        Array.fold
          (fun (m: Either<IrisError,int * Pin array>) box -> either {
              let! inner = m
              let! pin = Yaml.fromYaml box
              (snd inner).[fst inner] <- pin
              return (fst inner + 1, snd inner)
            })
          (Right (0, Array.zeroCreate n))
          (self.Value :?> PinYaml array)
      match pins with
      | Right (_, boxes) ->
        CompoundSliceD.Create self.Index boxes
      | Left (ParseError (_,error)) ->
        failwith error
      | error ->
        failwithf "Encountered unexpected error: %A" error

// * PinYaml

and PinYaml() =
  [<DefaultValue>] val mutable PinType    : string
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

// * Pin

and Pin =

#else

type Pin =

#endif
  | StringPin   of StringPinD
  | IntPin      of IntPinD
  | FloatPin    of FloatPinD
  | DoublePin   of DoublePinD
  | BoolPin     of BoolPinD
  | BytePin     of BytePinD
  | EnumPin     of EnumPinD
  | ColorPin    of ColorPinD
  | CompoundPin of CompoundPinD

  // ** Id

  member self.Id
    with get () =
      match self with
      | StringPin   data -> data.Id
      | IntPin      data -> data.Id
      | FloatPin    data -> data.Id
      | DoublePin   data -> data.Id
      | BoolPin     data -> data.Id
      | BytePin     data -> data.Id
      | EnumPin     data -> data.Id
      | ColorPin    data -> data.Id
      | CompoundPin data -> data.Id

  // ** Name

  member self.Name
    with get () =
      match self with
      | StringPin   data -> data.Name
      | IntPin      data -> data.Name
      | FloatPin    data -> data.Name
      | DoublePin   data -> data.Name
      | BoolPin     data -> data.Name
      | BytePin     data -> data.Name
      | EnumPin     data -> data.Name
      | ColorPin    data -> data.Name
      | CompoundPin data -> data.Name

  // ** SetName

  member self.SetName name =
    match self with
    | StringPin   data -> StringPin   { data with Name = name }
    | IntPin      data -> IntPin      { data with Name = name }
    | FloatPin    data -> FloatPin    { data with Name = name }
    | DoublePin   data -> DoublePin   { data with Name = name }
    | BoolPin     data -> BoolPin     { data with Name = name }
    | BytePin     data -> BytePin     { data with Name = name }
    | EnumPin     data -> EnumPin     { data with Name = name }
    | ColorPin    data -> ColorPin    { data with Name = name }
    | CompoundPin data -> CompoundPin { data with Name = name }

  // ** Patch

  member self.Patch
    with get () =
      match self with
      | StringPin   data -> data.Patch
      | IntPin      data -> data.Patch
      | FloatPin    data -> data.Patch
      | DoublePin   data -> data.Patch
      | BoolPin     data -> data.Patch
      | BytePin     data -> data.Patch
      | EnumPin     data -> data.Patch
      | ColorPin    data -> data.Patch
      | CompoundPin data -> data.Patch

  // ** Slices

  member pin.Slices
    with get () =
      match pin with
      | StringPin   data -> StringSlices   (pin.Id, data.Slices)
      | IntPin      data -> IntSlices      (pin.Id, data.Slices)
      | FloatPin    data -> FloatSlices    (pin.Id, data.Slices)
      | DoublePin   data -> DoubleSlices   (pin.Id, data.Slices)
      | BoolPin     data -> BoolSlices     (pin.Id, data.Slices)
      | BytePin     data -> ByteSlices     (pin.Id, data.Slices)
      | EnumPin     data -> EnumSlices     (pin.Id, data.Slices)
      | ColorPin    data -> ColorSlices    (pin.Id, data.Slices)
      | CompoundPin data -> CompoundSlices (pin.Id, data.Slices)

  // ** SetSlice

  //  ____       _   ____  _ _
  // / ___|  ___| |_/ ___|| (_) ___ ___
  // \___ \ / _ \ __\___ \| | |/ __/ _ \
  //  ___) |  __/ |_ ___) | | | (_|  __/
  // |____/ \___|\__|____/|_|_|\___\___|

  member self.SetSlice (value: Slice) =
    let update (arr : 'a array) (data: 'a) =

      if int value.Index > Array.length arr then
        #if FABLE_COMPILER
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
    | StringPin data as current ->
      match value with
        | StringSlice slice     -> StringPin { data with Slices = update data.Slices slice }
        | _                     -> current

    | IntPin data as current    ->
      match value with
        | IntSlice slice        -> IntPin { data with Slices = update data.Slices slice }
        | _                     -> current

    | FloatPin data as current  ->
      match value with
        | FloatSlice slice      -> FloatPin { data with Slices = update data.Slices slice }
        | _                     -> current

    | DoublePin data as current ->
      match value with
        | DoubleSlice slice     -> DoublePin { data with Slices = update data.Slices slice }
        | _                     -> current

    | BoolPin data as current   ->
      match value with
        | BoolSlice slice       -> BoolPin { data with Slices = update data.Slices slice }
        | _                     -> current

    | BytePin data as current   ->
      match value with
        | ByteSlice slice       -> BytePin { data with Slices = update data.Slices slice }
        | _                     -> current

    | EnumPin data as current   ->
      match value with
        | EnumSlice slice       -> EnumPin { data with Slices = update data.Slices slice }
        | _                     -> current

    | ColorPin data as current  ->
      match value with
        | ColorSlice slice      -> ColorPin { data with Slices = update data.Slices slice }
        | _                     -> current

    | CompoundPin data as current  ->
      match value with
        | CompoundSlice slice   -> CompoundPin { data with Slices = update data.Slices slice }
        | _                     -> current

  // ** SetSlices

  member pin.SetSlices slices =
    match pin with
    | StringPin data as value ->
      match slices with
      | StringSlices (id,arr) when id = data.Id ->
        StringPin { data with Slices = arr }
      | _ -> value

    | IntPin data as value ->
      match slices with
      | IntSlices (id,arr) when id = data.Id ->
        IntPin { data with Slices = arr }
      | _ -> value

    | FloatPin data as value ->
      match slices with
      | FloatSlices (id,arr) when id = data.Id ->
        FloatPin { data with Slices = arr }
      | _ -> value

    | DoublePin data as value ->
      match slices with
      | DoubleSlices (id,arr) when id = data.Id ->
        DoublePin { data with Slices = arr }
      | _ -> value

    | BoolPin data as value ->
      match slices with
      | BoolSlices (id, arr) when id = data.Id ->
        BoolPin { data with Slices = arr }
      | _ -> value

    | BytePin data as value ->
      match slices with
      | ByteSlices (id, arr) when id = data.Id ->
        BytePin { data with Slices = arr }
      | _ -> value

    | EnumPin data as value ->
      match slices with
      | EnumSlices (id, arr) when id = data.Id ->
        EnumPin { data with Slices = arr }
      | _ -> value

    | ColorPin data as value ->
      match slices with
      | ColorSlices (id,arr) when id = data.Id ->
        ColorPin { data with Slices = arr }
      | _ -> value

    | CompoundPin data as value ->
      match slices with
      | CompoundSlices (id, arr) when id = data.Id ->
        CompoundPin { data with Slices = arr }
      | _ -> value


  // ** static Toggle

  static member Toggle(id, name, patch, tags, values) =
    BoolPin { Id         = id
              Name       = name
              Patch      = patch
              Tags       = tags
              Behavior   = Behavior.Toggle
              Slices     = values }

  // ** static Bang

  static member Bang(id, name, patch, tags, values) =
    BoolPin { Id         = id
              Name       = name
              Patch      = patch
              Tags       = tags
              Behavior   = Behavior.Bang
              Slices     = values }

  // ** static String

  static member String(id, name, patch, tags, values) =
    StringPin { Id         = id
                Name       = name
                Patch      = patch
                Tags       = tags
                StringType = Simple
                FileMask   = None
                MaxChars   = sizeof<int>
                Slices     = values }

  // ** static MultiLine

  static member MultiLine(id, name, patch, tags, values) =
    StringPin { Id         = id
                Name       = name
                Patch      = patch
                Tags       = tags
                StringType = MultiLine
                FileMask   = None
                MaxChars   = sizeof<int>
                Slices     = values }

  // ** static FileName

  static member FileName(id, name, patch, tags, filemask, values) =
    StringPin { Id         = id
                Name       = name
                Patch      = patch
                Tags       = tags
                StringType = FileName
                FileMask   = Some filemask
                MaxChars   = sizeof<int>
                Slices     = values }

  // ** static Directory

  static member Directory(id, name, patch, tags, filemask, values) =
    StringPin { Id         = id
                Name       = name
                Patch      = patch
                Tags       = tags
                StringType = Directory
                FileMask   = Some filemask
                MaxChars   = sizeof<int>
                Slices     = values }

  // ** static Url

  static member Url(id, name, patch, tags, values) =
    StringPin { Id         = id
                Name       = name
                Patch      = patch
                Tags       = tags
                StringType = Url
                FileMask   = None
                MaxChars   = sizeof<int>
                Slices     = values }

  // ** static IP

  static member IP(id, name, patch, tags, values) =
    StringPin { Id         = id
                Name       = name
                Patch      = patch
                Tags       = tags
                StringType = Url
                FileMask   = None
                MaxChars   = sizeof<int>
                Slices     = values }

  // ** static Float

  static member Float(id, name, patch, tags, values) =
    FloatPin { Id         = id
               Name       = name
               Patch      = patch
               Tags       = tags
               VecSize    = 1u
               Min        = 0
               Max        = sizeof<float>
               Unit       = ""
               Precision  = 4u
               Slices     = values }

  // ** static Double

  static member Double(id, name, patch, tags, values) =
    DoublePin { Id         = id
                Name       = name
                Patch      = patch
                Tags       = tags
                VecSize    = 1u
                Min        = 0
                Max        = sizeof<double>
                Unit       = ""
                Precision  = 4u
                Slices     = values }

  // ** static Bytes

  static member Bytes(id, name, patch, tags, values) =
    BytePin { Id         = id
              Name       = name
              Patch      = patch
              Tags       = tags
              Slices     = values }

  // ** static Color

  static member Color(id, name, patch, tags, values) =
    ColorPin { Id         = id
               Name       = name
               Patch      = patch
               Tags       = tags
               Slices     = values }

  // ** static Enum

  static member Enum(id, name, patch, tags, properties, values) =
    EnumPin { Id         = id
              Name       = name
              Patch      = patch
              Tags       = tags
              Properties = properties
              Slices     = values }

  // ** static CompoundPin

  static member Compound(id, name, patch, tags, values) =
    CompoundPin { Id         = id
                  Name       = name
                  Patch      = patch
                  Tags       = tags
                  Slices     = values }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<PinFB> =
    let inline build (data: ^t) tipe =
      let offset = Binary.toOffset builder data
      PinFB.StartPinFB(builder)
      #if FABLE_COMPILER
      PinFB.AddPin(builder, offset)
      #else
      PinFB.AddPin(builder, offset.Value)
      #endif
      PinFB.AddPinType(builder, tipe)
      PinFB.EndPinFB(builder)

    match self with
    | StringPin   data -> build data PinTypeFB.StringPinFB
    | IntPin      data -> build data PinTypeFB.IntPinFB
    | FloatPin    data -> build data PinTypeFB.FloatPinFB
    | DoublePin   data -> build data PinTypeFB.DoublePinFB
    | BoolPin     data -> build data PinTypeFB.BoolPinFB
    | BytePin     data -> build data PinTypeFB.BytePinFB
    | EnumPin     data -> build data PinTypeFB.EnumPinFB
    | ColorPin    data -> build data PinTypeFB.ColorPinFB
    | CompoundPin data -> build data PinTypeFB.CompoundPinFB

  // ** FromFB

  static member FromFB(fb: PinFB) : Either<IrisError,Pin> =
    #if FABLE_COMPILER
    match fb.PinType with
    | x when x = PinTypeFB.StringPinFB ->
      StringPinFB.Create()
      |> fb.Pin
      |> StringPinD.FromFB
      |> Either.map StringPin

    | x when x = PinTypeFB.IntPinFB ->
      IntPinFB.Create()
      |> fb.Pin
      |> IntPinD.FromFB
      |> Either.map IntPin

    | x when x = PinTypeFB.FloatPinFB ->
      FloatPinFB.Create()
      |> fb.Pin
      |> FloatPinD.FromFB
      |> Either.map FloatPin

    | x when x = PinTypeFB.DoublePinFB ->
      DoublePinFB.Create()
      |> fb.Pin
      |> DoublePinD.FromFB
      |> Either.map DoublePin

    | x when x = PinTypeFB.BoolPinFB ->
      BoolPinFB.Create()
      |> fb.Pin
      |> BoolPinD.FromFB
      |> Either.map BoolPin

    | x when x = PinTypeFB.BytePinFB ->
      BytePinFB.Create()
      |> fb.Pin
      |> BytePinD.FromFB
      |> Either.map BytePin

    | x when x = PinTypeFB.EnumPinFB ->
      EnumPinFB.Create()
      |> fb.Pin
      |> EnumPinD.FromFB
      |> Either.map EnumPin

    | x when x = PinTypeFB.ColorPinFB ->
      ColorPinFB.Create()
      |> fb.Pin
      |> ColorPinD.FromFB
      |> Either.map ColorPin

    | x when x = PinTypeFB.CompoundPinFB ->
      CompoundPinFB.Create()
      |> fb.Pin
      |> CompoundPinD.FromFB
      |> Either.map CompoundPin

    | x ->
      sprintf "%A is not a valid PinTypeFB" x
      |> Error.asParseError "PinFB.FromFB"
      |> Either.fail

    #else

    match fb.PinType with
    | PinTypeFB.StringPinFB ->
      let v = fb.Pin<StringPinFB>()
      if v.HasValue then
        v.Value
        |> StringPinD.FromFB
        |> Either.map StringPin
      else
        "StringPinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | PinTypeFB.IntPinFB ->
      let v = fb.Pin<IntPinFB>()
      if v.HasValue then
        v.Value
        |> IntPinD.FromFB
        |> Either.map IntPin
      else
        "IntPinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | PinTypeFB.FloatPinFB ->
      let v = fb.Pin<FloatPinFB>()
      if v.HasValue then
        v.Value
        |> FloatPinD.FromFB
        |> Either.map FloatPin
      else
        "FloatPinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | PinTypeFB.DoublePinFB ->
      let v = fb.Pin<DoublePinFB>()
      if v.HasValue then
        v.Value
        |> DoublePinD.FromFB
        |> Either.map DoublePin
      else
        "DoublePinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | PinTypeFB.BoolPinFB ->
      let v = fb.Pin<BoolPinFB>()
      if v.HasValue then
        v.Value
        |> BoolPinD.FromFB
        |> Either.map BoolPin
      else
        "BoolPinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | PinTypeFB.BytePinFB ->
      let v = fb.Pin<BytePinFB>()
      if v.HasValue then
        v.Value
        |> BytePinD.FromFB
        |> Either.map BytePin
      else
        "BytePinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | PinTypeFB.EnumPinFB ->
      let v = fb.Pin<EnumPinFB>()
      if v.HasValue then
        v.Value
        |> EnumPinD.FromFB
        |> Either.map EnumPin
      else
        "EnumPinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | PinTypeFB.ColorPinFB ->
      let v = fb.Pin<ColorPinFB>()
      if v.HasValue then
        v.Value
        |> ColorPinD.FromFB
        |> Either.map ColorPin
      else
        "ColorPinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | PinTypeFB.CompoundPinFB ->
      let v = fb.Pin<CompoundPinFB>()
      if v.HasValue then
        v.Value
        |> CompoundPinD.FromFB
        |> Either.map CompoundPin
      else
        "CompoundPinFB has no value"
        |> Error.asParseError "PinFB.FromFB"
        |> Either.fail

    | x ->
      sprintf "PinTypeFB not recognized: %A" x
      |> Error.asParseError "PinFB.FromFB"
      |> Either.fail

    #endif

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,Pin> =
    Binary.createBuffer bytes
    |> PinFB.GetRootAsPinFB
    |> Pin.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    let yaml = new PinYaml()
    match self with
    | StringPin data ->
      let mask =
        match data.FileMask with
        | Some mask -> mask
        | _ -> null

      yaml.PinType    <- "StringPin"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.FileMask   <- mask
      yaml.MaxChars   <- data.MaxChars
      yaml.StringType <- string data.StringType
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | IntPin data ->
      yaml.PinType    <- "IntPin"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.VecSize    <- data.VecSize
      yaml.Min        <- data.Min
      yaml.Max        <- data.Max
      yaml.Unit       <- data.Unit
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | FloatPin data ->
      yaml.PinType    <- "FloatPin"
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

    | DoublePin data ->
      yaml.PinType    <- "DoublePin"
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

    | BoolPin data ->
      yaml.PinType    <- "BoolPin"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Behavior   <- string data.Behavior
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | BytePin data ->
      yaml.PinType    <- "BytePin"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | EnumPin data ->
      yaml.PinType    <- "EnumPin"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Properties <- Array.map Yaml.toYaml data.Properties
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | ColorPin  data ->
      yaml.PinType    <- "ColorPin"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    | CompoundPin data ->
      yaml.PinType    <- "CompoundPin"
      yaml.Id         <- string data.Id
      yaml.Name       <- data.Name
      yaml.Patch      <- string data.Patch
      yaml.Tags       <- data.Tags
      yaml.Slices     <- Array.map Yaml.toYaml data.Slices

    yaml

  // ** ParseSliceYamls

  /// ## Parse all SliceYamls for a given Pin data type
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

  // ** ParseTags

  /// ## Parse all tags in a Flatbuffer-serialized type
  ///
  /// Parses all tags in a given Pin inner data type.
  ///
  /// ### Signature:
  /// - fb: the inner Pin data type (BoolPinD, StringPinD, etc.)
  ///
  /// Returns: Either<IrisError, Tag array>
  static member inline ParseTagsFB< ^a when ^a : (member TagsLength : int)
                                       and  ^a : (member Tags : int -> Tag)>
                                       (fb: ^a)
                                       : Either<IrisError, Tag array> =
    let len = (^a : (member TagsLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<IrisError,int * Tag array>) _ -> either {
          let! (i, tags) = result
          tags.[i] <- (^a : (member Tags : int -> Tag) (fb, i))
          return (i + 1, tags)
        })
      (Right (0, arr))
      arr
    |> Either.map snd

  // ** ParseSlicesFB

  #if FABLE_COMPILER

  static member inline ParseSlicesFB< ^a, ^b, ^t when ^t : (static member FromFB : ^a -> Either<IrisError, ^t>)
                                                 and ^b : (member SlicesLength : int)
                                                 and ^b : (member Slices : int -> ^a)>
                                                 (fb: ^b)
                                                 : Either<IrisError, ^t array> =
    let len = (^b : (member SlicesLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<IrisError,int * ^t array>) _ -> either {

          let! (i, slices) = result

          // In Javascript, Flatbuffer types are not modeled as nullables,
          // hence parsing code is much simpler
          let! slice =
            let value = (^b : (member Slices : int -> ^a) (fb, i))
            (^t : (static member FromFB : ^a -> Either<IrisError, ^t>) value)

          // add the slice to the array> at its correct position
          slices.[i] <- slice
          return (i + 1, slices)
      })
      (Right (0, arr))
      arr
    |> Either.map snd

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
              |> Error.asParseError (sprintf "ParseSlices of %s" (typeof< ^t >).Name)
              |> Either.fail

          // add the slice to the array> at its correct position
          slices.[i] <- slice
          return (i + 1, slices)
      })
      (Right (0, arr))
      arr
    |> Either.map snd

  #endif

  // ** FromYamlObject

  #if !FABLE_COMPILER

  static member FromYamlObject(yml: PinYaml) =
    try
      match yml.PinType with
      | "StringPin" -> either {
          let! strtype = StringType.TryParse yml.StringType
          let! slices  = Pin.ParseSliceYamls yml.Slices

          return StringPin {
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

      | "IntPin" -> either {
          let! slices = Pin.ParseSliceYamls yml.Slices

          return IntPin {
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

      | "FloatPin" -> either {
          let! slices = Pin.ParseSliceYamls yml.Slices

          return FloatPin {
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

      | "DoublePin" -> either {
          let! slices = Pin.ParseSliceYamls yml.Slices
          return DoublePin {
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

      | "BoolPin" -> either {
          let! behavior = Behavior.TryParse yml.Behavior
          let! slices = Pin.ParseSliceYamls yml.Slices
          return BoolPin {
            Id       = Id yml.Id
            Name     = yml.Name
            Patch    = Id yml.Patch
            Tags     = yml.Tags
            Behavior = behavior
            Slices   = slices
          }
        }

      | "BytePin" -> either {
          let! slices = Pin.ParseSliceYamls yml.Slices
          return BytePin {
            Id     = Id yml.Id
            Name   = yml.Name
            Patch  = Id yml.Patch
            Tags   = yml.Tags
            Slices = slices
          }
        }

      | "EnumPin" -> either {
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

          let! slices = Pin.ParseSliceYamls yml.Slices

          return EnumPin {
            Id         = Id yml.Id
            Name       = yml.Name
            Patch      = Id yml.Patch
            Tags       = yml.Tags
            Properties = properties
            Slices     = slices
          }
        }

      | "ColorPin" -> either {
          let! slices = Pin.ParseSliceYamls yml.Slices
          return ColorPin {
            Id     = Id yml.Id
            Name   = yml.Name
            Patch  = Id yml.Patch
            Tags   = yml.Tags
            Slices = slices
          }
        }

      | "CompoundPin" -> either {
          let! slices = Pin.ParseSliceYamls yml.Slices
          return CompoundPin {
            Id     = Id yml.Id
            Name   = yml.Name
            Patch  = Id yml.Patch
            Tags   = yml.Tags
            Slices = slices
          }
        }

      | x ->
        sprintf "Could not parse PinYml type: %s" x
        |> Error.asParseError "PynYml.FromYamlObject"
        |> Either.fail

    with
      | exn ->
        sprintf "Could not parse PinYml: %s" exn.Message
        |> Error.asParseError "PynYml.FromYamlObject"
        |> Either.fail

  // ** ToYaml

  member self.ToYaml(serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  // ** FromYaml

  static member FromYaml(str: string) =
    let serializer = new Serializer()
    serializer.Deserialize<PinYaml>(str)
    |> Pin.FromYamlObject

  #endif

// * BoolPinD

//  ____              _ ____
// | __ )  ___   ___ | | __ )  _____  __
// |  _ \ / _ \ / _ \| |  _ \ / _ \ \/ /
// | |_) | (_) | (_) | | |_) | (_) >  <
// |____/ \___/ \___/|_|____/ \___/_/\_\

and BoolPinD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; Behavior   : Behavior
  ; Slices     : BoolSliceD array }

  // ** ToOffset

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
    let tags = BoolPinFB.CreateTagsVector(builder, tagoffsets)
    let slices = BoolPinFB.CreateSlicesVector(builder, sliceoffsets)
    BoolPinFB.StartBoolPinFB(builder)
    BoolPinFB.AddId(builder, id)
    BoolPinFB.AddName(builder, name)
    BoolPinFB.AddPatch(builder, patch)
    BoolPinFB.AddBehavior(builder, behavior)
    BoolPinFB.AddTags(builder, tags)
    BoolPinFB.AddSlices(builder, slices)
    BoolPinFB.EndBoolPinFB(builder)

  // ** FromFB

  static member FromFB(fb: BoolPinFB) : Either<IrisError,BoolPinD> =
    either {
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb
      let! behavior = Behavior.FromFB fb.Behavior

      return { Id         = Id fb.Id
               Name       = fb.Name
               Patch      = Id fb.Patch
               Tags       = tags
               Behavior   = behavior
               Slices     = slices }
    }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,BoolPinD> =
    Binary.createBuffer bytes
    |> BoolPinFB.GetRootAsBoolPinFB
    |> BoolPinD.FromFB

// * BoolSliceD

//  ____              _ ____  _ _
// | __ )  ___   ___ | / ___|| (_) ___ ___
// |  _ \ / _ \ / _ \| \___ \| | |/ __/ _ \
// | |_) | (_) | (_) | |___) | | | (_|  __/
// |____/ \___/ \___/|_|____/|_|_|\___\___|

and BoolSliceD =
  { Index: Index
  ; Value: bool }

  // ** Create

  static member Create (idx: Index) (value: bool) =
    { Index = idx
      Value = value }

  // ** ToOffset

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

  // ** FromFB

  static member FromFB(fb: BoolSliceFB) : Either<IrisError,BoolSliceD> =
    Either.tryWith (Error.asParseError "BoolSliceD.FromFB") <| fun _ ->
      { Index = fb.Index; Value = fb.Value }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,BoolSliceD> =
    Binary.createBuffer bytes
    |> BoolSliceFB.GetRootAsBoolSliceFB
    |> BoolSliceD.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.BoolSlice(self.Index, self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "BoolSlice" -> yaml.ToBoolSliceD()
    | x ->
      sprintf "Could not parse SliceType: %s" x
      |> Error.asParseError "BooldSliceD.FromYamlObjec"
      |> Either.fail

  #endif

// * IntPinD

//  ___       _   ____
// |_ _|_ __ | |_| __ )  _____  __
//  | || '_ \| __|  _ \ / _ \ \/ /
//  | || | | | |_| |_) | (_) >  <
// |___|_| |_|\__|____/ \___/_/\_\

and IntPinD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; VecSize    : uint32
  ; Min        : int
  ; Max        : int
  ; Unit       : string
  ; Slices     : IntSliceD array }

  // ** ToOffset

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
    let tags = IntPinFB.CreateTagsVector(builder, tagoffsets)
    let slices = IntPinFB.CreateSlicesVector(builder, sliceoffsets)
    IntPinFB.StartIntPinFB(builder)
    IntPinFB.AddId(builder, id)
    IntPinFB.AddName(builder, name)
    IntPinFB.AddPatch(builder, patch)
    IntPinFB.AddTags(builder, tags)
    IntPinFB.AddVecSize(builder, self.VecSize)
    IntPinFB.AddMin(builder, self.Min)
    IntPinFB.AddMax(builder, self.Max)
    IntPinFB.AddUnit(builder, unit)
    IntPinFB.AddSlices(builder, slices)
    IntPinFB.EndIntPinFB(builder)

  // ** FromFB

  static member FromFB(fb: IntPinFB) : Either<IrisError,IntPinD> =
    either {
      let unit = if isNull fb.Unit then "" else fb.Unit
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb

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

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,IntPinD> =
    Binary.createBuffer bytes
    |> IntPinFB.GetRootAsIntPinFB
    |> IntPinD.FromFB

// * IntSliceD

//  ___       _   ____  _ _
// |_ _|_ __ | |_/ ___|| (_) ___ ___
//  | || '_ \| __\___ \| | |/ __/ _ \
//  | || | | | |_ ___) | | | (_|  __/
// |___|_| |_|\__|____/|_|_|\___\___|

and IntSliceD =
  { Index: Index
  ; Value: int }

  // ** Create

  static member Create (idx: Index) (value: int) =
    { Index = idx
      Value = value }

  // ** ToOffset

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

  // ** FromFB

  static member FromFB(fb: IntSliceFB) : Either<IrisError,IntSliceD> =
    Either.tryWith (Error.asParseError "IntSliceD.FromFB") <| fun _ ->
      { Index = fb.Index
        Value = fb.Value }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,IntSliceD> =
    Binary.createBuffer bytes
    |> IntSliceFB.GetRootAsIntSliceFB
    |> IntSliceD.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.IntSlice(self.Index, self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "IntSlice" -> yaml.ToIntSliceD()
    | x ->
      sprintf "Could not parse %s as InSlice" x
      |> Error.asParseError "IntSliceD.FromYamlObject"
      |> Either.fail

  #endif

// * FloatPinD

//  _____ _             _   ____
// |  ___| | ___   __ _| |_| __ )  _____  __
// | |_  | |/ _ \ / _` | __|  _ \ / _ \ \/ /
// |  _| | | (_) | (_| | |_| |_) | (_) >  <
// |_|   |_|\___/ \__,_|\__|____/ \___/_/\_\

and FloatPinD =
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

  // ** ToOffset

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
    let tags = FloatPinFB.CreateTagsVector(builder, tagoffsets)
    let slices = FloatPinFB.CreateSlicesVector(builder, sliceoffsets)
    FloatPinFB.StartFloatPinFB(builder)
    FloatPinFB.AddId(builder, id)
    FloatPinFB.AddName(builder, name)
    FloatPinFB.AddPatch(builder, patch)
    FloatPinFB.AddTags(builder, tags)
    FloatPinFB.AddVecSize(builder, self.VecSize)
    FloatPinFB.AddMin(builder, self.Min)
    FloatPinFB.AddMax(builder, self.Max)
    FloatPinFB.AddUnit(builder, unit)
    FloatPinFB.AddPrecision(builder, self.Precision)
    FloatPinFB.AddSlices(builder, slices)
    FloatPinFB.EndFloatPinFB(builder)

  // ** FromFB

  static member FromFB(fb: FloatPinFB) : Either<IrisError,FloatPinD> =
    either {
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb
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


  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,FloatPinD> =
    Binary.createBuffer bytes
    |> FloatPinFB.GetRootAsFloatPinFB
    |> FloatPinD.FromFB

// * FloatSliceD

//  _____ _             _   ____  _ _
// |  ___| | ___   __ _| |_/ ___|| (_) ___ ___
// | |_  | |/ _ \ / _` | __\___ \| | |/ __/ _ \
// |  _| | | (_) | (_| | |_ ___) | | | (_|  __/
// |_|   |_|\___/ \__,_|\__|____/|_|_|\___\___|

and FloatSliceD =
  { Index: Index
  ; Value: float }

  // ** Create

  static member Create (idx: Index) (value: float) =
    { Index = idx
      Value = value }

  // ** ToOffset

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

  // ** FromFB

  static member FromFB(fb: FloatSliceFB) : Either<IrisError,FloatSliceD> =
    Either.tryWith (Error.asParseError "FloatSliceD.FromFB") <| fun _ ->
      { Index = fb.Index
        Value = float fb.Value }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,FloatSliceD> =
    Binary.createBuffer bytes
    |> FloatSliceFB.GetRootAsFloatSliceFB
    |> FloatSliceD.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.FloatSlice(self.Index, self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "FloatSlice" -> yaml.ToFloatSliceD()
    | x ->
      sprintf "Cannot parse %s as FloatSlice" x
      |> Error.asParseError "FloatSliceD.FromYamlObject"
      |> Either.fail

  #endif

// * DoublePinD

//  ____              _     _      ____
// |  _ \  ___  _   _| |__ | | ___| __ )  _____  __
// | | | |/ _ \| | | | '_ \| |/ _ \  _ \ / _ \ \/ /
// | |_| | (_) | |_| | |_) | |  __/ |_) | (_) >  <
// |____/ \___/ \__,_|_.__/|_|\___|____/ \___/_/\_\

and DoublePinD =
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

  // ** ToOffset

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
    let tags = DoublePinFB.CreateTagsVector(builder, tagoffsets)
    let slices = DoublePinFB.CreateSlicesVector(builder, sliceoffsets)
    DoublePinFB.StartDoublePinFB(builder)
    DoublePinFB.AddId(builder, id)
    DoublePinFB.AddName(builder, name)
    DoublePinFB.AddPatch(builder, patch)
    DoublePinFB.AddTags(builder, tags)
    DoublePinFB.AddVecSize(builder, self.VecSize)
    DoublePinFB.AddMin(builder, self.Min)
    DoublePinFB.AddMax(builder, self.Max)
    DoublePinFB.AddUnit(builder, unit)
    DoublePinFB.AddPrecision(builder, self.Precision)
    DoublePinFB.AddSlices(builder, slices)
    DoublePinFB.EndDoublePinFB(builder)

  // ** FromFB

  static member FromFB(fb: DoublePinFB) : Either<IrisError,DoublePinD> =
    either {
      let unit = if isNull fb.Unit then "" else fb.Unit
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb

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

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,DoublePinD> =
    Binary.createBuffer bytes
    |> DoublePinFB.GetRootAsDoublePinFB
    |> DoublePinD.FromFB

// * DoubleSliceD

//  ____              _     _      ____  _ _
// |  _ \  ___  _   _| |__ | | ___/ ___|| (_) ___ ___
// | | | |/ _ \| | | | '_ \| |/ _ \___ \| | |/ __/ _ \
// | |_| | (_) | |_| | |_) | |  __/___) | | | (_|  __/
// |____/ \___/ \__,_|_.__/|_|\___|____/|_|_|\___\___|

and DoubleSliceD =
  { Index: Index
  ; Value: double }

  // ** Create

  static member Create (idx: Index) (value: double) =
    { Index = idx
      Value = value }

  // ** ToOffset

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

  // ** FromFB

  static member FromFB(fb: DoubleSliceFB) : Either<IrisError,DoubleSliceD> =
    Either.tryWith (Error.asParseError "DoubleSliceD.FromFB") <| fun _ ->
      { Index = fb.Index
        Value = fb.Value }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,DoubleSliceD> =
    Binary.createBuffer bytes
    |> DoubleSliceFB.GetRootAsDoubleSliceFB
    |> DoubleSliceD.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.DoubleSlice(self.Index, self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "DoubleSlice" -> yaml.ToDoubleSliceD()
    | x ->
      sprintf "Could not parse %s as DoubleSliceD" x
      |> Error.asParseError "DoubleSliceD.FromYamlObject"
      |> Either.fail

  #endif

// * BytePinD

//  ____        _       ____
// | __ ) _   _| |_ ___| __ )  _____  __
// |  _ \| | | | __/ _ \  _ \ / _ \ \/ /
// | |_) | |_| | ||  __/ |_) | (_) >  <
// |____/ \__, |\__\___|____/ \___/_/\_\
//        |___/

and BytePinD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag        array
  ; Slices     : ByteSliceD array }

  // ** ToOffset

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
    let tags = BytePinFB.CreateTagsVector(builder, tagoffsets)
    let slices = BytePinFB.CreateSlicesVector(builder, sliceoffsets)
    BytePinFB.StartBytePinFB(builder)
    BytePinFB.AddId(builder, id)
    BytePinFB.AddName(builder, name)
    BytePinFB.AddPatch(builder, patch)
    BytePinFB.AddTags(builder, tags)
    BytePinFB.AddSlices(builder, slices)
    BytePinFB.EndBytePinFB(builder)

  // ** FromFB

  static member FromFB(fb: BytePinFB) : Either<IrisError,BytePinD> =
    either {
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb

      return { Id         = Id fb.Id
               Name       = fb.Name
               Patch      = Id fb.Patch
               Tags       = tags
               Slices     = slices }
    }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,BytePinD> =
    Binary.createBuffer bytes
    |> BytePinFB.GetRootAsBytePinFB
    |> BytePinD.FromFB

// * ByteSliceD

//  ____        _       ____  _ _
// | __ ) _   _| |_ ___/ ___|| (_) ___ ___
// |  _ \| | | | __/ _ \___ \| | |/ __/ _ \
// | |_) | |_| | ||  __/___) | | | (_|  __/
// |____/ \__, |\__\___|____/|_|_|\___\___|
//        |___/

and [<CustomEquality;CustomComparison>] ByteSliceD =
  { Index: Index
  ; Value: Binary.Buffer }

  // ** Create

  static member Create (idx: Index) (value: Binary.Buffer) =
    { Index = idx
      Value = value }

  // ** Equals

  override self.Equals(other) =
    match other with
    | :? ByteSliceD as slice ->
      (self :> System.IEquatable<ByteSliceD>).Equals(slice)
    | _ -> false

  // ** GetHashCode

  override self.GetHashCode() =
    let mutable hash = 42
    #if FABLE_COMPILER
    hash <- (hash * 7) + hashCode (string self.Index)
    hash <- (hash * 7) + hashCode (string self.Value.byteLength)
    #else
    hash <- (hash * 7) + self.Index.GetHashCode()
    hash <- (hash * 7) + self.Value.GetHashCode()
    #endif
    hash

  // ** CompareTo

  interface System.IComparable with
    member self.CompareTo other =
      match other with
      | :? ByteSliceD as slice -> compare self.Index slice.Index
      | _ -> invalidArg "other" "cannot compare value of different types"

  // ** Equals<ByteSliceD>

  interface System.IEquatable<ByteSliceD> with
    member self.Equals(slice: ByteSliceD) =
      let mutable contentsEqual = false
      let lengthEqual =
        #if FABLE_COMPILER
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

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.ByteSlice(self.Index,  Convert.ToBase64String self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "ByteSlice" -> yaml.ToByteSliceD()
    | x ->
      sprintf "Cannot parse %s as ByteSliceD" x
      |> Error.asParseError "ByteSliceD.FromYamlObject"
      |> Either.fail

  #endif

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let encode (bytes: Binary.Buffer) =
      #if FABLE_COMPILER
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

  // ** FromFB

  static member FromFB(fb: ByteSliceFB) : Either<IrisError,ByteSliceD> =
    let decode str =
      #if FABLE_COMPILER
      let binary = Fable.Import.Browser.window.atob str
      let bytes = Fable.Import.JS.Uint8Array.Create(float binary.Length)
      for i in 0 .. (binary.Length - 1) do
        bytes.[i] <- charCodeAt binary i
      bytes.buffer
      #else
      Convert.FromBase64String(str)
      #endif

    Either.tryWith (Error.asParseError "ByteSliceD.FromFB") <| fun _ ->
      { Index = fb.Index
        Value = decode fb.Value }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,ByteSliceD> =
    Binary.createBuffer bytes
    |> ByteSliceFB.GetRootAsByteSliceFB
    |> ByteSliceD.FromFB

// * EnumPinD

//  _____                       ____
// | ____|_ __  _   _ _ __ ___ | __ )  _____  __
// |  _| | '_ \| | | | '_ ` _ \|  _ \ / _ \ \/ /
// | |___| | | | |_| | | | | | | |_) | (_) >  <
// |_____|_| |_|\__,_|_| |_| |_|____/ \___/_/\_\

and EnumPinD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag        array
  ; Properties : Property   array
  ; Slices     : EnumSliceD array }

  // ** ToOffset

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
    let tags = EnumPinFB.CreateTagsVector(builder, tagoffsets)
    let slices = EnumPinFB.CreateSlicesVector(builder, sliceoffsets)
    let properties = EnumPinFB.CreatePropertiesVector(builder, propoffsets)
    EnumPinFB.StartEnumPinFB(builder)
    EnumPinFB.AddId(builder, id)
    EnumPinFB.AddName(builder, name)
    EnumPinFB.AddPatch(builder, patch)
    EnumPinFB.AddTags(builder, tags)
    EnumPinFB.AddProperties(builder, properties)
    EnumPinFB.AddSlices(builder, slices)
    EnumPinFB.EndEnumPinFB(builder)

  // ** FromFB

  static member FromFB(fb: EnumPinFB) : Either<IrisError,EnumPinD> =
    either {
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb

      let! properties =
        let properties = Array.zeroCreate fb.PropertiesLength
        Array.fold
          (fun (m: Either<IrisError, int * Property array>) _ -> either {
            let! (i, arr) = m
            #if FABLE_COMPILER
            let prop = fb.Properties(i)
            #else
            let! prop =
              let nullable = fb.Properties(i)
              if nullable.HasValue then
                Either.succeed nullable.Value
              else
                "Cannot parse empty property"
                |> Error.asParseError "EnumPin.FromFB"
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

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromEnums

  static member FromEnums(bytes: Binary.Buffer) : Either<IrisError,EnumPinD> =
    Binary.createBuffer bytes
    |> EnumPinFB.GetRootAsEnumPinFB
    |> EnumPinD.FromFB

// * EnumSlicdD

//  _____                       ____  _ _
// | ____|_ __  _   _ _ __ ___ / ___|| (_) ___ ___
// |  _| | '_ \| | | | '_ ` _ \\___ \| | |/ __/ _ \
// | |___| | | | |_| | | | | | |___) | | | (_|  __/
// |_____|_| |_|\__,_|_| |_| |_|____/|_|_|\___\___|

and EnumSliceD =
  { Index : Index
  ; Value : Property }

  // ** Create

  static member Create (idx: Index) (value: Property) =
    { Index = idx
      Value = value }

  // ** ToOffset

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

  // ** FromFB

  static member FromFB(fb: EnumSliceFB) : Either<IrisError,EnumSliceD> =
    Either.tryWith (Error.asParseError "EnumSliceD.FromFB") <| fun _ ->
      #if FABLE_COMPILER
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

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromEnums

  static member FromEnums(bytes: Binary.Buffer) : Either<IrisError,EnumSliceD> =
    Binary.createBuffer bytes
    |> EnumSliceFB.GetRootAsEnumSliceFB
    |> EnumSliceD.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.EnumSlice(self.Index, Yaml.toYaml self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "EnumSlice" -> yaml.ToEnumSliceD()
    | x ->
      sprintf "Cannot parse %s as EnumSlice" x
      |> Error.asParseError "EnumSliceD.FromYamlObject"
      |> Either.fail

  #endif

// ** ColorPinD

//   ____      _            ____
//  / ___|___ | | ___  _ __| __ )  _____  __
// | |   / _ \| |/ _ \| '__|  _ \ / _ \ \/ /
// | |__| (_) | | (_) | |  | |_) | (_) >  <
//  \____\___/|_|\___/|_|  |____/ \___/_/\_\

and ColorPinD =
  { Id     : Id
  ; Name   : string
  ; Patch  : Id
  ; Tags   : Tag         array
  ; Slices : ColorSliceD array }

  // ** ToOffset

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
    let tags = ColorPinFB.CreateTagsVector(builder, tagoffsets)
    let slices = ColorPinFB.CreateSlicesVector(builder, sliceoffsets)
    ColorPinFB.StartColorPinFB(builder)
    ColorPinFB.AddId(builder, id)
    ColorPinFB.AddName(builder, name)
    ColorPinFB.AddPatch(builder, patch)
    ColorPinFB.AddTags(builder, tags)
    ColorPinFB.AddSlices(builder, slices)
    ColorPinFB.EndColorPinFB(builder)

  // ** FromFB

  static member FromFB(fb: ColorPinFB) : Either<IrisError,ColorPinD> =
    either {
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb
      return { Id     = Id fb.Id
               Name   = fb.Name
               Patch  = Id fb.Patch
               Tags   = tags
               Slices = slices }
    }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromColors

  static member FromColors(bytes: Binary.Buffer) : Either<IrisError,ColorPinD> =
    Binary.createBuffer bytes
    |> ColorPinFB.GetRootAsColorPinFB
    |> ColorPinD.FromFB

// * ColorSliceD

//   ____      _            ____  _ _
//  / ___|___ | | ___  _ __/ ___|| (_) ___ ___
// | |   / _ \| |/ _ \| '__\___ \| | |/ __/ _ \
// | |__| (_) | | (_) | |   ___) | | | (_|  __/
//  \____\___/|_|\___/|_|  |____/|_|_|\___\___|

and ColorSliceD =
  { Index: Index
  ; Value: ColorSpace }

  // ** Create

  static member Create (idx: Index) (value: ColorSpace) =
    { Index = idx
      Value = value }

  // ** ToOffset

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

  // ** FromFB

  static member FromFB(fb: ColorSliceFB) : Either<IrisError,ColorSliceD> =
    Either.tryWith (Error.asParseError "ColorSliceD.FromFB") <| fun _ ->
      #if FABLE_COMPILER
      match fb.Value |> ColorSpace.FromFB with
      | Right color                 -> { Index = fb.Index; Value = color }
      | Left (ParseError (_,error)) -> failwith error
      | Left error ->
        failwithf "Unexpected error: %A" error
      #else
      let nullable = fb.Value
      if nullable.HasValue then
        match ColorSpace.FromFB nullable.Value with
        | Right color                 -> { Index = fb.Index; Value = color }
        | Left (ParseError (_,error)) -> failwith error
        | Left error ->
          failwithf "Unexpected error: %A" error
      else
        failwith "Cannot parse empty ColorSpaceFB"
      #endif

  // ** ToColors

  member self.ToColors() : Binary.Buffer = Binary.buildBuffer self

  // ** FromColors

  static member FromColors(bytes: Binary.Buffer) : Either<IrisError,ColorSliceD> =
    Binary.createBuffer bytes
    |> ColorSliceFB.GetRootAsColorSliceFB
    |> ColorSliceD.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.ColorSlice(self.Index, Yaml.toYaml self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "ColorSlice" -> yaml.ToColorSliceD()
    | x ->
      sprintf "Cannot parse %s as ColorSlice" x
      |> Error.asParseError "ColorSliceD.FromYamlObject"
      |> Either.fail

  #endif

// * StringPinD

//  ____  _        _             ____
// / ___|| |_ _ __(_)_ __   __ _| __ )  _____  __
// \___ \| __| '__| | '_ \ / _` |  _ \ / _ \ \/ /
//  ___) | |_| |  | | | | | (_| | |_) | (_) >  <
// |____/ \__|_|  |_|_| |_|\__, |____/ \___/_/\_\
//                         |___/

and StringPinD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; StringType : StringType
  ; FileMask   : FileMask
  ; MaxChars   : MaxChars
  ; Slices     : StringSliceD array }

  // ** ToOffset

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
    let tags = StringPinFB.CreateTagsVector(builder, tagoffsets)
    let slices = StringPinFB.CreateSlicesVector(builder, sliceoffsets)

    StringPinFB.StartStringPinFB(builder)
    StringPinFB.AddId(builder, id)
    StringPinFB.AddName(builder, name)
    StringPinFB.AddPatch(builder, patch)
    StringPinFB.AddTags(builder, tags)
    StringPinFB.AddStringType(builder, tipe)

    Option.map (fun mask -> StringPinFB.AddFileMask(builder, mask)) mask |> ignore

    StringPinFB.AddMaxChars(builder, self.MaxChars)
    StringPinFB.AddSlices(builder, slices)
    StringPinFB.EndStringPinFB(builder)

  // ** FromFB

  static member FromFB(fb: StringPinFB) : Either<IrisError,StringPinD> =
    either {
      let mask = if isNull fb.FileMask then None else Some fb.FileMask
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb
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

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromStrings

  static member FromStrings(bytes: Binary.Buffer) : Either<IrisError,StringPinD> =
    Binary.createBuffer bytes
    |> StringPinFB.GetRootAsStringPinFB
    |> StringPinD.FromFB

// * StringSliceD

//  ____  _        _             ____  _ _
// / ___|| |_ _ __(_)_ __   __ _/ ___|| (_) ___ ___
// \___ \| __| '__| | '_ \ / _` \___ \| | |/ __/ _ \
//  ___) | |_| |  | | | | | (_| |___) | | | (_|  __/
// |____/ \__|_|  |_|_| |_|\__, |____/|_|_|\___\___|
//                         |___/

and StringSliceD =
  { Index : Index
  ; Value : string }

  // ** Create

  static member Create (idx: Index) (value: string) =
    { Index = idx
      Value = value }

  // ** ToOffset

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

  // ** FromFB

  static member FromFB(fb: StringSliceFB) : Either<IrisError,StringSliceD> =
    Either.tryWith (Error.asParseError "StringSliceD.FromFB") <| fun _ ->
      { Index = fb.Index
        Value = fb.Value }

  // ** ToStrings

  member self.ToStrings() : Binary.Buffer = Binary.buildBuffer self

  // ** FromStrings

  static member FromStrings(bytes: Binary.Buffer) : Either<IrisError,StringSliceD> =
    Binary.createBuffer bytes
    |> StringSliceFB.GetRootAsStringSliceFB
    |> StringSliceD.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.StringSlice(self.Index, self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "StringSlice" -> yaml.ToStringSliceD()
    | x ->
      sprintf "Cannot parse %s as StringSlice" x
      |> Error.asParseError "StringSliceD.FromYamlObject"
      |> Either.fail

  #endif

// ** CompoundPinD

//   ____                                            _ ____
//  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| | __ )  _____  __
// | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` |  _ \ / _ \ \/ /
// | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| | |_) | (_) >  <
//  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/ \___/_/\_\
//                      |_|

and CompoundPinD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag   array
  ; Slices     : CompoundSliceD array }

  // ** ToOffset

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
    let tags = CompoundPinFB.CreateTagsVector(builder, tagoffsets)
    let slices = CompoundPinFB.CreateSlicesVector(builder, sliceoffsets)
    CompoundPinFB.StartCompoundPinFB(builder)
    CompoundPinFB.AddId(builder, id)
    CompoundPinFB.AddName(builder, name)
    CompoundPinFB.AddPatch(builder, patch)
    CompoundPinFB.AddTags(builder, tags)
    CompoundPinFB.AddSlices(builder, slices)
    CompoundPinFB.EndCompoundPinFB(builder)

  // ** FromFB

  static member FromFB(fb: CompoundPinFB) : Either<IrisError,CompoundPinD> =
    either {
      let! tags = Pin.ParseTagsFB fb
      let! slices = Pin.ParseSlicesFB fb

      return { Id     = Id fb.Id
               Name   = fb.Name
               Patch  = Id fb.Patch
               Tags   = tags
               Slices = slices }
    }

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromCompounds

  static member FromCompounds(bytes: Binary.Buffer) : Either<IrisError,CompoundPinD> =
    Binary.createBuffer bytes
    |> CompoundPinFB.GetRootAsCompoundPinFB
    |> CompoundPinD.FromFB

// * CompoundSliceD

//   ____                                            _ ____  _ _
//  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| / ___|| (_) ___ ___
// | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` \___ \| | |/ __/ _ \
// | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| |___) | | | (_|  __/
//  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/|_|_|\___\___|
//                      |_|

and CompoundSliceD =
  { Index      : Index
  ; Value      : Pin array }

  // ** Create

  static member Create (idx: Index) (value: Pin array) =
    { Index = idx
      Value = value }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let pinoffsets = Array.map (Binary.toOffset builder) self.Value
    let pins = CompoundSliceFB.CreateValueVector(builder, pinoffsets)
    CompoundSliceFB.StartCompoundSliceFB(builder)
    CompoundSliceFB.AddIndex(builder, self.Index)
    CompoundSliceFB.AddValue(builder, pins)
    CompoundSliceFB.EndCompoundSliceFB(builder)

  // ** FromFB

  static member FromFB(fb: CompoundSliceFB) : Either<IrisError,CompoundSliceD> =
    either {
      let! pins =
        let arr = Array.zeroCreate fb.ValueLength
        Array.fold
          (fun (m: Either<IrisError,int * Pin array>) _ -> either {
              let! (i, arr) = m

              #if FABLE_COMPILER
              let! pin = i |> fb.Value |> Pin.FromFB
              #else
              let! pin =
                let nullable = fb.Value(i)
                if nullable.HasValue then
                  nullable.Value
                  |> Pin.FromFB
                else
                  "Could not parse empty PinFB"
                  |> Error.asParseError "CompoundSliceD.FromFB"
                  |> Either.fail
              #endif

              arr.[i] <- pin
              return (i + 1, arr)
            })
          (Right (0, arr))
          arr
        |> Either.map snd

      return { Index = fb.Index
               Value = pins }
    }

  // ** ToCompounds

  member self.ToCompounds() : Binary.Buffer = Binary.buildBuffer self

  // ** FromCompounds

  static member FromCompounds(bytes: Binary.Buffer) : Either<IrisError,CompoundSliceD> =
    Binary.createBuffer bytes
    |> CompoundSliceFB.GetRootAsCompoundSliceFB
    |> CompoundSliceD.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    SliceYaml.CompoundSlice(self.Index, Array.map Yaml.toYaml self.Value)

  // ** FromYamlObject

  static member FromYamlObject(yaml: SliceYaml) =
    match yaml.SliceType with
    | "CompoundSlice" -> yaml.ToCompoundSliceD()
    | x ->
      sprintf "Could not parse %s as CompoundSlice" x
      |> Error.asParseError "CompoundSliceD.FromYamlObject"
      |> Either.fail

  #endif

// * Slice

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

  // ** Index

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

  // ** Value

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

  // ** StringValue

  member self.StringValue
    with get () =
      match self with
      | StringSlice data -> Some data.Value
      | _                -> None

  // ** StringData

  member self.StringData
    with get () =
      match self with
      | StringSlice data -> Some data
      | _                -> None

  // ** IntValue

  member self.IntValue
    with get () =
      match self with
      | IntSlice data -> Some data.Value
      | _             -> None

  // ** IntData

  member self.IntData
    with get () =
      match self with
      | IntSlice data -> Some data
      | _             -> None

  // ** FloatValue

  member self.FloatValue
    with get () =
      match self with
      | FloatSlice data -> Some data.Value
      | _               -> None

  // ** FloatData

  member self.FloatData
    with get () =
      match self with
      | FloatSlice data -> Some data
      | _               -> None

  // ** DoubleValue

  member self.DoubleValue
    with get () =
      match self with
      | DoubleSlice data -> Some data.Value
      | _                -> None

  // ** DoubleData

  member self.DoubleData
    with get () =
      match self with
      | DoubleSlice data -> Some data
      | _                -> None

  // ** BoolValue

  member self.BoolValue
    with get () =
      match self with
      | BoolSlice data -> Some data.Value
      | _              -> None

  // ** BoolData

  member self.BoolData
    with get () =
      match self with
      | BoolSlice data -> Some data
      | _              -> None

  // ** ByteValue

  member self.ByteValue
    with get () =
      match self with
      | ByteSlice data -> Some data.Value
      | _              -> None

  // ** ByteData

  member self.ByteData
    with get () =
      match self with
      | ByteSlice data -> Some data
      | _              -> None

  // ** EnumValue

  member self.EnumValue
    with get () =
      match self with
      | EnumSlice data -> Some data.Value
      | _              -> None

  // ** EnumData

  member self.EnumData
    with get () =
      match self with
      | EnumSlice data -> Some data
      | _              -> None

  // ** ColorValue

  member self.ColorValue
    with get () =
      match self with
      | ColorSlice data -> Some data.Value
      | _               -> None

  // ** ColorData

  member self.ColorData
    with get () =
      match self with
      | ColorSlice data -> Some data
      | _               -> None

  // ** CompoundValue

  member self.CompoundValue
    with get () =
      match self with
      | CompoundSlice data -> Some data.Value
      | _                  -> None

  // ** CompoundData

  member self.CompoundData
    with get () =
      match self with
      | CompoundSlice data -> Some data
      | _                  -> None

  // ** ToOffset

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
      #if FABLE_COMPILER
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

  // ** FromFB

  static member FromFB(fb: SliceFB) : Either<IrisError,Slice>  =
    match fb.SliceType with
    #if FABLE_COMPILER
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
      |> Error.asParseError "Slice.FromFB"
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
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.IntSliceFB      ->
      let slice = fb.Slice<IntSliceFB>()
      if slice.HasValue then
        slice.Value
        |> IntSliceD.FromFB
        |> Either.map IntSlice
      else
        "Could not parse IntSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.FloatSliceFB    ->
      let slice = fb.Slice<FloatSliceFB>()
      if slice.HasValue then
        slice.Value
        |> FloatSliceD.FromFB
        |> Either.map FloatSlice
      else
        "Could not parse FloatSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.DoubleSliceFB   ->
      let slice = fb.Slice<DoubleSliceFB>()
      if slice.HasValue then
        slice.Value
        |> DoubleSliceD.FromFB
        |> Either.map DoubleSlice
      else
        "Could not parse DoubleSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.BoolSliceFB     ->
      let slice = fb.Slice<BoolSliceFB>()
      if slice.HasValue then
        slice.Value
        |> BoolSliceD.FromFB
        |> Either.map BoolSlice
      else
        "Could not parse BoolSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.ByteSliceFB     ->
      let slice = fb.Slice<ByteSliceFB>()
      if slice.HasValue then
        slice.Value
        |> ByteSliceD.FromFB
        |> Either.map ByteSlice
      else
        "Could not parse ByteSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.EnumSliceFB     ->
      let slice = fb.Slice<EnumSliceFB>()
      if slice.HasValue then
        slice.Value
        |> EnumSliceD.FromFB
        |> Either.map EnumSlice
      else
        "Could not parse EnumSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.ColorSliceFB    ->
      let slice = fb.Slice<ColorSliceFB>()
      if slice.HasValue then
        slice.Value
        |> ColorSliceD.FromFB
        |> Either.map ColorSlice
      else
        "Could not parse ColorSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.CompoundSliceFB ->
      let slice = fb.Slice<CompoundSliceFB>()
      if slice.HasValue then
        slice.Value
        |> CompoundSliceD.FromFB
        |> Either.map CompoundSlice
      else
        "Could not parse CompoundSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | x ->
      sprintf "Cannot parse slice. Unknown slice type: %A" x
      |> Error.asParseError "Slice.FromFB"
      |> Either.fail

    #endif

  // ** ToBytes

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,Slice> =
    Binary.createBuffer bytes
    |> SliceFB.GetRootAsSliceFB
    |> Slice.FromFB

  // ** ToYaml

  #if !FABLE_COMPILER

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
        let pins = Array.map Yaml.toYaml slice.Value
        SliceYaml.CompoundSlice(self.Index, pins)

    serializer.Serialize yaml

  // ** FromYaml

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
      |> Error.asParseError "Slice.FromYaml"
      |> Either.fail

  #endif

// * Slices

//  ____  _ _
// / ___|| (_) ___ ___  ___
// \___ \| | |/ __/ _ \/ __|
//  ___) | | | (_|  __/\__ \
// |____/|_|_|\___\___||___/

and Slices =
  | StringSlices   of Id * StringSliceD   array
  | IntSlices      of Id * IntSliceD      array
  | FloatSlices    of Id * FloatSliceD    array
  | DoubleSlices   of Id * DoubleSliceD   array
  | BoolSlices     of Id * BoolSliceD     array
  | ByteSlices     of Id * ByteSliceD     array
  | EnumSlices     of Id * EnumSliceD     array
  | ColorSlices    of Id * ColorSliceD    array
  | CompoundSlices of Id * CompoundSliceD array

  // ** IsString

  member self.IsString
    with get () =
      match self with
      | StringSlices _ -> true
      |              _ -> false

  // ** IsInt

  member self.IsInt
    with get () =
      match self with
      | IntSlices _ -> true
      |           _ -> false

  // ** IsFloat

  member self.IsFloat
    with get () =
      match self with
      | FloatSlices _ -> true
      |           _ -> false

  // ** IsDouble

  member self.IsDouble
    with get () =
      match self with
      | DoubleSlices _ -> true
      |              _ -> false

  // ** IsBool

  member self.IsBool
    with get () =
      match self with
      | BoolSlices _ -> true
      |            _ -> false

  // ** IsByte

  member self.IsByte
    with get () =
      match self with
      | ByteSlices _ -> true
      |            _ -> false

  // ** IsEnum

  member self.IsEnum
    with get () =
      match self with
      | EnumSlices _ -> true
      |            _ -> false

  // ** IsColor

  member self.IsColor
    with get () =
      match self with
      | ColorSlices _ -> true
      |             _ -> false

  // ** IsCompound

  member self.IsCompound
    with get () =
      match self with
      | CompoundSlices _ -> true
      |                _ -> false

  // ** Item

  //  ___ _
  // |_ _| |_ ___ _ __ ___
  //  | || __/ _ \ '_ ` _ \
  //  | || ||  __/ | | | | |
  // |___|\__\___|_| |_| |_|

  member self.Item (idx: int) =
    match self with
    | StringSlices   (_,arr) -> StringSlice   arr.[idx]
    | IntSlices      (_,arr) -> IntSlice      arr.[idx]
    | FloatSlices    (_,arr) -> FloatSlice    arr.[idx]
    | DoubleSlices   (_,arr) -> DoubleSlice   arr.[idx]
    | BoolSlices     (_,arr) -> BoolSlice     arr.[idx]
    | ByteSlices     (_,arr) -> ByteSlice     arr.[idx]
    | EnumSlices     (_,arr) -> EnumSlice     arr.[idx]
    | ColorSlices    (_,arr) -> ColorSlice    arr.[idx]
    | CompoundSlices (_,arr) -> CompoundSlice arr.[idx]

  // ** At

  member self.At (idx: int) = self.Item idx

  // ** Map

  //  __  __
  // |  \/  | __ _ _ __
  // | |\/| |/ _` | '_ \
  // | |  | | (_| | |_) |
  // |_|  |_|\__,_| .__/
  //              |_|

  member self.Map (f: Slice -> 'a) : 'a array =
    match self with
    | StringSlices   (_,arr) -> Array.map (StringSlice   >> f) arr
    | IntSlices      (_,arr) -> Array.map (IntSlice      >> f) arr
    | FloatSlices    (_,arr) -> Array.map (FloatSlice    >> f) arr
    | DoubleSlices   (_,arr) -> Array.map (DoubleSlice   >> f) arr
    | BoolSlices     (_,arr) -> Array.map (BoolSlice     >> f) arr
    | ByteSlices     (_,arr) -> Array.map (ByteSlice     >> f) arr
    | EnumSlices     (_,arr) -> Array.map (EnumSlice     >> f) arr
    | ColorSlices    (_,arr) -> Array.map (ColorSlice    >> f) arr
    | CompoundSlices (_,arr) -> Array.map (CompoundSlice >> f) arr

  //  _   _      _
  // | | | | ___| |_ __   ___ _ __ ___
  // | |_| |/ _ \ | '_ \ / _ \ '__/ __|
  // |  _  |  __/ | |_) |  __/ |  \__ \
  // |_| |_|\___|_| .__/ \___|_|  |___/
  //              |_|

  // ** CreateString

  member __.CreateString (idx: Index) (value: string) =
    StringSlice { Index = idx; Value = value }

  // ** CreateInt

  member __.CreateInt (idx: Index) (value: int) =
    IntSlice { Index = idx; Value = value }

  // ** CreateFloat

  member __.CreateFloat (idx: Index) (value: float) =
    FloatSlice { Index = idx; Value = value }

  // ** CreateDouble

  member __.CreateDouble (idx: Index) (value: double) =
    DoubleSlice { Index = idx; Value = value }

  // ** CreateBool

  member __.CreateBool (idx: Index) (value: bool) =
    BoolSlice { Index = idx; Value = value }

  // ** CreateByte

  member __.CreateByte (idx: Index) (value: Binary.Buffer) =
    ByteSlice { Index = idx; Value = value }

  // ** CreateEnum

  member __.CreateEnum (idx: Index) (value: Property) =
    EnumSlice { Index = idx; Value = value }

  // ** CreateColor

  member __.CreateColor (idx: Index) (value: ColorSpace) =
    ColorSlice { Index = idx; Value = value }

  // ** CreateCompound

  member __.CreateCompound (idx: Index) (value: Pin array) =
    CompoundSlice { Index = idx; Value = value }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member slices.ToOffset(builder: FlatBufferBuilder) =
    match slices with
    | StringSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (StringSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

    | IntSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (IntSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

    | FloatSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (FloatSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

    | DoubleSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (DoubleSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

    | BoolSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (BoolSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

    | ByteSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (ByteSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

    | EnumSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (EnumSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

    | ColorSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (ColorSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

    | CompoundSlices (id,arr) ->
      let id = id |> string |> builder.CreateString
      let offsets =
        let converted = Array.map (CompoundSlice >> Binary.toOffset builder) arr
        SlicesFB.CreateSlicesVector(builder, converted)
      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddId(builder,id)
      SlicesFB.AddSlices(builder,offsets)
      SlicesFB.EndSlicesFB(builder)

  static member inline FromFB(fb: SlicesFB) : Either<IrisError,Slices> =
    either {
      let! (slices,_) =
        let arr = Array.zeroCreate fb.SlicesLength
        Array.fold
          (fun (m: Either<IrisError,Slice array * int>) _ -> either {
              let! (parsed,idx) = m
              let slicish = fb.Slices(idx)
              #if FABLE_COMPILER
              let! slice = Slice.FromFB slicish
              parsed.[idx] <- slice
              return parsed, idx + 1
              #else
              if slicish.HasValue then
                let value = slicish.Value
                let! slice = Slice.FromFB value
                parsed.[idx] <- slice
                return parsed, idx + 1
              else
                return!
                  "Empty slice value"
                  |> Error.asParseError "Slices.FromFB"
                  |> Either.fail
              #endif
            })
          (Right (arr, 0))
          arr

      if Array.length slices > 0 then
        let first = slices.[0]
        try
          return
            match first with
            | StringSlice   _ ->
              let stringslices = Array.map (fun (sl: Slice) -> sl.Value :?> StringSliceD) slices
              StringSlices(Id fb.Id, stringslices)
            | IntSlice      _ ->
              let intslices = Array.map (fun (sl: Slice) -> sl.Value :?> IntSliceD) slices
              IntSlices(Id fb.Id, intslices)
            | FloatSlice    _ ->
              let floatslices = Array.map (fun (sl: Slice) -> sl.Value :?> FloatSliceD) slices
              FloatSlices(Id fb.Id, floatslices)
            | DoubleSlice   _ ->
              let doubleslices = Array.map (fun (sl: Slice) -> sl.Value :?> DoubleSliceD) slices
              DoubleSlices(Id fb.Id, doubleslices)
            | BoolSlice     _ ->
              let boolslices = Array.map (fun (sl: Slice) -> sl.Value :?> BoolSliceD) slices
              BoolSlices(Id fb.Id, boolslices)
            | ByteSlice     _ ->
              let byteslices = Array.map (fun (sl: Slice) -> sl.Value :?> ByteSliceD) slices
              ByteSlices(Id fb.Id, byteslices)
            | EnumSlice     _ ->
              let enumslices = Array.map (fun (sl: Slice) -> sl.Value :?> EnumSliceD) slices
              EnumSlices(Id fb.Id, enumslices)
            | ColorSlice    _ ->
              let colorslices = Array.map (fun (sl: Slice) -> sl.Value :?> ColorSliceD) slices
              ColorSlices(Id fb.Id, colorslices)
            | CompoundSlice _ ->
              let compoundslices = Array.map (fun (sl: Slice) -> sl.Value :?> CompoundSliceD) slices
              CompoundSlices(Id fb.Id, compoundslices)
        with
          | exn ->
            return!
              exn.Message
              |> Error.asParseError "Slices.FromFB"
              |> Either.fail
      else
        return!
          "Empty slices makes no sense brotha"
          |> Error.asParseError "Slices.FromFB"
          |> Either.fail
    }
