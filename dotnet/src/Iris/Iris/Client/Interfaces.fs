namespace Iris.Client

// * Imports

open Iris.Core
open System
open System.Collections.Concurrent
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

// * IrisServer

//  ___      _     ____
// |_ _|_ __(_)___/ ___|  ___ _ ____   _____ _ __
//  | || '__| / __\___ \ / _ \ '__\ \ / / _ \ '__|
//  | || |  | \__ \___) |  __/ |   \ V /  __/ |
// |___|_|  |_|___/____/ \___|_|    \_/ \___|_|

type IrisServer =
  { Id: Id
    Name: string }

// * ClientEvent

//   ____ _ _            _   _____                 _
//  / ___| (_) ___ _ __ | |_| ____|_   _____ _ __ | |_
// | |   | | |/ _ \ '_ \| __|  _| \ \ / / _ \ '_ \| __|
// | |___| | |  __/ | | | |_| |___ \ V /  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|_____| \_/ \___|_| |_|\__|

[<RequireQualifiedAccess>]
type ClientEvent =
  | Registered
  | UnRegistered
  | Update

// * IApiClient

//  ___    _          _  ____ _ _            _
// |_ _|  / \   _ __ (_)/ ___| (_) ___ _ __ | |_
//  | |  / _ \ | '_ \| | |   | | |/ _ \ '_ \| __|
//  | | / ___ \| |_) | | |___| | |  __/ | | | |_
// |___/_/   \_\ .__/|_|\____|_|_|\___|_| |_|\__|
//             |_|

type IApiClient =
  inherit IDisposable
  abstract Start: unit -> Either<IrisError,unit>
  abstract Subscribe: (ClientEvent -> unit) -> IDisposable
