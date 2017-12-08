namespace Disco.Service

#if !DISCO_NODES

// * Imports

open System
open Disco.Net
open Disco.Core
open Disco.Raft
open Disco.Service

// * Disco

module Disco =

  // ** tag

  let private tag (str: string) = String.Format("Disco.{0}", str)

  // ** subscribeDiscovery

  let private subscribeDiscovery (disco: IDiscoService) = function
    | Some (service: IDiscoveryService) ->
      let subscription =
        service.Subscribe <| function
          | Appeared service -> service |> AddDiscoveredService    |> disco.Append
          | Vanished service -> service |> RemoveDiscoveredService |> disco.Append
          | Updated service  -> service |> UpdateDiscoveredService |> disco.Append
          | DiscoveryEvent.Status status ->
            status
            |> sprintf "discovery service status change to %O"
            |> Logger.info (tag "subscribeDiscovery")
      do service.Register disco.Project
      Some {
        new IDisposable with
          member self.Dispose () =
            do dispose subscription
            do service.UnRegister()
      }
    | None -> None

  // ** onShutdown

  let private onShutdown (disco: IDisco) = function
    | DiscoEvent.ConfigurationDone mems ->
      let ids = Array.map Member.id mems
      if not (Array.contains disco.Machine.MachineId ids) then
        match disco.UnloadProject() with
        | Right ()   -> Logger.info "onShutdown" "Unloaded project"
        | Left error -> Logger.err "onShutdown" error.Message
    | _ -> ()

  // ** create

  let create post (options: DiscoOptions) = either {
      let status = ref ServiceStatus.Stopped
      let disco = ref None
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
        new IDisco with
          member self.Machine
            with get () = options.Machine

          member self.HttpServer
            with get () = httpServer

          member self.DiscoveryService
            with get () = discovery

          member self.DiscoService
            with get () = !disco

          member self.LoadProject(name, username, password, site) = either {
              status := ServiceStatus.Starting
              Option.iter dispose !disco              // in case there was already something loaded
              Option.iter dispose !eventSubscription // and its subscription as well
              Option.iter dispose !shutdownSubscription // and its subscription as well
              let! discoService = DiscoService.create {
                Machine = options.Machine
                ProjectName = name
                UserName = username
                Password = password
                SiteId = site
              }
              match discoService.Start() with
              | Right () ->
                eventSubscription := subscribeDiscovery discoService discovery
                shutdownSubscription := self |> onShutdown |> discoService.Subscribe |> Some
                let mem = discoService.RaftServer.Raft.Member
                disco := Some discoService
                status := ServiceStatus.Running
                return ()
              | Left error ->
                status := ServiceStatus.Failed error
                return! Either.fail error
            }

          member self.SaveProject() =
            match !disco with
            | Some disco ->
              AppCommand.Save
              |> StateMachine.Command
              |> disco.Append
              |> Either.succeed
            | None ->
              "No project loaded"
              |> Error.asOther (tag "Save")
              |> Either.fail

          member self.UnloadProject() = either {
              match !disco, !eventSubscription with
              | Some discoService, subscription ->
                Option.iter dispose !shutdownSubscription
                Option.iter dispose !eventSubscription
                Option.iter dispose subscription
                dispose discoService
                disco := None
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
              Option.iter dispose !disco
              dispose httpServer
              Option.iter dispose discovery
              status := ServiceStatus.Disposed
        }
    }

#endif
