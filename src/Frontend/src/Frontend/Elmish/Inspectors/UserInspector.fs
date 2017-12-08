namespace Disco.Web.Inspectors

open System
open System.Collections.Generic
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Disco.Core
open Disco.Web.Core
open Disco.Web.Helpers
open Disco.Web.Types
open State

module UserInspector =

  let render dispatch (model: Model) (user: UserId) =
    match model.state with
    | None ->
      Common.render dispatch model "User" [
        str (string user + " (orphaned)")
      ]
    | Some state ->
      match Map.tryFind user state.Users with
      | None ->
        Common.render dispatch model "User" [
          str (string user + " (orphaned)")
        ]
      | Some user ->
        Common.render dispatch model "User" [
          Common.stringRow "Id"         (string user.Id)
          Common.stringRow "User Name"  (string user.UserName)
          Common.stringRow "First Name" (string user.FirstName)
          Common.stringRow "Last Name"  (string user.LastName)
          Common.stringRow "Email"      (string user.Email)
          Common.stringRow "Joined"     (string user.Joined)
          Common.stringRow "Created"    (string user.Created)
        ]
