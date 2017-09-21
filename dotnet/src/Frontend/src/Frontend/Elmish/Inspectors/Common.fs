namespace Iris.Web.Inspectors

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
open Iris.Web.Helpers
open Iris.Web.Types
open State

module Common =

  let bar dispatch (model: Model) =
    let disabled = List.isEmpty model.history
    div [
      Style [
        BackgroundColor "lightgrey"
        Height "20px"
        PaddingLeft "6px"
      ]
    ] [
      button [
        Style [
          Height "100%"
          Width "40px"
        ]
        Disabled disabled
        OnClick (fun _ -> Navigate.back dispatch)
      ] [ str "<"]
      button [
        Style [
          Height "100%"
          Width "40px"
        ]
        Disabled disabled
        OnClick (fun _ -> Navigate.forward dispatch)
      ] [ str ">"]
    ]

  let inline padding5() =
    Style [PaddingLeft "5px"]

  let inline topBorder() =
    Style [BorderTop "1px solid lightgray"]

  let inline padding5AndTopBorder() =
    Style [PaddingLeft "5px"; BorderTop "1px solid lightgray"]

  let link (content: string) f =
    div [
      Class "iris-link"
      OnClick (fun _ -> f())
    ] [ str content ]

  let leftColumn =
    Style [
      PaddingLeft  "10px"
      BorderTop   "1px solid lightgray"
      BorderRight "1px solid lightgray"
    ]

  let rightColumn =
    Style [
      PaddingLeft "10px"
      BorderTop   "1px solid lightgray"
    ]

  let leftSub =
    Style [
      BorderRight  "1px solid lightgray"
    ]

  let rightSub =
    Style [
      PaddingLeft "10px"
    ]

  let row (tag: string) children =
    tr [Key tag] [
      td [Class "width-10";  leftColumn ] [str tag]
      td [Class "width-30"; rightColumn ] children
    ]

  let stringRow (tag: string) (value: string) =
    row tag [ str value ]


  let toHeader (idx: int) (title: string) =
    match idx with
    | 0 -> th [ leftSub  ] [ str title ]
    | _ -> th [ rightSub ] [ str title ]

  let tableRow (tag: string) headers children =
    row tag [
      table [Class "iris-table"] [
        thead [] [
          tr [] (List.mapi toHeader headers)
        ]
        tbody [] children
      ]
    ]

  let header (title: string) =
    thead [] [
      tr [] [
        th [
          Style [
            Padding "5px 0 5px 10px"
            FontSize "1.2em"
          ]
        ] [
          str title
        ]
      ]
    ]

  let footer =
    tfoot [] [
      tr [] [
        td [ leftColumn  ] []
        td [ rightColumn ] []
      ]
    ]

  let render dispatch model (title: string) children =
    div [] [
      bar dispatch model
      table [Class "iris-table"] [
        header title
        tbody [] children
        footer
      ]
    ]
