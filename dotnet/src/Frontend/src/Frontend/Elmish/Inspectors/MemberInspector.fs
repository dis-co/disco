namespace Iris.Web.Inspectors

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
open Iris.Raft
open Iris.Core
open Iris.Web.Core
open Iris.Web.Helpers
open Iris.Web.Types
open State

module MemberInspector =

  let render dispatch (model: Model) (mem: RaftMember) =
    Common.render "Cluster Member" [
      Common.stringRow "Id"             (string mem.Id)
      Common.stringRow "Host Name"      (string mem.HostName)
      Common.stringRow "Raft State"     (string mem.State)
      Common.stringRow "IP Address"     (string mem.IpAddr)
      Common.stringRow "Raft Port"      (string mem.Port)
      Common.stringRow "API Port"       (string mem.ApiPort)
      Common.stringRow "Git Port"       (string mem.GitPort)
      Common.stringRow "WebSocket Port" (string mem.WsPort)
    ]
