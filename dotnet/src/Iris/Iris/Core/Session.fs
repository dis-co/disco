namespace Iris.Core

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

#endif


#if !FABLE_COMPILER

// __   __              _    ___  _     _           _
// \ \ / /_ _ _ __ ___ | |  / _ \| |__ (_) ___  ___| |_
//  \ V / _` | '_ ` _ \| | | | | | '_ \| |/ _ \/ __| __|
//   | | (_| | | | | | | | | |_| | |_) | |  __/ (__| |_
//   |_|\__,_|_| |_| |_|_|  \___/|_.__// |\___|\___|\__|
//                                   |__/

[<AllowNullLiteral>]
type SessionStatusYaml(typ, payload) as self =
  [<DefaultValue>] val mutable StatusType : string
  [<DefaultValue>] val mutable Payload    : string

  new () = new SessionStatusYaml(null, null)

  do
    self.StatusType <- typ
    self.Payload    <- payload


type SessionYaml(id, status, ip, ua) as self =
  [<DefaultValue>] val mutable Id        : string
  [<DefaultValue>] val mutable Status    : SessionStatusYaml
  [<DefaultValue>] val mutable IpAddress : string
  [<DefaultValue>] val mutable UserAgent : string

  new () = new SessionYaml(null, null, null, null)

  do
    self.Id        <- id
    self.Status    <- status
    self.IpAddress <- ip
    self.UserAgent <- ua

#endif

//  ____                _
// / ___|  ___  ___ ___(_) ___  _ __
// \___ \ / _ \/ __/ __| |/ _ \| '_ \
//  ___) |  __/\__ \__ \ | (_) | | | |
// |____/ \___||___/___/_|\___/|_| |_|

type SessionStatusType =
  | Login
  | Unathorized
  | Authorized

  override self.ToString() =
    match self with
    | Login -> "Login"
    | Unathorized -> "Unathorized"
    | Authorized  -> "Authorized"

  static member Parse (str: string) =
    match str with
    | "Login" -> Login
    | "Unathorized" -> Unathorized
    | "Authorized"  -> Authorized
    | _         -> failwithf "SessionStatusType: failed to parse %s" str

  static member TryParse (str: string) =
    try
      str |> SessionStatusType.Parse |> Either.succeed
    with
      | exn ->
        sprintf "Could not parse SessionStatusType: %s" exn.Message
        |> ParseError
        |> Either.fail

  member self.ToOffset () =
    match self with
      | Login -> SessionStatusTypeFB.LoginFB
      | Unathorized -> SessionStatusTypeFB.UnathorizedFB
      | Authorized  -> SessionStatusTypeFB.AuthorizedFB

  static member FromFB (fb: SessionStatusTypeFB) =
#if FABLE_COMPILER
    match fb with
      | x when x = SessionStatusTypeFB.LoginFB -> Right Login
      | x when x = SessionStatusTypeFB.UnathorizedFB -> Right Unathorized
      | x when x = SessionStatusTypeFB.AuthorizedFB  -> Right Authorized
      | x ->
        sprintf "Could not parse SessionStatusType: %A" x
        |> ParseError
        |> Either.fail
#else
    match fb with
      | SessionStatusTypeFB.LoginFB -> Right Login
      | SessionStatusTypeFB.UnathorizedFB -> Right Unathorized
      | SessionStatusTypeFB.AuthorizedFB  -> Right Authorized
      | x ->
        sprintf "Could not parse SessionStatusType: %A" x
        |> ParseError
        |> Either.fail
#endif

type SessionStatus =
  { StatusType: SessionStatusType
  ; Payload: string }

  static member FromFB(fb: SessionStatusFB) : Either<IrisError, SessionStatus> =
    SessionStatusType.FromFB fb.StatusType
    |> Either.map (fun typ -> { StatusType = typ; Payload  = fb.Payload })

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError, SessionStatus> =
    Binary.createBuffer bytes
    |> SessionStatusFB.GetRootAsSessionStatusFB
    |> SessionStatus.FromFB

  member self.ToOffset(builder: FlatBufferBuilder) =
    let typ = self.StatusType.ToOffset()
    let payload = self.Payload |> builder.CreateString
    SessionStatusFB.StartSessionStatusFB(builder)
    SessionStatusFB.AddStatusType(builder, typ)
    SessionStatusFB.AddPayload(builder, payload)
    SessionStatusFB.EndSessionStatusFB(builder)

  member self.ToBytes() = Binary.buildBuffer self

#if !FABLE_COMPILER

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    new SessionStatusYaml(
      string self.StatusType,
      self.Payload)

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYamlObject (yml: SessionStatusYaml) =
    either {
      let! typ = SessionStatusType.TryParse yml.StatusType
      return { StatusType = typ
               Payload = yml.Payload }
    }

  static member FromYaml (str: string): Either<IrisError,SessionStatus> =
    let serializer = new Serializer()
    serializer.Deserialize<SessionStatusYaml>(str)
    |> Yaml.fromYaml

#endif

type Session =
  { Id: Id
  ; Status:  SessionStatus
  ; IpAddress: IpAddress
  ; UserAgent: UserAgent }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: SessionFB) : Either<IrisError, Session> =
    Either.ofNullable fb.Status ParseError
    |> Either.bind SessionStatus.FromFB
    |> Either.map (fun status ->
      { Id = Id fb.Id
      ; Status  = status
      ; IpAddress = IpAddress.Parse fb.IpAddress
      ; UserAgent = fb.UserAgent })

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,Session> =
    Binary.createBuffer bytes
    |> SessionFB.GetRootAsSessionFB
    |> Session.FromFB

  member self.ToOffset(builder: FlatBufferBuilder) =
    let session = self.Id |> string |> builder.CreateString
    let status = self.Status.ToOffset(builder)
    let ip = self.IpAddress |> string |> builder.CreateString
    let ua = self.UserAgent |> string |> builder.CreateString
    SessionFB.StartSessionFB(builder)
    SessionFB.AddId(builder, session)
    SessionFB.AddStatus(builder, status)
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
    let status =
      new SessionStatusYaml(
        string self.Status.StatusType,
        self.Status.Payload)
    new SessionYaml(
      string self.Id,
      status,
      string self.IpAddress,
      self.UserAgent)

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYamlObject (yml: SessionYaml) =
    either {
      let! ip = IpAddress.TryParse yml.IpAddress
      let! status = SessionStatus.FromYamlObject yml.Status
      return { Id = Id yml.Id
               Status = status
               IpAddress = ip
               UserAgent = yml.UserAgent }
    }

  static member FromYaml (str: string): Either<IrisError,Session> =
    let serializer = new Serializer()
    serializer.Deserialize<SessionYaml>(str)
    |> Yaml.fromYaml

#endif
