namespace Iris.Core

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

//  ____                _
// / ___|  ___  ___ ___(_) ___  _ __
// \___ \ / _ \/ __/ __| |/ _ \| '_ \
//  ___) |  __/\__ \__ \ | (_) | | | |
// |____/ \___||___/___/_|\___/|_| |_|

type Session =
  { Id:        Id
  ; IpAddress: IpAddress
  ; UserAgent: UserAgent }

  static member Empty(id: Id) =
    { Id = id
    ; IpAddress = IPv4Address "0.0.0.0"
    ; UserAgent = "" }

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

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,Session> =
    Binary.createBuffer bytes
    |> SessionFB.GetRootAsSessionFB
    |> Session.FromFB

  member self.ToOffset(builder: FlatBufferBuilder) =
    let session = self.Id |> string |> builder.CreateString
    let ip = self.IpAddress |> string |> builder.CreateString
    let ua = self.UserAgent |> string |> builder.CreateString
    SessionFB.StartSessionFB(builder)
    SessionFB.AddId(builder, session)
    SessionFB.AddIpAddress(builder, ip)
    SessionFB.AddUserAgent(builder, ua)
    SessionFB.EndSessionFB(builder)

  member self.ToBytes() = Binary.buildBuffer self

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

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYamlObject (yml: SessionYaml) =
    either {
      let! ip = IpAddress.TryParse yml.IpAddress
      return { Id = Id yml.Id
               IpAddress = ip
               UserAgent = yml.UserAgent }
    }

  static member FromYaml (str: string): Either<IrisError,Session> =
    let serializer = new Serializer()
    serializer.Deserialize<SessionYaml>(str)
    |> Yaml.fromYaml

#endif
