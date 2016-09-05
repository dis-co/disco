namespace Iris.Core

open Iris.Raft

#if JAVASCRIPT
#else

open Iris.Serialization.Raft
open FlatBuffers

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

  with
#if JAVASCRIPT
#else
    static member FromFB (fb: AppCommandFB) =
      match fb.Command with
      | AppCommandTypeFB.UndoFB  -> Some Undo
      | AppCommandTypeFB.RedoFB  -> Some Redo
      | AppCommandTypeFB.ResetFB -> Some Reset
      | _                        -> None

    member self.ToOffset(builder: FlatBufferBuilder) : Offset<AppCommandFB> =
      let tipe =
        match self with
        | Undo  -> AppCommandTypeFB.UndoFB
        | Redo  -> AppCommandTypeFB.RedoFB
        | Reset -> AppCommandTypeFB.ResetFB

      AppCommandFB.StartAppCommandFB(builder)
      AppCommandFB.AddCommand(builder, tipe)
      AppCommandFB.EndAppCommandFB(builder)
#endif

type ApplicationEvent =
  // PROJECT
  // | OpenProject
  // | SaveProject
  // | CreateProject
  // | CloseProject
  // | DeleteProject

  // CLIENT
  // | AddClient    of string
  // | UpdateClient of string
  // | RemoveClient of string

  // NODE
  // | AddNode      of string
  // | UpdateNode   of string
  // | RemoveNode   of string

  // PATCH
  // | AddPatch    of Patch
  // | UpdatePatch of Patch
  // | RemovePatch of Patch

  // IOBOX
  // | AddIOBox    of IOBox
  // | UpdateIOBox of IOBox
  // | RemoveIOBox of IOBox

  // CUE
  | AddCue      of Cue
  | UpdateCue   of Cue
  | RemoveCue   of Cue

  | Command     of AppCommand

  | LogMsg      of LogLevel * string

  with

    override self.ToString() : string =
      match self with
      // PROJECT
      // | OpenProject   -> "OpenProject"
      // | SaveProject   -> "SaveProject"
      // | CreateProject -> "CreateProject"
      // | CloseProject  -> "CloseProject"
      // | DeleteProject -> "DeleteProject"

      // CLIENT
      // | AddClient    s -> sprintf "AddClient %s"    s
      // | UpdateClient s -> sprintf "UpdateClient %s" s
      // | RemoveClient s -> sprintf "RemoveClient %s" s

      // NODE
      // | AddNode    s -> sprintf "AddNode %s" s
      // | UpdateNode s -> sprintf "UpdateNode %s" s
      // | RemoveNode s -> sprintf "RemoveNode %s" s

      // PATCH
      // | AddPatch    patch -> sprintf "AddPatch %s"    (string patch)
      // | UpdatePatch patch -> sprintf "UpdatePatch %s" (string patch)
      // | RemovePatch patch -> sprintf "RemovePatch %s" (string patch)

      // IOBOX
      // | AddIOBox    iobox -> sprintf "AddIOBox %s"    (string iobox)
      // | UpdateIOBox iobox -> sprintf "UpdateIOBox %s" (string iobox)
      // | RemoveIOBox iobox -> sprintf "RemoveIOBox %s" (string iobox)

      // CUE
      | AddCue    cue      -> sprintf "AddCue %s"    (string cue)
      | UpdateCue cue      -> sprintf "UpdateCue %s" (string cue)
      | RemoveCue cue      -> sprintf "RemoveCue %s" (string cue)
      | Command    ev      -> sprintf "Command: %s"  (string ev)
      | LogMsg(level, msg) -> sprintf "LogMsg: [%A] %s" level msg

#if JAVASCRIPT
#else
    static member FromFB (fb: ApplicationEventFB) =
      match fb.AppEventType with
      | ApplicationEventTypeFB.AddCueFB ->
        let ev = fb.GetAppEvent(new AddCueFB())
        ev.GetCue(new CueFB())
        |> Cue.FromFB
        |> Option.map AddCue

      | ApplicationEventTypeFB.UpdateCueFB  ->
        let ev = fb.GetAppEvent(new UpdateCueFB())
        ev.GetCue(new CueFB())
        |> Cue.FromFB
        |> Option.map UpdateCue

      | ApplicationEventTypeFB.RemoveCueFB  ->
        let ev = fb.GetAppEvent(new RemoveCueFB())
        ev.GetCue(new CueFB())
        |> Cue.FromFB
        |> Option.map RemoveCue

      | ApplicationEventTypeFB.LogMsgFB     ->
        let ev = fb.GetAppEvent(new LogMsgFB())
        let level = LogLevel.Parse ev.LogLevel
        LogMsg(level, ev.Msg) |> Some

      | ApplicationEventTypeFB.AppCommandFB ->
        let ev = fb.GetAppEvent(new AppCommandFB())
        AppCommand.FromFB ev
        |> Option.map Command

      | _ -> None

    member self.ToOffset(builder: FlatBufferBuilder) : Offset<ApplicationEventFB> =
      let mkOffset tipe value =
        ApplicationEventFB.StartApplicationEventFB(builder)
        ApplicationEventFB.AddAppEventType(builder, tipe)
        ApplicationEventFB.AddAppEvent(builder, value)
        ApplicationEventFB.EndApplicationEventFB(builder)

      match self with
      | AddCue cue ->
        let cuefb = cue.ToOffset(builder)
        AddCueFB.StartAddCueFB(builder)
        AddCueFB.AddCue(builder, cuefb)
        let addfb = AddCueFB.EndAddCueFB(builder)
        mkOffset ApplicationEventTypeFB.AddCueFB addfb.Value

      | UpdateCue cue ->
        let cuefb = cue.ToOffset(builder)
        UpdateCueFB.StartUpdateCueFB(builder)
        UpdateCueFB.AddCue(builder, cuefb)
        let updatefb = UpdateCueFB.EndUpdateCueFB(builder)
        mkOffset ApplicationEventTypeFB.UpdateCueFB updatefb.Value

      | RemoveCue cue ->
        let cuefb = cue.ToOffset(builder)
        RemoveCueFB.StartRemoveCueFB(builder)
        RemoveCueFB.AddCue(builder, cuefb)
        let removefb = RemoveCueFB.EndRemoveCueFB(builder)
        mkOffset ApplicationEventTypeFB.RemoveCueFB removefb.Value

      | Command ev ->
        let cmdfb = ev.ToOffset(builder)
        mkOffset ApplicationEventTypeFB.AppCommandFB cmdfb.Value

      | LogMsg(level, msg) ->
        let level = string level |> builder.CreateString
        let msg = msg |> builder.CreateString
        let log = LogMsgFB.CreateLogMsgFB(builder, level, msg)
        mkOffset ApplicationEventTypeFB.LogMsgFB log.Value

    member self.ToBytes () =
      let builder = new FlatBufferBuilder(1)
      let offset = self.ToOffset(builder)
      builder.Finish(offset.Value)
      builder.SizedByteArray()

    static member FromBytes (bytes: byte array) : ApplicationEvent option =
      let msg = ApplicationEventFB.GetRootAsApplicationEventFB(new ByteBuffer(bytes))
      ApplicationEvent.FromFB(msg)

#endif


#if JAVASCRIPT

  (*
    ____ _ _            _
   / ___| (_) ___ _ __ | |_
  | |   | | |/ _ \ '_ \| __|
  | |___| | |  __/ | | | |_
   \____|_|_|\___|_| |_|\__|

  The client state machine:

        Window 1                        SharedWorker                             Window 2
  +------------------+               +------------------------+               +------------------+
  | Create Worker    |-------------->| make id, save port[0]  |<--------------| Create Worker    |
  |                  | "initialized" |     |                  | "initialized" |                  |
  | save session id  |<--------------|-----+------------------|-------------->| save session id  |
  |                  |               |                        |               |                  |
  |                  |   "close"     |                        |  "closed"     |                  |
  | User closes tab  |-------------->|    removes session     |-------------->| Notified of Close|
  |                  |               |                        |               |                  |
  |                  |    "log"      |                        |   "log"       |                  |
  | console.log      |<--------------|         Log            |-------------->| console.log      |
  |                  |               |                        |               |                  |
  |                  |  "connect"    |                        |  "connect"    |                  |
  |                  |-------------->|      Connect           |<------------- |                  |
  |                  |               |                        |               |                  |
  |                  |  "connected"  |                        |  "connected"  |                  |
  |                  |<--------------|      Connected         |-------------->|                  |
  |                  |               |                        |               |                  |
  |                  | "disconnected"|                        |"disconnected" |                  |
  |                  |<--------------|     Disconnected       |-------------->|                  |
  |                  |               |                        |               |                  |
  |                  |  "error"      |                        |  "error"      |                  |
  |   Handle Error   |<--------------|        Error           |-------------->|   Handle Error   |
  |                  |               |                        |               |                  |
  |                  |   "render"    |                        |  "render"     |                  |
  |   Updates view   |<--------------|       Render           |-------------->|   Updates view   |
  |                  |               |                        |               |                  |
  |                  |   "update"    |                        |  "render"     |                  |
  |    User Edits    |-------------->|    Updates State       |-------------->|   Updates view   |
  |                  |               |                        |               |                  |
  +------------------+               +------------------------+               +------------------+

  *)

[<RequireQualifiedAccess>]
type ClientMessage<'state> =
  | Initialized  of Session            // the worker has created a session for this tab/window
  | Close        of Session            // client tab/window was closed, so request to remove session
  | Closed       of Session            // other client tab/window notified of close
  | Stop                               // SharedWorker is requested to stop
  | Stopped                            // SharedWorker process has stopped
  | ClientLog    of ClientLog          // logs a piece of data to all connected clients
  | Error        of Error              // an error occuring inside the worker
  | Render       of 'state             // instruct all clients to render new state
  | Event        of Session * AppEvent // encapsulates an action or event that happened on the client
  | Connect      of string             // Connect to the specified endpoint
  | Connected                          // worker websocket is connected to service
  | Disconnect   of string             // Disconnect from server
  | Disconnected                       // worker websocket was disconnected from service

#endif
