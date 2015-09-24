[<ReflectedDefinition>]
module Iris.Core.Types.Patch

open Iris.Core.Types.Aliases
open Iris.Core.Types.IOBox

type Patch = {
    Id       : IrisId;
    NodePath : NodePath;
    HostIP   : IrisIP;
    HostId   : IrisId;
    HostName : Name;
    Name     : Name;
    FilePath : FilePath;
    IOBoxes  : IOBox list;
  }
