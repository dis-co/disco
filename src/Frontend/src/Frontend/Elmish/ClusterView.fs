module Disco.Web.ClusterView

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

let titleBar dispatch (model: Model) =
  button [
    Class "disco-button"
    OnClick(fun _ -> Modal.AddMember() :> IModal |> OpenModal |> dispatch)
    ] [str "Add member"]

let activeConfig dispatch state =
  let config = state.Project.Config
  let current = state.Project.Config.Machine
  let members =
    config.ActiveSite |> Option.bind (fun activeSite ->
      config.Sites |> Seq.tryFind (fun site -> site.Id = activeSite))
    |> Option.map (fun site -> site.Members)
    |> Option.defaultValue Map.empty
  table [Class "disco-table"] [
    thead [] [
      tr [] [
        th [Class "width-25"] [str "Host"]
        th [Class "width-15"] [str "IP"]
        th [Class "width-5"]  [str "Http"]
        th [Class "width-5"]  [str "Raft"]
        th [Class "width-5"]  [str "Api"]
        th [Class "width-5"]  [str "Git"]
        th [Class "width-10"] [str "WebSocket"]
        th [Class "width-10"] [str "State"]
        th [Class "width-10"] [str "Status"]
        th [Class "width-10"] [str "Remove"]
      ]
    ]
    tbody [] (
      members |> Seq.map (fun kv ->
        let node = kv.Value
        tr [
          Key (string kv.Key)
          classList [
            "is-current", node.Id = current.MachineId
          ]
        ] [
          td [Class "width-25"] [
            span [
              Class "disco-output disco-icon icon-host"
              OnClick (fun _ -> Select.clusterMember dispatch node)
              Style [ Cursor "pointer" ]
            ] [
              str (unwrap node.HostName)
              span [
                classList [
                  "disco-icon icon-bull", true
                  "disco-status-off", node.Status = MemberStatus.Failed
                  "disco-status-on", node.Status = MemberStatus.Running
                  "disco-status-warning", node.Status = MemberStatus.Joining
                ]
              ] []
            ]
          ]
          td [Class "width-15"] [str (string node.IpAddress)]
          td [Class "width-5"]  [str (string node.HttpPort)]
          td [Class "width-5"]  [str (string node.RaftPort)]
          td [Class "width-5"]  [str (string node.ApiPort)]
          td [Class "width-5"]  [str (string node.GitPort)]
          td [Class "width-10"] [str (string node.WsPort)]
          td [
            classList [
              "width-10",true
              "is-leader", node.State = MemberState.Leader
            ]
          ] [str (string node.State)]
          td [Class "width-10"] [str (string node.Status)]
          td [Class "width-10"] [
            button [
              Class "disco-button disco-icon icon-close"
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

let private addMember service =
  service
  |> DiscoveredService.tryFindPort ServiceType.Http
  |> function
  | None -> ()
  | Some port ->
    Array.iter
      (fun ip -> Lib.addMember (string ip, unwrap port))
      (DiscoveredService.addressList service)

let private discoveredServices dispatch (state:State) =
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
            [
              button [
                Class "button disco-button"
                OnClick
                  (fun e ->
                    e.stopPropagation()
                    addMember service)
              ] [ i [ Class "fa fa-plus" ] [] ]
            ]
        let http =
          service
          |> DiscoveredService.tryFindPort ServiceType.Http
          |> Option.map string
          |> Option.defaultValue ""
        let raft =
          service
          |> DiscoveredService.tryFindPort ServiceType.Raft
          |> Option.map string
          |> Option.defaultValue ""
        let git =
          service
          |> DiscoveredService.tryFindPort ServiceType.Git
          |> Option.map string
          |> Option.defaultValue ""
        let api =
          service
          |> DiscoveredService.tryFindPort ServiceType.Api
          |> Option.map string
          |> Option.defaultValue ""
        let websocket =
          service
          |> DiscoveredService.tryFindPort ServiceType.WebSocket
          |> Option.map string
          |> Option.defaultValue ""
        tr [] [
          td [Class "width-25"] [str service.HostName]
          td [Class "width-15"] addresses
          td [Class "width-5"]  [str http]
          td [Class "width-5"]  [str raft]
          td [Class "width-5"]  [str api]
          td [Class "width-5"]  [str git]
          td [Class "width-10"] [str websocket]
          td [Class "width-20"] [str status]
          td [Class "width-10"] actions
        ])
  table [Class "disco-table"] [
    thead [] [
      tr [] [
        th [Class "width-25"] [str "Host"]
        th [Class "width-15"] [str "Addresses"]
        th [Class "width-5"] [str "Http"]
        th [Class "width-5"] [str "Raft"]
        th [Class "width-5"] [str "Api"]
        th [Class "width-5"] [str "Git"]
        th [Class "width-10"] [str "WebSocket"]
        th [Class "width-20"] [str "Status"]
        th [Class "width-10"] [str "Actions"]
      ]
    ]
    tbody [] services
  ]

let body dispatch (model: Model) =
  match model.state with
  | None -> div [ Class "disco-cluster" ] []
  | Some state ->
    div [ Class "disco-cluster" ] [
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
        w = 10; h = 10
        minW = 5
        minH = 5 }
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
