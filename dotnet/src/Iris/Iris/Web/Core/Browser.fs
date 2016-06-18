namespace Iris.Web.Core

//  _   _ _   _ _ _ _   _
// | | | | |_(_) (_) |_(_) ___  ___
// | | | | __| | | | __| |/ _ \/ __|
// | |_| | |_| | | | |_| |  __/\__ \
//  \___/ \__|_|_|_|\__|_|\___||___/

[<AutoOpen>]
module Browser =

  open Fable.Core
  open Fable.Import
 
  [<Emit("$0 === null || $0 === undefined")>]
  let isNullValue (_: 't) : bool = failwith "JS ONLY"

  //  ____   ___  __  __
  // |  _ \ / _ \|  \/  |
  // | | | | | | | |\/| |
  // | |_| | |_| | |  | |
  // |____/ \___/|_|  |_|

  /// Safe function to get an element by id
  let getById<'T when 'T :> Browser.HTMLElement> id =
    let el = Browser.document.getElementById id
    if isNullValue el then None
    else Some el

  /// Safe function to get an element by id
  let getByClass<'T when 'T :> Browser.HTMLElement> klass =
    Browser.document.getElementsByClassName klass

  let getByTag<'T when 'T :> Browser.HTMLElement> tag =
    Browser.document.getElementsByTagName tag

  [<Emit("$0.split($1)")>]
  let split (_: string) (_: string) : string array = failwith "ONLY JS"

  [<Emit("$1.indexOf($0) != -1")>]
  let contains (_: 'a) (_: 'a array) : bool = failwith "ONLY JS"

  let hasClass klass (el: Browser.HTMLElement) =
    let attr = el.getAttribute "class"
    if isNullValue attr then false
    else 
      split " " attr |> contains klass

  let parseStyle (style: string) =
    let parsed = style.Split(':')
    in  (parsed.[0].Trim(), parsed.[1].Trim())

  let parseStyles (str: string) =
     str.Split(';') |> Array.map parseStyle

  let getStyle style (el: Browser.Element) =
    let attr = el.getAttribute "style"
    if isNullValue attr then None
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

  [<Emit "Object.is($0, $1)">]
  let identical (_: obj) (_: obj) = failwith "OH NO ITS ITS ITS JS"
    
  let asHtml (el: 'a when 'a :> Browser.HTMLElement) = el :> Browser.HTMLElement
