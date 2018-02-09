module Disco.Web.Main

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Disco.Web.State
open Disco.Core
open Disco.Core.Commands
open System
open Helpers
open Types

// Make sure to call this method so Elmish app
// files are included in the bundle
App.init()

// Public methods in this file will be exposed to JS
// through the DiscoLib global variable (see Webpack config)

let findPinByName(model: Model, name: string) =
  model.state |> Option.bind (fun state ->
    let i = name.LastIndexOf("/")
    let groupName = name.[0..(i-1)]
    let pinName = name.[(i+1)..]
    state.PinGroups
    |> PinGroupMap.unifiedPins
    |> PinGroupMap.byGroup
    |> Map.tryPick (fun _ g ->
      if unwrap g.Name = groupName then Some g else None)
    |> Option.bind (fun group ->
      group.Pins |> Map.tryPick (fun _ p ->
        if unwrap p.Name = pinName then Some p else None)))

let getPinValueAt(pin: Pin, idx: int): obj =
    let slice = pin.Slices.At(1<index> * idx)
    slice.Value

let renderWidget(id, name, headFn, bodyFn, dispatch, model): React.ReactElement =
  widget id name headFn bodyFn dispatch model

let toString x = string x
let createId () = DiscoId.Create()
let createNameAndId (name: Name) =
  { Id = DiscoId.Create(); Name = name }

let updatePinValueAt(pin: Pin, idx: int, value: obj): unit =
  Lib.updatePinValue(pin, idx, value)
