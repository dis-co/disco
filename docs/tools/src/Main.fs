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
    { Name: string
      Summary: string option }

type TypeInfo =
    { Summary: string option
      Members: MemberInfo list
      NestedTypes: Map<string, TypeInfo> }
    member this.IsEmpty =
        Option.isNone this.Summary
        && List.isEmpty this.Members
        && Map.isEmpty this.NestedTypes

let makeType summary members nestedTypes =
    { Summary = summary
      Members = members
      NestedTypes =nestedTypes }

let rec addType nameParts summary types: Map<string, TypeInfo> =
    match nameParts with
    | [] -> failwith "unexpected empty type name"
    | [part] ->
        let typ = makeType summary [] Map.empty
        Map.add part typ types
    | part::parts ->
        let parent =
            match Map.tryFind part types with
            | Some parent -> { parent with NestedTypes = addType parts summary parent.NestedTypes }
            | None -> addType parts summary Map.empty |> makeType None []
        Map.add part parent types

let rec addMembers nameParts members types: Map<string, TypeInfo> =
    match nameParts with
    | [] -> failwith "unexpected empty type name"
    | [part] ->
        let typ =
            match Map.tryFind part types with
            | Some typ -> { typ with Members = members }
            | None -> makeType None members Map.empty
        Map.add part typ types
    | part::parts ->
        let parent =
            match Map.tryFind part types with
            | Some parent -> { parent with NestedTypes = addMembers parts members parent.NestedTypes }
            | None -> addMembers parts members Map.empty |> makeType None []
        Map.add part parent types

let splitMemberName (memb: MemberInfo) =
    let name = memb.Name
    let parensIndex = name.IndexOf('(')
    let dotIndex = name.[..parensIndex-1].LastIndexOf('.')
    name.[..dotIndex-1], name.[dotIndex+1..]

let rec printTypes indent types =
    for (KeyValue(name, typ)) in types do
        printfn "%s%s >> %s" indent name (defaultArg typ.Summary "")
        for memb in typ.Members do
            let _, name = splitMemberName memb
            printfn "%s  %s: %s" indent name (defaultArg memb.Summary "")
        printTypes (indent + "  ") typ.NestedTypes

let tryAndTrim (k: string) (dic: IDictionary<string, string[]>) =
    match dic.TryGetValue(k) with
    | true, ar ->
        match Array.tryHead ar with
        | Some item -> String.trimWhitespace item |> Some
        | None -> None
    | false, _ -> None

let renderMembers (members: MemberInfo list) =
    match members with
    | [] -> opt None
    | members ->
        table [ClassName "table is-bordered is-striped is-fullwidth"] [
            tbody [] [
                for memb in members do
                    yield tr [] [
                        td [
                            Style [CSSProp.Width "50%"]
                        ] [strong [] [str memb.Name]]
                        td [] [str (defaultArg memb.Summary "")]
                    ]
            ]
        ] |> Some |> opt

let rec renderTypes depth parent (types: (string*TypeInfo) list) =
    let getTypeName parent name =
        match parent with Some p -> p + "." + name | None -> name
    match types with
    | [] -> []
    | [name, typ] when Option.isNone typ.Summary && List.isEmpty typ.Members ->
        let typeName = getTypeName parent name
        renderTypes depth (Some typeName) (Map.toList typ.NestedTypes) // Use same depth
    | types ->
        types
        |> Seq.filter (fun (_,t) -> not t.IsEmpty)
        |> Seq.map (fun (name, typ) ->
            let size = min 6 (depth + 2) |> string
            let subSize = min 6 (depth + 4) |> string
            let typeName = getTypeName parent name
            let nestedTypes = Map.toList typ.NestedTypes
            div [] [
                yield br []
                yield h1 [ClassName ("title is-" + size)] [str typeName]
                match typ.Summary with
                | Some sum -> yield h2 [ClassName ("subtitle is-" + subSize)] [str sum]
                | None -> ()
                yield renderMembers typ.Members
                yield! renderTypes (depth + 1) (Some typeName) nestedTypes
            ])
        |> Seq.toList

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
                    let memb = { Name = name.[2..]; Summary = summary; }
                    types, memb::members
                else
                    failwithf "Unknown member: %s" name
            | None -> types, members)
    let types =
        members
        |> Seq.groupBy (splitMemberName >> fst)
        |> Seq.fold (fun types (typName, members) ->
            let members = Seq.toList members
            let nameParts = typName.Split('.') |> Array.toList
            addMembers nameParts members types) types
    return div [] [
        yield h1 [ClassName "title is-1"] [str title]
        yield! renderTypes 0 None (Map.toList types)
    ]
}

let init() =
    // Copy img directory
    copy (resolve "${entryDir}/../../files/img") (resolve "${entryDir}/../public/img")

    // Markdown files
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

    // API reference
    promise {
        for title, xmlDocPath in xmlDocs do
            try
                let targetFile = Path.join(publicPath, title.ToLower(), "api_reference.html")
                let title = title + " API Reference"
                let! reactEl = parseApiReference title xmlDocPath
                [ "title" ==> title
                  "fontawesome" ==> Path.relative(targetFile, fontawesomePath)
                  "styles" ==> Path.relative(targetFile, stylesPath)
                  "body" ==> parseReactStatic reactEl ]
                |> parseTemplate templatePath
                |> writeFile targetFile
            with er ->
                printfn "Cannot parse %s: %s" xmlDocPath er.Message
    }
    |> Promise.catch (fun er -> printfn "ERROR: %s" er.Message)
    |> Promise.start

init()