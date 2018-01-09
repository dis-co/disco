module Disco.Web.AssetBrowserView

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
open Disco.Core
open Disco.Raft
open Disco.Web.Core
open Helpers
open State
open Types

///  ____       _            _
/// |  _ \ _ __(_)_   ____ _| |_ ___
/// | |_) | '__| \ \ / / _` | __/ _ \
/// |  __/| |  | |\ V / (_| | ||  __/
/// |_|   |_|  |_| \_/ \__,_|\__\___|

// * Selectable

[<RequireQualifiedAccess>]
type Selectable =
  | Directories
  | Files
  | Nothing

// * AssetBrowserProps

type [<Pojo>] AssetBrowserProps =
  { Id: Guid
    Model: Model
    Selectable: Selectable
    OnSelect: (FsPath list -> unit) option
    Dispatch: Msg -> unit }

// * AssetBrowserState

type [<Pojo>] AssetBrowserState =
  { Asset: FsEntry option
    OpenDirectories: Set<HostId * FsPath>
    CurrentDirectory: (HostId * FsPath) option
    CurrentAsset: (HostId * FsPath) option
    SelectedFiles: FsPath list
    Machine: HostId option }

// * AssetBrowserState module

module AssetBrowserState =

  let defaultState =
    { Asset = None
      OpenDirectories = Set.ofList []
      CurrentDirectory = None
      CurrentAsset = None
      SelectedFiles = List.empty
      Machine = None }

  let openDirectories { OpenDirectories = dirs } = dirs
  let setOpenDirectories (dirs: Set<HostId*FsPath>) s =
    { s with OpenDirectories = dirs }

  let modifyOpenDirectories f s =
    s |> openDirectories |> f |> flip setOpenDirectories s

  let addOpenDirectory dir s = modifyOpenDirectories (Set.add dir) s
  let removeOpenDirectory dir s = modifyOpenDirectories (Set.remove dir) s

  let selectDirectory dir s = { s with CurrentDirectory = Some dir }
  let selectAsset dir s = { s with CurrentAsset = Some dir }

  let addSelected path s = { s with SelectedFiles = path :: s.SelectedFiles }
  let setSelected files s = { s with SelectedFiles = files }
  let removeSelected i delete s =
    { s with
        SelectedFiles =
          List.fold
            (fun (idx, lst) path ->
              if idx = i && path = delete
              then (idx + 1, lst)
              else (idx + 1, path :: lst))
            (0, List.empty)
            s.SelectedFiles
          |> snd
          |> List.rev }

// * SelectedAssetProps

type [<Pojo>] SelectedAssetProps =
  { key: string
    OnRemove: FsPath -> unit
    FsPath: FsPath }

// * SelectedAssetHandle

let SelectedAssetHandle = Sortable.Handle(fun props ->
  span [Style [Cursor "move"]] [str props.value])

// * SelectedAssetElement

type private SelectedAssetElement(props) =
  inherit React.Component<SelectedAssetProps, obj>(props)

  member this.render() =
    div [ Class "asset-browser-selected-file" ] [
      button [
        Class "button"
        OnClick
          (fun e ->
            e.stopPropagation()
            this.props.OnRemove this.props.FsPath)
      ] [
        i [ Class "fa fa-close" ] []
      ]
      from SelectedAssetHandle { value = string this.props.FsPath } []
    ]

// * SelecteAssetSortable

let private SelectedAssetSortable = Sortable.Element <| fun props ->
  com<SelectedAssetElement,_,_> props.value []

// * SelectedAssetContainer

let private SelectedAssetContainer = Sortable.Container <| fun props ->
  props.items
  |> Array.mapi
    (fun i (props: SelectedAssetProps) ->
      from SelectedAssetSortable { key = props.key; index = i; value = props } [])
  |> Array.toList
  |> div [ Class "selected-files" ]

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
    if this.state.CurrentAsset <> Some entry then
      this.state
      |> AssetBrowserState.selectAsset entry
      |> this.setState

  // ** callSelected

  member this.callSelected state =
    Option.iter (fun f -> f state.SelectedFiles) this.props.OnSelect
    state

  // ** addSelected

  member this.addSelected fspath =
    this.state
    |> AssetBrowserState.addSelected fspath
    |> this.callSelected
    |> this.setState

  // ** removeSelected

  member this.removeSelected i fspath =
    this.state
    |> AssetBrowserState.removeSelected i fspath
    |> this.callSelected
    |> this.setState

  // ** renderDirectoryTree

  member this.renderDirectoryTree host = function
    | FsEntry.File(info) -> str (unwrap info.Name)
    | FsEntry.Directory(info, children) ->
      let hasChildren = Map.count children > 0
      let isOpen = Set.contains (host, info.Path) this.state.OpenDirectories
      let isCurrent =
        match this.state.CurrentDirectory with
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
        span [ classList [ "is-selected", isCurrent ] ] [
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
    header [ Class "machine-icon" ] [
      span [
        Class "disco-output disco-icon icon-host"
        OnClick (fun _ -> Select.clusterMember this.props.Dispatch node)
        Style [ Cursor "pointer" ]
      ] [
        str (unwrap node.HostName)
        span [
          classList [
            "disco-icon icon-bull",true
            "disco-status-off", node.Status <> MemberStatus.Running
            "disco-status-on", node.Status = MemberStatus.Running
          ]
        ] []
      ]
    ]

  // ** renderMachine

  member this.renderMachine trees (node:ClusterMember) =
    let isOpen =
      match this.state.Machine with
      | Some id when id = node.Id -> true
      | _ -> false

    let nodeId = ClusterMember.id node

    let directories =
      if isOpen then
        trees
        |> Map.tryFind nodeId
        |> Option.map (FsTree.directories >> this.renderDirectoryTree nodeId)
        |> Option.map (fun dirs -> div [ Class "directories" ] [ dirs ])
        |> Option.defaultValue (div [ Class "directories" ] [])
      else div [ Class "directories" ] []

    div [
      classList [
        "machine",true
        "is-open", isOpen
      ]
      OnClick (fun _ -> this.setState({ this.state with Machine = Some node.Id }))
    ] [
      div [ Class "machine-details" ] [
        this.renderMachineIcon node
        div [ Class "directory-list" ] [
          directories
        ]
      ]
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
      |> List.sortBy (snd >> ClusterMember.hostName)
      |> List.map (snd >> this.renderMachine trees)

    div [ Class "machines" ] members

  // ** renderAssetRow

  member this.renderAssetRow host (entry:FsEntry) =
    let path = FsEntry.path entry
    let isCurrent =
      match this.state.CurrentAsset with
      | Some entry -> entry = (host, path)
      | _ -> false
    let selectable =
      if this.props.Selectable = Selectable.Nothing
      then str ""
      else
        let isSelected = List.contains path this.state.SelectedFiles
        div [
          classList [
            "pull-right", true
            "is-selected", true
          ]
        ] [
          div [ Class "controls" ] [
            i [
              Class "fa fa-lg fa-plus-square-o"
              OnClick
                (fun e ->
                  e.stopPropagation()
                  this.addSelected path)
            ] []
          ]
        ]
    div [
      Class "file"
      OnClick
        (fun e ->
          e.stopPropagation()
          this.toggleAsset host path)
    ] [
      span [ Class "file-details" ] [
        i [
          classList [
            "icon fa", true
            "fa-file-o", not isCurrent
            "fa-file", isCurrent
          ]
        ] [ str "" ]
        str (FsEntry.name entry |> unwrap)
      ]
      selectable
    ]

  // ** renderAssetList

  member this.renderAssetList (trees:Map<HostId,FsTree>) =
    let children =
      match this.state.CurrentDirectory with
      | None -> List.empty
      | Some (host, path) ->
        match Map.tryFind host trees with
        | None -> List.empty
        | Some tree ->
          match FsTree.tryFind path tree with
          | Some (FsEntry.Directory(info, children)) ->
            let filter _ =
              match this.props.Selectable with
              | Selectable.Nothing | Selectable.Files -> FsEntry.isFile
              | Selectable.Directories -> FsEntry.isDirectory
            children
            |> Map.filter filter
            |> Map.toList
            |> List.map (snd >> this.renderAssetRow host)
          | _ -> List.empty
    div [ Class "files" ] children

  // ** renderDirectoryInfo

  member this.renderDirectoryInfo (info:FsInfo) =
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
            str (string info.Path)
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

  // ** renderFileInfo

  member this.renderFileInfo (info:FsInfo) =
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
            str (string info.Path)
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

  // ** renderAssetInfo

  member this.renderAssetInfo trees =
    match this.state.CurrentAsset with
    | None -> div [ Class "file-info" ] []
    | Some (host, path) ->
      match Map.tryFind host trees with
      | None -> div [ Class "file-info" ] []
      | Some tree ->
        match FsTree.tryFind path tree with
        | Some (FsEntry.Directory(info,_)) -> this.renderDirectoryInfo info
        | Some (FsEntry.File info) -> this.renderFileInfo info
        | _ -> div [ Class "file-info" ] []

  // ** renderBreadcrumbs

  member this.renderBreadcrumbs () =
    match this.state.CurrentDirectory with
    | None -> header [ Class "header" ] []
    | Some (_, path) ->
      let crumbs = List.map (fun elm -> span [ Class "crumb" ] [ str elm ]) path.Elements
      header [ Class "header" ] [
        div [ Class "bread" ] crumbs
      ]

  // ** render

  member this.render () =
    let trees =
      this.props.Model.state
      |> Option.map State.fsTrees
      |> Option.defaultValue Map.empty

    let listHeader, selectedList =
      match this.props.Selectable with
      | Selectable.Nothing -> None, None
      | Selectable.Directories | Selectable.Files ->
        let header = header [ Class "header" ] [ str "Selected" ]
        let selected =
          this.state.SelectedFiles
          |> List.mapi
            (fun i fspath ->
              { key = string fspath + "-" + string i
                FsPath= fspath
                OnRemove = this.removeSelected i })
        let container =
          from SelectedAssetContainer
            { items = Array.ofList selected
              useDragHandle = true
              onSortEnd = fun ev ->
                this.state.SelectedFiles
                |> Array.ofList
                |> fun selected -> Sortable.arrayMove(selected, ev.oldIndex, ev.newIndex)
                |> List.ofArray
                |> flip AssetBrowserState.setSelected this.state
                |> this.callSelected
                |> this.setState }
            []
        Some header, Some container

    let inspector =
      [ Some (header [ Class "header" ] [ str "Assetinfo" ])
        Some (this.renderAssetInfo trees)
        listHeader
        selectedList ]
      |> List.choose id

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
        div [ Class "inlay" ] inspector
      ]
    ]

// * createWidget

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
      let view =
        com<AssetBrowserView,_,_>
          { Id = id
            OnSelect = None
            Selectable = Selectable.Nothing
            Model = model
            Dispatch = dispatch } []
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 -> equalsRef s1.FsTrees s2.FsTrees
          | None, None -> true
          | _ -> false)
        (widget id this.Name None (fun _ _ -> view) dispatch)
        model
    }
