namespace Iris.Service

// * Imports

open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Utilities
open Iris.Service.Interfaces

open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Management
open System.Text.RegularExpressions
open Microsoft.FSharp.Control
open FSharpx.Functional

open Mono.Zeroconf

// * ServiceType

[<RequireQualifiedAccess>]
type ServiceType =
  | Git
  | Raft
  | Http
  | WebSocket

// * DiscoverableService

type DiscoverableService =
  { Port: Port
    Name: string
    Type: ServiceType
    IpAddress: IpAddress }

// * DiscoveryEvent

type DiscoveryEvent =
  | Registering of DiscoverableService
  | Registered  of DiscoverableService
  | Appeared    of DiscoverableService
  | Vanished    of DiscoverableService

// * Discovery module

//  ____  _
// |  _ \(_)___  ___ _____   _____ _ __ _   _
// | | | | / __|/ __/ _ \ \ / / _ \ '__| | | |
// | |_| | \__ \ (_| (_) \ V /  __/ |  | |_| |
// |____/|_|___/\___\___/ \_/ \___|_|   \__, |
//                                      |___/

module Discovery =

  type private OnServiceEvent =
    delegate of (obj * ServiceBrowseEventArgs) -> unit

  type private OnResolveEvent =
    delegate of (obj * ServiceResolvedEventArgs) -> unit

  type private OnRegisteredEvent =
    delegate of (obj * RegisterServiceEventArgs) -> unit

  let private tag (str: string) = sprintf "DiscoveryService.%s" str

  // ** Listener

  type private Listener = IObservable<GitEvent>

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<GitEvent>>

  // ** serviceType

  let private serviceType (tipe: ServiceType) =
    match tipe with
    | ServiceType.Git       -> "_Iris_GitDaemon._tcp"
    | ServiceType.Raft      -> "_Iris_RaftServer._tcp"
    | ServiceType.Http      -> "_Iris_Web._tcp"
    | ServiceType.WebSocket -> "_Iris_WebSocket._tcp"

  // ** serviceDescription

  let private serviceDescription (tipe: ServiceType) =
    match tipe with
    | ServiceType.Git       -> "Git Daemon"
    | ServiceType.Raft      -> "Raft Service"
    | ServiceType.Http      -> "Web Service"
    | ServiceType.WebSocket -> "WebSocket Service"

  // ** serviceAdded

  let private serviceAdded (obj: obj, args: ServiceBrowseEventArgs) =
    printfn "Found: name = '%s', type = '%s', domain = '%s'"
      args.Service.Name
      args.Service.RegType
      args.Service.ReplyDomain

  // ** serviceRemoved

  let private serviceRemoved (obj: obj, args: ServiceBrowseEventArgs) =
    printfn "Disappeared: name = '%s', type = '%s', domain = '%s'"
      args.Service.Name
      args.Service.RegType
      args.Service.ReplyDomain

  // ** serviceResolved

  let private serviceResolved (o: obj, args: ServiceResolvedEventArgs) =
    let service = o :?> IResolvableService
    printfn "Resolved: name = '%s', host ip = '%A', hostname = '%s', port = '%d', iface = '%A', type = '%A'"
      service.FullName
      (service.HostEntry.AddressList.[0])
      service.HostEntry.HostName
      service.Port
      service.NetworkInterface
      service.AddressProtocol

  // ** serviceRegistered

  let private serviceRegistered (o: obj) (args: RegisterServiceEventArgs) =
    match args.ServiceError with
    | ServiceErrorCode.NameConflict ->
      printfn "Error: Name-Collision! '%s' is already registered" args.Service.Name
    | ServiceErrorCode.None ->
      printfn "Registerd name = '%s'" args.Service.Name
    | _ ->
      printfn "Error registering name = '%s'" args.Service.Name

  // ** registerService

  let private registerService (disco: DiscoverableService) =
    let service = new RegisterService()
    service.Name <- disco.Name
    service.RegType <- serviceType disco.Type
    service.ReplyDomain <- "local."
    service.Port <- int16 disco.Port

    let record = new TxtRecord()

    service.TxtRecord <- record
    service.Response.AddHandler(new RegisterServiceEventHandler(serviceRegistered))

    service.Register()
