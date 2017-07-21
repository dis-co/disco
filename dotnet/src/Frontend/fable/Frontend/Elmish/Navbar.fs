module Iris.Web.Navbar

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop
open Helpers

let onClick id _ =
    printfn "Clicked %i" id

let dropdown () =
    div [Class "navbar-item has-dropdown is-hoverable"] [
        a [
            Class "navbar-link"
            Style [!!("fontSize", "14px")]
        ] [str "Iris Menu"]
        div [Class "navbar-dropdown"] [
            a [Class "navbar-item"; OnClick (onClick 0)] [str "Create Project"]
            a [Class "navbar-item"; OnClick (onClick 1)] [str "Load Project"]
            a [Class "navbar-item"; OnClick (onClick 2)] [str "Save Project"]
            a [Class "navbar-item"; OnClick (onClick 3)] [str "Unload Project"]
            a [Class "navbar-item"; OnClick (onClick 4)] [str "Shutdown"]
            a [Class "navbar-item"; OnClick (onClick 5)] [str ("Use right click: " + (string false))] // TODO
        ]
    ]

let view () =
    div [] [
        nav [Id "app-header"; Class "navbar "] [
            div [Class "navbar-brand"] [
                a [Class "navbar-item"; Href "http://nsynk.de"] [
                    img [Src "lib/img/nsynk.png"]
                ]
            ]
            div [Class "navbar-menu is-active"] [
                div [Class "navbar-start"] [
                    dropdown()
                ]
                div [Class "navbar-end"] [
                    div [Class "navbar-item"] [
                        str "Iris v0.0.0 - build 123" // TODO
                        // Iris v{this.state.serviceInfo.version} - build {this.state.serviceInfo.buildNumber}
                    ]
                ]
            ]
        ]
    ]
