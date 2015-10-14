[<ReflectedDefinition>]
module FSharp.Html

(*
 _____                      
|_   _|   _ _ __   ___  ___ 
  | || | | | '_ \ / _ \/ __|
  | || |_| | |_) |  __/\__ \
  |_| \__, | .__/ \___||___/
      |___/|_|              
*)


type Attribute =
  | Single of name : string
  | Pair   of name : string * value : string

type Html =
  | Parent of
    name     : string         *
    attrs    : Attribute list *
    children : Html      list

  | Leaf of
    name  : string    *
    attrs : Attribute list

  | Literal of
    tag   : string 

type Alignment = Left | Center | Right

type Draggable = True | False | Auto

type InputType =
  | Color
  | Date
  | DateTimeLocal
  | DateTime
  | EMail
  | Month
  | Number
  | Range
  | Search
  | Tel
  | Time
  | URL
  | Week
  | Text
  | Password
  | Submit
  | Radio
  | Checkbox
  | Button
  | Reset
  
type Shape =
  | ShpDefault
  | ShpRect
  | ShpCircle
  | ShpPoly

type Preload =
  | PreloadAuto
  | PreloadMetadata
  | PreloadNone

type Target =
  | TargetBlank
  | TargetParent
  | TargetSelf
  | TargetTop
  | TargetFrameName of string
  
type Dir = LTR | RTL

(*
  ____                _     _             _                 
 / ___|___  _ __ ___ | |__ (_)_ __   __ _| |_ ___  _ __ ___ 
| |   / _ \| '_ ` _ \| '_ \| | '_ \ / _` | __/ _ \| '__/ __|
| |__| (_) | | | | | | |_) | | | | | (_| | || (_) | |  \__ \
 \____\___/|_| |_| |_|_.__/|_|_| |_|\__,_|\__\___/|_|  |___/

*)

let renderAttr (attr : Attribute)=
  match attr with
    | Single(el) -> el
    | Pair(n,v)  -> n + "=\"" + v + "\""

let rec renderHtml (el : Html) =
  match el with
    | Literal(t)              -> t
    | Leaf(t, attrs)          ->
      let attributes = List.map renderAttr attrs |>
                       List.fold (fun m it -> m + " " + it ) ""
      in "<" + t + attributes + ">"
    | Parent(t, attrs, chldr) ->
      let children = List.map renderHtml chldr |>
                     List.fold (fun m it -> m + it) ""
      let attributes = List.map renderAttr attrs |>
                       List.fold (fun m it -> m + " " + it ) ""
      in "<" + t + attributes + ">" + children + "</" + t + ">"

// add an Attribute to an element
let (<@>) (el : Html) (attr : Attribute) =
  match el with
    | Parent(n,attrs,chldr) -> Parent(n, List.append attrs [attr], chldr)
    | Leaf(n, attrs)        -> Leaf(n, List.append attrs [attr])
    | item                  -> item

// add a child to an element (I guess its a Monoid!)
let (<|>) (el : Html) (chld : Html) =
  match el with
    | Parent(n, a, chldr) -> Parent(n, a, List.append chldr [chld])
    | item                -> item 

// add a list of children to an element
let (<||>) (el : Html) (chldr : Html list) =
  match el with
    | Parent(n, a, chldr') -> Parent(n, a, List.append chldr' chldr)
    | item                 -> item 

let class' n = Pair("class", n)

let data' d n = Pair("data-" + d, n)

let id' n = Pair("id", n)

let style' n = Pair("style", n)

let accesskey' n = Pair("accesskey", n)

let align' a =
  let a' =
    match a with
      | Left   -> "left"
      | Center -> "center"
      | Right  -> "right"
  in Pair("align", a')

let background' url = Pair("background", url)

let bgcolor' clr = Pair("bgcolor", clr)

let contenteditable' bl =
  let val' = if bl then "true" else "false"
  in Pair("contenteditable", val')

let contextmenu' id = Pair("contextmenu", id)

let draggable' drgbl =
  let val' = match drgbl with
              | True  -> "true"
              | False -> "false"
              | Auto  -> "auto"
  in Pair("draggable", val')

let height' i = Pair("height", i)

let hidden' = Pair("hidden", "hidden")

let spellcheck' bl =
  Pair("spellcheck", if bl then "true" else "flase")

let title' n = Pair("title", n)

let width' i = Pair("width", i)

let name' n  = Pair("name", n)

let type' t =
  let t' =
    match t with
      | Color         -> "color"
      | Date          -> "date"
      | DateTimeLocal -> "datetime-local"
      | DateTime      -> "datetime"
      | EMail         -> "email"
      | Month         -> "month"
      | Number        -> "number"
      | Range         -> "range"
      | Search        -> "search"
      | Tel           -> "tel"
      | Time          -> "time"
      | URL           -> "url"
      | Week          -> "week"
      | Text          -> "text"
      | Password      -> "password"
      | Submit        -> "submit"
      | Radio         -> "radio"
      | Checkbox      -> "checkbox"
      | Button        -> "button"
      | Reset         -> "reset"
  in Pair("type", t')

let href' url = Pair("href", url)

let rel' str = Pair("rel", str)

let charset' str = Pair("charset", str)

let src' url = Pair("src", url) 

let alt' str = Pair("alt", str)

let usemap' map = Pair("usemap", map)

let shape' shp =
  let shp' = 
    match shp with
      | ShpDefault -> "default" 
      | ShpRect    -> "rect"
      | ShpCircle  -> "circle"
      | ShpPoly    -> "poly"
  in Pair("shape", shp')
 
let coords' cs = Pair("coords", cs)

let download' str = Pair("download", str)
  
let autoplay' = Pair("autoplay", "autoplay")

let controls' = Pair("controls", "controls")

let loop' = Pair("loop", "loop")

let muted' = Pair("muted", "muted")

let preload' pl =
  let pl' =
    match pl with
      | PreloadAuto     -> "auto"
      | PreloadMetadata -> "metadata"
      | PreloadNone     -> "none"
  in Pair("preload", pl')

let target' tg =
  let tg' =
    match tg with
      | TargetBlank        -> "_blank"
      | TargetParent       -> "_parent"
      | TargetSelf         -> "_self"
      | TargetTop          -> "_top"
      | TargetFrameName(s) -> s
  in Pair("target", tg')
  
let dir' dr =
  let dr' =
    match dr with
      | LTR -> "ltr"
      | RTL -> "rtl"
  in Pair("dir", dr')

let cite' url = Pair("cite", url)

let value' txt = Pair("value", txt)

(*
 _   _ _____ __  __ _     
| | | |_   _|  \/  | |    
| |_| | | | | |\/| | |    
|  _  | | | | |  | | |___ 
|_| |_| |_| |_|  |_|_____|

*)

let text     t = Literal(t)

let doctype    = Literal("<!DOCTYPE html>")

let a          = Parent("a", [], [])

let abbr       = Parent("abbr", [], [])

let address    = Parent("address", [], [])

let area       = Parent("area", [], [])

let article    = Parent("article", [], [])

let aside      = Parent("aside", [], [])

let audio      = Parent("audio", [], [])

let b          = Parent("b", [], [])

let basetag    = Parent("base", [], [])

let bdi        = Parent("bdi", [], [])

let bdo        = Parent("bdo", [], [])

let blockquote = Parent("blockquote", [], [])

let body       = Parent("body", [], [])

let br         = Leaf("br", [])

let button     = Parent("button", [], [])

let canvas     = Leaf("canvas", [])

let caption    = Parent("caption", [], [])

let cite       = Parent("cite", [], [])

let code       = Parent("code", [], [])

let col        = Parent("col", [], [])

let colgroup   = Parent("colgroup", [], [])

let command    = Parent("command", [], [])

let datalist   = Parent("datalist", [], [])

let dd         = Parent("dd", [], [])

let del        = Parent("del", [], [])

let details    = Parent("details", [], [])

let dfn        = Parent("dfn", [], [])

let div        = Parent("div", [], [])

let dl         = Parent("dl", [], [])

let dt         = Parent("dt", [], [])

let em         = Parent("em", [], [])

let embed      = Parent("embed", [], [])

let fieldset   = Parent("fieldset", [], [])

let figcaption = Parent("figcaption", [], [])

let figure     = Parent("figure", [], [])

let footer     = Parent("footer", [], [])

let form       = Parent("form", [], [])

let h1         = Parent("h1", [], [])

let h2         = Parent("h2", [], [])

let h3         = Parent("h3", [], [])

let h4         = Parent("h4", [], [])

let h5         = Parent("h5", [], [])

let h6         = Parent("h6", [], [])

let head       = Parent("head", [], [])

let header     = Parent("header", [], [])

let hgroup     = Parent("hgroup", [], [])

let hr         = Leaf("hr", [])

let html       = Parent("html", [], [])

let i          = Parent("i", [], [])

let iframe     = Parent("iframe", [], [])

let img        = Leaf("img", [])

let input      = Parent("input", [], [])

let ins        = Parent("ins", [], [])

let kbd        = Parent("kbd", [], [])

let keygen     = Parent("keygen", [], [])

let label      = Parent("label", [], [])

let legend     = Parent("legend", [], [])

let li         = Parent("li", [], [])

let link       = Leaf("link", [])

let map        = Parent("map", [], [])

let mark       = Parent("mark", [], [])

let menu       = Parent("menu", [], [])

let meta       = Leaf("meta", [])

let meter      = Parent("meter", [], [])

let nav        = Parent("nav", [], [])

let noscript   = Parent("noscript", [], [])

let objectTag  = Parent("object", [], [])

let ol         = Parent("ol", [], [])

let optgroup   = Parent("optgroup", [], [])

let option     = Parent("option", [], [])

let output     = Parent("output", [], [])

let p          = Parent("p", [], [])

let param      = Parent("param", [], [])

let pre        = Parent("pre", [], [])

let progress   = Parent("progress", [], [])

let q          = Parent("q", [], [])

let rp         = Parent("rp", [], [])

let rt         = Parent("rt", [], [])

let samp       = Parent("samp", [], [])

let script     = Parent("script", [], [])

let section    = Parent("section", [], [])

let selectTag  = Parent("select", [], [])

let small      = Parent("small", [], [])

let source     = Parent("source", [], [])

let span       = Parent("span", [], [])

let strong     = Parent("strong", [], [])

let style      = Parent("style", [], [])

let sub        = Parent("sub", [], [])

let summary    = Parent("summary", [], [])

let sup        = Parent("sup", [], [])

let table      = Parent("table", [], [])

let tbody      = Parent("tbody", [], [])

let td         = Parent("td", [], [])

let textarea   = Parent("textarea", [], [])

let tfoot      = Parent("tfoot", [], [])

let th         = Parent("th", [], [])

let thead      = Parent("thead", [], [])

let time       = Parent("time", [], [])

let title      = Parent("title", [], [])

let tr         = Parent("tr", [], [])

let track      = Parent("track", [], [])

let ul         = Parent("ul", [], [])

let var        = Parent("var", [], [])

let video      = Parent("video", [], [])

let wbr        = Parent("wbr", [], [])
