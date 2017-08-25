module XmlParser

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open System.Text.RegularExpressions

type [<Pojo>] IContext =
    { title: string
      react: ReactElement }

let parseAndDisplay(path: string): unit = importMember "./util.js"
let parseAndGetMembersSummary(path: string): JS.Promise<(string*string) array> = importMember "./util.js"
let printPage(ctx: IContext, templatePath: string, targetPath: string): unit = importMember "./util.js"

let resolve (path: string) =
    Node.Exports.Path.resolve(Node.Globals.__dirname, path)

parseAndGetMembersSummary "src/Frontend/src/Frontend/bin/Debug/netstandard1.6/Frontend.xml"
|> Promise.map (fun kvs ->
    let reg = Regex(@"^(.*?)[(`]")
    table [ClassName "table"] [
      tbody [] [
        for (name, summary) in kvs do
            let name =
                let m = reg.Match(name)
                if m.Success then m.Groups.[1].Value else name
            let summary = summary.Replace("\n", " ")
            // printfn "%-10s%s\n%-10s%s\n" "Name:" name "Summary:" summary
            yield tr [] [
                td [] [str name]
                td [] [str summary]
            ]
        ]
      ]
)
|> Promise.iter (fun reactEl ->
    let ctx =
        { title = "Iris Frontend Documentation"
          react = reactEl }
    printPage(ctx, resolve "${entryDir}/template.hbs", resolve "${entryDir}/../public/index.html")
)
