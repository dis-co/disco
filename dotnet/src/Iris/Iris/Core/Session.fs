namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

#endif

// __   __              _    ___  _     _           _
// \ \ / /_ _ _ __ ___ | |  / _ \| |__ (_) ___  ___| |_
//  \ V / _` | '_ ` _ \| | | | | | '_ \| |/ _ \/ __| __|
//   | | (_| | | | | | | | | |_| | |_) | |  __/ (__| |_
//   |_|\__,_|_| |_| |_|_|  \___/|_.__// |\___|\___|\__|
//                                   |__/

type SessionYaml(id, name, ip, ua) as self =
  [<DefaultValue>] val mutable Id        : string
  [<DefaultValue>] val mutable UserName  : string
  [<DefaultValue>] val mutable IpAddress : string
  [<DefaultValue>] val mutable UserAgent : string

  new () = new SessionYaml(null, null, null, null)

  do
    self.Id        <- id
    self.UserName  <- name
    self.IpAddress <- ip
    self.UserAgent <- ua

//  ____                _
// / ___|  ___  ___ ___(_) ___  _ __
// \___ \ / _ \/ __/ __| |/ _ \| '_ \
//  ___) |  __/\__ \__ \ | (_) | | | |
// |____/ \___||___/___/_|\___/|_| |_|

type Session =
  { Id: Id
  ; UserName:  UserName
  ; IpAddress: IpAddress
  ; UserAgent: UserAgent
  }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: SessionFB) : Session option =
    try
      { Id = Id fb.Id
      ; UserName  = fb.UserName
      ; IpAddress = IpAddress.Parse fb.IpAddress
      ; UserAgent = fb.UserAgent
      }
      |> Some
    with
      | exn ->
        printfn "Could not de-serializae Session binary value: %s" exn.Message
        None

  static member FromBytes(bytes: Binary.Buffer) : Session option =
    Binary.createBuffer bytes
    |> SessionFB.GetRootAsSessionFB
    |> Session.FromFB

  member self.ToOffset(builder: FlatBufferBuilder) =
    let session = self.Id |> string |> builder.CreateString
    let name = self.UserName |> builder.CreateString
    let ip = self.IpAddress |> string |> builder.CreateString
    let ua = self.UserAgent |> string |> builder.CreateString
    SessionFB.StartSessionFB(builder)
    SessionFB.AddId(builder, session)
    SessionFB.AddUserName(builder, name)
    SessionFB.AddIpAddress(builder, ip)
    SessionFB.AddUserAgent(builder, ua)
    SessionFB.EndSessionFB(builder)

  member self.ToBytes() = Binary.buildBuffer self

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    new SessionYaml(
      string self.Id,
      self.UserName,
      string self.IpAddress,
      self.UserAgent)

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYamlObject (yml: SessionYaml) =
    maybe {
      let! ip = IpAddress.TryParse yml.IpAddress
      return { Id = Id yml.Id
               UserName = yml.UserName
               IpAddress = ip
               UserAgent = yml.UserAgent }
    }

  static member FromYaml (str: string) : Either<IrisError,Session> =
    let serializer = new Serializer()
    serializer.Deserialize<SessionYaml>(str)
    |> Yaml.fromYaml
