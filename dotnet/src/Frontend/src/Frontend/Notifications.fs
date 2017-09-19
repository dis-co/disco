module Iris.Web.Notifications

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open System
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props

///  ____       _            _
/// |  _ \ _ __(_)_   ____ _| |_ ___
/// | |_) | '__| \ \ / / _` | __/ _ \
/// |  __/| |  | |\ V / (_| | ||  __/
/// |_|   |_|  |_| \_/ \__,_|\__\___|

let private NotificationSystem: React.ComponentClass<obj> =
  importDefault "react-notification-system"

[<StringEnum>]
type private NotificationLevel =
  | Success
  | Info
  | Warning
  | Error

[<Pojo>]
type private Notification =
  { message: string
    level: NotificationLevel }

type private INotificationSystem =
  abstract addNotification: Notification -> unit

let mutable private notificationSystem: INotificationSystem option = None

let private notify level msg =
  match notificationSystem with
  | None -> printfn "Warning: Notification system uninitialized."
  | Some system ->
    system.addNotification {
      message = msg
      level = level
    }

///  ____        _     _ _
/// |  _ \ _   _| |__ | (_) ___
/// | |_) | | | | '_ \| | |/ __|
/// |  __/| |_| | |_) | | | (__
/// |_|    \__,_|_.__/|_|_|\___|

module Notifications =

  let success msg = notify Success msg
  let info msg = notify Info msg
  let warn msg = notify Warning msg
  let error msg = notify Error msg

  let root : ReactElement =
    let props = createObj [ "ref" ==> fun el -> notificationSystem <- el ]
    from NotificationSystem props []
