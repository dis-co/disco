module Iris.Web.LogView

open System.Text.RegularExpressions
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Helpers
open State
open Types

// fixed-data-table styles
importSideEffects("fixed-data-table/dist/fixed-data-table.css")

let Table: React.ComponentClass<obj> = importMember "fixed-data-table"
let Column: React.ComponentClass<obj> = importMember "fixed-data-table"
let Cell: React.ComponentClass<obj> = importMember "fixed-data-table"

module Columns =
  let [<Literal>] LogLevel = "LogLevel"
  let [<Literal>] Time = "Time"
  let [<Literal>] Tag = "Tag"
  let [<Literal>] Tier = "Tier"
  let [<Literal>] Message = "Message"
  let sortable = [LogLevel; Time; Tag; Tier]

let getWidth (col: string) =
  match col with
  | Columns.LogLevel -> 70
  | Columns.Time -> 80
  | Columns.Tag -> 75
  | Columns.Tier -> 75
  | Columns.Message -> 400
  | col -> failwithf "Unrecognized log column: %s" col

[<Emit("new Date($0).toLocaleTimeString()")>]
let printTime (t: uint32): string = jsNative

let readLog (col: string) (log: LogEvent) =
  match col with
  | Columns.LogLevel -> string log.LogLevel
  | Columns.Time -> printTime log.Time
  | Columns.Tag -> log.Tag
  | Columns.Tier -> string log.Tier
  | Columns.Message -> log.Message
  | col -> failwithf "Unrecognized log column: %s" col

let onSorted dispatch (cfg: UserConfig) col _ =
  let direction =
    match cfg.logSorting with
    | Some s when s.column = col ->
      s.direction.Reverse
    | _ -> Direction.Ascending
  { cfg with logSorting = Some { column = col; direction = direction } }
  |> UpdateUserConfig |> dispatch

let sortableCell dispatch (cfg: UserConfig) col =
    let suffix =
      match cfg.logSorting with
      | Some s when s.column = col ->
        match s.direction with
        | Ascending -> " ↓"
        | Descending -> " ↑"
      | _ -> ""
    from Cell
      %["style" => %["cursor" => "pointer"]
        "onClick" => onSorted dispatch cfg col]
      [str (col + " " + suffix)]

let makeSortableColumn dispatch cfg (viewLogs: LogEvent array) col =
  from Column
      %["width" => getWidth col
        "header" => sortableCell dispatch cfg col
        "cell" => fun (props: obj) ->
          from Cell props [readLog col viewLogs.[!!props?rowIndex] |> str]
       ] []

let getViewLogs (model: Model) (cfg: UserConfig) =
  let viewLogs =
    model.logs |> List.toArray
  let viewLogs =
    match cfg.logTextFilter with
    | Some filter ->
      try
        let reg = Regex(filter, RegexOptions.IgnoreCase);
        viewLogs |> Array.filter (fun log -> reg.IsMatch(log.Message))
      with _ -> viewLogs  // Do nothing if the RegExp is not well formed
    | None -> viewLogs
  let viewLogs =
    match cfg.logLevelFilter with
    | Some lv -> viewLogs |> Array.filter (fun log -> log.LogLevel = lv)
    | None -> viewLogs
  match cfg.logSorting with
  | Some sort ->
    viewLogs |> Array.sortWith (fun log1 log2 ->
      let col1 = readLog sort.column log1
      let col2 = readLog sort.column log2
      let res = compare col1 col2
      match sort.direction with
      | Direction.Ascending -> res
      | Direction.Descending -> res * -1)
  | None -> viewLogs

let body dispatch model =
  let viewLogs =
    getViewLogs model model.userConfig
  from Table
    %["rowsCount" => viewLogs.Length
      "rowHeight" => 30
      "headerHeight" => 30
      "width" => 700
      "height" => 600]
    [ yield! Columns.sortable |> Seq.choose (fun col ->
        match Map.tryFind col model.userConfig.logColumns with
        | Some true -> makeSortableColumn dispatch model.userConfig viewLogs col |> Some
        | Some false | None -> None)
      yield from Column
        %["width" => getWidth Columns.Message
          "header" => from Cell %[] [str Columns.Message]
          "cell" => fun (props: obj) ->
            // %["style" => %["whiteSpace"=>"nowrap"]]
            from Cell props [readLog Columns.Message viewLogs.[!!props?rowIndex] |> str]
         ] []
    ]

let dropdown title values generator =
  div [Class "iris-dropdown"] [
    div [Class "iris-dropdown-button"] [str title]
    div [Class "iris-dropdown-content"]
      (values |> List.map (fun x -> div [Key x] [generator x]))
  ]

let titleBar dispatch (model: Model) =
    let filter = defaultArg model.userConfig.logTextFilter ""
    div [] [
      input [
        Type "text"
        Placeholder "Filter by regex..."
        DefaultValue filter
        OnChange (fun ev ->
          let filter =
            match !!ev.target?value with
            | null | "" -> None
            | filter -> Some filter
          { model.userConfig with logTextFilter = filter } |> UpdateUserConfig |> dispatch)
      ]
      dropdown "Columns" Columns.sortable (fun col ->
        label [] [
          input [
            Type "checkbox"
            Checked model.userConfig.logColumns.[col]
            OnChange (fun ev ->
              let checked': bool = !!ev.target?``checked``
              let columns = Map.add col checked' model.userConfig.logColumns
              { model.userConfig with logColumns = columns } |> UpdateUserConfig |> dispatch)
          ]
          str col
        ]
      )
      dropdown "Log Filter" ["debug"; "info"; "warn"; "err"; "trace"; "none"] (fun strLv ->
        let lv =
          match strLv with
          | "none" -> None
          | lv -> Some(Iris.Core.LogLevel.Parse(lv))
        label [] [
          input [
            Type "radio"
            Checked (model.userConfig.logLevelFilter = lv)
            OnChange (fun ev ->
              { model.userConfig with logLevelFilter = lv }
              |> UpdateUserConfig |> dispatch)
          ]
          str strLv
        ]
      )
      dropdown "Set Log Level" ["debug"; "info"; "warn"; "err"; "trace"; "button"] (fun strLv ->
        if strLv = "button" then
          button [
            OnClick(fun _ ->
              model.userConfig.setLogLevel
              |> SetLogLevel
              |> ClientContext.Singleton.Post)
          ] [str "SET"]
        else
          let lv = Iris.Core.LogLevel.Parse(strLv)
          label [] [
            input [
              Type "radio"
              Checked (model.userConfig.setLogLevel = lv)
              OnChange (fun ev ->
                { model.userConfig with setLogLevel = lv }
                |> UpdateUserConfig |> dispatch)
            ]
            str strLv
          ])
    ]

let createWidget (id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.Log
    member this.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 6
        minW = 6; maxW = 20
        minH = 2; maxH = 20 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          equalsRef m1.logs m2.logs
              && equalsRef m1.userConfig m2.userConfig)
        (widget id this.Name (Some titleBar) body dispatch)
        model
  }
