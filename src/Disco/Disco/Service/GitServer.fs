(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service

// * Imports

open Disco.Raft
open Disco.Core
open Disco.Core.Utils
open Disco.Service.Interfaces

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
open System.Text.RegularExpressions
open System.Threading
open System.Diagnostics

open System.Collections.Concurrent

// * GitServer

module GitServer =

  // ** tag

  let private tag (str: string) = sprintf "GitServer.%s" str

  // ** Subscriptions

  type Subscriptions = ConcurrentDictionary<Guid,IObserver<DiscoEvent>>

  // ** logger

  let private makeLogger () =
    let reg = Regex("\{(\w+)(?:\:(.*?))?\}")
    { new Logger with
        member x.log(level: Suave.Logging.LogLevel) (nextLine: Suave.Logging.LogLevel -> Message): Async<unit> =
          match level with
          | Suave.Logging.LogLevel.Verbose -> ()
          | level ->
            let line = nextLine level
            match line.value with
            | Event template ->
              reg.Replace(template, fun m ->
                let value = line.fields.[m.Groups.[1].Value]
                if m.Groups.Count = 3
                then System.String.Format("{0:" + m.Groups.[2].Value + "}", value)
                else string value)
              |> Logger.debug (tag "logger")
            | Gauge _ -> ()
          async.Return ()
        member x.logWithAck(arg1: Suave.Logging.LogLevel) (arg2: Suave.Logging.LogLevel -> Message): Async<unit> =
          // failwith "Not implemented yet"
          async.Return ()
        member x.name: string [] =
          [| "disco" |] }

  // ** makeConfig

  let private makeConfig (ip: IpAddress) port (cts: CancellationTokenSource) =
    let addr = ip.toIPAddress()
    { defaultConfig with
        logger = makeLogger()
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

  let private routes (project: DiscoProject) (subscriptions: Subscriptions) =
    let name = unwrap project.Name
    let path = unwrap project.Path
    let handler = gitServer (Some name) path
    choose [
        Filters.POST >=>
          (choose [
            Filters.path (route name "/git-receive-pack") >=> fun req ->
              req.request.clientHost false []
              |> IpAddress.Parse
              |> DiscoEvent.GitPush
              |> Observable.onNext subscriptions
              handler req
            Filters.path (route name "/git-upload-pack" ) >=> fun req ->
              req.request.clientHost false []
              |> IpAddress.Parse
              |> DiscoEvent.GitPull
              |> Observable.onNext subscriptions
              handler req ])
        handler
      ]

  // ** create

  let create (mem: ClusterMember) (project: DiscoProject) =
    let mutable status = ServiceStatus.Stopped
    let cts = new CancellationTokenSource()
    let subscriptions = Subscriptions()

    { new IGitServer with
        member self.Status
          with get () = status

        member self.Subscribe(callback: DiscoEvent -> unit) =
          Observable.subscribe<DiscoEvent> callback subscriptions

        member self.Start () = either {
            do! Network.ensureIpAddress mem.IpAddress
            do! Network.ensureAvailability mem.IpAddress mem.GitPort

            status <- ServiceStatus.Starting
            let config = makeConfig mem.IpAddress (unwrap mem.GitPort) cts

            routes project subscriptions
            |> startWebServerAsync config
            |> (fun (_, server) -> Async.Start(server, cts.Token))

            Thread.Sleep(150)

            ServiceType.Git
            |> DiscoEvent.Started
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
