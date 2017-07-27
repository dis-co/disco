module Iris.Web.Log

open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Helpers
open State

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

let getWidth (col: string) =
  match col with
  | Columns.LogLevel -> 70
  | Columns.Time -> 80
  | Columns.Tag -> 75
  | Columns.Tier -> 75
  | Columns.Message -> 400
  | col -> failwithf "Unrecognized log column: %s" col

let readLog (cfg: LogConfig) (idx: int) (col: string) =
  let log = cfg.viewLogs.[idx]
  match col with
  | Columns.LogLevel -> str (string log.LogLevel)
  | Columns.Time -> str (string log.Time)
  | Columns.Tag -> str log.Tag
  | Columns.Tier -> str (string log.Tier)
  | Columns.Message -> str log.Message
  | col -> failwithf "Unrecognized log column: %s" col

let onSorted dispatch (cfg: LogConfig) col _ =
  let direction =
    match cfg.sorting with
    | Some s when s.column = col ->
      s.direction.Reverse
    | _ -> Direction.Ascending
  { cfg with sorting = Some { column = col; direction = direction } }
  |> UpdateLogConfig |> dispatch

let sortableCell dispatch (cfg: LogConfig) col =
    let suffix =
      match cfg.sorting with
      | Some s when s.column = col ->
        match s.direction with
        | Ascending -> " ↓"
        | Descending -> " ↑"
      | _ -> ""
    from Cell
      %["style" => %["cursor" => "pointer"]
        "onClick" => onSorted dispatch cfg col]
      [str ("column" + suffix)]

let makeSortableColumn dispatch cfg col =
  from Column
      %["width" => getWidth col
        "header" => sortableCell dispatch cfg col
        "cell" => fun (props: obj) ->
          from Cell props [readLog cfg (!!props?rowIndex) col]
       ] []

let body dispatch model =
  let cfg = model.logConfig
  from Table
    %["rowsCount" => cfg.viewLogs.Length
      "rowHeight" => 30
      "headerHeight" => 30
      "width" => 700
      "height" => 600]
    [ yield! cfg.columns |> Seq.choose (fun (KeyValue(col, visible)) ->
        if visible
        then makeSortableColumn dispatch cfg col |> Some
        else None)
      yield from Column
        %["width" => getWidth Columns.Message
          "header" => from Cell %[] [str Columns.Message]
          "cell" => fun (props: obj) ->
            // %["style" => %["whiteSpace"=>"nowrap"]]
            from Cell props [readLog cfg (!!props?rowIndex) Columns.Message]
         ] []
    ]

let dropdown title values generator =
  div [Class "iris-dropdown"] [
    div [Class "iris-dropdown-button"] [str title]
    div [Class "iris-dropdown-content"]
      (values |> List.map (fun x -> div [Key x] [generator x]))
  ]

let titleBar dispatch (model: Model) =
  let filter = defaultArg model.logConfig.filter ""
  div [] [
    input [
      Type "text"
      Placeholder "Filter by regex..."
      Value !^filter
      OnChange (fun ev ->
        let filter =
          match !!ev.target?value with
          | null | "" -> None
          | filter -> Some filter
        { model.logConfig with filter = filter } |> UpdateLogConfig |> dispatch
      )
    ]
    dropdown "Columns" ["LogLevel"; "Time"; "Tag"; "Tier"] (fun col ->
      label [] [
        input [
          Type "checkbox"
          Checked model.logConfig.columns.[col]
          OnChange (fun ev ->
            let checked': bool = !!ev.target?``checked``
            let columns = Map.add col checked' model.logConfig.columns
            { model.logConfig with columns = columns } |> UpdateLogConfig |> dispatch
          )
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
          Checked (model.logConfig.logLevel = lv)
          OnChange (fun ev ->
            { model.logConfig with logLevel = lv }
            |> UpdateLogConfig |> dispatch
          )
        ]
        str strLv
      ]
    )
    dropdown "Set Log Level" ["debug"; "info"; "warn"; "err"; "trace"; "button"] (fun strLv ->
      if strLv = "button" then
        button [
          OnClick(fun _ ->
            model.logConfig.setLogLevel
            |> SetLogLevel
            |> ClientContext.Singleton.Post)
        ] [str "SET"]
      else
        let lv = Iris.Core.LogLevel.Parse(strLv)
        label [] [
          input [
            Type "radio"
            Checked (model.logConfig.setLogLevel = lv)
            OnChange (fun ev ->
              { model.logConfig with setLogLevel = lv }
              |> UpdateLogConfig |> dispatch)
          ]
          str strLv
        ]
    )
  ]

let widget name titleBar body dispatch model =
  div [Class "iris-widget"] [
    div [Class "iris-draggable-handle"] [
      span [] [str name]
      div [Class "iris-title-bar"] [
        titleBar dispatch model
      ]
      div [Class "iris-window-control"] [
        button [
          Class "iris-button iris-icon icon-control icon-resize"
          OnClick(fun ev ->
            ev.stopPropagation()
            failwith "TODO" // AddTab id |> dispatch
          )
        ] []
        button [
          Class "iris-button iris-icon icon-control icon-close"
          OnClick(fun ev ->
            ev.stopPropagation()
            failwith "TODO" // RemoveWidget id |> dispatch
          )
        ] []
      ]
    ]
    div [Class "iris-widget-body"] [
      body dispatch model
    ]
  ]

let view dispatch model =
  widget "LOG" titleBar body dispatch model

let createLogWidget(id: System.Guid) =
  { new IWidget with
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 6
        minW = 6; maxW = 20
        minH = 2; maxH = 20 }
    member __.Render(id, dispatch, model) =
      div [Key (string id)] [
        lazyViewWith
          (fun m1 m2 ->
              equalsRef m1.logs m2.logs
                  && equalsRef m1.logConfig m2.logConfig)
          (view dispatch)
          model
      ]
  }
