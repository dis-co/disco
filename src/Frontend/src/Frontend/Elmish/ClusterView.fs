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
open Iris.Raft
open Iris.Web.Core
open Helpers
open State
open Types

let titleBar dispatch (model: Model) =
  button [
    Class "iris-button"
    OnClick(fun _ -> Modal.AddMember() :> IModal |> OpenModal |> dispatch)
    ] [str "Add member"]

let activeConfig dispatch state =
  let config = state.Project.Config
  let members =
    config.ActiveSite |> Option.bind (fun activeSite ->
      config.Sites |> Seq.tryFind (fun site -> site.Id = activeSite))
    |> Option.map (fun site -> site.Members)
    |> Option.defaultValue Map.empty
  table [Class "iris-table"] [
    thead [] [
      tr [] [
        th [Class "width-10"] [str "Host"]
        th [Class "width-10"] [str "IP"]
        th [Class "width-10"] [str "Http"]
        th [Class "width-10"] [str "Raft"]
        th [Class "width-10"] [str "Api"]
        th [Class "width-10"] [str "WebSocket"]
        th [Class "width-10"] [str "Git"]
        th [Class "width-10"] [str "State"]
        th [Class "width-10"] [str "Status"]
        th [Class "width-10"] [str "Remove"]
      ]
    ]
    tbody [] (
      members |> Seq.map (fun kv ->
        let node = kv.Value
        tr [Key (string kv.Key)] [
          td [Class "width-10"] [

            span [
              Class "iris-output iris-icon icon-host"
              OnClick (fun _ -> Select.clusterMember dispatch node)
              Style [ Cursor "pointer" ]
            ] [
              str (unwrap node.HostName)
              span [
                classList [
                  "iris-icon icon-bull", true
                  "iris-status-off", node.Status = MemberStatus.Failed
                  "iris-status-on", node.Status = MemberStatus.Running
                  "iris-status-warning", node.Status = MemberStatus.Joining
                ]
              ] []
            ]
          ]
          td [Class "width-10"] [str (string node.IpAddress)]
          td [Class "width-10"] [str (string node.HttpPort)]
          td [Class "width-10"] [str (string node.RaftPort)]
          td [Class "width-10"] [str (string node.ApiPort)]
          td [Class "width-10"] [str (string node.WsPort)]
          td [Class "width-10"] [str (string node.GitPort)]
          td [Class "width-10"] [str (string node.State)]
          td [Class "width-10"] [str (string node.Status)]
          td [Class "width-10"] [
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

let discoveredServices dispatch (state:State) =
  let services =
    state.DiscoveredServices
    |> Map.toList
    |> List.map
      (fun (_,service) ->
        let status =
          match service.Status with
          | Idle -> "Idle"
          | Busy (_, name) -> sprintf "Busy (%A)" name
        let addresses =
          service.AddressList
          |> Array.toList
          |> List.distinct
          |> List.map (fun addr -> div [ ] [ str (string addr) ])
        let actions =
          match service.Status with
          | Busy _ -> []
          | Idle ->
            [ button [
                Class "button iris-button"
              ] [ i [ Class "fa fa-plus" ] [] ] ]
        tr [] [
          td [Class "width-20"] [str (service.Id.Prefix())]
          td [Class "width-20"] [str service.HostName]
          td [Class "width-20"] addresses
          td [Class "width-20"] [str status]
          td [Class "width-20"] actions
        ])
  table [Class "iris-table"] [
    thead [] [
      tr [] [
        th [Class "width-20"] [str "Id"]
        th [Class "width-20"] [str "Host"]
        th [Class "width-20"] [str "Addresses"]
        th [Class "width-20"] [str "Status"]
        th [Class "width-20"] [str "Actions"]
      ]
    ]
    tbody [] services
  ]

let body dispatch (model: Model) =
  match model.state with
  | None -> div [ Class "iris-cluster" ] []
  | Some state ->
    div [ Class "iris-cluster" ] [
      div [ Class "headline" ] [ h2 [] [ str "Active Configuration" ] ]
      activeConfig dispatch state
      div [ Class "headline" ] [ h2 [] [ str "Discovered Services" ] ]
      discoveredServices dispatch state
    ]

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.Cluster
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 4
        minH = 1 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 ->
            equalsRef s1.Project s2.Project &&
            equalsRef s1.DiscoveredServices s2.DiscoveredServices
          | None, None -> true
          | _ -> false)
        (widget id this.Name (Some titleBar) body dispatch)
        model
  }
