namespace Iris.Service

#if !IRIS_NODES

// * Imports

open System
open Iris.Core
open Iris.Service.Http
open Iris.Service.Discovery
open FSharpx.Functional

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

  // ** create

  let create post (options: IrisOptions) = either {
      let iris = ref None
      let subscription = ref None

      let! httpServer = HttpServer.create options.Machine options.FrontendPath post

      let! discovery =
        DiscoveryService.create options.Machine
        |> fun service -> service.Start() |> Either.map (konst service)
        |> Either.map Some
        |> Either.orElse None

      do! httpServer.Start()
      return
        { new IIris with
            member self.Machine
              with get () = options.Machine

            member self.HttpServer
              with get () = httpServer

            member self.DiscoveryService
              with get () = discovery

            member self.IrisService
              with get () = !iris

            member self.LoadProject(name, username, password, site) = either {
                let irisService = IrisService.create {
                  Machine = options.Machine
                  ProjectName = name
                  UserName = username
                  Password = password
                  SiteName = site
                }
                do! irisService.Start()
                subscription := subscribeDiscovery irisService discovery
                iris := Some irisService
                return ()
              }

            member self.UnloadProject() = either {
                match !iris, !subscription with
                | Some irisService, subscription ->
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
              self.UnloadProject() |> ignore
              Option.iter dispose discovery
              dispose httpServer
          }
    }

#endif
