[<RequireQualifiedAccess>]
module Iris.Web.Sortable

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.React

type [<Pojo>] HandleProps<'T> =
  { value: 'T }

type [<Pojo>] ElementProps<'T> =
  { key: string
    index: int
    value: 'T }

type ISortEnd =
    abstract oldIndex: int
    abstract newIndex: int

type [<Pojo>] ContainerProps<'T> =
  { items: 'T[]
    useDragHandle: bool
    onSortEnd: ISortEnd -> unit
  }

let Element(f: (ElementProps<'T> -> ReactElement)): ComponentClass<ElementProps<'T>> = import "SortableElement" "react-sortable-hoc"
let Container(f: (ContainerProps<'T> -> ReactElement)): ComponentClass<ContainerProps<'T>> = import "SortableContainer" "react-sortable-hoc"
let Handle(f: (HandleProps<'T> -> ReactElement)): ComponentClass<HandleProps<'T>> = import "SortableHandle" "react-sortable-hoc"
let arrayMove(items: 'T[], oldIndex: int, newIndex: int): 'T[] = importMember "react-sortable-hoc"
