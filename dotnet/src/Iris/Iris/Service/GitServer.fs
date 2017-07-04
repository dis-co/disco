namespace Iris.Service

// * Imports

open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Interfaces

open Suave
open Suave.Http
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers
open Suave.Logging
open Suave.Logging.Log
open Suave.CORS
open Suave.Web
open Suave.Git

open System
open System.IO
open System.Text
open System.Threading
open System.Diagnostics

open System.Collections.Concurrent

// * GitServer

module GitServer =

  // ** tag

  let private tag (str: string) = sprintf "GitServer.%s" str

  // ** Subscriptions

  type Subscriptions = ConcurrentDictionary<Guid,IObserver<IrisEvent>>

  // ** makeConfig

  let private makeConfig (ip: IpAddress) port (cts: CancellationTokenSource) =
    let addr = ip.toIPAddress()
    { defaultConfig with
        cancellationToken = cts.Token
        bindings = [ HttpBinding.create HTTP addr port ]
        mimeTypesMap = defaultMimeTypesMap }

  // ** route

  let private route (name: string) (path: string) =
    if name.StartsWith("/") then
      String.Format("{0}{1}", name, path)
    else
      String.Format("/{0}{1}", name, path)

  // ** routes

  let private routes (project: IrisProject) (subscriptions: Subscriptions) =
    let name = unwrap project.Name
    let path = unwrap project.Path
    let handler = gitServer (Some name) path
    choose [
        Filters.POST >=>
          (choose [
            Filters.path (route name "/git-receive-pack") >=> fun req ->
              req.request.clientHost false []
              |> IpAddress.Parse
              |> IrisEvent.GitPush
              |> Observable.onNext subscriptions
              handler req
            Filters.path (route name "/git-upload-pack" ) >=> fun req ->
              req.request.clientHost false []
              |> IpAddress.Parse
              |> IrisEvent.GitPull
              |> Observable.onNext subscriptions
              handler req ])
        handler
      ]

  // ** create

  let create (mem: RaftMember) (project: IrisProject) =
    let mutable status = ServiceStatus.Stopped
    let cts = new CancellationTokenSource()
    let subscriptions = Subscriptions()

    { new IGitServer with
        member self.Status
          with get () = status

        member self.Subscribe(callback: IrisEvent -> unit) =
          Observable.subscribe<IrisEvent> callback subscriptions

        member self.Start () = either {
            do! Network.ensureIpAddress mem.IpAddr
            do! Network.ensureAvailability mem.IpAddr mem.GitPort

            status <- ServiceStatus.Starting
            let config = makeConfig mem.IpAddr (unwrap mem.GitPort) cts

            routes project subscriptions
            |> startWebServerAsync config
            |> (fun (_, server) -> Async.Start(server, cts.Token))

            Thread.Sleep(150)

            ServiceType.Git
            |> IrisEvent.Started
            |> Observable.onNext subscriptions

            status <- ServiceStatus.Running
          }

        member self.Dispose() =
          if not (Service.isDisposed status) then
            try
              cts.Cancel()
              cts.Dispose()
            finally
              status <- ServiceStatus.Disposed
      }
