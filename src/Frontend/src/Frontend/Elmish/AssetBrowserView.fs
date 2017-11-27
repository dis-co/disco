module Iris.Web.AssetBrowserView

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

// * AssetBrowserProps

type [<Pojo>] AssetBrowserProps =
  { Id: Guid
    Model: Model
    Dispatch: Msg -> unit }

// * AssetBrowserState

type [<Pojo>] AssetBrowserState =
  { Asset: FsEntry option
    OpenDirectories: Set<HostId * FsPath>
    SelectedDirectory: (HostId * FsPath) option
    SelectedAsset: (HostId * FsPath) option
    Machine: HostId option }

// * AssetBrowserState module

module AssetBrowserState =

  let defaultState =
    { Asset = None
      OpenDirectories = Set.ofList []
      SelectedDirectory = None
      SelectedAsset = None
      Machine = None }

  let openDirectories { OpenDirectories = dirs } = dirs
  let setOpenDirectories (dirs: Set<HostId*FsPath>) s =
    { s with OpenDirectories = dirs }

  let modifyOpenDirectories f s =
    s |> openDirectories |> f |> flip setOpenDirectories s

  let addOpenDirectory dir s = modifyOpenDirectories (Set.add dir) s
  let removeOpenDirectory dir s = modifyOpenDirectories (Set.remove dir) s

  let selectDirectory dir s = { s with SelectedDirectory = Some dir }
  let selectAsset dir s = { s with SelectedAsset = Some dir }

// * AssetBrowserView

type AssetBrowserView(props) =
  inherit React.Component<AssetBrowserProps, AssetBrowserState>(props)
  do base.setInitState(AssetBrowserState.defaultState)

  // ** toggleDirectory

  member this.toggleDirectory host fspath =
    let entry = host, fspath
    if Set.contains entry this.state.OpenDirectories then
      this.state
      |> AssetBrowserState.removeOpenDirectory entry
      |> AssetBrowserState.selectDirectory entry
      |> AssetBrowserState.selectAsset entry
      |> this.setState
    else
      this.state
      |> AssetBrowserState.addOpenDirectory entry
      |> AssetBrowserState.selectDirectory entry
      |> AssetBrowserState.selectAsset entry
      |> this.setState

  // ** toggleAsset

  member this.toggleAsset host fspath =
    let entry = host, fspath
    if this.state.SelectedAsset <> Some entry then
      this.state
      |> AssetBrowserState.selectAsset entry
      |> this.setState

  // ** renderDirectoryTree

  member this.renderDirectoryTree host = function
    | FsEntry.File(info) -> str (unwrap info.Name)
    | FsEntry.Directory(info, children) ->
      let hasChildren = Map.count children > 0
      let isOpen = Set.contains (host, info.Path) this.state.OpenDirectories
      let isSelected =
        match this.state.SelectedDirectory with
        | Some entry -> entry = (host, info.Path)
        | _ -> false

      let directories =
        if isOpen then
          children
          |> Map.toList
          |> List.map (snd >> this.renderDirectoryTree host)
        else List.empty

      div [
        classList [
          "directory", true
          "has-children", hasChildren
          "is-open", isOpen && hasChildren
        ]
        OnClick
          (fun e ->
            e.stopPropagation()         /// needed to stop all other toggles from firing
            this.toggleDirectory host info.Path)
      ] [
        span [ classList [ "is-selected", isSelected ] ] [
          i [
            classList [
              "icon fa", true
              "fa-folder-o", (not isOpen && hasChildren) || not hasChildren
              "fa-folder-open-o", isOpen && hasChildren
            ]
          ] [ str "" ]
          str (unwrap info.Name)
        ]
        div [ Class "children" ] directories
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

  // ** renderAssetRow

  member this.renderAssetRow host (entry:FsEntry) =
    let path = FsEntry.path entry
    let isSelected =
      match this.state.SelectedAsset with
      | Some entry -> entry = (host, path)
      | _ -> false

    div [
      Class "file"
      OnClick
        (fun e ->
          e.stopPropagation()
          this.toggleAsset host path)
    ] [
      span [ ] [
        i [
          classList [
            "icon fa", true
            "fa-file-o", not isSelected
            "fa-file", isSelected
          ]
        ] [ str "" ]
        str (FsEntry.name entry |> unwrap)
      ]
    ]

  // ** renderAssetList

  member this.renderAssetList (trees:Map<HostId,FsTree>) =
    let children =
      match this.state.SelectedDirectory with
      | None -> List.empty
      | Some (host, path) ->
        match Map.tryFind host trees with
        | None -> List.empty
        | Some tree ->
          match FsTree.tryFind path tree with
          | Some (FsEntry.Directory(info, children)) ->
            children
            |> Map.filter (fun _ -> FsEntry.isFile)
            |> Map.toList
            |> List.map (snd >> this.renderAssetRow host)
          | _ -> List.empty
    div [ Class "files" ] children

  // ** renderAssetInfo

  member this.renderAssetInfo trees =
    match this.state.SelectedAsset with
    | None -> div [ Class "file-info" ] []
    | Some (host, path) ->
      match Map.tryFind host trees with
      | None -> div [ Class "file-info" ] []
      | Some tree ->
        match FsTree.tryFind path tree with
        | Some (FsEntry.Directory(info,_)) ->
          div [ Class "file-info" ] [
            div [ Class "info" ] [
              div [ Class "columns" ] [
                div [ Class "column" ] [ strong [] [ str "Directory" ] ]
              ]
              div [ Class "columns" ] [
                div [ Class "column is-one-fifth" ] [
                  strong [] [ str "Path:" ]
                ]
                div [ Class "column" ] [
                  str (string path)
                ]
              ]
              div [ Class "columns" ] [
                div [ Class "column is-one-fifth" ] [
                  strong [] [ str "Name:" ]
                ]
                div [ Class "column" ] [
                  str (string info.Name)
                ]
              ]
              div [ Class "columns" ] [
                div [ Class "column is-one-fifth" ] [
                  strong [] [ str "Assets:" ]
                ]
                div [ Class "column" ] [
                  str (string info.Size)
                ]
              ]
              div [ Class "columns" ] [
                div [ Class "column is-one-fifth" ] [
                  strong [] [ str "Filtered:" ]
                ]
                div [ Class "column" ] [
                  str (string info.Filtered)
                ]
              ]
            ]
          ]
        | Some (FsEntry.File info) ->
          div [ Class "file-info" ] [
            div [ Class "info" ] [
              div [ Class "columns" ] [
                div [ Class "column" ] [ strong [] [ str "Asset" ] ]
              ]
              div [ Class "columns" ] [
                div [ Class "column is-one-fifth" ] [
                  strong [] [ str "Path:" ]
                ]
                div [ Class "column" ] [
                  str (string path)
                ]
              ]
              div [ Class "columns" ] [
                div [ Class "column is-one-fifth" ] [
                  strong [] [ str "Name:" ]
                ]
                div [ Class "column" ] [
                  str (string info.Name)
                ]
              ]
              div [ Class "columns" ] [
                div [ Class "column is-one-fifth" ] [
                  strong [] [ str "Type:" ]
                ]
                div [ Class "column" ] [
                  str info.MimeType
                ]
              ]
              div [ Class "columns" ] [
                div [ Class "column is-one-fifth" ] [
                  strong [] [ str "Size:" ]
                ]
                div [ Class "column" ] [
                  str (FsEntry.formatBytes info.Size)
                ]
              ]
            ]
          ]
        | _ -> div [ Class "file-info" ] []

  // ** renderBreadcrumbs

  member this.renderBreadcrumbs () =
    match this.state.SelectedDirectory with
    | None -> header [ Class "header" ] []
    | Some (_, path) ->
      let crumbs = List.map (fun elm -> span [ Class "crumb" ] [ str elm ]) path.Elements
      header [ Class "header" ] [
        div [ Class "bread" ] crumbs
      ]

  // ** renderBody

  member this.renderBody() =
    let trees =
      this.props.Model.state
      |> Option.map State.fsTrees
      |> Option.defaultValue Map.empty

    div [ Class "asset-browser" ] [
      div [ Class "left-panel" ] [
        div [ Class "inlay" ] [
          header [ Class "header" ] [ str "Machines" ]
          div [ Class "body" ] [
            this.renderMachineBrowser trees
          ]
        ]
      ]
      div [ Class "center" ] [
        div [ Class "inlay" ] [
          this.renderBreadcrumbs()
          div [ Class "body" ] [
            this.renderAssetList trees
          ]
        ]
      ]
      div [ Class "right-panel" ] [
        div [ Class "inlay" ] [
          header [ Class "header" ] [ str "Assetinfo" ]
          div [ Class "body" ] [
            this.renderAssetInfo trees
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

  member this.shouldComponentUpdate(nextProps: AssetBrowserProps, nextState: AssetBrowserState) =
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
    member __.Name = Types.Widgets.AssetBrowser
    member this.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 6
        minW = 6
        minH = 2 }
    member this.Render(dispatch, model) =
      com<AssetBrowserView,_,_>
        { Id = id
          Model = model
          Dispatch = dispatch } [] }
