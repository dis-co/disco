namespace Iris.Client

// * Imports

open Iris.Core
open FlatBuffers
open Iris.Serialization.Api


// * ApiRequest

//     _          _ ____                            _
//    / \   _ __ (_)  _ \ ___  __ _ _   _  ___  ___| |_
//   / _ \ | '_ \| | |_) / _ \/ _` | | | |/ _ \/ __| __|
//  / ___ \| |_) | |  _ <  __/ (_| | |_| |  __/\__ \ |_
// /_/   \_\ .__/|_|_| \_\___|\__, |\__,_|\___||___/\__|
//         |_|                   |_|

type ApiRequest =
  | Register   of IrisClient
  | UnRegister of IrisClient
  | Ping

  member request.ToOffset(builder: FlatBufferBuilder) =
    match request with
    | Register client ->
      let offset = client.ToOffset builder
      ApiRequestFB.StartApiRequestFB(builder)
      ApiRequestFB.AddCommand(builder, CommandFB.RegisterFB)
      ApiRequestFB.AddParameterType(builder, ParameterFB.IrisClientFB)
      ApiRequestFB.AddParameter(builder, offset.Value)
      ApiRequestFB.EndApiRequestFB(builder)
    | UnRegister client ->
      let offset = client.ToOffset builder
      ApiRequestFB.StartApiRequestFB(builder)
      ApiRequestFB.AddCommand(builder, CommandFB.UnReqisterFB)
      ApiRequestFB.AddParameterType(builder, ParameterFB.IrisClientFB)
      ApiRequestFB.AddParameter(builder, offset.Value)
      ApiRequestFB.EndApiRequestFB(builder)
    | Ping ->
      ApiRequestFB.StartApiRequestFB(builder)
      ApiRequestFB.AddCommand(builder, CommandFB.PingFB)
      ApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
      ApiRequestFB.EndApiRequestFB(builder)

  static member FromFB(fb: ApiRequestFB) =
    match fb.Command with
    | CommandFB.RegisterFB ->
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
    | CommandFB.UnReqisterFB ->
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
    | CommandFB.PingFB -> Either.succeed Ping
    | x ->
      sprintf "Unknown Command in ApiRequest: %A" x
      |> Error.asClientError "ApiRequest.FromFB"
      |> Either.fail

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ApiRequestFB.GetRootAsApiRequestFB(Binary.createBuffer raw)
    |> ApiRequest.FromFB

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
