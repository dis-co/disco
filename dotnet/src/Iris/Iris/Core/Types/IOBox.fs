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

    member self.StringValue
      with get () =
        match self with
          | StringSlice(_,value) -> value
          | _                    -> failwith "no a string slice"

    member self.IntValue
      with get () =
        match self with
          | IntSlice(_,value) -> value
          | _                 -> failwith "no an int slice"

    member self.FloatValue
      with get () =
        match self with
          | FloatSlice(_,value) -> value
          | _                   -> failwith "no a float slice"

    member self.DoubleValue
      with get () =
        match self with
          | DoubleSlice(_,value) -> value
          | _                    -> failwith "no a double slice"

    member self.BoolValue
      with get () =
        match self with
          | BoolSlice(_,value) -> value
          | _                  -> failwith "no a bool slice"

    static member index (slice: Slice) = slice.Index

    static member stringValue (slice: Slice) = slice.StringValue
    static member intValue    (slice: Slice) = slice.IntValue
    static member floatValue  (slice: Slice) = slice.FloatValue
    static member doubleValue (slice: Slice) = slice.DoubleValue
    static member boolValue   (slice: Slice) = slice.BoolValue

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
