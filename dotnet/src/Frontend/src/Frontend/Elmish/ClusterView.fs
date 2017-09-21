module Iris.Web.ClusterView

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

let inline padding5() =
  Style [PaddingLeft "5px"]

let inline topBorder() =
  Style [BorderTop "1px solid lightgray"]

let inline padding5AndTopBorder() =
  Style [PaddingLeft "5px"; BorderTop "1px solid lightgray"]

let titleBar dispatch (model: Model) =
  button [
    Class "iris-button"
    OnClick(fun _ -> Modal.AddMember() :> IModal |> OpenModal |> dispatch)
    ] [str "Add member"]

let body dispatch (model: Model) =
  match model.state with
  | None -> table [Class "iris-table"] []
  | Some state ->
    let config = state.Project.Config
    let members =
      config.ActiveSite |> Option.bind (fun activeSite ->
        config.Sites |> Seq.tryFind (fun site -> site.Id = activeSite))
      |> Option.map (fun site -> site.Members)
      |> Option.defaultValue Map.empty
    table [Class "iris-table"] [
      thead [] [
        tr [] [
          th [Class "width-20"; padding5()] [str "Host"]
          th [Class "width-15"] [str "IP"]
          th [Class "width-25"] []
          th [Class "width-15"] []
          th [Class "width-15"] []
          th [Class "width-5"] []
          th [Class "width-5"] []
        ]
      ]
      tbody [] (
        members |> Seq.map (fun kv ->
          let node = kv.Value
          tr [Key (string kv.Key)] [
            td [Class "width-20";padding5AndTopBorder()] [
              span [
                Class "iris-output iris-icon icon-host"
                OnClick (fun _ -> Select.clusterMember dispatch node)
                Style [ Cursor "pointer" ]
              ] [
                str (unwrap node.HostName)
                span [Class "iris-icon icon-bull iris-status-off"] []
              ]
            ]
            td [Class "width-15"; topBorder()] [str (string node.IpAddr)]
            td [Class "width-25"; topBorder()] [str (string node.Port)]
            td [Class "width-15"; topBorder()] [str (string node.State)]
            td [Class "width-15"; topBorder()] [str "shortkey"]
            td [Class "width-5"; topBorder()] [
              button [Class "iris-button iris-icon icon-autocall"] []
            ]
            td [Class "width-5"; topBorder()] [
              button [
                Class "iris-button iris-icon icon-close"
                OnClick (fun ev ->
                  ev.stopPropagation()
                  match Config.findMember config kv.Key with
                  | Right mem -> RemoveMember mem |> ClientContext.Singleton.Post
                  | Left error -> printfn "Cannot find member in config: %O" error)
              ] []
            ]
          ]
        ) |> Seq.toList
      )
    ]

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.Cluster
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 4; maxW = 10
        minH = 1; maxH = 10 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 ->
            equalsRef s1.Project s2.Project
          | None, None -> true
          | _ -> false)
        (widget id this.Name (Some titleBar) body dispatch)
        model
  }
