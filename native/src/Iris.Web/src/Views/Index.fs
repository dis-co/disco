module Iris.Web.Views.Index

open System.IO
open FSharp.Html

let charset = meta <@> charset' "utf-8" 
let doctype = Literal("<!doctype html>")

let plugins basepath =
  Directory.GetFiles(basepath, "*.js")
  |> Array.toSeq
  |> Seq.map (Path.GetFileName)
  |> Seq.filter(fun item -> not (item = "iris.js"))
  |> Seq.map (fun f -> script <@> src' ("js/" + f))
  |> Seq.toList
  
let header pth =
  let std = 
    [ title <|> text "Iris"
    ; charset
    ; script <@> src' "dependencies/virtual-dom/dist/virtual-dom.js"
    ; script <@> src' "dependencies/rxjs/dist/rx.all.js"
    ; script <@> src' "dependencies/jquery/dist/jquery.js"
    ; script <@> src' "dependencies/routie/dist/routie.js"
    ; script <@> src' "dependencies/fabric.js/dist/fabric.js"
    ]
  head <||> List.append std (plugins pth)

let content =
  body <||>
    [ h1 <|> text "Hi."
    ; script <@> src' "js/iris.js"
    ]

let page pth =
  [ doctype;
    html
    <|> header pth
    <|> content
  ]
  
let compileIndex pth = List.fold (fun m e -> m + renderHtml e) "" <| page pth
