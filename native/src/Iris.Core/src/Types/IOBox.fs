namespace Iris.Core.Types

open System.Runtime.Serialization
open System.Runtime.Serialization.Json
open WebSharper

[<AutoOpen>]
[<ReflectedDefinition>]
module IOBox =

  [<RequireQualifiedAccess>]
  type Behavior =
    | [<Constant "slider">] Slider
    | [<Constant "toggle">] Toggle
    | [<Constant "bang">]   Bang

  [<RequireQualifiedAccess>]
  type ValType =
    | [<Constant "real">] Real
    | [<Constant "int">]  Int
    | [<Constant "bool">] Bool

  [<RequireQualifiedAccess>]
  type StringType =
    | [<Constant "string">]    Simple
    | [<Constant "multi">]     MultiLine
    | [<Constant "file">]      FileName
    | [<Constant "directory">] Directory
    | [<Constant "url">]       Url
    | [<Constant "ip">]        IP

  [<NoEquality; NoComparison>]
  type Slice =
    {
      [<Name "idx">]   Idx   : int;
      [<Name "value">] Value : string;
    }

  [<RequireQualifiedAccess>]
  type PinType =
    | [<Constant "value">]  Value
    | [<Constant "string">] String
    | [<Constant "color">]  Color 
    | [<Constant "enum">]   Enum
    | [<Constant "node">]   Node

  [<NoEquality; NoComparison>]
  type IOBox =
    {
      [<Name "id">]          Id         : string;
      [<Name "name">]        Name       : string;
      [<Name "type">]        Type       : PinType;
      [<Name "patch">]       Patch      : string;
      [<Name "tag">]         Tag        : obj        option;
      [<Name "behavior">]    Behavior   : Behavior   option;
      [<Name "vecsize">]     VecSize    : VectorSize
      [<Name "min">]         Min        : Min
      [<Name "max">]         Max        : Max
      [<Name "unit">]        Unit       : Unit
      [<Name "precision">]   Precision  : Precision
      [<Name "string-type">] StringType : StringType option
      [<Name "filemask">]    FileMask   : FileMask
      [<Name "maxchars">]    MaxChars   : MaxChars
      [<Name "properties">]  Properties : Properties
      [<Name "slices">]      Slices     : Slice array;
    }
    with
      static member StringBox(id, name, patch) =
        { Id         = id
        ; Name       = name
        ; Type       = PinType.String
        ; Patch      = patch
        ; Tag        = None
        ; Behavior   = None
        ; VecSize    = None
        ; Min        = None
        ; Max        = None
        ; Unit       = None
        ; Precision  = None
        ; StringType = Some(StringType.Simple)
        ; FileMask   = None
        ; MaxChars   = None
        ; Properties = Array.empty
        ; Slices     = Array.empty
        }

      static member ValueBox(id, name, patch) =
        { Id         = id
        ; Name       = name
        ; Type       = PinType.Value
        ; Patch      = patch
        ; Tag        = None
        ; Behavior   = Some(Behavior.Slider)
        ; VecSize    = None
        ; Min        = None
        ; Max        = None
        ; Unit       = None
        ; Precision  = None
        ; StringType = None
        ; FileMask   = None
        ; MaxChars   = None
        ; Properties = Array.empty
        ; Slices     = Array.empty
        }

      static member ColorBox(id, name, patch) =
        { Id         = id
        ; Name       = name
        ; Type       = PinType.Color
        ; Patch      = patch
        ; Tag        = None
        ; Behavior   = None
        ; VecSize    = None
        ; Min        = None
        ; Max        = None
        ; Unit       = None
        ; Precision  = None
        ; StringType = None
        ; FileMask   = None
        ; MaxChars   = None
        ; Properties = Array.empty
        ; Slices     = Array.empty
        }

      static member EnumBox(id, name, patch) =
        { Id         = id
        ; Name       = name
        ; Type       = PinType.Enum
        ; Patch      = patch
        ; Tag        = None
        ; Behavior   = None
        ; VecSize    = None
        ; Min        = None
        ; Max        = None
        ; Unit       = None
        ; Precision  = None
        ; StringType = None
        ; FileMask   = None
        ; MaxChars   = None
        ; Properties = Array.empty
        ; Slices     = Array.empty
        }

      static member NodeBox(id, name, patch) =
        { Id         = id
        ; Name       = name
        ; Type       = PinType.Node
        ; Patch      = patch
        ; Tag        = None
        ; Behavior   = None
        ; VecSize    = None
        ; Min        = None
        ; Max        = None
        ; Unit       = None
        ; Precision  = None
        ; StringType = None
        ; FileMask   = None
        ; MaxChars   = None
        ; Properties = Array.empty
        ; Slices     = Array.empty
        }
