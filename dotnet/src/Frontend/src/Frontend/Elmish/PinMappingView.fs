module Iris.Web.PinMappingView

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

let touchesElement(el: Browser.Element option, x: float, y: float): bool = importMember "../../js/Util"

let inline padding5() =
  Style [PaddingLeft "5px"]

let inline topBorder() =
  Style [BorderTop "1px solid lightgray"]

let inline padding5AndTopBorder() =
  Style [PaddingLeft "5px"; BorderTop "1px solid lightgray"]

let renderPin (model: Model) (pin: Pin) =
  com<PinView.PinView,_,_>
    { key = string pin.Id
      pin = pin
      useRightClick = model.userConfig.useRightClick
      slices = Some pin.Slices
      updater = None
      onDragStart = None } []

type [<Pojo>] PinHoleProps =
  { Classes: string list
    Padding: bool
    AddPin: Pin -> unit
    Render: unit -> ReactElement }

type [<Pojo>] PinHoleState =
  { IsHighlit: bool }

type PinHole(props) =
  inherit Component<PinHoleProps, PinHoleState>(props)
  let mutable selfRef: Browser.Element option = None
  let mutable disposable: IDisposable option = None
  do base.setInitState({ IsHighlit = false })

  member this.componentWillUnmount() =
    disposable |> Option.iter (fun disp -> disp.Dispose())

  member this.componentDidMount() =
    disposable <-
      Drag.observe()
      |> Observable.choose(function
        | Drag.Moved(x,y,Drag.Pin pin) -> Some(pin,x,y,false)
        | Drag.Stopped(x,y,Drag.Pin pin) -> Some(pin,x,y,true))
      |> Observable.subscribe(fun (pin,x,y,stopped) ->
        let isHighlit  =
          if touchesElement(selfRef, x, y) then
            if not stopped then
              true
            else
              this.props.AddPin(pin)
              false
          else
            false
        if isHighlit <> this.state.IsHighlit then
          this.setState({ IsHighlit = isHighlit })
      ) |> Some

  member this.render() =
    let isHighlit = this.state.IsHighlit
    let classes =
      [ for c in this.props.Classes do
          yield c, true
        yield "iris-highlight", isHighlit
        yield "iris-blue", isHighlit]
    td [classList classes
        Style [PaddingLeft (if this.props.Padding then "5px" else "0")
               Border "2px solid transparent"]
        Ref (fun el -> selfRef <- Option.ofObj el)]
       [this.props.Render()]

type [<Pojo>] PinMappingProps =
  { Id: Guid
    Name: string
    Model: Model
    Dispatch: Msg -> unit }

type [<Pojo>] PinMappingState =
  { SourceCandidate: Pin option
    SinkCandidates: Pin list }

type PinMappingView(props) =
  inherit Component<PinMappingProps, PinMappingState>(props)
  do base.setInitState({ SourceCandidate = None; SinkCandidates = [] })

  member this.shouldComponentUpdate(nextProps, nextState, nextContext) =
    match this.props.Model.state, nextProps.Model.state with
    | Some s1, Some s2 ->
      distinctRef s1.Project s2.Project
    | None, None -> false
    | _ -> true

  member this.renderLastRow() =
    let disabled =
      Option.isNone this.state.SourceCandidate
        || List.isEmpty this.state.SinkCandidates
    tr [
      Key "iris-add-pinmapping"
      Class "iris-add-pinmapping"
    ] [
      com<PinHole,_,_>
        { Classes = ["width-20"]
          Padding = true
          AddPin = fun pin -> failwith "Add pin"
          Render = fun () -> div [] [] } []
      com<PinHole,_,_>
        { Classes = ["width-75"]
          Padding = true
          AddPin = fun pin -> failwith "Add pin"
          Render = fun () -> div [] [] } []
      td [Class "width-5"] [
        button [
          Class "iris-button iris-icon icon-more"
          Disabled disabled
          OnClick (fun ev ->
            ev.stopPropagation()
            printfn "TODO: Add PinMapping")
        ] []
      ]
    ]

  member this.renderBody() =
    let model = this.props.Model
    table [Class "iris-table"] [
      thead [] [
        tr [] [
          th [Class "width-20"; padding5()] [str "Source"]
          th [Class "width-75"] [str "Sinks"]
          th [Class "width-5"] []
        ]
      ]
      tbody [] [
        match model.state with
        | Some state ->
          for kv in state.PinMappings do
            let source =
              Lib.findPin kv.Value.Source state
              |> renderPin model
            let sinks =
              kv.Value.Sinks
              |> Seq.map (fun id -> Lib.findPin id state |> renderPin model)
              |> Seq.toList
            yield tr [Key (string kv.Key)] [
              td [Class "width-20"; padding5AndTopBorder()] [source]
              td [Class "width-75"; topBorder()] sinks
              td [Class "width-5"; topBorder()] [
                button [
                  Class "iris-button iris-icon icon-close"
                  OnClick (fun ev ->
                    ev.stopPropagation()
                    printfn "TODO: Remove PinMapping")
                ] []
              ]
            ]
          yield this.renderLastRow()
        | None -> ()
        ]
      ]

  member this.render() =
    widget this.props.Id this.props.Name
      None
      (fun _ _ -> this.renderBody()) this.props.Dispatch this.props.Model

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.PinMapping
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 4; maxW = 10
        minH = 1; maxH = 10 }
    member this.Render(dispatch, model) =
      com<PinMappingView,_,_>
        { Id = this.Id
          Name = this.Name
          Model = model
          Dispatch = dispatch } []
  }
