[<FunScript.JS>]
module Iris.Web.DOM

open FunScript
open FunScript.TypeScript
open FunScript.TypeScript.virtualDom

let hello () =
  let tree = virtualDom.Globals.h("div#hello", Array.empty)
  virtualDom.Globals.create tree


(*
    ____             _ 
   / ___| ___   __ _| |
  | |  _ / _ \ / _` | |
  | |_| | (_) | (_| | |
   \____|\___/ \__,_|_|~~:w

   we want a flexible, typed, set of combinators that will eventually be
   translated straight into HTML strings or trees of VNodes for DOM patching
   with `virtual-dom`.
*)

type Html =
  | Parent of
    name     : string *
    openTag  : string *
    closeTag : string *
    children : Html list

  | Leaf of
    name : string *
    tag  : string

type Attribute =
  | SingleAttr of name : string
  | PairedAttr of name : string * value : string


let appendAttr tag attr =
  match attr with
    | SingleAttr(a)   -> tag + " " + a
    | PairedAttr(a,v) -> tag + " " + a + "=" + v


let (<^>) (el : Html) (attr : Attribute) =
  match el with
    | Parent(n,op,cl,chdr) -> Parent(n, appendAttr op attr, cl, chdr)
    | Leaf(n, op)          -> Leaf(n, appendAttr op attr)
    

// html 5 only bae
let doctype =
  Leaf("doctype", "<!DOCTYPE html>")

let a children =
  Parent("a", "<a", "</a>", children)

let abbr children =
  Parent("abbr", "<abbr", "</abbr>", children)

let acronym children =
  Parent("acronym", "<acronym", "</acronym>", children)

let address children =
  Parent("address", "<address", "</address>", children)

let area children =
  Parent("area", "<area", "</area>", children)

let article children =
  Parent("article", "<article", "</article>", children)

let aside children =
  Parent("aside", "<aside", "</aside>", children)

let audio children =
  Parent("audio", "<audio", "</audio>", children)

let b children =
  Parent("b", "<b", "</b>", children)

let baseTag children =
  Parent("base", "<base", "</base>", children)

let basefont children =
  Parent("basefont", "<basefont", "</basefont>", children)

let bdi children =
  Parent("bdi", "<bdi", "</bdi>", children)

let bdo children =
  Parent("bdo", "<bdo", "</bdo>", children)

let big children =
  Parent("big", "<big", "</big>", children)

let blockquote children =
  Parent("blockquote", "<blockquote", "</blockquote>", children)

let body children =
  Parent("body", "<body", "</body>", children)

let br = Leaf("br", "<br")

let button children =
  Parent("button", "<button", "</button>", children)

let canvas = Leaf("canvas", "<canvas")

let caption children =
  Parent("caption", "<caption", "</caption>", children)

let center children =
  Parent("center", "<center", "</center>", children)

let cite children =
  Parent("cite", "<cite", "</cite>", children)

let code children =
  Parent("code", "<code", "</code>", children)

let col children =
  Parent("col", "<col", "</col>", children)

let colgroup children =
  Parent("colgroup", "<colgroup", "</colgroup>", children)

let command children =
  Parent("command", "<command", "</command>", children)

let datalist children =
  Parent("datalist", "<datalist", "</datalist>", children)

let dd children =
  Parent("dd", "<dd", "</dd>", children)

let del children =
  Parent("del", "<del", "</del>", children)

let details children =
  Parent("details", "<details", "</details>", children)

let dfn children =
  Parent("dfn", "<dfn", "</dfn>", children)

let dir children =
  Parent("dir", "<dir", "</dir>", children)

let div children =
  Parent("div", "<div", "</div>", children)

let dl children =
  Parent("dl", "<dl", "</dl>", children)

let dt children =
  Parent("dt", "<dt", "</dt>", children)

let em children =
  Parent("em", "<em", "</em>", children)

let embed children =
  Parent("embed", "<embed", "</embed>", children)

let fieldset children =
  Parent("fieldset", "<fieldset", "</fieldset>", children)

let figcaption children =
  Parent("figcaption", "<figcaption", "</figcaption>", children)

let figure children =
  Parent("figure", "<figure", "</figure>", children)

let font children =
  Parent("font", "<font", "</font>", children)

let footer children =
  Parent("footer", "<footer", "</footer>", children)

let form children =
  Parent("form", "<form", "</form>", children)

let frame children =
  Parent("frame", "<frame", "</frame>", children)

let frameset children =
  Parent("frameset", "<frameset", "</frameset>", children)

let h1 children =
  Parent("h1", "<h1", "</h1>", children)

let h2 children =
  Parent("h2", "<h2", "</h2>", children)

let h3 children =
  Parent("h3", "<h3", "</h3>", children)

let h4 children =
  Parent("h4", "<h4", "</h4>", children)

let h5 children =
  Parent("h5", "<h5", "</h5>", children)

let h6 children =
  Parent("h6", "<h6", "</h6>", children)

let head children =
  Parent("head", "<head", "</head>", children)

let header children =
  Parent("header", "<header", "</header>", children)

let hgroup children =
  Parent("hgroup", "<hgroup", "</hgroup>", children)

let hr =
  Leaf("hr", "<hr")

let html children =
  Parent("html", "<html", "</html>", children)

let i children =
  Parent("i", "<i", "</i>", children)

let iframe children =
  Parent("iframe", "<iframe", "</iframe>", children)

let img =
  Leaf("img", "<img")

let input children =
  Parent("input", "<input", "</input>", children)

let ins children =
  Parent("ins", "<ins", "</ins>", children)

let kbd children =
  Parent("kbd", "<kbd", "</kbd>", children)

let keygen children =
  Parent("keygen", "<keygen", "</keygen>", children)

let label children =
  Parent("label", "<label", "</label>", children)

let legend children =
  Parent("legend", "<legend", "</legend>", children)

let li children =
  Parent("li", "<li", "</li>", children)

let link =
  Leaf("link", "<link")

let map children =
  Parent("map", "<map", "</map>", children)

let mark children =
  Parent("mark", "<mark", "</mark>", children)

let menu children =
  Parent("menu", "<menu", "</menu>", children)

let meta =
  Leaf("meta", "<meta")

let meter children =
  Parent("meter", "<meter", "</meter>", children)

let nav children =
  Parent("nav", "<nav", "</nav>", children)

let noframes children =
  Parent("noframes", "<noframes", "</noframes>", children)

let noscript children =
  Parent("noscript", "<noscript", "</noscript>", children)

let objectTag children =
  Parent("object", "<object", "</object>", children)

let ol children =
  Parent("ol", "<ol", "</ol>", children)

let optgroup children =
  Parent("optgroup", "<optgroup", "</optgroup>", children)

let option children =
  Parent("option", "<option", "</option>", children)

let output children =
  Parent("output", "<output", "</output>", children)

let p children =
  Parent("p", "<p", "</p>", children)

let param children =
  Parent("param", "<param", "</param>", children)

let pre children =
  Parent("pre", "<pre", "</pre>", children)

let progress children =
  Parent("progress", "<progress", "</progress>", children)

let q children =
  Parent("q", "<q", "</q>", children)

let rp children =
  Parent("rp", "<rp", "</rp>", children)

let rt children =
  Parent("rt", "<rt", "</rt>", children)

let s children =
  Parent("s", "<s", "</s>", children)

let samp children =
  Parent("samp", "<samp", "</samp>", children)

let script children =
  Parent("script", "<script", "</script>", children)

let section children =
  Parent("section", "<section", "</section>", children)

let selectTag children =
  Parent("select", "<select", "</select>", children)

let small children =
  Parent("small", "<small", "</small>", children)

let source children =
  Parent("source", "<source", "</source>", children)

let span children =
  Parent("span", "<span", "</span>", children)

let strike children =
  Parent("strike", "<strike", "</strike>", children)

let strong children =
  Parent("strong", "<strong", "</strong>", children)

let style children =
  Parent("style", "<style", "</style>", children)

let sub children =
  Parent("sub", "<sub", "</sub>", children)

let summary children =
  Parent("summary", "<summary", "</summary>", children)

let sup children =
  Parent("sup", "<sup", "</sup>", children)

let table children =
  Parent("table", "<table", "</table>", children)

let tbody children =
  Parent("tbody", "<tbody", "</tbody>", children)

let td children =
  Parent("td", "<td", "</td>", children)

let textarea children =
  Parent("textarea", "<textarea", "</textarea>", children)

let tfoot children =
  Parent("tfoot", "<tfoot", "</tfoot>", children)

let th children =
  Parent("th", "<th", "</th>", children)

let thead children =
  Parent("thead", "<thead", "</thead>", children)

let time children =
  Parent("time", "<time", "</time>", children)

let title children =
  Parent("title", "<title", "</title>", children)

let tr children =
  Parent("tr", "<tr", "</tr>", children)

let track children =
  Parent("track", "<track", "</track>", children)

let tt children =
  Parent("tt", "<tt", "</tt>", children)

let u children =
  Parent("u", "<u", "</u>", children)

let ul children =
  Parent("ul", "<ul", "</ul>", children)

let var children =
  Parent("var", "<var", "</var>", children)

let video children =
  Parent("video", "<video", "</video>", children)

let wbr children =
  Parent("wbr", "<wbr", "</wbr>", children)
