namespace Iris.Service

#if !IRIS_NODES

// * Imports

open System
open Iris.Net
open Iris.Core
open Iris.Raft
open Iris.Service

// * Iris

module Iris =

  // ** tag

  let private tag (str: string) = String.Format("Iris.{0}", str)

  // ** subscribeDiscovery

  let private subscribeDiscovery (iris: IIrisService) = function
    | Some (service: IDiscoveryService) ->
      let subscription =
        service.Subscribe <| function
          | Appeared service -> service |> AddDiscoveredService    |> iris.Append
          | Vanished service -> service |> RemoveDiscoveredService |> iris.Append
          | Updated service  -> service |> UpdateDiscoveredService |> iris.Append
          | DiscoveryEvent.Status status ->
            status
            |> sprintf "discovery service status change to %O"
            |> Logger.info (tag "subscribeDiscovery")
      do service.Register iris.Project
      Some {
        new IDisposable with
          member self.Dispose () =
            do dispose subscription
            do service.UnRegister()
      }
    | None -> None

  // ** onShutdown

  let private onShutdown (iris: IIris) = function
    | IrisEvent.ConfigurationDone mems ->
      let ids = Array.map Member.id mems
      if not (Array.contains iris.Machine.MachineId ids) then
        match iris.UnloadProject() with
        | Right ()   -> Logger.info "onShutdown" "Unloaded project"
        | Left error -> Logger.err "onShutdown" error.Message
    | _ -> ()

  // ** create

  let create post (options: IrisOptions) = either {
      let status = ref ServiceStatus.Stopped
      let iris = ref None
      let eventSubscription = ref None
      let shutdownSubscription = ref None

      let! httpServer = HttpServer.create options.Machine options.FrontendPath post

      let! discovery =
        options.Machine
        |> DiscoveryService.create
        |> fun service -> service.Start() |> Either.map (konst service)
        |> Either.map Some
        |> Either.orElse None

      do! httpServer.Start()
      return {
        new IIris with
          member self.Machine
            with get () = options.Machine

          member self.HttpServer
            with get () = httpServer

          member self.DiscoveryService
            with get () = discovery

          member self.IrisService
            with get () = !iris

          member self.LoadProject(name, username, password, site) = either {
              status := ServiceStatus.Starting
              Option.iter dispose !iris              // in case there was already something loaded
              Option.iter dispose !eventSubscription // and its subscription as well
              Option.iter dispose !shutdownSubscription // and its subscription as well
              let! irisService = IrisService.create {
                Machine = options.Machine
                ProjectName = name
                UserName = username
                Password = password
                SiteId = site
              }
              match irisService.Start() with
              | Right () ->
                eventSubscription := subscribeDiscovery irisService discovery
                shutdownSubscription := self |> onShutdown |> irisService.Subscribe |> Some
                let mem = irisService.RaftServer.Raft.Member
                iris := Some irisService
                status := ServiceStatus.Running
                return ()
              | Left error ->
                status := ServiceStatus.Failed error
                return! Either.fail error
            }

          member self.SaveProject() =
            match !iris with
            | Some iris ->
              AppCommand.Save
              |> StateMachine.Command
              |> iris.Append
              |> Either.succeed
            | None ->
              "No project loaded"
              |> Error.asOther (tag "Save")
              |> Either.fail

          member self.UnloadProject() = either {
              match !iris, !eventSubscription with
              | Some irisService, subscription ->
                Option.iter dispose !shutdownSubscription
                Option.iter dispose !eventSubscription
                Option.iter dispose subscription
                dispose irisService
                iris := None
                return ()
              | None, _ ->
                return!
                  "No project was loaded"
                  |> Error.asOther (tag "UnloadProject")
                  |> Either.fail
            }

          member self.Dispose() =
            if not (Service.isDisposed !status) then
              status := ServiceStatus.Stopping
              Option.iter dispose !eventSubscription
              Option.iter dispose !shutdownSubscription
              Option.iter dispose !iris
              dispose httpServer
              Option.iter dispose discovery
              status := ServiceStatus.Disposed
        }
    }

#endif
