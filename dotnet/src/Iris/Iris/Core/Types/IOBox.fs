namespace Iris.Core

open Fable.Core

[<RequireQualifiedAccess>]
type Behavior =
  | Toggle
  | Bang

type StringType =
  | Simple
  | MultiLine
  | FileName
  | Directory
  | Url
  | IP

//  ___ ___  ____
// |_ _/ _ \| __ )  _____  __
//  | | | | |  _ \ / _ \ \/ /
//  | | |_| | |_) | (_) >  <
// |___\___/|____/ \___/_/\_\

type IOBox =
  | StringBox of StringBoxD
  | IntBox    of IntBoxD
  | FloatBox  of FloatBoxD
  | DoubleBox of DoubleBoxD
  | BoolBox   of BoolBoxD
  | ByteBox   of ByteBoxD
  | EnumBox   of EnumBoxD
  | ColorBox  of ColorBoxD
  | Compound  of CompoundBoxD

  with
    member self.Id
      with get () =
        match self with
          | StringBox data -> data.Id
          | IntBox    data -> data.Id
          | FloatBox  data -> data.Id
          | DoubleBox data -> data.Id
          | BoolBox   data -> data.Id
          | ByteBox   data -> data.Id
          | EnumBox   data -> data.Id
          | ColorBox  data -> data.Id
          | Compound  data -> data.Id

    member self.Name
      with get () =
        match self with
          | StringBox data -> data.Name
          | IntBox    data -> data.Name
          | FloatBox  data -> data.Name
          | DoubleBox data -> data.Name
          | BoolBox   data -> data.Name
          | ByteBox   data -> data.Name
          | EnumBox   data -> data.Name
          | ColorBox  data -> data.Name
          | Compound  data -> data.Name

    member self.SetName name =
      match self with
      | StringBox data -> StringBox { data with Name = name }
      | IntBox    data -> IntBox    { data with Name = name }
      | FloatBox  data -> FloatBox  { data with Name = name }
      | DoubleBox data -> DoubleBox { data with Name = name }
      | BoolBox   data -> BoolBox   { data with Name = name }
      | ByteBox   data -> ByteBox   { data with Name = name }
      | EnumBox   data -> EnumBox   { data with Name = name }
      | ColorBox  data -> ColorBox  { data with Name = name }
      | Compound  data -> Compound  { data with Name = name }
      
    member self.Patch
      with get () =
        match self with
          | StringBox data -> data.Patch
          | IntBox    data -> data.Patch
          | FloatBox  data -> data.Patch
          | DoubleBox data -> data.Patch
          | BoolBox   data -> data.Patch
          | ByteBox   data -> data.Patch
          | EnumBox   data -> data.Patch
          | ColorBox  data -> data.Patch
          | Compound  data -> data.Patch

    member self.Slices
      with get () =
        match self with
          | StringBox data -> StringSlices   data.Slices
          | IntBox    data -> IntSlices      data.Slices
          | FloatBox  data -> FloatSlices    data.Slices
          | DoubleBox data -> DoubleSlices   data.Slices
          | BoolBox   data -> BoolSlices     data.Slices
          | ByteBox   data -> ByteSlices     data.Slices
          | EnumBox   data -> EnumSlices     data.Slices
          | ColorBox  data -> ColorSlices    data.Slices
          | Compound  data -> CompoundSlices data.Slices

    member self.SetSlices slices =
        match self with
          | StringBox data as value ->
            match slices with
            | StringSlices arr -> StringBox { data with Slices = arr }
            | _ -> value

          | IntBox data as value ->
            match slices with
            | IntSlices arr -> IntBox { data with Slices = arr }  
            | _ -> value

          | FloatBox data as value ->
            match slices with
            | FloatSlices  arr -> FloatBox { data with Slices = arr }   
            | _ -> value

          | DoubleBox data as value ->
            match slices with
            | DoubleSlices arr -> DoubleBox { data with Slices = arr }
            | _ -> value

          | BoolBox data as value ->
            match slices with
            | BoolSlices arr -> BoolBox { data with Slices = arr }
            | _ -> value

          | ByteBox data as value ->
            match slices with
            | ByteSlices arr -> ByteBox { data with Slices = arr }
            | _ -> value
          
          | EnumBox data as value ->
            match slices with
            | EnumSlices arr -> EnumBox { data with Slices = arr }
            | _ -> value

          | ColorBox data as value ->
            match slices with 
            | ColorSlices arr -> ColorBox { data with Slices = arr }
            | _ -> value

          | Compound data as value ->
            match slices with 
            | CompoundSlices arr -> Compound { data with Slices = arr }
            | _ -> value

    static member Toggle(id, name, patch, tags, values) =
      BoolBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; Behavior   = Behavior.Toggle
              ; Slices     = values }

    static member Bang(id, name, patch, tags, values) =
      BoolBox { Id         = id
              ; Name       = name
              ; Patch      = patch
              ; Tags       = tags
              ; Behavior   = Behavior.Bang
              ; Slices     = values }

    static member String(id, name, patch, tags, values) =
      StringBox { Id         = id
                ; Name       = name
                ; Patch      = patch
                ; Tag        = tags
                ; StringType = Simple
                ; FileMask   = None
                ; MaxChars   = sizeof<int>
                ; Slices     = values }

    static member MultiLine(id, name, patch, tags, values) =
      StringBox { Id         = id
                ; Name       = name
                ; Patch      = patch
                ; Tag        = tags
                ; StringType = MultiLine
                ; FileMask   = None
                ; MaxChars   = sizeof<int>
                ; Slices     = values }

    static member FileName(id, name, patch, tags, filemask, values) =
      StringBox { Id         = id
                ; Name       = name
                ; Patch      = patch
                ; Tag        = tags
                ; StringType = FileName
                ; FileMask   = Some filemask
                ; MaxChars   = sizeof<int>
                ; Slices     = values }

    static member Directory(id, name, patch, tags, filemask, values) =
      StringBox { Id         = id
                ; Name       = name
                ; Patch      = patch
                ; Tag        = tags
                ; StringType = Directory
                ; FileMask   = Some filemask
                ; MaxChars   = sizeof<int>
                ; Slices     = values }

    static member Url(id, name, patch, tags, values) =
      StringBox { Id         = id
                ; Name       = name
                ; Patch      = patch
                ; Tag        = tags
                ; StringType = Url
                ; FileMask   = None
                ; MaxChars   = sizeof<int>
                ; Slices     = values }

    static member IP(id, name, patch, tags, values) =
      StringBox { Id         = id
                ; Name       = name
                ; Patch      = patch
                ; Tag        = tags
                ; StringType = Url
                ; FileMask   = None
                ; MaxChars   = sizeof<int>
                ; Slices     = values }

//  ____              _
// | __ )  ___   ___ | |
// |  _ \ / _ \ / _ \| |
// | |_) | (_) | (_) | |
// |____/ \___/ \___/|_|

and BoolBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; Behavior   : Behavior
  ; Slices     : BoolSliceD array }

and BoolSliceD = { Index: Index; Value: bool }

//  ___       _
// |_ _|_ __ | |_
//  | || '_ \| __|
//  | || | | | |_
// |___|_| |_|\__|

and IntBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; VecSize    : uint32
  ; Min        : int
  ; Max        : int
  ; Unit       : string
  ; Slices     : IntSliceD array }

and IntSliceD = { Index: Index; Value: int }

//  _____ _             _
// |  ___| | ___   __ _| |_
// | |_  | |/ _ \ / _` | __|
// |  _| | | (_) | (_| | |_
// |_|   |_|\___/ \__,_|\__|

and FloatBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; VecSize    : uint32
  ; Min        : int
  ; Max        : int
  ; Unit       : string
  ; Precision  : uint32
  ; Slices     : FloatSliceD array }

and FloatSliceD = { Index: Index; Value: float }

//  ____              _     _
// |  _ \  ___  _   _| |__ | | ___
// | | | |/ _ \| | | | '_ \| |/ _ \
// | |_| | (_) | |_| | |_) | |  __/
// |____/ \___/ \__,_|_.__/|_|\___|

and DoubleBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag array
  ; VecSize    : uint32
  ; Min        : int
  ; Max        : int
  ; Unit       : string
  ; Precision  : uint32
  ; Slices     : DoubleSliceD array }

and DoubleSliceD = { Index: Index; Value: double }

//  ____        _
// | __ ) _   _| |_ ___
// |  _ \| | | | __/ _ \
// | |_) | |_| | ||  __/
// |____/ \__, |\__\___|
//        |___/

and ByteBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag        array
  ; Slices     : ByteSliceD array }

and ByteSliceD = { Index: Index; Value: byte array }

//  _____
// | ____|_ __  _   _ _ __ ___
// |  _| | '_ \| | | | '_ ` _ \
// | |___| | | | |_| | | | | | |
// |_____|_| |_|\__,_|_| |_| |_|

and EnumBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag        array
  ; Properties : Property   array
  ; Slices     : EnumSliceD array }

and EnumSliceD = { Index: Index; Value: Property }

//   ____      _
//  / ___|___ | | ___  _ __
// | |   / _ \| |/ _ \| '__|
// | |__| (_) | | (_) | |
//  \____\___/|_|\___/|_|

and ColorBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag         array
  ; Slices     : ColorSliceD array }

and ColorSliceD = { Index: Index; Value: ColorSpace }

//  ____  _        _
// / ___|| |_ _ __(_)_ __   __ _
// \___ \| __| '__| | '_ \ / _` |
//  ___) | |_| |  | | | | | (_| |
// |____/ \__|_|  |_|_| |_|\__, |
//                         |___/

and StringBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tag        : Tag array
  ; StringType : StringType
  ; FileMask   : FileMask option
  ; MaxChars   : MaxChars
  ; Slices     : StringSliceD array }

and StringSliceD =
  { Index      : uint32
  ; Value      : string }

//   ____                                            _
//  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| |
// | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` |
// | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| |
//  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|
//                      |_|

and CompoundBoxD =
  { Id         : Id
  ; Name       : string
  ; Patch      : Id
  ; Tags       : Tag   array
  ; Slices     : CompoundSliceD array }

and CompoundSliceD =
  { Index      : Index
  ; Value      : IOBox }

//  ____  _ _
// / ___|| (_) ___ ___
// \___ \| | |/ __/ _ \
//  ___) | | | (_|  __/
// |____/|_|_|\___\___|

and Slice =
  | StringSlice   of StringSliceD
  | IntSlice      of IntSliceD
  | FloatSlice    of FloatSliceD
  | DoubleSlice   of DoubleSliceD
  | BoolSlice     of BoolSliceD
  | ByteSlice     of ByteSliceD
  | EnumSlice     of EnumSliceD
  | ColorSlice    of ColorSliceD
  | CompoundSlice of CompoundSliceD

  with
    member self.Index
      with get () =
        match self with
        | StringSlice   data -> data.Index
        | IntSlice      data -> data.Index
        | FloatSlice    data -> data.Index
        | DoubleSlice   data -> data.Index
        | BoolSlice     data -> data.Index
        | ByteSlice     data -> data.Index
        | EnumSlice     data -> data.Index
        | ColorSlice    data -> data.Index
        | CompoundSlice data -> data.Index

    member self.StringValue
      with get () =
        match self with
        | StringSlice data -> data.Value
        | _                -> failwith "Slice is not a string value type"

    member self.IntValue
      with get () =
        match self with
        | IntSlice data -> data.Value
        | _             -> failwith "Slice is not an int value type"

    member self.FloatValue
      with get () =
        match self with
        | FloatSlice data -> data.Value
        | _               -> failwith "Slice is not a float value type"

    member self.DoubleValue
      with get () =
        match self with
        | DoubleSlice data -> data.Value
        | _                -> failwith "Slice is not a double value type"

    member self.BoolValue
      with get () =
        match self with
        | BoolSlice data -> data.Value
        | _              -> failwith "Slice is not a boolean value type"

    member self.ByteValue
      with get () =
        match self with
        | ByteSlice data -> data.Value
        | _              -> failwith "Slice is not a byte value type"

    member self.EnumValue
      with get () =
        match self with
        | EnumSlice data -> data.Value
        | _              -> failwith "Slice is not an enum value type"

    member self.ColorValue
      with get () =
        match self with
        | ColorSlice data -> data.Value
        | _               -> failwith "Slice is not a color value type"

    member self.CompoundValue
      with get () =
        match self with
        | CompoundSlice data -> data.Value
        | _                  -> failwith "Slice is not a compound value type"

and Slices =
  | StringSlices   of StringSliceD   array 
  | IntSlices      of IntSliceD      array
  | FloatSlices    of FloatSliceD    array
  | DoubleSlices   of DoubleSliceD   array
  | BoolSlices     of BoolSliceD     array
  | ByteSlices     of ByteSliceD     array
  | EnumSlices     of EnumSliceD     array
  | ColorSlices    of ColorSliceD    array
  | CompoundSlices of CompoundSliceD array

  with
    member self.Item (idx: int) =
      match self with
      | StringSlices    arr -> StringSlice   arr.[idx]
      | IntSlices       arr -> IntSlice      arr.[idx]
      | FloatSlices     arr -> FloatSlice    arr.[idx]
      | DoubleSlices    arr -> DoubleSlice   arr.[idx]
      | BoolSlices      arr -> BoolSlice     arr.[idx]
      | ByteSlices      arr -> ByteSlice     arr.[idx]
      | EnumSlices      arr -> EnumSlice     arr.[idx]
      | ColorSlices     arr -> ColorSlice    arr.[idx]
      | CompoundSlices  arr -> CompoundSlice arr.[idx]
  

[<StringEnum>]
type PinType =
  | [<CompiledName("ValuePin")>]    ValuePin
  | [<CompiledName("StringPin")>]   StringPin
  | [<CompiledName("ColorPin")>]    ColorPin
  | [<CompiledName("EnumPin")>]     EnumPin
  | [<CompiledName("NodePin")>]     NodePin
  | [<CompiledName("BytePin")>]     BytePin
  | [<CompiledName("CompoundPin")>] CompoundPin

//  _   _ _   _ _
// | | | | |_(_) |___
// | | | | __| | / __|
// | |_| | |_| | \__ \
//  \___/ \__|_|_|___/

[<AutoOpen>]
module IOBoxUtils =

  let getType = function
    | IntBox _ | FloatBox  _ | DoubleBox _ | BoolBox _ -> ValuePin
    | StringBox                                      _ -> StringPin
    | ByteBox                                        _ -> BytePin
    | EnumBox                                        _ -> EnumPin
    | ColorBox                                       _ -> ColorPin
    | Compound                                       _ -> CompoundPin
