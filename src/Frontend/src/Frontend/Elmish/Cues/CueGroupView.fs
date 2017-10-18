module Iris.Web.Cues.CueGroupView

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Iris.Web
open Types
open Helpers

// ** Types

type [<Pojo>] State =
  { IsOpen: bool
    IsHighlit: bool }

type [<Pojo>] Props =
  { key: string
    Model: Model
    State: Iris.Core.State
    Cue: Cue
    CueRef: CueReference
    CueGroup: CueGroup
    CueList: CueList
    CueIndex: int
    CueGroupIndex: int
    SelectedCueIndex: int
    SelectedCueGroupIndex: int
    SelectCue: int -> int -> unit
    Dispatch: Elmish.Dispatch<Msg> }

// ** React components

type Component(props) =
  inherit React.Component<Props, State>(props)
  let mutable selfRef: Browser.Element option = None
  let mutable disposable: IDisposable option = None
  do base.setInitState({ IsOpen = false; IsHighlit = false })

  member this.render() =
    div [] []