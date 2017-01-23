namespace Iris.Client

// * Imports

open Iris.Core
open FlatBuffers
open Iris.Serialization

// * ApiError

[<RequireQualifiedAccess>]
type ApiError =
  | Internal         of string
  | UnknownCommand   of string
  | MalformedRequest of string

  member error.ToOffset(builder: FlatBufferBuilder) =
    match error with
    | Internal         str ->
      let err = builder.CreateString str
      ApiErrorFB.StartApiErrorFB(builder)
      ApiErrorFB.AddType(builder, ApiErrorTypeFB.InternalFB)
      ApiErrorFB.AddData(builder, err)
      ApiErrorFB.EndApiErrorFB(builder)

    | UnknownCommand   str ->
      let err = builder.CreateString str
      ApiErrorFB.StartApiErrorFB(builder)
      ApiErrorFB.AddType(builder, ApiErrorTypeFB.UnknownCommandFB)
      ApiErrorFB.AddData(builder, err)
      ApiErrorFB.EndApiErrorFB(builder)

    | MalformedRequest str ->
      let err = builder.CreateString str
      ApiErrorFB.StartApiErrorFB(builder)
      ApiErrorFB.AddType(builder, ApiErrorTypeFB.MalformedRequestFB)
      ApiErrorFB.AddData(builder, err)
      ApiErrorFB.EndApiErrorFB(builder)

  static member FromFB(fb: ApiErrorFB) =
    match fb.Type with
    | ApiErrorTypeFB.InternalFB         ->
      Internal fb.Data
      |> Either.succeed
    | ApiErrorTypeFB.UnknownCommandFB   ->
      UnknownCommand fb.Data
      |> Either.succeed
    | ApiErrorTypeFB.MalformedRequestFB ->
      MalformedRequest fb.Data
      |> Either.succeed
    | x ->
      sprintf "Unknown ApiErrorFB: %A" x
      |> Error.asClientError "ApiErrorFB.FromFB"
      |> Either.fail

// * ClientApiRequest

type ClientApiRequest =
  | Snapshot of State
  | Ping

  member request.ToOffset(builder: FlatBufferBuilder) =
    match request with
    | Ping ->
      ClientApiRequestFB.StartClientApiRequestFB(builder)
      ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.PingFB)
      ClientApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
      ClientApiRequestFB.EndClientApiRequestFB(builder)
    | Snapshot state ->
      let offset = state.ToOffset(builder)
      ClientApiRequestFB.StartClientApiRequestFB(builder)
      ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.SnapshotFB)
      ClientApiRequestFB.AddParameterType(builder, ParameterFB.StateFB)
      ClientApiRequestFB.AddParameter(builder, offset.Value)
      ClientApiRequestFB.EndClientApiRequestFB(builder)

  static member FromFB(fb: ClientApiRequestFB) =
    match fb.Command with
    | ClientApiCommandFB.PingFB -> Either.succeed Ping
    | ClientApiCommandFB.SnapshotFB ->
      either {
        let! state =
          let statish = fb.Parameter<StateFB>()
          if statish.HasValue then
            let value = statish.Value
            State.FromFB(value)
          else
            "Empty StateFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return Snapshot state
      }
    | x ->
      sprintf "Unknown Command in ApiRequest: %A" x
      |> Error.asClientError "ClientApiRequest.FromFB"
      |> Either.fail

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ClientApiRequestFB.GetRootAsClientApiRequestFB(Binary.createBuffer raw)
    |> ClientApiRequest.FromFB

// * ServerApiRequest

type ServerApiRequest =
  | Register   of IrisClient
  | UnRegister of IrisClient

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
          "Empty IrisClientFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
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
          "Empty IrisClientFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | x ->
      sprintf "Unknown Command in ServerApiRequest: %A" x
      |> Error.asClientError "ServerApiRequest.FromFB"
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
  | NOK of ApiError

  member response.ToOffset(builder: FlatBufferBuilder) =
    match response with
    | Pong ->
      ApiResponseFB.StartApiResponseFB(builder)
      ApiResponseFB.AddStatus(builder, StatusFB.PongFB)
      ApiResponseFB.EndApiResponseFB(builder)
    | OK ->
      ApiResponseFB.StartApiResponseFB(builder)
      ApiResponseFB.AddStatus(builder, StatusFB.OKFB)
      ApiResponseFB.EndApiResponseFB(builder)
    | NOK error ->
      let err = error.ToOffset(builder)
      ApiResponseFB.StartApiResponseFB(builder)
      ApiResponseFB.AddStatus(builder, StatusFB.OKFB)
      ApiResponseFB.AddError(builder, err)
      ApiResponseFB.EndApiResponseFB(builder)

  static member FromFB(fb: ApiResponseFB) =
    match fb.Status with
    | StatusFB.PongFB -> Right Pong
    | StatusFB.OKFB   -> Right OK
    | StatusFB.NOKFB  ->
      either {
        let! error =
          let errorish = fb.Error
          if errorish.HasValue then
            let value = errorish.Value
            ApiError.FromFB value
          else
            "Empty ApiErrorFB value"
            |> Error.asParseError "ApiResponse.FromFB"
            |> Either.fail
        return NOK error
      }
    | x ->
      sprintf "Unknown StatusFB value: %A" x
      |> Error.asParseError "ApiResponse.FromFB"
      |> Either.fail

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ApiResponseFB.GetRootAsApiResponseFB(Binary.createBuffer raw)
    |> ApiResponse.FromFB
