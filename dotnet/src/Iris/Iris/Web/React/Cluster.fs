module Iris.Web.React.Cluster

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node

module R = Fable.Helpers.React
open R.Props

type RCom = React.ComponentClass<obj>

// let deepOrange500 = importMember<string> "material-ui/styles/colors"
// let RaisedButton = importDefault<RCom> "material-ui/RaisedButton"
// let Dialog = importDefault<RCom> "material-ui/Dialog"
// let FlatButton = importDefault<RCom> "material-ui/FlatButton"
// let MuiThemeProvider = importDefault<RCom> "material-ui/styles/MuiThemeProvider"
// let getMuiTheme = importDefault<obj->obj> "material-ui/styles/getMuiTheme"

// Dynamic programming helpers
let inline (~%) x = createObj x
let inline (=>) x y = x ==> y
let inline (!) x y = x ==> y

type ClusterViewProps = { NodeIds: string list }

let clusterView (props: ClusterViewProps) =
    props.NodeIds
    |> List.map (fun id -> R.li [] [R.str id])
    |> R.ul []
