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
    ServiceId: Id
    Status: ServiceStatus
    IpAddress: IpAddress
    Port: Port }

  // ** ToOffset

  member client.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string client.Id)
    let service = builder.CreateString (string client.ServiceId)
    let name = Option.mapNull builder.CreateString client.Name
    let ip = builder.CreateString (string client.IpAddress)
    let role = client.Role.ToOffset(builder)
    let status = Binary.toOffset builder client.Status

    IrisClientFB.StartIrisClientFB(builder)
    IrisClientFB.AddId(builder, id)
    IrisClientFB.AddServiceId(builder, service)
    Option.iter (fun value -> IrisClientFB.AddName(builder, value)) name
    IrisClientFB.AddStatus(builder, status)
    IrisClientFB.AddRole(builder, role)
    IrisClientFB.AddIpAddress(builder, ip)
    IrisClientFB.AddPort(builder, unwrap client.Port)
    IrisClientFB.EndIrisClientFB(builder)

  // ** FromFB

  static member FromFB(fb: IrisClientFB) =
    either {
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
          |> Error.asParseError "IrisClient.FromFB"
          |> Either.fail
        #endif
      return { Id        = Id fb.Id
               Name      = fb.Name
               Status    = status
               IpAddress = ip
               ServiceId = Id fb.ServiceId
               Port      = port fb.Port
               Role      = role }
    }

  // ** ToBytes

  member request.ToBytes() =
    Binary.buildBuffer request

  // ** FromBytes

  static member FromBytes(raw: byte[]) =
    IrisClientFB.GetRootAsIrisClientFB(Binary.createBuffer raw)
    |> IrisClient.FromFB
