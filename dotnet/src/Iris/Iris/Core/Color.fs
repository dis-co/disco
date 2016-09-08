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

  with

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
      let json = JToken.FromObject(self) :?> JObject
      json.Add("$type", new JValue("Iris.Core.RGBAValue"))
      json :> JToken

    member self.ToJson() =
      self.ToJToken() |> string

#endif


type HSLAValue =
  { Hue        : uint8
  ; Saturation : uint8
  ; Lightness  : uint8
  ; Alpha      : uint8 }

#if JAVASCRIPT
#else

  with

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
      let json = JToken.FromObject(self) :?> JObject
      json.Add("$type", new JValue("Iris.Core.HSLAValue"))
      json :> JToken

    member self.ToJson() =
      self.ToJToken() |> string

#endif

type ColorSpace =
  | RGBA of RGBAValue
  | HSLA of HSLAValue

#if JAVASCRIPT
#else

  with

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
        fb.GetValue(new RGBAValueFB())
        |> RGBAValue.FromFB
        |> Option.map RGBA
      | ColorSpaceTypeFB.HSLAValueFB ->
        fb.GetValue(new HSLAValueFB())
        |> HSLAValue.FromFB
        |> Option.map HSLA
      | _ -> None

    member self.ToBytes () = Binary.buildBuffer self

    static member FromBytes(bytes: byte array) =
      ColorSpaceFB.GetRootAsColorSpaceFB(new ByteBuffer(bytes))
      |> ColorSpace.FromFB

    //      _
    //     | |___  ___  _ __
    //  _  | / __|/ _ \| '_ \
    // | |_| \__ \ (_) | | | |
    //  \___/|___/\___/|_| |_|

    member self.ToJToken() : JToken =
      let json = new JObject()
      json.Add("$type", new JValue("Iris.Core.ColorSpace"))

      let add (case: string) token =
        json.Add("Case", new JValue(case))
        json.Add("Fields", new JArray([| token |]))

      match self with
      | RGBA data -> add "RGBA" (Json.tokenize data)
      | HSLA data -> add "HSLA" (Json.tokenize data)

      json :> JToken

    member self.ToJson() =
      self.ToJToken() |> string

#endif
