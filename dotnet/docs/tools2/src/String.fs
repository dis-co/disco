#if !INTERACTIVE
module Iris.DocGenerator.String
#endif

open System
open System.Text.RegularExpressions

let genArgsRegex = Regex @"``(\d)(\()?"
let argRegex = Regex @"([\w.]+)(\{.*?\})?"
let surroundingWhitespaceRegex = Regex @"^\s*(.*?)\s*$"

let konst k _ = k
let apply f x = f x

let trimWhitespace (str: string) =
    let m = surroundingWhitespaceRegex.Match(str)
    if m.Success then m.Groups.[1].Value else ""

let concatAndFormat format separator args =
    String.Format(format, String.concat separator args)

let matchToGroupList (m: Match) =
    m.Groups
    |> Seq.cast<Group>
    |> Seq.map (fun g ->
        if String.IsNullOrEmpty(g.Value)
        then None
        else Some g.Value)
    |> Seq.skip 1 // Skip whole match
    |> Seq.toList

let replace (reg: Regex) (evaluator: Match->(string option) list->string) (str: string) =
    reg.Replace(str, (fun (m: Match) ->
        let groups = matchToGroupList m
        evaluator m groups))

let substr (f: string->int*(int option)) (str: string) =
    match f str with
    | firstIndex, Some lastIndex -> str.Substring(firstIndex, lastIndex)
    | firstIndex, None -> str.Substring(firstIndex)

let replacements: Map<string, string seq->string> =
    ["Microsoft.FSharp.Core.FSharpFunc", concatAndFormat "{0}" "->"
     "Microsoft.FSharp.Core.FSharpOption", concatAndFormat "{0} option" ""
     "Microsoft.FSharp.Core.Unit", konst "()"
     "System.String", konst "string"
    ] |> Map

let rec processArgs (str: string) =
    // printfn "Process args for %s" str
    str |> replace argRegex (fun m -> function
        | [Some arg; genArgs] ->
            match Map.tryFind arg replacements with
            | Some f ->
                match genArgs with
                | Some genArgs -> (genArgs.Trim('{','}') |> processArgs).Split(',') |> f
                | None -> f []
            | None -> m.Value
        | _ -> failwith "unexpected")

let processName(name: string) =
    name
    |> replace genArgsRegex (fun m -> function
        | [Some g1; Some g2] ->
            List.init (int g1) (fun i -> "T" + (string i))
            |> String.concat ", "
            |> sprintf "<%s>("
        | [Some g1; None] -> "T" + g1
        | _ -> failwith "unexpected")
    |> processArgs

// processName "M:Iris.Web.Lib.op_AmpGreater``2(Microsoft.FSharp.Core.FSharpFunc{``0,Microsoft.FSharp.Core.Unit},``1,``0)"
