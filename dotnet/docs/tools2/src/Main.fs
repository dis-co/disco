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
let xmlDocs =
    ["Frontend", resolve "${entryDir}/../../../src/Frontend/src/Frontend/bin/Debug/netstandard1.6/Frontend.xml"
    ]

let parseAndGetMembersSummary(path: string): JS.Promise<(string*string) array> = importMember "./util.js"

let parseApiReference title xmlDocPath = promise {
    let! kvs = parseAndGetMembersSummary xmlDocPath
    let reg = Regex(@"^(\w+):([^(`]+)")
    return div [] [
        h1 [ClassName "title is-1"] [str title]
        table [ClassName "table"] [
          tbody [] [
            for (name, summary) in kvs do
                let m = reg.Match(name)
                let category, name =
                    if m.Success then
                        let cat = if m.Groups.[1].Value = "T" then "Type" else "Method"
                        cat, m.Groups.[2].Value
                    else "Method", name
                let summary = summary.Replace("\n", " ")
                // printfn "%-10s%s\n%-10s%s\n" "Name:" name "Summary:" summary
                yield tr [] [
                    td [] [str category]
                    td [] [strong [] [str name]]
                    td [] [str summary]
                ]
            ]
          ]
    ]
}

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

    promise {
        for title, xmlDocPath in xmlDocs do
            let targetFile = Path.join(publicPath, title.ToLower(), "api_reference.html")
            let title = title + " API Reference"
            let! reactEl = parseApiReference title xmlDocPath
            [ "title" ==> title
              "body" ==> parseReactStatic reactEl ]
            |> parseTemplate templatePath
            |> writeFile targetFile
    } |> Promise.start

init()