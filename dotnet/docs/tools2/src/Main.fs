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
open System.Collections.Generic
open Helpers

let fontawesomePath = resolve "${entryDir}/../public/css/font-awesome/css/font-awesome.min.css"
let stylesPath = resolve "${entryDir}/../public/css/styles.css"
let templatePath = resolve "${entryDir}/../templates/template.hbs"
let docsPath = resolve "${entryDir}/../../files"
let publicPath = resolve "${entryDir}/../public"
let xmlDocs =
    ["Frontend", resolve "${entryDir}/../../../src/Frontend/src/Frontend/bin/Debug/netstandard1.6/Frontend.xml"
    ]

let parseXmlDocAndGetMembers(path: string): JS.Promise<IDictionary<string, string[]> array> =
    importMember "./util.js"

type MemberInfo =
    { name: string
      summary: string option }

type TypeInfo =
    { summary: string option
      members: MemberInfo list
      nestedTypes: Map<string, TypeInfo> }

let makeType summary members nestedTypes =
    { summary = summary
      members = members
      nestedTypes =nestedTypes }

let rec addType nameParts summary types: Map<string, TypeInfo> =
    match nameParts with
    | [] -> failwith "unexpected empty type name"
    | [part] ->
        let typ = makeType summary [] Map.empty
        Map.add part typ types
    | part::parts ->
        let parent =
            match Map.tryFind part types with
            | Some parent -> { parent with nestedTypes = addType parts summary parent.nestedTypes }
            | None -> addType parts summary Map.empty |> makeType None []
        Map.add part parent types

let rec addMembers nameParts members types: Map<string, TypeInfo> =
    match nameParts with
    | [] -> failwith "unexpected empty type name"
    | [part] ->
        let typ =
            match Map.tryFind part types with
            | Some typ -> { typ with members = members }
            | None -> makeType None members Map.empty
        Map.add part typ types
    | part::parts ->
        let parent =
            match Map.tryFind part types with
            | Some parent -> { parent with nestedTypes = addMembers parts members parent.nestedTypes }
            | None -> addMembers parts members Map.empty |> makeType None []
        Map.add part parent types

let rec printTypes indent types =
    for (KeyValue(name, typ)) in types do
        printfn "%s%s >> %s" indent name (defaultArg typ.summary "")
        for memb in typ.members do
            printfn "%s  %s: %s" indent memb.name (defaultArg memb.summary "")
        printTypes (indent + "  ") typ.nestedTypes

let tryAndTrim (k: string) (dic: IDictionary<string, string[]>) =
    match dic.TryGetValue(k) with
    | true, ar ->
        match Array.tryHead ar with
        | Some item -> String.trimWhitespace item |> Some
        | None -> None
    | false, _ -> None

let parseApiReference title xmlDocPath = promise {
    let! members = parseXmlDocAndGetMembers xmlDocPath
    let types, members =
        ((Map.empty, []), members) ||> Seq.fold (fun (types, members) memb ->
            match tryAndTrim "name" memb with
            | Some name ->
                let name = String.processName name
                let summary = tryAndTrim "summary" memb
                if name.StartsWith("T:") then // Type
                    let nameParts = name.[2..].Split('.') |> Array.toList
                    addType nameParts summary types, members
                elif name.StartsWith("M:") then // Method
                    let memb = { name = name.[2..]; summary = summary; }
                    types, memb::members
                else
                    failwithf "Unknown member: %s" name
            | None -> types, members)
    let types =
        members
        |> Seq.groupBy (fun x -> let i = x.name.LastIndexOf('.') in x.name.[i+1..])
        |> Seq.fold (fun types (typName, members) ->
            let members = Seq.toList members
            let nameParts = typName.Split('.') |> Array.toList
            addMembers nameParts members types) types
    printTypes "" types
    let reg = Regex(@"^(\w+):([^(`]+)")
    return div [] [
        h1 [ClassName "title is-1"] [str title]
        table [ClassName "table"] [
          tbody [] [
            // for (name, summary) in members do
            //     let m = reg.Match(name)
            //     let category, name =
            //         if m.Success then
            //             let cat = if m.Groups.[1].Value = "T" then "Type" else "Method"
            //             cat, m.Groups.[2].Value
            //         else "Method", name
            //     let summary = summary.Replace("\n", " ")
            //     // printfn "%-10s%s\n%-10s%s\n" "Name:" name "Summary:" summary
            //     yield tr [] [
            //         td [] [str category]
            //         td [] [strong [] [str name]]
            //         td [] [str summary]
            //     ]
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
          "fontawesome" ==> Path.relative(targetFile, fontawesomePath)
          "styles" ==> Path.relative(targetFile, stylesPath)
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
              "fontawesome" ==> Path.relative(targetFile, fontawesomePath)
              "styles" ==> Path.relative(targetFile, stylesPath)
              "body" ==> parseReactStatic reactEl ]
            |> parseTemplate templatePath
            |> writeFile targetFile
    }
    |> Promise.catch (fun er -> printfn "ERROR: %s" er.Message)
    |> Promise.start

init()