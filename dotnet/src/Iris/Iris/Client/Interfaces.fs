namespace Iris.Client

// * Imports

open Iris.Core
open System
open System.Collections.Concurrent
open FlatBuffers
open Iris.Serialization


// * IrisServer

//  ___      _     ____
// |_ _|_ __(_)___/ ___|  ___ _ ____   _____ _ __
//  | || '__| / __\___ \ / _ \ '__\ \ / / _ \ '__|
//  | || |  | \__ \___) |  __/ |   \ V /  __/ |
// |___|_|  |_|___/____/ \___|_|    \_/ \___|_|

type IrisServer =
  { Id: Id
    Port: Port
    Name: string
    IpAddress: IpAddress }

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
  | Update of StateMachine
  | Snapshot
  | Status of ServiceStatus

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
  abstract State: Either<IrisError,State>
  abstract Status: ServiceStatus
  abstract Subscribe: (ClientEvent -> unit) -> IDisposable
  abstract AddPinGroup: PinGroup -> Either<IrisError,unit>
  abstract UpdatePinGroup: PinGroup -> Either<IrisError,unit>
  abstract RemovePinGroup: PinGroup -> Either<IrisError,unit>
  abstract AddCue: Cue -> Either<IrisError,unit>
  abstract UpdateCue: Cue -> Either<IrisError,unit>
  abstract RemoveCue: Cue -> Either<IrisError,unit>
  abstract AddCueList: CueList -> Either<IrisError,unit>
  abstract UpdateCueList: CueList -> Either<IrisError,unit>
  abstract RemoveCueList: CueList -> Either<IrisError,unit>
  abstract AddPin: Pin -> Either<IrisError,unit>
  abstract UpdatePin: Pin -> Either<IrisError,unit>
  abstract UpdateSlices: Slices -> Either<IrisError,unit>
  abstract RemovePin: Pin -> Either<IrisError,unit>
