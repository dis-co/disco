namespace Iris.Web.Widgets

open System
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
  abstract ``global``: IGlobalModel

type IDragEvent<'T> =
  abstract origin: int
  abstract x: float
  abstract y: float
  abstract ``type``: string
  abstract model: 'T