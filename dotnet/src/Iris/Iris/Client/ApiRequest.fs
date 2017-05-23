namespace Iris.Client

// * Imports

open System
open Iris.Core
open Iris.Raft
open FlatBuffers
open Iris.Serialization

// * ApiError

[<RequireQualifiedAccess>]
type ApiError =
  | Internal         of string
  | UnknownCommand   of string
  | MalformedRequest of string

  override error.ToString() =
    match error with
    | Internal         str -> String.Format("Internal: {0}", str)
    | UnknownCommand   str -> String.Format("UnknownCommand: {0}", str)
    | MalformedRequest str -> String.Format("MalformedRequest: {0}", str)

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
  | Update   of StateMachine
  | Ping

  // ** ToOffset

  member request.ToOffset(builder: FlatBufferBuilder) =
    let inline withPayload builder cmd tipe (value: Offset<'a>) =
      ClientApiRequestFB.StartClientApiRequestFB(builder)
      ClientApiRequestFB.AddCommand(builder, cmd)
      ClientApiRequestFB.AddParameterType(builder, tipe)
      ClientApiRequestFB.AddParameter(builder, value.Value)
      ClientApiRequestFB.EndClientApiRequestFB(builder)

    let withoutPayload builder cmd =
      ClientApiRequestFB.StartClientApiRequestFB(builder)
      ClientApiRequestFB.AddCommand(builder, cmd)
      ClientApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
      ClientApiRequestFB.EndClientApiRequestFB(builder)

    match request with
    | Ping -> withoutPayload builder ClientApiCommandFB.PingFB
    | Snapshot state ->
      state.ToOffset(builder)
      |> withPayload builder ClientApiCommandFB.SnapshotFB ParameterFB.StateFB

    | Update sm ->
      match sm with
      // Project
      | UnloadProject -> withoutPayload builder ClientApiCommandFB.UnloadProjectFB

      | UpdateProject project ->
        project
        |> Binary.toOffset builder
        |> withPayload builder ClientApiCommandFB.UpdateProjectFB ParameterFB.ProjectFB

      // CuePlayer
      | AddCuePlayer player
      | UpdateCuePlayer player
      | RemoveCuePlayer player as cmd ->
        match cmd with
        | AddCuePlayer    _ -> ClientApiCommandFB.AddCuePlayerFB
        | UpdateCuePlayer _ -> ClientApiCommandFB.UpdateCuePlayerFB
        | RemoveCuePlayer _ -> ClientApiCommandFB.RemoveCuePlayerFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          player
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.CuePlayerFB

      // CLIENT
      | AddClient client
      | UpdateClient client
      | RemoveClient client as cmd ->
        match cmd with
        | AddClient    _ -> ClientApiCommandFB.AddClientFB
        | UpdateClient _ -> ClientApiCommandFB.UpdateClientFB
        | RemoveClient _ -> ClientApiCommandFB.RemoveClientFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          client
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.IrisClientFB

      // MEMBER
      | AddMember mem
      | UpdateMember mem
      | RemoveMember mem as cmd ->
        match cmd with
        | AddMember    _ -> ClientApiCommandFB.AddMemberFB
        | UpdateMember _ -> ClientApiCommandFB.UpdateMemberFB
        | RemoveMember _ -> ClientApiCommandFB.RemoveMemberFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          mem
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.RaftMemberFB

      // GROUP
      | AddPinGroup group
      | UpdatePinGroup group
      | RemovePinGroup group as cmd ->
        match cmd with
        | AddPinGroup    _ -> ClientApiCommandFB.AddPinGroupFB
        | UpdatePinGroup _ -> ClientApiCommandFB.UpdatePinGroupFB
        | RemovePinGroup _ -> ClientApiCommandFB.RemovePinGroupFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          group
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.PinGroupFB

      // PIN
      | AddPin pin
      | UpdatePin pin
      | RemovePin pin as cmd ->
        match cmd with
        | AddPin    _ -> ClientApiCommandFB.AddPinFB
        | UpdatePin _ -> ClientApiCommandFB.UpdatePinFB
        | RemovePin _ -> ClientApiCommandFB.RemovePinFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          pin
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.PinFB

      // CUE
      | AddCue cue
      | UpdateCue cue
      | RemoveCue cue as cmd ->
        match cmd with
        | AddCue    _ -> ClientApiCommandFB.AddCueFB
        | UpdateCue _ -> ClientApiCommandFB.UpdateCueFB
        | RemoveCue _ -> ClientApiCommandFB.RemoveCueFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          cue
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.CueFB

      // CUELIST
      | AddCueList cuelist
      | UpdateCueList cuelist
      | RemoveCueList cuelist as cmd ->
        match cmd with
        | AddCueList    _ -> ClientApiCommandFB.AddCueListFB
        | UpdateCueList _ -> ClientApiCommandFB.UpdateCueListFB
        | RemoveCueList _ -> ClientApiCommandFB.RemoveCueListFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          cuelist
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.CueListFB

      // User
      | AddUser user
      | UpdateUser user
      | RemoveUser user as cmd ->
        match cmd with
        | AddUser    _ -> ClientApiCommandFB.AddUserFB
        | UpdateUser _ -> ClientApiCommandFB.UpdateUserFB
        | RemoveUser _ -> ClientApiCommandFB.RemoveUserFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          user
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.UserFB

      // SESSION
      | AddSession session
      | UpdateSession session
      | RemoveSession session as cmd ->
        match cmd with
        | AddSession    _ -> ClientApiCommandFB.AddSessionFB
        | UpdateSession _ -> ClientApiCommandFB.UpdateSessionFB
        | RemoveSession _ -> ClientApiCommandFB.RemoveSessionFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          session
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.SessionFB

      // DISCOVERED SERVICES
      | AddDiscoveredService service
      | UpdateDiscoveredService service
      | RemoveDiscoveredService service as cmd ->
        match cmd with
        | AddDiscoveredService    _ -> ClientApiCommandFB.AddDiscoveredServiceFB
        | UpdateDiscoveredService _ -> ClientApiCommandFB.UpdateDiscoveredServiceFB
        | RemoveDiscoveredService _ -> ClientApiCommandFB.RemoveDiscoveredServiceFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          service
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.DiscoveredServiceFB

      // SLICES
      | UpdateSlices slices ->
        slices
        |> Binary.toOffset builder
        |> withPayload builder ClientApiCommandFB.UpdateSlicesFB ParameterFB.SlicesFB

      // CLOCK
      | UpdateClock tick ->
        ClockFB.CreateClockFB(builder, tick)
        |> withPayload builder ClientApiCommandFB.UpdateClockFB ParameterFB.ClockFB

      // SNAPSHOT
      | DataSnapshot state ->
        state
        |> Binary.toOffset builder
        |> withPayload builder ClientApiCommandFB.DataSnapshotFB ParameterFB.StateFB

      // LOG
      | LogMsg log ->
        log
        |> Binary.toOffset builder
        |> withPayload builder ClientApiCommandFB.LogEventFB ParameterFB.LogEventFB

      // SET LOG LEVEL
      | SetLogLevel level ->
        let offset = string level |> builder.CreateString
        StringFB.CreateStringFB(builder, offset)
        |> withPayload builder ClientApiCommandFB.SetLogLevelFB ParameterFB.StringFB

      // CALL CUE
      | CallCue cue ->
        cue
        |> Binary.toOffset builder
        |> withPayload builder ClientApiCommandFB.CallCueFB ParameterFB.CueFB

      | Command AppCommand.Undo ->
        withoutPayload builder ClientApiCommandFB.UndoFB

      | Command AppCommand.Redo ->
        withoutPayload builder ClientApiCommandFB.RedoFB

      | Command AppCommand.Reset ->
        withoutPayload builder ClientApiCommandFB.ResetFB

      | _ ->
        // OK OK, this is not really good, but for now its better to have slightly more traffic than
        // failures or more complex solution
        withoutPayload builder ClientApiCommandFB.PingFB

  // ** FromFB

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

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/

    | ClientApiCommandFB.UnloadProjectFB ->
      ClientApiRequest.Update UnloadProject
      |> Either.succeed

    | ClientApiCommandFB.UpdateProjectFB ->
      either {
        let! project =
          let projectish = fb.Parameter<ProjectFB>()
          if projectish.HasValue then
            let value = projectish.Value
            IrisProject.FromFB value
          else
            "Empty IrisProjectFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateProject project)
      }

    //   ____           ____  _
    //  / ___|   _  ___|  _ \| | __ _ _   _  ___ _ __
    // | |  | | | |/ _ \ |_) | |/ _` | | | |/ _ \ '__|
    // | |__| |_| |  __/  __/| | (_| | |_| |  __/ |
    //  \____\__,_|\___|_|   |_|\__,_|\__, |\___|_|
    //                                |___/

    | ClientApiCommandFB.AddCuePlayerFB ->
      either {
        let! player =
          let playerish = fb.Parameter<CuePlayerFB>()
          if playerish.HasValue then
            let value = playerish.Value
            CuePlayer.FromFB value
          else
            "Empty CuePlayer payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddCuePlayer player)
      }
    | ClientApiCommandFB.UpdateCuePlayerFB ->
      either {
        let! player =
          let playerish = fb.Parameter<CuePlayerFB>()
          if playerish.HasValue then
            let value = playerish.Value
            CuePlayer.FromFB value
          else
            "Empty CuePlayer payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateCuePlayer player)
      }
    | ClientApiCommandFB.RemoveCuePlayerFB ->
      either {
        let! player =
          let playerish = fb.Parameter<CuePlayerFB>()
          if playerish.HasValue then
            let value = playerish.Value
            CuePlayer.FromFB value
          else
            "Empty CuePlayer payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemoveCuePlayer player)
      }

    //   ____ _ _            _
    //  / ___| (_) ___ _ __ | |_
    // | |   | | |/ _ \ '_ \| __|
    // | |___| | |  __/ | | | |_
    //  \____|_|_|\___|_| |_|\__|

    | ClientApiCommandFB.AddClientFB ->
      either {
        let! client =
          let clientish = fb.Parameter<IrisClientFB>()
          if clientish.HasValue then
            let value = clientish.Value
            IrisClient.FromFB value
          else
            "Empty IrisClientFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddClient client)
      }
    | ClientApiCommandFB.UpdateClientFB ->
      either {
        let! client =
          let clientish = fb.Parameter<IrisClientFB>()
          if clientish.HasValue then
            let value = clientish.Value
            IrisClient.FromFB value
          else
            "Empty IrisClientFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateClient client)
      }
    | ClientApiCommandFB.RemoveClientFB ->
      either {
        let! client =
          let clientish = fb.Parameter<IrisClientFB>()
          if clientish.HasValue then
            let value = clientish.Value
            IrisClient.FromFB value
          else
            "Empty IrisClientFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemoveClient client)
      }

    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
    // | |  | |  __/ | | | | | |_) |  __/ |
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|

    | ClientApiCommandFB.AddMemberFB ->
      either {
        let! mem =
          let memish = fb.Parameter<RaftMemberFB>()
          if memish.HasValue then
            let value = memish.Value
            RaftMember.FromFB value
          else
            "Empty RaftMemberFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddMember mem)
      }
    | ClientApiCommandFB.UpdateMemberFB ->
      either {
        let! mem =
          let memish = fb.Parameter<RaftMemberFB>()
          if memish.HasValue then
            let value = memish.Value
            RaftMember.FromFB value
          else
            "Empty RaftMemberFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateMember mem)
      }
    | ClientApiCommandFB.RemoveMemberFB ->
      either {
        let! mem =
          let memish = fb.Parameter<RaftMemberFB>()
          if memish.HasValue then
            let value = memish.Value
            RaftMember.FromFB value
          else
            "Empty RaftMemberFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemoveMember mem)
      }

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | ClientApiCommandFB.AddPinGroupFB ->
      either {
        let! group =
          let groupish = fb.Parameter<PinGroupFB>()
          if groupish.HasValue then
            let value = groupish.Value
            PinGroup.FromFB value
          else
            "Empty PinGroupFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddPinGroup group)
      }
    | ClientApiCommandFB.UpdatePinGroupFB ->
      either {
        let! group =
          let groupish = fb.Parameter<PinGroupFB>()
          if groupish.HasValue then
            let value = groupish.Value
            PinGroup.FromFB value
          else
            "Empty PinGroupFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdatePinGroup group)
      }
    | ClientApiCommandFB.RemovePinGroupFB ->
      either {
        let! group =
          let groupish = fb.Parameter<PinGroupFB>()
          if groupish.HasValue then
            let value = groupish.Value
            PinGroup.FromFB value
          else
            "Empty PinGroupFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemovePinGroup group)
      }

    //  ____  _
    // |  _ \(_)_ __
    // | |_) | | '_ \
    // |  __/| | | | |
    // |_|   |_|_| |_|

    | ClientApiCommandFB.AddPinFB ->
      either {
        let! pin =
          let pinish = fb.Parameter<PinFB>()
          if pinish.HasValue then
            let value = pinish.Value
            Pin.FromFB value
          else
            "Empty PinFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddPin pin)
      }
    | ClientApiCommandFB.UpdatePinFB ->
      either {
        let! pin =
          let pinish = fb.Parameter<PinFB>()
          if pinish.HasValue then
            let value = pinish.Value
            Pin.FromFB value
          else
            "Empty PinFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdatePin pin)
      }
    | ClientApiCommandFB.RemovePinFB ->
      either {
        let! pin =
          let pinish = fb.Parameter<PinFB>()
          if pinish.HasValue then
            let value = pinish.Value
            Pin.FromFB value
          else
            "Empty PinFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemovePin pin)
      }
    | ClientApiCommandFB.UpdateSlicesFB ->
      either {
        let! slices =
          let slicish = fb.Parameter<SlicesFB>()
          if slicish.HasValue then
            let value = slicish.Value
            Slices.FromFB value
          else
            "Empty SlicesFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateSlices slices)
      }

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | ClientApiCommandFB.AddCueFB ->
      either {
        let! cue =
          let cueish = fb.Parameter<CueFB>()
          if cueish.HasValue then
            let value = cueish.Value
            Cue.FromFB value
          else
            "Empty CueFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddCue cue)
      }
    | ClientApiCommandFB.UpdateCueFB ->
      either {
        let! cue =
          let cueish = fb.Parameter<CueFB>()
          if cueish.HasValue then
            let value = cueish.Value
            Cue.FromFB value
          else
            "Empty CueFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateCue cue)
      }
    | ClientApiCommandFB.RemoveCueFB ->
      either {
        let! cue =
          let cueish = fb.Parameter<CueFB>()
          if cueish.HasValue then
            let value = cueish.Value
            Cue.FromFB value
          else
            "Empty CueFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemoveCue cue)
      }
    | ClientApiCommandFB.CallCueFB ->
      either {
        let! cue =
          let cueish = fb.Parameter<CueFB>()
          if cueish.HasValue then
            let value = cueish.Value
            Cue.FromFB value
          else
            "Empty CueFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (CallCue cue)
      }

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | ClientApiCommandFB.AddCueListFB ->
      either {
        let! cueList =
          let cueListish = fb.Parameter<CueListFB>()
          if cueListish.HasValue then
            let value = cueListish.Value
            CueList.FromFB value
          else
            "Empty CueListFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddCueList cueList)
      }
    | ClientApiCommandFB.UpdateCueListFB ->
      either {
        let! cueList =
          let cueListish = fb.Parameter<CueListFB>()
          if cueListish.HasValue then
            let value = cueListish.Value
            CueList.FromFB value
          else
            "Empty CueListFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateCueList cueList)
      }
    | ClientApiCommandFB.RemoveCueListFB ->
      either {
        let! cueList =
          let cueListish = fb.Parameter<CueListFB>()
          if cueListish.HasValue then
            let value = cueListish.Value
            CueList.FromFB value
          else
            "Empty CueListFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemoveCueList cueList)
      }

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | ClientApiCommandFB.AddUserFB ->
      either {
        let! user =
          let userish = fb.Parameter<UserFB>()
          if userish.HasValue then
            let value = userish.Value
            User.FromFB value
          else
            "Empty UserFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddUser user)
      }
    | ClientApiCommandFB.UpdateUserFB ->
      either {
        let! user =
          let userish = fb.Parameter<UserFB>()
          if userish.HasValue then
            let value = userish.Value
            User.FromFB value
          else
            "Empty UserFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateUser user)
      }
    | ClientApiCommandFB.RemoveUserFB ->
      either {
        let! user =
          let userish = fb.Parameter<UserFB>()
          if userish.HasValue then
            let value = userish.Value
            User.FromFB value
          else
            "Empty UserFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemoveUser user)
      }

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | ClientApiCommandFB.AddSessionFB ->
      either {
        let! session =
          let sessionish = fb.Parameter<SessionFB>()
          if sessionish.HasValue then
            let value = sessionish.Value
            Session.FromFB value
          else
            "Empty SessionFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddSession session)
      }
    | ClientApiCommandFB.UpdateSessionFB ->
      either {
        let! session =
          let sessionish = fb.Parameter<SessionFB>()
          if sessionish.HasValue then
            let value = sessionish.Value
            Session.FromFB value
          else
            "Empty SessionFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateSession session)
      }
    | ClientApiCommandFB.RemoveSessionFB ->
      either {
        let! session =
          let sessionish = fb.Parameter<SessionFB>()
          if sessionish.HasValue then
            let value = sessionish.Value
            Session.FromFB value
          else
            "Empty SessionFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemoveSession session)
      }

    //  ____        _        ____                        _           _
    // |  _ \  __ _| |_ __ _/ ___| _ __   __ _ _ __  ___| |__   ___ | |_
    // | | | |/ _` | __/ _` \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
    // | |_| | (_| | || (_| |___) | | | | (_| | |_) \__ \ | | | (_) | |_
    // |____/ \__,_|\__\__,_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
    //                                        |_|

    | ClientApiCommandFB.DataSnapshotFB ->
      either {
        let! state =
          let stateish = fb.Parameter<StateFB>()
          if stateish.HasValue then
            let value = stateish.Value
            State.FromFB value
          else
            "Empty StateFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (DataSnapshot state)
      }

    //  ____  _                                     _
    // |  _ \(_)___  ___ _____   _____ _ __ ___  __| |
    // | | | | / __|/ __/ _ \ \ / / _ \ '__/ _ \/ _` |
    // | |_| | \__ \ (_| (_) \ V /  __/ | |  __/ (_| |
    // |____/|_|___/\___\___/ \_/ \___|_|  \___|\__,_|

    | ClientApiCommandFB.AddDiscoveredServiceFB
    | ClientApiCommandFB.UpdateDiscoveredServiceFB
    | ClientApiCommandFB.RemoveDiscoveredServiceFB as cmd ->
      either {
        let! service =
          let serviceish = fb.Parameter<DiscoveredServiceFB>()
          if serviceish.HasValue then
            let value = serviceish.Value
            DiscoveredService.FromFB value
          else
            "Empty DiscoveredServiceFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        let mapper =
          match cmd with
          | ClientApiCommandFB.AddDiscoveredServiceFB    -> AddDiscoveredService
          | ClientApiCommandFB.UpdateDiscoveredServiceFB -> UpdateDiscoveredService
          | ClientApiCommandFB.RemoveDiscoveredServiceFB -> RemoveDiscoveredService
          | _ -> failwith "the impossible happened"
        return ClientApiRequest.Update (mapper service)
      }

    //  _
    // | |    ___   __ _
    // | |   / _ \ / _` |
    // | |__| (_) | (_| |
    // |_____\___/ \__, |
    //             |___/

    | ClientApiCommandFB.LogEventFB ->
      either {
        let! log =
          let logish = fb.Parameter<LogEventFB>()
          if logish.HasValue then
            let value = logish.Value
            LogEvent.FromFB value
          else
            "Empty LogEventFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (LogMsg log)
      }

    | ClientApiCommandFB.SetLogLevelFB ->
      either {
        let! level =
          let levelish = fb.Parameter<StringFB>()
          if levelish.HasValue then
            let value = levelish.Value
            LogLevel.TryParse value.Value
          else
            "Empty StringFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (SetLogLevel level)
      }

    //   ____ _            _
    //  / ___| | ___   ___| | __
    // | |   | |/ _ \ / __| |/ /
    // | |___| | (_) | (__|   <
    //  \____|_|\___/ \___|_|\_\

    | ClientApiCommandFB.UpdateClockFB ->
      either {
        let! clock =
          let clockish = fb.Parameter<ClockFB>()
          if clockish.HasValue then
            let value = clockish.Value
            Right value.Value
          else
            "Empty ClockFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdateClock clock)
      }

    //   ____               _
    //  / ___|_ __ ___   __| |
    // | |   | '_ ` _ \ / _` |
    // | |___| | | | | | (_| |
    //  \____|_| |_| |_|\__,_|

    | ClientApiCommandFB.UndoFB ->
      AppCommand.Undo
      |> Command
      |> ClientApiRequest.Update
      |> Either.succeed

    | ClientApiCommandFB.RedoFB ->
      AppCommand.Redo
      |> Command
      |> ClientApiRequest.Update
      |> Either.succeed

    | ClientApiCommandFB.ResetFB ->
      AppCommand.Reset
      |> Command
      |> ClientApiRequest.Update
      |> Either.succeed

    | x ->
      sprintf "Unknown Command in ApiRequest: %A" x
      |> Error.asClientError "ClientApiRequest.FromFB"
      |> Either.fail

  // ** ToBytes

  member request.ToBytes() =
    Binary.buildBuffer request

  // ** FromBytes

  static member FromBytes(raw: byte array) =
    raw
    |> Binary.createBuffer
    |> ClientApiRequestFB.GetRootAsClientApiRequestFB
    |> ClientApiRequest.FromFB

// * ServerApiRequest

type ServerApiRequest =
  | Register   of IrisClient
  | UnRegister of IrisClient
  | Update     of StateMachine

  member request.ToOffset(builder: FlatBufferBuilder) =
    match request with
    //   ____ _ _            _
    //  / ___| (_) ___ _ __ | |_
    // | |   | | |/ _ \ '_ \| __|
    // | |___| | |  __/ | | | |_
    //  \____|_|_|\___|_| |_|\__|

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

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | Update (AddCue cue) ->
      let offset = cue.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.AddCueFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.CueFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (UpdateCue cue) ->
      let offset = cue.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.UpdateCueFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.CueFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (RemoveCue cue) ->
      let offset = cue.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.RemoveCueFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.CueFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | Update (AddCueList cueList) ->
      let offset = cueList.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.AddCueListFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.CueListFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (UpdateCueList cueList) ->
      let offset = cueList.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.UpdateCueListFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.CueListFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (RemoveCueList cueList) ->
      let offset = cueList.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.RemoveCueListFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.CueListFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | Update (AddPinGroup group) ->
      let offset = group.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.AddPinGroupFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.PinGroupFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (UpdatePinGroup group) ->
      let offset = group.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.UpdatePinGroupFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.PinGroupFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (RemovePinGroup group) ->
      let offset = group.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.RemovePinGroupFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.PinGroupFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)

    //  ____  _
    // |  _ \(_)_ __
    // | |_) | | '_ \
    // |  __/| | | | |
    // |_|   |_|_| |_|

    | Update (AddPin pin) ->
      let offset = pin.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.AddPinFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.PinFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (UpdatePin pin) ->
      let offset = pin.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.UpdatePinFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.PinFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (RemovePin pin) ->
      let offset = pin.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.RemovePinFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.PinFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (UpdateSlices slices) ->
      let offset = slices.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.UpdateSlicesFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.SlicesFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update (CallCue cue) ->
      let offset = cue.ToOffset(builder)
      ServerApiRequestFB.StartServerApiRequestFB(builder)
      ServerApiRequestFB.AddCommand(builder, ServerApiCommandFB.CallCueFB)
      ServerApiRequestFB.AddParameterType(builder, ParameterFB.CueFB)
      ServerApiRequestFB.AddParameter(builder, offset.Value)
      ServerApiRequestFB.EndServerApiRequestFB(builder)
    | Update x ->
      failwithf "ServerApiRequest.ToOffset currently does not support command: %A" x

  static member FromFB(fb: ServerApiRequestFB) =
    match fb.Command with
    //   ____ _ _            _
    //  / ___| (_) ___ _ __ | |_
    // | |   | | |/ _ \ '_ \| __|
    // | |___| | |  __/ | | | |_
    //  \____|_|_|\___|_| |_|\__|

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

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | ServerApiCommandFB.AddCueFB ->
      match fb.ParameterType with
      | ParameterFB.CueFB ->
        let cueish = fb.Parameter<CueFB>()
        if cueish.HasValue then
          either {
            let value = cueish.Value
            let! cue = Cue.FromFB(value)
            return ServerApiRequest.Update(AddCue cue)
          }
        else
          "Empty CueFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.UpdateCueFB ->
      match fb.ParameterType with
      | ParameterFB.CueFB ->
        let cueish = fb.Parameter<CueFB>()
        if cueish.HasValue then
          either {
            let value = cueish.Value
            let! cue = Cue.FromFB(value)
            return ServerApiRequest.Update(UpdateCue cue)
          }
        else
          "Empty CueFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.RemoveCueFB ->
      match fb.ParameterType with
      | ParameterFB.CueFB ->
        let cueish = fb.Parameter<CueFB>()
        if cueish.HasValue then
          either {
            let value = cueish.Value
            let! cue = Cue.FromFB(value)
            return ServerApiRequest.Update(RemoveCue cue)
          }
        else
          "Empty CueFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.CallCueFB ->
      match fb.ParameterType with
      | ParameterFB.CueFB ->
        let cueish = fb.Parameter<CueFB>()
        if cueish.HasValue then
          either {
            let value = cueish.Value
            let! cue = Cue.FromFB(value)
            return ServerApiRequest.Update(CallCue cue)
          }
        else
          "Empty CueFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | ServerApiCommandFB.AddPinGroupFB ->
      match fb.ParameterType with
      | ParameterFB.PinGroupFB ->
        let groupish = fb.Parameter<PinGroupFB>()
        if groupish.HasValue then
          either {
            let value = groupish.Value
            let! group = PinGroup.FromFB(value)
            return ServerApiRequest.Update(AddPinGroup group)
          }
        else
          "Empty PinGroupFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.UpdatePinGroupFB ->
      match fb.ParameterType with
      | ParameterFB.PinGroupFB ->
        let groupish = fb.Parameter<PinGroupFB>()
        if groupish.HasValue then
          either {
            let value = groupish.Value
            let! group = PinGroup.FromFB(value)
            return ServerApiRequest.Update(UpdatePinGroup group)
          }
        else
          "Empty PinGroupFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.RemovePinGroupFB ->
      match fb.ParameterType with
      | ParameterFB.PinGroupFB ->
        let groupish = fb.Parameter<PinGroupFB>()
        if groupish.HasValue then
          either {
            let value = groupish.Value
            let! group = PinGroup.FromFB(value)
            return ServerApiRequest.Update(RemovePinGroup group)
          }
        else
          "Empty PinGroupFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | ServerApiCommandFB.AddCueListFB ->
      match fb.ParameterType with
      | ParameterFB.CueListFB ->
        let cueListish = fb.Parameter<CueListFB>()
        if cueListish.HasValue then
          either {
            let value = cueListish.Value
            let! cueList = CueList.FromFB(value)
            return ServerApiRequest.Update(AddCueList cueList)
          }
        else
          "Empty CueListFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.UpdateCueListFB ->
      match fb.ParameterType with
      | ParameterFB.CueListFB ->
        let cueListish = fb.Parameter<CueListFB>()
        if cueListish.HasValue then
          either {
            let value = cueListish.Value
            let! cueList = CueList.FromFB(value)
            return ServerApiRequest.Update(UpdateCueList cueList)
          }
        else
          "Empty CueListFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.RemoveCueListFB ->
      match fb.ParameterType with
      | ParameterFB.CueListFB ->
        let cueListish = fb.Parameter<CueListFB>()
        if cueListish.HasValue then
          either {
            let value = cueListish.Value
            let! cueList = CueList.FromFB(value)
            return ServerApiRequest.Update(RemoveCueList cueList)
          }
        else
          "Empty CueListFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail

    //  ____  _
    // |  _ \(_)_ __
    // | |_) | | '_ \
    // |  __/| | | | |
    // |_|   |_|_| |_|

    | ServerApiCommandFB.AddPinFB ->
      match fb.ParameterType with
      | ParameterFB.PinFB ->
        let pinish = fb.Parameter<PinFB>()
        if pinish.HasValue then
          either {
            let value = pinish.Value
            let! pin = Pin.FromFB(value)
            return ServerApiRequest.Update(AddPin pin)
          }
        else
          "Empty PinFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.UpdatePinFB ->
      match fb.ParameterType with
      | ParameterFB.PinFB ->
        let pinish = fb.Parameter<PinFB>()
        if pinish.HasValue then
          either {
            let value = pinish.Value
            let! pin = Pin.FromFB(value)
            return ServerApiRequest.Update(UpdatePin pin)
          }
        else
          "Empty PinFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.RemovePinFB ->
      match fb.ParameterType with
      | ParameterFB.PinFB ->
        let pinish = fb.Parameter<PinFB>()
        if pinish.HasValue then
          either {
            let value = pinish.Value
            let! pin = Pin.FromFB(value)
            return ServerApiRequest.Update(RemovePin pin)
          }
        else
          "Empty PinFB Parameter in ServerApiRequest"
          |> Error.asClientError "ServerApiRequest.FromFB"
          |> Either.fail
      | x ->
        sprintf "Wrong ParameterType in ServerApiRequest: %A" x
        |> Error.asClientError "ServerApiRequest.FromFB"
        |> Either.fail
    | ServerApiCommandFB.UpdateSlicesFB ->
      match fb.ParameterType with
      | ParameterFB.SlicesFB ->
        let slicish = fb.Parameter<SlicesFB>()
        if slicish.HasValue then
          either {
            let value = slicish.Value
            let! slices = Slices.FromFB(value)
            return ServerApiRequest.Update(UpdateSlices slices)
          }
        else
          "Empty SlicesFB Parameter in ServerApiRequest"
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
  | Registered
  | Unregistered
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
    | Registered ->
      ApiResponseFB.StartApiResponseFB(builder)
      ApiResponseFB.AddStatus(builder, StatusFB.RegisteredFB)
      ApiResponseFB.EndApiResponseFB(builder)
    | Unregistered ->
      ApiResponseFB.StartApiResponseFB(builder)
      ApiResponseFB.AddStatus(builder, StatusFB.UnregisteredFB)
      ApiResponseFB.EndApiResponseFB(builder)
    | NOK error ->
      let err = error.ToOffset(builder)
      ApiResponseFB.StartApiResponseFB(builder)
      ApiResponseFB.AddStatus(builder, StatusFB.OKFB)
      ApiResponseFB.AddError(builder, err)
      ApiResponseFB.EndApiResponseFB(builder)

  static member FromFB(fb: ApiResponseFB) =
    match fb.Status with
    | StatusFB.PongFB         -> Right Pong
    | StatusFB.OKFB           -> Right OK
    | StatusFB.RegisteredFB   -> Right Registered
    | StatusFB.UnregisteredFB -> Right Unregistered
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
    Binary.createBuffer raw
    |> ApiResponseFB.GetRootAsApiResponseFB
    |> ApiResponse.FromFB
