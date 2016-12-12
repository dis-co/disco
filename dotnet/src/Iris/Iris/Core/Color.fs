namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

// * Color Yaml

type ColorYaml(tipe, alpha, ch1, ch2, ch3) as self =
  [<DefaultValue>] val mutable ColorType : string
  [<DefaultValue>] val mutable Alpha     : uint8
  [<DefaultValue>] val mutable Channel1  : uint8
  [<DefaultValue>] val mutable Channel2  : uint8
  [<DefaultValue>] val mutable Channel3  : uint8

  new () = new ColorYaml(null, 0uy, 0uy, 0uy, 0uy)

  do
    self.ColorType <- tipe
    self.Alpha <- alpha
    self.Channel1 <- ch1
    self.Channel2 <- ch2
    self.Channel3 <- ch3

#endif

// * RGBAValue

//   ____      _
//  / ___|___ | | ___  _ __ ___
// | |   / _ \| |/ _ \| '__/ __|
// | |__| (_) | | (_) | |  \__ \
//  \____\___/|_|\___/|_|  |___/

type RGBAValue =
  { Red   : uint8
  ; Green : uint8
  ; Blue  : uint8
  ; Alpha : uint8 }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    RGBAValueFB.StartRGBAValueFB(builder)
    RGBAValueFB.AddRed(builder, self.Red)
    RGBAValueFB.AddGreen(builder, self.Green)
    RGBAValueFB.AddBlue(builder, self.Blue)
    RGBAValueFB.AddAlpha(builder, self.Alpha)
    RGBAValueFB.EndRGBAValueFB(builder)

  // ** FromFB

  static member FromFB(fb: RGBAValueFB) : Either<IrisError,RGBAValue> =
    try
      { Red   = fb.Red
      ; Green = fb.Green
      ; Blue  = fb.Blue
      ; Alpha = fb.Alpha
      } |> Right
    with
      | exn ->
        exn.Message
        |> Error.asParseError "RGBAValue.FromFB"
        |> Either.fail

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> RGBAValueFB.GetRootAsRGBAValueFB
    |> RGBAValue.FromFB


// * HSLAValue

type HSLAValue =
  { Hue        : uint8
  ; Saturation : uint8
  ; Lightness  : uint8
  ; Alpha      : uint8 }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    HSLAValueFB.StartHSLAValueFB(builder)
    HSLAValueFB.AddHue(builder, self.Hue)
    HSLAValueFB.AddSaturation(builder, self.Saturation)
    HSLAValueFB.AddLightness(builder, self.Lightness)
    HSLAValueFB.AddAlpha(builder, self.Alpha)
    HSLAValueFB.EndHSLAValueFB(builder)

  // ** FromFB

  static member FromFB(fb: HSLAValueFB) : Either<IrisError,HSLAValue> =
    try
      { Hue        = fb.Hue
      ; Saturation = fb.Saturation
      ; Lightness  = fb.Lightness
      ; Alpha      = fb.Alpha
      } |> Right
    with
      | exn ->
        exn.Message
        |> Error.asParseError "HSLAValue.FromFB"
        |> Either.fail

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> HSLAValueFB.GetRootAsHSLAValueFB
    |> HSLAValue.FromFB

// * ColorSpace

type ColorSpace =
  | RGBA of RGBAValue
  | HSLA of HSLAValue

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let build tipe (offset: Offset<_>) =
      ColorSpaceFB.StartColorSpaceFB(builder)
      ColorSpaceFB.AddValueType(builder, tipe)
#if FABLE_COMPILER
      ColorSpaceFB.AddValue(builder,offset)
#else
      ColorSpaceFB.AddValue(builder,offset.Value)
#endif
      ColorSpaceFB.EndColorSpaceFB(builder)

    match self with
    | RGBA value ->
      value.ToOffset(builder)
      |> build ColorSpaceTypeFB.RGBAValueFB

    | HSLA value ->
      value.ToOffset(builder)
      |> build ColorSpaceTypeFB.HSLAValueFB

  // ** FromFB

  static member FromFB(fb: ColorSpaceFB) : Either<IrisError,ColorSpace> =
#if FABLE_COMPILER
    match fb.ValueType with
    | x when x = ColorSpaceTypeFB.RGBAValueFB ->
      RGBAValueFB.Create()
      |> fb.Value
      |> RGBAValue.FromFB
      |> Either.map RGBA

    | x when x = ColorSpaceTypeFB.HSLAValueFB ->
      HSLAValueFB.Create()
      |> fb.Value
      |> HSLAValue.FromFB
      |> Either.map HSLA

    | x ->
      sprintf "Could not deserialize %A" x
      |> Error.asParseError "ColorSpace.FromFB"
      |> Either.fail

#else
    // On .NET side, System.Nullables are used. Hard to emulate rn.
    match fb.ValueType with
    | ColorSpaceTypeFB.RGBAValueFB ->
      let v = fb.Value<RGBAValueFB>()
      if v.HasValue then
        v.Value
        |> RGBAValue.FromFB
        |> Either.map RGBA
      else
        "Could not parse RGBAValue"
        |> Error.asParseError "ColorSpace.FromFB"
        |> Either.fail

    | ColorSpaceTypeFB.HSLAValueFB ->
      let v = fb.Value<HSLAValueFB>()
      if v.HasValue then
        v.Value
        |> HSLAValue.FromFB
        |> Either.map HSLA
      else
        "Could not parse RGBAValue"
        |> Error.asParseError "ColorSpace.FromFB"
        |> Either.fail

    | x ->
      sprintf "Could not parse ColorSpaceFB. Unknown type: %A" x
      |> Error.asParseError "ColorSpace.FromFB"
      |> Either.fail

#endif

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> ColorSpaceFB.GetRootAsColorSpaceFB
    |> ColorSpace.FromFB

  // ** ToYamlObject

#if !FABLE_COMPILER

  member self.ToYamlObject() =
    match self with
    | RGBA value ->
      let yml = new ColorYaml()
      yml.ColorType <- "RGBA"
      yml.Alpha     <- value.Alpha
      yml.Channel1  <- value.Red
      yml.Channel2  <- value.Green
      yml.Channel3  <- value.Blue
      yml

    | HSLA value ->
      let yml = new ColorYaml()
      yml.ColorType <- "HSLA"
      yml.Alpha     <- value.Alpha
      yml.Channel1  <- value.Hue
      yml.Channel2  <- value.Saturation
      yml.Channel3  <- value.Lightness
      yml

  // ** FromYamlObject

  static member FromYamlObject(yml: ColorYaml) =
    match yml.ColorType with
    | "RGBA" ->
      RGBA {
        Red = yml.Channel1;
        Green = yml.Channel2;
        Blue = yml.Channel3;
        Alpha = yml.Alpha
      } |> Right
    | "HSLA" ->
      HSLA {
        Hue = yml.Channel1;
        Saturation = yml.Channel2;
        Lightness = yml.Channel3;
        Alpha = yml.Alpha
      } |> Right
    | x ->
      sprintf "Could not parse ColorYaml. Unknown type: %s" x
      |> Error.asParseError "ColorSpace.FromYamlObject"
      |> Either.fail

#endif
