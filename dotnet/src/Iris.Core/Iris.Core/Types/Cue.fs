namespace Iris.Core.Types

[<AutoOpen>]
module Cue =

  type Cue =
    {
      Id      : string;
      Name    : string;
      IOBoxes : IOBox array;
    }
