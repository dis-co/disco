namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open System
open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open Iris.Core
open System
open FlatBuffers
open Iris.Serialization

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
    | x when x = RoleFB.RendererFB -> Either.succeed Renderer
    | x ->
      sprintf "Unknown RoleFB value: %A" x
      |> Error.asClientError "Role.FromFB"
      |> Either.fail
    #else
    match fb with
    | RoleFB.RendererFB -> Either.succeed Renderer
    | x ->
      sprintf "Unknown RoleFB value: %A" x
      |> Error.asClientError "Role.FromFB"
      |> Either.fail
    #endif

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
    IrisClientFB.AddPort(builder, client.Port)
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

  static member FromBytes(raw: Binary.Buffer) =
    IrisClientFB.GetRootAsIrisClientFB(Binary.createBuffer raw)
    |> IrisClient.FromFB
