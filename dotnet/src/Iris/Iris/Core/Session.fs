namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open FlatBuffers
open Iris.Serialization
open SharpYaml.Serialization

#endif

// * SessionYaml

#if !FABLE_COMPILER

// __   __              _    ___  _     _           _
// \ \ / /_ _ _ __ ___ | |  / _ \| |__ (_) ___  ___| |_
//  \ V / _` | '_ ` _ \| | | | | | '_ \| |/ _ \/ __| __|
//   | | (_| | | | | | | | | |_| | |_) | |  __/ (__| |_
//   |_|\__,_|_| |_| |_|_|  \___/|_.__// |\___|\___|\__|
//                                   |__/

type SessionYaml(id, ip, ua) as self =
  [<DefaultValue>] val mutable Id        : string
  [<DefaultValue>] val mutable IpAddress : string
  [<DefaultValue>] val mutable UserAgent : string

  new () = new SessionYaml(null, null, null)

  do
    self.Id        <- id
    self.IpAddress <- ip
    self.UserAgent <- ua

#endif

// * Session

//  ____                _
// / ___|  ___  ___ ___(_) ___  _ __
// \___ \ / _ \/ __/ __| |/ _ \| '_ \
//  ___) |  __/\__ \__ \ | (_) | | | |
// |____/ \___||___/___/_|\___/|_| |_|

type Session =
  { Id:        Id
    IpAddress: IpAddress
    UserAgent: UserAgent }

  // ** Empty

  static member Empty(id: Id) =
    { Id = id
      IpAddress = IPv4Address "0.0.0.0"
      UserAgent = "" }

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: SessionFB) : Either<IrisError, Session> =
    either {
      let! ip = IpAddress.TryParse fb.IpAddress
      return { Id = Id fb.Id
               IpAddress = ip
               UserAgent = fb.UserAgent }
    }

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<IrisError,Session> =
    Binary.createBuffer bytes
    |> SessionFB.GetRootAsSessionFB
    |> Session.FromFB

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let session = self.Id |> string |> builder.CreateString
    let ip = self.IpAddress |> string |> builder.CreateString
    let ua = self.UserAgent |> string |> builder.CreateString
    SessionFB.StartSessionFB(builder)
    SessionFB.AddId(builder, session)
    SessionFB.AddIpAddress(builder, ip)
    SessionFB.AddUserAgent(builder, ua)
    SessionFB.EndSessionFB(builder)

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** ToYamlObject

  #if !FABLE_COMPILER

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    new SessionYaml(
      string self.Id,
      string self.IpAddress,
      self.UserAgent)

  // ** ToYaml

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  // ** FromYamlObject

  static member FromYamlObject (yml: SessionYaml) =
    either {
      let! ip = IpAddress.TryParse yml.IpAddress
      return { Id = Id yml.Id
               IpAddress = ip
               UserAgent = yml.UserAgent }
    }

  // ** FromYaml

  static member FromYaml (str: string): Either<IrisError,Session> =
    let serializer = new Serializer()
    serializer.Deserialize<SessionYaml>(str)
    |> Yaml.fromYaml

  #endif
