namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

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

  static member FromFB(fb: RGBAValueFB) : RGBAValue option =
    try
      { Red   = fb.Red
      ; Green = fb.Green
      ; Blue  = fb.Blue
      ; Alpha = fb.Alpha
      } |> Some
    with
      | _ -> None

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> RGBAValueFB.GetRootAsRGBAValueFB
    |> RGBAValue.FromFB


type HSLAValue =
  { Hue        : uint8
  ; Saturation : uint8
  ; Lightness  : uint8
  ; Alpha      : uint8 }

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

  static member FromFB(fb: HSLAValueFB) : HSLAValue option =
    try
      { Hue        = fb.Hue
      ; Saturation = fb.Saturation
      ; Lightness  = fb.Lightness
      ; Alpha      = fb.Alpha
      } |> Some
    with
      | _ -> None

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> HSLAValueFB.GetRootAsHSLAValueFB
    |> HSLAValue.FromFB

type ColorSpace =
  | RGBA of RGBAValue
  | HSLA of HSLAValue

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
#if JAVASCRIPT
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

  static member FromFB(fb: ColorSpaceFB) : ColorSpace option =
#if JAVASCRIPT
    match fb.ValueType with
    | x when x = ColorSpaceTypeFB.RGBAValueFB ->
      RGBAValueFB.Create()
      |> fb.Value
      |> RGBAValue.FromFB
      |> Option.map RGBA
    | x when x = ColorSpaceTypeFB.HSLAValueFB ->
      HSLAValueFB.Create()
      |> fb.Value
      |> HSLAValue.FromFB
      |> Option.map HSLA
    | _ -> None
#else
    // On .NET side, System.Nullables are used. Hard to emulate rn.
    match fb.ValueType with
    | ColorSpaceTypeFB.RGBAValueFB ->
      let v = fb.Value<RGBAValueFB>()
      if v.HasValue then
        v.Value
        |> RGBAValue.FromFB
        |> Option.map RGBA
      else None
    | ColorSpaceTypeFB.HSLAValueFB ->
      let v = fb.Value<HSLAValueFB>()
      if v.HasValue then
        v.Value
        |> HSLAValue.FromFB
        |> Option.map HSLA
      else None
    | _ -> None
#endif

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> ColorSpaceFB.GetRootAsColorSpaceFB
    |> ColorSpace.FromFB

#if JAVASCRIPT
#else

  member self.ToYamlObject() =
    match self with
    | RGBA value ->
      new ColorYaml("RGBA", value.Alpha, value.Red, value.Green, value.Blue)
    | HSLA value ->
      new ColorYaml("HSLA", value.Alpha, value.Hue, value.Saturation, value.Lightness)

  static member FromYamlObject(yml: ColorYaml) =
    match yml.ColorType with
    | "RGBA" ->
      RGBA {
        Red = yml.Channel1;
        Green = yml.Channel2;
        Blue = yml.Channel3;
        Alpha = yml.Alpha
      } |> Some
    | "HSLA" ->
      HSLA {
        Hue = yml.Channel1;
        Saturation = yml.Channel2;
        Lightness = yml.Channel3;
        Alpha = yml.Alpha
      } |> Some
    | _ -> None

#endif
