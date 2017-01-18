namespace Iris.Client

open Iris.Core

// * IrisClient

type IrisClient =
  { Id: Id
    Name: string }

// * ClientApiRequest

type ClientApiRequest =
  | Register   of IrisClient
  | UnRegister of IrisClient
  | Ping

// * ClientApiResponse

type ClientApiResponse =
  | Pong
  | OK
  | NOK of string

// * Client module

[<RequireQualifiedAccess>]
module Client =

  // ** create

  let create () = failwith "never"

  // ** parseRequest

  let parseRequest (raw: byte array) : ClientApiRequest =
    implement "parseRequest"

  // ** serializeResponse

  let serializeResponse (command: ClientApiResponse) : byte array =
    implement "serializeResponse"
