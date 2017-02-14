namespace Iris.Client

// * Imports

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
    | Update sm ->
      match sm with
      // Project
      | UpdateProject project ->
        let offset = project.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdateProjectFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.ProjectFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      // Member
      | AddMember    mem ->
        let offset = mem.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.AddMemberFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.RaftMemberFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | UpdateMember mem ->
        let offset = mem.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdateMemberFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.RaftMemberFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | RemoveMember mem ->
        let offset = mem.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.RemoveMemberFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.RaftMemberFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      // PATCH
      | AddPatch    patch ->
        let offset = patch.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.AddPatchFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.PatchFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | UpdatePatch patch ->
        let offset = patch.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdatePatchFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.PatchFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | RemovePatch patch ->
        let offset = patch.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.RemovePatchFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.PatchFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      // PIN
      | AddPin pin ->
        let offset = pin.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.AddPinFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.PinFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | UpdatePin pin ->
        let offset = pin.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdatePinFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.PinFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | RemovePin pin ->
        let offset = pin.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.RemovePinFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.PinFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      // CUE
      | AddCue cue ->
        let offset = cue.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.AddCueFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.CueFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | UpdateCue cue ->
        let offset = cue.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdateCueFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.CueFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | RemoveCue cue ->
        let offset = cue.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.RemoveCueFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.CueFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      // CUE
      | AddCueList cuelist ->
        let offset = cuelist.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.AddCueListFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.CueListFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | UpdateCueList cuelist ->
        let offset = cuelist.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdateCueListFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.CueListFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | RemoveCueList cuelist ->
        let offset = cuelist.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.RemoveCueListFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.CueListFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      // User
      | AddUser user ->
        let offset = user.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.AddUserFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.UserFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | UpdateUser user ->
        let offset = user.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdateUserFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.UserFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | RemoveUser user ->
        let offset = user.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.RemoveUserFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.UserFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      // Session
      | AddSession session ->
        let offset = session.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.AddSessionFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.SessionFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | UpdateSession session ->
        let offset = session.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdateSessionFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.SessionFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | RemoveSession session ->
        let offset = session.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.RemoveSessionFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.SessionFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | UpdateSlices slices ->
        let offset = slices.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UpdateSlicesFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.SlicesFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | CallCue cue ->
        let offset = cue.ToOffset(builder)
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.CallCueFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.CueFB)
        ClientApiRequestFB.AddParameter(builder, offset.Value)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | Command AppCommand.Undo ->
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.UndoFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | Command AppCommand.Redo ->
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.RedoFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | Command AppCommand.Reset ->
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.ResetFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
        ClientApiRequestFB.EndClientApiRequestFB(builder)

      | _ ->
        // OK OK, this is not really good, but for now its better to have slightly more traffic than
        // failures or more complex solution
        ClientApiRequestFB.StartClientApiRequestFB(builder)
        ClientApiRequestFB.AddCommand(builder, ClientApiCommandFB.PingFB)
        ClientApiRequestFB.AddParameterType(builder, ParameterFB.NONE)
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

    //  ____            _           _
    // |  _ \ _ __ ___ (_) ___  ___| |_
    // | |_) | '__/ _ \| |/ _ \/ __| __|
    // |  __/| | | (_) | |  __/ (__| |_
    // |_|   |_|  \___// |\___|\___|\__|
    //               |__/

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

    | ClientApiCommandFB.AddPatchFB ->
      either {
        let! patch =
          let patchish = fb.Parameter<PatchFB>()
          if patchish.HasValue then
            let value = patchish.Value
            Patch.FromFB value
          else
            "Empty PatchFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (AddPatch patch)
      }
    | ClientApiCommandFB.UpdatePatchFB ->
      either {
        let! patch =
          let patchish = fb.Parameter<PatchFB>()
          if patchish.HasValue then
            let value = patchish.Value
            Patch.FromFB value
          else
            "Empty PatchFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (UpdatePatch patch)
      }
    | ClientApiCommandFB.RemovePatchFB ->
      either {
        let! patch =
          let patchish = fb.Parameter<PatchFB>()
          if patchish.HasValue then
            let value = patchish.Value
            Patch.FromFB value
          else
            "Empty PatchFB payload"
            |> Error.asParseError "ClientApiRequest.FromFB"
            |> Either.fail
        return ClientApiRequest.Update (RemovePatch patch)
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

  member request.ToBytes() =
    Binary.buildBuffer request

  static member FromBytes(raw: byte array) =
    ClientApiRequestFB.GetRootAsClientApiRequestFB(Binary.createBuffer raw)
    |> ClientApiRequest.FromFB

// * ServerApiRequest

type ServerApiRequest =
  | Register   of IrisClient
  | UnRegister of IrisClient
  | Update     of StateMachine

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
