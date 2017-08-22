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

// * ApiRequest

type ApiRequest =
  | Snapshot   of State
  | Update     of StateMachine
  | Register   of IrisClient
  | UnRegister of IrisClient

  // ** ToOffset

  member request.ToOffset(builder: FlatBufferBuilder) =
    let inline withPayload builder cmd tipe (value: Offset<'a>) =
      ApiRequestFB.StartApiRequestFB(builder)
      ApiRequestFB.AddCommand(builder, cmd)
      ApiRequestFB.AddParameterType(builder, tipe)
      ApiRequestFB.AddParameter(builder, value.Value)
      ApiRequestFB.EndApiRequestFB(builder)

    let withoutPayload builder cmd =
      ApiRequestFB.StartApiRequestFB(builder)
      ApiRequestFB.AddCommand(builder, cmd)
      ApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
      ApiRequestFB.EndApiRequestFB(builder)

    match request with
    //  ____                        _           _
    // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
    // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
    //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
    // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
    //                   |_|
    | Snapshot state ->
      builder
      |> state.ToOffset
      |> withPayload builder ApiCommandFB.SnapshotFB ParameterFB.StateFB

    | Register client ->
      builder
      |> client.ToOffset
      |> withPayload builder ApiCommandFB.RegisterFB ParameterFB.IrisClientFB

    | UnRegister client ->
      builder
      |> client.ToOffset
      |> withPayload builder ApiCommandFB.UnRegisterFB ParameterFB.IrisClientFB

    | Update sm ->
      match sm with
      // Project
      | UnloadProject -> withoutPayload builder ApiCommandFB.UnloadProjectFB

      | UpdateProject project ->
        project
        |> Binary.toOffset builder
        |> withPayload builder ApiCommandFB.UpdateProjectFB ParameterFB.ProjectFB

      // CommandBatch
      | CommandBatch _ as batch ->
        batch
        |> Binary.toOffset builder
        |> withPayload builder ApiCommandFB.BatchFB ParameterFB.CommandBatchFB

      // CuePlayer
      | AddCuePlayer player
      | UpdateCuePlayer player
      | RemoveCuePlayer player as cmd ->
        match cmd with
        | AddCuePlayer    _ -> ApiCommandFB.AddCuePlayerFB
        | UpdateCuePlayer _ -> ApiCommandFB.UpdateCuePlayerFB
        | RemoveCuePlayer _ -> ApiCommandFB.RemoveCuePlayerFB
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
        | AddClient    _ -> ApiCommandFB.AddClientFB
        | UpdateClient _ -> ApiCommandFB.UpdateClientFB
        | RemoveClient _ -> ApiCommandFB.RemoveClientFB
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
        | AddMember    _ -> ApiCommandFB.AddMemberFB
        | UpdateMember _ -> ApiCommandFB.UpdateMemberFB
        | RemoveMember _ -> ApiCommandFB.RemoveMemberFB
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
        | AddPinGroup    _ -> ApiCommandFB.AddPinGroupFB
        | UpdatePinGroup _ -> ApiCommandFB.UpdatePinGroupFB
        | RemovePinGroup _ -> ApiCommandFB.RemovePinGroupFB
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
        | AddPin    _ -> ApiCommandFB.AddPinFB
        | UpdatePin _ -> ApiCommandFB.UpdatePinFB
        | RemovePin _ -> ApiCommandFB.RemovePinFB
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
        | AddCue    _ -> ApiCommandFB.AddCueFB
        | UpdateCue _ -> ApiCommandFB.UpdateCueFB
        | RemoveCue _ -> ApiCommandFB.RemoveCueFB
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
        | AddCueList    _ -> ApiCommandFB.AddCueListFB
        | UpdateCueList _ -> ApiCommandFB.UpdateCueListFB
        | RemoveCueList _ -> ApiCommandFB.RemoveCueListFB
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
        | AddUser    _ -> ApiCommandFB.AddUserFB
        | UpdateUser _ -> ApiCommandFB.UpdateUserFB
        | RemoveUser _ -> ApiCommandFB.RemoveUserFB
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
        | AddSession    _ -> ApiCommandFB.AddSessionFB
        | UpdateSession _ -> ApiCommandFB.UpdateSessionFB
        | RemoveSession _ -> ApiCommandFB.RemoveSessionFB
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
        | AddDiscoveredService    _ -> ApiCommandFB.AddDiscoveredServiceFB
        | UpdateDiscoveredService _ -> ApiCommandFB.UpdateDiscoveredServiceFB
        | RemoveDiscoveredService _ -> ApiCommandFB.RemoveDiscoveredServiceFB
        | _ -> failwith "the impossible happened"
        |> fun cmd ->
          service
          |> Binary.toOffset builder
          |> withPayload builder cmd ParameterFB.DiscoveredServiceFB

      // SLICES
      | UpdateSlices slices ->
        slices
        |> Binary.toOffset builder
        |> withPayload builder ApiCommandFB.UpdateSlicesFB ParameterFB.SlicesFB

      // CLOCK
      | UpdateClock tick ->
        ClockFB.CreateClockFB(builder, tick)
        |> withPayload builder ApiCommandFB.UpdateClockFB ParameterFB.ClockFB

      // SNAPSHOT
      | DataSnapshot state ->
        state
        |> Binary.toOffset builder
        |> withPayload builder ApiCommandFB.DataSnapshotFB ParameterFB.StateFB

      // LOG
      | LogMsg log ->
        log
        |> Binary.toOffset builder
        |> withPayload builder ApiCommandFB.LogEventFB ParameterFB.LogEventFB

      // SET LOG LEVEL
      | SetLogLevel level ->
        let offset = string level |> builder.CreateString
        StringFB.CreateStringFB(builder, offset)
        |> withPayload builder ApiCommandFB.SetLogLevelFB ParameterFB.StringFB

      // CALL CUE
      | CallCue cue ->
        cue
        |> Binary.toOffset builder
        |> withPayload builder ApiCommandFB.CallCueFB ParameterFB.CueFB

      | Command AppCommand.Undo ->
        withoutPayload builder ApiCommandFB.UndoFB

      | Command AppCommand.Redo ->
        withoutPayload builder ApiCommandFB.RedoFB

      | Command AppCommand.Reset ->
        withoutPayload builder ApiCommandFB.ResetFB

      | Command AppCommand.SaveProject ->
        withoutPayload builder ApiCommandFB.SaveProjectFB

  // ** FromFB

  static member FromFB(fb: ApiRequestFB) =
    match fb.Command with

    //  ____                        _           _
    // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
    // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
    //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
    // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
    //                   |_|
    | ApiCommandFB.SnapshotFB ->
      either {
        let! state =
          let statish = fb.Parameter<StateFB>()
          if statish.HasValue then
            let value = statish.Value
            State.FromFB(value)
          else
            "Empty StateFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return Snapshot state
      }

    //   ____ _ _            _
    //  / ___| (_) ___ _ __ | |_
    // | |   | | |/ _ \ '_ \| __|
    // | |___| | |  __/ | | | |_
    //  \____|_|_|\___|_| |_|\__|

    | ApiCommandFB.RegisterFB ->
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

    | ApiCommandFB.UnRegisterFB ->
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

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/

    | ApiCommandFB.UnloadProjectFB ->
      ApiRequest.Update UnloadProject
      |> Either.succeed

    | ApiCommandFB.UpdateProjectFB ->
      either {
        let! project =
          let projectish = fb.Parameter<ProjectFB>()
          if projectish.HasValue then
            let value = projectish.Value
            IrisProject.FromFB value
          else
            "Empty IrisProjectFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateProject project)
      }

    //   ____                                          _ ____        _       _
    //  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| | __ )  __ _| |_ ___| |__
    // | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |  _ \ / _` | __/ __| '_ \
    // | |__| (_) | | | | | | | | | | | (_| | | | | (_| | |_) | (_| | || (__| | | |
    //  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|____/ \__,_|\__\___|_| |_|

    | ApiCommandFB.BatchFB ->
      either {
        let! commands =
          let batchish = fb.Parameter<CommandBatchFB>()
          if batchish.HasValue then
            let batch = batchish.Value
            StateMachineBatch.FromFB batch
          else
            "Empty CommandBatchFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail

        return ApiRequest.Update (CommandBatch commands)
      }

    //   ____           ____  _
    //  / ___|   _  ___|  _ \| | __ _ _   _  ___ _ __
    // | |  | | | |/ _ \ |_) | |/ _` | | | |/ _ \ '__|
    // | |__| |_| |  __/  __/| | (_| | |_| |  __/ |
    //  \____\__,_|\___|_|   |_|\__,_|\__, |\___|_|
    //                                |___/

    | ApiCommandFB.AddCuePlayerFB ->
      either {
        let! player =
          let playerish = fb.Parameter<CuePlayerFB>()
          if playerish.HasValue then
            let value = playerish.Value
            CuePlayer.FromFB value
          else
            "Empty CuePlayer payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddCuePlayer player)
      }
    | ApiCommandFB.UpdateCuePlayerFB ->
      either {
        let! player =
          let playerish = fb.Parameter<CuePlayerFB>()
          if playerish.HasValue then
            let value = playerish.Value
            CuePlayer.FromFB value
          else
            "Empty CuePlayer payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateCuePlayer player)
      }
    | ApiCommandFB.RemoveCuePlayerFB ->
      either {
        let! player =
          let playerish = fb.Parameter<CuePlayerFB>()
          if playerish.HasValue then
            let value = playerish.Value
            CuePlayer.FromFB value
          else
            "Empty CuePlayer payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemoveCuePlayer player)
      }

    //   ____ _ _            _
    //  / ___| (_) ___ _ __ | |_
    // | |   | | |/ _ \ '_ \| __|
    // | |___| | |  __/ | | | |_
    //  \____|_|_|\___|_| |_|\__|

    | ApiCommandFB.AddClientFB ->
      either {
        let! client =
          let clientish = fb.Parameter<IrisClientFB>()
          if clientish.HasValue then
            let value = clientish.Value
            IrisClient.FromFB value
          else
            "Empty IrisClientFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddClient client)
      }
    | ApiCommandFB.UpdateClientFB ->
      either {
        let! client =
          let clientish = fb.Parameter<IrisClientFB>()
          if clientish.HasValue then
            let value = clientish.Value
            IrisClient.FromFB value
          else
            "Empty IrisClientFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateClient client)
      }
    | ApiCommandFB.RemoveClientFB ->
      either {
        let! client =
          let clientish = fb.Parameter<IrisClientFB>()
          if clientish.HasValue then
            let value = clientish.Value
            IrisClient.FromFB value
          else
            "Empty IrisClientFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemoveClient client)
      }

    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
    // | |  | |  __/ | | | | | |_) |  __/ |
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|

    | ApiCommandFB.AddMemberFB ->
      either {
        let! mem =
          let memish = fb.Parameter<RaftMemberFB>()
          if memish.HasValue then
            let value = memish.Value
            RaftMember.FromFB value
          else
            "Empty RaftMemberFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddMember mem)
      }
    | ApiCommandFB.UpdateMemberFB ->
      either {
        let! mem =
          let memish = fb.Parameter<RaftMemberFB>()
          if memish.HasValue then
            let value = memish.Value
            RaftMember.FromFB value
          else
            "Empty RaftMemberFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateMember mem)
      }
    | ApiCommandFB.RemoveMemberFB ->
      either {
        let! mem =
          let memish = fb.Parameter<RaftMemberFB>()
          if memish.HasValue then
            let value = memish.Value
            RaftMember.FromFB value
          else
            "Empty RaftMemberFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemoveMember mem)
      }

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | ApiCommandFB.AddPinGroupFB ->
      either {
        let! group =
          let groupish = fb.Parameter<PinGroupFB>()
          if groupish.HasValue then
            let value = groupish.Value
            PinGroup.FromFB value
          else
            "Empty PinGroupFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddPinGroup group)
      }
    | ApiCommandFB.UpdatePinGroupFB ->
      either {
        let! group =
          let groupish = fb.Parameter<PinGroupFB>()
          if groupish.HasValue then
            let value = groupish.Value
            PinGroup.FromFB value
          else
            "Empty PinGroupFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdatePinGroup group)
      }
    | ApiCommandFB.RemovePinGroupFB ->
      either {
        let! group =
          let groupish = fb.Parameter<PinGroupFB>()
          if groupish.HasValue then
            let value = groupish.Value
            PinGroup.FromFB value
          else
            "Empty PinGroupFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemovePinGroup group)
      }

    //  ____  _
    // |  _ \(_)_ __
    // | |_) | | '_ \
    // |  __/| | | | |
    // |_|   |_|_| |_|

    | ApiCommandFB.AddPinFB ->
      either {
        let! pin =
          let pinish = fb.Parameter<PinFB>()
          if pinish.HasValue then
            let value = pinish.Value
            Pin.FromFB value
          else
            "Empty PinFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddPin pin)
      }
    | ApiCommandFB.UpdatePinFB ->
      either {
        let! pin =
          let pinish = fb.Parameter<PinFB>()
          if pinish.HasValue then
            let value = pinish.Value
            Pin.FromFB value
          else
            "Empty PinFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdatePin pin)
      }
    | ApiCommandFB.RemovePinFB ->
      either {
        let! pin =
          let pinish = fb.Parameter<PinFB>()
          if pinish.HasValue then
            let value = pinish.Value
            Pin.FromFB value
          else
            "Empty PinFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemovePin pin)
      }
    | ApiCommandFB.UpdateSlicesFB ->
      either {
        let! slices =
          let slicish = fb.Parameter<SlicesFB>()
          if slicish.HasValue then
            let value = slicish.Value
            Slices.FromFB value
          else
            "Empty SlicesFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateSlices slices)
      }

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | ApiCommandFB.AddCueFB ->
      either {
        let! cue =
          let cueish = fb.Parameter<CueFB>()
          if cueish.HasValue then
            let value = cueish.Value
            Cue.FromFB value
          else
            "Empty CueFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddCue cue)
      }
    | ApiCommandFB.UpdateCueFB ->
      either {
        let! cue =
          let cueish = fb.Parameter<CueFB>()
          if cueish.HasValue then
            let value = cueish.Value
            Cue.FromFB value
          else
            "Empty CueFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateCue cue)
      }
    | ApiCommandFB.RemoveCueFB ->
      either {
        let! cue =
          let cueish = fb.Parameter<CueFB>()
          if cueish.HasValue then
            let value = cueish.Value
            Cue.FromFB value
          else
            "Empty CueFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemoveCue cue)
      }
    | ApiCommandFB.CallCueFB ->
      either {
        let! cue =
          let cueish = fb.Parameter<CueFB>()
          if cueish.HasValue then
            let value = cueish.Value
            Cue.FromFB value
          else
            "Empty CueFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (CallCue cue)
      }

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | ApiCommandFB.AddCueListFB ->
      either {
        let! cueList =
          let cueListish = fb.Parameter<CueListFB>()
          if cueListish.HasValue then
            let value = cueListish.Value
            CueList.FromFB value
          else
            "Empty CueListFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddCueList cueList)
      }
    | ApiCommandFB.UpdateCueListFB ->
      either {
        let! cueList =
          let cueListish = fb.Parameter<CueListFB>()
          if cueListish.HasValue then
            let value = cueListish.Value
            CueList.FromFB value
          else
            "Empty CueListFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateCueList cueList)
      }
    | ApiCommandFB.RemoveCueListFB ->
      either {
        let! cueList =
          let cueListish = fb.Parameter<CueListFB>()
          if cueListish.HasValue then
            let value = cueListish.Value
            CueList.FromFB value
          else
            "Empty CueListFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemoveCueList cueList)
      }

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | ApiCommandFB.AddUserFB ->
      either {
        let! user =
          let userish = fb.Parameter<UserFB>()
          if userish.HasValue then
            let value = userish.Value
            User.FromFB value
          else
            "Empty UserFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddUser user)
      }
    | ApiCommandFB.UpdateUserFB ->
      either {
        let! user =
          let userish = fb.Parameter<UserFB>()
          if userish.HasValue then
            let value = userish.Value
            User.FromFB value
          else
            "Empty UserFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateUser user)
      }
    | ApiCommandFB.RemoveUserFB ->
      either {
        let! user =
          let userish = fb.Parameter<UserFB>()
          if userish.HasValue then
            let value = userish.Value
            User.FromFB value
          else
            "Empty UserFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemoveUser user)
      }

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | ApiCommandFB.AddSessionFB ->
      either {
        let! session =
          let sessionish = fb.Parameter<SessionFB>()
          if sessionish.HasValue then
            let value = sessionish.Value
            Session.FromFB value
          else
            "Empty SessionFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (AddSession session)
      }
    | ApiCommandFB.UpdateSessionFB ->
      either {
        let! session =
          let sessionish = fb.Parameter<SessionFB>()
          if sessionish.HasValue then
            let value = sessionish.Value
            Session.FromFB value
          else
            "Empty SessionFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateSession session)
      }
    | ApiCommandFB.RemoveSessionFB ->
      either {
        let! session =
          let sessionish = fb.Parameter<SessionFB>()
          if sessionish.HasValue then
            let value = sessionish.Value
            Session.FromFB value
          else
            "Empty SessionFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (RemoveSession session)
      }

    //  ____        _        ____                        _           _
    // |  _ \  __ _| |_ __ _/ ___| _ __   __ _ _ __  ___| |__   ___ | |_
    // | | | |/ _` | __/ _` \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
    // | |_| | (_| | || (_| |___) | | | | (_| | |_) \__ \ | | | (_) | |_
    // |____/ \__,_|\__\__,_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
    //                                        |_|

    | ApiCommandFB.DataSnapshotFB ->
      either {
        let! state =
          let stateish = fb.Parameter<StateFB>()
          if stateish.HasValue then
            let value = stateish.Value
            State.FromFB value
          else
            "Empty StateFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (DataSnapshot state)
      }

    //  ____  _                                     _
    // |  _ \(_)___  ___ _____   _____ _ __ ___  __| |
    // | | | | / __|/ __/ _ \ \ / / _ \ '__/ _ \/ _` |
    // | |_| | \__ \ (_| (_) \ V /  __/ | |  __/ (_| |
    // |____/|_|___/\___\___/ \_/ \___|_|  \___|\__,_|

    | ApiCommandFB.AddDiscoveredServiceFB
    | ApiCommandFB.UpdateDiscoveredServiceFB
    | ApiCommandFB.RemoveDiscoveredServiceFB as cmd ->
      either {
        let! service =
          let serviceish = fb.Parameter<DiscoveredServiceFB>()
          if serviceish.HasValue then
            let value = serviceish.Value
            DiscoveredService.FromFB value
          else
            "Empty DiscoveredServiceFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        let mapper =
          match cmd with
          | ApiCommandFB.AddDiscoveredServiceFB    -> AddDiscoveredService
          | ApiCommandFB.UpdateDiscoveredServiceFB -> UpdateDiscoveredService
          | ApiCommandFB.RemoveDiscoveredServiceFB -> RemoveDiscoveredService
          | _ -> failwith "the impossible happened"
        return ApiRequest.Update (mapper service)
      }

    //  _
    // | |    ___   __ _
    // | |   / _ \ / _` |
    // | |__| (_) | (_| |
    // |_____\___/ \__, |
    //             |___/

    | ApiCommandFB.LogEventFB ->
      either {
        let! log =
          let logish = fb.Parameter<LogEventFB>()
          if logish.HasValue then
            let value = logish.Value
            LogEvent.FromFB value
          else
            "Empty LogEventFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (LogMsg log)
      }

    | ApiCommandFB.SetLogLevelFB ->
      either {
        let! level =
          let levelish = fb.Parameter<StringFB>()
          if levelish.HasValue then
            let value = levelish.Value
            LogLevel.TryParse value.Value
          else
            "Empty StringFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (SetLogLevel level)
      }

    //   ____ _            _
    //  / ___| | ___   ___| | __
    // | |   | |/ _ \ / __| |/ /
    // | |___| | (_) | (__|   <
    //  \____|_|\___/ \___|_|\_\

    | ApiCommandFB.UpdateClockFB ->
      either {
        let! clock =
          let clockish = fb.Parameter<ClockFB>()
          if clockish.HasValue then
            let value = clockish.Value
            Right value.Value
          else
            "Empty ClockFB payload"
            |> Error.asParseError "ApiRequest.FromFB"
            |> Either.fail
        return ApiRequest.Update (UpdateClock clock)
      }

    //   ____               _
    //  / ___|_ __ ___   __| |
    // | |   | '_ ` _ \ / _` |
    // | |___| | | | | | (_| |
    //  \____|_| |_| |_|\__,_|

    | ApiCommandFB.UndoFB ->
      AppCommand.Undo
      |> Command
      |> ApiRequest.Update
      |> Either.succeed

    | ApiCommandFB.RedoFB ->
      AppCommand.Redo
      |> Command
      |> ApiRequest.Update
      |> Either.succeed

    | ApiCommandFB.ResetFB ->
      AppCommand.Reset
      |> Command
      |> ApiRequest.Update
      |> Either.succeed

    | x ->
      sprintf "Unknown Command in ApiRequest: %A" x
      |> Error.asClientError "ApiRequest.FromFB"
      |> Either.fail

  // ** ToBytes

  member request.ToBytes() =
    Binary.buildBuffer request

  // ** FromBytes

  static member FromBytes(raw: byte array) =
    raw
    |> Binary.createBuffer
    |> ApiRequestFB.GetRootAsApiRequestFB
    |> ApiRequest.FromFB

// * ApiResponse

//     _          _ ____
//    / \   _ __ (_)  _ \ ___  ___ _ __   ___  _ __  ___  ___
//   / _ \ | '_ \| | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
//  / ___ \| |_) | |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
// /_/   \_\ .__/|_|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//         |_|                    |_|

type ApiResponse =
  | Registered
  | Unregistered
  | NOK of ApiError

  member response.ToOffset(builder: FlatBufferBuilder) =
    match response with
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
      ApiResponseFB.AddStatus(builder, StatusFB.NOKFB)
      ApiResponseFB.AddError(builder, err)
      ApiResponseFB.EndApiResponseFB(builder)

  static member FromFB(fb: ApiResponseFB) =
    match fb.Status with
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
