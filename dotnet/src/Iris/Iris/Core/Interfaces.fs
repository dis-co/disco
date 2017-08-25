[<AutoOpen>]
module Iris.Core.Interfaces

// * Imports

open System
open Iris.Core
open Iris.Raft

// * IAgentStore

type IAgentStore<'t when 't : not struct> =
  abstract State: 't
  abstract Update: 't -> unit

// * AgentStore module

module AgentStore =
  open System.Threading

  let create<'t when 't : not struct> () =
    let mutable state = Unchecked.defaultof<'t>

    { new IAgentStore<'t> with
        member self.State with get () = state
        member self.Update update =
          Interlocked.CompareExchange<'t>(&state, update, state)
          |> ignore }

// * Origin

type Origin =
  | Raft
  | Api
  | Service
  | Web     of session:Id
  | Client  of client:Id

// * DispatchStrategy

type DispatchStrategy =
  | Replicate
  | Process
  | Ignore
  | Publish

// * DiscoveryEvent

type DiscoveryEvent =
  | Status       of ServiceStatus
  | Registering  of DiscoverableService
  | UnRegistered of DiscoverableService
  | Registered   of DiscoverableService
  | Appeared     of DiscoveredService
  | Updated      of DiscoveredService
  | Vanished     of DiscoveredService

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () =
      match ev with
      | Status       _ -> Process
      | Registering  _
      | Registered   _
      | UnRegistered _ -> Ignore
      | Appeared     _
      | Updated      _
      | Vanished     _ -> Replicate

// * ClockEvent

type ClockEvent =
  { Frame: int64<frame>
    Deviation: int64<ns> }

  member tick.DispatchStrategy
    with get () = Publish

// * FileSystemEvent

type FileSystemEvent =
  | Created of name:Name * path:FilePath
  | Changed of name:Name * path:FilePath
  | Renamed of oldname:Name * oldpath:FilePath *  name:Name * path:FilePath
  | Deleted of name:Name * path:FilePath

// * IrisEvent

[<NoComparison;NoEquality>]
type IrisEvent =
  | Started             of tipe:ServiceType
  | ConfigurationDone   of members:RaftMember array
  | EnterJointConsensus of changes:ConfigChange array
  | LeaderChanged       of leader:MemberId option
  | StateChanged        of oldstate:RaftState * newstate:RaftState
  | PersistSnapshot     of log:RaftLogEntry
  | RaftError           of error:IrisError
  | Status              of ServiceStatus
  | GitPull             of remote:IpAddress
  | GitPush             of remote:IpAddress
  | FileSystem          of fs:FileSystemEvent
  | Append              of origin:Origin * cmd:StateMachine
  | SessionOpened       of session:Id
  | SessionClosed       of session:Id

  // ** ToString

  override ev.ToString() =
    match ev with
    | Started             _  -> "Started"
    | ConfigurationDone   _  -> "ConfigurationDone"
    | EnterJointConsensus _  -> "EnterJointConsensus"
    | LeaderChanged       _  -> "LeaderChanged"
    | StateChanged        _  -> "StateChanged"
    | PersistSnapshot     _  -> "PersistSnapshot"
    | RaftError           _  -> "RaftError"
    | Status              _  -> "Status"
    | SessionOpened       _  -> "SessionOpened"
    | SessionClosed       _  -> "SessionClosed"
    | GitPull             _  -> "GitPull"
    | GitPush             _  -> "GitPush"
    | FileSystem          e  -> String.format "FileSystem ({0})" e
    | Append (origin,cmd) -> sprintf "Append(%s, %s)" (string origin) (string cmd)

  // ** Origin

  member ev.Origin
    with get () =
      match ev with
      | Started             _
      | ConfigurationDone   _
      | EnterJointConsensus _
      | LeaderChanged       _
      | StateChanged        _
      | PersistSnapshot     _
      | RaftError           _
      | SessionOpened       _
      | SessionClosed       _
      | GitPull             _
      | GitPush             _
      | Status              _
      | FileSystem          _  -> None
      | Append (origin,     _) -> Some origin

  // ** DispatchStrategy

  member ev.DispatchStrategy
    with get () =
      match ev with
      | Status  _                                            -> Ignore
      | Started _                                            -> Publish

      //  _____ _ _      ____            _
      // |  ___(_) | ___/ ___| _   _ ___| |_ ___ _ __ ___
      // | |_  | | |/ _ \___ \| | | / __| __/ _ \ '_ ` _ \
      // |  _| | | |  __/___) | |_| \__ \ ||  __/ | | | | |
      // |_|   |_|_|\___|____/ \__, |___/\__\___|_| |_| |_|
      //                       |___/

      | FileSystem          _                                -> Ignore

      //  ____        __ _
      // |  _ \ __ _ / _| |_
      // | |_) / _` | |_| __|
      // |  _ < (_| |  _| |_
      // |_| \_\__,_|_|  \__|

      | ConfigurationDone   _
      | EnterJointConsensus _
      | StateChanged        _
      | LeaderChanged       _
      | PersistSnapshot     _
      | RaftError           _                                -> Process

      //   ____ _ _
      //  / ___(_) |_
      // | |  _| | __|
      // | |_| | | |_
      //  \____|_|\__|

      | GitPull         _
      | GitPush         _                                    -> Process

      // __        __   _    ____             _        _
      // \ \      / /__| |__/ ___|  ___   ___| | _____| |_
      //  \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __|
      //   \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_
      //    \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|

      | SessionOpened   _
      | SessionClosed   _                                    -> Replicate

      // *all* Raft and Api events get processed right away
      | Append (Origin.Raft, _)                              -> Publish
      | Append (Origin.Api,  _)                              -> Publish

      //  ____        _       _
      // | __ )  __ _| |_ ___| |__
      // |  _ \ / _` | __/ __| '_ \
      // | |_) | (_| | || (__| | | |
      // |____/ \__,_|\__\___|_| |_|

      | Append (Origin.Client  _, CommandBatch _)
      | Append (Origin.Service _, CommandBatch _)
      | Append (Origin.Web     _, CommandBatch _)            -> Replicate

      //  ____            _           _
      // |  _ \ _ __ ___ (_) ___  ___| |_
      // | |_) | '__/ _ \| |/ _ \/ __| __|
      // |  __/| | | (_) | |  __/ (__| |_
      // |_|   |_|  \___// |\___|\___|\__|
      //               |__/

      | Append (Origin.Web     _, UnloadProject)             -> Replicate
      | Append (Origin.Client  _, UnloadProject)             -> Ignore
      | Append (Origin.Service _, UnloadProject)             -> Replicate

      | Append (Origin.Web     _, UpdateProject _)
      | Append (Origin.Client  _, UpdateProject _)
      | Append (Origin.Service _, UpdateProject _)           -> Replicate

      //  __  __                _
      // |  \/  | ___ _ __ ___ | |__   ___ _ __
      // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
      // | |  | |  __/ | | | | | |_) |  __/ |
      // |_|  |_|\___|_| |_| |_|_.__/ \___|_|

      | Append (Origin.Web     _, AddMember    _)            -> Replicate
      | Append (Origin.Web     _, UpdateMember _)            -> Ignore
      | Append (Origin.Web     _, RemoveMember _)            -> Replicate
      | Append (Origin.Client  _, AddMember    _)
      | Append (Origin.Client  _, UpdateMember _)
      | Append (Origin.Client  _, RemoveMember _)            -> Ignore
      | Append (Origin.Service _, AddMember    _)
      | Append (Origin.Service _, UpdateMember _)
      | Append (Origin.Service _, RemoveMember _)            -> Replicate

      //   ____ _ _            _
      //  / ___| (_) ___ _ __ | |_
      // | |   | | |/ _ \ '_ \| __|
      // | |___| | |  __/ | | | |_
      //  \____|_|_|\___|_| |_|\__|

      | Append (Origin.Web     _, AddClient    _)
      | Append (Origin.Web     _, UpdateClient _)
      | Append (Origin.Web     _, RemoveClient _)
      | Append (Origin.Client  _, AddClient    _)
      | Append (Origin.Client  _, UpdateClient _)
      | Append (Origin.Client  _, RemoveClient _)            -> Ignore
      | Append (Origin.Service _, AddClient    _)
      | Append (Origin.Service _, UpdateClient _)
      | Append (Origin.Service _, RemoveClient _)            -> Replicate

      //  ____  _        ____
      // |  _ \(_)_ __  / ___|_ __ ___  _   _ _ __
      // | |_) | | '_ \| |  _| '__/ _ \| | | | '_ \
      // |  __/| | | | | |_| | | | (_) | |_| | |_) |
      // |_|   |_|_| |_|\____|_|  \___/ \__,_| .__/
      //                                     |_|

      | Append (Origin.Web     _, AddPinGroup    _)
      | Append (Origin.Web     _, UpdatePinGroup _)
      | Append (Origin.Web     _, RemovePinGroup _)          -> Ignore
      | Append (Origin.Client  _, AddPinGroup    _)
      | Append (Origin.Client  _, UpdatePinGroup _)
      | Append (Origin.Client  _, RemovePinGroup _)
      | Append (Origin.Service _, AddPinGroup    _)
      | Append (Origin.Service _, UpdatePinGroup _)
      | Append (Origin.Service _, RemovePinGroup _)          -> Replicate

      //  ____  _
      // |  _ \(_)_ __
      // | |_) | | '_ \
      // |  __/| | | | |
      // |_|   |_|_| |_|

      | Append (Origin.Web     _, AddPin    _)               -> Ignore
      | Append (Origin.Web     _, UpdatePin _)               -> Replicate
      | Append (Origin.Web     _, RemovePin _)               -> Ignore
      | Append (Origin.Client  _, AddPin    _)
      | Append (Origin.Client  _, UpdatePin _)
      | Append (Origin.Client  _, RemovePin _)
      | Append (Origin.Service _, AddPin    _)
      | Append (Origin.Service _, UpdatePin _)
      | Append (Origin.Service _, RemovePin _)               -> Replicate

      //   ____
      //  / ___|   _  ___
      // | |  | | | |/ _ \
      // | |__| |_| |  __/
      //  \____\__,_|\___|

      | Append (Origin.Web     _, AddCue    _)
      | Append (Origin.Web     _, UpdateCue _)
      | Append (Origin.Web     _, RemoveCue _)
      | Append (Origin.Client  _, AddCue    _)
      | Append (Origin.Client  _, UpdateCue _)
      | Append (Origin.Client  _, RemoveCue _)
      | Append (Origin.Service _, AddCue    _)
      | Append (Origin.Service _, UpdateCue _)
      | Append (Origin.Service _, RemoveCue _)               -> Replicate

      //   ____           _     _     _
      //  / ___|   _  ___| |   (_)___| |_
      // | |  | | | |/ _ \ |   | / __| __|
      // | |__| |_| |  __/ |___| \__ \ |_
      //  \____\__,_|\___|_____|_|___/\__|

      | Append (Origin.Web     _, AddCueList    _)
      | Append (Origin.Web     _, UpdateCueList _)
      | Append (Origin.Web     _, RemoveCueList _)
      | Append (Origin.Client  _, AddCueList    _)
      | Append (Origin.Client  _, UpdateCueList _)
      | Append (Origin.Client  _, RemoveCueList _)
      | Append (Origin.Service _, AddCueList    _)
      | Append (Origin.Service _, UpdateCueList _)
      | Append (Origin.Service _, RemoveCueList _)           -> Replicate

      //   ____           ____  _
      //  / ___|   _  ___|  _ \| | __ _ _   _  ___ _ __
      // | |  | | | |/ _ \ |_) | |/ _` | | | |/ _ \ '__|
      // | |__| |_| |  __/  __/| | (_| | |_| |  __/ |
      //  \____\__,_|\___|_|   |_|\__,_|\__, |\___|_|
      //                                |___/

      | Append (Origin.Web     _, AddCuePlayer    _)
      | Append (Origin.Web     _, UpdateCuePlayer _)
      | Append (Origin.Web     _, RemoveCuePlayer _)
      | Append (Origin.Client  _, AddCuePlayer    _)
      | Append (Origin.Client  _, UpdateCuePlayer _)
      | Append (Origin.Client  _, RemoveCuePlayer _)
      | Append (Origin.Service _, AddCuePlayer    _)
      | Append (Origin.Service _, UpdateCuePlayer _)
      | Append (Origin.Service _, RemoveCuePlayer _)         -> Replicate

      //  _   _
      // | | | |___  ___ _ __
      // | | | / __|/ _ \ '__|
      // | |_| \__ \  __/ |
      //  \___/|___/\___|_|

      | Append (Origin.Web     _, AddUser    _)
      | Append (Origin.Web     _, UpdateUser _)
      | Append (Origin.Web     _, RemoveUser _)              -> Replicate
      | Append (Origin.Client  _, AddUser    _)
      | Append (Origin.Client  _, UpdateUser _)
      | Append (Origin.Client  _, RemoveUser _)
      | Append (Origin.Service _, AddUser    _)
      | Append (Origin.Service _, UpdateUser _)
      | Append (Origin.Service _, RemoveUser _)              -> Ignore

      //  ____                _
      // / ___|  ___  ___ ___(_) ___  _ __
      // \___ \ / _ \/ __/ __| |/ _ \| '_ \
      //  ___) |  __/\__ \__ \ | (_) | | | |
      // |____/ \___||___/___/_|\___/|_| |_|

      | Append (Origin.Web     _, AddSession    _)
      | Append (Origin.Web     _, UpdateSession _)
      | Append (Origin.Web     _, RemoveSession _)           -> Replicate
      | Append (Origin.Client  _, AddSession    _)
      | Append (Origin.Client  _, UpdateSession _)
      | Append (Origin.Client  _, RemoveSession _)
      | Append (Origin.Service _, AddSession    _)
      | Append (Origin.Service _, UpdateSession _)
      | Append (Origin.Service _, RemoveSession _)           -> Ignore

      //  ____  _                                     _
      // |  _ \(_)___  ___ _____   _____ _ __ ___  __| |
      // | | | | / __|/ __/ _ \ \ / / _ \ '__/ _ \/ _` |
      // | |_| | \__ \ (_| (_) \ V /  __/ | |  __/ (_| |
      // |____/|_|___/\___\___/ \_/ \___|_|  \___|\__,_|

      | Append (Origin.Web     _, AddDiscoveredService    _)
      | Append (Origin.Web     _, UpdateDiscoveredService _)
      | Append (Origin.Web     _, RemoveDiscoveredService _)
      | Append (Origin.Client  _, AddDiscoveredService    _)
      | Append (Origin.Client  _, UpdateDiscoveredService _)
      | Append (Origin.Client  _, RemoveDiscoveredService _) -> Ignore
      | Append (Origin.Service _, AddDiscoveredService    _)
      | Append (Origin.Service _, UpdateDiscoveredService _)
      | Append (Origin.Service _, RemoveDiscoveredService _) -> Publish

      //   ____ _            _
      //  / ___| | ___   ___| | __
      // | |   | |/ _ \ / __| |/ /
      // | |___| | (_) | (__|   <
      //  \____|_|\___/ \___|_|\_\

      | Append (Origin.Service _, UpdateClock _)             -> Publish
      | Append (               _, UpdateClock _)             -> Ignore

      //   ____                                          _
      //  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| |
      // | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |
      // | |__| (_) | | | | | | | | | | | (_| | | | | (_| |
      //  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|

      | Append (Origin.Web     _, Command _)
      | Append (Origin.Client  _, Command _)
      | Append (Origin.Service _, Command _)                 -> Replicate

      //  ____        _        ____                        _           _
      // |  _ \  __ _| |_ __ _/ ___| _ __   __ _ _ __  ___| |__   ___ | |_
      // | | | |/ _` | __/ _` \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
      // | |_| | (_| | || (_| |___) | | | | (_| | |_) \__ \ | | | (_) | |_
      // |____/ \__,_|\__\__,_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
      //                                        |_|

      | Append (Origin.Web     _, DataSnapshot _)
      | Append (Origin.Client  _, DataSnapshot _)
      | Append (Origin.Service _, DataSnapshot _)            -> Ignore

      //  _____         _   ____
      // |  ___|_ _ ___| |_|  _ \ _ __ ___   ___ ___  ___ ___
      // | |_ / _` / __| __| |_) | '__/ _ \ / __/ _ \/ __/ __|
      // |  _| (_| \__ \ |_|  __/| | | (_) | (_|  __/\__ \__ \
      // |_|  \__,_|___/\__|_|   |_|  \___/ \___\___||___/___/

      | Append (_, UpdateSlices _)                           -> Publish
      | Append (_, CallCue      _)                           -> Publish

      //  _                __  __
      // | |    ___   __ _|  \/  |___  __ _
      // | |   / _ \ / _` | |\/| / __|/ _` |
      // | |__| (_) | (_| | |  | \__ \ (_| |
      // |_____\___/ \__, |_|  |_|___/\__, |
      //             |___/            |___/

      | Append (_, LogMsg _)                                 -> Publish

      //  __  __ _
      // |  \/  (_)___  ___
      // | |\/| | / __|/ __|
      // | |  | | \__ \ (__
      // |_|  |_|_|___/\___|

      | Append (_, SetLogLevel  _)                           -> Replicate
