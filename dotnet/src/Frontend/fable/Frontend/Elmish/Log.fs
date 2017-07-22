module Iris.Web.Log

open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop
open Elmish.React
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

let onSorted dispatch (cfg: LogConfig) col =
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

let view dispatch model =
  let cfg = model.logConfig
  from Table
    %["rowsCount" => cfg.viewLogs.Length
      "rowHeight" => 30
      "headerHeight" => 30
      "width" => 30
      "height" => 30]
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

let createLogWidget() =
  { new IWidget with
    member __.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
            equalsRef m1.logs m2.logs
                && equalsRef m1.logConfig m2.logConfig)
        (view dispatch)
        model
  }
