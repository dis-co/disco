module Iris.Web.Navbar

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop

let onClick id _ =
    printfn "Clicked %i" id

let dropdown () =
    div [ClassName "navbar-item has-dropdown is-hoverable"] [
        a [
            ClassName "navbar-link"
            Style [!!("fontSize", "14px")]
        ] [str "Iris Menu"]
        div [ClassName "navbar-dropdown"] [
            a [ClassName "navbar-item"; OnClick (onClick 0)] [str "Create Project"]
            a [ClassName "navbar-item"; OnClick (onClick 1)] [str "Load Project"]
            a [ClassName "navbar-item"; OnClick (onClick 2)] [str "Save Project"]
            a [ClassName "navbar-item"; OnClick (onClick 3)] [str "Unload Project"]
            a [ClassName "navbar-item"; OnClick (onClick 4)] [str "Shutdown"]
            a [ClassName "navbar-item"; OnClick (onClick 5)] [str ("Use right click: " + (string false))] // TODO
        ]
    ]

let view () =
    div [] [
        nav [Id "app-header"; ClassName "navbar "] [
            div [ClassName "navbar-brand"] [
                a [ClassName "navbar-item"; Href "http://nsynk.de"] [
                    img [Src "lib/img/nsynk.png"]
                ]
            ]
            div [ClassName "navbar-menu is-active"] [
                div [ClassName "navbar-start"] [
                    dropdown()
                ]
                div [ClassName "navbar-end"] [
                    div [ClassName "navbar-item"] [
                        str "Iris v0.0.0 - build 123" // TODO
                        // Iris v{this.state.serviceInfo.version} - build {this.state.serviceInfo.buildNumber}
                    ]
                ]
            ]
        ]
    ]
