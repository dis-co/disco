namespace Iris.Core

[<RequireQualifiedAccess>]
type Behavior =
  | Slider
  | Toggle
  | Bang

[<RequireQualifiedAccess>]
type ValType =
  | Real
  | Int
  | Bool

[<RequireQualifiedAccess>]
type StringType =
  | Simple
  | MultiLine
  | FileName
  | Directory
  | Url
  | IP

type Slice =
  | IntSlice    of index: int * value: int
  | StringSlice of index: int * value: string
  | FloatSlice  of index: int * value: float
  | DoubleSlice of index: int * value: double
  | BoolSlice   of index: int * value: bool

  with
    member self.Index
      with get () =
        match self with
          | IntSlice(idx,_)    -> idx
          | StringSlice(idx,_) -> idx
          | FloatSlice(idx,_)  -> idx
          | DoubleSlice(idx,_) -> idx
          | BoolSlice(idx,_)   -> idx

    static member index (slice: Slice) = slice.Index

[<RequireQualifiedAccess>]
type PinType =
  | Value
  | String
  | Color 
  | Enum
  | Node

[<NoComparison>]
type IOBox =
  { Id         : string
  ; Name       : string
  ; Type       : PinType
  ; Patch      : string
  ; Tag        : obj        option
  ; Behavior   : Behavior   option
  ; VecSize    : VectorSize
  ; Min        : Min
  ; Max        : Max
  ; Unit       : Unit
  ; Precision  : Precision
  ; StringType : StringType option
  ; FileMask   : FileMask
  ; MaxChars   : MaxChars
  ; Properties : Properties
  ; Slices     : Slice array
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
