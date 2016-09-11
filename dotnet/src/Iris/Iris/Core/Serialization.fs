namespace Iris.Core

// open System.Runtime.CompilerServices
// open Iris.Serialization.Raft
// open Iris.Raft

open FlatBuffers
open Newtonsoft.Json
open Newtonsoft.Json.Linq

open System
open System.Text
open System.Runtime.Serialization
open System.Runtime.Serialization.Formatters

type Serialization =

  static member private RemoveAssemblyDetails(fullyQualifiedTypeName: string) =
    let builder = new StringBuilder()

    // loop through the type name and filter out qualified assembly details from nested type names
    let mutable writingAssemblyName = false
    let mutable writingAssemblyName = false
    let mutable skippingAssemblyDetails = false

    for i in 0 .. (fullyQualifiedTypeName.Length - 1) do
      let current = fullyQualifiedTypeName.[i]
      match current with
      | '[' ->
        writingAssemblyName <- false
        skippingAssemblyDetails <- false
        builder.Append(current) |> ignore
      | ']' ->
        writingAssemblyName <- false
        skippingAssemblyDetails <- false
        builder.Append(current) |> ignore
      | ',' ->
        if not writingAssemblyName then
          writingAssemblyName <- true
          builder.Append(current) |> ignore
        else
          skippingAssemblyDetails <- true
      | _ ->
        if not skippingAssemblyDetails then
          builder.Append(current) |> ignore

    builder.ToString();

  static member GetTypeName<'t> _ =
    let t = typeof<'t>
    t.AssemblyQualifiedName
    |> Serialization.RemoveAssemblyDetails

//  ____  _
// | __ )(_)_ __   __ _ _ __ _   _
// |  _ \| | '_ \ / _` | '__| | | |
// | |_) | | | | | (_| | |  | |_| |
// |____/|_|_| |_|\__,_|_|   \__, |
//                           |___/

[<RequireQualifiedAccess>]
module Binary =

  let inline encode (value : ^t when ^t : (member ToBytes : unit -> byte array)) =
    (^t : (member ToBytes : unit -> byte array) value)

  let inline decode< ^t when ^t : (static member FromBytes : byte array -> ^t option)>
                                  (bytes: byte array) :
                                  ^t option =
    (^t : (static member FromBytes : byte array -> ^t option) bytes)

  let inline buildBuffer< ^t, ^a when ^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >)> (thing: ^a) : byte array =
    let builder = new FlatBufferBuilder(1)
    let offset = (^a : (member ToOffset : FlatBufferBuilder -> Offset< ^t >) (thing, builder))
    builder.Finish(offset.Value)
    builder.SizedByteArray()


//      _
//     | |___  ___  _ __
//  _  | / __|/ _ \| '_ \
// | |_| \__ \ (_) | | | |
//  \___/|___/\___/|_| |_|

[<RequireQualifiedAccess>]
module Json =

  let inline encode (value: ^t when ^t : (member ToJson : unit -> string)) : string =
    (^t : (member ToJson : unit -> string) value)

  let inline decode< ^t when ^t : (static member FromJson : string -> ^t option)> (str: string) : ^t option =
    (^t : (static member FromJson : string -> ^t option) str)

  let inline tokenize (value: ^t when ^t : (member ToJToken : unit -> JToken)) : JToken =
    (^t : (member ToJToken : unit -> JToken) value)

  let inline parse< ^t when ^t : (static member FromJToken : JToken -> ^t option)> (token: JToken) : ^t option =
    (^t : (static member FromJToken : JToken -> ^t option) token)

#if JAVASCRIPT
#else

[<AutoOpen>]
module JsonHelpers =

  type TokenWrap<'a> =
    | Wrap of 'a

    member self.ToJToken() =
      match self with
      | Wrap ting -> new JValue(ting) :> JToken

  let inline addProp (prop: string) (json: JToken) (value: JToken) =
    json.[prop] <- value
    json

  let addStrings (prop: string) (value: string array) (json: JToken) =
    new JArray(value) |> addProp prop json

  let addString (prop: string) (value: string) (json: JToken) =
    new JValue(value) |> addProp prop json

  let addInt (prop: string) (value: int) (json: JToken) =
    new JValue(value) |> addProp prop json

  let addUInt32 (prop: string) (value: uint32) (json: JToken) =
    new JValue(value) |> addProp prop json

  let inline addArray< ^t when ^t : (member ToJToken : unit -> JToken)> (prop: string) (value: ^t array) (json: JToken) : JToken =
    new JArray(Array.map Json.tokenize value) |> addProp prop json

  let inline addFields< ^t when ^t : (member ToJToken : unit -> JToken)> (value: ^t array) (json: JToken) : JToken =
    new JArray(Array.map Json.tokenize value) |> addProp "Fields" json

  let addCase (case: string) (json: JToken) =
    new JValue(case) |> addProp "Case" json

  let addType (tipe: string) (json: JToken) =
    new JValue(tipe) |> addProp "$type" json

  let addLong (prop: string) (value: uint64) (json: JToken) =
    new JValue(value) |> addProp prop json

  let addBool (prop: string) (value: bool) (json: JToken) =
    new JValue(value) |> addProp prop json

  let addFloat (prop: string) (value: float) (json: JToken) =
    new JValue(value) |> addProp prop json

  let addDouble (prop: string) (value: double) (json: JToken) =
    new JValue(value) |> addProp prop json

  let inline addToken (prop: string) (value: ^t) (json: JToken) =
     value |> Json.tokenize |> addProp prop json

#endif
