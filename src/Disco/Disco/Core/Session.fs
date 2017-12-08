namespace Disco.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System
open FlatBuffers
open Disco.Serialization

#endif

// * SessionYaml

#if !FABLE_COMPILER && !DISCO_NODES

open SharpYaml.Serialization

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
  { Id:        SessionId
    IpAddress: IpAddress
    UserAgent: UserAgent }

  // ** Empty

  static member Empty(id: SessionId) =
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

  static member FromFB(fb: SessionFB) : Either<DiscoError, Session> =
    either {
      let! ip = IpAddress.TryParse fb.IpAddress
      let! id = Id.decodeId fb
      return {
        Id = id
        IpAddress = ip
        UserAgent = fb.UserAgent
      }
    }

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<DiscoError,Session> =
    Binary.createBuffer bytes
    |> SessionFB.GetRootAsSessionFB
    |> Session.FromFB

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let session = SessionFB.CreateIdVector(builder,self.Id.ToByteArray())
    let ip = self.IpAddress |> string |> builder.CreateString
    let ua = self.UserAgent |> Option.mapNull builder.CreateString
    SessionFB.StartSessionFB(builder)
    SessionFB.AddId(builder, session)
    SessionFB.AddIpAddress(builder, ip)
    Option.iter (fun value -> SessionFB.AddUserAgent(builder, value)) ua
    SessionFB.EndSessionFB(builder)

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYaml () =
    SessionYaml(
      string self.Id,
      string self.IpAddress,
      self.UserAgent)

  // ** FromYaml

  static member FromYaml (yml: SessionYaml) =
    either {
      let! ip = IpAddress.TryParse yml.IpAddress
      let! id = DiscoId.TryParse yml.Id
      return {
        Id = id
        IpAddress = ip
        UserAgent = yml.UserAgent
      }
    }

  #endif
