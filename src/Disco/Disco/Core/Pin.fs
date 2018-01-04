namespace rec Disco.Core

// * Imports

#if FABLE_COMPILER

open System
open Fable.Core
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System
open System.Text
open FlatBuffers
open Disco.Serialization

#endif

#if !FABLE_COMPILER && !DISCO_NODES
open SharpYaml.Serialization
#endif

// * Behavior

//  ____       _                 _
// | __ )  ___| |__   __ ___   _(_) ___  _ __
// |  _ \ / _ \ '_ \ / _` \ \ / / |/ _ \| '__|
// | |_) |  __/ | | | (_| |\ V /| | (_) | |
// |____/ \___|_| |_|\__,_| \_/ |_|\___/|_|

/// Behavior of string based Pins. Used to validate user input.
type Behavior =
  /// Regular, single-line string without any special properties.
  | Simple

  /// Multi-line string (text blob)
  | MultiLine

  /// FileName indicates that the values of the Pin are file paths. Will cause the UI to open a file
  /// chooser dialog.
  | FileName

  /// Directory indicates that the values of the Pin are paths to directory on the target systems. A
  /// file chooser dialog will handle this type of StringPin in the UI.
  | Directory

  /// A generic URI type.
  | Url

  /// will validate as an IP address
  | IP

  // ** TryParse

  static member TryParse (str: string) =
    match String.toLower str with
    | "string" | "simple" -> Right Simple
    | "multiline" -> Right MultiLine
    | "filename"  -> Right FileName
    | "directory" -> Right Directory
    | "url"       -> Right Url
    | "ip"        -> Right IP
    | _ ->
      sprintf "Invalid Behavior value: %s" str
      |> Error.asParseError "Behavior.TryParse"
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

  static member FromFB (fb: BehaviorFB) =
    #if FABLE_COMPILER
    match fb with
    | x when x = BehaviorFB.SimpleFB    -> Right Simple
    | x when x = BehaviorFB.MultiLineFB -> Right MultiLine
    | x when x = BehaviorFB.FileNameFB  -> Right FileName
    | x when x = BehaviorFB.DirectoryFB -> Right Directory
    | x when x = BehaviorFB.UrlFB       -> Right Url
    | x when x = BehaviorFB.IPFB        -> Right IP
    | x ->
      sprintf "Cannot parse Behavior. Unknown type: %A" x
      |> Error.asParseError "Behavior.FromFB"
      |> Either.fail

    #else

    match fb with
    | BehaviorFB.SimpleFB    -> Right Simple
    | BehaviorFB.MultiLineFB -> Right MultiLine
    | BehaviorFB.FileNameFB  -> Right FileName
    | BehaviorFB.DirectoryFB -> Right Directory
    | BehaviorFB.UrlFB       -> Right Url
    | BehaviorFB.IPFB        -> Right IP
    | x ->
      sprintf "Cannot parse Behavior. Unknown type: %A" x
      |> Error.asParseError "Behavior.FromFB"
      |> Either.fail

    #endif

  // ** ToOffset

  member self.ToOffset(_: FlatBufferBuilder) : BehaviorFB =
    match self with
    | Simple    -> BehaviorFB.SimpleFB
    | MultiLine -> BehaviorFB.MultiLineFB
    | FileName  -> BehaviorFB.FileNameFB
    | Directory -> BehaviorFB.DirectoryFB
    | Url       -> BehaviorFB.UrlFB
    | IP        -> BehaviorFB.IPFB


// * PinConfiguration

/// Configuration of the Pin indicating its role in the system. This setting is particularly important
/// for UI purposes to govern the way the user can interact with a Pin.
[<RequireQualifiedAccess>]
type PinConfiguration =
  /// Source pins, are, as the name suggests, sources of data and not editable. That could be anything
  /// from a sensor delivering values for consumption or VVVV IOBoxes that are connected at the top.
  | Source

  /// Sinks by contrast are pins that are editable.
  | Sink

  /// Preset pins are special sinks which can have individual values per client. This is used in
  /// scenarios where its important to be able to set values in a per-client fashion,
  /// e.g. client-specific configurations like display offsets and such.
  | Preset

  // ** ToString

  override config.ToString() =
    match config with
    | Source -> "Source"
    | Sink   -> "Sink"
    | Preset -> "Preset"

  // ** Parse

  static member Parse(str: string) =
    match str.ToLowerInvariant() with
    | "source" -> Source
    | "sink" -> Sink
    | "preset" -> Preset
    | _ -> failwithf "Unknown PinConfiguration %A" str

  // ** TryParse

  static member TryParse(str: string) =
    try
      str
      |> PinConfiguration.Parse
      |> Either.succeed
    with
      | x ->
        x.Message
        |> Error.asParseError "PinConfiguration.TryParse"
        |> Either.fail

  // ** ToOffset

  member configuration.ToOffset(_: FlatBufferBuilder) =
    match configuration with
    | Sink   -> PinConfigurationFB.SinkFB
    | Source -> PinConfigurationFB.SourceFB
    | Preset -> PinConfigurationFB.PresetFB

  // ** FromFB

  static member FromFB(fb: PinConfigurationFB) =
    #if FABLE_COMPILER
    match fb with
    | x when x = PinConfigurationFB.SinkFB   -> Right Sink
    | x when x = PinConfigurationFB.SourceFB -> Right Source
    | x when x = PinConfigurationFB.PresetFB -> Right Preset
    | x ->
      sprintf "Unknown PinConfigurationFB value: %A" x
      |> Error.asParseError "PinConfiguration.FromFB"
      |> Either.fail
    #else
    match fb with
    | PinConfigurationFB.SinkFB   -> Right Sink
    | PinConfigurationFB.SourceFB -> Right Source
    | PinConfigurationFB.PresetFB -> Right Preset
    | x ->
      sprintf "Unknown PinConfigurationFB value: %A" x
      |> Error.asParseError "PinConfiguration.FromFB"
      |> Either.fail
    #endif

// * VecSize

/// Indicates the behavior of the underlying value array. `VecSize.Dynamic` means that the underlying
/// array can have any length and may change at use request. By contrast, `VecSize.Fixed` in
/// combination with a specified length will cause the values array to be validated to always have the
/// length requested by the user.
[<RequireQualifiedAccess>]
type VecSize =
  /// Dynmically size value arrays in Pin
  | Dynamic
  /// Fixed size value array
  | Fixed of size:uint16

  // ** ToString

  override vecsize.ToString() =
    match vecsize with
    | Dynamic -> "dynamic"
    | Fixed n -> sprintf "fixed %d" n

  // ** Parse

  static member Parse(str: string) =
    match str with
    | "dynamic" -> Dynamic
    | other ->
      match other.Split(' ') with
      | [| "fixed"; n |] ->
        try
          let num = System.Convert.ToUInt16 n
          Fixed num
        with
          | _ -> failwithf "Unable to parse %A in VecSize string %A" n str
      | _ -> failwithf "Unable to parse VecSize string %A" str

  // ** TryParse

  static member TryParse(str: string) =
    try
      str
      |> VecSize.Parse
      |> Either.succeed
    with
      | x ->
        x.Message
        |> Error.asParseError "VecSize.TryParse"
        |> Either.fail

  // ** ToOffset

  member vecsize.ToOffset(builder: FlatBufferBuilder) =
    match vecsize with
    | Dynamic ->
      VecSizeFB.StartVecSizeFB(builder)
      VecSizeFB.AddType(builder, VecSizeTypeFB.DynamicFB)
      VecSizeFB.EndVecSizeFB(builder)
    | Fixed n ->
      VecSizeFB.StartVecSizeFB(builder)
      VecSizeFB.AddType(builder, VecSizeTypeFB.FixedFB)
      VecSizeFB.AddSize(builder, n)
      VecSizeFB.EndVecSizeFB(builder)

  // ** FromFB

  static member FromFB(fb: VecSizeFB) =
    #if FABLE_COMPILER
    match fb.Type with
    | x when x = VecSizeTypeFB.DynamicFB -> Right Dynamic
    | x when x = VecSizeTypeFB.FixedFB ->
      Right (Fixed fb.Size)
    | x ->
      sprintf "Unknown VecSizeFB value: %A" x
      |> Error.asParseError "VecSize.FromFB"
      |> Either.fail
    #else
    match fb.Type with
    | VecSizeTypeFB.DynamicFB -> Right Dynamic
    | VecSizeTypeFB.FixedFB -> Right (Fixed fb.Size)
    | x ->
      sprintf "Unknown PinConfigurationFB value: %A" x
      |> Error.asParseError "PinConfiguration.FromFB"
      |> Either.fail
    #endif

// * Pin

type Pin =
  | StringPin   of StringPinD
  | NumberPin   of NumberPinD
  | BoolPin     of BoolPinD
  | BytePin     of BytePinD
  | EnumPin     of EnumPinD
  | ColorPin    of ColorPinD

  // ** Id

  member self.Id
    with get () =
      match self with
      | StringPin   data -> data.Id
      | NumberPin   data -> data.Id
      | BoolPin     data -> data.Id
      | BytePin     data -> data.Id
      | EnumPin     data -> data.Id
      | ColorPin    data -> data.Id

  // ** Name

  member self.Name
    with get () =
      match self with
      | StringPin   data -> data.Name
      | NumberPin   data -> data.Name
      | BoolPin     data -> data.Name
      | BytePin     data -> data.Name
      | EnumPin     data -> data.Name
      | ColorPin    data -> data.Name

  // ** Dirty

  member self.Dirty
    with get () =
      match self with
      | StringPin   data -> data.Dirty
      | NumberPin   data -> data.Dirty
      | BoolPin     data -> data.Dirty
      | BytePin     data -> data.Dirty
      | EnumPin     data -> data.Dirty
      | ColorPin    data -> data.Dirty

  // ** Client

  member self.ClientId
    with get () =
      match self with
      | StringPin   data -> data.ClientId
      | NumberPin   data -> data.ClientId
      | BoolPin     data -> data.ClientId
      | BytePin     data -> data.ClientId
      | EnumPin     data -> data.ClientId
      | ColorPin    data -> data.ClientId

  // ** PinConfiguration

  member self.PinConfiguration
    with get () =
      match self with
      | StringPin   data -> data.PinConfiguration
      | NumberPin   data -> data.PinConfiguration
      | BoolPin     data -> data.PinConfiguration
      | BytePin     data -> data.PinConfiguration
      | EnumPin     data -> data.PinConfiguration
      | ColorPin    data -> data.PinConfiguration

  // ** PinGroupId

  member self.PinGroupId
    with get () =
      match self with
      | StringPin   data -> data.PinGroupId
      | NumberPin   data -> data.PinGroupId
      | BoolPin     data -> data.PinGroupId
      | BytePin     data -> data.PinGroupId
      | EnumPin     data -> data.PinGroupId
      | ColorPin    data -> data.PinGroupId

  // ** Type

  member self.Type
    with get () =
      match self with
      | StringPin   _ -> "StringPin"
      | NumberPin   _ -> "NumberPin"
      | BoolPin     _ -> "BoolPin"
      | BytePin     _ -> "BytePin"
      | EnumPin     _ -> "EnumPin"
      | ColorPin    _ -> "ColorPin"

  // ** GetTags

  member self.GetTags
    with get () =
      match self with
      | StringPin data -> data.Tags
      | NumberPin data -> data.Tags
      | BoolPin   data -> data.Tags
      | BytePin   data -> data.Tags
      | EnumPin   data -> data.Tags
      | ColorPin  data -> data.Tags

  // ** VecSize

  member self.VecSize
    with get () =
      match self with
      | StringPin data -> data.VecSize
      | NumberPin data -> data.VecSize
      | BoolPin   data -> data.VecSize
      | BytePin   data -> data.VecSize
      | EnumPin   data -> data.VecSize
      | ColorPin  data -> data.VecSize

  // ** Slices

  member pin.Slices
    with get () =
      let client = if Pin.isPreset pin then Some pin.ClientId else None
      match pin with
      | StringPin   data -> StringSlices (pin.Id, client, data.Values)
      | NumberPin   data -> NumberSlices (pin.Id, client, data.Values)
      | BoolPin     data -> BoolSlices   (pin.Id, client, data.IsTrigger, data.Values)
      | BytePin     data -> ByteSlices   (pin.Id, client, data.Values)
      | EnumPin     data -> EnumSlices   (pin.Id, client, data.Values)
      | ColorPin    data -> ColorSlices  (pin.Id, client, data.Values)

  // ** Labels

  member pin.Labels
    with get () =
      match pin with
      | StringPin data -> data.Labels
      | NumberPin data -> data.Labels
      | BoolPin   data -> data.Labels
      | BytePin   data -> data.Labels
      | EnumPin   data -> data.Labels
      | ColorPin  data -> data.Labels

  // ** Persisted

  member pin.Persisted
    with get () =
      match pin with
      | StringPin data -> data.Persisted
      | NumberPin data -> data.Persisted
      | BoolPin   data -> data.Persisted
      | BytePin   data -> data.Persisted
      | EnumPin   data -> data.Persisted
      | ColorPin  data -> data.Persisted

  // ** Online

  member pin.Online
    with get () =
      match pin with
      | StringPin data -> data.Online
      | NumberPin data -> data.Online
      | BoolPin   data -> data.Online
      | BytePin   data -> data.Online
      | EnumPin   data -> data.Online
      | ColorPin  data -> data.Online

  // ** ToSpread

  #if !FABLE_COMPILER

  member pin.ToSpread() =
    pin.Slices.ToSpread()

  #endif

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
    | NumberPin   data -> build data PinTypeFB.NumberPinFB
    | BoolPin     data -> build data PinTypeFB.BoolPinFB
    | BytePin     data -> build data PinTypeFB.BytePinFB
    | EnumPin     data -> build data PinTypeFB.EnumPinFB
    | ColorPin    data -> build data PinTypeFB.ColorPinFB

  // ** FromFB

  static member FromFB(fb: PinFB) : Either<DiscoError,Pin> =
    #if FABLE_COMPILER
    match fb.PinType with
    | x when x = PinTypeFB.StringPinFB ->
      StringPinFB.Create()
      |> fb.Pin
      |> StringPinD.FromFB
      |> Either.map StringPin

    | x when x = PinTypeFB.NumberPinFB ->
      NumberPinFB.Create()
      |> fb.Pin
      |> NumberPinD.FromFB
      |> Either.map NumberPin

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

    | PinTypeFB.NumberPinFB ->
      let v = fb.Pin<NumberPinFB>()
      if v.HasValue then
        v.Value
        |> NumberPinD.FromFB
        |> Either.map NumberPin
      else
        "NumberPinFB has no value"
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

    | x ->
      sprintf "PinTypeFB not recognized: %A" x
      |> Error.asParseError "PinFB.FromFB"
      |> Either.fail

    #endif

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<DiscoError,Pin> =
    Binary.createBuffer bytes
    |> PinFB.GetRootAsPinFB
    |> Pin.FromFB

  // ** ToYaml

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !DISCO_NODES

  member pin.ToYaml() = PinYaml.ofPin pin

  #endif

  // ** FromYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  static member FromYaml(yml: PinYaml) = PinYaml.toPin yml

  #endif

// * Pin module

module Pin =
  // ** parseComplexValues

  #if FABLE_COMPILER

  let inline parseComplexValues< ^a, ^b, ^t when ^t : (static member FromFB : ^a -> Either<DiscoError, ^t>)
                                            and ^b : (member ValuesLength : int)
                                            and ^b : (member Values : int -> ^a)>
                                            (fb: ^b)
                                            : Either<DiscoError, ^t array> =
    let len = (^b : (member ValuesLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<DiscoError,int * ^t array>) _ -> either {

          let! (i, slices) = result

          // In Javascript, Flatbuffer types are not modeled as nullables,
          // hence parsing code is much simpler
          let! slice =
            let value = (^b : (member Values : int -> ^a) (fb, i))
            (^t : (static member FromFB : ^a -> Either<DiscoError, ^t>) value)

          // add the slice to the array> at its correct position
          slices.[i] <- slice
          return (i + 1, slices)
      })
      (Right (0, arr))
      arr
    |> Either.map snd

  #else

  let inline parseComplexValues< ^a, ^b, ^t when ^t : (static member FromFB : ^a -> Either<DiscoError, ^t>)
                                            and ^b : (member ValuesLength : int)
                                            and ^b : (member Values : int -> Nullable< ^a >)>
                                            (fb: ^b)
                                            : Either<DiscoError, ^t array> =
    let len = (^b : (member ValuesLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<DiscoError,int * ^t array>) _ -> either {
          let! (i, slices) = result

          // In .NET, Flatbuffers are modelled with nullables, hence
          // parsing is slightly more elaborate
          let! slice =
            let value = (^b : (member Values : int -> Nullable< ^a >) (fb, i))
            if value.HasValue then
              (^t : (static member FromFB : ^a -> Either<DiscoError, ^t>) value.Value)
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

  // ** parseSimpleValues

  #if FABLE_COMPILER

  let inline parseSimpleValues< ^a, ^b when ^b : (member ValuesLength : int)
                                       and  ^b : (member Values : int -> ^a)>
                                       (fb: ^b)
                                       : Either<DiscoError, ^a array> =
    let len = (^b : (member ValuesLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<DiscoError,int * ^a array>) _ -> either {

          let! (i, slices) = result

          // In Javascript, Flatbuffer types are not modeled as nullables,
          // hence parsing code is much simpler
          let slice = (^b : (member Values : int -> ^a) (fb, i))

          // add the slice to the array> at its correct position
          slices.[i] <- slice
          return (i + 1, slices)
      })
      (Right (0, arr))
      arr
    |> Either.map snd

  #else

  let inline parseSimpleValues< ^a, ^b when ^b : (member ValuesLength : int)
                                       and ^b : (member Values : int -> ^a)>
                                       (fb: ^b)
                                       : Either<DiscoError, ^a array> =
    let len = (^b : (member ValuesLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<DiscoError,int * ^a array>) _ -> either {
          let! (i, slices) = result

          // In .NET, Flatbuffers are modelled with nullables, hence
          // parsing is slightly more elaborate
          let slice =
            try (^b : (member Values : int -> ^a) (fb, i))
            with | _ -> Unchecked.defaultof< ^a >

          // add the slice to the array> at its correct position
          slices.[i] <- slice
          return (i + 1, slices)
      })
      (Right (0, arr))
      arr
    |> Either.map snd

  #endif

  // ** parseVecSize

  #if FABLE_COMPILER
  let inline parseVecSize< ^a when ^a : (member VecSize : VecSizeFB)> (fb: ^a)=
    let fb = (^a : (member VecSize : VecSizeFB) fb)
    VecSize.FromFB fb
  #else
  let inline parseVecSize< ^a when ^a : (member VecSize : Nullable<VecSizeFB>)> (fb: ^a) =
    let fb = (^a : (member VecSize : Nullable<VecSizeFB>) fb)
    if fb.HasValue then
      let sizish = fb.Value
      VecSize.FromFB sizish
    else
      "Cannot parse empty VecSize"
      |> Error.asParseError "VecSize.FromFB"
      |> Either.fail
  #endif

  // ** parseLabels

  /// Parse all labels in a Flatbuffer-serialized type
  let inline parseLabels< ^a when ^a : (member LabelsLength : int)
                             and  ^a : (member Labels : int -> string)>
                            (fb: ^a)
                            : Either<DiscoError, string array> =
    let len = (^a : (member LabelsLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<DiscoError,int * string array>) _ -> either {
          let! (i, labels) = result
          let value =
            try (^a : (member Labels : int -> string) (fb, i))
            with | _ -> null
          labels.[i] <- value
          return (i + 1, labels)
        })
      (Right (0, arr))
      arr
    |> Either.map snd

  // ** parseTags

  /// Parses all tags in a Flatbuffer-serialized type as the UoM Tag
  let inline parseTags< ^a when ^a : (member TagsLength : int)
                           #if FABLE_COMPILER
                           and  ^a : (member Tags : int -> KeyValueFB)>
                           #else
                           and  ^a : (member Tags : int -> Nullable<KeyValueFB>)>
                           #endif
                      (fb: ^a)
                      : Either<DiscoError, Property array> =
    let len = (^a : (member TagsLength : int) fb)
    let arr = Array.zeroCreate len
    Array.fold
      (fun (result: Either<DiscoError,int * Property array>) _ -> either {
          let! (i, arr) = result
          #if FABLE_COMPILER
          let prop = (^a : (member Tags: int -> KeyValueFB) fb, i)
          #else
          let! prop =
            let nullable = (^a : (member Tags: int -> Nullable<KeyValueFB>) fb,i)
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
      (Right (0, arr))
      arr
    |> Either.map snd

  // ** parseProperties

  let inline parseProperties< ^a when ^a : (member PropertiesLength: int)
                                 #if FABLE_COMPILER
                                 and  ^a : (member Properties: int -> KeyValueFB)>
                                 #else
                                 and  ^a : (member Properties: int -> Nullable<KeyValueFB>)>
                                 #endif
                            (fb: ^a)
                            : Either<DiscoError, Property array> =
        let len = (^a : (member PropertiesLength: int) fb)
        let properties = Array.zeroCreate len
        Array.fold
          (fun (m: Either<DiscoError, int * Property array>) _ -> either {
            let! (i, arr) = m
            #if FABLE_COMPILER
            let prop = (^a : (member Properties: int -> KeyValueFB) fb, i)
            #else
            let! prop =
              let nullable = (^a : (member Properties: int -> Nullable<KeyValueFB>) fb,i)
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

  // ** emtpyLabels

  let private emptyLabels (count: int) =
    [| for _ in 1 .. count -> "" |]

  // ** defaultTags

  let private defaultTags = Array.empty

  // ** Generic module

  module Generic =

    // *** toggle

    let toggle id name dir group client values =
      BoolPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        Tags             = defaultTags
        ClientId         = client
        IsTrigger        = false
        Persisted        = false
        Online           = true
        Dirty            = false
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        Labels           = emptyLabels (Array.length values)
        Values           = values
      }

    // *** bang

    let bang id name dir group client values =
      BoolPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        IsTrigger        = true
        Persisted        = false
        Online           = true
        Dirty            = false
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        Labels           = emptyLabels(Array.length values)
        Values           = values
      }

    // *** string

    let string id name dir group client values =
      StringPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        Behavior         = Simple
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        MaxChars         = sizeof<int> * 1<chars>
        Labels           = emptyLabels (Array.length values)
        Values           = values
      }

    // *** multiLine

    let multiLine id name dir group client values =
      StringPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        Behavior         = MultiLine
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        MaxChars         = sizeof<int> * 1<chars>
        Labels           = emptyLabels(Array.length values)
        Values           = values
      }

    // *** fileName

    let fileName id name dir group client values =
      StringPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        Behavior         = FileName
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        MaxChars         = sizeof<int> * 1<chars>
        Labels           = emptyLabels(Array.length values)
        Values           = values
      }

    // *** directory

    let directory id name dir group client values =
      StringPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        Behavior         = Directory
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        MaxChars         = sizeof<int> * 1<chars>
        Labels           = emptyLabels (Array.length values)
        Values           = values
      }

    // *** url

    let url id name dir group client values =
      StringPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        Behavior         = Url
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        MaxChars         = sizeof<int> * 1<chars>
        Labels           = emptyLabels(Array.length values)
        Values           = values
      }

    // *** ip

    let ip id name dir group client values =
      StringPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        Behavior         = IP
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        MaxChars         = sizeof<int> * 1<chars>
        Labels           = emptyLabels(Array.length values)
        Values           = values
      }

    // *** number

    let number id name dir group client values =
      NumberPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        Min              = 0
        Max              = sizeof<double>
        Unit             = ""
        Precision        = 4u
        VecSize          = VecSize.Dynamic
        PinConfiguration = dir
        Labels           = emptyLabels (Array.length values)
        Values           = values
      }

    // *** bytes

    let bytes id name dir group client values =
      BytePin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        VecSize          = VecSize.Dynamic
        PinConfiguration = dir
        Labels           = emptyLabels (Array.length values)
        Values           = values
      }

    // *** color

    let color id name dir group client values =
      ColorPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        VecSize          = VecSize.Dynamic
        PinConfiguration = dir
        Labels           = emptyLabels (Array.length values)
        Values           = values
      }

    // *** enum

    let enum id name dir group client properties values =
      EnumPin {
        Id               = id
        Name             = name
        PinGroupId       = group
        ClientId         = client
        Tags             = defaultTags
        Persisted        = false
        Online           = true
        Dirty            = false
        Properties       = properties
        PinConfiguration = dir
        VecSize          = VecSize.Dynamic
        Labels           = emptyLabels (Array.length values)
        Values           = values
      }

  // ** Sink module

  module Sink =

    // *** toggle

    let toggle id name group client values =
      Generic.toggle id name PinConfiguration.Sink group client values

    // *** bang

    let bang id name group client values =
      Generic.bang id name PinConfiguration.Sink group client values

    // *** string

    let string id name group client values =
      Generic.string id name PinConfiguration.Sink group client values

    // *** multiLine

    let multiLine id name group client values =
      Generic.multiLine id name PinConfiguration.Sink group client values

    // *** fileName

    let fileName id name group client values =
      Generic.fileName id name PinConfiguration.Sink group client values

    // *** directory

    let directory id name group client values =
      Generic.directory id name PinConfiguration.Sink group client values

    // *** url

    let url id name group client values =
      Generic.url id name PinConfiguration.Sink group client values

    // *** ip

    let ip id name group client values =
      Generic.ip id name PinConfiguration.Sink group client values

    // *** number

    let number id name group client values =
      Generic.number id name PinConfiguration.Sink group client values

    // *** bytes

    let bytes id name group client values =
      Generic.bytes id name PinConfiguration.Sink group client values

    // *** color

    let color id name group client values =
      Generic.color id name PinConfiguration.Sink group client values

    // *** enum

    let enum id name group client properties values =
      Generic.enum id name PinConfiguration.Sink group client properties values

  // ** Source module

  module Source =

    // *** toggle

    let toggle id name group client values =
      Generic.toggle id name PinConfiguration.Source group client values

    // *** bang

    let bang id name group client values =
      Generic.bang id name PinConfiguration.Source group client values

    // *** string

    let string id name group client values =
      Generic.string id name PinConfiguration.Source group client values

    // *** multiLine

    let multiLine id name group client values =
      Generic.multiLine id name PinConfiguration.Source group client values

    // *** fileName

    let fileName id name group client values =
      Generic.fileName id name PinConfiguration.Source group client values

    // *** directory

    let directory id name group client values =
      Generic.directory id name PinConfiguration.Source group client values

    // *** url

    let url id name group client values =
      Generic.url id name PinConfiguration.Source group client values

    // *** ip

    let ip id name group client values =
      Generic.ip id name PinConfiguration.Source group client values

    // *** number

    let number id name group client values =
      Generic.number id name PinConfiguration.Source group client values

    // *** bytes

    let bytes id name group client values =
      Generic.bytes id name PinConfiguration.Source group client values

    // *** color

    let color id name group client values =
      Generic.color id name PinConfiguration.Source group client values

    // *** enum

    let enum id name group client properties values =
      Generic.enum id name PinConfiguration.Source group client properties values

  // ** Player module

  module Player =

    // *** next

    let next (clientId:ClientId) (groupId:PinGroupId) (pinId:PinId) =
      BoolPin {
        Id               = pinId
        Name             = Measure.name "Next"
        PinGroupId       = groupId
        Tags             = Array.empty
        ClientId         = clientId
        Persisted        = true
        IsTrigger        = true
        Online           = true
        Dirty            = false
        PinConfiguration = PinConfiguration.Sink
        VecSize          = VecSize.Dynamic
        Labels           = emptyLabels 1
        Values           = [| false |]
      }

    // *** previous

    let previous (clientId:ClientId) (groupId:PinGroupId) (pinId:PinId) =
      BoolPin {
        Id               = pinId
        Name             = Measure.name "Previous"
        PinGroupId       = groupId
        ClientId         = clientId
        Tags             = Array.empty
        Persisted        = true
        IsTrigger        = true
        Online           = true
        Dirty            = false
        PinConfiguration = PinConfiguration.Sink
        VecSize          = VecSize.Dynamic
        Labels           = emptyLabels 1
        Values           = [| false |]
      }

    // *** call

    let call (clientId:ClientId) (groupId:PinGroupId) (pinId:PinId) =
      BoolPin {
        Id         = pinId
        Name       = Measure.name "Call"
        PinGroupId = groupId
        ClientId   = clientId
        Tags       = Array.empty
        Persisted  = true
        IsTrigger  = true
        Online     = true
        Dirty      = false
        PinConfiguration  = PinConfiguration.Sink
        VecSize    = VecSize.Dynamic
        Labels     = emptyLabels 1
        Values     = [| false |]
      }

  // ** setVecSize

  let setVecSize vecSize = function
    | StringPin data -> StringPin { data with VecSize = vecSize }
    | NumberPin data -> NumberPin { data with VecSize = vecSize }
    | BoolPin   data -> BoolPin   { data with VecSize = vecSize }
    | BytePin   data -> BytePin   { data with VecSize = vecSize }
    | EnumPin   data -> EnumPin   { data with VecSize = vecSize }
    | ColorPin  data -> ColorPin  { data with VecSize = vecSize }

  // ** setPinConfiguration

  let setPinConfiguration config = function
    | StringPin   data -> StringPin   { data with PinConfiguration = config }
    | NumberPin   data -> NumberPin   { data with PinConfiguration = config }
    | BoolPin     data -> BoolPin     { data with PinConfiguration = config }
    | BytePin     data -> BytePin     { data with PinConfiguration = config }
    | EnumPin     data -> EnumPin     { data with PinConfiguration = config }
    | ColorPin    data -> ColorPin    { data with PinConfiguration = config }

  // ** setPinGroup

  let setPinGroup groupId = function
    | StringPin data -> StringPin { data with PinGroupId = groupId }
    | NumberPin data -> NumberPin { data with PinGroupId = groupId }
    | BoolPin   data -> BoolPin   { data with PinGroupId = groupId }
    | BytePin   data -> BytePin   { data with PinGroupId = groupId }
    | EnumPin   data -> EnumPin   { data with PinGroupId = groupId }
    | ColorPin  data -> ColorPin  { data with PinGroupId = groupId }

  // ** setClient

  let setClient clientId = function
    | StringPin data -> StringPin { data with ClientId = clientId }
    | NumberPin data -> NumberPin { data with ClientId = clientId }
    | BoolPin   data -> BoolPin   { data with ClientId = clientId }
    | BytePin   data -> BytePin   { data with ClientId = clientId }
    | EnumPin   data -> EnumPin   { data with ClientId = clientId }
    | ColorPin  data -> ColorPin  { data with ClientId = clientId }

  // ** setName

  let setName name = function
    | StringPin   data -> StringPin   { data with Name = name }
    | NumberPin   data -> NumberPin   { data with Name = name }
    | BoolPin     data -> BoolPin     { data with Name = name }
    | BytePin     data -> BytePin     { data with Name = name }
    | EnumPin     data -> EnumPin     { data with Name = name }
    | ColorPin    data -> ColorPin    { data with Name = name }

  // ** setDirty

  let setDirty dirty = function
    | StringPin data -> StringPin { data with Dirty = dirty }
    | NumberPin data -> NumberPin { data with Dirty = dirty }
    | BoolPin   data -> BoolPin   { data with Dirty = dirty }
    | BytePin   data -> BytePin   { data with Dirty = dirty }
    | EnumPin   data -> EnumPin   { data with Dirty = dirty }
    | ColorPin  data -> ColorPin  { data with Dirty = dirty }

  // ** setTags

  let setTags tags = function
    | StringPin data -> StringPin { data with Tags = tags }
    | NumberPin data -> NumberPin { data with Tags = tags }
    | BoolPin   data -> BoolPin   { data with Tags = tags }
    | BytePin   data -> BytePin   { data with Tags = tags }
    | EnumPin   data -> EnumPin   { data with Tags = tags }
    | ColorPin  data -> ColorPin  { data with Tags = tags }

  // ** setBehavior

  let setBehavior behavior = function
    | StringPin data -> StringPin { data with Behavior = behavior }
    | other -> other

  // ** setSlice

  //  ____       _   ____  _ _
  // / ___|  ___| |_/ ___|| (_) ___ ___
  // \___ \ / _ \ __\___ \| | |/ __/ _ \
  //  ___) |  __/ |_ ___) | | | (_|  __/
  // |____/ \___|\__|____/|_|_|\___\___|

  let setSlice (value: Slice) (pin: Pin) =
    let update (arr : 'a array) (idx: Index) (data: 'a) =
      let idx = int idx
      if idx > Array.length arr then
        #if FABLE_COMPILER
        /// Rationale:
        ///
        /// in JavaScript an array> will re-allocate automatically under the hood
        /// hence we don't need to worry about out-of-bounds errors.
        let newarr = Array.copy arr
        newarr.[idx] <- data
        newarr
        #else
        /// Rationale:
        ///
        /// in .NET, we need to worry about out-of-bounds errors, and we
        /// detected that we are about to run into one, hence re-alloc, copy
        /// and finally set the value at the correct index.
        let newarr = Array.zeroCreate (idx + 1)
        arr.CopyTo(newarr, 0)
        newarr.[idx] <- data
        newarr
        #endif
      else
        Array.mapi (fun i old -> if i = idx then data else old) arr

    match pin with
    | StringPin data as current ->
      match value with
        | StringSlice (i,slice) -> StringPin { data with Values = update data.Values i slice }
        | _                     -> current

    | NumberPin data as current ->
      match value with
        | NumberSlice (i,slice) -> NumberPin { data with Values = update data.Values i slice }
        | _                     -> current

    | BoolPin data as current   ->
      match value with
        | BoolSlice (i,_,slice) -> BoolPin { data with Values = update data.Values i slice }
        | _                     -> current

    | BytePin data as current   ->
      match value with
        | ByteSlice (i,slice)   -> BytePin { data with Values = update data.Values i slice }
        | _                     -> current

    | EnumPin data as current   ->
      match value with
        | EnumSlice (i,slice)   -> EnumPin { data with Values = update data.Values i slice }
        | _                     -> current

    | ColorPin data as current  ->
      match value with
        | ColorSlice (i,slice)  -> ColorPin { data with Values = update data.Values i slice }
        | _                     -> current

  // ** maybeDirty

  let maybeSetDirty (pin:Pin) =
    if pin.Persisted
    then Pin.setDirty true pin
    else pin

  // ** setSlices

  let setSlices slices = function
    | StringPin data as value ->
      match slices with
      | StringSlices (id,None,arr) when id = data.Id ->
        StringPin { data with Values = arr } |> maybeSetDirty
      | StringSlices (id,Some client,arr) when id = data.Id && client = data.ClientId ->
        StringPin { data with Values = arr } |> maybeSetDirty
      | _ -> value

    | NumberPin data as value ->
      match slices with
      | NumberSlices (id,None,arr) when id = data.Id ->
        NumberPin { data with Values = arr } |> maybeSetDirty
      | NumberSlices (id,Some client,arr) when id = data.Id && client = data.ClientId ->
        NumberPin { data with Values = arr } |> maybeSetDirty
      | _ -> value

    | BoolPin data as value ->
      match slices with
      | BoolSlices (id,None,_,arr) when id = data.Id ->
        BoolPin { data with Values = arr } |> maybeSetDirty
      | BoolSlices (id,Some client,_,arr) when id = data.Id && client = data.ClientId ->
        BoolPin { data with Values = arr } |> maybeSetDirty
      | _ -> value

    | BytePin data as value ->
      match slices with
      | ByteSlices (id,None,arr) when id = data.Id ->
        BytePin { data with Values = arr } |> maybeSetDirty
      | ByteSlices (id,Some client, arr) when id = data.Id && client = data.ClientId ->
        BytePin { data with Values = arr } |> maybeSetDirty
      | _ -> value

    | EnumPin data as value ->
      match slices with
      | EnumSlices (id,None,arr) when id = data.Id ->
        EnumPin { data with Values = arr } |> maybeSetDirty
      | EnumSlices (id,Some client,arr) when id = data.Id && client = data.ClientId ->
        EnumPin { data with Values = arr } |> maybeSetDirty
      | _ -> value

    | ColorPin data as value ->
      match slices with
      | ColorSlices (id,None,arr) when id = data.Id ->
        ColorPin { data with Values = arr } |> maybeSetDirty
      | ColorSlices (id,Some client,arr) when id = data.Id && client = data.ClientId ->
        ColorPin { data with Values = arr } |> maybeSetDirty
      | _ -> value


  // ** slices

  let slices (pin: Pin) = pin.Slices

  // ** name

  let name (pin: Pin) = pin.Name

  // ** tags

  let tags (pin: Pin) = pin.GetTags

  // ** vecSize

  let vecSize (pin: Pin) = pin.VecSize

  // ** setPersisted

  let setPersisted (persisted: bool) = function
    | StringPin data -> StringPin { data with Persisted = persisted }
    | NumberPin data -> NumberPin { data with Persisted = persisted }
    | BoolPin   data -> BoolPin   { data with Persisted = persisted }
    | BytePin   data -> BytePin   { data with Persisted = persisted }
    | EnumPin   data -> EnumPin   { data with Persisted = persisted }
    | ColorPin  data -> ColorPin  { data with Persisted = persisted }

  // ** setOnline

  let setOnline (online: bool) = function
    | StringPin data -> StringPin { data with Online = online }
    | NumberPin data -> NumberPin { data with Online = online }
    | BoolPin   data -> BoolPin   { data with Online = online }
    | BytePin   data -> BytePin   { data with Online = online }
    | EnumPin   data -> EnumPin   { data with Online = online }
    | ColorPin  data -> ColorPin  { data with Online = online }

  // ** configuration

  let configuration (pin: Pin) = pin.PinConfiguration

  // ** isOnline

  let isOnline (pin: Pin) = pin.Online

  // ** isOffline

  let isOffline (pin: Pin) = not pin.Online

  // ** isPersisted

  let isPersisted (pin: Pin) = pin.Persisted

  // ** isSink

  let isSink (pin: Pin) =
    configuration pin = PinConfiguration.Sink

  // ** isSource

  let isSource (pin: Pin) =
    configuration pin = PinConfiguration.Source

  // ** isPreset

  let isPreset (pin: Pin) =
    pin.PinConfiguration = PinConfiguration.Preset

  // ** isDirty

  let isDirty (pin: Pin) = pin.Dirty

  // ** str2offset

  let str2offset (builder: FlatBufferBuilder) = function
    #if FABLE_COMPILER
    | null -> Unchecked.defaultof<Offset<string>>
    #else
    | null -> Unchecked.defaultof<StringOffset>
    #endif
    | str  -> builder.CreateString str

// * NumberPinD

///  _   _                 _               ____  _       ____
/// | \ | |_   _ _ __ ___ | |__   ___ _ __|  _ \(_)_ __ |  _ \
/// |  \| | | | | '_ ` _ \| '_ \ / _ \ '__| |_) | | '_ \| | | |
/// | |\  | |_| | | | | | | |_) |  __/ |  |  __/| | | | | |_| |
/// |_| \_|\__,_|_| |_| |_|_.__/ \___|_|  |_|   |_|_| |_|____/

[<CustomComparison; CustomEquality>]
type NumberPinD =
  { /// A unique identifier for this Pin. This Id can overlap between clients though, and
    /// can only be considered unique in the scope of its parent PinGroup and the Client it
    /// was created on.
    Id: PinId

    /// Human readable name of a Pin
    Name: Name

    /// the PinGroup this pin belongs to
    PinGroupId: PinGroupId

    /// the Client this Pin was created on
    ClientId: ClientId

    /// Tags are for adding unstructured meta data to a Pin. This can be used for grouping
    /// functions, filtering et al.
    Tags: Property array

    /// A Pin with the Persisted flag turned on will be saved to disk together with its
    /// parent PinGroup.
    Persisted: bool

    /// Flag to track whether this Pin value has changed since it was saved last
    Dirty: bool

    /// Indicates whether the Client that created this Pin is currently on- or offline.
    Online: bool

    /// The PinConfiguration of a Pin determines how it can be mapped to other Pins, and whether it
    /// is editable from the user interface. A Pin with PinConfiguration.Sink can be
    /// written to, while a Pin with PinConfiguration.Source is read-only in the UI, and
    /// used for displaying data from a Client.
    PinConfiguration: PinConfiguration

    /// Determines whether this Pin can dynamically change the length of its underlying
    /// value array.
    VecSize: VecSize

    /// String labels for each of the slices
    Labels: string array

    /// Minimum value for this number pin
    Min: int

    /// Maximum value for this number pin
    Max: int

    /// string unit to display next to the Pin
    Unit: string

    /// Floating point precision. Zero is equivalent to integers.
    Precision: uint32

    /// the underlying number values encoded as doubles
    Values: double array }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = NumberPinFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let group = NumberPinFB.CreatePinGroupIdVector(builder,self.PinGroupId.ToByteArray())
    let client = NumberPinFB.CreateClientIdVector(builder,self.ClientId.ToByteArray())
    let unit = self.Unit |> Option.mapNull builder.CreateString
    let tagoffsets = Array.map (Binary.toOffset builder) self.Tags
    let tags = NumberPinFB.CreateTagsVector(builder, tagoffsets)
    let labeloffsets = Array.map (Pin.str2offset builder) self.Labels
    let labels = NumberPinFB.CreateLabelsVector(builder, labeloffsets)
    let values = NumberPinFB.CreateValuesVector(builder, self.Values)
    let vecsize = self.VecSize.ToOffset(builder)
    let configuration = self.PinConfiguration.ToOffset(builder)
    NumberPinFB.StartNumberPinFB(builder)
    NumberPinFB.AddId(builder, id)
    Option.iter (fun value -> NumberPinFB.AddName(builder, value)) name
    NumberPinFB.AddPinGroupId(builder, group)
    NumberPinFB.AddClientId(builder, client)
    NumberPinFB.AddPersisted(builder, self.Persisted)
    NumberPinFB.AddDirty(builder, self.Dirty)
    NumberPinFB.AddOnline(builder, self.Online)
    NumberPinFB.AddTags(builder, tags)
    NumberPinFB.AddVecSize(builder, vecsize)
    NumberPinFB.AddMin(builder, self.Min)
    NumberPinFB.AddMax(builder, self.Max)
    Option.iter (fun value -> NumberPinFB.AddUnit(builder, value)) unit
    NumberPinFB.AddPrecision(builder, self.Precision)
    NumberPinFB.AddVecSize(builder, vecsize)
    NumberPinFB.AddPinConfiguration(builder, configuration)
    NumberPinFB.AddLabels(builder, labels)
    NumberPinFB.AddValues(builder, values)
    NumberPinFB.EndNumberPinFB(builder)

  // ** FromFB

  static member FromFB(fb: NumberPinFB) : Either<DiscoError,NumberPinD> =
    either {
      let! id = Id.decodeId fb
      let! groupId = Id.decodePinGroupId fb
      let! clientId = Id.decodeClientId fb
      let! tags = Pin.parseTags fb
      let! labels = Pin.parseLabels fb
      let! vecsize = Pin.parseVecSize fb
      let! configuration = PinConfiguration.FromFB fb.PinConfiguration

      let! slices =
        fb
        |> Pin.parseSimpleValues
        |> Either.map (Array.map double)

      return {
        Id               = id
        Name             = name fb.Name
        PinGroupId       = groupId
        Tags             = tags
        Persisted        = fb.Persisted
        Dirty            = fb.Dirty
        ClientId         = clientId
        Online           = fb.Online
        Min              = fb.Min
        Max              = fb.Max
        Unit             = fb.Unit
        Precision        = fb.Precision
        VecSize          = vecsize
        PinConfiguration = configuration
        Labels           = labels
        Values           = slices
      }
    }

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<DiscoError,NumberPinD> =
    Binary.createBuffer bytes
    |> NumberPinFB.GetRootAsNumberPinFB
    |> NumberPinD.FromFB

  // ** Equals

  override self.Equals(other) =
    match other with
    | :? NumberPinD as pin ->
      (self :> System.IEquatable<NumberPinD>).Equals(pin)
    | _ -> false

  override self.GetHashCode() =
    self.Id.ToString().GetHashCode()

  // ** Equals<NumberPinD>

  interface System.IEquatable<NumberPinD> with
    member self.Equals(pin: NumberPinD) =
      let valuesEqual =
        if Array.length pin.Values = Array.length self.Values then
          Array.fold
            (fun m (left, right) ->
              if m then
                match left, right with
                | _,_ when Double.IsNaN left && Double.IsNaN right -> true
                | _ -> left = right
              else m)
            true
            (Array.zip pin.Values self.Values)
        else false

      pin.Id = self.Id &&
      pin.Name = self.Name &&
      pin.PinGroupId = self.PinGroupId &&
      pin.ClientId = self.ClientId &&
      pin.Tags = self.Tags &&
      pin.VecSize = self.VecSize &&
      pin.Dirty = self.Dirty &&
      pin.Online = self.Online &&
      pin.Persisted = self.Persisted &&
      pin.PinConfiguration = self.PinConfiguration &&
      pin.Labels = self.Labels &&
      valuesEqual

  // ** CompareTo

  interface System.IComparable with
    member self.CompareTo other =
      match other with
      | :? NumberPinD as pin -> compare self.Name pin.Name
      | _ -> invalidArg "other" "cannot compare value of different types"

// * StringPinD

///  ____  _        _             ____  _       ____
/// / ___|| |_ _ __(_)_ __   __ _|  _ \(_)_ __ |  _ \
/// \___ \| __| '__| | '_ \ / _` | |_) | | '_ \| | | |
///  ___) | |_| |  | | | | | (_| |  __/| | | | | |_| |
/// |____/ \__|_|  |_|_| |_|\__, |_|   |_|_| |_|____/
///                         |___/

type StringPinD =
  { /// A unique identifier for this Pin. This Id can overlap between clients though, and
    /// can only be considered unique in the scope of its parent PinGroup and the Client it
    /// was created on.
    Id: PinId

    /// Human readable name of a Pin
    Name: Name

    /// the PinGroup this pin belongs to
    PinGroupId: PinGroupId

    /// the Client this Pin was created on
    ClientId: ClientId

    /// Tags are for adding unstructured meta data to a Pin. This can be used for grouping
    /// functions, filtering et al.
    Tags: Property array

    /// A Pin with the Persisted flag turned on will be saved to disk together with its
    /// parent PinGroup.
    Persisted: bool

    /// Flag to track whether this Pin value has changed since it was saved last
    Dirty: bool

    /// Indicates whether the Client that created this Pin is currently on- or offline.
    Online: bool

    /// The PinConfiguration of a Pin determines how it can be mapped to other Pins, and whether it
    /// is editable from the user interface. A Pin with PinConfiguration.Sink can be
    /// written to, while a Pin with PinConfiguration.Source is read-only in the UI, and
    /// used for displaying data from a Client.
    PinConfiguration: PinConfiguration

    /// Determines whether this Pin can dynamically change the length of its underlying
    /// value array.
    VecSize: VecSize

    /// String labels for each of the slices
    Labels: string array

    /// The Behavior (string type) of this Pin used by the UI. See type above for more details.
    Behavior: Behavior

    /// Maximum number of characters allowed
    MaxChars: MaxChars

    /// The underlying values
    Values: string array }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = StringPinFB.CreateIdVector(builder, self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let group = StringPinFB.CreatePinGroupIdVector(builder,self.PinGroupId.ToByteArray())
    let client = StringPinFB.CreateClientIdVector(builder,self.ClientId.ToByteArray())
    let tipe = self.Behavior.ToOffset(builder)
    let tagoffsets = Array.map (Binary.toOffset builder) self.Tags
    let labeloffsets = Array.map (Pin.str2offset builder) self.Labels
    let sliceoffsets = Array.map (Pin.str2offset builder) self.Values
    let tags = StringPinFB.CreateTagsVector(builder, tagoffsets)
    let labels = StringPinFB.CreateLabelsVector(builder, labeloffsets)
    let slices = StringPinFB.CreateValuesVector(builder, sliceoffsets)
    let vecsize = self.VecSize.ToOffset(builder)
    let configuration = self.PinConfiguration.ToOffset(builder)

    StringPinFB.StartStringPinFB(builder)
    StringPinFB.AddId(builder, id)
    Option.iter (fun value -> StringPinFB.AddName(builder,value)) name
    StringPinFB.AddPinGroupId(builder, group)
    StringPinFB.AddClientId(builder, client)
    StringPinFB.AddPersisted(builder, self.Persisted)
    StringPinFB.AddDirty(builder, self.Dirty)
    StringPinFB.AddOnline(builder, self.Online)
    StringPinFB.AddTags(builder, tags)
    StringPinFB.AddBehavior(builder, tipe)
    StringPinFB.AddMaxChars(builder, int self.MaxChars)
    StringPinFB.AddVecSize(builder, vecsize)
    StringPinFB.AddPinConfiguration(builder, configuration)
    StringPinFB.AddLabels(builder, labels)
    StringPinFB.AddValues(builder, slices)
    StringPinFB.EndStringPinFB(builder)

  // ** FromFB

  static member FromFB(fb: StringPinFB) : Either<DiscoError,StringPinD> =
    either {
      let! id = Id.decodeId fb
      let! groupId = Id.decodePinGroupId fb
      let! clientId = Id.decodeClientId fb
      let! tags = Pin.parseTags fb
      let! labels = Pin.parseLabels fb
      let! slices = Pin.parseSimpleValues fb
      let! tipe = Behavior.FromFB fb.Behavior
      let! vecsize = Pin.parseVecSize fb
      let! configuration = PinConfiguration.FromFB fb.PinConfiguration

      return {
        Id               = id
        Name             = name fb.Name
        PinGroupId       = groupId
        Tags             = tags
        Online           = fb.Online
        Persisted        = fb.Persisted
        Dirty            = fb.Dirty
        ClientId         = clientId
        Behavior         = tipe
        MaxChars         = 1<chars> * fb.MaxChars
        VecSize          = vecsize
        PinConfiguration = configuration
        Labels           = labels
        Values           = slices
      }
    }

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromStrings

  static member FromStrings(bytes: byte[]) : Either<DiscoError,StringPinD> =
    Binary.createBuffer bytes
    |> StringPinFB.GetRootAsStringPinFB
    |> StringPinD.FromFB

// * BoolPinD

///  ____              _ ____  _       ____
/// | __ )  ___   ___ | |  _ \(_)_ __ |  _ \
/// |  _ \ / _ \ / _ \| | |_) | | '_ \| | | |
/// | |_) | (_) | (_) | |  __/| | | | | |_| |
/// |____/ \___/ \___/|_|_|   |_|_| |_|____/

type BoolPinD =
  { /// A unique identifier for this Pin. This Id can overlap between clients though, and
    /// can only be considered unique in the scope of its parent PinGroup and the Client it
    /// was created on.
    Id: PinId

    /// Human readable name of a Pin
    Name: Name

    /// the PinGroup this pin belongs to
    PinGroupId: PinGroupId

    /// the Client this Pin was created on
    ClientId: ClientId

    /// Tags are for adding unstructured meta data to a Pin. This can be used for grouping
    /// functions, filtering et al.
    Tags: Property array

    /// A Pin with the Persisted flag turned on will be saved to disk together with its
    /// parent PinGroup.
    Persisted: bool

    /// Flag to track whether this Pin value has changed since it was saved last
    Dirty: bool

    /// Indicates whether the Client that created this Pin is currently on- or offline.
    Online: bool

    /// The PinConfiguration of a Pin determines how it can be mapped to other Pins, and whether it
    /// is editable from the user interface. A Pin with PinConfiguration.Sink can be
    /// written to, while a Pin with PinConfiguration.Source is read-only in the UI, and
    /// used for displaying data from a Client.
    PinConfiguration: PinConfiguration

    /// Determines whether this Pin can dynamically change the length of its underlying
    /// value array.
    VecSize: VecSize

    /// String labels for each of the slices
    Labels: string array

    /// Determines the reset behavior of this pin
    IsTrigger  : bool

    /// the underlying values of this pin
    Values     : bool array }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = BoolPinFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let group = BoolPinFB.CreatePinGroupIdVector(builder,self.PinGroupId.ToByteArray())
    let client = BoolPinFB.CreateClientIdVector(builder, self.ClientId.ToByteArray())
    let tagoffsets = Array.map (Binary.toOffset builder) self.Tags
    let tags = BoolPinFB.CreateTagsVector(builder, tagoffsets)
    let labeloffsets = Array.map (Pin.str2offset builder) self.Labels
    let labels = BoolPinFB.CreateLabelsVector(builder, labeloffsets)
    let slices = BoolPinFB.CreateValuesVector(builder, self.Values)
    let configuration = self.PinConfiguration.ToOffset(builder)
    let vecsize = self.VecSize.ToOffset(builder)
    BoolPinFB.StartBoolPinFB(builder)
    BoolPinFB.AddId(builder, id)
    Option.iter (fun value -> BoolPinFB.AddName(builder,value)) name
    BoolPinFB.AddPinGroupId(builder, group)
    BoolPinFB.AddClientId(builder, client)
    BoolPinFB.AddPersisted(builder, self.Persisted)
    BoolPinFB.AddDirty(builder, self.Dirty)
    BoolPinFB.AddOnline(builder, self.Online)
    BoolPinFB.AddIsTrigger(builder, self.IsTrigger)
    BoolPinFB.AddTags(builder, tags)
    BoolPinFB.AddPinConfiguration(builder, configuration)
    BoolPinFB.AddVecSize(builder, vecsize)
    BoolPinFB.AddLabels(builder, labels)
    BoolPinFB.AddValues(builder, slices)
    BoolPinFB.EndBoolPinFB(builder)

  // ** FromFB

  static member FromFB(fb: BoolPinFB) : Either<DiscoError,BoolPinD> =
    either {
      let! id = Id.decodeId fb
      let! groupId = Id.decodePinGroupId fb
      let! clientId = Id.decodeClientId fb
      let! tags = Pin.parseTags fb
      let! labels = Pin.parseLabels fb
      let! slices = Pin.parseSimpleValues fb
      let! vecsize = Pin.parseVecSize fb
      let! configuration = PinConfiguration.FromFB fb.PinConfiguration

      return {
        Id               = id
        Name             = name fb.Name
        PinGroupId       = groupId
        ClientId         = clientId
        Tags             = tags
        Persisted        = fb.Persisted
        Dirty            = fb.Dirty
        Online           = fb.Online
        IsTrigger        = fb.IsTrigger
        VecSize          = vecsize
        PinConfiguration = configuration
        Labels           = labels
        Values           = slices
      }
    }

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<DiscoError,BoolPinD> =
    Binary.createBuffer bytes
    |> BoolPinFB.GetRootAsBoolPinFB
    |> BoolPinD.FromFB

// * BytePinD

///  ____        _       ____  _       ____
/// | __ ) _   _| |_ ___|  _ \(_)_ __ |  _ \
/// |  _ \| | | | __/ _ \ |_) | | '_ \| | | |
/// | |_) | |_| | ||  __/  __/| | | | | |_| |
/// |____/ \__, |\__\___|_|   |_|_| |_|____/
///        |___/

type [<CustomEquality;CustomComparison>] BytePinD =
  { /// A unique identifier for this Pin. This Id can overlap between clients though, and
    /// can only be considered unique in the scope of its parent PinGroup and the Client it
    /// was created on.
    Id: PinId

    /// Human readable name of a Pin
    Name: Name

    /// the PinGroup this pin belongs to
    PinGroupId: PinGroupId

    /// the Client this Pin was created on
    ClientId: ClientId

    /// Tags are for adding unstructured meta data to a Pin. This can be used for grouping
    /// functions, filtering et al.
    Tags: Property array

    /// A Pin with the Persisted flag turned on will be saved to disk together with its
    /// parent PinGroup.
    Persisted: bool

    /// Flag to track whether this Pin value has changed since it was saved last
    Dirty: bool

    /// Indicates whether the Client that created this Pin is currently on- or offline.
    Online: bool

    /// The PinConfiguration of a Pin determines how it can be mapped to other Pins, and whether it
    /// is editable from the user interface. A Pin with PinConfiguration.Sink can be
    /// written to, while a Pin with PinConfiguration.Source is read-only in the UI, and
    /// used for displaying data from a Client.
    PinConfiguration: PinConfiguration

    /// Determines whether this Pin can dynamically change the length of its underlying
    /// value array.
    VecSize: VecSize

    /// String labels for each of the slices
    Labels: string array

    /// The byte array values
    Values: byte[] array }

  // ** Equals

  override self.Equals(other) =
    match other with
    | :? BytePinD as pin ->
      (self :> System.IEquatable<BytePinD>).Equals(pin)
    | _ -> false

  override self.GetHashCode() =
    self.Id.ToString().GetHashCode()

  // ** Equals<ByteSliceD>

  interface System.IEquatable<BytePinD> with
    member self.Equals(pin: BytePinD) =
      let mutable contentsEqual = false
      let lengthEqual =
        #if FABLE_COMPILER
        let mylen = Array.fold (fun m (t: byte[]) -> m + int t.Length) (Array.length self.Values) self.Values
        let itlen = Array.fold (fun m (t: byte[]) -> m + int t.Length) (Array.length pin.Values) pin.Values
        let result = mylen = itlen
        if result then
          let mutable contents = true
          let mutable n = 0

          while n < Array.length self.Values do
            let me = self.Values.[n]
            let it = pin.Values.[n]
            let mutable i = 0
            while i < int self.Values.[n].Length do
              if contents then
                contents <- me.[i] = it.[i]
              i <- i + 1
            n <- n + 1

          contentsEqual <- contents
        result
        #else
        let result = self.Values = pin.Values
        contentsEqual <- result
        result
        #endif
      pin.Id = self.Id &&
      pin.Name = self.Name &&
      pin.PinGroupId = self.PinGroupId &&
      pin.ClientId = self.ClientId &&
      pin.Tags = self.Tags &&
      pin.VecSize = self.VecSize &&
      pin.Dirty = self.Dirty &&
      pin.Online = self.Online &&
      pin.Persisted = self.Persisted &&
      pin.PinConfiguration = self.PinConfiguration &&
      pin.Labels = self.Labels &&
      lengthEqual &&
      contentsEqual

  // ** CompareTo

  interface System.IComparable with
    member self.CompareTo other =
      match other with
      | :? BytePinD as pin -> compare self.Name pin.Name
      | _ -> invalidArg "other" "cannot compare value of different types"

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = BytePinFB.CreateIdVector(builder, self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let group = BytePinFB.CreatePinGroupIdVector(builder,self.PinGroupId.ToByteArray())
    let client = BytePinFB.CreateClientIdVector(builder,self.ClientId.ToByteArray())
    let tagoffsets = Array.map (Binary.toOffset builder) self.Tags
    let labeloffsets = Array.map (Pin.str2offset builder) self.Labels
    let sliceoffsets = Array.map (String.encodeBase64 >> builder.CreateString) self.Values
    let labels = BytePinFB.CreateLabelsVector(builder, labeloffsets)
    let tags = BytePinFB.CreateTagsVector(builder, tagoffsets)
    let slices = BytePinFB.CreateValuesVector(builder, sliceoffsets)
    let vecsize = self.VecSize.ToOffset(builder)
    let configuration = self.PinConfiguration.ToOffset(builder)
    BytePinFB.StartBytePinFB(builder)
    BytePinFB.AddId(builder, id)
    Option.iter (fun value -> BytePinFB.AddName(builder,value)) name
    BytePinFB.AddPinGroupId(builder, group)
    BytePinFB.AddClientId(builder, client)
    BytePinFB.AddPersisted(builder, self.Persisted)
    BytePinFB.AddDirty(builder, self.Dirty)
    BytePinFB.AddOnline(builder, self.Online)
    BytePinFB.AddTags(builder, tags)
    BytePinFB.AddVecSize(builder, vecsize)
    BytePinFB.AddPinConfiguration(builder, configuration)
    BytePinFB.AddLabels(builder, labels)
    BytePinFB.AddValues(builder, slices)
    BytePinFB.EndBytePinFB(builder)

  // ** FromFB

  static member FromFB(fb: BytePinFB) : Either<DiscoError,BytePinD> =
    either {
      let! id = Id.decodeId fb
      let! group = Id.decodePinGroupId fb
      let! client = Id.decodeClientId fb
      let! tags = Pin.parseTags fb
      let! labels = Pin.parseLabels fb
      let! vecsize = Pin.parseVecSize fb
      let! configuration = PinConfiguration.FromFB fb.PinConfiguration
      let! slices =
        fb
        |> Pin.parseSimpleValues
        |> Either.map (Array.map String.decodeBase64)

      return {
        Id               = id
        Name             = name fb.Name
        PinGroupId       = group
        ClientId         = client
        Tags             = tags
        Online           = fb.Online
        Persisted        = fb.Persisted
        Dirty            = fb.Dirty
        VecSize          = vecsize
        PinConfiguration = configuration
        Labels           = labels
        Values           = slices
      }
    }

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<DiscoError,BytePinD> =
    Binary.createBuffer bytes
    |> BytePinFB.GetRootAsBytePinFB
    |> BytePinD.FromFB

// * EnumPinD

//  _____                       ____
// | ____|_ __  _   _ _ __ ___ | __ )  _____  __
// |  _| | '_ \| | | | '_ ` _ \|  _ \ / _ \ \/ /
// | |___| | | | |_| | | | | | | |_) | (_) >  <
// |_____|_| |_|\__,_|_| |_| |_|____/ \___/_/\_\

type EnumPinD =
  { /// A unique identifier for this Pin. This Id can overlap between clients though, and
    /// can only be considered unique in the scope of its parent PinGroup and the Client it
    /// was created on.
    Id: PinId

    /// Human readable name of a Pin
    Name: Name

    /// the PinGroup this pin belongs to
    PinGroupId: PinGroupId

    /// the Client this Pin was created on
    ClientId: ClientId

    /// Tags are for adding unstructured meta data to a Pin. This can be used for grouping
    /// functions, filtering et al.
    Tags: Property array

    /// A Pin with the Persisted flag turned on will be saved to disk together with its
    /// parent PinGroup.
    Persisted: bool

    /// Flag to track whether this Pin value has changed since it was saved last
    Dirty: bool

    /// Indicates whether the Client that created this Pin is currently on- or offline.
    Online: bool

    /// The PinConfiguration of a Pin determines how it can be mapped to other Pins, and whether it
    /// is editable from the user interface. A Pin with PinConfiguration.Sink can be
    /// written to, while a Pin with PinConfiguration.Source is read-only in the UI, and
    /// used for displaying data from a Client.
    PinConfiguration: PinConfiguration

    /// Determines whether this Pin can dynamically change the length of its underlying
    /// value array.
    VecSize: VecSize

    /// String labels for each of the slices
    Labels: string array

    /// Properties as Key/Value pairs
    Properties: Property array

    Values: Property array }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = EnumPinFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let group = EnumPinFB.CreatePinGroupIdVector(builder,self.PinGroupId.ToByteArray())
    let client = EnumPinFB.CreateClientIdVector(builder,self.ClientId.ToByteArray())
    let tagoffsets = Array.map (Binary.toOffset builder) self.Tags
    let labeloffsets = Array.map (Pin.str2offset builder) self.Labels
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Values
    let propoffsets = Array.map (Binary.toOffset builder) self.Properties
    let tags = EnumPinFB.CreateTagsVector(builder, tagoffsets)
    let labels = EnumPinFB.CreateLabelsVector(builder, labeloffsets)
    let slices = EnumPinFB.CreateValuesVector(builder, sliceoffsets)
    let properties = EnumPinFB.CreatePropertiesVector(builder, propoffsets)
    let configuration = self.PinConfiguration.ToOffset(builder)
    let vecsize = self.VecSize.ToOffset(builder)
    EnumPinFB.StartEnumPinFB(builder)
    EnumPinFB.AddId(builder, id)
    Option.iter (fun value -> EnumPinFB.AddName(builder,value)) name
    EnumPinFB.AddPinGroupId(builder, group)
    EnumPinFB.AddClientId(builder, client)
    EnumPinFB.AddPersisted(builder, self.Persisted)
    EnumPinFB.AddDirty(builder, self.Dirty)
    EnumPinFB.AddOnline(builder, self.Online)
    EnumPinFB.AddTags(builder, tags)
    EnumPinFB.AddProperties(builder, properties)
    EnumPinFB.AddPinConfiguration(builder, configuration)
    EnumPinFB.AddVecSize(builder, vecsize)
    EnumPinFB.AddLabels(builder, labels)
    EnumPinFB.AddValues(builder, slices)
    EnumPinFB.EndEnumPinFB(builder)

  // ** FromFB

  static member FromFB(fb: EnumPinFB) : Either<DiscoError,EnumPinD> =
    either {
      let! id = Id.decodeId fb
      let! group = Id.decodePinGroupId fb
      let! client = Id.decodeClientId fb
      let! labels = Pin.parseLabels fb
      let! tags = Pin.parseTags fb
      let! slices = Pin.parseComplexValues fb
      let! vecsize = Pin.parseVecSize fb
      let! configuration = PinConfiguration.FromFB fb.PinConfiguration

      let! properties =
        let properties = Array.zeroCreate fb.PropertiesLength
        Array.fold
          (fun (m: Either<DiscoError, int * Property array>) _ -> either {
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

      return {
        Id               = id
        Name             = name fb.Name
        PinGroupId       = group
        ClientId         = client
        Tags             = tags
        Online           = fb.Online
        Persisted        = fb.Persisted
        Dirty            = fb.Dirty
        Properties       = properties
        PinConfiguration = configuration
        VecSize          = vecsize
        Labels           = labels
        Values           = slices
      }
    }

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromEnums

  static member FromEnums(bytes: byte[]) : Either<DiscoError,EnumPinD> =
    Binary.createBuffer bytes
    |> EnumPinFB.GetRootAsEnumPinFB
    |> EnumPinD.FromFB

// * ColorPinD

//   ____      _            ____
//  / ___|___ | | ___  _ __| __ )  _____  __
// | |   / _ \| |/ _ \| '__|  _ \ / _ \ \/ /
// | |__| (_) | | (_) | |  | |_) | (_) >  <
//  \____\___/|_|\___/|_|  |____/ \___/_/\_\

type ColorPinD =
  { /// A unique identifier for this Pin. This Id can overlap between clients though, and
    /// can only be considered unique in the scope of its parent PinGroup and the Client it
    /// was created on.
    Id: PinId

    /// Human readable name of a Pin
    Name: Name

    /// the PinGroup this pin belongs to
    PinGroupId: PinGroupId

    /// the Client this Pin was created on
    ClientId: ClientId

    /// Tags are for adding unstructured meta data to a Pin. This can be used for grouping
    /// functions, filtering et al.
    Tags: Property array

    /// A Pin with the Persisted flag turned on will be saved to disk together with its
    /// parent PinGroup.
    Persisted: bool

    /// Flag to track whether this Pin value has changed since it was saved last
    Dirty: bool

    /// Indicates whether the Client that created this Pin is currently on- or offline.
    Online: bool

    /// The PinConfiguration of a Pin determines how it can be mapped to other Pins, and whether it
    /// is editable from the user interface. A Pin with PinConfiguration.Sink can be
    /// written to, while a Pin with PinConfiguration.Source is read-only in the UI, and
    /// used for displaying data from a Client.
    PinConfiguration: PinConfiguration

    /// Determines whether this Pin can dynamically change the length of its underlying
    /// value array.
    VecSize: VecSize

    /// String labels for each of the slices
    Labels: string array

    Values: ColorSpace array }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = ColorPinFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let group = ColorPinFB.CreatePinGroupIdVector(builder,self.PinGroupId.ToByteArray())
    let client = ColorPinFB.CreateClientIdVector(builder,self.ClientId.ToByteArray())
    let tagoffsets = Array.map (Binary.toOffset builder) self.Tags
    let labeloffsets = Array.map (Pin.str2offset builder) self.Labels
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Values
    let tags = ColorPinFB.CreateTagsVector(builder, tagoffsets)
    let labels = ColorPinFB.CreateLabelsVector(builder, labeloffsets)
    let slices = ColorPinFB.CreateValuesVector(builder, sliceoffsets)
    let configuration = self.PinConfiguration.ToOffset(builder)
    let vecsize = self.VecSize.ToOffset(builder)
    ColorPinFB.StartColorPinFB(builder)
    ColorPinFB.AddId(builder, id)
    Option.iter (fun value -> ColorPinFB.AddName(builder,value)) name
    ColorPinFB.AddPinGroupId(builder, group)
    ColorPinFB.AddClientId(builder, client)
    ColorPinFB.AddPersisted(builder, self.Persisted)
    ColorPinFB.AddDirty(builder, self.Dirty)
    ColorPinFB.AddOnline(builder, self.Online)
    ColorPinFB.AddTags(builder, tags)
    ColorPinFB.AddVecSize(builder, vecsize)
    ColorPinFB.AddPinConfiguration(builder, configuration)
    ColorPinFB.AddLabels(builder, labels)
    ColorPinFB.AddValues(builder, slices)
    ColorPinFB.EndColorPinFB(builder)

  // ** FromFB

  static member FromFB(fb: ColorPinFB) : Either<DiscoError,ColorPinD> =
    either {
      let! id = Id.decodeId fb
      let! group = Id.decodePinGroupId fb
      let! client = Id.decodeClientId fb
      let! tags = Pin.parseTags fb
      let! labels = Pin.parseLabels fb
      let! slices = Pin.parseComplexValues fb
      let! vecsize = Pin.parseVecSize fb
      let! configuration = PinConfiguration.FromFB fb.PinConfiguration
      return {
        Id               = id
        Name             = name fb.Name
        Online           = fb.Online
        PinGroupId       = group
        ClientId         = client
        Tags             = tags
        Persisted        = fb.Persisted
        Dirty            = fb.Dirty
        VecSize          = vecsize
        PinConfiguration = configuration
        Labels           = labels
        Values           = slices
      }
    }

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromColors

  static member FromColors(bytes: byte[]) : Either<DiscoError,ColorPinD> =
    Binary.createBuffer bytes
    |> ColorPinFB.GetRootAsColorPinFB
    |> ColorPinD.FromFB

// * Slice

//  ____  _ _
// / ___|| (_) ___ ___
// \___ \| | |/ __/ _ \
//  ___) | | | (_|  __/
// |____/|_|_|\___\___|

[<CustomEquality;CustomComparison>]
type Slice =
  | StringSlice   of index:Index * value:string
  | NumberSlice   of index:Index * value:double
  | BoolSlice     of index:Index * trigger:bool * value:bool
  | ByteSlice     of index:Index * value:byte[]
  | EnumSlice     of index:Index * value:Property
  | ColorSlice    of index:Index * value:ColorSpace

  // ** Index

  member self.Index
    with get () =
      match self with
      | StringSlice (idx, _)    -> idx
      | NumberSlice (idx, _)    -> idx
      | BoolSlice   (idx, _, _) -> idx
      | ByteSlice   (idx, _)    -> idx
      | EnumSlice   (idx, _)    -> idx
      | ColorSlice  (idx, _)    -> idx

  // ** Value

  member self.Value
    with get () : obj =
      match self with
      | StringSlice  (_, data) -> data :> obj
      | NumberSlice  (_, data) -> data :> obj
      | BoolSlice (_, _, data) -> data :> obj
      | ByteSlice    (_, data) -> data :> obj
      | EnumSlice    (_, data) -> data :> obj
      | ColorSlice   (_, data) -> data :> obj

  // ** Equals

  override self.Equals(other) =
    match other with
    | :? Slice as slice -> (self :> System.IEquatable<Slice>).Equals(slice)
    | _ -> false

  override self.GetHashCode() =
      match self with
      | StringSlice  _ -> 0
      | NumberSlice  _ -> 1
      | BoolSlice    _ -> 2
      | ByteSlice    _ -> 3
      | EnumSlice    _ -> 4
      | ColorSlice   _ -> 5

  // ** CompareTo

  interface System.IComparable with
    member self.CompareTo other =
      match other with
      | :? Slice as slice -> compare self.Index slice.Index
      | _ -> invalidArg "other" "cannot compare value of different types"

  // ** Equals<Slice>

  interface System.IEquatable<Slice> with
    member self.Equals(slice: Slice) =
      match slice with
      | StringSlice (idx, value) ->
        match self with
        | StringSlice (sidx, svalue) -> idx = sidx && value = svalue
        | _ -> false
      | NumberSlice (idx, value) when Double.IsNaN value ->
        match self with
        | NumberSlice (sidx, svalue) when Double.IsNaN svalue -> idx = sidx
        | _ -> false
      | NumberSlice (idx, value) ->
        match self with
        | NumberSlice (sidx, svalue) -> idx = sidx && value = svalue
        | _ -> false
      | BoolSlice   (idx, ot, value) ->
        match self with
        | BoolSlice (sidx, st, svalue) -> idx = sidx && value = svalue && ot = st
        | _ -> false
      | ByteSlice   (idx, value) ->
        match self with
        | ByteSlice (sidx, svalue) -> idx = sidx && value = svalue
        | _ -> false
      | EnumSlice (idx, value) ->
        match self with
        | EnumSlice (sidx, svalue) -> idx = sidx && value = svalue
        | _ -> false
      | ColorSlice (idx, value) ->
        match self with
        | ColorSlice (sidx, svalue) -> idx = sidx && value = svalue
        | _ -> false

  // ** StringValue

  member self.StringValue
    with get () =
      match self with
      | StringSlice (_,data) -> Some data
      | _                    -> None

  // ** NumberValue

  member self.NumberValue
    with get () =
      match self with
      | NumberSlice (_,data) -> Some data
      | _                    -> None

  // ** BoolValue

  member self.BoolValue
    with get () =
      match self with
      | BoolSlice (_,_,data) -> Some data
      | _ -> None

  // ** ByteValue

  member self.ByteValue
    with get () =
      match self with
      | ByteSlice (_,data) -> Some data
      | _                  -> None

  // ** EnumValue

  member self.EnumValue
    with get () =
      match self with
      | EnumSlice (_,data) -> Some data
      | _                  -> None

  // ** ColorValue

  member self.ColorValue
    with get () =
      match self with
      | ColorSlice (_,data) -> Some data
      | _                   -> None

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    match self with
    | StringSlice (idx, data) ->
      let str = Option.mapNull builder.CreateString data
      StringFB.StartStringFB(builder)
      Option.iter (fun data -> StringFB.AddValue(builder, data)) str
      let offset = StringFB.EndStringFB(builder)
      SliceFB.StartSliceFB(builder)
      SliceFB.AddIndex(builder, int idx)
      SliceFB.AddSliceType(builder, SliceTypeFB.StringFB)
      #if FABLE_COMPILER
      SliceFB.AddSlice(builder, offset)
      #else
      SliceFB.AddSlice(builder, offset.Value)
      #endif
      SliceFB.EndSliceFB(builder)

    | NumberSlice (idx, data) ->
      DoubleFB.StartDoubleFB(builder)
      DoubleFB.AddValue(builder, data)
      let offset = DoubleFB.EndDoubleFB(builder)
      SliceFB.StartSliceFB(builder)
      SliceFB.AddIndex(builder, int idx)
      SliceFB.AddSliceType(builder, SliceTypeFB.DoubleFB)
      #if FABLE_COMPILER
      SliceFB.AddSlice(builder, offset)
      #else
      SliceFB.AddSlice(builder, offset.Value)
      #endif
      SliceFB.EndSliceFB(builder)

    | BoolSlice (idx, trigger, data) ->
      BoolFB.StartBoolFB(builder)
      BoolFB.AddTrigger(builder,trigger)
      BoolFB.AddValue(builder,data)
      let offset = BoolFB.EndBoolFB(builder)
      SliceFB.StartSliceFB(builder)
      SliceFB.AddIndex(builder, int idx)
      SliceFB.AddSliceType(builder, SliceTypeFB.BoolFB)
      #if FABLE_COMPILER
      SliceFB.AddSlice(builder, offset)
      #else
      SliceFB.AddSlice(builder, offset.Value)
      #endif
      SliceFB.EndSliceFB(builder)

    | ByteSlice (idx, data) ->
      let str = data |> String.encodeBase64 |> builder.CreateString
      StringFB.StartStringFB(builder)
      StringFB.AddValue(builder,str)
      let offset = StringFB.EndStringFB(builder)
      SliceFB.StartSliceFB(builder)
      SliceFB.AddIndex(builder, int idx)
      SliceFB.AddSliceType(builder, SliceTypeFB.ByteFB)
      #if FABLE_COMPILER
      SliceFB.AddSlice(builder, offset)
      #else
      SliceFB.AddSlice(builder, offset.Value)
      #endif
      SliceFB.EndSliceFB(builder)

    | EnumSlice (idx, data) ->
      let offset: Offset<KeyValueFB> = data.ToOffset(builder)
      SliceFB.StartSliceFB(builder)
      SliceFB.AddIndex(builder, int idx)
      SliceFB.AddSliceType(builder, SliceTypeFB.KeyValueFB)
      #if FABLE_COMPILER
      SliceFB.AddSlice(builder, offset)
      #else
      SliceFB.AddSlice(builder, offset.Value)
      #endif
      SliceFB.EndSliceFB(builder)

    | ColorSlice (idx, data) ->
      let offset = data.ToOffset(builder)
      SliceFB.StartSliceFB(builder)
      SliceFB.AddIndex(builder, int idx)
      SliceFB.AddSliceType(builder, SliceTypeFB.ColorSpaceFB)
      #if FABLE_COMPILER
      SliceFB.AddSlice(builder, offset)
      #else
      SliceFB.AddSlice(builder, offset.Value)
      #endif
      SliceFB.EndSliceFB(builder)

  // ** FromFB

  static member FromFB(fb: SliceFB) : Either<DiscoError,Slice>  =
    match fb.SliceType with
    #if FABLE_COMPILER
    | x when x = SliceTypeFB.StringFB ->
      let slice = StringFB.Create() |> fb.Slice
      StringSlice(index fb.Index, slice.Value)
      |> Either.succeed

    | x when x = SliceTypeFB.DoubleFB ->
      let slice = DoubleFB.Create() |> fb.Slice
      NumberSlice(index fb.Index, slice.Value)
      |> Either.succeed

    | x when x = SliceTypeFB.BoolFB ->
      let slice = BoolFB.Create() |> fb.Slice
      BoolSlice(index fb.Index, slice.Trigger, slice.Value)
      |> Either.succeed

    | x when x = SliceTypeFB.ByteFB ->
      let slice = ByteFB.Create() |> fb.Slice
      ByteSlice(index fb.Index,String.decodeBase64 slice.Value)
      |> Either.succeed

    | x when x = SliceTypeFB.KeyValueFB ->
      either {
        let slice = KeyValueFB.Create() |> fb.Slice
        let! prop = Property.FromFB slice
        return EnumSlice(index fb.Index,prop)
      }

    | x when x = SliceTypeFB.ColorSpaceFB ->
      either {
        let slice = ColorSpaceFB.Create() |> fb.Slice
        let! color = ColorSpace.FromFB slice
        return ColorSlice(index fb.Index, color)
      }

    | x ->
      sprintf "Could not parse slice. Unknown slice type %A" x
      |> Error.asParseError "Slice.FromFB"
      |> Either.fail

    #else

    | SliceTypeFB.StringFB   ->
      let slice = fb.Slice<StringFB>()
      if slice.HasValue then
        let value = slice.Value
        StringSlice(index fb.Index, value.Value)
        |> Either.succeed
      else
        "Could not parse StringSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.DoubleFB   ->
      let slice = fb.Slice<DoubleFB>()
      if slice.HasValue then
        let value = slice.Value
        NumberSlice(index fb.Index,value.Value)
        |> Either.succeed
      else
        "Could not parse NumberSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.BoolFB     ->
      let slice = fb.Slice<BoolFB>()
      if slice.HasValue then
        let value = slice.Value
        BoolSlice(index fb.Index, value.Trigger, value.Value)
        |> Either.succeed
      else
        "Could not parse BoolSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.ByteFB     ->
      let slice = fb.Slice<ByteFB>()
      if slice.HasValue then
        let value = slice.Value
        ByteSlice(index fb.Index, String.decodeBase64 value.Value)
        |> Either.succeed
      else
        "Could not parse ByteSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.KeyValueFB     ->
      let slice = fb.Slice<KeyValueFB>()
      if slice.HasValue then
        either {
          let value = slice.Value
          let! prop = Property.FromFB value
          return EnumSlice(index fb.Index, prop)
        }
      else
        "Could not parse EnumSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | SliceTypeFB.ColorSpaceFB    ->
      let slice = fb.Slice<ColorSpaceFB>()
      if slice.HasValue then
        either {
          let value = slice.Value
          let! color = ColorSpace.FromFB value
          return ColorSlice(index fb.Index, color)
        }
      else
        "Could not parse ColorSlice"
        |> Error.asParseError "Slice.FromFB"
        |> Either.fail

    | x ->
      sprintf "Cannot parse slice. Unknown slice type: %A" x
      |> Error.asParseError "Slice.FromFB"
      |> Either.fail

    #endif

  // ** ToBytes

  member self.ToBytes() : byte[] = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<DiscoError,Slice> =
    Binary.createBuffer bytes
    |> SliceFB.GetRootAsSliceFB
    |> Slice.FromFB

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  member slice.ToYaml() = SliceYaml.ofSlice slice

  // ** FromYaml

  static member FromYaml(yml: SliceYaml) = SliceYaml.toSlice yml

  #endif

// * Slices

//  ____  _ _
// / ___|| (_) ___ ___  ___
// \___ \| | |/ __/ _ \/ __|
//  ___) | | | (_|  __/\__ \
// |____/|_|_|\___\___||___/

[<CustomEquality; CustomComparison>]
type Slices =
  | StringSlices of pin:PinId * client:ClientId option * values:string array
  | NumberSlices of pin:PinId * client:ClientId option * values:double array
  | BoolSlices   of pin:PinId * client:ClientId option * trigger:bool * values:bool array
  | ByteSlices   of pin:PinId * client:ClientId option * values:byte[] array
  | EnumSlices   of pin:PinId * client:ClientId option * values:Property array
  | ColorSlices  of pin:PinId * client:ClientId option * values:ColorSpace array

  // ** PinId

  member self.PinId
    with get () =
      match self with
      | StringSlices   (id,_,_)   -> id
      | NumberSlices   (id,_,_)   -> id
      | BoolSlices     (id,_,_,_) -> id
      | ByteSlices     (id,_,_)   -> id
      | EnumSlices     (id,_,_)   -> id
      | ColorSlices    (id,_,_)   -> id

  // ** ClientId

  member self.ClientId
    with get () =
      match self with
      | StringSlices   (_,id,_)   -> id
      | NumberSlices   (_,id,_)   -> id
      | BoolSlices     (_,id,_,_) -> id
      | ByteSlices     (_,id,_)   -> id
      | EnumSlices     (_,id,_)   -> id
      | ColorSlices    (_,id,_)   -> id

  // ** IsString

  member self.IsString
    with get () =
      match self with
      | StringSlices _ -> true
      |              _ -> false

  // ** IsNumber

  member self.IsNumber
    with get () =
      match self with
      | NumberSlices _ -> true
      |              _ -> false

  // ** IsBool

  member self.IsBool
    with get () =
      match self with
      | BoolSlices _ -> true
      |            _ -> false

  // ** IsTrigger

  member self.IsTrigger
    with get () =
      match self with
      | BoolSlices (_,_,trigger,_) -> trigger
      | _ -> false

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

  // ** Item

  //  ___ _
  // |_ _| |_ ___ _ __ ___
  //  | || __/ _ \ '_ ` _ \
  //  | || ||  __/ | | | | |
  // |___|\__\___|_| |_| |_|

  member self.Item (idx: Index) =
    match self with
    | StringSlices (_,_,arr)      -> StringSlice (idx, arr.[int idx])
    | NumberSlices (_,_,arr)      -> NumberSlice (idx, arr.[int idx])
    | BoolSlices   (_,_,trig,arr) -> BoolSlice   (idx, trig, arr.[int idx])
    | ByteSlices   (_,_,arr)      -> ByteSlice   (idx, arr.[int idx])
    | EnumSlices   (_,_,arr)      -> EnumSlice   (idx, arr.[int idx])
    | ColorSlices  (_,_,arr)      -> ColorSlice  (idx, arr.[int idx])

  // ** At

  member self.At (idx: Index) = self.Item idx

  member self.Length =
    match self with
    | StringSlices (_,_,arr) -> arr.Length
    | NumberSlices (_,_,arr) -> arr.Length
    | BoolSlices (_,_,_,arr) -> arr.Length
    | ByteSlices   (_,_,arr) -> arr.Length
    | EnumSlices   (_,_,arr) -> arr.Length
    | ColorSlices  (_,_,arr) -> arr.Length

  // ** Map

  //  __  __
  // |  \/  | __ _ _ __
  // | |\/| |/ _` | '_ \
  // | |  | | (_| | |_) |
  // |_|  |_|\__,_| .__/
  //              |_|

  member self.Map (f: Slice -> 'a) : 'a array =
    match self with
    | StringSlices (_,_,arr) -> Array.mapi (fun i el -> StringSlice (index i, el) |> f) arr
    | NumberSlices (_,_,arr) -> Array.mapi (fun i el -> NumberSlice (index i, el) |> f) arr
    | BoolSlices (_,_,t,arr) -> Array.mapi (fun i el -> BoolSlice   (index i, t, el) |> f) arr
    | ByteSlices   (_,_,arr) -> Array.mapi (fun i el -> ByteSlice   (index i, el) |> f) arr
    | EnumSlices   (_,_,arr) -> Array.mapi (fun i el -> EnumSlice   (index i, el) |> f) arr
    | ColorSlices  (_,_,arr) -> Array.mapi (fun i el -> ColorSlice  (index i, el) |> f) arr

  #if !FABLE_COMPILER

  // ** ToSpread

  member self.ToSpread() =
    let sb = new StringBuilder()
    match self with
    | StringSlices(_,_,arr) ->
      Array.iteri
        (fun i (str: string) ->
          let escape =
            if isNull str then false
            else str.IndexOf ' ' > -1
          let value =
            if isNull str || str.IndexOf '|' = -1 then
              str
            else
              str.Replace("|","||")
          if i > 0  then sb.Append ',' |> ignore
          if escape then sb.Append '|' |> ignore
          sb.Append value |> ignore
          if escape then sb.Append '|' |> ignore)
        arr
    | NumberSlices(_,_,arr) ->
      Array.iteri
        (fun i (num: double) ->
          if i > 0 then sb.Append ',' |> ignore
          num |> string |> sb.Append |> ignore)
        arr
    | BoolSlices(_,_,_,arr) ->
      Array.iteri
        (fun i (value: bool) ->
          if i > 0 then sb.Append ',' |> ignore
          match value with
          | true  -> "1" |> string |> sb.Append |> ignore
          | false -> "0" |> string |> sb.Append |> ignore)
        arr
    | ByteSlices(_,_,arr) ->
      Array.iteri
        (fun i (value: byte[]) ->
          if i > 0 then sb.Append ',' |> ignore
          sb.Append '|' |> ignore
          value |> String.encodeBase64 |> sb.Append |> ignore
          sb.Append '|' |> ignore)
        arr
    | EnumSlices(_,_,arr) ->
      Array.iteri
        (fun i (prop: Property) ->
          let escape = prop.Value.IndexOf ' ' > -1
          if i > 0  then sb.Append ',' |> ignore
          if escape then sb.Append '|' |> ignore
          prop.Value |> sb.Append |> ignore
          if escape then sb.Append '|' |> ignore)
        arr
    | ColorSlices(_,_,arr) ->
      Array.iteri
        (fun i (color: ColorSpace) ->
          if i > 0 then sb.Append ',' |> ignore
          sb.Append '|' |> ignore
          let rgba =
            match color with
            | RGBA rgba -> rgba
            | HSLA hsla -> hsla.ToRGBA()
          // Add F# string conversion which is culture invariant
          sb.Append(float rgba.Red / 255.0 |> string)
            .Append(',')
            .Append(float rgba.Green / 255.0 |> string)
            .Append(',')
            .Append(float rgba.Blue / 255.0 |> string)
            .Append(',')
            .Append(float rgba.Alpha / 255.0 |> string)
          |> ignore
          sb.Append '|' |> ignore)
        arr
    string sb

  #endif

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  member slices.ToYaml() = SlicesYaml.ofSlices slices

  // ** FromYaml

  static member FromYaml(yaml: SlicesYaml) = SlicesYaml.toSlices yaml

  #endif

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member slices.ToOffset(builder: FlatBufferBuilder) =
    let pid = slices.PinId
    let id = SlicesFB.CreatePinIdVector(builder,pid.ToByteArray())
    let client =
      Option.map
        (fun (clid: ClientId) -> SlicesFB.CreateClientIdVector(builder,clid.ToByteArray()))
        slices.ClientId
    match slices with
    | StringSlices (_,_,arr) ->
      let strings =
        Array.map (Pin.str2offset builder) arr
        |> fun coll -> StringsFB.CreateValuesVector(builder,coll)
      StringsFB.StartStringsFB(builder)
      StringsFB.AddValues(builder, strings)
      let offset = StringsFB.EndStringsFB(builder)

      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddPinId(builder,id)
      Option.iter (fun value -> SlicesFB.AddClientId(builder,value)) client
      SlicesFB.AddSlicesType(builder,SlicesTypeFB.StringsFB)
      #if FABLE_COMPILER
      SlicesFB.AddSlices(builder, offset)
      #else
      SlicesFB.AddSlices(builder, offset.Value)
      #endif
      SlicesFB.EndSlicesFB(builder)

    | NumberSlices (_,_,arr) ->
      let vector = DoublesFB.CreateValuesVector(builder, arr)
      DoublesFB.StartDoublesFB(builder)
      DoublesFB.AddValues(builder, vector)
      let offset = DoublesFB.EndDoublesFB(builder)

      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddPinId(builder,id)
      Option.iter (fun value -> SlicesFB.AddClientId(builder,value)) client
      SlicesFB.AddSlicesType(builder,SlicesTypeFB.DoublesFB)
      #if FABLE_COMPILER
      SlicesFB.AddSlices(builder,offset)
      #else
      SlicesFB.AddSlices(builder,offset.Value)
      #endif
      SlicesFB.EndSlicesFB(builder)

    | BoolSlices (_,_,t,arr) ->
      let vector = BoolsFB.CreateValuesVector(builder, arr)
      BoolsFB.StartBoolsFB(builder)
      BoolsFB.AddTrigger(builder, t)
      BoolsFB.AddValues(builder, vector)
      let offset = BoolsFB.EndBoolsFB(builder)

      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddPinId(builder,id)
      Option.iter (fun value -> SlicesFB.AddClientId(builder,value)) client
      SlicesFB.AddSlicesType(builder,SlicesTypeFB.BoolsFB)
      #if FABLE_COMPILER
      SlicesFB.AddSlices(builder,offset)
      #else
      SlicesFB.AddSlices(builder,offset.Value)
      #endif
      SlicesFB.EndSlicesFB(builder)

    | ByteSlices (_,_,arr) ->
      let vector =
        Array.map (String.encodeBase64 >> builder.CreateString) arr
        |> fun coll -> BytesFB.CreateValuesVector(builder, coll)

      BytesFB.StartBytesFB(builder)
      BytesFB.AddValues(builder, vector)
      let offset = BytesFB.EndBytesFB(builder)

      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddPinId(builder,id)
      Option.iter (fun value -> SlicesFB.AddClientId(builder,value)) client
      SlicesFB.AddSlicesType(builder,SlicesTypeFB.BytesFB)
      #if FABLE_COMPILER
      SlicesFB.AddSlices(builder,offset)
      #else
      SlicesFB.AddSlices(builder,offset.Value)
      #endif
      SlicesFB.EndSlicesFB(builder)

    | EnumSlices (_,_,arr) ->
      let vector =
        Array.map (Binary.toOffset builder) arr
        |> fun coll -> KeyValuesFB.CreateValuesVector(builder, coll)

      KeyValuesFB.StartKeyValuesFB(builder)
      KeyValuesFB.AddValues(builder,vector)
      let offset = KeyValuesFB.EndKeyValuesFB(builder)

      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddPinId(builder,id)
      Option.iter (fun value -> SlicesFB.AddClientId(builder,value)) client
      SlicesFB.AddSlicesType(builder,SlicesTypeFB.KeyValuesFB)
      #if FABLE_COMPILER
      SlicesFB.AddSlices(builder,offset)
      #else
      SlicesFB.AddSlices(builder,offset.Value)
      #endif
      SlicesFB.EndSlicesFB(builder)

    | ColorSlices (_,_,arr) ->
      let vector =
        Array.map (Binary.toOffset builder) arr
        |> fun coll -> ColorSpacesFB.CreateValuesVector(builder,coll)

      ColorSpacesFB.StartColorSpacesFB(builder)
      ColorSpacesFB.AddValues(builder, vector)
      let offset = ColorSpacesFB.EndColorSpacesFB(builder)

      SlicesFB.StartSlicesFB(builder)
      SlicesFB.AddPinId(builder,id)
      Option.iter (fun value -> SlicesFB.AddClientId(builder,value)) client
      SlicesFB.AddSlicesType(builder,SlicesTypeFB.ColorSpacesFB)
      #if FABLE_COMPILER
      SlicesFB.AddSlices(builder,offset)
      #else
      SlicesFB.AddSlices(builder,offset.Value)
      #endif
      SlicesFB.EndSlicesFB(builder)

  // ** FromFB

  static member inline FromFB(fb: SlicesFB) : Either<DiscoError,Slices> =
    either {
      let! id = Id.decodePinId fb
      let! client =
        try
          if fb.ClientIdLength = 0
          then Either.succeed None
          else Id.decodeClientId fb |> Either.map Some
        with exn ->
          Either.succeed None
      return!
        //      _ ____
        //     | / ___|
        //  _  | \___ \
        // | |_| |___) |
        //  \___/|____/
        #if FABLE_COMPILER
        match fb.SlicesType with
        | x when x = SlicesTypeFB.StringsFB ->
          let slices = StringsFB.Create() |> fb.Slices
          let arr = Array.zeroCreate slices.ValuesLength
          Array.fold
            (fun (m: Either<DiscoError,string array * int>) _ -> either {
                let! (parsed,idx) = m
                parsed.[idx] <- slices.Values(idx)
                return parsed, idx + 1 })
            (Right (arr, 0))
            arr
          |> Either.map (fun (strings, _) -> StringSlices(id,client,strings))
        | x when x = SlicesTypeFB.DoublesFB ->
          let slices = DoublesFB.Create() |> fb.Slices
          let arr = Array.zeroCreate slices.ValuesLength
          Array.fold
            (fun (m: Either<DiscoError,double array * int>) _ -> either {
                let! (parsed,idx) = m
                parsed.[idx] <- slices.Values(idx)
                return parsed, idx + 1 })
            (Right (arr, 0))
            arr
          |> Either.map (fun (doubles,_) -> NumberSlices(id,client,doubles))
        | x when x = SlicesTypeFB.BoolsFB ->
          let slices = BoolsFB.Create() |> fb.Slices
          let arr = Array.zeroCreate slices.ValuesLength
          Array.fold
            (fun (m: Either<DiscoError,bool array * int>) _ -> either {
                let! (parsed,idx) = m
                parsed.[idx] <- slices.Values(idx)
                return parsed, idx + 1 })
            (Right (arr, 0))
            arr
          |> Either.map (fun (bools,_) -> BoolSlices(id,client,slices.Trigger,bools))
        | x when x = SlicesTypeFB.BytesFB ->
          let slices = BytesFB.Create() |> fb.Slices
          let arr = Array.zeroCreate slices.ValuesLength
          Array.fold
            (fun (m: Either<DiscoError,byte[] array * int>) _ -> either {
                let! (parsed,idx) = m
                let bytes = slices.Values(idx) |> String.decodeBase64
                parsed.[idx] <- bytes
                return parsed, idx + 1 })
            (Right (arr, 0))
            arr
          |> Either.map (fun (bytes,_) -> ByteSlices(id,client,bytes))
        | x when x = SlicesTypeFB.KeyValuesFB ->
          let slices = KeyValuesFB.Create() |> fb.Slices
          let arr = Array.zeroCreate slices.ValuesLength
          Array.fold
            (fun (m: Either<DiscoError,Property array * int>) _ -> either {
                let! (parsed,idx) = m
                let! prop =
                  let value = slices.Values(idx)
                  Property.FromFB value
                parsed.[idx] <- prop
                return parsed, idx + 1 })
            (Right (arr, 0))
            arr
          |> Either.map (fun (props,_) -> EnumSlices(id,client,props))
        | x when x = SlicesTypeFB.ColorSpacesFB ->
          let slices = ColorSpacesFB.Create() |> fb.Slices
          let arr = Array.zeroCreate slices.ValuesLength
          Array.fold
            (fun (m: Either<DiscoError,ColorSpace array * int>) _ -> either {
                let! (parsed,idx) = m
                let! color =
                  let value = slices.Values(idx)
                  ColorSpace.FromFB value
                parsed.[idx] <- color
                return parsed, idx + 1 })
            (Right (arr, 0))
            arr
          |> Either.map (fun (colors,_) -> ColorSlices(id,client,colors))
        | x ->
          sprintf "unknown slices type: %O" x
          |> Error.asParseError "Slices.FromFB"
          |> Either.fail

        //    _   _ _____ _____
        //   | \ | | ____|_   _|
        //   |  \| |  _|   | |
        //  _| |\  | |___  | |
        // (_)_| \_|_____| |_|

        #else

        match fb.SlicesType with
        | SlicesTypeFB.StringsFB ->
          let slicesish = fb.Slices<StringsFB>()
          if slicesish.HasValue then
            let slices = slicesish.Value
            let arr = Array.zeroCreate slices.ValuesLength
            Array.fold
              (fun (m: Either<DiscoError,string array * int>) _ -> either {
                  let! (parsed,idx) = m
                  let value =
                    try slices.Values(idx)
                    with | _ -> null
                  parsed.[idx] <- value
                  return parsed, idx + 1 })
              (Right (arr, 0))
              arr
            |> Either.map (fun (strings, _) -> StringSlices(id, client, strings))
          else
            "empty slices value"
            |> Error.asParseError "Slices.FromFB"
            |> Either.fail
        | SlicesTypeFB.DoublesFB ->
          let slicesish = fb.Slices<DoublesFB>()
          if slicesish.HasValue then
            let slices = slicesish.Value
            let arr = Array.zeroCreate slices.ValuesLength
            Array.fold
              (fun (m: Either<DiscoError,double array * int>) _ -> either {
                  let! (parsed,idx) = m
                  parsed.[idx] <- slices.Values(idx)
                  return parsed, idx + 1 })
              (Right (arr, 0))
              arr
            |> Either.map (fun (doubles,_) -> NumberSlices(id, client, doubles))
          else
            "empty slices value"
            |> Error.asParseError "Slices.FromFB"
            |> Either.fail
        | SlicesTypeFB.BoolsFB ->
          let slicesish = fb.Slices<BoolsFB>()
          if slicesish.HasValue then
            let slices = slicesish.Value
            let arr = Array.zeroCreate slices.ValuesLength
            Array.fold
              (fun (m: Either<DiscoError,bool array * int>) _ -> either {
                  let! (parsed,idx) = m
                  parsed.[idx] <- slices.Values(idx)
                  return parsed, idx + 1 })
              (Right (arr, 0))
              arr
            |> Either.map (fun (bools,_) -> BoolSlices(id, client, slices.Trigger, bools))
          else
            "empty slices value"
            |> Error.asParseError "Slices.FromFB"
            |> Either.fail
        | SlicesTypeFB.BytesFB ->
          let slicesish = fb.Slices<BytesFB>()
          if slicesish.HasValue then
            let slices = slicesish.Value
            let arr = Array.zeroCreate slices.ValuesLength
            Array.fold
              (fun (m: Either<DiscoError,byte[] array * int>) _ -> either {
                  let! (parsed,idx) = m
                  let bytes = slices.Values(idx) |> String.decodeBase64
                  parsed.[idx] <- bytes
                  return parsed, idx + 1 })
              (Right (arr, 0))
              arr
            |> Either.map (fun (bytes,_) -> ByteSlices(id, client, bytes))
          else
            "empty slices value"
            |> Error.asParseError "Slices.FromFB"
            |> Either.fail
        | SlicesTypeFB.KeyValuesFB ->
          let slicesish = fb.Slices<KeyValuesFB>()
          if slicesish.HasValue then
            let slices = slicesish.Value
            let arr = Array.zeroCreate slices.ValuesLength
            Array.fold
              (fun (m: Either<DiscoError,Property array * int>) _ -> either {
                  let! (parsed,idx) = m
                  let! prop =
                    let propish = slices.Values(idx)
                    if propish.HasValue then
                      let value = propish.Value
                      Property.FromFB value
                    else
                      "could not parse empty property"
                      |> Error.asParseError "Slices.FromFB"
                      |> Either.fail
                  parsed.[idx] <- prop
                  return parsed, idx + 1 })
              (Right (arr, 0))
              arr
            |> Either.map (fun (props,_) -> EnumSlices(id, client, props))
          else
            "empty slices value"
            |> Error.asParseError "Slices.FromFB"
            |> Either.fail
        | SlicesTypeFB.ColorSpacesFB ->
          let slicesish = fb.Slices<ColorSpacesFB>()
          if slicesish.HasValue then
            let slices = slicesish.Value
            let arr = Array.zeroCreate slices.ValuesLength
            Array.fold
              (fun (m: Either<DiscoError,ColorSpace array * int>) _ -> either {
                  let! (parsed,idx) = m
                  let! color =
                    let colorish = slices.Values(idx)
                    if colorish.HasValue then
                      let value = colorish.Value
                      ColorSpace.FromFB value
                    else
                      "could not parse empty colorspace"
                      |> Error.asParseError "Slices.FromFB"
                      |> Either.fail
                  parsed.[idx] <- color
                  return parsed, idx + 1 })
              (Right (arr, 0))
              arr
            |> Either.map (fun (colors,_) -> ColorSlices(id,client,colors))
          else
            "empty slices value"
            |> Error.asParseError "Slices.FromFB"
            |> Either.fail
        | x ->
          sprintf "unknown slices type: %O" x
          |> Error.asParseError "Slices.FromFB"
          |> Either.fail
        #endif
    }

  // ** ToBytes

  member slices.ToBytes() : byte[] =
    Binary.buildBuffer slices

  // ** FromBytes

  static member FromBytes(raw: byte[]) : Either<DiscoError,Slices> =
    Binary.createBuffer raw
    |> SlicesFB.GetRootAsSlicesFB
    |> Slices.FromFB

  // ** CompareTo

  interface System.IComparable with
    member self.CompareTo other =
      match other with
      | :? Slices as slices -> compare self.PinId slices.PinId
      | _ -> invalidArg "other" "cannot compare value of different types"

  // ** Equals

  override self.Equals(other) =
    match other with
    | :? Slices as slices -> (self :> System.IEquatable<Slices>).Equals(slices)
    | _ -> false

  override self.GetHashCode() =
    let id = self.PinId
    in id.GetHashCode()

  // ** Equals<Slices>

  interface System.IEquatable<Slices> with
    member self.Equals(slices: Slices) =
      match slices with
      | StringSlices (id, client, values) ->
        match self with
        | StringSlices (sid, sclient, svalues) when id = sid && client = sclient -> values = svalues
        | _ -> false
      | NumberSlices (id, client, values) ->
        match self with
        | NumberSlices (sid, sclient, svalues) when id = sid && client = sclient ->
          if Array.length values = Array.length svalues then
            Array.fold
              (fun m (left,right) ->
                if m then
                  match left, right with
                  | _,_ when Double.IsNaN left && Double.IsNaN right  -> true
                  | _,_ when left = right -> true
                  | _ -> false
                else m)
              true
              (Array.zip values svalues)
          else false
        | _ -> false
      | BoolSlices  (id, client, otrig, values) ->
        match self with
        | BoolSlices (sid, sclient, strig, svalues) ->
          id = sid && client = sclient && otrig = strig && values = svalues
        | _ -> false
      | ByteSlices  (id, client, values) ->
        match self with
        | ByteSlices (sid, sclient, svalues) when id = sid && client = sclient -> values = svalues
        | _ -> false
      | EnumSlices (id, client, values) ->
        match self with
        | EnumSlices (sid, sclient, svalues) when id = sid && client = sclient -> values = svalues
        | _ -> false
      | ColorSlices (id, client, values) ->
        match self with
        | ColorSlices (sid, sclient, svalues) when id = sid && client = sclient -> values = svalues
        | _ -> false


// * Slices module

module Slices =

  // ** setId

  let setId id = function
    | StringSlices (_,c,values) -> StringSlices (id,c,values)
    | NumberSlices (_,c,values) -> NumberSlices (id,c,values)
    | BoolSlices (_,c,t,values) -> BoolSlices   (id,c,t,values)
    | ByteSlices   (_,c,values) -> ByteSlices   (id,c,values)
    | EnumSlices   (_,c,values) -> EnumSlices   (id,c,values)
    | ColorSlices  (_,c,values) -> ColorSlices  (id,c,values)

  // ** setClient

  let setClient id = function
    | StringSlices (i,_,values) -> StringSlices (i,id,values)
    | NumberSlices (i,_,values) -> NumberSlices (i,id,values)
    | BoolSlices (i,_,t,values) -> BoolSlices   (i,id,t,values)
    | ByteSlices   (i,_,values) -> ByteSlices   (i,id,values)
    | EnumSlices   (i,_,values) -> EnumSlices   (i,id,values)
    | ColorSlices  (i,_,values) -> ColorSlices  (i,id,values)

// * SliceYaml

#if !FABLE_COMPILER && !DISCO_NODES

//  ____  _ _        __   __              _
// / ___|| (_) ___ __\ \ / /_ _ _ __ ___ | |
// \___ \| | |/ __/ _ \ V / _` | '_ ` _ \| |
//  ___) | | | (_|  __/| | (_| | | | | | | |
// |____/|_|_|\___\___||_|\__,_|_| |_| |_|_|

type SliceYaml(tipe, idx, trig, value: obj) as self =
  [<DefaultValue>] val mutable SliceType : string
  [<DefaultValue>] val mutable Index     : int
  [<DefaultValue>] val mutable Trigger   : bool
  [<DefaultValue>] val mutable Value     : obj

  new () = SliceYaml(null,0, false, null)

  do
    self.SliceType <- tipe
    self.Index     <- idx
    self.Trigger   <- trig
    self.Value     <- value

  static member StringSlice (idx: int) (value: string) =
    SliceYaml("StringSlice", idx, false, value)

  static member NumberSlice (idx: int) (value: double) =
    SliceYaml("NumberSlice", idx, false, value)

  static member BoolSlice idx trig (value: bool) =
    SliceYaml("BoolSlice", idx, trig, value)

  static member ByteSlice idx (value: byte array) =
    SliceYaml("ByteSlice", idx, false, Convert.ToBase64String(value))

  static member EnumSlice idx (value: Property) =
    SliceYaml("EnumSlice", idx, false, Yaml.toYaml value)

  static member ColorSlice idx (value: ColorSpace) =
    SliceYaml("ColorSlice", idx, false, Yaml.toYaml value)

// * SliceYaml module

module SliceYaml =

  // ** ofSlice

  let ofSlice = function
      | StringSlice (idx, slice)  -> SliceYaml.StringSlice (int idx) slice
      | NumberSlice (idx, slice)  -> SliceYaml.NumberSlice (int idx) slice
      | BoolSlice (idx, t, slice) -> SliceYaml.BoolSlice (int idx) t slice
      | ByteSlice (idx, slice)    -> SliceYaml.ByteSlice (int idx) slice
      | EnumSlice (idx, slice)    -> SliceYaml.EnumSlice (int idx) slice
      | ColorSlice (idx, slice)   -> SliceYaml.ColorSlice (int idx) slice

  // ** toSlice

  let toSlice (yml: SliceYaml) : Either<DiscoError,Slice> =
    match yml.SliceType with
    | "StringSlice" ->
      Either.tryWith (Error.asParseError "SliceYaml.ToSlice (String)") <| fun _ ->
        let parse (str: obj) =
          match str with
          | null -> null
          | _ -> str :?> String
        StringSlice(index yml.Index, parse yml.Value)
    | "NumberSlice" ->
      Either.tryWith (Error.asParseError "SliceYaml.ToSlice (Number)") <| fun _ ->
        let parse (value: obj) =
          try
            match value with
            | :? Double -> value :?> Double
            | :? String when (value :?> string).Contains "-Infinity" -> Double.NegativeInfinity
            | :? String when (value :?> string).Contains "Infinity" -> Double.PositiveInfinity
            | :? String when (value :?> string).Contains "NaN" -> Double.NaN
            | _ -> 0.0
          with
            | exn ->
              exn.Message
              |> sprintf "normalizing to 0.0. offending value: %A reason: %s" value
              |> Logger.err "toSlices (Number)"
              0.0
        NumberSlice(index yml.Index, parse yml.Value)
    | "BoolSlice" ->
      Either.tryWith (Error.asParseError "SliceYaml.ToSlice (Bool)") <| fun _ ->
        BoolSlice(index yml.Index, yml.Trigger, yml.Value :?> bool)
    | "ByteSlice" ->
      Either.tryWith (Error.asParseError "SliceYaml.ToSlice (Byte)") <| fun _ ->
        ByteSlice(index yml.Index, yml.Value |> string |> Convert.FromBase64String)
    | "EnumSlice" ->
      Either.tryWith (Error.asParseError "SliceYaml.ToSlice (Enum)") <| fun _ ->
        let pyml = yml.Value :?> PropertyYaml
        EnumSlice(index yml.Index, { Key = pyml.Key; Value = pyml.Value })
    | "ColorSlice" ->
      either {
        let! color = Yaml.fromYaml(yml.Value :?> ColorYaml)
        return ColorSlice(index yml.Index, color)
      }
    | unknown ->
      sprintf "Could not de-serialize unknown type: %A" unknown
      |> Error.asParseError "SliceYaml.ToSlice"
      |> Either.fail

// * SlicesYaml

//  ____  _ _             __   __              _
// / ___|| (_) ___ ___  __\ \ / /_ _ _ __ ___ | |
// \___ \| | |/ __/ _ \/ __\ V / _` | '_ ` _ \| |
//  ___) | | | (_|  __/\__ \| | (_| | | | | | | |
// |____/|_|_|\___\___||___/|_|\__,_|_| |_| |_|_|

type SlicesYaml(tipe, pinid, clientid, trig, values: obj array) as self =
  [<DefaultValue>] val mutable PinId: string
  [<DefaultValue>] val mutable ClientId: string
  [<DefaultValue>] val mutable Trigger: bool
  [<DefaultValue>] val mutable SliceType: string
  [<DefaultValue>] val mutable Values: obj array

  new () = SlicesYaml(null,null,null,false,null)

  do
    self.PinId     <- pinid
    self.ClientId  <- clientid
    self.SliceType <- tipe
    self.Trigger   <- trig
    self.Values    <- values

  static member StringSlices id client (values: string array) =
    SlicesYaml("StringSlices", id, client, false, Array.map box values)

  static member NumberSlices id client (values: double array) =
    SlicesYaml("NumberSlices", id, client, false, Array.map box values)

  static member BoolSlices id client trig (values: bool array) =
    SlicesYaml("BoolSlices", id, client, trig, Array.map box values)

  static member ByteSlices id client (values: byte array array) =
    SlicesYaml("ByteSlices", id, client, false, Array.map (Convert.ToBase64String >> box) values)

  static member EnumSlices id client (values: Property array) =
    SlicesYaml("EnumSlices", id, client, false, Array.map (Yaml.toYaml >> box) values)

  static member ColorSlices id client (values: ColorSpace array) =
    SlicesYaml("ColorSlices", id, client, false, Array.map (Yaml.toYaml >> box) values)

// * SlicesYaml module

module SlicesYaml =

  // ** ofSlices

  let ofSlices (slices: Slices) =
    let client =
      match slices.ClientId with
      | Some id -> string id
      | None -> null
    in
    match slices with
    | StringSlices (id, _, slices) -> SlicesYaml.StringSlices (string id) client   slices
    | NumberSlices (id, _, slices) -> SlicesYaml.NumberSlices (string id) client   slices
    | BoolSlices(id, _, t, slices) -> SlicesYaml.BoolSlices   (string id) client t slices
    | ByteSlices   (id, _, slices) -> SlicesYaml.ByteSlices   (string id) client   slices
    | EnumSlices   (id, _, slices) -> SlicesYaml.EnumSlices   (string id) client   slices
    | ColorSlices  (id, _, slices) -> SlicesYaml.ColorSlices  (string id) client   slices

  // ** toSlices

  let toSlices (yml: SlicesYaml) =
    match yml.SliceType with
    | "StringSlices" ->
      Either.tryWith (Error.asParseError "SlicesYaml.ToSlice (String)") <| fun _ ->
        let parse (str: obj) =
          match str with
          | null -> null
          | _ -> str :?> String
        let client = if isNull yml.ClientId then None else Some (DiscoId.Parse yml.ClientId)
        StringSlices(DiscoId.Parse yml.PinId, client, Array.map parse yml.Values)
    | "NumberSlices" ->
      Either.tryWith (Error.asParseError "SlicesYaml.ToSlice (Number)") <| fun _ ->
        let parse (value: obj) =
          try
            match value with
            | :? Double -> value :?> Double
            | :? String when (value :?> string).Contains "-Infinity" -> Double.NegativeInfinity
            | :? String when (value :?> string).Contains "Infinity" -> Double.PositiveInfinity
            | :? String when (value :?> string).Contains "NaN" -> Double.NaN
            | _ -> 0.0
          with
            | exn ->
              exn.Message
              |> sprintf "normalizing to 0.0. offending value: %A reason: %s" value
              |> Logger.err "toSlices (Number)"
              0.0
        let client = if isNull yml.ClientId then None else Some (DiscoId.Parse yml.ClientId)
        NumberSlices(DiscoId.Parse yml.PinId, client, Array.map parse yml.Values)
    | "BoolSlices" ->
      Either.tryWith (Error.asParseError "SlicesYaml.ToSlice (Bool)") <| fun _ ->
        let client = if isNull yml.ClientId then None else Some (DiscoId.Parse yml.ClientId)
        BoolSlices(DiscoId.Parse yml.PinId, client, yml.Trigger, Array.map unbox<bool> yml.Values)
    | "ByteSlices" ->
      Either.tryWith (Error.asParseError "SlicesYaml.ToSlice (Byte)") <| fun _ ->
        let parse (value: obj) =
          match value with
          | :? String -> (value :?> String) |> Convert.FromBase64String
          | :? Double -> (value :?> Double) |> BitConverter.GetBytes
          | :? Int32  -> (value :?> Int32)  |> BitConverter.GetBytes
          | other ->
            printfn "(ByteSlices): offending value: %A" other
            printfn "(ByteSlices): type of offending value: %A" (other.GetType())
            [| |]
        let client = if isNull yml.ClientId then None else Some (DiscoId.Parse yml.ClientId)
        ByteSlices(DiscoId.Parse yml.PinId, client, Array.map parse yml.Values)
    | "EnumSlices" ->
      Either.tryWith (Error.asParseError "SlicesYaml.ToSlice (Enum)") <| fun _ ->
        let ofPyml (o: obj) =
          let pyml: PropertyYaml = unbox o
          { Key = pyml.Key; Value = pyml.Value }
        let client = if isNull yml.ClientId then None else Some (DiscoId.Parse yml.ClientId)
        EnumSlices(DiscoId.Parse yml.PinId, client, Array.map ofPyml yml.Values)
    | "ColorSlices" ->
      either {
        let! colors =
          Array.fold
            (fun (m: Either<DiscoError,int * ColorSpace array>) value -> either {
              let! (idx, colors) = m
              let unboxed: ColorYaml = unbox value
              let! color = Yaml.fromYaml unboxed
              colors.[idx] <- color
              return (idx + 1, colors)
              })
            (Right(0, Array.zeroCreate yml.Values.Length))
            yml.Values
          |> Either.map snd
        let client = if isNull yml.ClientId then None else Some (DiscoId.Parse yml.ClientId)
        return ColorSlices(DiscoId.Parse yml.PinId, client, colors)
      }
    | unknown ->
      sprintf "Could not de-serialize unknown type: %A" unknown
      |> Error.asParseError "SlicesYaml.ToSlice"
      |> Either.fail


// * PinYaml

type PinYaml() =
  [<DefaultValue>] val mutable PinType          : string
  [<DefaultValue>] val mutable Id               : string
  [<DefaultValue>] val mutable Name             : string
  [<DefaultValue>] val mutable PinGroupId       : string
  [<DefaultValue>] val mutable ClientId         : string
  [<DefaultValue>] val mutable Tags             : PropertyYaml array
  [<DefaultValue>] val mutable Persisted        : bool
  [<DefaultValue>] val mutable Online           : bool
  [<DefaultValue>] val mutable Behavior         : string
  [<DefaultValue>] val mutable PinConfiguration : string
  [<DefaultValue>] val mutable MaxChars         : int
  [<DefaultValue>] val mutable IsTrigger        : bool
  [<DefaultValue>] val mutable VecSize          : string
  [<DefaultValue>] val mutable Precision        : uint32
  [<DefaultValue>] val mutable Min              : int
  [<DefaultValue>] val mutable Max              : int
  [<DefaultValue>] val mutable Unit             : string
  [<DefaultValue>] val mutable Properties       : PropertyYaml array
  [<DefaultValue>] val mutable Labels           : string array
  [<DefaultValue>] val mutable Values           : SliceYaml array

// * PinYaml module

module PinYaml =

  // ** ofPin

  let ofPin = function
    | StringPin data ->
      let yaml = PinYaml()
      yaml.PinType          <- "StringPin"
      yaml.Id               <- string data.Id
      yaml.Name             <- unwrap data.Name
      yaml.PinGroupId       <- string data.PinGroupId
      yaml.ClientId         <- string data.ClientId
      yaml.Persisted        <- data.Persisted
      yaml.Online           <- data.Online
      yaml.Tags             <- Array.map Yaml.toYaml data.Tags
      yaml.MaxChars         <- int data.MaxChars
      yaml.Behavior         <- string data.Behavior
      yaml.PinConfiguration <- string data.PinConfiguration
      yaml.VecSize          <- string data.VecSize
      yaml.Labels           <- data.Labels
      yaml.Values           <- Array.mapi SliceYaml.StringSlice data.Values
      yaml

    | NumberPin data ->
      let yaml = PinYaml()
      yaml.PinType          <- "NumberPin"
      yaml.Id               <- string data.Id
      yaml.Name             <- unwrap data.Name
      yaml.PinGroupId       <- string data.PinGroupId
      yaml.ClientId         <- string data.ClientId
      yaml.Persisted        <- data.Persisted
      yaml.Online           <- data.Online
      yaml.Tags             <- Array.map Yaml.toYaml data.Tags
      yaml.Precision        <- data.Precision
      yaml.Min              <- data.Min
      yaml.Max              <- data.Max
      yaml.Unit             <- data.Unit
      yaml.VecSize          <- string data.VecSize
      yaml.PinConfiguration <- string data.PinConfiguration
      yaml.Labels           <- data.Labels
      yaml.Values           <- Array.mapi SliceYaml.NumberSlice data.Values
      yaml

    | BoolPin data ->
      let yaml = PinYaml()
      yaml.PinType          <- "BoolPin"
      yaml.Id               <- string data.Id
      yaml.Name             <- unwrap data.Name
      yaml.PinGroupId       <- string data.PinGroupId
      yaml.ClientId         <- string data.ClientId
      yaml.Persisted        <- data.Persisted
      yaml.Online           <- data.Online
      yaml.Tags             <- Array.map Yaml.toYaml data.Tags
      yaml.IsTrigger        <- data.IsTrigger
      yaml.VecSize          <- string data.VecSize
      yaml.PinConfiguration <- string data.PinConfiguration
      yaml.Labels           <- data.Labels
      yaml.Values           <- Array.mapi (fun i v -> SliceYaml.BoolSlice i data.IsTrigger v) data.Values
      yaml

    | BytePin data ->
      let yaml = PinYaml()
      yaml.PinType          <- "BytePin"
      yaml.Id               <- string data.Id
      yaml.Name             <- unwrap data.Name
      yaml.PinGroupId       <- string data.PinGroupId
      yaml.ClientId         <- string data.ClientId
      yaml.Persisted        <- data.Persisted
      yaml.Online           <- data.Online
      yaml.Tags             <- Array.map Yaml.toYaml data.Tags
      yaml.VecSize          <- string data.VecSize
      yaml.PinConfiguration <- string data.PinConfiguration
      yaml.Labels           <- data.Labels
      yaml.Values           <- Array.mapi SliceYaml.ByteSlice data.Values
      yaml

    | EnumPin data ->
      let yaml = PinYaml()
      yaml.PinType          <- "EnumPin"
      yaml.Id               <- string data.Id
      yaml.Name             <- unwrap data.Name
      yaml.PinGroupId       <- string data.PinGroupId
      yaml.ClientId         <- string data.ClientId
      yaml.Persisted        <- data.Persisted
      yaml.Online           <- data.Online
      yaml.Tags             <- Array.map Yaml.toYaml data.Tags
      yaml.VecSize          <- string data.VecSize
      yaml.PinConfiguration <- string data.PinConfiguration
      yaml.Properties       <- Array.map Yaml.toYaml data.Properties
      yaml.Labels           <- data.Labels
      yaml.Values           <- Array.mapi SliceYaml.EnumSlice data.Values
      yaml

    | ColorPin  data ->
      let yaml = PinYaml()
      yaml.PinType          <- "ColorPin"
      yaml.Id               <- string data.Id
      yaml.Name             <- unwrap data.Name
      yaml.PinGroupId       <- string data.PinGroupId
      yaml.ClientId         <- string data.ClientId
      yaml.Persisted        <- data.Persisted
      yaml.Online           <- data.Online
      yaml.Tags             <- Array.map Yaml.toYaml data.Tags
      yaml.VecSize          <- string data.VecSize
      yaml.PinConfiguration <- string data.PinConfiguration
      yaml.Labels           <- data.Labels
      yaml.Values           <- Array.mapi SliceYaml.ColorSlice data.Values
      yaml


  // ** toPin

  let toPin (yml: PinYaml) =
    let parseTags (yaml: PinYaml) =
      Array.fold
        (fun (m: Either<DiscoError, int * Property array>) yml ->
          either {
            let! state = m
            let! parsed = Yaml.fromYaml yml
            (snd state).[fst state] <- parsed
            return (fst state + 1, snd state)
          })
        (Right (0, Array.zeroCreate yaml.Tags.Length))
        yaml.Tags
      |> Either.map snd

    try
      match yml.PinType with
      | "StringPin" ->
        either {
          let! id = DiscoId.TryParse yml.Id
          let! group = DiscoId.TryParse yml.PinGroupId
          let! client = DiscoId.TryParse yml.ClientId
          let! strtype = Behavior.TryParse yml.Behavior
          let! dir = PinConfiguration.TryParse yml.PinConfiguration
          let! tags = parseTags yml
          let! vecsize = VecSize.TryParse yml.VecSize
          let! (_, slices) =
            let arr = Array.zeroCreate yml.Values.Length
            Array.fold
              (fun (m: Either<DiscoError,int * string array>) (yml: SliceYaml) ->
                either {
                  let! (i, arr) = m
                  let! value = SliceYaml.toSlice yml
                  arr.[i] <- (value.Value :?> String)
                  return (i + 1, arr)
                })
              (Right(0, arr))
              yml.Values

          return StringPin {
            Id               = id
            Name             = name yml.Name
            PinGroupId       = group
            ClientId         = client
            Tags             = tags
            Persisted        = yml.Persisted
            Online           = yml.Online
            Dirty            = false
            MaxChars         = yml.MaxChars * 1<chars>
            Behavior         = strtype
            VecSize          = vecsize
            PinConfiguration = dir
            Labels           = yml.Labels
            Values           = slices
          }
        }

      | "NumberPin" -> either {
          let! id = DiscoId.TryParse yml.Id
          let! group = DiscoId.TryParse yml.PinGroupId
          let! client = DiscoId.TryParse yml.ClientId
          let! dir = PinConfiguration.TryParse yml.PinConfiguration
          let! tags = parseTags yml
          let! vecsize = VecSize.TryParse yml.VecSize
          let! (_, slices) =
            let arr = Array.zeroCreate yml.Values.Length
            Array.fold
              (fun (m: Either<DiscoError,int * double array>) (yml: SliceYaml) ->
                either {
                  let! (i, arr) = m
                  let! value = SliceYaml.toSlice yml
                  let! value =
                    try value.Value :?> double |> Either.succeed
                    with | x ->
                      sprintf "Could not parse double: %s" x.Message
                      |> Error.asParseError "FromYaml NumberPin"
                      |> Either.fail
                  arr.[i] <- value
                  return (i + 1, arr)
                })
              (Right(0, arr))
              yml.Values

          return NumberPin {
            Id               = id
            Name             = name yml.Name
            PinGroupId       = group
            ClientId         = client
            Tags             = tags
            VecSize          = vecsize
            PinConfiguration = dir
            Persisted        = yml.Persisted
            Online           = yml.Online
            Dirty            = false
            Min              = yml.Min
            Max              = yml.Max
            Unit             = yml.Unit
            Precision        = yml.Precision
            Labels           = yml.Labels
            Values           = slices
          }
        }

      | "BoolPin" -> either {
          let! id = DiscoId.TryParse yml.Id
          let! group = DiscoId.TryParse yml.PinGroupId
          let! client = DiscoId.TryParse yml.ClientId
          let! dir = PinConfiguration.TryParse yml.PinConfiguration
          let! tags = parseTags yml
          let! vecsize = VecSize.TryParse yml.VecSize
          let! (_, slices) =
            let arr = Array.zeroCreate yml.Values.Length
            Array.fold
              (fun (m: Either<DiscoError,int * bool array>) (yml: SliceYaml) ->
                either {
                  let! (i, arr) = m
                  let! value = SliceYaml.toSlice yml
                  let! value =
                    try
                      value.Value
                      :?> bool
                      |> Either.succeed
                    with
                      | x ->
                        sprintf "Could not parse double: %s" x.Message
                        |> Error.asParseError "FromYaml NumberPin"
                        |> Either.fail
                  arr.[i] <- value
                  return (i + 1, arr)
                })
              (Right(0, arr))
              yml.Values

          return BoolPin {
            Id               = id
            Name             = name yml.Name
            PinGroupId       = group
            ClientId         = client
            Tags             = tags
            Persisted        = yml.Persisted
            Online           = yml.Online
            Dirty            = false
            IsTrigger        = yml.IsTrigger
            VecSize          = vecsize
            PinConfiguration = dir
            Labels           = yml.Labels
            Values           = slices
          }
        }

      | "BytePin" -> either {
          let! id = DiscoId.TryParse yml.Id
          let! group = DiscoId.TryParse yml.PinGroupId
          let! client = DiscoId.TryParse yml.ClientId
          let! dir = PinConfiguration.TryParse yml.PinConfiguration
          let! tags = parseTags yml
          let! vecsize = VecSize.TryParse yml.VecSize
          let! (_, slices) =
            let arr = Array.zeroCreate yml.Values.Length
            Array.fold
              (fun (m: Either<DiscoError,int * byte[] array>) (yml: SliceYaml) ->
                either {
                  let! (i, arr) = m
                  let! value = SliceYaml.toSlice yml
                  let! value =
                    try
                      value.Value
                      :?> byte array
                      |> Either.succeed
                    with
                      | x ->
                        sprintf "Could not parse double: %s" x.Message
                        |> Error.asParseError "FromYaml NumberPin"
                        |> Either.fail
                  arr.[i] <- value
                  return (i + 1, arr)
                })
              (Right(0, arr))
              yml.Values

          return BytePin {
            Id               = id
            Name             = name yml.Name
            PinGroupId       = group
            ClientId         = client
            Tags             = tags
            Persisted        = yml.Persisted
            Online           = yml.Online
            Dirty            = false
            VecSize          = vecsize
            PinConfiguration = dir
            Labels           = yml.Labels
            Values           = slices
          }
        }

      | "EnumPin" -> either {
          let! properties =
            Array.fold
              (fun (m: Either<DiscoError, int * Property array>) yml ->
                either {
                  let! state = m
                  let! parsed = Yaml.fromYaml yml
                  (snd state).[fst state] <- parsed
                  return (fst state + 1, snd state)
                })
              (Right (0, Array.zeroCreate yml.Properties.Length))
              yml.Properties
            |> Either.map snd

          let! (_, slices) =
            let arr = Array.zeroCreate yml.Values.Length
            Array.fold
              (fun (m: Either<DiscoError,int * Property array>) (yml: SliceYaml) ->
                either {
                  let! (i, arr) = m
                  let! value = SliceYaml.toSlice yml
                  let! value =
                    try
                      value.Value
                      :?> Property
                      |> Either.succeed
                    with
                      | x ->
                        sprintf "Could not parse Property: %s" x.Message
                        |> Error.asParseError "FromYaml NumberPin"
                        |> Either.fail
                  arr.[i] <- value
                  return (i + 1, arr)
                })
              (Right(0, arr))
              yml.Values

          let! id = DiscoId.TryParse yml.Id
          let! group = DiscoId.TryParse yml.PinGroupId
          let! client = DiscoId.TryParse yml.ClientId
          let! dir = PinConfiguration.TryParse yml.PinConfiguration
          let! vecsize = VecSize.TryParse yml.VecSize
          let! tags = parseTags yml

          return EnumPin {
            Id               = id
            Name             = name yml.Name
            PinGroupId       = group
            ClientId         = client
            Tags             = tags
            Online           = yml.Online
            Dirty            = false
            Persisted        = yml.Persisted
            Properties       = properties
            VecSize          = vecsize
            PinConfiguration = dir
            Labels           = yml.Labels
            Values           = slices
          }
        }

      | "ColorPin" -> either {
          let! id = DiscoId.TryParse yml.Id
          let! group = DiscoId.TryParse yml.PinGroupId
          let! client = DiscoId.TryParse yml.ClientId
          let! dir = PinConfiguration.TryParse yml.PinConfiguration
          let! tags = parseTags yml
          let! vecsize = VecSize.TryParse yml.VecSize

          let! (_, slices) =
            let arr = Array.zeroCreate yml.Values.Length
            Array.fold
              (fun (m: Either<DiscoError,int * ColorSpace array>) (yml: SliceYaml) ->
                either {
                  let! (i, arr) = m
                  let! value = SliceYaml.toSlice yml
                  let! value =
                    try
                      value.Value
                      :?> ColorSpace
                      |> Either.succeed
                    with
                      | x ->
                        sprintf "Could not parse Property: %s" x.Message
                        |> Error.asParseError "FromYaml NumberPin"
                        |> Either.fail
                  arr.[i] <- value
                  return (i + 1, arr)
                })
              (Right(0, arr))
              yml.Values

          return ColorPin {
            Id               = id
            Name             = name yml.Name
            PinGroupId       = group
            ClientId         = client
            Tags             = tags
            Persisted        = yml.Persisted
            Online           = yml.Online
            Dirty            = false
            VecSize          = vecsize
            PinConfiguration = dir
            Labels           = yml.Labels
            Values           = slices
          }
        }

      | x ->
        sprintf "Could not parse PinYml type: %s" x
        |> Error.asParseError "PynYml.FromYaml"
        |> Either.fail

    with
      | exn ->
        sprintf "Could not parse PinYml: %s" exn.Message
        |> Error.asParseError "PynYml.FromYaml"
        |> Either.fail

#endif
