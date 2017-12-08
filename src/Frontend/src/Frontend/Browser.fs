namespace Disco.Web.Core

//  _   _ _   _ _ _ _   _
// | | | | |_(_) (_) |_(_) ___  ___
// | | | | __| | | | __| |/ _ \/ __|
// | |_| | |_| | | | |_| |  __/\__ \
//  \___/ \__|_|_|_|\__|_|\___||___/

[<AutoOpen>]
module Browser =

  open Fable.Core
  open Fable.Import

  // jQuery
  let [<Global("$")>] jQuery(arg: obj): obj = jsNative

  //  ____   ___  __  __
  // |  _ \ / _ \|  \/  |
  // | | | | | | | |\/| |
  // | |_| | |_| | |  | |
  // |____/ \___/|_|  |_|

  /// Safe function to get an element by id
  let getById<'T when 'T :> Browser.HTMLElement> id =
    let el = Browser.document.getElementById id
    if isNull el then None
    else Some el

  /// Safe function to get an element by id
  let getByClass<'T when 'T :> Browser.HTMLElement> klass =
    Browser.document.getElementsByClassName klass

  let getByTag<'T when 'T :> Browser.HTMLElement> tag =
    Browser.document.getElementsByTagName tag

  let inline split (c: char) (txt: string) : string array = txt.Split(c)

  let inline contains (el: 'a) (ar: 'a array) : bool = Array.contains el ar

  let hasClass klass (el: Browser.HTMLElement) =
    split ' ' el.className |> contains klass

  let parseStyle (style: string) =
    let parsed = split ':' style
    (parsed.[0].Trim(), parsed.[1].Trim())

  let parseStyles (str: string) =
    Array.filter ((<>) "") (split ';' str)
    |> Array.map parseStyle

  let getStyle style (el: Browser.Element) =
    let attr = el.getAttribute "style"
    if isNull attr then None
    else
      let styles = parseStyles attr |> Map.ofArray
      Map.tryFind style styles

  let childrenByClass klass (el: Browser.Element) : Browser.NodeListOf<Browser.Element> =
    el.getElementsByClassName klass

  let childrenByTag tag (el: Browser.Element) : Browser.NodeListOf<Browser.Element> =
    el.getElementsByTagName tag

  let nthElement n (lst: Browser.NodeListOf<_>) : Browser.Element =
    lst.[n]

  let appendChild (el: Browser.Element) (target: Browser.Element) =
    target.appendChild el

  let inline asHtml (el: 'a when 'a :> Browser.Node) : Browser.HTMLElement = unbox el

  let asHtmlInput (el: 'a when 'a :> Browser.Node) : Browser.HTMLInputElement = unbox el

  //  _____                 _
  // | ____|_   _____ _ __ | |_ ___
  // |  _| \ \ / / _ \ '_ \| __/ __|
  // | |___ \ V /  __/ | | | |_\__ \
  // |_____| \_/ \___|_| |_|\__|___/

  let trigger (t: 'a when 'a :> Browser.Event) (el: 'e when 'e :> Browser.Element) =
    el.dispatchEvent(t)

  let click (el: 'e when 'e :> Browser.Element) =
    let ev = Browser.MouseEvent.Create("click")
    trigger ev el

  let change (el: 'e when 'e :> Browser.Element) =
    let ev = Browser.Event.Create("change")
    trigger ev el
