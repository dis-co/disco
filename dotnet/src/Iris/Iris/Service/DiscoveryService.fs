namespace Iris.Service

// * Imports

open Iris.Core
open Iris.Core.Utils
open Iris.Service.Interfaces

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
  open Iris.Core.Discovery

  // ** tag

  let private tag (str: string) = String.Format("DiscoveryService.{0}", str)

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<DiscoveryEvent>

  // ** DiscoveryState

  [<NoEquality;NoComparison>]
  type private DiscoveryState =
    { Status: ServiceStatus
      Machine: IrisMachine
      Browser: ServiceBrowser
      Subscriptions: Subscriptions
      RegisteredServices: Map<Id,RegisterService>
      ResolvedServices: Map<Id,DiscoveredService> }

    interface IDisposable with
      member self.Dispose() =
        Map.iter (fun _ service -> dispose service) self.RegisteredServices
        dispose self.Browser
        self.Subscriptions.Clear()

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Start
    | Stop        of AutoResetEvent
    | Notify      of DiscoveryEvent
    | Register    of srvc:DiscoverableService
    | UnRegister  of srvc:DiscoverableService
    | RegisterErr of err:string * srvc:DiscoverableService
    | Discovered  of srvc:DiscoveredService
    | Vanished    of id:Id

  // ** DiscoveryAgent

  type private DiscoveryAgent = MailboxProcessor<Msg>

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
    args.Service.Resolved.AddHandler(new ServiceResolvedEventHandler(addResolved agent))
    args.Service.Resolve()

  // ** serviceRemoved

  let private serviceRemoved (agent: DiscoveryAgent) (_: obj) (args: ServiceBrowseEventArgs) =
    match args.Service.Name with
    | ServiceId id -> id |> Msg.Vanished |> agent.Post
    | _ -> ()

  // ** serviceRegistered

  let private serviceRegistered (agent: DiscoveryAgent)
                                (disco: DiscoverableService)
                                (args: RegisterServiceEventArgs) =
    match args.ServiceError with
    | ServiceErrorCode.None ->
      disco |> DiscoveryEvent.Registered |> Msg.Notify |> agent.Post
    | ServiceErrorCode.NameConflict ->
      let err = sprintf "Error: Name-Collision! '%s' is already registered" args.Service.Name
      agent.Post(Msg.RegisterErr(err,disco))
    | x ->
      let err = sprintf "Error (%A) registering name = '%s'" x args.Service.Name
      agent.Post(Msg.RegisterErr(err,disco))

  // ** registerService

  let private registerService (agent: DiscoveryAgent) (disco: DiscoverableService) =
    try
      let service = Discovery.toDiscoverableService disco
      service.Response.Add(serviceRegistered agent disco)
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
    { state with Status = status }

  // ** handleRegister

  let private handleRegister (state: DiscoveryState)
                             (agent: DiscoveryAgent)
                             (srvc: DiscoverableService) =
    match Map.tryFind srvc.Id state.RegisteredServices with
    | Some registered ->
      dispose registered
      srvc |> DiscoveryEvent.UnRegistered |> Msg.Notify |> agent.Post
      match registerService agent srvc with
      | Right service ->
        srvc |> DiscoveryEvent.Registering |> Msg.Notify |> agent.Post
        { state with RegisteredServices = Map.add srvc.Id service state.RegisteredServices }
      | Left error ->
        error
        |> sprintf "error registering new service: %O"
        |> Logger.err (tag "handleRegister")
        state
    | None ->
      match registerService agent srvc with
      | Right service ->
        srvc |> DiscoveryEvent.Registering |> Msg.Notify |> agent.Post
        { state with RegisteredServices = Map.add srvc.Id service state.RegisteredServices }
      | Left error ->
        error
        |> sprintf "error registering new service: %O"
        |> Logger.err (tag "handleRegister")
        state

  // ** handleUnRegister

  let private handleUnRegister (state: DiscoveryState)
                               (agent: DiscoveryAgent)
                               (srvc: DiscoverableService) =
    match Map.tryFind srvc.Id state.RegisteredServices with
    | None -> state
    | Some registered ->
      dispose registered
      srvc |> DiscoveryEvent.UnRegistered |> Msg.Notify |> agent.Post
      { state with RegisteredServices = Map.remove srvc.Id state.RegisteredServices }

  // ** handleRegisterErr

  let private handleRegisterErr (state: DiscoveryState)
                                (agent: DiscoveryAgent)
                                (_: string)
                                (srvc: DiscoverableService) =
    match Map.tryFind srvc.Id state.RegisteredServices with
    | None -> state
    | Some registered ->
      dispose registered
      srvc |> DiscoveryEvent.UnRegistered |> Msg.Notify |> agent.Post
      { state with RegisteredServices = Map.remove srvc.Id state.RegisteredServices }

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

  let private handleVanishing (state: DiscoveryState) (agent: DiscoveryAgent) (id: Id) =
    match Map.tryFind id state.ResolvedServices with
    | None -> state
    | Some service ->
      service |> DiscoveryEvent.Vanished |> Msg.Notify |> agent.Post
      { state with ResolvedServices = Map.remove id state.ResolvedServices }

  // ** loop

  let private loop (store: IAgentStore<DiscoveryState>) (inbox: DiscoveryAgent) =
    let rec act () =
      async {
        let! msg = inbox.Receive()
        let state = store.State
        let newstate =
          match msg with
          | Msg.Stop            are    -> handleStop        state inbox are
          | Msg.Start                  -> handleStart       state inbox
          | Msg.Register       srvc    -> handleRegister    state inbox srvc
          | Msg.UnRegister       srvc  -> handleUnRegister  state inbox srvc
          | Msg.RegisterErr (err,srvc) -> handleRegisterErr state inbox err  srvc
          | Msg.Discovered srvc        -> handleDiscovery   state inbox srvc
          | Msg.Vanished id            -> handleVanishing   state inbox id
          | Msg.Notify ev              -> handleNotify      state       ev
        store.Update newstate
        if Service.isStopping newstate.Status then
          return ()
        else
          return! act ()
      }
    act ()

  // ** create

  let create (config: IrisMachine) =
    let source = new CancellationTokenSource()

    let state = {
      Status = ServiceStatus.Stopped
      Machine = config
      Browser = Unchecked.defaultof<ServiceBrowser>
      Subscriptions = Subscriptions()
      RegisteredServices = Map.empty
      ResolvedServices = Map.empty
    }

    let store = AgentStore.create()

    let agent = DiscoveryAgent.Start(loop store, source.Token)

    { new IDiscoveryService with
        member self.Start() = either {
            let! browser = makeBrowser agent
            store.Update { state with Browser = browser }

            do! try
                  browser.Browse(0u, AddressProtocol.IPv4, ZEROCONF_TCP_SERVICE, "local")
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
          with get () = store.State.RegisteredServices, store.State.ResolvedServices

        member self.Subscribe (callback: DiscoveryEvent -> unit) =
          Observable.subscribe callback store.State.Subscriptions

        member self.Register (service: DiscoverableService) =
          service |> Msg.Register |> agent.Post
          { new IDisposable with
              member self.Dispose () =
                service |> Msg.UnRegister |> agent.Post }

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
