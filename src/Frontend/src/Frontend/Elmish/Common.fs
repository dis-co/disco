[<RequireQualifiedAccess>]
module Iris.Web.Common

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Iris.Web
open Types
open Helpers

type private RCom = React.ComponentClass<obj>
let private ContentEditable: RCom = importDefault "../../js/widgets/ContentEditable"
let private DropdownEditable: RCom = importDefault "../../js/widgets/DropdownEditable"

let editableString content (update: string -> unit) =
  from ContentEditable
    %["tagName" ==> "div"
      "html" ==> content
      "className" ==> "iris-contenteditable"
      "onChange" ==> update] []

let editableDropdown
  (content:string)
  (selected: string option)
  (props: (string*string)[])
  (update: string option -> unit) =
  from DropdownEditable
    %["tagName" ==> "span"
      "html" ==> content
      "data-selected" ==> selected
      "data-options" ==> props
      "onChange" ==> update] []
