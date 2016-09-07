namespace Iris.Core

#if JAVASCRIPT

open Fable.Core

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

//  ____       _                 _
// | __ )  ___| |__   __ ___   _(_) ___  _ __
// |  _ \ / _ \ '_ \ / _` \ \ / / |/ _ \| '__|
// | |_) |  __/ | | | (_| |\ V /| | (_) | |
// |____/ \___|_| |_|\__,_| \_/ |_|\___/|_|

[<RequireQualifiedAccess>]
type Behavior =
  | Toggle
  | Bang

#if JAVASCRIPT
#else
  with

    static member FromFB (fb: BehaviorTypeFB) =
      match fb with
      | BehaviorTypeFB.ToggleFB -> Some Toggle
      | BehaviorTypeFB.BangFB   -> Some Bang
      | _                       -> None

    member self.ToOffset(builder: FlatBufferBuilder) : BehaviorTypeFB =
      match self with
      | Toggle -> BehaviorTypeFB.ToggleFB
      | Bang   -> BehaviorTypeFB.BangFB

#endif

//  ____  _        _            _____
// / ___|| |_ _ __(_)_ __   __ |_   _|   _ _ __   ___
// \___ \| __| '__| | '_ \ / _` || || | | | '_ \ / _ \
//  ___) | |_| |  | | | | | (_| || || |_| | |_) |  __/
// |____/ \__|_|  |_|_| |_|\__, ||_| \__, | .__/ \___|
//                         |___/     |___/|_|

type StringType =
  | Simple
  | MultiLine
  | FileName
  | Directory
  | Url
  | IP

#if JAVASCRIPT
#else
  with

    static member FromFB (fb: StringTypeFB) =
      match fb with
      | StringTypeFB.SimpleFB    -> Some Simple
      | StringTypeFB.MultiLineFB -> Some MultiLine
      | StringTypeFB.FileNameFB  -> Some FileName
      | StringTypeFB.DirectoryFB -> Some Directory
      | StringTypeFB.UrlFB       -> Some Url
      | StringTypeFB.IPFB        -> Some IP
      | _                        -> None

    member self.ToOffset(builder: FlatBufferBuilder) : StringTypeFB =
      match self with
      | Simple    -> StringTypeFB.SimpleFB
      | MultiLine -> StringTypeFB.MultiLineFB
      | FileName  -> StringTypeFB.FileNameFB
      | Directory -> StringTypeFB.DirectoryFB
      | Url       -> StringTypeFB.UrlFB
      | IP        -> StringTypeFB.IPFB

#endif

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
          let newarr = Array.copy arr
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

    //  ___ ___  ____
    // |_ _/ _ \| __ )  _____  __
    //  | | | | |  _ \ / _ \ \/ /
    //  | | |_| | |_) | (_) >  <
    // |___\___/|____/ \___/_/\_\

    //  ____            _       _ _          _   _
    // / ___|  ___ _ __(_) __ _| (_)______ _| |_(_) ___  _ __
    // \___ \ / _ \ '__| |/ _` | | |_  / _` | __| |/ _ \| '_ \
    //  ___) |  __/ |  | | (_| | | |/ / (_| | |_| | (_) | | | |
    // |____/ \___|_|  |_|\__,_|_|_/___\__,_|\__|_|\___/|_| |_|


    member self.ToOffset(builder: FlatBufferBuilder) : Offset<IOBoxFB> =
      failwith "IOBOX FIXME"

    static member FromFB(fb: IOBoxFB) =
      failwith "IOBOX FIXME"

    member self.ToBytes() : byte array = buildBuffer self

    static member FromBytes(bytes: byte array) : IOBox option =
      failwith "IOBOX FIXME"


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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "BoolBox ToOffset FIXME"

  //   static member FromFB(fb: BoolBoxFB) : BoolBoxD option =
  //     failwith "BoolBox FromFB FIXME"

  //   member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : BoolBoxD option =
  //     failwith "BoolBox ToBytes FIXME"

and BoolSliceD =
  { Index: Index
  ; Value: bool }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      BoolSliceFB.StartBoolSliceFB(builder)
      BoolSliceFB.AddIndex(builder, self.Index)
      BoolSliceFB.AddValue(builder, self.Value)
      BoolSliceFB.EndBoolSliceFB(builder)

    static member FromFB(fb: BoolSliceFB) : BoolSliceD option =
      try
        { Index = fb.Index
        ; Value = fb.Value }
        |> Some
      with
        | _ -> None

    member self.ToBytes() : byte array = buildBuffer self

    static member FromBytes(bytes: byte array) : BoolSliceD option =
      BoolSliceFB.GetRootAsBoolSliceFB(new ByteBuffer(bytes))
      |> BoolSliceD.FromFB

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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "IntBox ToOffset FIXME"

  //   static member FromFB(fb: IntBoxFB) : IntBoxD option =
  //     failwith "IntBox FromFB FIXME"

  //   member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : IntBoxD option =
  //     failwith "IntBox ToBytes FIXME"

and IntSliceD =
  { Index: Index
  ; Value: int }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      IntSliceFB.StartIntSliceFB(builder)
      IntSliceFB.AddIndex(builder, self.Index)
      IntSliceFB.AddValue(builder, self.Value)
      IntSliceFB.EndIntSliceFB(builder)

    static member FromFB(fb: IntSliceFB) : IntSliceD option =
      try
        { Index = fb.Index
        ; Value = fb.Value }
        |> Some
      with
        | _ -> None

    member self.ToBytes() : byte array = buildBuffer self

    static member FromBytes(bytes: byte array) : IntSliceD option =
      IntSliceFB.GetRootAsIntSliceFB(new ByteBuffer(bytes))
      |> IntSliceD.FromFB

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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "FloatBox ToOffset FIXME"

  //   static member FromFB(fb: FloatBoxFB) : FloatBoxD option =
  //     failwith "FloatBox FromFB FIXME"

  //   member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : FloatBoxD option =
  //     failwith "FloatBox ToBytes FIXME"

and FloatSliceD =
  { Index: Index
  ; Value: float }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      FloatSliceFB.StartFloatSliceFB(builder)
      FloatSliceFB.AddIndex(builder, self.Index)
      FloatSliceFB.AddValue(builder, float32 self.Value)
      FloatSliceFB.EndFloatSliceFB(builder)

    static member FromFB(fb: FloatSliceFB) : FloatSliceD option =
      try
        { Index = fb.Index
        ; Value = float fb.Value }
        |> Some
      with
        | _ -> None

    member self.ToBytes() : byte array = buildBuffer self

    static member FromBytes(bytes: byte array) : FloatSliceD option =
      FloatSliceFB.GetRootAsFloatSliceFB(new ByteBuffer(bytes))
      |> FloatSliceD.FromFB

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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "DoubleBox ToOffset FIXME"

  //   static member FromFB(fb: DoubleBoxFB) : DoubleBoxD option =
  //     failwith "DoubleBox FromFB FIXME"

  //   member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : DoubleBoxD option =
  //     failwith "DoubleBox ToBytes FIXME"

and DoubleSliceD =
  { Index: Index
  ; Value: double }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      DoubleSliceFB.StartDoubleSliceFB(builder)
      DoubleSliceFB.AddIndex(builder, self.Index)
      DoubleSliceFB.AddValue(builder, self.Value)
      DoubleSliceFB.EndDoubleSliceFB(builder)

    static member FromFB(fb: DoubleSliceFB) : DoubleSliceD option =
      try
        { Index = fb.Index
        ; Value = fb.Value }
        |> Some
      with
        | _ -> None

    member self.ToBytes() : byte array = buildBuffer self

    static member FromBytes(bytes: byte array) : DoubleSliceD option =
      DoubleSliceFB.GetRootAsDoubleSliceFB(new ByteBuffer(bytes))
      |> DoubleSliceD.FromFB




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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "ByteBox ToOffset FIXME"

  //   static member FromFB(fb: ByteBoxFB) : ByteBoxD option =
  //     failwith "ByteBox FromFB FIXME"

  //   member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : ByteBoxD option =
  //     failwith "ByteBox ToBytes FIXME"

and ByteSliceD =
  { Index: Index
  ; Value: byte array }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      let bytes = ByteSliceFB.CreateValueVector(builder, self.Value)
      ByteSliceFB.StartByteSliceFB(builder)
      ByteSliceFB.AddIndex(builder, self.Index)
      ByteSliceFB.AddValue(builder, bytes)
      ByteSliceFB.EndByteSliceFB(builder)

    static member FromFB(fb: ByteSliceFB) : ByteSliceD option =
      try
        let values = Array.zeroCreate fb.ValueLength

        for i in 0 .. (fb.ValueLength - 1) do
          values.[i] <- fb.GetValue(i)

        { Index = fb.Index
        ; Value = values }
        |> Some
      with
        | _ -> None

    member self.ToBytes() : byte array = buildBuffer self

    static member FromBytes(bytes: byte array) : ByteSliceD option =
      ByteSliceFB.GetRootAsByteSliceFB(new ByteBuffer(bytes))
      |> ByteSliceD.FromFB

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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "EnumBox ToOffset FIXME"

  //   static member FromFB(fb: EnumBoxFB) : EnumBoxD option =
  //     failwith "EnumBox FromFB FIXME"

  //   member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : EnumBoxD option =
  //     failwith "EnumBox ToBytes FIXME"

and EnumSliceD =
  { Index: Index
  ; Value: Property }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      let property =
        let key, value =
          match self.Value with
          | (k, v) ->
            builder.CreateString k, builder.CreateString v

        EnumPropertyFB.StartEnumPropertyFB(builder)
        EnumPropertyFB.AddKey(builder, key)
        EnumPropertyFB.AddValue(builder, value)
        EnumPropertyFB.EndEnumPropertyFB(builder)

      EnumSliceFB.StartEnumSliceFB(builder)
      EnumSliceFB.AddIndex(builder, self.Index)
      EnumSliceFB.AddValue(builder, property)
      EnumSliceFB.EndEnumSliceFB(builder)

    static member FromFB(fb: EnumSliceFB) : EnumSliceD option =
      let property =
        let kv = fb.GetValue(new EnumPropertyFB())
        (kv.Key, kv.Value)

      try
        { Index = fb.Index
        ; Value = property }
        |> Some
      with
        | _ -> None

    member self.ToEnums() : byte array = buildBuffer self

    static member FromEnums(bytes: byte array) : EnumSliceD option =
      EnumSliceFB.GetRootAsEnumSliceFB(new ByteBuffer(bytes))
      |> EnumSliceD.FromFB

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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "ColorBox ToOffset FIXME"

  //   static member FromFB(fb: ColorBoxFB) : ColorBoxD option =
  //     failwith "ColorBox FromFB FIXME"

  //  member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : ColorBoxD option =
  //     failwith "ColorBox ToBytes FIXME"


and ColorSliceD =
  { Index: Index
  ; Value: ColorSpace }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      let offset = self.Value.ToOffset(builder)
      ColorSliceFB.StartColorSliceFB(builder)
      ColorSliceFB.AddIndex(builder, self.Index)
      ColorSliceFB.AddValue(builder, offset)
      ColorSliceFB.EndColorSliceFB(builder)

    static member FromFB(fb: ColorSliceFB) : ColorSliceD option =
      ColorSpace.FromFB fb.Value
      |> Option.map (fun color -> { Index = fb.Index; Value = color })

    member self.ToColors() : byte array = buildBuffer self

    static member FromColors(bytes: byte array) : ColorSliceD option =
      ColorSliceFB.GetRootAsColorSliceFB(new ByteBuffer(bytes))
      |> ColorSliceD.FromFB

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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "StringBox ToOffset FIXME"

  //   static member FromFB(fb: StringBoxFB) : StringBoxD option =
  //     failwith "StringBox FromFB FIXME"

  // member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : StringBoxD option =
  //     failwith "StringBox ToBytes FIXME"


and StringSliceD =
  { Index      : Index
  ; Value      : string }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      let value = builder.CreateString self.Value
      StringSliceFB.StartStringSliceFB(builder)
      StringSliceFB.AddIndex(builder, self.Index)
      StringSliceFB.AddValue(builder, value)
      StringSliceFB.EndStringSliceFB(builder)

    static member FromFB(fb: StringSliceFB) : StringSliceD option =
      try
        { Index = fb.Index
        ; Value = fb.Value }
        |> Some
      with
        | _ -> None

    member self.ToStrings() : byte array = buildBuffer self

    static member FromStrings(bytes: byte array) : StringSliceD option =
      StringSliceFB.GetRootAsStringSliceFB(new ByteBuffer(bytes))
      |> StringSliceD.FromFB

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

  // with
  //   member self.ToOffset(builder: FlatBufferBuilder) =
  //     failwith "CompundBox ToOffset FIXME"

  //   static member FromFB(fb: CompundBoxFB) : CompundBoxD option =
  //     failwith "CompundBox FromFB FIXME"

  //  member self.ToBytes() : byte array = buildBuffer self

  //   static member FromBytes(bytes: byte array) : CompundBoxD option =
  //     failwith "CompundBox ToBytes FIXME"

and CompoundSliceD =
  { Index      : Index
  ; Value      : IOBox array }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      let ioboxes = CompoundSliceFB.CreateValueVector(builder, [| |])
      CompoundSliceFB.StartCompoundSliceFB(builder)
      CompoundSliceFB.AddIndex(builder, self.Index)
      CompoundSliceFB.AddValue(builder, ioboxes)
      CompoundSliceFB.EndCompoundSliceFB(builder)

    static member FromFB(fb: CompoundSliceFB) : CompoundSliceD option =
      try
        { Index = fb.Index
        ; Value = [| |] }
        |> Some
      with
        | _ -> None

    member self.ToCompounds() : byte array = buildBuffer self

    static member FromCompounds(bytes: byte array) : CompoundSliceD option =
      CompoundSliceFB.GetRootAsCompoundSliceFB(new ByteBuffer(bytes))
      |> CompoundSliceD.FromFB




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
        | StringSlice data -> Some data.Value
        | _                -> None

    member self.StringData
      with get () =
        match self with
        | StringSlice data -> Some data
        | _                -> None

    member self.IntValue
      with get () =
        match self with
        | IntSlice data -> Some data.Value
        | _             -> None

    member self.IntData
      with get () =
        match self with
        | IntSlice data -> Some data
        | _             -> None

    member self.FloatValue
      with get () =
        match self with
        | FloatSlice data -> Some data.Value
        | _               -> None

    member self.FloatData
      with get () =
        match self with
        | FloatSlice data -> Some data
        | _               -> None

    member self.DoubleValue
      with get () =
        match self with
        | DoubleSlice data -> Some data.Value
        | _                -> None

    member self.DoubleData
      with get () =
        match self with
        | DoubleSlice data -> Some data
        | _                -> None

    member self.BoolValue
      with get () =
        match self with
        | BoolSlice data -> Some data.Value
        | _              -> None

    member self.BoolData
      with get () =
        match self with
        | BoolSlice data -> Some data
        | _              -> None

    member self.ByteValue
      with get () =
        match self with
        | ByteSlice data -> Some data.Value
        | _              -> None

    member self.ByteData
      with get () =
        match self with
        | ByteSlice data -> Some data
        | _              -> None

    member self.EnumValue
      with get () =
        match self with
        | EnumSlice data -> Some data.Value
        | _              -> None

    member self.EnumData
      with get () =
        match self with
        | EnumSlice data -> Some data
        | _              -> None

    member self.ColorValue
      with get () =
        match self with
        | ColorSlice data -> Some data.Value
        | _               -> None

    member self.ColorData
      with get () =
        match self with
        | ColorSlice data -> Some data
        | _               -> None

    member self.CompoundValue
      with get () =
        match self with
        | CompoundSlice data -> Some data.Value
        | _                  -> None

    member self.CompoundData
      with get () =
        match self with
        | CompoundSlice data -> Some data
        | _                  -> None

    //  _____      ___   __  __          _
    // |_   _|__  / _ \ / _|/ _|___  ___| |_
    //   | |/ _ \| | | | |_| |_/ __|/ _ \ __|
    //   | | (_) | |_| |  _|  _\__ \  __/ |_
    //   |_|\___/ \___/|_| |_| |___/\___|\__|

    member self.ToOffset(builder: FlatBufferBuilder) =
      let build tipe (offset: Offset<_>) =
        SliceFB.StartSliceFB(builder)
        SliceFB.AddSliceType(builder, tipe)
        SliceFB.AddSlice(builder, offset.Value)
        SliceFB.EndSliceFB(builder)

      match self with
      | StringSlice   data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.StringSliceFB

      | IntSlice      data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.IntSliceFB

      | FloatSlice    data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.FloatSliceFB

      | DoubleSlice   data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.DoubleSliceFB

      | BoolSlice     data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.BoolSliceFB

      | ByteSlice     data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.ByteSliceFB

      | EnumSlice     data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.EnumSliceFB

      | ColorSlice    data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.ColorSliceFB

      | CompoundSlice data ->
        data.ToOffset(builder)
        |> build SliceTypeFB.CompoundSliceFB

    //  _____                    _____ ____
    // |  ___| __ ___  _ __ ___ |  ___| __ )
    // | |_ | '__/ _ \| '_ ` _ \| |_  |  _ \
    // |  _|| | | (_) | | | | | |  _| | |_) |
    // |_|  |_|  \___/|_| |_| |_|_|   |____/

    static member FromFB(fb: SliceFB) : Slice option =
      match fb.SliceType with
      | SliceTypeFB.StringSliceFB   ->
        fb.GetSlice(new StringSliceFB())
        |> StringSliceD.FromFB
        |> Option.map StringSlice

      | SliceTypeFB.IntSliceFB      ->
        fb.GetSlice(new IntSliceFB())
        |> IntSliceD.FromFB
        |> Option.map IntSlice

      | SliceTypeFB.FloatSliceFB    ->
        fb.GetSlice(new FloatSliceFB())
        |> FloatSliceD.FromFB
        |> Option.map FloatSlice

      | SliceTypeFB.DoubleSliceFB   ->
        fb.GetSlice(new DoubleSliceFB())
        |> DoubleSliceD.FromFB
        |> Option.map DoubleSlice

      | SliceTypeFB.BoolSliceFB     ->
        fb.GetSlice(new BoolSliceFB())
        |> BoolSliceD.FromFB
        |> Option.map BoolSlice

      | SliceTypeFB.ByteSliceFB     ->
        fb.GetSlice(new ByteSliceFB())
        |> ByteSliceD.FromFB
        |> Option.map ByteSlice

      | SliceTypeFB.EnumSliceFB     ->
        fb.GetSlice(new EnumSliceFB())
        |> EnumSliceD.FromFB
        |> Option.map EnumSlice

      | SliceTypeFB.ColorSliceFB    ->
        fb.GetSlice(new ColorSliceFB())
        |> ColorSliceD.FromFB
        |> Option.map ColorSlice

      | SliceTypeFB.CompoundSliceFB ->
        fb.GetSlice(new CompoundSliceFB())
        |> CompoundSliceD.FromFB
        |> Option.map CompoundSlice

      | _ -> None

    member self.ToBytes() : byte array = buildBuffer self

    static member FromBytes(bytes: byte array) : Slice option =
      SliceFB.GetRootAsSliceFB(new ByteBuffer(bytes))
      |> Slice.FromFB

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

//  ____  _     _____
// |  _ \(_)_ _|_   _|   _ _ __   ___
// | |_) | | '_ \| || | | | '_ \ / _ \
// |  __/| | | | | || |_| | |_) |  __/
// |_|   |_|_| |_|_| \__, | .__/ \___| 4 IOBox Plugins
//                   |___/|_|

#if JAVASCRIPT

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

#endif
