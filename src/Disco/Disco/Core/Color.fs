(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Disco.Serialization

#endif

// * Color Yaml

#if !FABLE_COMPILER && !DISCO_NODES

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

// * ColorUtils

#if FABLE_COMPILER

module ColorUtils =

  [<Emit("$0.toString(16)")>]
  let private toHexString (n:byte): string = jsNative

  [<Emit("\"0\".repeat($0 - $1.length) + $1")>]
  let private pad (n:int) (str:string): string = jsNative

  let toPaddedHexString (n:byte) =
    toHexString n |> pad 2

#endif

// * Parsing

module Parsing =

  open System
  open System.Text.RegularExpressions

  let private parse (str:string): byte = Convert.ToByte(str,16)

  let (|RGBA|_|) (str:string) =
    let pattern = "^#([A-Fa-f0-9]{2})([A-Fa-f0-9]{2})([A-Fa-f0-9]{2})([A-Fa-f0-9]{2})$"
    let matches = Regex.Match(str,pattern)
    if matches.Success then
      let red = parse matches.Groups.[1].Value
      let green = parse matches.Groups.[2].Value
      let blue = parse matches.Groups.[3].Value
      let alpha = parse matches.Groups.[4].Value
      Some(red, green, blue, alpha)
    else None

  let (|RGB|_|) (str:string) =
    let pattern = "^#([A-Fa-f0-9]{2})([A-Fa-f0-9]{2})([A-Fa-f0-9]{2})$"
    let matches = Regex.Match(str,pattern)
    if matches.Success then
      let red = parse matches.Groups.[1].Value
      let green = parse matches.Groups.[2].Value
      let blue = parse matches.Groups.[3].Value
      Some(red, green, blue)
    else None

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

  // ** ToHex

  member rgba.ToHex(alpha:bool) =
    if alpha then
      #if FABLE_COMPILER
      sprintf "#%s%s%s%s"
        (ColorUtils.toPaddedHexString rgba.Red)
        (ColorUtils.toPaddedHexString rgba.Green)
        (ColorUtils.toPaddedHexString rgba.Blue)
        (ColorUtils.toPaddedHexString rgba.Alpha)
      #else
      System.String.Format(
        "#{0:X2}{1:X2}{2:X2}{3:X2}",
        rgba.Red,
        rgba.Green,
        rgba.Blue,
        rgba.Alpha)
      #endif
    else
      #if FABLE_COMPILER
      sprintf "#%s%s%s"
        (ColorUtils.toPaddedHexString rgba.Red)
        (ColorUtils.toPaddedHexString rgba.Green)
        (ColorUtils.toPaddedHexString rgba.Blue)
      #else
      System.String.Format(
        "#{0:X2}{1:X2}{2:X2}",
        rgba.Red,
        rgba.Green,
        rgba.Blue)
      #endif

  // ** TryParse

  static member TryParse(value:string) =
    match value with
    | Parsing.RGBA (red, green, blue, alpha) ->
      { Red = red
        Green = green
        Blue = blue
        Alpha = alpha }
      |> Result.succeed
    | Parsing.RGB (red, green, blue) ->
      { Red = red
        Green = green
        Blue = blue
        Alpha = 255uy }
      |> Result.succeed
    | _ ->
      System.String.Format("Cannot parse {0} as RGB(A)", value)
      |> Error.asParseError "RGBAValue.TryParse"
      |> Result.fail

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

  static member FromFB(fb: RGBAValueFB) : DiscoResult<RGBAValue> =
    try
      { Red   = fb.Red
      ; Green = fb.Green
      ; Blue  = fb.Blue
      ; Alpha = fb.Alpha
      } |> Ok
    with
      | exn ->
        exn.Message
        |> Error.asParseError "RGBAValue.FromFB"
        |> Result.fail

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) =
    Binary.createBuffer bytes
    |> RGBAValueFB.GetRootAsRGBAValueFB
    |> RGBAValue.FromFB


// * HSLAValue

type HSLAValue =
  { Hue        : uint8
  ; Saturation : uint8
  ; Lightness  : uint8
  ; Alpha      : uint8 }

  // function hslToRgb(h, s, l){
  //     var r, g, b;
  //     if(s == 0){
  //         r = g = b = l; // achromatic
  //     }else{
  //         var hue2rgb = function hue2rgb(p, q, t){
  //             if(t < 0) t += 1;
  //             if(t > 1) t -= 1;
  //             if(t < 1/6) return p + (q - p) * 6 * t;
  //             if(t < 1/2) return q;
  //             if(t < 2/3) return p + (q - p) * (2/3 - t) * 6;
  //             return p;
  //         }
  //         var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
  //         var p = 2 * l - q;
  //         r = hue2rgb(p, q, h + 1/3);
  //         g = hue2rgb(p, q, h);
  //         b = hue2rgb(p, q, h - 1/3);
  //     }
  //     return [Math.round(r * 255), Math.round(g * 255), Math.round(b * 255)];
  // }

  member self.ToRGBA() =
    { Red = 0uy
      Green = 0uy
      Blue = 0uy
      Alpha = self.Alpha }

  // ** ToHex

  member hsla.ToHex(alpha:bool) =
    if alpha then
      #if FABLE_COMPILER
      sprintf "#%s%s%s%s"
        (ColorUtils.toPaddedHexString hsla.Hue)
        (ColorUtils.toPaddedHexString hsla.Saturation)
        (ColorUtils.toPaddedHexString hsla.Lightness)
        (ColorUtils.toPaddedHexString hsla.Alpha)
      #else
      System.String.Format(
        "#{0:X3}{1:X2}{2:X2}{3:X2}",
        hsla.Hue,
        hsla.Saturation,
        hsla.Lightness,
        hsla.Alpha)
      #endif
    else
      #if FABLE_COMPILER
      sprintf "#%s%s%s"
        (ColorUtils.toPaddedHexString hsla.Hue)
        (ColorUtils.toPaddedHexString hsla.Saturation)
        (ColorUtils.toPaddedHexString hsla.Lightness)
      #else
      System.String.Format(
        "#{0:X3}{1:X2}{2:X2}",
        hsla.Hue,
        hsla.Saturation,
        hsla.Lightness)
      #endif

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

  static member FromFB(fb: HSLAValueFB) : DiscoResult<HSLAValue> =
    try
      { Hue        = fb.Hue
      ; Saturation = fb.Saturation
      ; Lightness  = fb.Lightness
      ; Alpha      = fb.Alpha
      } |> Ok
    with
      | exn ->
        exn.Message
        |> Error.asParseError "HSLAValue.FromFB"
        |> Result.fail

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) =
    Binary.createBuffer bytes
    |> HSLAValueFB.GetRootAsHSLAValueFB
    |> HSLAValue.FromFB

// * ColorSpace

type ColorSpace =
  | RGBA of RGBAValue
  | HSLA of HSLAValue

  // ** ToHex

  member self.ToHex(alpha) =
    match self with
    | RGBA value -> value.ToHex(alpha)
    | HSLA value -> value.ToHex(alpha)

  // ** TryParse

  static member TryParse(value:string) =
    value
    |> RGBAValue.TryParse
    |> Result.map ColorSpace.RGBA

  // ** Black

  static member Black
    with get () =
      RGBA { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 0uy }

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

  static member FromFB(fb: ColorSpaceFB) : DiscoResult<ColorSpace> =
#if FABLE_COMPILER
    match fb.ValueType with
    | x when x = ColorSpaceTypeFB.RGBAValueFB ->
      RGBAValueFB.Create()
      |> fb.Value
      |> RGBAValue.FromFB
      |> Result.map RGBA

    | x when x = ColorSpaceTypeFB.HSLAValueFB ->
      HSLAValueFB.Create()
      |> fb.Value
      |> HSLAValue.FromFB
      |> Result.map HSLA

    | x ->
      sprintf "Could not deserialize %A" x
      |> Error.asParseError "ColorSpace.FromFB"
      |> Result.fail

#else
    // On .NET side, System.Nullables are used. Hard to emulate rn.
    match fb.ValueType with
    | ColorSpaceTypeFB.RGBAValueFB ->
      let v = fb.Value<RGBAValueFB>()
      if v.HasValue then
        v.Value
        |> RGBAValue.FromFB
        |> Result.map RGBA
      else
        "Could not parse RGBAValue"
        |> Error.asParseError "ColorSpace.FromFB"
        |> Result.fail

    | ColorSpaceTypeFB.HSLAValueFB ->
      let v = fb.Value<HSLAValueFB>()
      if v.HasValue then
        v.Value
        |> HSLAValue.FromFB
        |> Result.map HSLA
      else
        "Could not parse RGBAValue"
        |> Error.asParseError "ColorSpace.FromFB"
        |> Result.fail

    | x ->
      sprintf "Could not parse ColorSpaceFB. Unknown type: %A" x
      |> Error.asParseError "ColorSpace.FromFB"
      |> Result.fail

#endif

  // ** ToBytes

  member self.ToBytes () = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) =
    Binary.createBuffer bytes
    |> ColorSpaceFB.GetRootAsColorSpaceFB
    |> ColorSpace.FromFB

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  member self.ToYaml() =
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

  // ** FromYaml

  static member FromYaml(yml: ColorYaml) =
    match yml.ColorType with
    | "RGBA" ->
      RGBA {
        Red = yml.Channel1;
        Green = yml.Channel2;
        Blue = yml.Channel3;
        Alpha = yml.Alpha
      } |> Ok
    | "HSLA" ->
      HSLA {
        Hue = yml.Channel1;
        Saturation = yml.Channel2;
        Lightness = yml.Channel3;
        Alpha = yml.Alpha
      } |> Ok
    | x ->
      sprintf "Could not parse ColorYaml. Unknown type: %s" x
      |> Error.asParseError "ColorSpace.FromYaml"
      |> Result.fail

#endif
