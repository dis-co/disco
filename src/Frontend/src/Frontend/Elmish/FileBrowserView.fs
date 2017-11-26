module Iris.Web.FileBrowserView

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
open Iris.Raft
open Iris.Web.Core
open Helpers
open State
open Types

///  ____       _            _
/// |  _ \ _ __(_)_   ____ _| |_ ___
/// | |_) | '__| \ \ / / _` | __/ _ \
/// |  __/| |  | |\ V / (_| | ||  __/
/// |_|   |_|  |_| \_/ \__,_|\__\___|

let rec private directoryTree dispatch model = function
  | FsEntry.File(info) -> str (unwrap info.Name)
  | FsEntry.Directory(info, children) ->
    let children =
      children
      |> Map.toList
      |> List.map (snd >> directoryTree dispatch model)
    div [ Class "directory" ] [
      span [ ] [
        i [ Class "icon fa fa-folder-o" ] [ str "" ]
        str (unwrap info.Name)
      ]
      div [ Class "children" ] children
    ]

let private machineIcon dispatch node =
  span [
    Class "iris-output iris-icon icon-host"
    OnClick (fun _ -> Select.clusterMember dispatch node)
    Style [ Cursor "pointer" ]
  ] [
    str (unwrap node.HostName)
    span [
      classList [
        "iris-icon icon-bull",true
        "iris-status-off", node.State <> RaftMemberState.Running
        "iris-status-on", node.State = RaftMemberState.Running
      ]
    ] []
  ]

let private machine dispatch model trees node =
  let rnd = Random()
  let isOpen = if (rnd.Next(0,2)) > 0 then true else false
  let directories =
    if isOpen then
      trees
      |> Map.tryFind (Member.id node)
      |> Option.map (FsTree.directories >> directoryTree dispatch model)
      |> Option.map (fun dirs -> div [ Class "directories" ] [ dirs ])
    else None

  [ Some (machineIcon dispatch node); directories ]
  |> List.choose id
  |> div [ Class "machine" ]

let private machineBrowser dispatch model trees =
  let sites =
    model.state
    |> Option.map (State.sites >> Array.toList)
    |> Option.defaultValue List.empty

  let members =
    model.state
    |> Option.bind State.activeSite
    |> Option.bind (fun id -> List.tryFind (fun site -> ClusterConfig.id site = id) sites)
    |> Option.map (ClusterConfig.members >> Map.toList)
    |> Option.defaultValue List.empty
    |> List.sortBy (snd >> Member.hostName)
    |> List.map (snd >> machine dispatch model trees)

  div [ Class "machines" ] members

let private fileRow dispatch model (entry:FsEntry) =
  div [ Class "file" ] [
    span [ ] [
      i [ Class "icon fa fa-file-o" ] [ str "" ]
      str (FsEntry.name entry |> unwrap)
    ]
  ]

let private fileList dispatch model (trees:Map<HostId,FsTree>) =
  let files: ReactElement list =
    if not (Map.isEmpty trees) then
      trees
      |> Map.toList
      |> List.head
      |> snd
      |> FsTree.files
      |> List.map (fileRow dispatch model)
    else List.empty
  div [ Class "files" ] files

let private fileInfo dispatch model (entry:FsEntry) =
  div [ Class "file-info" ] [
    div [ Class "info" ] [
      div [ Class "columns" ] [
        div [ Class "column is-one-fifth" ] [
          strong [] [ str "Path:" ]
        ]
        div [ Class "column" ] [
          str (entry |> FsEntry.path |> string)
        ]
      ]
      div [ Class "columns" ] [
        div [ Class "column is-one-fifth" ] [
          strong [] [ str "Name:" ]
        ]
        div [ Class "column" ] [
          str (FsEntry.name entry |> string)
        ]
      ]
      div [ Class "columns" ] [
        div [ Class "column is-one-fifth" ] [
          strong [] [ str "Type:" ]
        ]
        div [ Class "column" ] [
          str (FsEntry.mimeType entry)
        ]
      ]
      div [ Class "columns" ] [
        div [ Class "column is-one-fifth" ] [
          strong [] [ str "Size:" ]
        ]
        div [ Class "column" ] [
          str (entry |> FsEntry.size |> FsEntry.formatBytes)
        ]
      ]
    ]
  ]

let private body dispatch model =
  let trees =
    model.state
    |> Option.map State.fsTrees
    |> Option.defaultValue Map.empty

  let entry =
    FsEntry.File(
      { Path = { Drive = 'C'; Platform = Windows; Elements = [ "tmp"; "hello"; "bye.txt" ] }
        Name = name "bye.txt"
        MimeType = "text/plain"
        Size = 1173741825u
        Filtered = 0u })

  div [ Class "asset-browser" ] [
    div [ Class "panel" ] [
      div [ Class "inlay" ] [
        header [ Class "header" ] [ str "Machines" ]
        div [ Class "body" ] [
          machineBrowser dispatch model trees
        ]
      ]
    ]
    div [ Class "center" ] [
      div [ Class "inlay" ] [
        header [ Class "header" ] [
          div [ Class "bread" ] [
            span [ Class "crumb" ] [ str "assets" ]
            span [ Class "crumb" ] [ str "vm-2017" ]
            span [ Class "crumb" ] [ str "stack-01" ]
          ]
        ]
        div [ Class "body" ] [
          fileList dispatch model trees
        ]
      ]
    ]
    div [ Class "panel" ] [
      div [ Class "inlay" ] [
        header [ Class "header" ] [ str "Fileinfo" ]
        div [ Class "body" ] [
          fileInfo dispatch model entry
        ]
      ]
    ]
  ]

/// __        ___     _            _
/// \ \      / (_) __| | __ _  ___| |_
///  \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
///   \ V  V / | | (_| | (_| |  __/ |_
///    \_/\_/  |_|\__,_|\__, |\___|\__|
///                     |___/

let createWidget (id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.FileBrowser
    member this.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 6
        minW = 6
        minH = 2 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 -> equalsRef s1.FsTrees s2.FsTrees
          | None, None -> true
          | _ -> false)
        (widget id this.Name None body dispatch)
        model
  }
