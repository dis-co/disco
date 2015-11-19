namespace Iris.Core.Types

open WebSharper

[<AutoOpen>]
[<JavaScript>]
module Message =

  type Action =
    | [<Constant "patch.add">]    AddPatch
    | [<Constant "patch.update">] UpdatePatch
    | [<Constant "patch.remove">] RemovePatch
    | [<Constant "iobox.add">]    AddIOBox
    | [<Constant "iobox.update">] UpdateIOBox
    | [<Constant "iobox.remove">] RemoveIOBox

  type Message =
    {
      [<Name "type">]    Type    : Action;
      [<Name "payload">] Payload : obj;
    }
