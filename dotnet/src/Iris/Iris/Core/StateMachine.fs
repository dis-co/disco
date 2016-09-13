namespace Iris.Core

open Iris.Raft

#if JAVASCRIPT

open Fable.Core.JsInterop

#else

open Iris.Serialization.Raft
open FlatBuffers
open Newtonsoft.Json
open Newtonsoft.Json.Linq

#endif

(*
        _                _____                 _
       / \   _ __  _ __ | ____|_   _____ _ __ | |_ ™
      / _ \ | '_ \| '_ \|  _| \ \ / / _ \ '_ \| __|
     / ___ \| |_) | |_) | |___ \ V /  __/ | | | |_
    /_/   \_\ .__/| .__/|_____| \_/ \___|_| |_|\__|
            |_|   |_|

    The AppEventT type models all possible state-changes the app can legally
    undergo. Using this design, we have a clean understanding of how data flows
    through the system, and have the compiler assist us in handling all possible
    states with total functions.

*)

[<RequireQualifiedAccess>]
type AppCommand =
  | Undo
  | Redo
  | Reset

  // PROJECT
  | SaveProject
  // | OpenProject
  // | CreateProject
  // | CloseProject
  // | DeleteProject

#if JAVASCRIPT
#else

  static member Type
    with get () = Serialization.GetTypeName<AppCommand>()

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: AppCommandFB) =
    match fb.Command with
    | AppCommandTypeFB.UndoFB        -> Some Undo
    | AppCommandTypeFB.RedoFB        -> Some Redo
    | AppCommandTypeFB.ResetFB       -> Some Reset
    | AppCommandTypeFB.SaveProjectFB -> Some SaveProject
    | _                              -> None

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<AppCommandFB> =
    let tipe =
      match self with
      | Undo        -> AppCommandTypeFB.UndoFB
      | Redo        -> AppCommandTypeFB.RedoFB
      | Reset       -> AppCommandTypeFB.ResetFB
      | SaveProject -> AppCommandTypeFB.SaveProjectFB

    AppCommandFB.StartAppCommandFB(builder)
    AppCommandFB.AddCommand(builder, tipe)
    AppCommandFB.EndAppCommandFB(builder)

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() : JToken =
    let json = new JObject()
    json.["$type"] <- new JValue(AppCommand.Type)

    let add (case: string) =
      json.["Case"] <- new JValue(case)

    match self with
    | Undo        -> add "Undo"
    | Redo        -> add "Redo"
    | Reset       -> add "Reset"
    | SaveProject -> add "SaveProject"

    json :> JToken

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : AppCommand option =
    try
      match string token.["Case"] with
      | "Undo"        -> Some Undo
      | "Redo"        -> Some Redo
      | "Reset"       -> Some Reset
      | "SaveProject" -> Some SaveProject
      | _             -> None
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : AppCommand option =
    JToken.Parse(str) |> AppCommand.FromJToken

#endif

type StateMachine =

  // CLIENT
  // | AddClient    of string
  // | UpdateClient of string
  // | RemoveClient of string

  // NODE
  | AddNode       of RaftNode
  | UpdateNode    of RaftNode
  | RemoveNode    of RaftNode

  // PATCH
  | AddPatch      of Patch
  | UpdatePatch   of Patch
  | RemovePatch   of Patch

  // IOBOX
  | AddIOBox      of IOBox
  | UpdateIOBox   of IOBox
  | RemoveIOBox   of IOBox

  // CUE
  | AddCue        of Cue
  | UpdateCue     of Cue
  | RemoveCue     of Cue

  // CUE
  | AddCueList    of CueList
  | UpdateCueList of CueList
  | RemoveCueList of CueList

  // User
  | AddUser       of User
  | UpdateUser    of User
  | RemoveUser    of User

  // Session
  | AddSession    of Session
  | UpdateSession of Session
  | RemoveSession of Session

  | Command       of AppCommand

  | DataSnapshot  of string

  | LogMsg        of LogLevel * string

  override self.ToString() : string =
    match self with
    // PROJECT
    // | OpenProject   -> "OpenProject"
    // | SaveProject   -> "SaveProject"
    // | CreateProject -> "CreateProject"
    // | CloseProject  -> "CloseProject"
    // | DeleteProject -> "DeleteProject

    // CLIENT
    // | AddClient    s -> sprintf "AddClient %s"    s
    // | UpdateClient s -> sprintf "UpdateClient %s" s
    // | RemoveClient s -> sprintf "RemoveClient %s" s

    // NODE
    | AddNode    node       -> sprintf "AddNode %s"    (string node)
    | UpdateNode node       -> sprintf "UpdateNode %s" (string node)
    | RemoveNode node       -> sprintf "RemoveNode %s" (string node)

    // PATCH
    | AddPatch    patch     -> sprintf "AddPatch %s"    (string patch)
    | UpdatePatch patch     -> sprintf "UpdatePatch %s" (string patch)
    | RemovePatch patch     -> sprintf "RemovePatch %s" (string patch)

    // IOBOX
    | AddIOBox    iobox     -> sprintf "AddIOBox %s"    (string iobox)
    | UpdateIOBox iobox     -> sprintf "UpdateIOBox %s" (string iobox)
    | RemoveIOBox iobox     -> sprintf "RemoveIOBox %s" (string iobox)

    // CUE
    | AddCue    cue         -> sprintf "AddCue %s"    (string cue)
    | UpdateCue cue         -> sprintf "UpdateCue %s" (string cue)
    | RemoveCue cue         -> sprintf "RemoveCue %s" (string cue)

    // CUELIST
    | AddCueList    cuelist -> sprintf "AddCueList %s"    (string cuelist)
    | UpdateCueList cuelist -> sprintf "UpdateCueList %s" (string cuelist)
    | RemoveCueList cuelist -> sprintf "RemoveCueList %s" (string cuelist)

    // User
    | AddUser    user       -> sprintf "AddUser %s"    (string user)
    | UpdateUser user       -> sprintf "UpdateUser %s" (string user)
    | RemoveUser user       -> sprintf "RemoveUser %s" (string user)

    // Session
    | AddSession    session -> sprintf "AddSession %s"    (string session)
    | UpdateSession session -> sprintf "UpdateSession %s" (string session)
    | RemoveSession session -> sprintf "RemoveSession %s" (string session)

    | Command    ev         -> sprintf "Command: %s"  (string ev)
    | DataSnapshot str      -> sprintf "DataSnapshot: %s" str
    | LogMsg(level, msg)    -> sprintf "LogMsg: [%A] %s" level msg

#if JAVASCRIPT
#else

  static member Type
    with get () = Serialization.GetTypeName<StateMachine>()

  static member FromFB (fb: StateMachineFB) =
    match fb.AppEventType with

    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | StateMachineTypeFB.AddCueFB ->
      let ev = fb.GetAppEvent(new AddCueFB())
      ev.GetCue(new CueFB())
      |> Cue.FromFB
      |> Option.map AddCue

    | StateMachineTypeFB.UpdateCueFB  ->
      let ev = fb.GetAppEvent(new UpdateCueFB())
      ev.GetCue(new CueFB())
      |> Cue.FromFB
      |> Option.map UpdateCue

    | StateMachineTypeFB.RemoveCueFB  ->
      let ev = fb.GetAppEvent(new RemoveCueFB())
      ev.GetCue(new CueFB())
      |> Cue.FromFB
      |> Option.map RemoveCue

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | StateMachineTypeFB.AddCueListFB ->
      let ev = fb.GetAppEvent(new AddCueListFB())
      ev.GetCueList(new CueListFB())
      |> CueList.FromFB
      |> Option.map AddCueList

    | StateMachineTypeFB.UpdateCueListFB  ->
      let ev = fb.GetAppEvent(new UpdateCueListFB())
      ev.GetCueList(new CueListFB())
      |> CueList.FromFB
      |> Option.map UpdateCueList

    | StateMachineTypeFB.RemoveCueListFB  ->
      let ev = fb.GetAppEvent(new RemoveCueListFB())
      ev.GetCueList(new CueListFB())
      |> CueList.FromFB
      |> Option.map RemoveCueList

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | StateMachineTypeFB.AddPatchFB ->
      let ev = fb.GetAppEvent(new AddPatchFB())
      ev.GetPatch(new PatchFB())
      |> Patch.FromFB
      |> Option.map AddPatch

    | StateMachineTypeFB.UpdatePatchFB  ->
      let ev = fb.GetAppEvent(new UpdatePatchFB())
      ev.GetPatch(new PatchFB())
      |> Patch.FromFB
      |> Option.map UpdatePatch

    | StateMachineTypeFB.RemovePatchFB  ->
      let ev = fb.GetAppEvent(new RemovePatchFB())
      ev.GetPatch(new PatchFB())
      |> Patch.FromFB
      |> Option.map RemovePatch

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    | StateMachineTypeFB.AddIOBoxFB ->
      let ev = fb.GetAppEvent(new AddIOBoxFB())
      ev.GetIOBox(new IOBoxFB())
      |> IOBox.FromFB
      |> Option.map AddIOBox

    | StateMachineTypeFB.UpdateIOBoxFB  ->
      let ev = fb.GetAppEvent(new UpdateIOBoxFB())
      ev.GetIOBox(new IOBoxFB())
      |> IOBox.FromFB
      |> Option.map UpdateIOBox

    | StateMachineTypeFB.RemoveIOBoxFB  ->
      let ev = fb.GetAppEvent(new RemoveIOBoxFB())
      ev.GetIOBox(new IOBoxFB())
      |> IOBox.FromFB
      |> Option.map RemoveIOBox

    //  _   _           _
    // | \ | | ___   __| | ___
    // |  \| |/ _ \ / _` |/ _ \
    // | |\  | (_) | (_| |  __/
    // |_| \_|\___/ \__,_|\___|

    | StateMachineTypeFB.AddNodeFB ->
      let ev = fb.GetAppEvent(new AddNodeFB())
      ev.GetNode(new NodeFB())
      |> RaftNode.FromFB
      |> Option.map AddNode

    | StateMachineTypeFB.UpdateNodeFB  ->
      let ev = fb.GetAppEvent(new UpdateNodeFB())
      ev.GetNode(new NodeFB())
      |> RaftNode.FromFB
      |> Option.map UpdateNode

    | StateMachineTypeFB.RemoveNodeFB  ->
      let ev = fb.GetAppEvent(new RemoveNodeFB())
      ev.GetNode(new NodeFB())
      |> RaftNode.FromFB
      |> Option.map RemoveNode

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | StateMachineTypeFB.AddUserFB ->
      let ev = fb.GetAppEvent(new AddUserFB())
      ev.GetUser(new UserFB())
      |> User.FromFB
      |> Option.map AddUser

    | StateMachineTypeFB.UpdateUserFB  ->
      let ev = fb.GetAppEvent(new UpdateUserFB())
      ev.GetUser(new UserFB())
      |> User.FromFB
      |> Option.map UpdateUser

    | StateMachineTypeFB.RemoveUserFB  ->
      let ev = fb.GetAppEvent(new RemoveUserFB())
      ev.GetUser(new UserFB())
      |> User.FromFB
      |> Option.map RemoveUser

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | StateMachineTypeFB.AddSessionFB ->
      let ev = fb.GetAppEvent(new AddSessionFB())
      ev.GetSession(new SessionFB())
      |> Session.FromFB
      |> Option.map AddSession

    | StateMachineTypeFB.UpdateSessionFB  ->
      let ev = fb.GetAppEvent(new UpdateSessionFB())
      ev.GetSession(new SessionFB())
      |> Session.FromFB
      |> Option.map UpdateSession

    | StateMachineTypeFB.RemoveSessionFB  ->
      let ev = fb.GetAppEvent(new RemoveSessionFB())
      ev.GetSession(new SessionFB())
      |> Session.FromFB
      |> Option.map RemoveSession

    //  __  __ _
    // |  \/  (_)___  ___
    // | |\/| | / __|/ __|
    // | |  | | \__ \ (__
    // |_|  |_|_|___/\___|

    | StateMachineTypeFB.LogMsgFB     ->
      let ev = fb.GetAppEvent(new LogMsgFB())
      LogLevel.Parse ev.LogLevel
      |> Option.map (fun level -> LogMsg(level, ev.Msg))

    | StateMachineTypeFB.AppCommandFB ->
      let ev = fb.GetAppEvent(new AppCommandFB())
      AppCommand.FromFB ev
      |> Option.map Command

    | StateMachineTypeFB.DataSnapshotFB ->
      let entry = fb.GetAppEvent(new DataSnapshotFB())
      DataSnapshot entry.Data |> Some

    | _ -> None

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<StateMachineFB> =
    let mkOffset tipe value =
      StateMachineFB.StartStateMachineFB(builder)
      StateMachineFB.AddAppEventType(builder, tipe)
      StateMachineFB.AddAppEvent(builder, value)
      StateMachineFB.EndStateMachineFB(builder)

    match self with
    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | AddCue cue ->
      let cuefb = cue.ToOffset(builder)
      AddCueFB.StartAddCueFB(builder)
      AddCueFB.AddCue(builder, cuefb)
      let addfb = AddCueFB.EndAddCueFB(builder)
      mkOffset StateMachineTypeFB.AddCueFB addfb.Value

    | UpdateCue cue ->
      let cuefb = cue.ToOffset(builder)
      UpdateCueFB.StartUpdateCueFB(builder)
      UpdateCueFB.AddCue(builder, cuefb)
      let updatefb = UpdateCueFB.EndUpdateCueFB(builder)
      mkOffset StateMachineTypeFB.UpdateCueFB updatefb.Value

    | RemoveCue cue ->
      let cuefb = cue.ToOffset(builder)
      RemoveCueFB.StartRemoveCueFB(builder)
      RemoveCueFB.AddCue(builder, cuefb)
      let removefb = RemoveCueFB.EndRemoveCueFB(builder)
      mkOffset StateMachineTypeFB.RemoveCueFB removefb.Value

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | AddCueList cuelist ->
      let cuelistfb = cuelist.ToOffset(builder)
      AddCueListFB.StartAddCueListFB(builder)
      AddCueListFB.AddCueList(builder, cuelistfb)
      let addfb = AddCueListFB.EndAddCueListFB(builder)
      mkOffset StateMachineTypeFB.AddCueListFB addfb.Value

    | UpdateCueList cuelist ->
      let cuelistfb = cuelist.ToOffset(builder)
      UpdateCueListFB.StartUpdateCueListFB(builder)
      UpdateCueListFB.AddCueList(builder, cuelistfb)
      let updatefb = UpdateCueListFB.EndUpdateCueListFB(builder)
      mkOffset StateMachineTypeFB.UpdateCueListFB updatefb.Value

    | RemoveCueList cuelist ->
      let cuelistfb = cuelist.ToOffset(builder)
      RemoveCueListFB.StartRemoveCueListFB(builder)
      RemoveCueListFB.AddCueList(builder, cuelistfb)
      let removefb = RemoveCueListFB.EndRemoveCueListFB(builder)
      mkOffset StateMachineTypeFB.RemoveCueListFB removefb.Value

    //  ____       _       _
    // |  _ \ __ _| |_ ___| |__
    // | |_) / _` | __/ __| '_ \
    // |  __/ (_| | || (__| | | |
    // |_|   \__,_|\__\___|_| |_|

    | AddPatch patch ->
      let patchfb = patch.ToOffset(builder)
      AddPatchFB.StartAddPatchFB(builder)
      AddPatchFB.AddPatch(builder, patchfb)
      let addfb = AddPatchFB.EndAddPatchFB(builder)
      mkOffset StateMachineTypeFB.AddPatchFB addfb.Value

    | UpdatePatch patch ->
      let patchfb = patch.ToOffset(builder)
      UpdatePatchFB.StartUpdatePatchFB(builder)
      UpdatePatchFB.AddPatch(builder, patchfb)
      let updatefb = UpdatePatchFB.EndUpdatePatchFB(builder)
      mkOffset StateMachineTypeFB.UpdatePatchFB updatefb.Value

    | RemovePatch patch ->
      let patchfb = patch.ToOffset(builder)
      RemovePatchFB.StartRemovePatchFB(builder)
      RemovePatchFB.AddPatch(builder, patchfb)
      let removefb = RemovePatchFB.EndRemovePatchFB(builder)
      mkOffset StateMachineTypeFB.RemovePatchFB removefb.Value

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    | AddIOBox iobox ->
      let ioboxfb = iobox.ToOffset(builder)
      AddIOBoxFB.StartAddIOBoxFB(builder)
      AddIOBoxFB.AddIOBox(builder, ioboxfb)
      let addfb = AddIOBoxFB.EndAddIOBoxFB(builder)
      mkOffset StateMachineTypeFB.AddIOBoxFB addfb.Value

    | UpdateIOBox iobox ->
      let ioboxfb = iobox.ToOffset(builder)
      UpdateIOBoxFB.StartUpdateIOBoxFB(builder)
      UpdateIOBoxFB.AddIOBox(builder, ioboxfb)
      let updatefb = UpdateIOBoxFB.EndUpdateIOBoxFB(builder)
      mkOffset StateMachineTypeFB.UpdateIOBoxFB updatefb.Value

    | RemoveIOBox iobox ->
      let ioboxfb = iobox.ToOffset(builder)
      RemoveIOBoxFB.StartRemoveIOBoxFB(builder)
      RemoveIOBoxFB.AddIOBox(builder, ioboxfb)
      let removefb = RemoveIOBoxFB.EndRemoveIOBoxFB(builder)
      mkOffset StateMachineTypeFB.RemoveIOBoxFB removefb.Value

    //  ____        __ _   _   _           _
    // |  _ \ __ _ / _| |_| \ | | ___   __| | ___
    // | |_) / _` | |_| __|  \| |/ _ \ / _` |/ _ \
    // |  _ < (_| |  _| |_| |\  | (_) | (_| |  __/
    // |_| \_\__,_|_|  \__|_| \_|\___/ \__,_|\___|

    | AddNode node ->
      let nodefb = node.ToOffset(builder)
      AddNodeFB.StartAddNodeFB(builder)
      AddNodeFB.AddNode(builder, nodefb)
      let addfb = AddNodeFB.EndAddNodeFB(builder)
      mkOffset StateMachineTypeFB.AddNodeFB addfb.Value

    | UpdateNode node ->
      let nodefb = node.ToOffset(builder)
      UpdateNodeFB.StartUpdateNodeFB(builder)
      UpdateNodeFB.AddNode(builder, nodefb)
      let updatefb = UpdateNodeFB.EndUpdateNodeFB(builder)
      mkOffset StateMachineTypeFB.UpdateNodeFB updatefb.Value

    | RemoveNode node ->
      let nodefb = node.ToOffset(builder)
      RemoveNodeFB.StartRemoveNodeFB(builder)
      RemoveNodeFB.AddNode(builder, nodefb)
      let removefb = RemoveNodeFB.EndRemoveNodeFB(builder)
      mkOffset StateMachineTypeFB.RemoveNodeFB removefb.Value

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | AddUser user ->
      let userfb = user.ToOffset(builder)
      AddUserFB.StartAddUserFB(builder)
      AddUserFB.AddUser(builder, userfb)
      let addfb = AddUserFB.EndAddUserFB(builder)
      mkOffset StateMachineTypeFB.AddUserFB addfb.Value

    | UpdateUser user ->
      let userfb = user.ToOffset(builder)
      UpdateUserFB.StartUpdateUserFB(builder)
      UpdateUserFB.AddUser(builder, userfb)
      let updatefb = UpdateUserFB.EndUpdateUserFB(builder)
      mkOffset StateMachineTypeFB.UpdateUserFB updatefb.Value

    | RemoveUser user ->
      let userfb = user.ToOffset(builder)
      RemoveUserFB.StartRemoveUserFB(builder)
      RemoveUserFB.AddUser(builder, userfb)
      let removefb = RemoveUserFB.EndRemoveUserFB(builder)
      mkOffset StateMachineTypeFB.RemoveUserFB removefb.Value

    //  ____                _
    // / ___|  ___  ___ ___(_) ___  _ __
    // \___ \ / _ \/ __/ __| |/ _ \| '_ \
    //  ___) |  __/\__ \__ \ | (_) | | | |
    // |____/ \___||___/___/_|\___/|_| |_|

    | AddSession session ->
      let sessionfb = session.ToOffset(builder)
      AddSessionFB.StartAddSessionFB(builder)
      AddSessionFB.AddSession(builder, sessionfb)
      let addfb = AddSessionFB.EndAddSessionFB(builder)
      mkOffset StateMachineTypeFB.AddSessionFB addfb.Value

    | UpdateSession session ->
      let sessionfb = session.ToOffset(builder)
      UpdateSessionFB.StartUpdateSessionFB(builder)
      UpdateSessionFB.AddSession(builder, sessionfb)
      let updatefb = UpdateSessionFB.EndUpdateSessionFB(builder)
      mkOffset StateMachineTypeFB.UpdateSessionFB updatefb.Value

    | RemoveSession session ->
      let sessionfb = session.ToOffset(builder)
      RemoveSessionFB.StartRemoveSessionFB(builder)
      RemoveSessionFB.AddSession(builder, sessionfb)
      let removefb = RemoveSessionFB.EndRemoveSessionFB(builder)
      mkOffset StateMachineTypeFB.RemoveSessionFB removefb.Value

    //  __  __ _
    // |  \/  (_)___  ___
    // | |\/| | / __|/ __|
    // | |  | | \__ \ (__
    // |_|  |_|_|___/\___|

    | Command ev ->
      let cmdfb = ev.ToOffset(builder)
      mkOffset StateMachineTypeFB.AppCommandFB cmdfb.Value

    | LogMsg(level, msg) ->
      let level = string level |> builder.CreateString
      let msg = msg |> builder.CreateString
      let log = LogMsgFB.CreateLogMsgFB(builder, level, msg)
      mkOffset StateMachineTypeFB.LogMsgFB log.Value

    | DataSnapshot str ->
      let data = builder.CreateString str
      DataSnapshotFB.StartDataSnapshotFB(builder)
      DataSnapshotFB.AddData(builder, data)
      let snapshot = DataSnapshotFB.EndDataSnapshotFB(builder)
      mkOffset StateMachineTypeFB.DataSnapshotFB snapshot.Value

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes (bytes: byte array) : StateMachine option =
    let msg = StateMachineFB.GetRootAsStateMachineFB(new ByteBuffer(bytes))
    StateMachine.FromFB(msg)

#endif

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

#if JAVASCRIPT

  member self.ToJson () = toJson self

  static member FromJson (str: string) : StateMachine option =
    try
      ofJson<StateMachine> str
      |> Some
    with
      | _ -> None

#else

  member self.ToJToken () =
    let json = new JObject() |> addType StateMachine.Type

    let inline add (case: string) data =
      json |> addCase case |> addFields [| data |]

    match self with
    // NODE
    | AddNode          node -> add "AddNode"    node
    | UpdateNode       node -> add "UpdateNode" node
    | RemoveNode       node -> add "RemoveNode" node

    // PATCH
    | AddPatch        patch -> add "AddPatch"    patch
    | UpdatePatch     patch -> add "UpdatePatch" patch
    | RemovePatch     patch -> add "RemovePatch" patch

    // IOBOX
    | AddIOBox        iobox -> add "AddIOBox"    iobox
    | UpdateIOBox     iobox -> add "UpdateIOBox" iobox
    | RemoveIOBox     iobox -> add "RemoveIOBox" iobox

    // CUE
    | AddCue            cue -> add "AddCue"    cue
    | UpdateCue         cue -> add "UpdateCue" cue
    | RemoveCue         cue -> add "RemoveCue" cue

    // CUELIST
    | AddCueList    cuelist -> add "AddCueList"    cuelist
    | UpdateCueList cuelist -> add "UpdateCueList" cuelist
    | RemoveCueList cuelist -> add "RemoveCueList" cuelist

    // USER
    | AddUser          user -> add "AddUser"    user
    | UpdateUser       user -> add "UpdateUser" user
    | RemoveUser       user -> add "RemoveUser" user

    // SESSION
    | AddSession    session -> add "AddSession"    session
    | UpdateSession session -> add "UpdateSession" session
    | RemoveSession session -> add "RemoveSession" session

    | Command           cmd -> add "Command" cmd

    | DataSnapshot     data -> add "DataSnapshot" (Wrap data)

    | LogMsg (level, str) ->
      json |> addCase "LogMsg" |> addFields [| Wrap(string level); Wrap(str) |]

  member self.ToJson () =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : StateMachine option =
    try
      let fields = token.["Fields"] :?> JArray

      let inline parseSingle (cnst: ^t -> StateMachine) =
        Json.parse fields.[0]
        |> Option.map cnst

      match string token.["Case"] with
      // NODE
      | "AddNode"       -> parseSingle AddNode
      | "UpdateNode"    -> parseSingle UpdateNode
      | "RemoveNode"    -> parseSingle RemoveNode

      | "AddPatch"      -> parseSingle AddPatch
      | "UpdatePatch"   -> parseSingle UpdatePatch
      | "RemovePatch"   -> parseSingle RemovePatch

      | "AddIOBox"      -> parseSingle AddIOBox
      | "UpdateIOBox"   -> parseSingle UpdateIOBox
      | "RemoveIOBox"   -> parseSingle RemoveIOBox

      | "AddCue"        -> parseSingle AddCue
      | "UpdateCue"     -> parseSingle UpdateCue
      | "RemoveCue"     -> parseSingle RemoveCue

      | "AddCueList"    -> parseSingle AddCueList
      | "UpdateCueList" -> parseSingle UpdateCueList
      | "RemoveCueList" -> parseSingle RemoveCueList

      | "AddUser"       -> parseSingle AddUser
      | "UpdateUser"    -> parseSingle UpdateUser
      | "RemoveUser"    -> parseSingle RemoveUser

      | "AddSession"    -> parseSingle AddSession
      | "UpdateSession" -> parseSingle UpdateSession
      | "RemoveSession" -> parseSingle RemoveSession

      | "Command"       -> parseSingle Command

      | "DataSnapshot"  -> DataSnapshot (string fields.[0]) |> Some

      | "LogMsg" ->
        Json.parse fields.[0]
        |> Option.map (fun level -> LogMsg (level,string fields.[1]))

      | _ -> None
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : StateMachine option =
    JToken.Parse(str) |> StateMachine.FromJToken

#endif
