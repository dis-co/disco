namespace Iris.Service

#if !IRIS_NODES

// * Imports

open System
open Iris.Core
open Iris.Service.Http

// * Iris

module Iris =

  let create post (options: IrisOptions) = either {
      let! httpServer = HttpServer.create options.Machine options.FrontendPath post
      do! httpServer.Start()
      return
        { new IIris with
            member self.Machine
              with get () = options.Machine

            member self.HttpServer
              with get () = httpServer

            member self.DiscoveryService
              with get () = failwith "discoveryservcie"

            member self.IrisService
              with get () = failwith "irisservice"

            member self.LoadProject(name, username, password, site) =
              failwith "load-project"

            member self.UnloadProject() = Right ()

            member self.Dispose() =
              dispose httpServer
          }
    }

#endif
