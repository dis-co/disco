namespace Iris.Core

// * Imports

open Iris.Core
open System
open FlatBuffers
open Iris.Serialization

// * Role

[<RequireQualifiedAccess>]
type Role =
  | Renderer

  member role.ToOffset(builder: FlatBufferBuilder) =
    match role with
    | Renderer -> RoleFB.RendererFB

  static member FromFB(fb: RoleFB) =
    match fb with
    | RoleFB.RendererFB -> Either.succeed Renderer
    | x ->
      sprintf "Unknown RoleFB value: %A" x
      |> Error.asClientError "Role.FromFB"
      |> Either.fail

// * IrisClient

//  ___      _      ____ _ _            _
// |_ _|_ __(_)___ / ___| (_) ___ _ __ | |_
//  | || '__| / __| |   | | |/ _ \ '_ \| __|
//  | || |  | \__ \ |___| | |  __/ | | | |_
// |___|_|  |_|___/\____|_|_|\___|_| |_|\__|

type IrisClient =
  { Id: Id
    Name: string
    Role: Role
    Status: ServiceStatus
    IpAddress: IpAddress
    Port: Port }

  member client.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string client.Id)
    let name = builder.CreateString client.Name
    let ip = builder.CreateString (string client.IpAddress)
    let role = client.Role.ToOffset(builder)

    IrisClientFB.StartIrisClientFB(builder)
    IrisClientFB.AddId(builder, id)
    IrisClientFB.AddName(builder, name)
    IrisClientFB.AddRole(builder, role)
    IrisClientFB.AddIpAddress(builder, ip)
    IrisClientFB.AddPort(builder, uint32 client.Port)
    IrisClientFB.EndIrisClientFB(builder)

  static member FromFB(fb: IrisClientFB) =
    either {
      let! role = Role.FromFB fb.Role
      let! ip = IpAddress.TryParse fb.IpAddress
      return { Id = Id fb.Id
               Name = fb.Name
               Status = ServiceStatus.Running
               IpAddress = ip
               Port = uint16 fb.Port
               Role = role }
    }

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    IrisClientFB.GetRootAsIrisClientFB(Binary.createBuffer raw)
    |> IrisClient.FromFB
