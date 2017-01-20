namespace Iris.Client

// * Imports

open Iris.Core
open System
open System.Collections.Concurrent
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

// * IrisServer

//  ___      _     ____
// |_ _|_ __(_)___/ ___|  ___ _ ____   _____ _ __
//  | || '__| / __\___ \ / _ \ '__\ \ / / _ \ '__|
//  | || |  | \__ \___) |  __/ |   \ V /  __/ |
// |___|_|  |_|___/____/ \___|_|    \_/ \___|_|

type IrisServer =
  { Id: Id
    Port: Port
    Name: string
    IpAddress: IpAddress }

// * ClientEvent

//   ____ _ _            _   _____                 _
//  / ___| (_) ___ _ __ | |_| ____|_   _____ _ __ | |_
// | |   | | |/ _ \ '_ \| __|  _| \ \ / / _ \ '_ \| __|
// | |___| | |  __/ | | | |_| |___ \ V /  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|_____| \_/ \___|_| |_|\__|

[<RequireQualifiedAccess>]
type ClientEvent =
  | Registered
  | UnRegistered
  | Update
  | Status of ServiceStatus

// * IApiClient

//  ___    _          _  ____ _ _            _
// |_ _|  / \   _ __ (_)/ ___| (_) ___ _ __ | |_
//  | |  / _ \ | '_ \| | |   | | |/ _ \ '_ \| __|
//  | | / ___ \| |_) | | |___| | |  __/ | | | |_
// |___/_/   \_\ .__/|_|\____|_|_|\___|_| |_|\__|
//             |_|

type IApiClient =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract State: Either<IrisError,State>
  abstract Status: ServiceStatus
  abstract Subscribe: (ClientEvent -> unit) -> IDisposable
