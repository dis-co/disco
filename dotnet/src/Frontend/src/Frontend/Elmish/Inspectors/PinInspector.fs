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

let private renderNameRow (pin: Pin) =
  tr [Key (string "")] [
    td [Class "width-15"; topBorder()] [str "Name"]
    td [Class "width-15"; topBorder()] [str (string pin.Name)]
  ]

///  ____        _     _ _
/// |  _ \ _   _| |__ | (_) ___
/// | |_) | | | | '_ \| | |/ __|
/// |  __/| |_| | |_) | | | (__
/// |_|    \__,_|_.__/|_|_|\___|

module PinInspector =

  let render (pin: Pin) =
    table [Class "iris-table"] [
      thead [] [
        tr [] [
          th [Class "width-20"; padding5()] [str "Name"]
          th [Class "width-15"] [str "Value"]
        ]
      ]
      tbody [] [
        renderNameRow pin
      ]
    ]
