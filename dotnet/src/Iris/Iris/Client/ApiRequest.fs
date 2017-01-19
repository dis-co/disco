namespace Iris.Client

// * Imports

open Iris.Core
open FlatBuffers
open Iris.Serialization.Api


// * ServerApiRequest

type ServerApiRequest =
  | Register   of IrisClient
  | UnRegister of IrisClient
  | Ping

  member request.ToOffset(builder: FlatBufferBuilder) =
    match request with
    | Register client ->
      let offset = client.ToOffset builder
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.RegisterFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.IrisClientFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | UnRegister client ->
      let offset = client.ToOffset builder
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.UnReqisterFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.IrisClientFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Ping ->
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.PingFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
      ServerApiRequestFB.EndServerApiRequestFB(builder)

  static member FromFB(fb: ServerApiRequestFB) =
    match fb.Command with
    | ServerApiCommandFB.RegisterFB ->
      match fb.ParameterType with
      | ParameterFB.IrisClientFB ->
        let clientish = fb.Parameter<IrisClientFB>()
        if clientish.HasValue then
          either {
            let value = clientish.Value
            let! client = IrisClient.FromFB(value)
            return Register client
          }
        else
          "Empty IrisClientFB Parameter in ApiRequest"
          |> Error.asClientError "ApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ApiRequest: %A" x
        |> Error.asClientError "ApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.UnReqisterFB ->
      match fb.ParameterType with
      | ParameterFB.IrisClientFB ->
        let clientish = fb.Parameter<IrisClientFB>()
        if clientish.HasValue then
          either {
            let value = clientish.Value
            let! client = IrisClient.FromFB(value)
            return UnRegister client
          }
        else
          "Empty IrisClientFB Parameter in ApiRequest"
          |> Error.asClientError "ApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ApiRequest: %A" x
        |> Error.asClientError "ApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.PingFB -> Either.succeed Ping
    | x ->
      sprintf "Unknown Command in ApiRequest: %A" x
      |> Error.asClientError "ApiRequest.FromFB"
      |> Either.fail

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ServerApiRequestFB.GetRootAsServerApiRequestFB(Binary.createBuffer raw)
    |> ServerApiRequest.FromFB

// * ApiResponse

//     _          _ ____
//    / \   _ __ (_)  _ \ ___  ___ _ __   ___  _ __  ___  ___
//   / _ \ | '_ \| | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
//  / ___ \| |_) | |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
// /_/   \_\ .__/|_|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//         |_|                    |_|

type ApiResponse =
  | Pong
  | OK
  | NOK of string

  member self.ToOffset(builder: FlatBufferBuilder) =
    implement "ToOffset"

  static member FromFB(fb: ApiResponseFB) =
    implement "FromFB"

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ApiResponseFB.GetRootAsApiResponseFB(Binary.createBuffer raw)
    |> ApiResponse.FromFB
