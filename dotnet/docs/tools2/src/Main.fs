module Iris.DocGenerator.Main

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node.Exports
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack
open System.Text.RegularExpressions
open Helpers

let templatePath = resolve "${entryDir}/../templates/template.hbs"
let docsPath = resolve "${entryDir}/../../files"
let publicPath = resolve "${entryDir}/../public"

// let parseAndGetMembersSummary(path: string): JS.Promise<(string*string) array> = importMember "./util.js"

// parseAndGetMembersSummary "src/Frontend/src/Frontend/bin/Debug/netstandard1.6/Frontend.xml"
// |> Promise.map (fun kvs ->
//     let reg = Regex(@"^(.*?)[(`]")
//     table [ClassName "table"] [
//       tbody [] [
//         for (name, summary) in kvs do
//             let name =
//                 let m = reg.Match(name)
//                 if m.Success then m.Groups.[1].Value else name
//             let summary = summary.Replace("\n", " ")
//             // printfn "%-10s%s\n%-10s%s\n" "Name:" name "Summary:" summary
//             let category =
//                 if name.StartsWith("T")
//                 then "Type"
//                 else "Method"
//             yield tr [] [
//                 td [] [strong [] [str category]]
//                 td [] [str name]
//                 td [] [str summary]
//             ]
//         ]
//       ]
// )

let init() =
    let reg = Regex(@"\.md\b")
    getDirectoryFiles true docsPath
    |> Seq.filter reg.IsMatch
    |> Seq.iter (fun filePath ->
        let targetFile =
            let relPath = Path.relative(docsPath, filePath)
            reg.Replace(Path.join(publicPath, relPath), ".html")
        // printfn "Target file %s" targetFile
        let body =
            reg.Replace(parseMarkdown filePath, ".html")
            |> sprintf """<div class="content">%s</div>"""
        [ "title" ==> "Iris Documentation"
          "body" ==> body ]
        |> parseTemplate templatePath
        |> writeFile targetFile
    )

init()