module Iris.Web.Inspectors

open System
open System.Collections.Generic
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Helpers
open State
open Types

///  ____       _            _
/// |  _ \ _ __(_)_   ____ _| |_ ___
/// | |_) | '__| \ \ / / _` | __/ _ \
/// |  __/| |  | |\ V / (_| | ||  __/
/// |_|   |_|  |_| \_/ \__,_|\__\___|

let inline private padding5() =
  Style [PaddingLeft "5px"]

let inline private topBorder() =
  Style [BorderTop "1px solid lightgray"]

let inline private padding5AndTopBorder() =
  Style [PaddingLeft "5px"; BorderTop "1px solid lightgray"]

let private leftColumn =
  Style [
    PaddingLeft  "10px"
    BorderTop   "1px solid lightgray"
    BorderRight "1px solid lightgray"
  ]

let private rightColumn =
  Style [
    PaddingLeft "10px"
    BorderTop   "1px solid lightgray"
  ]

let private leftSub =
  Style [
    BorderRight  "1px solid lightgray"
  ]

let private rightSub =
  Style [
    PaddingLeft "10px"
  ]

let private renderRow (tag: string) (value: string) =
  tr [Key tag] [
    td [Class "width-10";  leftColumn ] [str tag]
    td [Class "width-30"; rightColumn ] [str value]
  ]

let private renderSub (tag: string) (value: string) =
  tr [Key tag] [
    td [Class "width-5";  leftSub ] [str (tag + ":")]
    td [Class "width-30"; rightSub ] [str value]
  ]

let private renderSlices (tag: string) (slices: Slices) =
  let slices =
    slices.Map (function
    | StringSlice(idx, value) -> renderSub (string idx) (string value)
    | NumberSlice(idx, value) -> renderSub (string idx) (string value)
    | BoolSlice(idx, value)   -> renderSub (string idx) (string value)
    | ByteSlice(idx, value)   -> renderSub (string idx) (string value)
    | EnumSlice(idx, value)   -> renderSub (string idx) (string value)
    | ColorSlice(idx, value)  -> renderSub (string idx) (string value))
    |> List.ofArray
  tr [ Key tag ] [
    td [Class "width-10"; leftColumn  ] [ str tag ]
    td [Class "width-30"; rightColumn ] [
      table [Class "iris-table"] [
        thead [] [
          tr [] [
            th [ leftSub ]  [ str "Index"]
            th [ rightSub ] [ str "Value"]
          ]
        ]
        tbody [] slices
      ]
    ]
  ]

///  ____        _     _ _
/// |  _ \ _   _| |__ | (_) ___
/// | |_) | | | | '_ \| | |/ __|
/// |  __/| |_| | |_) | | | (__
/// |_|    \__,_|_.__/|_|_|\___|

module PinInspector =

  let render (pin: Pin) =
    table [Class "iris-table"] [
      tbody [] [
        renderRow "Id"            (string pin.Id)
        renderRow "Name"          (string pin.Name)
        renderRow "Type"          (string pin.Type)
        renderRow "Configuration" (string pin.PinConfiguration)
        renderRow "VecSize"       (string pin.VecSize)
        renderRow "Clients"       (string pin.ClientId)
        renderRow "Group"         (string pin.PinGroupId)
        renderRow "Online"        (string pin.Online)
        renderRow "Persisted"     (string pin.Persisted)
        renderRow "Dirty"         (string pin.Dirty)
        renderRow "Labels"        (string pin.Labels)
        renderRow "Tags"          (string pin.GetTags)
        renderSlices "Values"     pin.Slices
      ]
      tfoot [] [
        tr [] [
          td [ leftColumn  ] []
          td [ rightColumn ] []
        ]
      ]
    ]
