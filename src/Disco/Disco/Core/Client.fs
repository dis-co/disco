(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

// * Imports

#if FABLE_COMPILER

open System
open Fable.Core
open Fable.Import
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open Disco.Core
open System
open FlatBuffers
open Disco.Serialization

#endif

// * Role

[<RequireQualifiedAccess>]
type Role =
  | Renderer

  override role.ToString () =
    match role with
    | Renderer -> "Renderer"

  member role.ToOffset(_: FlatBufferBuilder) =
    match role with
    | Renderer -> RoleFB.RendererFB

  static member FromFB(fb: RoleFB) =
    #if FABLE_COMPILER
    match fb with
    | x when x = RoleFB.RendererFB -> Result.succeed Renderer
    | x ->
      sprintf "Unknown RoleFB value: %A" x
      |> Error.asClientError "Role.FromFB"
      |> Result.fail
    #else
    match fb with
    | RoleFB.RendererFB -> Result.succeed Renderer
    | x ->
      sprintf "Unknown RoleFB value: %A" x
      |> Error.asClientError "Role.FromFB"
      |> Result.fail
    #endif

// * DiscoClient

//  ___      _      ____ _ _            _
// |_ _|_ __(_)___ / ___| (_) ___ _ __ | |_
//  | || '__| / __| |   | | |/ _ \ '_ \| __|
//  | || |  | \__ \ |___| | |  __/ | | | |_
// |___|_|  |_|___/\____|_|_|\___|_| |_|\__|

type DiscoClient =
  { Id: ClientId
    Name: Name
    Role: Role
    ServiceId: ServiceId
    Status: ServiceStatus
    IpAddress: IpAddress
    Port: Port }

  // ** ToOffset

  member client.ToOffset(builder: FlatBufferBuilder) =
    let id = DiscoClientFB.CreateIdVector(builder,client.Id.ToByteArray())
    let service = DiscoClientFB.CreateServiceIdVector(builder, client.ServiceId.ToByteArray())
    let name = Option.mapNull builder.CreateString (unwrap client.Name)
    let ip = builder.CreateString (string client.IpAddress)
    let role = client.Role.ToOffset(builder)
    let status = Binary.toOffset builder client.Status

    DiscoClientFB.StartDiscoClientFB(builder)
    DiscoClientFB.AddId(builder, id)
    DiscoClientFB.AddServiceId(builder, service)
    Option.iter (fun value -> DiscoClientFB.AddName(builder, value)) name
    DiscoClientFB.AddStatus(builder, status)
    DiscoClientFB.AddRole(builder, role)
    DiscoClientFB.AddIpAddress(builder, ip)
    DiscoClientFB.AddPort(builder, unwrap client.Port)
    DiscoClientFB.EndDiscoClientFB(builder)

  // ** FromFB

  static member FromFB(fb: DiscoClientFB) =
    result {
      let! id = Id.decodeId fb
      let! serviceId = Id.decodeServiceId fb
      let! role = Role.FromFB fb.Role
      let! ip = IpAddress.TryParse fb.IpAddress
      let! status =
        #if FABLE_COMPILER
        ServiceStatus.FromFB fb.Status
        #else
        let statusish = fb.Status
        if statusish.HasValue then
          let status = statusish.Value
          ServiceStatus.FromFB status
        else
          "could not parse empty status payload"
          |> Error.asParseError "DiscoClient.FromFB"
          |> Result.fail
        #endif
      return {
        Id        = id
        Name      = name fb.Name
        Status    = status
        IpAddress = ip
        ServiceId = serviceId
        Port      = port fb.Port
        Role      = role
      }
    }

  // ** ToBytes

  member request.ToBytes() =
    Binary.buildBuffer request

  // ** FromBytes

  static member FromBytes(raw: byte[]) =
    DiscoClientFB.GetRootAsDiscoClientFB(Binary.createBuffer raw)
    |> DiscoClient.FromFB
