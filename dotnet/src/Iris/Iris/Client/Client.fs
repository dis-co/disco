namespace Iris.Client

open Iris.Core
open FlatBuffers
open Iris.Serialization.Api

// * IrisClient

type IrisClient =
  { Id: Id
    Name: string }

  member self.ToOffset(builder: FlatBufferBuilder) =
    implement "ToOffset"

  static member FromFB(fb: IrisClientFB) =
    implement "FromFB"

// * ClientApiRequest

type ClientApiRequest =
  | Register   of IrisClient
  | UnRegister of IrisClient
  | Ping

  member self.ToOffset(builder: FlatBufferBuilder) =
    implement "ToOffset"

  static member FromFB(fb: ClientApiRequestFB) =
    implement "FromFB"

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ClientApiRequestFB.GetRootAsClientApiRequestFB(Binary.createBuffer raw)
    |> ClientApiRequest.FromFB

// * ClientApiResponse

type ClientApiResponse =
  | Pong
  | OK
  | NOK of string

  member self.ToOffset(builder: FlatBufferBuilder) =
    implement "ToOffset"

  static member FromFB(fb: ClientApiResponseFB) =
    implement "FromFB"

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ClientApiResponseFB.GetRootAsClientApiResponseFB(Binary.createBuffer raw)
    |> ClientApiResponse.FromFB

// * Client module

[<RequireQualifiedAccess>]
module Client =

  // ** create

  let create () = failwith "never"

  // ** parseRequest

  let parseRequest (raw: byte array) : Either<IrisError,ClientApiRequest> =
    Binary.decode raw

  // ** serializeResponse

  let serializeResponse (command: ClientApiResponse) : byte array =
    implement "serializeResponse"
