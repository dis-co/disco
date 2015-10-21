[<ReflectedDefinition>]
module Iris.Web.Core.Html

#nowarn "1182"

open FunScript
open FunScript.TypeScript

(*
 _____                      
|_   _|   _ _ __   ___  ___ 
  | || | | | '_ \ / _ \/ __|
  | || |_| | |_) |  __/\__ \
  |_| \__, | .__/ \___||___/
      |___/|_|              
*)

type VProperties () = class end
type VTree () = class end
type VPatch () = class end

type Attribute =
  | Single of name : string
  | Pair   of name : string * value : string

type Html =
  | Parent of
    name     : string         *
    attrs    : Attribute array *
    children : Html array

  | Leaf of
    name  : string    *
    attrs : Attribute array

  | Literal of
    tag   : string 

  | Raw of VTree

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

(*__     ___      _               _ ____                  
  \ \   / (_)_ __| |_ _   _  __ _| |  _ \  ___  _ __ ___  
   \ \ / /| | '__| __| | | |/ _` | | | | |/ _ \| '_ ` _ \ 
    \ V / | | |  | |_| |_| | (_| | | |_| | (_) | | | | | |
     \_/  |_|_|   \__|\__,_|\__,_|_|____/ \___/|_| |_| |_|
*)

[<JSEmit("""return new virtualDom.VNode({0}, {1}, {2})""")>]
let mkVNode (tag : string) (p : VProperties) (c : VTree array) : VTree = failwith "never"

[<JSEmit("""return new virtualDom.VText({0})""")>]
let mkVText (txt : string) : VTree = failwith "never"

[<JSEmit("""return virtualDom.create({0})""")>]
let createElement (tree : VTree) : HTMLElement = failwith "never"

[<JSEmit("""return virtualDom.diff({0}, {1})""")>]
let diff (tree : VTree) (newtree : VTree) : VPatch = failwith "never"

[<JSEmit("""return virtualDom.patch({0}, {1})""")>]
let patch (root : HTMLElement) (patch : VPatch) : HTMLElement = failwith "never"

[<JSEmit("""return new Object()""")>]
let mkProperties () : VProperties = failwith "never"

[<JSEmit("""
         if({1} === "class")
           {0}.className = {2};
         else
           {0}[{1}] = {2};
         return {0};
         """)>]
let addAttr (o : VProperties) (n : string) (v : string) : VProperties =
  failwith "never"

(*
  ____                _     _             _                 
 / ___|___  _ __ ___ | |__ (_)_ __   __ _| |_ ___  _ __ ___ 
| |   / _ \| '_ ` _ \| '_ \| | '_ \ / _` | __/ _ \| '__/ __|
| |__| (_) | | | | | | |_) | | | | | (_| | || (_) | |  \__ \
 \____\___/|_| |_| |_|_.__/|_|_| |_|\__,_|\__\___/|_|  |___/

*)

// type Attribute =
//   | Single of name : string
//   | Pair   of name : string * value : string

let attrToProp (p : VProperties) (a : Attribute) : VProperties =
  match a with
    | Single(name) -> addAttr p name name 
    | Pair(name, value) -> addAttr p name value

let attrsToProp (attrs : Attribute array) : VProperties =
  Array.fold attrToProp (mkProperties ()) attrs

let rec renderHtml (el : Html) =
  match el with
    | Raw(vtree) -> vtree
    | Literal(t) -> mkVText t

    | Leaf(t, attrs) ->
      mkVNode t (attrsToProp attrs) Array.empty

    | Parent(t, attrs, ch) ->
      mkVNode t (attrsToProp attrs) (Array.map renderHtml ch)

// add an Attribute to an element
let (<@>) (el : Html) (attr : Attribute) =
  match el with
    | Parent(n,attrs,chldr) -> Parent(n, Array.append attrs [| attr |], chldr)
    | Leaf(n, attrs)        -> Leaf(n, Array.append attrs [| attr |])
    | item                  -> item

// add a child to an element (I guess its a Monoid!)
let (<|>) (el : Html) (chld : Html) =
  match el with
    | Parent(n, a, chldr) -> Parent(n, a, Array.append chldr [| chld |])
    | item                -> item 

// add a list of children to an element
let (<||>) (el : Html) (chldr : Html array) =
  match el with
    | Parent(n, a, chldr') -> Parent(n, a, Array.append chldr' chldr)
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

let a          = Parent("a",  Array.empty, Array.empty)

let abbr       = Parent("abbr",  Array.empty, Array.empty)

let address    = Parent("address",  Array.empty, Array.empty)

let area       = Parent("area",  Array.empty, Array.empty)

let article    = Parent("article",  Array.empty, Array.empty)

let aside      = Parent("aside",  Array.empty, Array.empty)

let audio      = Parent("audio",  Array.empty, Array.empty)

let b          = Parent("b",  Array.empty, Array.empty)

let basetag    = Parent("base",  Array.empty, Array.empty)

let bdi        = Parent("bdi",  Array.empty, Array.empty)

let bdo        = Parent("bdo",  Array.empty, Array.empty)

let blockquote = Parent("blockquote",  Array.empty, Array.empty)

let body       = Parent("body",  Array.empty, Array.empty)

let br         = Leaf("br", Array.empty)

let button     = Parent("button",  Array.empty, Array.empty)

let canvas     = Leaf("canvas", Array.empty)

let caption    = Parent("caption",  Array.empty, Array.empty)

let cite       = Parent("cite",  Array.empty, Array.empty)

let code       = Parent("code",  Array.empty, Array.empty)

let col        = Parent("col",  Array.empty, Array.empty)

let colgroup   = Parent("colgroup",  Array.empty, Array.empty)

let command    = Parent("command",  Array.empty, Array.empty)

let datalist   = Parent("datalist",  Array.empty, Array.empty)

let dd         = Parent("dd",  Array.empty, Array.empty)

let del        = Parent("del",  Array.empty, Array.empty)

let details    = Parent("details",  Array.empty, Array.empty)

let dfn        = Parent("dfn",  Array.empty, Array.empty)

let div        = Parent("div",  Array.empty, Array.empty)

let dl         = Parent("dl",  Array.empty, Array.empty)

let dt         = Parent("dt",  Array.empty, Array.empty)

let em         = Parent("em",  Array.empty, Array.empty)

let embed      = Parent("embed",  Array.empty, Array.empty)

let fieldset   = Parent("fieldset",  Array.empty, Array.empty)

let figcaption = Parent("figcaption",  Array.empty, Array.empty)

let figure     = Parent("figure",  Array.empty, Array.empty)

let footer     = Parent("footer",  Array.empty, Array.empty)

let form       = Parent("form",  Array.empty, Array.empty)

let h1         = Parent("h1",  Array.empty, Array.empty)

let h2         = Parent("h2",  Array.empty, Array.empty)

let h3         = Parent("h3",  Array.empty, Array.empty)

let h4         = Parent("h4",  Array.empty, Array.empty)

let h5         = Parent("h5",  Array.empty, Array.empty)

let h6         = Parent("h6",  Array.empty, Array.empty)

let head       = Parent("head",  Array.empty, Array.empty)

let header     = Parent("header",  Array.empty, Array.empty)

let hgroup     = Parent("hgroup",  Array.empty, Array.empty)

let hr         = Leaf("hr", Array.empty)

let html       = Parent("html",  Array.empty, Array.empty)

let i          = Parent("i",  Array.empty, Array.empty)

let iframe     = Parent("iframe",  Array.empty, Array.empty)

let img        = Leaf("img", Array.empty)

let input      = Parent("input",  Array.empty, Array.empty)

let ins        = Parent("ins",  Array.empty, Array.empty)

let kbd        = Parent("kbd",  Array.empty, Array.empty)

let keygen     = Parent("keygen",  Array.empty, Array.empty)

let label      = Parent("label",  Array.empty, Array.empty)

let legend     = Parent("legend",  Array.empty, Array.empty)

let li         = Parent("li",  Array.empty, Array.empty)

let link       = Leaf("link", Array.empty)

let map        = Parent("map",  Array.empty, Array.empty)

let mark       = Parent("mark",  Array.empty, Array.empty)

let menu       = Parent("menu",  Array.empty, Array.empty)

let meta       = Leaf("meta", Array.empty)

let meter      = Parent("meter",  Array.empty, Array.empty)

let nav        = Parent("nav",  Array.empty, Array.empty)

let noscript   = Parent("noscript",  Array.empty, Array.empty)

let objectTag  = Parent("object",  Array.empty, Array.empty)

let ol         = Parent("ol",  Array.empty, Array.empty)

let optgroup   = Parent("optgroup",  Array.empty, Array.empty)

let option     = Parent("option",  Array.empty, Array.empty)

let output     = Parent("output",  Array.empty, Array.empty)

let p          = Parent("p",  Array.empty, Array.empty)

let param      = Parent("param",  Array.empty, Array.empty)

let pre        = Parent("pre",  Array.empty, Array.empty)

let progress   = Parent("progress",  Array.empty, Array.empty)

let q          = Parent("q",  Array.empty, Array.empty)

let rp         = Parent("rp",  Array.empty, Array.empty)

let rt         = Parent("rt",  Array.empty, Array.empty)

let samp       = Parent("samp",  Array.empty, Array.empty)

let script     = Parent("script",  Array.empty, Array.empty)

let section    = Parent("section",  Array.empty, Array.empty)

let selectTag  = Parent("select",  Array.empty, Array.empty)

let small      = Parent("small",  Array.empty, Array.empty)

let source     = Parent("source",  Array.empty, Array.empty)

let span       = Parent("span",  Array.empty, Array.empty)

let strong     = Parent("strong",  Array.empty, Array.empty)

let style      = Parent("style",  Array.empty, Array.empty)

let sub        = Parent("sub",  Array.empty, Array.empty)

let summary    = Parent("summary",  Array.empty, Array.empty)

let sup        = Parent("sup",  Array.empty, Array.empty)

let table      = Parent("table",  Array.empty, Array.empty)

let tbody      = Parent("tbody",  Array.empty, Array.empty)

let td         = Parent("td",  Array.empty, Array.empty)

let textarea   = Parent("textarea",  Array.empty, Array.empty)

let tfoot      = Parent("tfoot",  Array.empty, Array.empty)

let th         = Parent("th",  Array.empty, Array.empty)

let thead      = Parent("thead",  Array.empty, Array.empty)

let time       = Parent("time",  Array.empty, Array.empty)

let title      = Parent("title",  Array.empty, Array.empty)

let tr         = Parent("tr",  Array.empty, Array.empty)

let track      = Parent("track",  Array.empty, Array.empty)

let ul         = Parent("ul",  Array.empty, Array.empty)

let var        = Parent("var",  Array.empty, Array.empty)

let video      = Parent("video",  Array.empty, Array.empty)

let wbr        = Parent("wbr",  Array.empty, Array.empty)
