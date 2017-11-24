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
      service.Subscribe <| function
        | Appeared service -> service |> AddDiscoveredService |> iris.Append
        | Vanished service -> service |> RemoveDiscoveredService |> iris.Append
        | Updated service -> service |> UpdateDiscoveredService |> iris.Append
        | Registered service ->
          service.Id
          |> sprintf "successfully registered service %O"
          |> Logger.info (tag "subscribeDiscovery")
        | UnRegistered service ->
          service.Id
          |> sprintf "successfully unregistered service %O"
          |> Logger.info (tag "subscribeDiscovery")
        | DiscoveryEvent.Status status ->
          status
          |> sprintf "discovery service status change to %O"
          |> Logger.info (tag "subscribeDiscovery")
        | _ -> ()
      |> Some
    | None -> None

  // ** registerService

  let private registerService (service: IDiscoveryService)
                              (config: IrisMachine)
                              (status: MachineStatus)
                              (services: ExposedService[])
                              (metadata: Property[])=
    { Id = config.MachineId
      WebPort = config.WebPort
      Status = status
      Services = services
      ExtraMetadata = metadata }
    |> service.Register
    |> Some

  // ** registerIdleServices

  let private registerIdleServices (config: IrisMachine) (service: IDiscoveryService) =
    let services =
      [| { ServiceType = ServiceType.Http
           Port = config.WebPort } |]
    registerService service config MachineStatus.Idle services [| |]

  // ** registerLoadedServices

  let private registerLoadedServices (mem: RaftMember) (project: IrisProject) service =
    let status = MachineStatus.Busy (project.Id, project.Name)
    let services =
      [| { ServiceType = ServiceType.Api;       Port = mem.ApiPort }
         { ServiceType = ServiceType.Git;       Port = mem.GitPort }
         { ServiceType = ServiceType.Raft;      Port = mem.Port    }
         { ServiceType = ServiceType.Http;      Port = project.Config.Machine.WebPort }
         { ServiceType = ServiceType.WebSocket; Port = mem.WsPort  } |]
    registerService service project.Config.Machine status services [| |]

  // ** create

  let create post (options: IrisOptions) = either {
      let status = ref ServiceStatus.Stopped
      let iris = ref None
      let registration = ref None
      let eventSubscription = ref None
      let assetSubscription = ref None

      let! httpServer = HttpServer.create options.Machine options.FrontendPath post
      let! assetService = AssetService.create options.Machine

      do! assetService.Start()

      let! discovery =
        DiscoveryService.create options.Machine
        |> fun service -> service.Start() |> Either.map (konst service)
        |> Either.map Some
        |> Either.orElse None

      registration := Option.bind (registerIdleServices options.Machine) discovery

      do! httpServer.Start()
      return
        { new IIris with
            member self.Machine
              with get () = options.Machine

            member self.AssetService
              with get () = assetService

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
                Option.iter dispose !registration      // and any registered service
                Option.iter dispose !assetSubscription // as well as subscription to asset service
                let! irisService = IrisService.create {
                  Machine = options.Machine
                  ProjectName = name
                  UserName = username
                  Password = password
                  SiteId = site
                }
                match irisService.Start() with
                | Right () ->
                  do Option.iter (AddFsTree >> irisService.Append) assetService.State
                  eventSubscription := subscribeDiscovery irisService discovery
                  assetSubscription := Some(assetService.Subscribe irisService.Append)
                  let mem = irisService.RaftServer.Raft.Member
                  let project = irisService.Project
                  registration := Option.bind (registerLoadedServices mem project) discovery
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
                  Option.iter dispose !registration
                  Option.iter dispose !assetSubscription
                  Option.iter dispose subscription
                  dispose irisService
                  iris := None
                  registration := Option.bind (registerIdleServices options.Machine) discovery
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
                Option.iter dispose !registration
                Option.iter dispose !assetSubscription
                Option.iter dispose !eventSubscription
                Option.iter dispose !iris
                dispose httpServer
                dispose assetService
                Option.iter dispose discovery
                status := ServiceStatus.Disposed
          }
    }

#endif
