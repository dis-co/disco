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

  (*----------------------------------------------------------------------------
    
      ____ _ _            _   
     / ___| (_) ___ _ __ | |_ 
    | |   | | |/ _ \ '_ \| __|
    | |___| | |  __/ | | | |_ 
     \____|_|_|\___|_| |_|\__|
                              
   ---------------------------------------------------------------------------*)
    
  type ClientAction =
    | [<Constant "log">]              Log
    | [<Constant "add">]              Add
    | [<Constant "update">]           Update
    | [<Constant "remove">]           Remove
    | [<Constant "render">]           Render
    | [<Constant "connected">]        Connected
    | [<Constant "disconnected">]     Disconnected
    | [<Constant "connection-error">] ConnectionError

  type ClientMessage =
    {
      [<Name "type">]    Type    : ClientAction;
      [<Name "payload">] Payload : obj option;
    }
