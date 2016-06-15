namespace Iris.Core.Types

[<AutoOpen>]
module Message =

  (*----------------------------------------------------------------------------
         _          _
        / \   _ __ (_)      Types for modeling communication between nodes
       / _ \ | '_ \| |      on the network layer.
      / ___ \| |_) | |
     /_/   \_\ .__/|_|
             |_|
   ---------------------------------------------------------------------------*)

  type ApiAction =
    | AddPatch
    | UpdatePatch
    | RemovePatch
    | AddIOBox
    | UpdateIOBox
    | RemoveIOBox

  type ApiMessage =
    {
      Type    : ApiAction;
      Payload : obj;
    }

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

  type Crud =
    | Create
    | Read
    | Update
    | Delete
  
  type AppEventT =
    | Initialize
    | Save
    | Undo
    | Redo
    | Reset

  type AppEvent =
    | AppEvent   of AppEventT
    | IOBoxEvent of Crud * IOBox
    | PatchEvent of Crud * Patch
    | CueEvent   of Crud * Cue option
    | UnknownEvent

    with override self.ToString() : string =
                  match self with
                    | AppEvent(t) ->
                      match t with
                        | AppEventT.Initialize  -> "AppEvent(Initialize)"
                        | AppEventT.Save        -> "AppEvent(Save)"
                        | AppEventT.Undo        -> "AppEvent(Undo)"
                        | AppEventT.Redo        -> "AppEvent(Redo)"
                        | AppEventT.Reset       -> "AppEvent(Reset)"

                    | IOBoxEvent(t,_) -> 
                      match t with
                        | Create -> "IOBoxEvent(Create)"
                        | Delete -> "IOBoxEvent(Delete)"
                        | Update -> "IOBoxEvent(Update)"
                        | Read   -> "IOBoxEvent(Read)"

                    | PatchEvent(t,_) -> 
                      match t with
                        | Create -> "PatchEvent(Create)" 
                        | Delete -> "PatchEvent(Delete)"
                        | Update -> "PatchEvent(Update)"
                        | Read   -> "PatchEvent(Read)"

                    | CueEvent(t,_) -> 
                      match t with
                        | Create -> "CueEvent(Create)" 
                        | Delete -> "CueEvent(Delete)"
                        | Update -> "CueEvent(Update)"
                        | Read   -> "CueEvent(Read)"

                    | UnknownEvent -> "UnknownEvent"
      
  (*---------------
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

    *--------------------------------------------------------------------------*)
  type Session = string
  type Error = string

  [<RequireQualifiedAccess>]
  type ClientMessage<'state> =
    | Initialized of Session            // the worker has created a session for this tab/window
    | Close       of Session            // client tab/window was closed, so request to remove session
    | Closed      of Session            // other client tab/window notified of close
    | Stop                              // SharedWorker is requested to stop
    | Stopped                           // SharedWorker process has stopped
    | Undo                              // Undo last step 
    | Redo                              // Redo last undo step
    | Save                              // Save current state
    | Open                              // Open a project
    | Log         of obj                // logs a piece of data to all connected clients
    | Error       of Error              // an error occuring inside the worker
    | Render      of 'state             // instruct all clients to render new state
    | Event       of Session * AppEvent // encapsulates an action or event that happened on the client
    | Connected                         // worker websocket is connected to service
    | Disconnected                      // worker websocket was disconnected from service


  type SessionId = string

  type WsMsg =
    | Broadcast        of string
    | Multicast        of SessionId * string
    | ClientDisconnect of SessionId

    with
      override self.ToString() =
        match self with
          | Broadcast(str)        -> sprintf "Broadcast: %s" str
          | Multicast(ses, str)   -> sprintf "Multicast %s %s" ses str
          | ClientDisconnect(str) -> sprintf "ClientDisconnect %s" str
