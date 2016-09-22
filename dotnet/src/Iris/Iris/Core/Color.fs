namespace Iris.Core

#if JAVASCRIPT
#else

open FlatBuffers
open Iris.Serialization.Raft
open Newtonsoft.Json
open Newtonsoft.Json.Linq

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

#if JAVASCRIPT
#else

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

  static member FromBytes(bytes: byte array) =
    RGBAValueFB.GetRootAsRGBAValueFB(new ByteBuffer(bytes))
    |> RGBAValue.FromFB

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() : JToken =
    JToken.FromObject self

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : RGBAValue option =
    try
      { Red   = uint8 token.["Red"]
      ; Green = uint8 token.["Green"]
      ; Blue  = uint8 token.["Blue"]
      ; Alpha = uint8 token.["Alpha"]
      } |> Some
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : RGBAValue option =
    JObject.Parse(str) |> RGBAValue.FromJToken

#endif


type HSLAValue =
  { Hue        : uint8
  ; Saturation : uint8
  ; Lightness  : uint8
  ; Alpha      : uint8 }

#if JAVASCRIPT
#else

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

  static member FromBytes(bytes: byte array) =
    HSLAValueFB.GetRootAsHSLAValueFB(new ByteBuffer(bytes))
    |> HSLAValue.FromFB

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() : JToken =
    JToken.FromObject self

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : HSLAValue option =
    try
      { Hue        = uint8 token.["Hue"]
      ; Saturation = uint8 token.["Saturation"]
      ; Lightness  = uint8 token.["Lightness"]
      ; Alpha      = uint8 token.["Alpha"]
      } |> Some
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : HSLAValue option =
    JObject.Parse(str) |> HSLAValue.FromJToken

#endif

type ColorSpace =
  | RGBA of RGBAValue
  | HSLA of HSLAValue

#if JAVASCRIPT
#else

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
      ColorSpaceFB.AddValue(builder,offset.Value)
      ColorSpaceFB.EndColorSpaceFB(builder)

    match self with
    | RGBA value ->
      value.ToOffset(builder)
      |> build ColorSpaceTypeFB.RGBAValueFB

    | HSLA value ->
      value.ToOffset(builder)
      |> build ColorSpaceTypeFB.HSLAValueFB

  static member FromFB(fb: ColorSpaceFB) : ColorSpace option =
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

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes(bytes: byte array) =
    ColorSpaceFB.GetRootAsColorSpaceFB(new ByteBuffer(bytes))
    |> ColorSpace.FromFB

#endif
