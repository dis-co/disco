namespace Iris.Core

#if JAVASCRIPT
#else

open System
open FlatBuffers
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Iris.Serialization.Raft

#endif

type Session =
  { SessionId: Id
  ; UserName:  UserName
  ; IpAddress: IpAddress
  ; UserAgent: UserAgent
  }

#if JAVASCRIPT
#else

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: SessionFB) : Session option =
    try
      { SessionId = Id fb.SessionId
      ; UserName  = fb.UserName
      ; IpAddress = IpAddress.Parse fb.IpAddress
      ; UserAgent = fb.UserAgent
      }
      |> Some
    with
      | exn ->
        printfn "Could not de-serializae Session binary value: %s" exn.Message
        None

  static member FromBytes(bytes: byte array) : Session option =
    SessionFB.GetRootAsSessionFB(new ByteBuffer(bytes))
    |> Session.FromFB

  member self.ToOffset(builder: FlatBufferBuilder) =
    let session = self.SessionId |> string |> builder.CreateString
    let name = self.UserName |> builder.CreateString
    let ip = self.IpAddress |> string |> builder.CreateString
    let ua = self.UserAgent |> string |> builder.CreateString
    SessionFB.StartSessionFB(builder)
    SessionFB.AddSessionId(builder, session)
    SessionFB.AddUserName(builder, name)
    SessionFB.AddIpAddress(builder, ip)
    SessionFB.AddUserAgent(builder, ua)
    SessionFB.EndSessionFB(builder)

  member self.ToBytes() = Binary.buildBuffer self

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() =
    new JObject()
    |> addString "SessionId" (string self.SessionId)
    |> addString "UserName"   self.UserName
    |> addString "IpAddress" (string  self.IpAddress)
    |> addString "UserAgent"  self.UserAgent

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : Session option =
    try
      { SessionId = Id (string token.["SessionId"])
      ; UserName  = (string token.["UserName"])
      ; IpAddress = IpAddress.Parse (string token.["IpAddress"])
      ; UserAgent = (string token.["UserAgent"])
      }
      |> Some
    with
      | exn ->
        printfn "Could not deserialize session json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(json: string) : Session option =
    JToken.Parse(json) |> Session.FromJToken

#endif
