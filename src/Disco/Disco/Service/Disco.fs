(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

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
        | Ok ()   -> Logger.info "onShutdown" "Unloaded project"
        | Error error -> Logger.err "onShutdown" error.Message
    | _ -> ()

  // ** create

  let create post (options: DiscoOptions) = result {
      let status = ref ServiceStatus.Stopped
      let disco = ref None
      let eventSubscription = ref None
      let shutdownSubscription = ref None

      let! httpServer = HttpServer.create options.Machine options.FrontendPath post

      let! discovery =
        options.Machine
        |> DiscoveryService.create
        |> fun service -> service.Start() |> Result.map (konst service)
        |> Result.map Some
        |> Result.orElse None

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

          member self.LoadProject(name, username, password, site) = result {
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
              | Ok () ->
                eventSubscription := subscribeDiscovery discoService discovery
                shutdownSubscription := self |> onShutdown |> discoService.Subscribe |> Some
                let mem = discoService.RaftServer.Raft.Member
                disco := Some discoService
                status := ServiceStatus.Running
                return ()
              | Error error ->
                status := ServiceStatus.Failed error
                return! Result.fail error
            }

          member self.SaveProject() =
            match !disco with
            | Some disco ->
              AppCommand.Save
              |> StateMachine.Command
              |> disco.Append
              |> Result.succeed
            | None ->
              "No project loaded"
              |> Error.asOther (tag "Save")
              |> Result.fail

          member self.UnloadProject() = result {
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
                  |> Result.fail
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
