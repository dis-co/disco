module Iris.Web.Core.MockData

open System
open System.Collections.Generic
open Fable.Core
open Fable.Import
open Fable.Core.JsInterop
open Fable.PowerPack
open Iris.Core

let private (|IsJsArray|_|) (o: obj) =
    if JS.Array.isArray(o) then Some(o :?> ResizeArray<obj>) else None

let private failParse (gid: Id) (pk: string) (x: obj) =
    printfn "Unexpected value %A when parsing %s in PinGroup %O" x pk gid; None

let inline forcePin<'T> gid pk (values: obj seq) =
    let labels = ResizeArray()
    let values =
        values
        |> Seq.choose (function
            | IsJsArray ar when ar.Count = 2 ->
                match ar.[1] with
                | :? 'T as x -> labels.Add(string ar.[0] |> astag); Some x
                | x -> failParse gid pk x
            | :? 'T as x -> Some x
            | x -> failParse gid pk x)
        |> Seq.toArray
    Seq.toArray labels, values

let makeNumberPin gid pid pk values =
    let labels, values = forcePin<float> gid pk values
    Pin.number pid pk gid labels values |> Some

let makeTogglePin gid pid pk values =
    let labels, values = forcePin<bool> gid pk values
    Pin.toggle pid pk gid labels values |> Some

let makeStringPin gid pid pk values =
    let labels, values = forcePin<string> gid pk values
    Pin.string pid pk gid labels values |> Some

let makePin gid pk (v: obj) =
    let pid = Id pk
    match v with
    | IsJsArray ar ->
        match Seq.tryHead ar with
        | Some (IsJsArray ar2) when ar2.Count = 2 ->
            match ar2.[1] with
            | :? float -> makeNumberPin gid pid pk ar
            | :? bool -> makeTogglePin gid pid pk ar
            | :? string -> makeStringPin gid pid pk ar
            | _ -> failParse gid pk ar            
        | Some(:? float) -> makeNumberPin gid pid pk ar
        | Some(:? bool) -> makeTogglePin gid pid pk ar
        | Some(:? string) -> makeStringPin gid pid pk ar
        | _ -> failParse gid pk ar
    | :? float as x ->
        Pin.number pid pk gid [||] [|x|] |> Some
    | :? bool as x ->
        Pin.toggle pid pk gid [||] [|x|] |> Some
    | :? string as x ->
        Pin.string pid pk gid [||] [|x|] |> Some
    | x -> failParse gid pk x

let pinGroups: Map<Id, PinGroup> =
    let pinGroups: obj = Node.Globals.require.Invoke("../../data/pingroups.json")
    JS.Object.keys(pinGroups)
    |> Seq.map (fun gk ->
        let g = box pinGroups?(gk)
        let gid = Id gk            
        let pins =
            JS.Object.keys(g)
            |> Seq.choose (fun pk ->
                box g?(pk) |> makePin gid pk)
            |> Seq.map (fun pin -> pin.Id, pin)
            |> Map
        let pinGroup =
            { Id = gid
              Name = name gk
              Client = Id "mockup"
              Pins = pins }
        pinGroup.Id, pinGroup)
    |> Map

let getMockState() =
    { State.Empty with PinGroups = pinGroups }