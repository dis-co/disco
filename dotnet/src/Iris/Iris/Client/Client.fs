namespace Iris.Client

open Iris.Core
open FlatBuffers
open Iris.Serialization.Api

// * IrisClient

//  ___      _      ____ _ _            _
// |_ _|_ __(_)___ / ___| (_) ___ _ __ | |_
//  | || '__| / __| |   | | |/ _ \ '_ \| __|
//  | || |  | \__ \ |___| | |  __/ | | | |_
// |___|_|  |_|___/\____|_|_|\___|_| |_|\__|

type IrisClient =
  { Id: Id
    Name: string }

  member client.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string client.Id)
    let name = builder.CreateString client.Name

    IrisClientFB.StartIrisClientFB(builder)
    IrisClientFB.AddId(builder, id)
    IrisClientFB.AddName(builder, name)
    IrisClientFB.EndIrisClientFB(builder)

  static member FromFB(fb: IrisClientFB) =
    { Id = Id fb.Id
      Name = fb.Name }
    |> Either.succeed

// * ClientApiRequest

//   ____ _ _            _      _          _ ____                            _
//  / ___| (_) ___ _ __ | |_   / \   _ __ (_)  _ \ ___  __ _ _   _  ___  ___| |_
// | |   | | |/ _ \ '_ \| __| / _ \ | '_ \| | |_) / _ \/ _` | | | |/ _ \/ __| __|
// | |___| | |  __/ | | | |_ / ___ \| |_) | |  _ <  __/ (_| | |_| |  __/\__ \ |_
//  \____|_|_|\___|_| |_|\__/_/   \_\ .__/|_|_| \_\___|\__, |\__,_|\___||___/\__|
//                                  |_|                   |_|

type ClientApiRequest =
  | Register   of IrisClient
  | UnRegister of IrisClient
  | Ping

  member request.ToOffset(builder: FlatBufferBuilder) =
    match request with
    | Register client ->
      let offset = client.ToOffset builder
      ClientApiRequestFB.StartClientApiRequestFB(builder)
      ClientApiRequestFB.AddCommand(builder, CommandFB.RegisterFB)
      ClientApiRequestFB.AddParameterType(builder, ParameterFB.IrisClientFB)
      ClientApiRequestFB.AddParameter(builder, offset.Value)
      ClientApiRequestFB.EndClientApiRequestFB(builder)
    | UnRegister client ->
      let offset = client.ToOffset builder
      ClientApiRequestFB.StartClientApiRequestFB(builder)
      ClientApiRequestFB.AddCommand(builder, CommandFB.UnReqisterFB)
      ClientApiRequestFB.AddParameterType(builder, ParameterFB.IrisClientFB)
      ClientApiRequestFB.AddParameter(builder, offset.Value)
      ClientApiRequestFB.EndClientApiRequestFB(builder)
    | Ping ->
      ClientApiRequestFB.StartClientApiRequestFB(builder)
      ClientApiRequestFB.AddCommand(builder, CommandFB.PingFB)
      ClientApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
      ClientApiRequestFB.EndClientApiRequestFB(builder)

  static member FromFB(fb: ClientApiRequestFB) =
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
          "Empty IrisClientFB Parameter in ClientApiRequest"
          |> Error.asClientError "ClientApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ClientApiRequest: %A" x
        |> Error.asClientError "ClientApiRequest.FromFB"
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
          "Empty IrisClientFB Parameter in ClientApiRequest"
          |> Error.asClientError "ClientApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ClientApiRequest: %A" x
        |> Error.asClientError "ClientApiRequest.FromFB"
        |> Either.fail
    | CommandFB.PingFB -> Either.succeed Ping
    | x ->
      sprintf "Unknown Command in ClientApiRequest: %A" x
      |> Error.asClientError "ClientApiRequest.FromFB"
      |> Either.fail

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ClientApiRequestFB.GetRootAsClientApiRequestFB(Binary.createBuffer raw)
    |> ClientApiRequest.FromFB

// * ClientApiResponse

//   ____ _ _            _      _          _ ____
//  / ___| (_) ___ _ __ | |_   / \   _ __ (_)  _ \ ___  ___ _ __   ___  _ __  ___  ___
// | |   | | |/ _ \ '_ \| __| / _ \ | '_ \| | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
// | |___| | |  __/ | | | |_ / ___ \| |_) | |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
//  \____|_|_|\___|_| |_|\__/_/   \_\ .__/|_|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//                                  |_|                    |_|

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

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

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
