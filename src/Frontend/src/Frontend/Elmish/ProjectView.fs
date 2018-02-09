module Disco.Web.ProjectView

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
open Disco.Web.Core
open Helpers
open State
open Types

importSideEffects "../../../lib/react-ui-tree/react-ui-tree.css"
let Tree: ComponentClass<obj> = importDefault "../../../lib/react-ui-tree/react-ui-tree"

type [<Pojo>] TreeNode =
  { ``module``: string; children: TreeNode[] option }

let project2tree (p: DiscoProject) =
  let leaf m = { ``module``=m; children=None }
  let node m c = { ``module``=m; children=Some c }
  let rec obj2tree k (o: obj) =
    Fable.Import.JS.Object.getOwnPropertyNames(o)
    |> Seq.map (fun k ->
    match box o?(k) with
      | :? (obj[]) as arr ->
        arr2tree k arr
      | :? IDictionary<obj, obj> as dic ->
        dic |> Seq.map (fun kv -> obj2tree (string kv.Key) kv.Value)
        |> Seq.toArray |> node k
      | v -> sprintf "%s: %O" k v |> leaf)
    |> Seq.toArray
    |> node k
  and arr2tree k (arr: obj[]) =
    Array.mapi (fun i v -> obj2tree (string i) v) arr
    |> node k
  let cfg2tree (c: DiscoConfig) =
    [| leaf ("MachineId: " + string c.Machine.MachineId)
    ;  obj2tree "Audio" c.Audio
    ;  obj2tree "Clients" c.Clients
    ;  obj2tree "Raft" c.Raft
    ;  obj2tree "Timing" c.Timing
    ;  leaf ("ActiveSite" + string c.ActiveSite)
    ;  arr2tree "Sites" (c.Sites |> Map.toArray |> Array.map (snd >> box))
    |] |> node "Config"
  [| leaf ("Id: " + string p.Id)
  ;  leaf ("Name: " + unwrap p.Name)
  ;  leaf ("Path: " + unwrap p.Path)
  ;  leaf ("CreatedOn: " + p.CreatedOn)
  ;  leaf ("LastSaved: " + defaultArg p.LastSaved "unknown")
  ;  leaf ("Copyright: " + defaultArg p.Copyright "unknown")
  ;  leaf ("Author: " + defaultArg p.Author "unknown")
  ;  cfg2tree p.Config
  |] |> node "Project"

let body dispatch (model: Model) =
  match model.state with
  | None -> p [Style [Margin 10.]] [str "No project loaded"]
  | Some state ->
    from Tree %["paddingLeft" => 20
                "tree" => project2tree(state.Project)
                "freeze" => true
                "renderNode" => fun (node: TreeNode) ->
                  span [] [str node.``module``]
                ] []

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.ProjectView
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 3; h = 6
        minW = 1
        minH = 1 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 ->
            equalsRef s1.Project s2.Project
          | None, None -> true
          | _ -> false)
        (widget id this.Name None body dispatch)
        model
  }
