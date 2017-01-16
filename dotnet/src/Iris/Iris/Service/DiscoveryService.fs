namespace Iris.Service

// * Imports

open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Utilities
open Iris.Service.Interfaces

open System
open System.IO
open System.Text
open System.Threading
open System.Diagnostics
open System.Management
open System.Text.RegularExpressions
open Microsoft.FSharp.Control
open FSharpx.Functional

open Mono.Zeroconf


// * Discovery module

//  ____  _
// |  _ \(_)___  ___ _____   _____ _ __ _   _
// | | | | / __|/ __/ _ \ \ / / _ \ '__| | | |
// | |_| | \__ \ (_| (_) \ V /  __/ |  | |_| |
// |____/|_|___/\___\___/ \_/ \___|_|   \__, |
//                                      |___/

[<AutoOpen>]
module Discovery =

  // ** tag

  let private tag (str: string) = sprintf "DiscoveryService.%s" str

  // ** Listener

  type private Listener = IObservable<DiscoveryEvent>

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<DiscoveryEvent>>

  // ** DiscoveryStateData

  [<NoEquality;NoComparison>]
  type private DiscoveryStateData =
    { Browser: ServiceBrowser
      RegisteredServices: Map<Id,RegisterService>
      ResolvedServices: Map<Id,DiscoveredService> }

  // ** DiscoveryState

  [<NoEquality;NoComparison>]
  type private DiscoveryState =
    | Loaded of DiscoveryStateData
    | Idle

  // ** Reply

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Reply  =
    | Services of Map<Id,RegisterService> * Map<Id,DiscoveredService>
    | Ok

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Register    of chan:ReplyChan * srvc:DiscoverableService
    | UnRegister  of chan:ReplyChan * srvc:DiscoverableService
    | RegisterErr of err:string * srvc:DiscoverableService
    | Services    of chan:ReplyChan
    | Start       of chan:ReplyChan
    | Stop        of chan:ReplyChan
    | Discovered  of srvc:DiscoveredService
    | Vanished    of id:Id

  // ** DiscoveryAgent

  type private DiscoveryAgent = MailboxProcessor<Msg>

  // ** createListener

  let private createListener (subscriptions: Subscriptions) =
    { new Listener with
        member self.Subscribe(obs) =
          lock subscriptions <| fun _ ->
            subscriptions.Add obs

          { new IDisposable with
              member self.Dispose() =
                lock subscriptions <| fun _ ->
                  subscriptions.Remove obs
                  |> ignore } }

  // ** notify

  let private notify (subscriptions: Subscriptions) (ev: DiscoveryEvent) =
    for subscription in subscriptions do
      subscription.OnNext ev

  // ** createId

  let private createId (id: Id) (port: Port) (tipe: ServiceType) (ip: IpAddress) =
    sprintf "%s%s%s%d" (string id) (string tipe) (string ip) port
    |> Encoding.ASCII.GetBytes
    |> Crypto.sha1sum
    |> Id

  // ** serviceName

  let private serviceName (id: Id) (tipe: ServiceType) =
    match tipe with
    | ServiceType.Git       -> sprintf "Git Service [%s]" (string id)
    | ServiceType.Raft      -> sprintf "SRaft Service [%s]" (string id)
    | ServiceType.Http      -> sprintf "Http Service [%s]" (string id)
    | ServiceType.WebSocket -> sprintf "WebSocket Service [%s]" (string id)
    | ServiceType.Other str -> sprintf "%s Service [%s]" str (string id)

  // ** parseServiceId

  let private parseServiceId (name: string) =
    let m = Regex.Match(name, "^.*\[(.*)\]$")
    if m.Success then
      Id m.Groups.[1].Value
    else
      Id "<empty>"

  // ** parseServiceType

  let private parseServiceType (txt: ITxtRecord) =
    try
      let item = txt.["type"]
      match item.ValueString with
      | "git"  -> ServiceType.Git
      | "raft" -> ServiceType.Raft
      | "http" -> ServiceType.Http
      | "ws"   -> ServiceType.WebSocket
      | null      -> ServiceType.Other "<null>"
      | other  -> ServiceType.Other other
    with
      | _ -> ServiceType.Other "<field missing>"

  // ** parseMachine

  let private parseMachine (txt: ITxtRecord) =
    try
      let item = txt.["machine"]
      match item.ValueString with
      | null  -> Id "<field empty>"
      | id -> Id id
    with
      | _ -> Id "<field missing>"

  // ** parseProtocol

  let private parseProtocol (proto: AddressProtocol) =
    match proto with
    | AddressProtocol.IPv6 -> IPv6
    |                    _ -> IPv4

  // ** toDiscoveredService

  let private toDiscoveredService (service: IResolvableService) =
    let entry = service.HostEntry
    let proto = parseProtocol service.AddressProtocol
    let addresses =
      if isNull entry then
        [| |]
      else
        Array.map IpAddress.ofIPAddress entry.AddressList

    let id = parseServiceId service.Name

    { Id = id
      Protocol = proto
      Port = uint16 service.Port
      Name = service.Name
      FullName = service.FullName
      Machine = parseMachine service.TxtRecord
      Type = parseServiceType service.TxtRecord
      HostName = if isNull entry then "" else entry.HostName
      HostTarget = service.HostTarget
      Aliases = if isNull entry then [| |] else entry.Aliases
      AddressList = addresses }

  // ** mergeDiscovered

  let private mergeDiscovered (have: DiscoveredService) (got: DiscoveredService) =
    { have with AddressList = Array.append have.AddressList got.AddressList }

  // ** addResolved

  let private addResolved (agent: DiscoveryAgent) (o: obj) (args: ServiceResolvedEventArgs) =
    o :?> IResolvableService
    |> toDiscoveredService
    |> Msg.Discovered
    |> agent.Post

  // ** serviceAdded

  let private serviceAdded (agent: DiscoveryAgent) (obj: obj) (args: ServiceBrowseEventArgs) =
    args.Service.Resolved.AddHandler(new ServiceResolvedEventHandler(addResolved agent))
    args.Service.Resolve()

  // ** serviceRemoved

  let private serviceRemoved (agent: DiscoveryAgent) (obj: obj) (args: ServiceBrowseEventArgs) =
    parseServiceId args.Service.Name
    |> Msg.Vanished
    |> agent.Post

  // ** serviceRegistered

  let private serviceRegistered (subs: Subscriptions)
                                (agent: DiscoveryAgent)
                                (disco: DiscoverableService)
                                (o: obj)
                                (args: RegisterServiceEventArgs) =
    match args.ServiceError with
    | ServiceErrorCode.None ->
      notify subs (DiscoveryEvent.Registered disco)
    | ServiceErrorCode.NameConflict ->
      let err = sprintf "Error: Name-Collision! '%s' is already registered" args.Service.Name
      agent.Post(Msg.RegisterErr(err,disco))
    | x ->
      let err = sprintf "Error (%A) registering name = '%s'" x args.Service.Name
      agent.Post(Msg.RegisterErr(err,disco))

  // ** registerService

  let private registerService (subs: Subscriptions)
                              (agent: DiscoveryAgent)
                              (config: IrisMachine)
                              (disco: DiscoverableService) =
    let service = new RegisterService()
    service.Name <- disco.Name
    service.RegType <- ZEROCONF_TCP_SERVICE
    service.ReplyDomain <- "local."
    service.Port <- int16 disco.Port

    let record = new TxtRecord()

    record.Add("type", string disco.Type)
    record.Add("machine", string config.MachineId)

    service.TxtRecord <- record
    let handler = new RegisterServiceEventHandler(serviceRegistered subs agent disco)
    service.Response.AddHandler(handler)

    service.Register()
    service

  // ** unregisterService

  let private unregisterService (services: RegisterService list)
                                (srvc: DiscoverableService)
                                (subs: Subscriptions) =
    for service in services do
      if service.Name = srvc.Name then
        dispose service

  // ** startBrowser

  let private startBrowser (agent: DiscoveryAgent) =
    let browser = new ServiceBrowser()
    browser.ServiceAdded.AddHandler(new ServiceBrowseEventHandler(serviceAdded agent))
    browser.ServiceRemoved.AddHandler(new ServiceBrowseEventHandler(serviceRemoved agent))
    browser.Browse(0u, AddressProtocol.IPv4, ZEROCONF_TCP_SERVICE, "local")
    browser

  // ** handleGetServices

  let private handleGetServices (chan: ReplyChan) (state: DiscoveryState) =
    match state with
    | Loaded data ->
      chan.Reply (Right (Reply.Services (data.RegisteredServices, data.ResolvedServices)))
      state
    | Idle ->
      chan.Reply (Left (Error.asGitError (tag "loop") "Not loaded"))
      state

  // ** handleStop

  let private handleStop (chan: ReplyChan) (state: DiscoveryState) (subs: Subscriptions) =
    match state with
    | Loaded data ->
      dispose data.Browser
      chan.Reply (Right (Reply.Ok))
      Idle
    | Idle ->
      chan.Reply (Left (Error.asGitError (tag "loop") "Not loaded"))
      state

  // ** handleStart

  let private handleStart (chan: ReplyChan)
                          (state: DiscoveryState)
                          (subs: Subscriptions)
                          (agent: DiscoveryAgent) =
    match state with
    | Loaded data ->
      dispose data.Browser
      chan.Reply (Left (Error.asGitError (tag "loop") "Already running"))
      state
    | Idle ->
      let browser = startBrowser agent
      chan.Reply (Right Reply.Ok)
      Loaded { RegisteredServices = Map.empty
               ResolvedServices = Map.empty
               Browser = browser }

  // ** handleRegister

  let private handleRegister (chan: ReplyChan)
                             (state: DiscoveryState)
                             (subs: Subscriptions)
                             (agent: DiscoveryAgent)
                             (config: IrisMachine)
                             (srvc: DiscoverableService) =
    match state with
    | Loaded data ->
      match Map.tryFind srvc.Id data.RegisteredServices with
      | Some registered ->
        dispose registered
        notify subs (DiscoveryEvent.UnRegistered srvc)
        let service = registerService subs agent config srvc
        chan.Reply(Right Reply.Ok)
        notify subs (DiscoveryEvent.Registering srvc)
        Loaded { data with RegisteredServices = Map.add srvc.Id service data.RegisteredServices }
      | None ->
        let service = registerService subs agent config srvc
        chan.Reply(Right Reply.Ok)
        notify subs (DiscoveryEvent.Registering srvc)
        Loaded { data with RegisteredServices = Map.add srvc.Id service data.RegisteredServices }
    | Idle ->
      chan.Reply(Right Reply.Ok)
      state

  // ** handleUnRegister

  let private handleUnRegister (chan: ReplyChan)
                               (state: DiscoveryState)
                               (subs: Subscriptions)
                               (srvc: DiscoverableService) =
    match state with
    | Loaded data ->
      match Map.tryFind srvc.Id data.RegisteredServices with
      | Some registered ->
        dispose registered
        notify subs (DiscoveryEvent.UnRegistered srvc)
        chan.Reply(Right Reply.Ok)
        Loaded { data with RegisteredServices = Map.remove srvc.Id data.RegisteredServices }
      | None ->
        chan.Reply(Right Reply.Ok)
        state
    | Idle ->
      chan.Reply(Right Reply.Ok)
      state

  // ** handleRegisterErr

  let private handleRegisterErr (state: DiscoveryState)
                                (subs: Subscriptions)
                                (err: string)
                                (srvc: DiscoverableService) =
    match state with
    | Loaded data ->
      match Map.tryFind srvc.Id data.RegisteredServices with
      | Some registered ->
        dispose registered
        notify subs (DiscoveryEvent.UnRegistered srvc)
        Loaded { data with RegisteredServices = Map.remove srvc.Id data.RegisteredServices }
      | None -> state
    | Idle -> Idle

  // ** handleDiscovery

  let private handleDiscovery (state: DiscoveryState)
                              (subs: Subscriptions)
                              (srvc: DiscoveredService) =
    match state with
    | Loaded data ->
      match Map.tryFind srvc.Id data.ResolvedServices with
      | Some service ->
        let updated = mergeDiscovered service srvc
        notify subs (DiscoveryEvent.Updated updated)
        Loaded { data with ResolvedServices = Map.add updated.Id updated data.ResolvedServices }
      | None ->
        notify subs (DiscoveryEvent.Appeared srvc)
        Loaded { data with ResolvedServices = Map.add srvc.Id srvc data.ResolvedServices }
    | Idle -> state

  // ** handleVanishing

  let private handleVanishing (state: DiscoveryState) (subs: Subscriptions) (id: Id) =
    match state with
    | Loaded data ->
      match Map.tryFind id data.ResolvedServices with
      | Some service ->
        notify subs (DiscoveryEvent.Vanished service)
        Loaded { data with ResolvedServices = Map.remove id data.ResolvedServices }
      | None -> state
    | Idle -> state

  // ** loop

  let private loop (initial: DiscoveryState)
                   (subscriptions: Subscriptions)
                   (config: IrisMachine)
                   (inbox: DiscoveryAgent) =
    let rec act (state: DiscoveryState) =
      async {
        let! msg = inbox.Receive()

        let newstate =
          match msg with
          | Msg.Services chan          -> handleGetServices chan state
          | Msg.Stop chan              -> handleStop chan state subscriptions
          | Msg.Start chan             -> handleStart chan state subscriptions inbox
          | Msg.Register (chan,srvc)   -> handleRegister chan state subscriptions inbox config srvc
          | Msg.UnRegister (chan,srvc) -> handleUnRegister chan state subscriptions srvc
          | Msg.RegisterErr (err,srvc) -> handleRegisterErr state subscriptions err srvc
          | Msg.Discovered srvc        -> handleDiscovery state subscriptions srvc
          | Msg.Vanished id            -> handleVanishing state subscriptions id

        return! act newstate
      }

    act initial

  [<RequireQualifiedAccess>]
  module DiscoveryService =

    let create (config: IrisMachine) =
      try
        let source = new CancellationTokenSource()
        let subscriptions = new Subscriptions()
        let listener = createListener subscriptions
        let agent = DiscoveryAgent.Start(loop Idle subscriptions config, source.Token)

        Either.succeed
          { new IDiscoveryService with
              member self.Start() =
                match agent.PostAndReply(fun chan -> Msg.Start chan) with
                | Right Reply.Ok -> Either.succeed ()
                | Right other ->
                  sprintf "Unexpected reply type from DiscoveryAgent: %A" other
                  |> Error.asGitError (tag "Start")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Services
                with get () =
                  match agent.PostAndReply(fun chan -> Msg.Services chan) with
                  | Right (Reply.Services (reg,res)) -> Either.succeed (reg,res)
                  | Right other ->
                    sprintf "Unexpected reply type from DiscoveryAgent: %A" other
                    |> Error.asGitError (tag "Start")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.Subscribe (callback: DiscoveryEvent -> unit) =
                { new IObserver<DiscoveryEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Register (tipe: ServiceType) (port: Port) (addr: IpAddress) =
                let id = createId config.MachineId port tipe addr

                let service =
                  { Id = id
                    Port = port
                    Name = serviceName id tipe
                    Type = tipe
                    IpAddress = addr }

                match agent.PostAndReply(fun chan -> Msg.Register(chan, service)) with
                | Right Reply.Ok ->
                  { new IDisposable with
                      member self.Dispose () =
                        agent.PostAndReply(fun chan -> Msg.UnRegister(chan, service))
                        |> ignore }
                  |> Either.succeed
                | Right other ->
                  sprintf "Unexpected reply type from DiscoveryAgent: %A" other
                  |> Error.asGitError (tag "Register")
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Dispose() =
                lock subscriptions <| fun _ ->
                  subscriptions.Clear()

                agent.PostAndReply(fun chan -> Msg.Stop chan) |> ignore

                dispose agent
            }
      with
        | exn ->
          sprintf "Exception starting the DiscoveryService: %s" exn.Message
          |> Error.asGitError (tag "create")
          |> Either.fail
