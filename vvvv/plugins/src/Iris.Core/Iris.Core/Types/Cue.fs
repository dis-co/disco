namespace Iris.Core.Types

open WebSharper

[<AutoOpen>]
[<ReflectedDefinition>]
module Cue =

  type Cue =
    {
      [<Name "id">]      Id      : string;
      [<Name "name">]    Name    : string;
      [<Name "ioboxes">] IOBoxes : IOBox array;
    }
