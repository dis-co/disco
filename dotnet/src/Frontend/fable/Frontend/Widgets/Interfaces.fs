namespace Iris.Web.Widgets

open System
open System.Collections.Generic
open Fable.Core
open Iris.Core
open Iris.Web.Core

type [<Pojo>] Layout =
  {
    x: int; y: int;
    w: int; h: int;
    minW: int; maxW: int;
    minH: int; maxH: int;
  }

type IWidgetModel =
  abstract name: string
  abstract layout: Layout
  abstract view: System.Type

type [<Pojo>] IWidgetProps<'T> =
  abstract id: Guid
  abstract model: 'T
  abstract ``global``: GlobalModel

type IDragEvent<'T> =
  abstract origin: int
  abstract x: float
  abstract y: float
  abstract ``type``: string
  abstract model: 'T

type GenericObservable<'T>() =
  let listeners = Dictionary<Guid,IObserver<'T>>()
  member x.Trigger v =
    for lis in listeners.Values do
      lis.OnNext v
  interface IObservable<'T> with
    member x.Subscribe w =
      let guid = Guid.NewGuid()
      listeners.Add(guid, w)
      { new IDisposable with
        member x.Dispose() = listeners.Remove(guid) |> ignore }
