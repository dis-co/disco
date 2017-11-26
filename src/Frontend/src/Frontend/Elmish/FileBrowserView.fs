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

// * FileBrowserProps

type [<Pojo>] FileBrowserProps =
  { Id: Guid
    Model: Model
    Dispatch: Msg -> unit }

// * FileBrowserState

type [<Pojo>] FileBrowserState =
  { File: FsEntry option
    OpenDirectories: Set<HostId * FsPath>
    Machine: HostId option }

// * FileBrowserState module

module FileBrowserState =

  let defaultState =
    { File = None
      OpenDirectories = Set.ofList []
      Machine = None }

  let openDirectories { OpenDirectories = dirs } = dirs
  let setOpenDirectories (dirs: Set<HostId*FsPath>) s =
    printfn "dirs: %A" dirs
    { s with OpenDirectories = dirs }

  let modifyOpenDirectories f s =
    s |> openDirectories |> f |> flip setOpenDirectories s

  let addOpenDirectory dir s = modifyOpenDirectories (Set.add dir) s
  let removeOpenDirectory dir s = modifyOpenDirectories (Set.remove dir) s

// * FileBrowserView

type FileBrowserView(props) =
  inherit React.Component<FileBrowserProps, FileBrowserState>(props)
  do base.setInitState(FileBrowserState.defaultState)

  // ** toggleDirectory

  member this.toggleDirectory host fspath =
    let entry = host, fspath
    if Set.contains entry this.state.OpenDirectories then
      this.state
      |> FileBrowserState.removeOpenDirectory entry
      |> this.setState
    else
      this.state
      |> FileBrowserState.addOpenDirectory entry
      |> this.setState

  // ** renderDirectoryTree

  member this.renderDirectoryTree host = function
    | FsEntry.File(info) -> str (unwrap info.Name)
    | FsEntry.Directory(info, children) ->

      let children =
        if Set.contains (host, info.Path) this.state.OpenDirectories then
          children
          |> Map.toList
          |> List.map (snd >> this.renderDirectoryTree host)
        else List.empty

      div [
        Class "directory"
        OnClick
          (fun e ->
            e.stopPropagation()         /// needed to stop all other toggles from firing
            this.toggleDirectory host info.Path)
      ] [
        span [ classList [ "has-children", List.length children > 0 ] ] [
          i [ Class "icon fa fa-folder-o" ] [ str "" ]
          str (unwrap info.Name)
        ]
        div [ Class "children" ] children
      ]

  // ** renderMachineIcon

  member this.renderMachineIcon(node) =
    span [
      Class "iris-output iris-icon icon-host"
      OnClick (fun _ -> Select.clusterMember this.props.Dispatch node)
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

  // ** renderMachine

  member this.renderMachine trees (node:RaftMember) =
    let isOpen =
      match this.state.Machine with
      | Some id when id = node.Id -> true
      | _ -> false

    let nodeId = Member.id node

    let directories =
      if isOpen then
        trees
        |> Map.tryFind nodeId
        |> Option.map (FsTree.directories >> this.renderDirectoryTree nodeId)
        |> Option.map (fun dirs -> div [ Class "directories" ] [ dirs ])
      else None

    [ Some (this.renderMachineIcon node); directories ]
    |> List.choose id
    |> div [
      classList [
        "machine",true
        "is-open", isOpen
      ]
      OnClick (fun _ -> this.setState({ this.state with Machine = Some node.Id }))
    ]

  // ** renderMachineBrowser

  member this.renderMachineBrowser trees =
    let sites =
      this.props.Model.state
      |> Option.map (State.sites >> Array.toList)
      |> Option.defaultValue List.empty

    let members =
      this.props.Model.state
      |> Option.bind State.activeSite
      |> Option.bind (fun id -> List.tryFind (fun site -> ClusterConfig.id site = id) sites)
      |> Option.map (ClusterConfig.members >> Map.toList)
      |> Option.defaultValue List.empty
      |> List.sortBy (snd >> Member.hostName)
      |> List.map (snd >> this.renderMachine trees)

    div [ Class "machines" ] members

  // ** renderFileRow

  member this.renderFileRow (entry:FsEntry) =
    div [ Class "file" ] [
      span [ ] [
        i [ Class "icon fa fa-file-o" ] [ str "" ]
        str (FsEntry.name entry |> unwrap)
      ]
    ]

  // ** renderFileList

  member this.renderFileList (trees:Map<HostId,FsTree>) =
    let files: ReactElement list =
      if not (Map.isEmpty trees) then
        trees
        |> Map.toList
        |> List.head
        |> snd
        |> FsTree.files
        |> List.map this.renderFileRow
      else List.empty
    div [ Class "files" ] files

  // ** renderFileInfo

  member this.renderFileInfo() =
    let entry =
      FsEntry.File(
        { Path = { Drive = 'C'; Platform = Windows; Elements = [ "tmp"; "hello"; "bye.txt" ] }
          Name = name "bye.txt"
          MimeType = "text/plain"
          Size = 1173741825u
          Filtered = 0u })

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

  // ** renderBody

  member this.renderBody() =
    let trees =
      this.props.Model.state
      |> Option.map State.fsTrees
      |> Option.defaultValue Map.empty

    div [ Class "asset-browser" ] [
      div [ Class "panel" ] [
        div [ Class "inlay" ] [
          header [ Class "header" ] [ str "Machines" ]
          div [ Class "body" ] [
            this.renderMachineBrowser trees
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
            this.renderFileList trees
          ]
        ]
      ]
      div [ Class "panel" ] [
        div [ Class "inlay" ] [
          header [ Class "header" ] [ str "Fileinfo" ]
          div [ Class "body" ] [
            this.renderFileInfo()
          ]
        ]
      ]
    ]

  // ** render

  member this.render () =
    widget this.props.Id "Asset Browser" None
      (fun _ _ -> this.renderBody())
      this.props.Dispatch
      this.props.Model

  // *** shouldComponentUpdate

  member this.shouldComponentUpdate(nextProps: FileBrowserProps, nextState: FileBrowserState) =
    this.state <> nextState
      || match this.props.Model.state, nextProps.Model.state with
         | Some s1, Some s2 -> distinctRef s1.FsTrees s2.FsTrees
         | None, None -> false
         | _ -> true


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
      com<FileBrowserView,_,_>
        { Id = id
          Model = model
          Dispatch = dispatch } [] }
