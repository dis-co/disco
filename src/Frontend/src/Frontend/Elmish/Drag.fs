[<RequireQualifiedAccess>]
module Iris.Web.Drag

open System
open System.Collections.Generic
open Iris.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Helpers
open Types

type IDomToImage =
  abstract toPng: el:Browser.Element * options:obj -> JS.Promise<string>

let domtoimage: IDomToImage = importDefault "dom-to-image"
let jQueryEventAsPromise(selector:obj, events:string): JS.Promise<obj> = importMember "../../js/Util"

type Event =
  | Moved of x:float * y:float * data: DragItems
  | Stopped of x:float * y:float * data: DragItems

let private observers = Dictionary<Guid, IObserver<Event>>()

let observe () =
  { new IObservable<_> with
    member __.Subscribe(obs) =
      let guid = Guid.NewGuid()
      observers.Add(guid, obs)
      { new IDisposable with
          member __.Dispose() = observers.Remove(guid) |> ignore } }

let private trigger (ev: Event) =
  for obs in observers.Values do
    obs.OnNext(ev)

let length (items: DragItems) =
  match items with
  | DragItems.Pins ids -> List.length ids
  | DragItems.CueAtoms ids -> List.length ids

let start el (data: DragItems) =
  let mutable prev: Point option = None
  let img = jQuery("#iris-drag-image")
  !!jQuery(Browser.window.document)
    ?on("mousemove.drag", fun e ->
      let cur = { Point.x = !!e?clientX; y = !!e?clientY }
      match prev with
      | None ->
        prev <- Some cur
        let styles = %["display" => "flex"
                       "left" => cur.x
                       "top" => cur.y]
        !!jQuery(img)?text(length data)?css(styles)
        Moved(cur.x, cur.y, data) |> trigger
      | Some p when distance p cur > 5. ->
        prev <- Some cur
        let styles = %["left" => cur.x
                       "top" => cur.y]
        !!jQuery(img)?css(styles)
        Moved(cur.x, cur.y, data) |> trigger
      | Some _ -> ())
    ?on("mouseup.drag", fun e ->
      !!jQuery(img)?css(%["display" => "none"])
      Stopped(!!e?clientX, !!e?clientY, data) |> trigger
      jQuery(Browser.window.document)?off("mousemove.drag mouseup.drag"))
