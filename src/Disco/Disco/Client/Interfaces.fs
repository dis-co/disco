(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

[<AutoOpen>]
module Disco.Client.Interfaces

// * Imports

open Disco.Core
open System
open System.Collections.Concurrent
open FlatBuffers
open Disco.Serialization

// * DiscoServer

type DiscoServer =
  { Port: Port
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
  abstract Id: ClientId
  abstract Start: unit -> DiscoResult<unit>
  abstract Restart: server:DiscoServer -> DiscoResult<unit>
  abstract State: State
  abstract Status: ServiceStatus
  abstract Subscribe: (ClientEvent -> unit) -> IDisposable
  abstract AddPinGroup: PinGroup -> unit
  abstract UpdatePinGroup: PinGroup -> unit
  abstract RemovePinGroup: PinGroup -> unit
  abstract AddCue: Cue -> unit
  abstract UpdateCue: Cue -> unit
  abstract RemoveCue: Cue -> unit
  abstract AddCueList: CueList -> unit
  abstract UpdateCueList: CueList -> unit
  abstract RemoveCueList: CueList -> unit
  abstract AddPin: Pin -> unit
  abstract UpdatePin: Pin -> unit
  abstract UpdateSlices: Slices list -> unit
  abstract RemovePin: Pin -> unit
  abstract Append: StateMachine -> unit
