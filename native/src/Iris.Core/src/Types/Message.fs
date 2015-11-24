namespace Iris.Core.Types

open WebSharper

[<AutoOpen>]
[<JavaScript>]
module Message =

  type ApiAction =
    | [<Constant "patch.add">]    AddPatch
    | [<Constant "patch.update">] UpdatePatch
    | [<Constant "patch.remove">] RemovePatch
    | [<Constant "iobox.add">]    AddIOBox
    | [<Constant "iobox.update">] UpdateIOBox
    | [<Constant "iobox.remove">] RemoveIOBox

    
  type ClientAction =
    | [<Constant "log">]              Log
    | [<Constant "add">]              Add
    | [<Constant "update">]           Update
    | [<Constant "remove">]           Remove
    | [<Constant "render">]           Render
    | [<Constant "connected">]        Connected
    | [<Constant "disconnected">]     Disconnected
    | [<Constant "connection-error">] ConnectionError

  type Message =
    {
      [<Name "type">]    Type    : ApiAction;
      [<Name "payload">] Payload : obj;
    }

  type ClientEvent =
    {
      [<Name "type">]    Type    : ClientAction;
      [<Name "payload">] Payload : obj;
    }
