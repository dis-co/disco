namespace Iris.Core.Types

open WebSharper

[<AutoOpen>]
[<JavaScript>]
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
    | [<Constant "patch.add">]    AddPatch
    | [<Constant "patch.update">] UpdatePatch
    | [<Constant "patch.remove">] RemovePatch
    | [<Constant "iobox.add">]    AddIOBox
    | [<Constant "iobox.update">] UpdateIOBox
    | [<Constant "iobox.remove">] RemoveIOBox

  type ApiMessage =
    {
      [<Name "type">]    Type    : ApiAction;
      [<Name "payload">] Payload : obj;
    }

  (*---------------------------------------------------------------------------*
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
    |                  |               |                        |               |                  |     
    |                  |               |                        |               |                  |     
    |                  |               |                        |               |                  |     
    +------------------+               +------------------------+               +------------------+     

    *--------------------------------------------------------------------------*)

  type ClientAction =
    | [<Constant "initialized">]  Initialized  // the worker has created a session for this tab/window
    | [<Constant "close">]        Close        // client tab/window was closed, so remove session
    | [<Constant "closed">]       Closed       // client tab/window was closed notification
    | [<Constant "log">]          Log          // logs a piece of data to all connected clients
    | [<Constant "connected">]    Connected    // worker websocket is connected to service
    | [<Constant "disconnected">] Disconnected // worker websocket was disconnected from service
    | [<Constant "error">]        Error        // an error occuring inside the worker
    | [<Constant "render">]       Render       // instruct all clients to render new state
    | [<Constant "update">]       Update       // update state in worker and notify all others

  type ClientMessage =
    {
      [<Name "type">]    Type    : ClientAction;
      [<Name "payload">] Payload : obj option;
    }
