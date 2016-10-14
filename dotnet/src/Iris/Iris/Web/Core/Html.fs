namespace Iris.Web.Core

open Fable.Core
open Fable.Import
open Fable.Import.Browser

[<Import("*","dom-delegator")>]
module DomDelegator =

  type Delegator =
    [<Emit("$0.addEventListener($1,$2,$3)")>]
    abstract AddEventListener: HTMLElement -> string -> (Event -> unit) -> unit

  type DelegatorConstructor =
    abstract prototype: Delegator with get, set

    [<Emit("""require("dom-delegator")()""")>]
    abstract Create: unit -> Delegator

  let Delegator: DelegatorConstructor = failwith "JS only"

module Html =

  //  _____
  // |_   _|   _ _ __   ___  ___
  //   | || | | | '_ \ / _ \/ __|
  //   | || |_| | |_) |  __/\__ \
  //   |_| \__, | .__/ \___||___/
  //       |___/|_|

  [<StringEnum>]
  type InputType =
    | [<CompiledName "color">]          Color
    | [<CompiledName "date">]           Date
    | [<CompiledName "datetime-local">] DateTimeLocal
    | [<CompiledName "datetime">]       DateTime
    | [<CompiledName "email">]          EMail
    | [<CompiledName "month">]          Month
    | [<CompiledName "number">]         Number
    | [<CompiledName "range">]          Range
    | [<CompiledName "search">]         Search
    | [<CompiledName "tel">]            Tel
    | [<CompiledName "time">]           Time
    | [<CompiledName "url">]            URL
    | [<CompiledName "week">]           Week
    | [<CompiledName "text">]           Text
    | [<CompiledName "password">]       Password
    | [<CompiledName "submit">]         Submit
    | [<CompiledName "radio">]          Radio
    | [<CompiledName "checkbox">]       Checkbox
    | [<CompiledName "button">]         Button
    | [<CompiledName "reset">]          Reset

  [<KeyValueList>]
  type CSSProperty =
    | TextAlign of string
    | Margin    of string

  [<KeyValueList;NoComparison;NoEquality>]
  type ElementProperty =
    | [<CompiledName("id")>]        ElmId   of string
    | [<CompiledName("href")>]      Href    of string
    | [<CompiledName("className")>] Class   of string
    | [<CompiledName("type")>]      Type    of InputType
    | [<CompiledName("style")>]     Style   of CSSProperty list
    | [<CompiledName("onclick")>]   OnClick of (MouseEvent -> unit)
    | [<CompiledName("ev-click")>]  OnPlay  of (MouseEvent -> unit)

  type Properties = ElementProperty list

  [<Emit("Object.assign({}, $0, $1)")>]
  let (++) (a:'a list) (b:'a list) : 'a list = failwithf "(++): JS Only %A %A" a b

  type VTree = class end

  type VPatch = class end

  [<NoComparison;NoEquality>]
  type Html =
    | Parent  of name: string * attrs: Properties * children: Html array
    | Leaf    of name: string * attrs: Properties
    | Literal of tag: string
    | Raw     of VTree

  type Alignment = Left | Center | Right

  type Draggable = True | False | Auto

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

  let (+@) (elm: Html) (prop: Properties) =
    match elm with
      | Parent(t,p,children) -> Parent(t,p ++ prop,children)
      | Leaf(t,p) -> Leaf(t,p ++ prop)
      | _ -> elm

  let (<+) (parent: Html) (newkids: Html) =
    match parent with
      | Parent(t,p,children) -> Parent(t,p,Array.append children [| newkids |])
      | _ -> parent

  let (<++) (parent: Html) (newkids: Html array) =
    match parent with
      | Parent(t,p,children) -> Parent(t,p,Array.append children newkids)
      | _ -> parent

  let (+>) (parent: Html) (kid: Html) =
    match parent with
      | Parent(t,p,children) -> Parent(t,p, Array.append [| kid |] children)
      | _ -> parent

  let (++>) (parent: Html) (newkids: Html array) =
    match parent with
      | Parent(t,p,children) -> Parent(t,p, Array.append newkids children)
      | _ -> parent

  (*__     ___      _               _ ____
    \ \   / (_)_ __| |_ _   _  __ _| |  _ \  ___  _ __ ___
     \ \ / /| | '__| __| | | |/ _` | | | | |/ _ \| '_ ` _ \
      \ V / | | |  | |_| |_| | (_| | | |_| | (_) | | | | | |
       \_/  |_|_|   \__|\__,_|\__,_|_|____/ \___/|_| |_| |_|
  *)

  [<Emit "virtualDom.create($0)">]
  let createElement (_: VTree) : HTMLElement = failwith "JS Only"

  [<Emit "virtualDom.diff($0,$1)">]
  let diff (_: VTree) (_: VTree) : VPatch = failwith "JS Only"

  [<Emit "virtualDom.patch($0, $1)">]
  let patch (_: HTMLElement) (_: VPatch) : HTMLElement = failwith "JS Only"

  [<Emit("new virtualDom.h($0,$1,$2)")>]
  let VNode (_: string) (_: Properties) (_: VTree array) : VTree = failwith "ONLY IN JS"

  [<Emit "new virtualDom.VText($0)">]
  let VText (_: string) : VTree = failwith "ONLY IN JS"

  // `this` makes me feel uneasy
  [<Emit "$0.apply({},arguments)">]
  let withArgs (_: 'a -> unit) = failwith "JS Only"

  (*
    ____                _     _             _
   / ___|___  _ __ ___ | |__ (_)_ __   __ _| |_ ___  _ __ ___
  | |   / _ \| '_ ` _ \| '_ \| | '_ \ / _` | __/ _ \| '__/ __|
  | |__| (_) | | | | | | |_) | | | | | (_| | || (_) | |  \__ \
   \____\___/|_| |_| |_|_.__/|_|_| |_|\__,_|\__\___/|_|  |___/

  *)

  let rec renderHtml (el : Html) =
    match el with
      | Raw(vtree) -> vtree
      | Literal(t) -> VText t

      | Leaf(t, attrs) ->
        VNode t attrs [| |]

      | Parent(t, attrs, ch) ->
        VNode t attrs (Array.map renderHtml ch)

  // // add an Attribute to an element
  // let (<@>) (el : Html) (attrs : Properties) =
  //   match el with
  //     | Parent(n,attributes,chldr) -> Parent(n, attrs ++ attributes, chldr)
  //     | Leaf(n, attributes)        -> Leaf(n, attrs ++ attributes)
  //     | item                       -> item

  // // add a child to an element (I guess its a Monoid!)
  // let (<|>) (el : Html) (chld : Html) =
  //   match el with
  //     | Parent(n, a, chldr) -> Parent(n, a, List.append chldr [ chld ])
  //     | item                -> item

  // // add a list of children to an element
  // let (<||>) (el : Html) (chldr : Html list) =
  //   match el with
  //     | Parent(n, a, chldr') -> Parent(n, a, List.append chldr' chldr)
  //     | item                 -> item

  (*
  let _klass n = ClassName n

  let _id n = Id n

  let _style n = Pair("style", StrVal(n))

  let _accesskey n = Pair("accesskey", StrVal(n))

  let _align a =
    let a' =
      match a with
        | Left   -> "left"
        | Center -> "center"
        | Right  -> "right"
    in Pair("align", StrVal(a'))

  let _background url = Pair("background", url)

  let _bgcolor clr = Pair("bgcolor", clr)

  let _contenteditable bl =
    let val' = if bl then "true" else "false"
    in Pair("contenteditable", StrVal(val'))

  let _contextmenu id = Pair("contextmenu", id)

  let _draggable drgbl =
    let val' = match drgbl with
                | True  -> "true"
                | False -> "false"
                | Auto  -> "auto"
    in Pair("draggable", StrVal(val'))

  let _height i = Pair("height", i)

  let _hidden = Pair("hidden", StrVal("hidden"))

  let _spellcheck bl =
    Pair("spellcheck", if bl then StrVal("true") else StrVal("false"))

  let _title n = Pair("title", n)

  let _width i = Pair("width", i)

  let _name n  = Pair("name", n)

  let _type t =
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
    in Pair("type", StrVal(t'))

  let _href url = Pair("href", url)

  let _rel str = Pair("rel", str)

  let _charset str = Pair("charset", str)

  let _src url = Pair("src", url)

  let _alt str = Pair("alt", str)

  let _usemap map = Pair("usemap", map)

  let _shape shp =
    let shp' =
      match shp with
        | ShpDefault -> "default"
        | ShpRect    -> "rect"
        | ShpCircle  -> "circle"
        | ShpPoly    -> "poly"
    in Pair("shape", StrVal(shp'))

  let _coords cs = Pair("coords", cs)

  let _download str = Pair("download", str)

  let _autoplay = Pair("autoplay", StrVal("autoplay"))

  let _controls = Pair("controls", StrVal("controls"))

  let _loop = Pair("loop", StrVal("loop"))

  let _muted = Pair("muted", StrVal("muted"))

  let _preload pl =
    let pl' =
      match pl with
        | PreloadAuto     -> "auto"
        | PreloadMetadata -> "metadata"
        | PreloadNone     -> "none"
    in Pair("preload", StrVal(pl'))

  let _target tg =
    let tg' =
      match tg with
        | TargetBlank        -> "_blank"
        | TargetParent       -> "_parent"
        | TargetSelf         -> "_self"
        | TargetTop          -> "_top"
        | TargetFrameName(s) -> s
    in Pair("target", StrVal(tg'))

  let _dir dr =
    let dr' =
      match dr with
        | LTR -> "ltr"
        | RTL -> "rtl"
    in Pair("dir", StrVal(dr'))

  let _cite url = Pair("cite", url)

  let _value txt = Pair("value", txt)

  let onClick (cb : MouseEvent -> unit) =
    Pair("onclick", EvVal(fun () -> withArgs cb))

  let onAbort (cb : Event -> unit) =
    Pair("onabort", EvVal(fun () -> withArgs cb))

  let onBlur (cb : Event -> unit) =
    Pair("onblur", EvVal(fun () -> withArgs cb))

  let onChange (cb : Event -> unit) =
    Pair("onchange", EvVal(fun () -> withArgs cb))

  let onClose (cb : Event -> unit) =
    Pair("onclose", EvVal(fun () -> withArgs cb))

  let onContextMenu (cb : Event -> unit) =
    Pair("oncontextmenu", EvVal(fun () -> withArgs cb))

  let onDblClick (cb : Event -> unit) =
    Pair("ondblclick", EvVal(fun () -> withArgs cb))

  let onError (cb : Event -> unit) =
    Pair("onerror", EvVal(fun () -> withArgs cb))

  let onFocus (cb : Event -> unit) =
    Pair("onfocus", EvVal(fun () -> withArgs cb))

  let onInput (cb : Event -> unit) =
    Pair("oninput", EvVal(fun () -> withArgs cb))

  let onKeyDown (cb : Event -> unit) =
    Pair("onkeydown", EvVal(fun () -> withArgs cb))

  let onKeyPress (cb : Event -> unit) =
    Pair("onkeypress", EvVal(fun () -> withArgs cb))

  let onKeyUp (cb : Event -> unit) =
    Pair("onkeyup", EvVal(fun () -> withArgs cb))

  let onLoad (cb : Event -> unit) =
    Pair("onload", EvVal(fun () -> withArgs cb))

  let onMouseDown (cb : Event -> unit) =
    Pair("onmousedown", EvVal(fun () -> withArgs cb))

  let onMouseMove (cb : Event -> unit) =
    Pair("onmousemove", EvVal(fun () -> withArgs cb))

  let onMouseOut (cb : Event -> unit) =
    Pair("onmouseout", EvVal(fun () -> withArgs cb))

  let onMouseOver (cb : Event -> unit) =
    Pair("onmouseover", EvVal(fun () -> withArgs cb))

  let onMouseUp (cb : Event -> unit) =
    Pair("onmouseup", EvVal(fun () -> withArgs cb))

  let onPointerCancel (cb : Event -> unit) =
    Pair("onpointercancel", EvVal(fun () -> withArgs cb))

  let onPointerDown (cb : Event -> unit) =
    Pair("onpointerdown", EvVal(fun () -> withArgs cb))

  let onPointerEnter (cb : Event -> unit) =
    Pair("onpointerenter", EvVal(fun () -> withArgs cb))

  let onPointerLeave (cb : Event -> unit) =
    Pair("onpointerleave", EvVal(fun () -> withArgs cb))

  let onPointerMove (cb : Event -> unit) =
    Pair("onpointermove", EvVal(fun () -> withArgs cb))

  let onPointerOut (cb : Event -> unit) =
    Pair("onpointerout", EvVal(fun () -> withArgs cb))

  let onPointerOver (cb : Event -> unit) =
    Pair("onpointerover", EvVal(fun () -> withArgs cb))

  let onPointerUp (cb : Event -> unit) =
    Pair("onpointerup", EvVal(fun () -> withArgs cb))

  let onReset (cb : Event -> unit) =
    Pair("onreset", EvVal(fun () -> withArgs cb))

  let onResize (cb : Event -> unit) =
    Pair("onresize", EvVal(fun () -> withArgs cb))

  let onScroll (cb : Event -> unit) =
    Pair("onscroll", EvVal(fun () -> withArgs cb))

  let onSelect (cb : Event -> unit) =
    Pair("onselect", EvVal(fun () -> withArgs cb))

  let onSelectStart (cb : Event -> unit) =
    Pair("onselectstart", EvVal(fun () -> withArgs cb))

  let onSubmit (cb : Event -> unit) =
    Pair("onsubmit", EvVal(fun () -> withArgs cb))

  let onTouchCancel (cb : Event -> unit) =
    Pair("ontouchcancel", EvVal(fun () -> withArgs cb))

  let onTouchMove (cb : Event -> unit) =
    Pair("ontouchmove", EvVal(fun () -> withArgs cb))

  let onTouchStart (cb : Event -> unit) =
    Pair("ontouchstart", EvVal(fun () -> withArgs cb))

  *)

  (*
   _   _ _____ __  __ _
  | | | |_   _|  \/  | |
  | |_| | | | | |\/| | |
  |  _  | | | | |  | | |___
  |_| |_| |_| |_|  |_|_____|

  *)

  let Text            text = Literal(text)

  let DocType              = Literal("<!DOCTYPE html>")

  let A          props chd = Parent("a", props, chd)

  let Abbr       props chd = Parent("abbr", props, chd)

  let Address    props chd = Parent("address", props, chd)

  let Area       props chd = Parent("area", props, chd)

  let Article    props chd = Parent("article", props, chd)

  let Aside      props chd = Parent("aside", props, chd)

  let Audio      props chd = Parent("audio", props, chd)

  let B          props chd = Parent("b", props, chd)

  let BaseTag    props chd = Parent("base", props, chd)

  let Bdi        props chd = Parent("bdi", props, chd)

  let Bdo        props chd = Parent("bdo", props, chd)

  let BlockQuote props chd = Parent("blockquote", props, chd)

  let Body       props chd = Parent("body", props, chd)

  let Br         props     = Leaf("br", props)

  let Button     props chd = Parent("button", props, chd)

  let Canvas     props     = Leaf("canvas", props)

  let Caption    props chd = Parent("caption", props, chd)

  let Cite       props chd = Parent("cite", props, chd)

  let Code       props chd = Parent("code", props, chd)

  let Col        props chd = Parent("col", props, chd)

  let Colgroup   props chd = Parent("colgroup", props, chd)

  let Command    props chd = Parent("command", props, chd)

  let Datalist   props chd = Parent("datalist", props, chd)

  let Dd         props chd = Parent("dd", props, chd)

  let Del        props chd = Parent("del", props, chd)

  let Details    props chd = Parent("details", props, chd)

  let Dfn        props chd = Parent("dfn", props, chd)

  let Div        props chd = Parent("div", props, chd)

  let Dl         props chd = Parent("dl", props, chd)

  let Dt         props chd = Parent("dt", props, chd)

  let Em         props chd = Parent("em", props, chd)

  let Embed      props chd = Parent("embed", props, chd)

  let Fieldset   props chd = Parent("fieldset", props, chd)

  let Figcaption props chd = Parent("figcaption", props, chd)

  let Figure     props chd  = Parent("figure", props, chd)

  let Footer     props chd  = Parent("footer", props, chd)

  let Form       props chd  = Parent("form", props, chd)

  let H1         props chd  = Parent("h1", props, chd)

  let H2         props chd  = Parent("h2", props, chd)

  let H3         props chd  = Parent("h3", props, chd)

  let H4         props chd  = Parent("h4", props, chd)

  let H5         props chd  = Parent("h5", props, chd)

  let H6         props chd  = Parent("h6", props, chd)

  let Head       props chd  = Parent("head", props, chd)

  let Header     props chd  = Parent("header", props, chd)

  let Hgroup     props chd  = Parent("hgroup", props, chd)

  let Hr         props      = Leaf("hr", props)

  let HtmlElm    props chd  = Parent("html", props, chd)

  let I          props chd  = Parent("i", props, chd)

  let Iframe     props chd  = Parent("iframe", props, chd)

  let Img        props      = Leaf("img", props)

  let Input      props chd  = Parent("input", props, chd)

  let Ins        props chd  = Parent("ins", props, chd)

  let Kbd        props chd  = Parent("kbd", props, chd)

  let Keygen     props chd  = Parent("keygen", props, chd)

  let Label      props chd  = Parent("label", props, chd)

  let Legend     props chd  = Parent("legend", props, chd)

  let Li         props chd  = Parent("li", props, chd)

  let Link       props      = Leaf("link", props)

  let ImgMap      props chd = Parent("map", props, chd)

  let Mark       props chd  = Parent("mark", props, chd)

  let Menu       props chd  = Parent("menu", props, chd)

  let Meta       props      = Leaf("meta", props)

  let Meter      props chd  = Parent("meter", props, chd)

  let Nav        props chd  = Parent("nav", props, chd)

  let Noscript   props chd  = Parent("noscript", props, chd)

  let ObjectTag  props chd  = Parent("object", props, chd)

  let Ol         props chd  = Parent("ol", props, chd)

  let Optgroup   props chd  = Parent("optgroup", props, chd)

  let OptionElm  props chd  = Parent("option", props, chd)

  let Output     props chd  = Parent("output", props, chd)

  let P          props chd  = Parent("p", props, chd)

  let Param      props chd  = Parent("param", props, chd)

  let Pre        props chd  = Parent("pre", props, chd)

  let Progress   props chd  = Parent("progress", props, chd)

  let Q          props chd  = Parent("q", props, chd)

  let Rp         props chd  = Parent("rp", props, chd)

  let Rt         props chd  = Parent("rt", props, chd)

  let Samp       props chd  = Parent("samp", props, chd)

  let Script     props chd  = Parent("script", props, chd)

  let Section    props chd  = Parent("section", props, chd)

  let SelectTag  props chd  = Parent("select", props, chd)

  let Small      props chd  = Parent("small", props, chd)

  let Source     props chd  = Parent("source", props, chd)

  let Span       props chd  = Parent("span", props, chd)

  let Strong     props chd  = Parent("strong", props, chd)

  let StyleElm   props chd  = Parent("style", props, chd)

  let Sub        props chd  = Parent("sub", props, chd)

  let Summary    props chd  = Parent("summary", props, chd)

  let Sup        props chd  = Parent("sup", props, chd)

  let Table      props chd  = Parent("table", props, chd)

  let Tbody      props chd  = Parent("tbody", props, chd)

  let Td         props chd  = Parent("td", props, chd)

  let Textarea   props chd  = Parent("textarea", props, chd)

  let Tfoot      props chd  = Parent("tfoot", props, chd)

  let Th         props chd  = Parent("th", props, chd)

  let Thead      props chd  = Parent("thead", props, chd)

  let Time       props chd  = Parent("time", props, chd)

  let Title      props chd  = Parent("title", props, chd)

  let Tr         props chd  = Parent("tr", props, chd)

  let Track      props chd  = Parent("track", props, chd)

  let Ul         props chd  = Parent("ul", props, chd)

  let Var        props chd  = Parent("var", props, chd)

  let Video      props chd  = Parent("video", props, chd)

  let Wbr        props chd  = Parent("wbr", props, chd)

  let IrisCue    props chd  = Parent("iris-cue", props, chd)

  let UserWidget props chd  = Parent("iris-user", props, chd)

  let SessionWidget props chd  = Parent("iris-user", props, chd)
