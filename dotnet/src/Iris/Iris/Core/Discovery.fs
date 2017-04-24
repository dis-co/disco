module Iris.Core.Discovery

#if FABLE_COMPILER

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization

#endif

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

  member service.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string service.Id)
    let machine = builder.CreateString (string service.Machine)
    let name = builder.CreateString service.Name
    let fullname = builder.CreateString service.FullName
    let hostname = builder.CreateString service.HostName
    let hosttarget = builder.CreateString service.HostTarget

    let typ =
      match service.Type with
      | ServiceType.Iris -> "Iris"
      | ServiceType.Git -> "Git"
      | ServiceType.Raft -> "Raft"
      | ServiceType.Http -> "Http"
      | ServiceType.WebSocket -> "WebSocket"
      | ServiceType.Other name -> name
      |> builder.CreateString

    let protocol =
      match service.Protocol with
      | IPProtocol.IPv4 -> "IPv4"
      | IPProtocol.IPv6 -> "IPv6"
      |> builder.CreateString

    let aliases =
      (builder, Array.map builder.CreateString service.Aliases)
      |> DiscoveredServiceFB.CreateAliasesVector

    let addressList =
      (builder, Array.map (string >> builder.CreateString) service.AddressList)
      |> DiscoveredServiceFB.CreateAddressListVector

    let metadata =
      (builder, service.Metadata |> Seq.map (fun kv -> kv.Key + ":" + kv.Value |> builder.CreateString) |> Seq.toArray)
      |> DiscoveredServiceFB.CreateMetadataVector

    DiscoveredServiceFB.StartDiscoveredServiceFB(builder)
    DiscoveredServiceFB.AddId(builder, id)
    DiscoveredServiceFB.AddMachine(builder, machine)
    DiscoveredServiceFB.AddPort(builder, unwrap service.Port)
    DiscoveredServiceFB.AddName(builder, name)
    DiscoveredServiceFB.AddFullName(builder, fullname)
    DiscoveredServiceFB.AddType(builder, typ)
    DiscoveredServiceFB.AddHostName(builder, hostname)
    DiscoveredServiceFB.AddHostTarget(builder, hosttarget)
    DiscoveredServiceFB.AddAliases(builder, aliases)
    DiscoveredServiceFB.AddProtocol(builder, protocol)
    DiscoveredServiceFB.AddAddressList(builder, addressList)
    DiscoveredServiceFB.AddMetadata(builder, metadata)
    DiscoveredServiceFB.EndDiscoveredServiceFB(builder)

  static member FromFB(fb: DiscoveredServiceFB) =
    either {
      let! protocol =
        match fb.Type with
        | "IPv4" -> IPProtocol.IPv4 |> Right
        | "IPv6" -> IPProtocol.IPv6 |> Right
        | other  -> Other("Unknown protocol: " + other, "Discovery.fromFB") |> Left

      let! metadata =
        try
          seq { for i = 0 to fb.MetadataLength do yield fb.Metadata(i) }
          |> Seq.map (fun s -> let parts = s.Split(':') in parts.[0], parts.[1])
          |> Map |> Right
        with ex ->
          IrisError.Other(ex.Message, "Discovery.fromFB.getMetadata") |> Left

      let! revAddressList =
        let mutable ls = Right []
        for i = 0 to fb.AddressListLength do
          match ls with
          | Right addresses ->
            match fb.AddressList(i) |> IpAddress.TryParse with
            | Right address -> ls <- Right <| address::addresses
            | Left err -> ls <- Left err
          | Left _ -> ()
        ls

      let aliases =
        [| for i = 0 to fb.AliasesLength do yield fb.Aliases(i) |]

      let typ =
        match fb.Type with
        | "Git" -> ServiceType.Git
        | "Http" -> ServiceType.Http
        | "Iris" -> ServiceType.Iris
        | "Raft" -> ServiceType.Raft
        | "WebSocket" -> ServiceType.WebSocket
        | other -> ServiceType.Other other

      return
        { Id           = Id fb.Id
          Machine      = Id fb.Machine
          Port         = port fb.Port
          Name         = fb.Name
          FullName     = fb.FullName
          Type         = typ
          HostName     = fb.HostName
          HostTarget   = fb.HostTarget
          Aliases      = aliases
          Protocol     = protocol
          AddressList  = Seq.rev revAddressList |> Seq.toArray
          Metadata     = metadata }
    }

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte[]) =
    IrisClientFB.GetRootAsIrisClientFB(Binary.createBuffer raw)
    |> IrisClient.FromFB

// * DiscoveryEvent

type DiscoveryEvent =
  | Registering  of DiscoverableService
  | UnRegistered of DiscoverableService
  | Registered   of DiscoverableService
  | Appeared     of DiscoveredService
  | Updated      of DiscoveredService
  | Vanished     of DiscoveredService
