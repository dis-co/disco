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

    //  ____       _   ____  _ _
    // / ___|  ___| |_/ ___|| (_) ___ ___
    // \___ \ / _ \ __\___ \| | |/ __/ _ \
    //  ___) |  __/ |_ ___) | | | (_|  __/
    // |____/ \___|\__|____/|_|_|\___\___|

    member self.SetSlice (value: Slice) =
      let update (arr : 'a array) (data: 'a) =
        if int value.Index > Array.length arr then

#if JAVASCRIPT
          /// Rationale:
          ///
          /// in JavaScript an array will re-allocate automatically under the hood
          /// hence we don't need to worry about out-of-bounds errors.

          let newarr = Array.map id arr
          newarr.[int value.Index] <- data
          newarr
#else
          /// Rationale:
          ///
          /// in .NET, we need to worry about out-of-bounds errors, and we
          /// detected that we are about to run into one, hence re-alloc, copy
          /// and finally set the value at the correct index.

          let newarr = Array.zeroCreate (int value.Index + 1)
          arr.CopyTo(newarr, 0)
          newarr.[int value.Index] <- data
          newarr
#endif
        else
          Array.mapi (fun i d -> if i = int value.Index then data else d) arr

      match self with
      | StringBox data as current ->
        match value with
          | StringSlice slice     -> StringBox { data with Slices = update data.Slices slice }
          | _                     -> current

      | IntBox data as current    -> 
        match value with
          | IntSlice slice        -> IntBox { data with Slices = update data.Slices slice }
          | _                     -> current

      | FloatBox data as current  ->
        match value with
          | FloatSlice slice      -> FloatBox { data with Slices = update data.Slices slice }
          | _                     -> current

      | DoubleBox data as current ->
        match value with
          | DoubleSlice slice     -> DoubleBox { data with Slices = update data.Slices slice }
          | _                     -> current

      | BoolBox data as current   ->
        match value with
          | BoolSlice slice       -> BoolBox { data with Slices = update data.Slices slice }
          | _                     -> current

      | ByteBox data as current   ->
        match value with
          | ByteSlice slice       -> ByteBox { data with Slices = update data.Slices slice }
          | _                     -> current

      | EnumBox data as current   ->
        match value with
          | EnumSlice slice       -> EnumBox { data with Slices = update data.Slices slice }
          | _                     -> current

      | ColorBox data as current  ->
        match value with
          | ColorSlice slice      -> ColorBox { data with Slices = update data.Slices slice }
          | _                     -> current

      | Compound data as current  ->
        match value with
          | CompoundSlice slice   -> Compound { data with Slices = update data.Slices slice }
          | _                     -> current


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
  { Index      : Index
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
  ; Value      : IOBox array }

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

    member self.Value
      with get () =
        match self with
        | StringSlice   data -> data.Value :> obj
        | IntSlice      data -> data.Value :> obj
        | FloatSlice    data -> data.Value :> obj
        | DoubleSlice   data -> data.Value :> obj
        | BoolSlice     data -> data.Value :> obj
        | ByteSlice     data -> data.Value :> obj
        | EnumSlice     data -> data.Value :> obj
        | ColorSlice    data -> data.Value :> obj
        | CompoundSlice data -> data.Value :> obj

    member self.StringValue
      with get () =
        match self with
        | StringSlice data -> data.Value
        | _                -> failwith "Slice is not a string value type"

    member self.StringData
      with get () =
        match self with
        | StringSlice data -> data
        | _                -> failwith "Slice is not a string value type"

    member self.IntValue
      with get () =
        match self with
        | IntSlice data -> data.Value
        | _             -> failwith "Slice is not an int value type"

    member self.IntData
      with get () =
        match self with
        | IntSlice data -> data
        | _             -> failwith "Slice is not an int value type"

    member self.FloatValue
      with get () =
        match self with
        | FloatSlice data -> data.Value
        | _               -> failwith "Slice is not a float value type"

    member self.FloatData
      with get () =
        match self with
        | FloatSlice data -> data
        | _               -> failwith "Slice is not a float value type"

    member self.DoubleValue
      with get () =
        match self with
        | DoubleSlice data -> data.Value
        | _                -> failwith "Slice is not a double value type"

    member self.DoubleData
      with get () =
        match self with
        | DoubleSlice data -> data
        | _                -> failwith "Slice is not a double value type"

    member self.BoolValue
      with get () =
        match self with
        | BoolSlice data -> data.Value
        | _              -> failwith "Slice is not a boolean value type"

    member self.BoolData
      with get () =
        match self with
        | BoolSlice data -> data
        | _              -> failwith "Slice is not a boolean value type"

    member self.ByteValue
      with get () =
        match self with
        | ByteSlice data -> data.Value
        | _              -> failwith "Slice is not a byte value type"

    member self.ByteData
      with get () =
        match self with
        | ByteSlice data -> data
        | _              -> failwith "Slice is not a byte value type"

    member self.EnumValue
      with get () =
        match self with
        | EnumSlice data -> data.Value
        | _              -> failwith "Slice is not an enum value type"

    member self.EnumData
      with get () =
        match self with
        | EnumSlice data -> data
        | _              -> failwith "Slice is not an enum value type"

    member self.ColorValue
      with get () =
        match self with
        | ColorSlice data -> data.Value
        | _               -> failwith "Slice is not a color value type"

    member self.ColorData
      with get () =
        match self with
        | ColorSlice data -> data
        | _               -> failwith "Slice is not a color value type"

    member self.CompoundValue
      with get () =
        match self with
        | CompoundSlice data -> data.Value
        | _                  -> failwith "Slice is not a compound value type"

    member self.CompoundData
      with get () =
        match self with
        | CompoundSlice data -> data
        | _                  -> failwith "Slice is not a compound value type"
//  ____  _ _
// / ___|| (_) ___ ___  ___
// \___ \| | |/ __/ _ \/ __|
//  ___) | | | (_|  __/\__ \
// |____/|_|_|\___\___||___/

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
    member self.IsString
      with get () =
        match self with
        | StringSlices _ -> true
        |              _ -> false

    member self.IsInt
      with get () =
        match self with
        | IntSlices _ -> true
        |           _ -> false

    member self.IsFloat
      with get () =
        match self with
        | FloatSlices _ -> true
        |           _ -> false

    member self.IsDouble
      with get () =
        match self with
        | DoubleSlices _ -> true
        |              _ -> false

    member self.IsBool
      with get () =
        match self with
        | BoolSlices _ -> true
        |            _ -> false

    member self.IsByte
      with get () =
        match self with
        | ByteSlices _ -> true
        |            _ -> false

    member self.IsEnum
      with get () =
        match self with
        | EnumSlices _ -> true
        |            _ -> false

    member self.IsColor
      with get () =
        match self with
        | ColorSlices _ -> true
        |             _ -> false

    member self.IsCompound
      with get () =
        match self with
        | CompoundSlices _ -> true
        |                _ -> false

    //  ___ _
    // |_ _| |_ ___ _ __ ___
    //  | || __/ _ \ '_ ` _ \
    //  | || ||  __/ | | | | |
    // |___|\__\___|_| |_| |_|

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

    member self.At (idx: int) = self.Item idx

    //  __  __
    // |  \/  | __ _ _ __
    // | |\/| |/ _` | '_ \
    // | |  | | (_| | |_) |
    // |_|  |_|\__,_| .__/
    //              |_|

    member self.Map (f: Slice -> 'a) : 'a array =
      match self with
      | StringSlices    arr -> Array.map (StringSlice   >> f) arr
      | IntSlices       arr -> Array.map (IntSlice      >> f) arr
      | FloatSlices     arr -> Array.map (FloatSlice    >> f) arr
      | DoubleSlices    arr -> Array.map (DoubleSlice   >> f) arr
      | BoolSlices      arr -> Array.map (BoolSlice     >> f) arr
      | ByteSlices      arr -> Array.map (ByteSlice     >> f) arr
      | EnumSlices      arr -> Array.map (EnumSlice     >> f) arr
      | ColorSlices     arr -> Array.map (ColorSlice    >> f) arr
      | CompoundSlices  arr -> Array.map (CompoundSlice >> f) arr

    //  _   _      _
    // | | | | ___| |_ __   ___ _ __ ___
    // | |_| |/ _ \ | '_ \ / _ \ '__/ __|
    // |  _  |  __/ | |_) |  __/ |  \__ \
    // |_| |_|\___|_| .__/ \___|_|  |___/
    //              |_|

    member __.CreateString (idx: Index) (value: string) =
      StringSlice { Index = idx; Value = value }

    member __.CreateInt (idx: Index) (value: int) =
      IntSlice { Index = idx; Value = value }

    member __.CreateFloat (idx: Index) (value: float) =
      FloatSlice { Index = idx; Value = value }

    member __.CreateDouble (idx: Index) (value: double) =
      DoubleSlice { Index = idx; Value = value }

    member __.CreateBool (idx: Index) (value: bool) =
      BoolSlice { Index = idx; Value = value }

    member __.CreateByte (idx: Index) (value: byte array) =
      ByteSlice { Index = idx; Value = value }

    member __.CreateEnum (idx: Index) (value: Property) =
      EnumSlice { Index = idx; Value = value }

    member __.CreateColor (idx: Index) (value: ColorSpace) =
      ColorSlice { Index = idx; Value = value }

    member __.CreateCompound (idx: Index) (value: IOBox array) =
      CompoundSlice { Index = idx; Value = value }

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
