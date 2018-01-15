(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service

// * Imports

open Disco.Core
open Disco.Core.Utils
open Disco.Service.Interfaces

open System
open System.IO
open System.Threading
open System.Collections.Concurrent

open Mono.Zeroconf

// * DiscoveryService

//  ____  _
// |  _ \(_)___  ___ _____   _____ _ __ _   _
// | | | | / __|/ __/ _ \ \ / / _ \ '__| | | |
// | |_| | \__ \ (_| (_) \ V /  __/ |  | |_| |
// |____/|_|___/\___\___/ \_/ \___|_|   \__, |
//                                      |___/

module DiscoveryService =
  open Disco.Core.Discovery

  // ** tag

  let private tag (str: string) = String.Format("DiscoveryService.{0}", str)

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<DiscoveryEvent>

  // ** DiscoveryState

  [<NoEquality;NoComparison>]
  type private DiscoveryState =
    { Status: ServiceStatus
      Machine: DiscoMachine
      Browser: ServiceBrowser
      Subscriptions: Subscriptions
      RegisterService: RegisterService option
      ResolvedServices: Map<ServiceId,DiscoveredService> }

    interface IDisposable with
      member self.Dispose() =
        Option.iter dispose self.RegisterService
        dispose self.Browser
        self.Subscriptions.Clear()

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start
    | Stop        of AutoResetEvent
    | Notify      of DiscoveryEvent
    | Register    of project:DiscoProject
    | UnRegister
    | RegisterErr of err:string * srvc:DiscoverableService
    | Discovered  of srvc:DiscoveredService
    | Vanished    of id:ServiceId

  // ** DiscoveryAgent

  type private DiscoveryAgent = IActor<Msg>

  // ** handleNotify

  let private handleNotify (state: DiscoveryState) (ev: DiscoveryEvent) =
    Observable.onNext state.Subscriptions ev
    state

  // ** addResolved

  let private addResolved (agent: DiscoveryAgent) (resolved: obj) _ =
    let service =
      resolved
      :?> IResolvableService
      |> Discovery.toDiscoveredService

    match service with
    | Right parsed ->
      parsed.Id
      |> sprintf "resolved new service %O"
      |> Logger.debug (tag "addResolved")

      parsed
      |> Msg.Discovered
      |> agent.Post
    | Left _ -> ()

  // ** serviceAdded

  let private serviceAdded (agent: DiscoveryAgent) (_: obj) (args: ServiceBrowseEventArgs) =
    try
      args.Service.Resolved.AddHandler(new ServiceResolvedEventHandler(addResolved agent))
      args.Service.Resolve()
    with exn ->
      exn.Message
      |> String.format "Unable to resolve service: {0}"
      |> Logger.err (tag "serviceAdded")

  // ** serviceRemoved

  let private serviceRemoved (agent: DiscoveryAgent) (_: obj) (args: ServiceBrowseEventArgs) =
    match args.Service.Name with
    | ServiceId id -> id |> Msg.Vanished |> agent.Post
    | _ -> ()

  // ** serviceRegistered

  let private serviceRegistered (agent: DiscoveryAgent)
                                discoverable
                                (args: RegisterServiceEventArgs) =
    match args.ServiceError with
    | ServiceErrorCode.None -> ()
    | ServiceErrorCode.NameConflict ->
      args.Service.Name
      |> sprintf "name collision: '%s' is already registered"
      |> fun err -> Msg.RegisterErr(err,discoverable)
      |> agent.Post
    | x ->
      args.Service.Name
      |> sprintf "Error (%A) registering name = '%s'" x
      |> fun err -> Msg.RegisterErr(err,discoverable)
      |> agent.Post

  // ** registerService

  let private registerService (agent: DiscoveryAgent) discoverable =
    try
      let service = Discovery.toDiscoverableService discoverable
      service.Response.Add(serviceRegistered agent discoverable)
      service.Register()
      Either.succeed service
    with | exn ->
      exn.Message
      |> Error.asOther (tag "registerService")
      |> Either.fail

  // ** unregisterService

  let private unregisterService (services: RegisterService list)
                                (srvc: DiscoverableService)
                                (_: Subscriptions) =
    for service in services do
      match service.Name with
      | ServiceId id when id = srvc.Id -> dispose service
      | _ -> ()

  // ** makeBrowser

  let private makeBrowser (agent: DiscoveryAgent) =
    try
      let browser = new ServiceBrowser()
      browser.ServiceAdded.AddHandler(new ServiceBrowseEventHandler(serviceAdded agent))
      browser.ServiceRemoved.AddHandler(new ServiceBrowseEventHandler(serviceRemoved agent))
      browser |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Error.asOther (tag "makeBrowser")
        |> Either.fail

  // ** handleStop

  let private handleStop (state: DiscoveryState) (agent: DiscoveryAgent) (are: AutoResetEvent) =
    are.Set() |> ignore
    let status = ServiceStatus.Stopping
    status |> DiscoveryEvent.Status |> Msg.Notify |> agent.Post
    { state with Status = status }

  // ** handleStart

  let private handleStart (state: DiscoveryState) (agent: DiscoveryAgent) =
    let status = ServiceStatus.Running
    status |> DiscoveryEvent.Status |> Msg.Notify |> agent.Post
    agent.Post Msg.UnRegister
    { state with Status = status }

  // ** handleRegister

  let private handleRegister state agent project =
    Option.iter dispose state.RegisterService
    let machine = state.Machine
    let discoverable =
      { Id = state.Machine.MachineId
        WebPort = state.Machine.WebPort
        Status = Busy(Project.id project, Project.name project)
        Services =
          [| { ServiceType = ServiceType.Api;       Port = machine.ApiPort  }
             { ServiceType = ServiceType.Git;       Port = machine.GitPort  }
             { ServiceType = ServiceType.Raft;      Port = machine.RaftPort }
             { ServiceType = ServiceType.Http;      Port = machine.WebPort  }
             { ServiceType = ServiceType.WebSocket; Port = machine.WsPort   } |]
        ExtraMetadata = Array.empty }
    match registerService agent discoverable with
    | Right service -> { state with RegisterService = Some service }
    | Left error ->
      error
      |> sprintf "error registering busy service: %O"
      |> Logger.err (tag "handleRegister")
      state

  // ** handleUnRegister

  let private handleUnRegister (state: DiscoveryState) (agent: DiscoveryAgent) =
    Option.iter dispose state.RegisterService
    let machine = state.Machine
    let discoverable =
      { Id = state.Machine.MachineId
        WebPort = state.Machine.WebPort
        Status = Idle
        Services =
          [| { ServiceType = ServiceType.Api;       Port = machine.ApiPort  }
             { ServiceType = ServiceType.Git;       Port = machine.GitPort  }
             { ServiceType = ServiceType.Raft;      Port = machine.RaftPort }
             { ServiceType = ServiceType.Http;      Port = machine.WebPort  }
             { ServiceType = ServiceType.WebSocket; Port = machine.WsPort   } |]
        ExtraMetadata = Array.empty }
    match registerService agent discoverable with
    | Right service -> { state with RegisterService = Some service }
    | Left error ->
      error
      |> sprintf "error registering idle service: %O"
      |> Logger.err (tag "handleUnRegister")
      state

  // ** handleRegisterErr

  let private handleRegisterErr state (error: string) =
    Logger.err (tag "handleRegisterErr") error
    match state.RegisterService with
    | None -> state
    | Some registered ->
      dispose registered
      { state with RegisterService = None }

  // ** handleDiscovery

  let private handleDiscovery (state: DiscoveryState)
                              (agent: DiscoveryAgent)
                              (srvc: DiscoveredService) =
    match Map.tryFind srvc.Id state.ResolvedServices with
    | Some service ->
      let updated = Discovery.mergeDiscovered service srvc
      updated |> DiscoveryEvent.Updated |> Msg.Notify |> agent.Post
      { state with ResolvedServices = Map.add updated.Id updated state.ResolvedServices }
    | None ->
      srvc |> DiscoveryEvent.Appeared |> Msg.Notify |> agent.Post
      { state with ResolvedServices = Map.add srvc.Id srvc state.ResolvedServices }

  // ** handleVanishing

  let private handleVanishing (state: DiscoveryState) (agent: DiscoveryAgent) (id: ServiceId) =
    match Map.tryFind id state.ResolvedServices with
    | None -> state
    | Some service ->
      service |> DiscoveryEvent.Vanished |> Msg.Notify |> agent.Post
      { state with ResolvedServices = Map.remove id state.ResolvedServices }

  // ** loop

  let private loop (store: IAgentStore<DiscoveryState>) inbox msg =
    async {
      let state = store.State
      let newstate =
        match msg with
        | Msg.Stop              are  -> handleStop        state inbox are
        | Msg.Start                  -> handleStart       state inbox
        | Msg.Register      project  -> handleRegister    state inbox project
        | Msg.UnRegister             -> handleUnRegister  state inbox
        | Msg.RegisterErr (error,_)  -> handleRegisterErr state error
        | Msg.Discovered srvc        -> handleDiscovery   state inbox srvc
        | Msg.Vanished id            -> handleVanishing   state inbox id
        | Msg.Notify ev              -> handleNotify      state       ev
      store.Update newstate
      /// if Service.isStopping newstate.Status then
      ///   printfn "DiscoveryService"
      /// else
    }

  // ** startBrowser

  let private startBrowser (browser:ServiceBrowser) =
    browser.Browse(0u, AddressProtocol.IPv4, ZEROCONF_TCP_SERVICE, "local")

  // ** create

  let create (config: DiscoMachine) =
    let source = new CancellationTokenSource()

    let state = {
      Status = ServiceStatus.Stopped
      Machine = config
      Browser = Unchecked.defaultof<ServiceBrowser>
      Subscriptions = Subscriptions()
      RegisterService = None
      ResolvedServices = Map.empty
    }

    let store = AgentStore.create()
    let agent = Actor.create (loop store)
    agent.Start()

    { new IDiscoveryService with
        member self.Start() =
          either {
            let! browser = makeBrowser agent
            store.Update { state with Browser = browser }

            do! try
                  browser
                  |> startBrowser
                  |> Either.succeed
                with
                  | exn ->
                    tryDispose browser ignore
                    source.Cancel()
                    dispose source
                    dispose agent
                    let msg = exn.Message |> sprintf "error starting browser: %s"
                    Logger.err (tag "Start") msg
                    msg
                    |> Error.asOther (tag "Start")
                    |> Either.fail

            agent.Post Msg.Start
            return ()
          }

        member self.Services
          with get () = store.State.ResolvedServices

        member self.Subscribe (callback: DiscoveryEvent -> unit) =
          Observable.subscribe callback store.State.Subscriptions

        member self.Register (project:DiscoProject) =
          project |> Msg.Register |> agent.Post

        member self.UnRegister () =
          Msg.UnRegister |> agent.Post

        member self.Dispose() =
          if not (Service.isDisposed store.State.Status) then
            use are = new AutoResetEvent(false)
            are |> Msg.Stop |> agent.Post
            if not (are.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
              Logger.debug (tag "Dispose") "timeout: attempt to dispose discovery service failed"
            dispose store.State
            source.Cancel()
            dispose source
            dispose agent
            store.Update { state with Status = ServiceStatus.Disposed }
      }
