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

  let link (content: string) f =
    div [
      Class "iris-link"
      OnClick (fun _ -> f())
    ] [ str content ]

  let activeLink (content: string) f =
    div [
      Class "iris-link iris-link-active"
      OnClick (fun _ -> f())
    ] [ str content ]

  let makeLink (history: BrowseHistory) (idx: int) (content: string) f =
    let link =
      if idx = abs (history.index - (history.previous.Length - 1))
      then activeLink content f
      else link content f
    if idx < history.previous.Length - 1
    then [link; str ">"]
    else [link]

  let breadcrumb dispatch (history: BrowseHistory) (idx: int) (selected: Selected) =
    let content =
      match selected with
      | Selected.Pin pin ->
        makeLink history idx (string pin.Name) <| fun () ->
          Select.pin dispatch pin
      | Selected.PinGroup group ->
        makeLink history idx (string group.Name) <| fun () ->
          Select.group dispatch group
      | Selected.Client client ->
        makeLink history idx (string client.Name) <| fun () ->
          Select.client dispatch client
      | Selected.Member mem ->
        makeLink history idx (string mem.HostName) <| fun () ->
          Select.clusterMember dispatch mem
      | Selected.Cue cue ->
        makeLink history idx (string cue.Name) <| fun () ->
          Select.cue dispatch cue
      | Selected.CueList cuelist ->
        makeLink history idx (string cuelist.Name) <| fun () ->
          Select.cuelist dispatch cuelist
      | Selected.Player player ->
        makeLink history idx (string player.Name) <| fun () ->
          Select.player dispatch player
      | Selected.Session session ->
        makeLink history idx (string session.IpAddress) <| fun () ->
          Select.session dispatch session
      | Selected.User user ->
        makeLink history idx (string user.UserName) <| fun () ->
          Select.user dispatch user
      | Selected.Mapping mapping ->
        makeLink history idx (mapping.Id.Prefix()) <| fun () ->
          Select.mapping dispatch mapping
      | Selected.Nothing -> [str ""]
    li [ Style [ Display "inline-block" ] ] content

  let bar dispatch (model: Model) =
    let disabled = List.isEmpty model.history.previous
    let history =
      model.history.previous
      |> List.rev
      |> List.mapi (breadcrumb dispatch model.history)
    div [
      Style [
        Display "flex"
        BackgroundColor "lightgrey"
        Height "25px"
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
      ul [] history
    ]

  let inline padding5() =
    Style [PaddingLeft "5px"]

  let inline topBorder() =
    Style [BorderTop "1px solid lightgray"]

  let inline padding5AndTopBorder() =
    Style [PaddingLeft "5px"; BorderTop "1px solid lightgray"]

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
