module Iris.Core.Discovery

open Iris.Core

// * ServiceType

[<RequireQualifiedAccess>]
type ServiceType =
  | Iris
  | Git
  | Raft
  | Http
  | WebSocket
  | Other of string

  override self.ToString() =
    match self with
    | Iris      -> "iris"
    | Git       -> "git"
    | Raft      -> "raft"
    | Http      -> "http"
    | WebSocket -> "ws"
    | Other str -> str

// * DiscoverableService

type DiscoverableService =
  { Id: Id
    Port: Port
    Name: string
    Type: ServiceType
    IpAddress: IpAddress
    Metadata: Map<string, string> }

// * DiscoveredService

type DiscoveredService =
  { Id: Id
    Machine: Id
    Port: Port
    Name: string
    FullName: string
    Type: ServiceType
    HostName: string
    HostTarget: string
    Aliases: string array
    Protocol: IPProtocol
    AddressList: IpAddress array
    Metadata: Map<string, string> }

// * DiscoveryEvent

type DiscoveryEvent =
  | Registering  of DiscoverableService
  | UnRegistered of DiscoverableService
  | Registered   of DiscoverableService
  | Appeared     of DiscoveredService
  | Updated      of DiscoveredService
  | Vanished     of DiscoveredService
