[<RequireQualifiedAccess>]
module Iris.Web.Drag

open System
open System.Collections.Generic
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Import
open Elmish
open Types
open Helpers

type IDomToImage =
  abstract toPng: el:Browser.Element * options:obj -> JS.Promise<string>

let domtoimage: IDomToImage = importDefault "dom-to-image"
let jQueryEventAsPromise(selector:obj, events:string): JS.Promise<obj> = importMember "../../js/Util"

type Data =
  | Pin of Pin list
  member this.Length =
    match this with
    | Pin pins -> List.length pins

type Event =
  | Moved of x:float * y:float * data:Data
  | Stopped of x:float * y:float * data:Data

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

let start el (data:Data) =
  let mutable prev: Point option = None
  let img = jQuery("#iris-drag-image")
  !!jQuery(Browser.window.document)
    ?on("mousemove.drag", fun e ->
      let cur = { Point.x = !!e?clientX; y = !!e?clientY }
      match prev with
      | None ->
        prev <- Some cur
        !!jQuery(img)?text(data.Length)?css(%["display" => "flex"; "left" => cur.x; "top" => cur.y])
        Moved(cur.x, cur.y, data) |> trigger
      | Some p when distance p cur > 5. ->
        prev <- Some cur
        !!jQuery(img)?css(%["left" => cur.x; "top" => cur.y])
        Moved(cur.x, cur.y, data) |> trigger
      | Some _ -> ())
    ?on("mouseup.drag", fun e ->
      !!jQuery(img)?css(%["display" => "none"])
      Stopped(!!e?clientX, !!e?clientY, data) |> trigger
      jQuery(Browser.window.document)?off("mousemove.drag mouseup.drag"))
