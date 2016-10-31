namespace Iris.Web.Core

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers


[<Import("Iris", from="buffers")>]
module UserFlatBuffers =
  //  _   _               _____ ____
  // | | | |___  ___ _ __|  ___| __ )
  // | | | / __|/ _ \ '__| |_  |  _ \
  // | |_| \__ \  __/ |  |  _| | |_) |
  //  \___/|___/\___|_|  |_|   |____/

  type UserFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.UserName()")>]
    abstract UserName: string

    [<Emit("$0.FirstName()")>]
    abstract FirstName: string

    [<Emit("$0.LastName()")>]
    abstract LastName: string

    [<Emit("$0.Email()")>]
    abstract Email: string

    [<Emit("$0.Joined()")>]
    abstract Joined: string

    [<Emit("$0.Created()")>]
    abstract Created: string

  type UserFBConstructor =
    abstract prototype: UserFB with get, set

    [<Emit("Iris.Serialization.Raft.UserFB.startUserFB($1)")>]
    abstract StartUserFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.UserFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.UserFB.addUserName($1, $2)")>]
    abstract AddUserName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.UserFB.addFirstName($1, $2)")>]
    abstract AddFirstName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.UserFB.addLastName($1, $2)")>]
    abstract AddLastName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.UserFB.addEmail($1, $2)")>]
    abstract AddEmail: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.UserFB.addJoined($1, $2)")>]
    abstract AddJoined: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.UserFB.addCreated($1, $2)")>]
    abstract AddCreated: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.UserFB.endUserFB($1)")>]
    abstract EndUserFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.UserFB.getRootAsUserFB($1)")>]
    abstract GetRootAsUserFB: buffer: ByteBuffer -> UserFB

  let UserFB : UserFBConstructor = failwith "JS only"

 open UserFlatBuffers

[<Import("Iris", from="buffers")>]
module FlatBufferTypes =
  //  ____                _             _____ ____
  // / ___|  ___  ___ ___(_) ___  _ __ |  ___| __ )
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \| |_  |  _ \
  //  ___) |  __/\__ \__ \ | (_) | | | |  _| | |_) |
  // |____/ \___||___/___/_|\___/|_| |_|_|   |____/

  type SessionFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.UserName()")>]
    abstract UserName: string

    [<Emit("$0.IpAddress()")>]
    abstract IpAddress: string

    [<Emit("$0.UserAgent()")>]
    abstract UserAgent: string

  type SessionFBConstructor =
    abstract prototype: SessionFB with get, set

    [<Emit("Iris.Serialization.Raft.SessionFB.startSessionFB($1)")>]
    abstract StartSessionFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.SessionFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.SessionFB.addUserName($1, $2)")>]
    abstract AddUserName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.SessionFB.addIpAddress($1, $2)")>]
    abstract AddIpAddress: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.SessionFB.addUserAgent($1, $2)")>]
    abstract AddUserAgent: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.SessionFB.endSessionFB($1)")>]
    abstract EndSessionFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.SessionFB.getRootAsSessionFB($1)")>]
    abstract GetRootAsSessionFB: buffer: ByteBuffer -> SessionFB

  let SessionFB : SessionFBConstructor = failwith "JS only"

  //  ____   ____ ____    ___     __    _
  // |  _ \ / ___| __ )  / \ \   / /_ _| |_   _  ___
  // | |_) | |  _|  _ \ / _ \ \ / / _` | | | | |/ _ \
  // |  _ <| |_| | |_) / ___ \ V / (_| | | |_| |  __/
  // |_| \_\\____|____/_/   \_\_/ \__,_|_|\__,_|\___|

  type RGBAValueFB =
    [<Emit("$0.Red()")>]
    abstract Red: uint8

    [<Emit("$0.Green()")>]
    abstract Green: uint8

    [<Emit("$0.Blue()")>]
    abstract Blue: uint8

    [<Emit("$0.Alpha()")>]
    abstract Alpha: uint8

  type RGBAValueFBConstructor =
    abstract prototype: RGBAValueFB with get, set

    [<Emit("Iris.Serialization.Raft.RGBAValueFB.startRGBAValueFB($1)")>]
    abstract StartRGBAValueFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.RGBAValueFB.addRed($1, $2)")>]
    abstract AddRed: builder: FlatBufferBuilder * id: uint8 -> unit

    [<Emit("Iris.Serialization.Raft.RGBAValueFB.addGreen($1, $2)")>]
    abstract AddGreen: builder: FlatBufferBuilder * id: uint8 -> unit

    [<Emit("Iris.Serialization.Raft.RGBAValueFB.addBlue($1, $2)")>]
    abstract AddBlue: builder: FlatBufferBuilder * id: uint8 -> unit

    [<Emit("Iris.Serialization.Raft.RGBAValueFB.addAlpha($1, $2)")>]
    abstract AddAlpha: builder: FlatBufferBuilder * id: uint8 -> unit

    [<Emit("Iris.Serialization.Raft.RGBAValueFB.endRGBAValueFB($1)")>]
    abstract EndRGBAValueFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.RGBAValueFB.getRootAsRGBAValueFB($1)")>]
    abstract GetRootAsRGBAValueFB: buffer: ByteBuffer -> RGBAValueFB

    [<Emit("new Iris.Serialization.Raft.RGBAValueFB()")>]
    abstract Create: unit -> RGBAValueFB

  //  _   _ ____  _        ___     __    _
  // | | | / ___|| |      / \ \   / /_ _| |_   _  ___
  // | |_| \___ \| |     / _ \ \ / / _` | | | | |/ _ \
  // |  _  |___) | |___ / ___ \ V / (_| | | |_| |  __/
  // |_| |_|____/|_____/_/   \_\_/ \__,_|_|\__,_|\___|

  type HSLAValueFB =
    [<Emit("$0.Hue()")>]
    abstract Hue: uint8

    [<Emit("$0.Saturation()")>]
    abstract Saturation: uint8

    [<Emit("$0.Lightness()")>]
    abstract Lightness: uint8

    [<Emit("$0.Alpha()")>]
    abstract Alpha: uint8

  type HSLAValueFBConstructor =
    abstract prototype: HSLAValueFB with get, set

    [<Emit("Iris.Serialization.Raft.HSLAValueFB.startHSLAValueFB($1)")>]
    abstract StartHSLAValueFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.HSLAValueFB.addHue($1, $2)")>]
    abstract AddHue: builder: FlatBufferBuilder * id: uint8 -> unit

    [<Emit("Iris.Serialization.Raft.HSLAValueFB.addSaturation($1, $2)")>]
    abstract AddSaturation: builder: FlatBufferBuilder * id: uint8 -> unit

    [<Emit("Iris.Serialization.Raft.HSLAValueFB.addLightness($1, $2)")>]
    abstract AddLightness: builder: FlatBufferBuilder * id: uint8 -> unit

    [<Emit("Iris.Serialization.Raft.HSLAValueFB.addAlpha($1, $2)")>]
    abstract AddAlpha: builder: FlatBufferBuilder * id: uint8 -> unit

    [<Emit("Iris.Serialization.Raft.HSLAValueFB.endHSLAValueFB($1)")>]
    abstract EndHSLAValueFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.HSLAValueFB.getRootAsHSLAValueFB($1)")>]
    abstract GetRootAsHSLAValueFB: buffer: ByteBuffer -> HSLAValueFB

    [<Emit("new Iris.Serialization.Raft.HSLAValueFB()")>]
    abstract Create: unit -> HSLAValueFB

  //  _____
  // |_   _|   _ _ __   ___
  //   | || | | | '_ \ / _ \
  //   | || |_| | |_) |  __/
  //   |_| \__, | .__/ \___|
  //       |___/|_|

  type ColorSpaceTypeEnumFB =
    [<Emit("Iris.Serialization.Raft.ColorSpaceTypeFB.RGBAValueFB")>]
    abstract RGBAValueFB : int

    [<Emit("Iris.Serialization.Raft.ColorSpaceTypeFB.HSLAValueFB")>]
    abstract HSLAValueFB : int

  //   ____      _            ____
  //  / ___|___ | | ___  _ __/ ___| _ __   __ _  ___ ___
  // | |   / _ \| |/ _ \| '__\___ \| '_ \ / _` |/ __/ _ \
  // | |__| (_) | | (_) | |   ___) | |_) | (_| | (_|  __/
  //  \____\___/|_|\___/|_|  |____/| .__/ \__,_|\___\___|
  //                               |_|

  type ColorSpaceFB =
    [<Emit("$0.ValueType()")>]
    abstract ValueType: int

    [<Emit("$0.Value($1)")>]
    abstract Value: 'a -> 'a

  type ColorSpaceFBConstructor =
    abstract prototype: ColorSpaceFB with get, set

    [<Emit("Iris.Serialization.Raft.ColorSpaceFB.startColorSpaceFB($1)")>]
    abstract StartColorSpaceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ColorSpaceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * offset: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.ColorSpaceFB.addValueType($1, $2)")>]
    abstract AddValueType: builder: FlatBufferBuilder * tipe: int -> unit

    [<Emit("Iris.Serialization.Raft.ColorSpaceFB.endColorSpaceFB($1)")>]
    abstract EndColorSpaceFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.ColorSpaceFB.getRootAsColorSpaceFB($1)")>]
    abstract GetRootAsColorSpaceFB: buffer: ByteBuffer -> ColorSpaceFB

  let HSLAValueFB : HSLAValueFBConstructor = failwith "JS only"
  let RGBAValueFB : RGBAValueFBConstructor = failwith "JS only"
  let ColorSpaceFB : ColorSpaceFBConstructor = failwith "JS only"
  let ColorSpaceTypeFB : ColorSpaceTypeEnumFB = failwith "JS only"

  //  ____       _                 _            _____ ____
  // | __ )  ___| |__   __ ___   _(_) ___  _ __|  ___| __ )
  // |  _ \ / _ \ '_ \ / _` \ \ / / |/ _ \| '__| |_  |  _ \
  // | |_) |  __/ | | | (_| |\ V /| | (_) | |  |  _| | |_) |
  // |____/ \___|_| |_|\__,_| \_/ |_|\___/|_|  |_|   |____/

  type BehaviorFB = int

  type BehaviorFBConstructor =
    [<Emit("Iris.Serialization.Raft.BehaviorFB.ToggleFB")>]
    abstract ToggleFB: int

    [<Emit("Iris.Serialization.Raft.BehaviorFB.BangFB")>]
    abstract BangFB: int

  let BehaviorFB: BehaviorFBConstructor = failwith "JS only"

  //  ____  _        _            _____                 _____ ____
  // / ___|| |_ _ __(_)_ __   __ |_   _|   _ _ __   ___|  ___| __ )
  // \___ \| __| '__| | '_ \ / _` || || | | | '_ \ / _ \ |_  |  _ \
  //  ___) | |_| |  | | | | | (_| || || |_| | |_) |  __/  _| | |_) |
  // |____/ \__|_|  |_|_| |_|\__, ||_| \__, | .__/ \___|_|   |____/
  //                         |___/     |___/|_|

  type StringTypeFB = int

  type StringTypeFBConstructor =

    [<Emit("Iris.Serialization.Raft.StringTypeFB.SimpleFB")>]
    abstract SimpleFB: StringTypeFB

    [<Emit("Iris.Serialization.Raft.StringTypeFB.MultiLineFB")>]
    abstract MultiLineFB: StringTypeFB

    [<Emit("Iris.Serialization.Raft.StringTypeFB.FileNameFB")>]
    abstract FileNameFB: StringTypeFB

    [<Emit("Iris.Serialization.Raft.StringTypeFB.DirectoryFB")>]
    abstract DirectoryFB: StringTypeFB

    [<Emit("Iris.Serialization.Raft.StringTypeFB.UrlFB")>]
    abstract UrlFB: StringTypeFB

    [<Emit("Iris.Serialization.Raft.StringTypeFB.IPFB")>]
    abstract IPFB: StringTypeFB

  let StringTypeFB: StringTypeFBConstructor = failwith "JS only"

  //  ___ ___  ____            _____                 _____ ____
  // |_ _/ _ \| __ )  _____  _|_   _|   _ _ __   ___|  ___| __ )
  //  | | | | |  _ \ / _ \ \/ / | || | | | '_ \ / _ \ |_  |  _ \
  //  | | |_| | |_) | (_) >  <  | || |_| | |_) |  __/  _| | |_) |
  // |___\___/|____/ \___/_/\_\ |_| \__, | .__/ \___|_|   |____/
  //                                |___/|_|

  type IOBoxTypeFB = int

  type IOBoxTypeFBConstructor =

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.StringBoxFB")>]
    abstract StringBoxFB: IOBoxTypeFB

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.IntBoxFB")>]
    abstract IntBoxFB: IOBoxTypeFB

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.FloatBoxFB")>]
    abstract FloatBoxFB: IOBoxTypeFB

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.DoubleBoxFB")>]
    abstract DoubleBoxFB: IOBoxTypeFB

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.BoolBoxFB")>]
    abstract BoolBoxFB: IOBoxTypeFB

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.ByteBoxFB")>]
    abstract ByteBoxFB: IOBoxTypeFB

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.EnumBoxFB")>]
    abstract EnumBoxFB: IOBoxTypeFB

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.ColorBoxFB")>]
    abstract ColorBoxFB: IOBoxTypeFB

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.CompoundBoxFB")>]
    abstract CompoundBoxFB: IOBoxTypeFB

  let IOBoxTypeFB: IOBoxTypeFBConstructor = failwith "JS only"

  //  ___ ___  ____            _____ ____
  // |_ _/ _ \| __ )  _____  _|  ___| __ )
  //  | | | | |  _ \ / _ \ \/ / |_  |  _ \
  //  | | |_| | |_) | (_) >  <|  _| | |_) |
  // |___\___/|____/ \___/_/\_\_|   |____/

  type IOBoxFB =

    [<Emit("$0.IOBox($1)")>]
    abstract IOBox: 'a -> 'a

    [<Emit("$0.IOBoxType()")>]
    abstract IOBoxType: int

  type IOBoxFBContructor =

    [<Emit("Iris.Serialization.Raft.IOBoxFB.startIOBoxFB($1)")>]
    abstract StartIOBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.IOBoxFB.addIOBox($1,$2)")>]
    abstract AddIOBox: builder: FlatBufferBuilder * iobox: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.IOBoxFB.addIOBoxType($1,$2)")>]
    abstract AddIOBoxType: builder: FlatBufferBuilder * tipe: IOBoxTypeFB -> unit

    [<Emit("Iris.Serialization.Raft.IOBoxFB.endIOBoxFB($1)")>]
    abstract EndIOBoxFB: builder: FlatBufferBuilder -> Offset<IOBoxFB>

    [<Emit("Iris.Serialization.Raft.IOBoxFB.getRootAsIOBoxFB($1)")>]
    abstract GetRootAsIOBoxFB: buffer: ByteBuffer -> IOBoxFB

  let IOBoxFB: IOBoxFBContructor = failwith "JS only"

  //  ____              _ ____  _ _          _____ ____
  // | __ )  ___   ___ | / ___|| (_) ___ ___|  ___| __ )
  // |  _ \ / _ \ / _ \| \___ \| | |/ __/ _ \ |_  |  _ \
  // | |_) | (_) | (_) | |___) | | | (_|  __/  _| | |_) |
  // |____/ \___/ \___/|_|____/|_|_|\___\___|_|   |____/

  type BoolSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type BoolSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.BoolSliceFB.startBoolSliceFB($1)")>]
    abstract StartBoolSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.BoolSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.BoolSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.BoolSliceFB.endBoolSliceFB($1)")>]
    abstract EndBoolSliceFB: builder: FlatBufferBuilder -> Offset<BoolSliceFB>

    [<Emit("Iris.Serialization.Raft.BoolSliceFB.getRootAsBoolSliceFB($1)")>]
    abstract GetRootAsBoolSliceFB: buffer: ByteBuffer -> BoolSliceFB

    [<Emit("new Iris.Serialization.Raft.BoolSliceFB()")>]
    abstract Create: unit -> BoolSliceFB

  let BoolSliceFB: BoolSliceFBConstructor = failwith "JS only"

  //  ____              _ ____            _____ ____
  // | __ )  ___   ___ | | __ )  _____  _|  ___| __ )
  // |  _ \ / _ \ / _ \| |  _ \ / _ \ \/ / |_  |  _ \
  // | |_) | (_) | (_) | | |_) | (_) >  <|  _| | |_) |
  // |____/ \___/ \___/|_|____/ \___/_/\_\_|   |____/

  type BoolBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.TagsLength()")>]
    abstract TagsLength: int

    [<Emit("$0.SlicesLength()")>]
    abstract SlicesLength: int

    [<Emit("$0.Tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.Slices($1)")>]
    abstract Slices: index: int -> BoolSliceFB

  type BoolBoxFBConstructor =
    abstract prototype: BoolBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.startBoolBoxFB($1)")>]
    abstract StartBoolBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.endBoolBoxFB($1)")>]
    abstract EndBoolBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.getRootAsBoolBoxFB($1)")>]
    abstract GetRootAsBoolBoxFB: buffer: ByteBuffer -> BoolBoxFB

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * slices: Offset<BoolSliceFB> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.BoolBoxFB()")>]
    abstract Create: unit -> BoolBoxFB

  let BoolBoxFB : BoolBoxFBConstructor = failwith "JS only"

  //  ___       _   ____  _ _          _____ ____
  // |_ _|_ __ | |_/ ___|| (_) ___ ___|  ___| __ )
  //  | || '_ \| __\___ \| | |/ __/ _ \ |_  |  _ \
  //  | || | | | |_ ___) | | | (_|  __/  _| | |_) |
  // |___|_| |_|\__|____/|_|_|\___\___|_|   |____/

  type IntSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value()")>]
    abstract Value: int

  type IntSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.IntSliceFB.startIntSliceFB($1)")>]
    abstract StartIntSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.IntSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.IntSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: int -> unit

    [<Emit("Iris.Serialization.Raft.IntSliceFB.endIntSliceFB($1)")>]
    abstract EndIntSliceFB: builder: FlatBufferBuilder -> Offset<IntSliceFB>

    [<Emit("Iris.Serialization.Raft.IntSliceFB.getRootAsIntSliceFB($1)")>]
    abstract GetRootAsIntSliceFB: buffer: ByteBuffer -> IntSliceFB

    [<Emit("new Iris.Serialization.Raft.IntSliceFB()")>]
    abstract Create: unit -> IntSliceFB

  let IntSliceFB: IntSliceFBConstructor = failwith "JS only"

  //  ___       _   ____            _____ ____
  // |_ _|_ __ | |_| __ )  _____  _|  ___| __ )
  //  | || '_ \| __|  _ \ / _ \ \/ / |_  |  _ \
  //  | || | | | |_| |_) | (_) >  <|  _| | |_) |
  // |___|_| |_|\__|____/ \___/_/\_\_|   |____/

  type IntBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.VecSize()")>]
    abstract VecSize: uint32

    [<Emit("$0.Min()")>]
    abstract Min: int

    [<Emit("$0.Max()")>]
    abstract Max: int

    [<Emit("$0.Unit()")>]
    abstract Unit: string

    [<Emit("$0.TagsLength($1)")>]
    abstract TagsLength: int

    [<Emit("$0.tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.SlicesLength($1)")>]
    abstract SlicesLength: int

    [<Emit("$0.slices($1)")>]
    abstract Slices: int -> IntSliceFB

  type IntBoxFBConstructor =
    abstract prototype: IntBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.IntBoxFB.startIntBoxFB($1)")>]
    abstract StartIntBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addVecSize($1, $2)")>]
    abstract AddVecSize: builder: FlatBufferBuilder * vecsize: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addMin($1, $2)")>]
    abstract AddMin: builder: FlatBufferBuilder * min: int -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addMax($1, $2)")>]
    abstract AddMax: builder: FlatBufferBuilder * max: int -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addUnit($1, $2)")>]
    abstract AddUnit: builder: FlatBufferBuilder * unit: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.endIntBoxFB($1)")>]
    abstract EndIntBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.IntBoxFB.getRootAsIntBoxFB($1)")>]
    abstract GetRootAsIntBoxFB: buffer: ByteBuffer -> IntBoxFB

    [<Emit("Iris.Serialization.Raft.IntBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.IntBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.IntBoxFB()")>]
    abstract Create: unit -> IntBoxFB

  let IntBoxFB : IntBoxFBConstructor = failwith "JS only"

  //  _____ _             _   ____  _ _          _____ ____
  // |  ___| | ___   __ _| |_/ ___|| (_) ___ ___|  ___| __ )
  // | |_  | |/ _ \ / _` | __\___ \| | |/ __/ _ \ |_  |  _ \
  // |  _| | | (_) | (_| | |_ ___) | | | (_|  __/  _| | |_) |
  // |_|   |_|\___/ \__,_|\__|____/|_|_|\___\___|_|   |____/

  type FloatSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value()")>]
    abstract Value: float

  type FloatSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.FloatSliceFB.startFloatSliceFB($1)")>]
    abstract StartFloatSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.FloatSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.FloatSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: float32 -> unit

    [<Emit("Iris.Serialization.Raft.FloatSliceFB.endFloatSliceFB($1)")>]
    abstract EndFloatSliceFB: builder: FlatBufferBuilder -> Offset<FloatSliceFB>

    [<Emit("Iris.Serialization.Raft.FloatSliceFB.getRootAsFloatSliceFB($1)")>]
    abstract GetRootAsFloatSliceFB: buffer: ByteBuffer -> FloatSliceFB

    [<Emit("new Iris.Serialization.Raft.FloatSliceFB()")>]
    abstract Create: unit -> FloatSliceFB

  let FloatSliceFB: FloatSliceFBConstructor = failwith "JS only"

  //  _____ _             _   ____            _____ ____
  // |  ___| | ___   __ _| |_| __ )  _____  _|  ___| __ )
  // | |_  | |/ _ \ / _` | __|  _ \ / _ \ \/ / |_  |  _ \
  // |  _| | | (_) | (_| | |_| |_) | (_) >  <|  _| | |_) |
  // |_|   |_|\___/ \__,_|\__|____/ \___/_/\_\_|   |____/

  type FloatBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.VecSize()")>]
    abstract VecSize: uint32

    [<Emit("$0.Min()")>]
    abstract Min: int

    [<Emit("$0.Max()")>]
    abstract Max: int

    [<Emit("$0.Unit()")>]
    abstract Unit: string

    [<Emit("$0.Precision()")>]
    abstract Precision: uint32

    [<Emit("$0.TagsLength()")>]
    abstract TagsLength: int

    [<Emit("$0.Tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.SlicesLength()")>]
    abstract SlicesLength: int

    [<Emit("$0.Slices($1)")>]
    abstract Slices: int -> FloatSliceFB

  type FloatBoxFBConstructor =
    abstract prototype: FloatBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.startFloatBoxFB($1)")>]
    abstract StartFloatBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addVecSize($1, $2)")>]
    abstract AddVecSize: builder: FlatBufferBuilder * vecsize: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addMin($1, $2)")>]
    abstract AddMin: builder: FlatBufferBuilder * min: int -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addMax($1, $2)")>]
    abstract AddMax: builder: FlatBufferBuilder * max: int -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addUnit($1, $2)")>]
    abstract AddUnit: builder: FlatBufferBuilder * unit: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addPrecision($1, $2)")>]
    abstract AddPrecision: builder: FlatBufferBuilder * precision: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.endFloatBoxFB($1)")>]
    abstract EndFloatBoxFB: builder: FlatBufferBuilder -> Offset<FloatBoxFB>

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.getRootAsFloatBoxFB($1)")>]
    abstract GetRootAsFloatBoxFB: buffer: ByteBuffer -> FloatBoxFB

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.FloatBoxFB()")>]
    abstract Create: unit -> FloatBoxFB

  let FloatBoxFB : FloatBoxFBConstructor = failwith "JS only"

  //  ____              _     _      ____  _ _          _____ ____
  // |  _ \  ___  _   _| |__ | | ___/ ___|| (_) ___ ___|  ___| __ )
  // | | | |/ _ \| | | | '_ \| |/ _ \___ \| | |/ __/ _ \ |_  |  _ \
  // | |_| | (_) | |_| | |_) | |  __/___) | | | (_|  __/  _| | |_) |
  // |____/ \___/ \__,_|_.__/|_|\___|____/|_|_|\___\___|_|   |____/

  type DoubleSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value()")>]
    abstract Value: double

  type DoubleSliceFBConstructor =
    abstract prototype: DoubleSliceFB with get, set

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.startDoubleSliceFB($1)")>]
    abstract StartDoubleSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: double -> unit

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.endDoubleSliceFB($1)")>]
    abstract EndDoubleSliceFB: builder: FlatBufferBuilder -> Offset<DoubleSliceFB>

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.getRootAsDoubleSliceFB($1)")>]
    abstract GetRootAsDoubleSliceFB: buffer: ByteBuffer -> DoubleSliceFB

    [<Emit("new Iris.Serialization.Raft.DoubleSliceFB()")>]
    abstract Create: unit -> DoubleSliceFB

  let DoubleSliceFB: DoubleSliceFBConstructor = failwith "JS only"

  //  ____              _     _      ____            _____ ____
  // |  _ \  ___  _   _| |__ | | ___| __ )  _____  _|  ___| __ )
  // | | | |/ _ \| | | | '_ \| |/ _ \  _ \ / _ \ \/ / |_  |  _ \
  // | |_| | (_) | |_| | |_) | |  __/ |_) | (_) >  <|  _| | |_) |
  // |____/ \___/ \__,_|_.__/|_|\___|____/ \___/_/\_\_|   |____/

  type DoubleBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.VecSize()")>]
    abstract VecSize: uint32

    [<Emit("$0.Min()")>]
    abstract Min: int

    [<Emit("$0.Max()")>]
    abstract Max: int

    [<Emit("$0.Unit()")>]
    abstract Unit: string

    [<Emit("$0.Precision()")>]
    abstract Precision: uint32

    [<Emit("$0.Tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.TagsLength()")>]
    abstract TagsLength: int

    [<Emit("$0.Slices($1)")>]
    abstract Slices: int -> DoubleSliceFB

    [<Emit("$0.SlicesLength()")>]
    abstract SlicesLength: int

  type DoubleBoxFBConstructor =
    abstract prototype: DoubleBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.startDoubleBoxFB($1)")>]
    abstract StartDoubleBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addVecSize($1, $2)")>]
    abstract AddVecSize: builder: FlatBufferBuilder * vecsize: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addMin($1, $2)")>]
    abstract AddMin: builder: FlatBufferBuilder * min: int -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addMax($1, $2)")>]
    abstract AddMax: builder: FlatBufferBuilder * max: int -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addUnit($1, $2)")>]
    abstract AddUnit: builder: FlatBufferBuilder * unit: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addPrecision($1, $2)")>]
    abstract AddPrecision: builder: FlatBufferBuilder * precision: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.endDoubleBoxFB($1)")>]
    abstract EndDoubleBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.getRootAsDoubleBoxFB($1)")>]
    abstract GetRootAsDoubleBoxFB: buffer: ByteBuffer -> DoubleBoxFB

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.DoubleBoxFB()")>]
    abstract Create: unit -> DoubleBoxFB

  let DoubleBoxFB : DoubleBoxFBConstructor = failwith "JS only"

  //  ____        _       ____  _ _          _____ ____
  // | __ ) _   _| |_ ___/ ___|| (_) ___ ___|  ___| __ )
  // |  _ \| | | | __/ _ \___ \| | |/ __/ _ \ |_  |  _ \
  // | |_) | |_| | ||  __/___) | | | (_|  __/  _| | |_) |
  // |____/ \__, |\__\___|____/|_|_|\___\___|_|   |____/
  //        |___/

  type ByteSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value()")>]
    abstract Value: string

  type ByteSliceFBConstructor =
    abstract prototype: ByteSliceFB with get, set

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.startByteSliceFB($1)")>]
    abstract StartByteSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.endByteSliceFB($1)")>]
    abstract EndByteSliceFB: builder: FlatBufferBuilder -> Offset<ByteSliceFB>

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.getRootAsByteSliceFB($1)")>]
    abstract GetRootAsByteSliceFB: buffer: ByteBuffer -> ByteSliceFB

    [<Emit("new Iris.Serialization.Raft.ByteSliceFB()")>]
    abstract Create: unit -> ByteSliceFB

  let ByteSliceFB: ByteSliceFBConstructor = failwith "JS only"

  //  ____        _       ____            _____ ____
  // | __ ) _   _| |_ ___| __ )  _____  _|  ___| __ )
  // |  _ \| | | | __/ _ \  _ \ / _ \ \/ / |_  |  _ \
  // | |_) | |_| | ||  __/ |_) | (_) >  <|  _| | |_) |
  // |____/ \__, |\__\___|____/ \___/_/\_\_|   |____/
  //        |___/

  type ByteBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.TagsLength()")>]
    abstract TagsLength: int

    [<Emit("$0.Tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.SlicesLength()")>]
    abstract SlicesLength: int

    [<Emit("$0.Slices($1)")>]
    abstract Slices: int -> ByteSliceFB

  type ByteBoxFBConstructor =
    abstract prototype: ByteBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.startByteBoxFB($1)")>]
    abstract StartByteBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.endByteBoxFB($1)")>]
    abstract EndByteBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.getRootAsByteBoxFB($1)")>]
    abstract GetRootAsByteBoxFB: buffer: ByteBuffer -> ByteBoxFB

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.ByteBoxFB()")>]
    abstract Create: unit -> ByteBoxFB

  let ByteBoxFB : ByteBoxFBConstructor = failwith "JS only"

  //  _____                       ____                  _____ ____
  // | ____|_ __  _   _ _ __ ___ |  _ \ _ __ ___  _ __ |  ___| __ )
  // |  _| | '_ \| | | | '_ ` _ \| |_) | '__/ _ \| '_ \| |_  |  _ \
  // | |___| | | | |_| | | | | | |  __/| | | (_) | |_) |  _| | |_) |
  // |_____|_| |_|\__,_|_| |_| |_|_|   |_|  \___/| .__/|_|   |____/
  //                                             |_|

  type EnumPropertyFB =
    [<Emit("$0.Key()")>]
    abstract Key: string

    [<Emit("$0.Value()")>]
    abstract Value: string

  type EnumPropertyFBConstructor =
    abstract prototype: EnumPropertyFB with get, set

    [<Emit("Iris.Serialization.Raft.EnumPropertyFB.startEnumPropertyFB($1)")>]
    abstract StartEnumPropertyFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.EnumPropertyFB.addKey($1, $2)")>]
    abstract AddKey: builder: FlatBufferBuilder * key: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.EnumPropertyFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.EnumPropertyFB.endEnumPropertyFB($1)")>]
    abstract EndEnumPropertyFB: builder: FlatBufferBuilder -> Offset<EnumPropertyFB>

    [<Emit("Iris.Serialization.Raft.EnumPropertyFB.getRootAsEnumPropertyFB($1)")>]
    abstract GetRootAsEnumPropertyFB: buffer: ByteBuffer -> EnumPropertyFB

    [<Emit("new Iris.Serialization.Raft.EnumPropertyFB()")>]
    abstract Create: unit -> EnumPropertyFB

  let EnumPropertyFB: EnumPropertyFBConstructor = failwith "JS only"

  //  _____                       ____  _ _          _____ ____
  // | ____|_ __  _   _ _ __ ___ / ___|| (_) ___ ___|  ___| __ )
  // |  _| | '_ \| | | | '_ ` _ \\___ \| | |/ __/ _ \ |_  |  _ \
  // | |___| | | | |_| | | | | | |___) | | | (_|  __/  _| | |_) |
  // |_____|_| |_|\__,_|_| |_| |_|____/|_|_|\___\___|_|   |____/

  type EnumSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value()")>]
    abstract Value: EnumPropertyFB

  type EnumSliceFBConstructor =
    abstract prototype: EnumSliceFB with get, set

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.startEnumSliceFB($1)")>]
    abstract StartEnumSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: Offset<EnumPropertyFB> -> unit

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.endEnumSliceFB($1)")>]
    abstract EndEnumSliceFB: builder: FlatBufferBuilder -> Offset<EnumSliceFB>

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.getRootAsEnumSliceFB($1)")>]
    abstract GetRootAsEnumSliceFB: buffer: ByteBuffer -> EnumSliceFB

    [<Emit("new Iris.Serialization.Raft.EnumSliceFB()")>]
    abstract Create: unit -> EnumSliceFB

  let EnumSliceFB: EnumSliceFBConstructor = failwith "JS only"

  //  _____                       ____            _____ ____
  // | ____|_ __  _   _ _ __ ___ | __ )  _____  _|  ___| __ )
  // |  _| | '_ \| | | | '_ ` _ \|  _ \ / _ \ \/ / |_  |  _ \
  // | |___| | | | |_| | | | | | | |_) | (_) >  <|  _| | |_) |
  // |_____|_| |_|\__,_|_| |_| |_|____/ \___/_/\_\_|   |____/

  type EnumBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.Tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.TagsLength($1)")>]
    abstract TagsLength: int

    [<Emit("$0.Properties($1)")>]
    abstract Properties: int -> EnumPropertyFB

    [<Emit("$0.PropertiesLength()")>]
    abstract PropertiesLength: int

    [<Emit("$0.Slices($1)")>]
    abstract Slices: int -> EnumSliceFB

    [<Emit("$0.SlicesLength()")>]
    abstract SlicesLength: int

  type EnumBoxFBConstructor =
    abstract prototype: EnumBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.startEnumBoxFB($1)")>]
    abstract StartEnumBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.addProperties($1, $2)")>]
    abstract AddProperties: builder: FlatBufferBuilder * properties: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.endEnumBoxFB($1)")>]
    abstract EndEnumBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.getRootAsEnumBoxFB($1)")>]
    abstract GetRootAsEnumBoxFB: buffer: ByteBuffer -> EnumBoxFB

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.createPropertiesVector($1, $2)")>]
    abstract CreatePropertiesVector: builder: FlatBufferBuilder * Offset<EnumPropertyFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.EnumBoxFB()")>]
    abstract Create: unit -> EnumBoxFB

  let EnumBoxFB : EnumBoxFBConstructor = failwith "JS only"

  //   ____      _            ____  _ _          _____ ____
  //  / ___|___ | | ___  _ __/ ___|| (_) ___ ___|  ___| __ )
  // | |   / _ \| |/ _ \| '__\___ \| | |/ __/ _ \ |_  |  _ \
  // | |__| (_) | | (_) | |   ___) | | | (_|  __/  _| | |_) |
  //  \____\___/|_|\___/|_|  |____/|_|_|\___\___|_|   |____/

  type ColorSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value()")>]
    abstract Value: ColorSpaceFB

  type ColorSliceFBConstructor =
    abstract prototype: ColorSliceFB with get, set

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.startColorSliceFB($1)")>]
    abstract StartColorSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: Offset<ColorSpaceFB> -> unit

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.endColorSliceFB($1)")>]
    abstract EndColorSliceFB: builder: FlatBufferBuilder -> Offset<ColorSliceFB>

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.getRootAsColorSliceFB($1)")>]
    abstract GetRootAsColorSliceFB: buffer: ByteBuffer -> ColorSliceFB

    [<Emit("new Iris.Serialization.Raft.ColorSliceFB()")>]
    abstract Create: unit -> ColorSliceFB

  let ColorSliceFB: ColorSliceFBConstructor = failwith "JS only"

  //   ____      _            ____            _____ ____
  //  / ___|___ | | ___  _ __| __ )  _____  _|  ___| __ )
  // | |   / _ \| |/ _ \| '__|  _ \ / _ \ \/ / |_  |  _ \
  // | |__| (_) | | (_) | |  | |_) | (_) >  <|  _| | |_) |
  //  \____\___/|_|\___/|_|  |____/ \___/_/\_\_|   |____/

  type ColorBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.TagsLength()")>]
    abstract TagsLength: int

    [<Emit("$0.Tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.Slices($1)")>]
    abstract Slices: int -> ColorSliceFB

    [<Emit("$0.SlicesLength()")>]
    abstract SlicesLength: int

  type ColorBoxFBConstructor =
    abstract prototype: ColorBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.startColorBoxFB($1)")>]
    abstract StartColorBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.endColorBoxFB($1)")>]
    abstract EndColorBoxFB: builder: FlatBufferBuilder -> Offset<ColorBoxFB>

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.getRootAsColorBoxFB($1)")>]
    abstract GetRootAsColorBoxFB: buffer: ByteBuffer -> ColorBoxFB

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.ColorBoxFB()")>]
    abstract Create: unit -> ColorBoxFB

  let ColorBoxFB : ColorBoxFBConstructor = failwith "JS only"

  //  ____  _        _             ____  _ _          _____ ____
  // / ___|| |_ _ __(_)_ __   __ _/ ___|| (_) ___ ___|  ___| __ )
  // \___ \| __| '__| | '_ \ / _` \___ \| | |/ __/ _ \ |_  |  _ \
  //  ___) | |_| |  | | | | | (_| |___) | | | (_|  __/  _| | |_) |
  // |____/ \__|_|  |_|_| |_|\__, |____/|_|_|\___\___|_|   |____/
  //                         |___/

  type StringSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value()")>]
    abstract Value: string

  type StringSliceFBConstructor =
    abstract prototype: StringSliceFB with get, set

    [<Emit("Iris.Serialization.Raft.StringSliceFB.startStringSliceFB($1)")>]
    abstract StartStringSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.StringSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.StringSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.StringSliceFB.endStringSliceFB($1)")>]
    abstract EndStringSliceFB: builder: FlatBufferBuilder -> Offset<StringSliceFB>

    [<Emit("Iris.Serialization.Raft.StringSliceFB.getRootAsStringSliceFB($1)")>]
    abstract GetRootAsStringSliceFB: buffer: ByteBuffer -> StringSliceFB

    [<Emit("new Iris.Serialization.Raft.StringSliceFB()")>]
    abstract Create: unit -> StringSliceFB

  let StringSliceFB: StringSliceFBConstructor = failwith "JS only"

  //  ____  _        _             ____            _____ ____
  // / ___|| |_ _ __(_)_ __   __ _| __ )  _____  _|  ___| __ )
  // \___ \| __| '__| | '_ \ / _` |  _ \ / _ \ \/ / |_  |  _ \
  //  ___) | |_| |  | | | | | (_| | |_) | (_) >  <|  _| | |_) |
  // |____/ \__|_|  |_|_| |_|\__, |____/ \___/_/\_\_|   |____/
  //                         |___/

  type StringBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.StringType()")>]
    abstract StringType: StringTypeFB

    [<Emit("$0.FileMask()")>]
    abstract FileMask: string

    [<Emit("$0.MaxChars()")>]
    abstract MaxChars: int

    [<Emit("$0.TagsLength()")>]
    abstract TagsLength: int

    [<Emit("$0.Tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.Slices($1)")>]
    abstract Slices: int -> StringSliceFB

    [<Emit("$0.SlicesLength()")>]
    abstract SlicesLength: int

  type StringBoxFBConstructor =
    abstract prototype: StringBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.StringBoxFB.startStringBoxFB($1)")>]
    abstract StartStringBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addStringType($1, $2)")>]
    abstract AddStringType: builder: FlatBufferBuilder * tipe: StringTypeFB -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addFileMask($1, $2)")>]
    abstract AddFileMask: builder: FlatBufferBuilder * mask: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addMaxChars($1, $2)")>]
    abstract AddMaxChars: builder: FlatBufferBuilder * max: int -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.endStringBoxFB($1)")>]
    abstract EndStringBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StringBoxFB.getRootAsStringBoxFB($1)")>]
    abstract GetRootAsStringBoxFB: buffer: ByteBuffer -> StringBoxFB

    [<Emit("Iris.Serialization.Raft.StringBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StringBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<StringSliceFB> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.StringBoxFB()")>]
    abstract Create: unit -> StringBoxFB

  let StringBoxFB : StringBoxFBConstructor = failwith "JS only"

  //   ____                                            _ ____  _ _          _____
  //  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| / ___|| (_) ___ ___|  ___|
  // | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` \___ \| | |/ __/ _ \ |_
  // | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| |___) | | | (_|  __/  _|
  //  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/|_|_|\___\___|_| B
  //                      |_|

  type CompoundSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: uint32

    [<Emit("$0.Value($1)")>]
    abstract Value: int -> IOBoxFB

    [<Emit("$0.ValueLength()")>]
    abstract ValueLength: int

  type CompoundSliceFBConstructor =
    abstract prototype: CompoundSliceFB with get, set

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.startCompoundSliceFB($1)")>]
    abstract StartCompoundSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.createValueVector($1, $2)")>]
    abstract CreateValueVector: builder: FlatBufferBuilder * value: Offset<IOBoxFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.endCompoundSliceFB($1)")>]
    abstract EndCompoundSliceFB: builder: FlatBufferBuilder -> Offset<CompoundSliceFB>

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.getRootAsCompoundSliceFB($1)")>]
    abstract GetRootAsCompoundSliceFB: buffer: ByteBuffer -> CompoundSliceFB

    [<Emit("new Iris.Serialization.Raft.CompoundSliceFB()")>]
    abstract Create: unit -> CompoundSliceFB

  let CompoundSliceFB: CompoundSliceFBConstructor = failwith "JS only"

  //   ____                                            _ ____
  //  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| | __ )  _____  __
  // | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` |  _ \ / _ \ \/ /
  // | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| | |_) | (_) >  <
  //  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/ \___/_/\_\
  //                      |_|

  type CompoundBoxFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.Patch()")>]
    abstract Patch: string

    [<Emit("$0.Tags($1)")>]
    abstract Tags: int -> string

    [<Emit("$0.TagsLength()")>]
    abstract TagsLength: int

    [<Emit("$0.Slices($1)")>]
    abstract Slices: int -> CompoundSliceFB

    [<Emit("$0.SlicesLength()")>]
    abstract SlicesLength: int

  type CompoundBoxFBConstructor =
    abstract prototype: CompoundBoxFB with get, set

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.startCompoundBoxFB($1)")>]
    abstract StartCompoundBoxFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.addPatch($1, $2)")>]
    abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.endCompoundBoxFB($1)")>]
    abstract EndCompoundBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.getRootAsCompoundBoxFB($1)")>]
    abstract GetRootAsCompoundBoxFB: buffer: ByteBuffer -> CompoundBoxFB

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.CompoundBoxFB()")>]
    abstract Create: unit -> CompoundBoxFB

  let CompoundBoxFB : CompoundBoxFBConstructor = failwith "JS only"

  //  ____  _ _         _____                 _____ ____
  // / ___|| (_) ___ __|_   _|   _ _ __   ___|  ___| __ )
  // \___ \| | |/ __/ _ \| || | | | '_ \ / _ \ |_  |  _ \
  //  ___) | | | (_|  __/| || |_| | |_) |  __/  _| | |_) |
  // |____/|_|_|\___\___||_| \__, | .__/ \___|_|   |____/
  //                         |___/|_|

  type SliceTypeFB = int

  type SliceTypeFBConstructor =

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.StringSliceFB")>]
    abstract StringSliceFB : SliceTypeFB

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.IntSliceFB")>]
    abstract IntSliceFB : SliceTypeFB

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.FloatSliceFB")>]
    abstract FloatSliceFB : SliceTypeFB

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.DoubleSliceFB")>]
    abstract DoubleSliceFB : SliceTypeFB

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.BoolSliceFB")>]
    abstract BoolSliceFB : SliceTypeFB

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.ByteSliceFB")>]
    abstract ByteSliceFB : SliceTypeFB

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.EnumSliceFB")>]
    abstract EnumSliceFB : SliceTypeFB

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.ColorSliceFB")>]
    abstract ColorSliceFB : SliceTypeFB

    [<Emit("Iris.Serialization.Raft.SliceTypeFB.CompoundSliceFB")>]
    abstract CompoundSliceFB : SliceTypeFB

  let SliceTypeFB: SliceTypeFBConstructor = failwith "JS only"

  //  ____  _ _          _____ ____
  // / ___|| (_) ___ ___|  ___| __ )
  // \___ \| | |/ __/ _ \ |_  |  _ \
  //  ___) | | | (_|  __/  _| | |_) |
  // |____/|_|_|\___\___|_|   |____/

  type SliceFB =

    [<Emit("$0.Slice($1)")>]
    abstract Slice: 'a -> 'a

    [<Emit("$0.SliceType()")>]
    abstract SliceType: int

  type SliceFBConstructor =

    [<Emit("Iris.Serialization.Raft.SliceFB.startSliceFB($1)")>]
    abstract StartSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.SliceFB.addSlice($1,$2)")>]
    abstract AddSlice: builder: FlatBufferBuilder * offset: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.SliceFB.addSliceType($1,$2)")>]
    abstract AddSliceType: builder: FlatBufferBuilder * tipe: SliceTypeFB -> unit

    [<Emit("Iris.Serialization.Raft.SliceFB.endSliceFB($1)")>]
    abstract EndSliceFB: builder: FlatBufferBuilder -> Offset<SliceFB>

    [<Emit("Iris.Serialization.Raft.SliceFB.getRootAsSliceFB($1)")>]
    abstract GetRootAsSliceFB: bytes: ByteBuffer -> SliceFB

  let SliceFB: SliceFBConstructor = failwith "JS only"

  //   ____           _____ ____
  //  / ___|   _  ___|  ___| __ )
  // | |  | | | |/ _ \ |_  |  _ \
  // | |__| |_| |  __/  _| | |_) |
  //  \____\__,_|\___|_|   |____/

  type CueFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.IOBoxesLength()")>]
    abstract IOBoxesLength: int

    [<Emit("$0.IOBoxes($1)")>]
    abstract IOBoxes: int -> IOBoxFB

  type CueFBConstructor =
    abstract prototype: CueFB with get, set

    [<Emit("Iris.Serialization.Raft.CueFB.startCueFB($1)")>]
    abstract StartCueFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.CueFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.CueFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.CueFB.addIOBoxes($1, $2)")>]
    abstract AddIOBoxes: builder: FlatBufferBuilder * ioboxes: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.CueFB.endCueFB($1)")>]
    abstract EndCueFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.CueFB.getRootAsCueFB($1)")>]
    abstract GetRootAsCueFB: buffer: ByteBuffer -> CueFB

    [<Emit("Iris.Serialization.Raft.CueFB.createIOBoxesVector($1, $2)")>]
    abstract CreateIOBoxesVector: builder: FlatBufferBuilder * Offset<IOBoxFB> array -> Offset<'a>

    [<Emit("new Iris.Serialization.Raft.CueFB()")>]
    abstract Create: unit -> CueFB

  let CueFB : CueFBConstructor = failwith "JS only"

  //  ____       _       _     _____ ____
  // |  _ \ __ _| |_ ___| |__ |  ___| __ )
  // | |_) / _` | __/ __| '_ \| |_  |  _ \
  // |  __/ (_| | || (__| | | |  _| | |_) |
  // |_|   \__,_|\__\___|_| |_|_|   |____/

  type PatchFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.IOBoxesLength()")>]
    abstract IOBoxesLength: int

    [<Emit("$0.IOBoxes($1)")>]
    abstract IOBoxes: int -> IOBoxFB

  type PatchFBConstructor =
    abstract prototype: PatchFB with get, set

    [<Emit("Iris.Serialization.Raft.PatchFB.startPatchFB($1)")>]
    abstract StartPatchFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.PatchFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.PatchFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.PatchFB.addIOBoxes($1, $2)")>]
    abstract AddIOBoxes: builder: FlatBufferBuilder * ioboxes: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.PatchFB.endPatchFB($1)")>]
    abstract EndPatchFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.PatchFB.getRootAsPatchFB($1)")>]
    abstract GetRootAsPatchFB: buffer: ByteBuffer -> PatchFB

    [<Emit("Iris.Serialization.Raft.PatchFB.createIOBoxesVector($1, $2)")>]
    abstract CreateIOBoxesVector: builder: FlatBufferBuilder * Offset<IOBoxFB> array -> Offset<'a>

  let PatchFB : PatchFBConstructor = failwith "JS only"

  //   ____           _     _     _   _____ ____
  //  / ___|   _  ___| |   (_)___| |_|  ___| __ )
  // | |  | | | |/ _ \ |   | / __| __| |_  |  _ \
  // | |__| |_| |  __/ |___| \__ \ |_|  _| | |_) |
  //  \____\__,_|\___|_____|_|___/\__|_|   |____/

  type CueListFB =
    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.Name()")>]
    abstract Name: string

    [<Emit("$0.CuesLength()")>]
    abstract CuesLength: int

    [<Emit("$0.Cues($1)")>]
    abstract Cues: int -> CueFB

  type CueListFBConstructor =
    abstract prototype: CueListFB with get, set

    [<Emit("Iris.Serialization.Raft.CueListFB.startCueListFB($1)")>]
    abstract StartCueListFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.CueListFB.addId($1, $2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.CueListFB.addName($1, $2)")>]
    abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.CueListFB.addCues($1, $2)")>]
    abstract AddCues: builder: FlatBufferBuilder * cues: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.CueListFB.endCueListFB($1)")>]
    abstract EndCueListFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.CueListFB.getRootAsCueListFB($1)")>]
    abstract GetRootAsCueListFB: buffer: ByteBuffer -> CueListFB

    [<Emit("Iris.Serialization.Raft.CueListFB.createCuesVector($1, $2)")>]
    abstract CreateCuesVector: builder: FlatBufferBuilder * Offset<CueFB> array -> Offset<'a>

  let CueListFB : CueListFBConstructor = failwith "JS only"

  //  _   _           _      ____  _        _       _____ ____
  // | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___|  ___| __ )
  // |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \ |_  |  _ \
  // | |\  | (_) | (_| |  __/___) | || (_| | ||  __/  _| | |_) |
  // |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|_|   |____/

  type NodeStateFB = int

  type NodeStateFBConstructor =

    [<Emit("Iris.Serialization.Raft.NodeStateFB.JoiningFB")>]
    abstract JoiningFB: NodeStateFB

    [<Emit("Iris.Serialization.Raft.NodeStateFB.RunningFB")>]
    abstract RunningFB: NodeStateFB

    [<Emit("Iris.Serialization.Raft.NodeStateFB.FailedFB")>]
    abstract FailedFB: NodeStateFB

  let NodeStateFB: NodeStateFBConstructor = failwith "JS only"

  //  _   _           _
  // | \ | | ___   __| | ___
  // |  \| |/ _ \ / _` |/ _ \
  // | |\  | (_) | (_| |  __/
  // |_| \_|\___/ \__,_|\___|

  type NodeFB =

    [<Emit("$0.Id()")>]
    abstract Id: string

    [<Emit("$0.HostName()")>]
    abstract HostName: string

    [<Emit("$0.IpAddr()")>]
    abstract IpAddr: string

    [<Emit("$0.Port()")>]
    abstract Port: uint16

    [<Emit("$0.WebPort()")>]
    abstract WebPort: uint16

    [<Emit("$0.WsPort()")>]
    abstract WsPort: uint16

    [<Emit("$0.GitPort()")>]
    abstract GitPort: uint16

    [<Emit("$0.Voting()")>]
    abstract Voting: bool

    [<Emit("$0.VotedForMe()")>]
    abstract VotedForMe: bool

    [<Emit("$0.State()")>]
    abstract State: NodeStateFB

    [<Emit("$0.NextIndex()")>]
    abstract NextIndex: uint32

    [<Emit("$0.MatchIndex()")>]
    abstract MatchIndex: uint32

  type NodeFBConstructor =
    abstract prototype: NodeFB with get, set

    [<Emit("Iris.Serialization.Raft.NodeFB.startNodeFB($1)")>]
    abstract StartNodeFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addId($1,$2)")>]
    abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addHostName($1,$2)")>]
    abstract AddHostName: builder: FlatBufferBuilder * hostname: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addIpAddr($1,$2)")>]
    abstract AddIpAddr: builder: FlatBufferBuilder * ip: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addPort($1,$2)")>]
    abstract AddPort: builder: FlatBufferBuilder * port: int -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addWebPort($1,$2)")>]
    abstract AddWebPort: builder: FlatBufferBuilder * port: int -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addWsPort($1,$2)")>]
    abstract AddWsPort: builder: FlatBufferBuilder * port: int -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addGitPort($1,$2)")>]
    abstract AddGitPort: builder: FlatBufferBuilder * port: int -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addVoting($1,$2)")>]
    abstract AddVoting: builder: FlatBufferBuilder * voting: bool -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addVotedForMe($1,$2)")>]
    abstract AddVotedForMe: builder: FlatBufferBuilder * votedforme: bool -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addState($1,$2)")>]
    abstract AddState: builder: FlatBufferBuilder * state: NodeStateFB -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addNextIndex($1,$2)")>]
    abstract AddNextIndex: builder: FlatBufferBuilder * idx: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.addMatchIndex($1,$2)")>]
    abstract AddMatchIndex: builder: FlatBufferBuilder * idx: uint32 -> unit

    [<Emit("Iris.Serialization.Raft.NodeFB.endNodeFB($1)")>]
    abstract EndNodeFB: builder: FlatBufferBuilder -> Offset<NodeFB>

    [<Emit("Iris.Serialization.Raft.NodeFB.getRootAsNodeFB($1)")>]
    abstract GetRootAsNodeFB: bytes: ByteBuffer -> NodeFB

    [<Emit("new Iris.Serialization.Raft.NodeFB()")>]
    abstract Create: unit -> NodeFB

  let NodeFB: NodeFBConstructor = failwith "JS only"

  //   ____ _                           _____
  //  / ___| |__   __ _ _ __   __ _  __|_   _|   _ _ __   ___
  // | |   | '_ \ / _` | '_ \ / _` |/ _ \| || | | | '_ \ / _ \
  // | |___| | | | (_| | | | | (_| |  __/| || |_| | |_) |  __/
  //  \____|_| |_|\__,_|_| |_|\__, |\___||_| \__, | .__/ \___|
  //                          |___/          |___/|_|

  type ConfigChangeTypeFB = int

  type ConfigChangeTypeFBConstructor =

    [<Emit("Iris.Serialization.Raft.ConfigChangeTypeFB.NodeAddedFB")>]
    abstract NodeAdded: ConfigChangeTypeFB

    [<Emit("Iris.Serialization.Raft.ConfigChangeTypeFB.NodeRemovedFB")>]
    abstract NodeRemoved: ConfigChangeTypeFB

  let ConfigChangeTypeFB: ConfigChangeTypeFBConstructor = failwith "JS only"

  //   ____             __ _        ____ _                            _____ ____
  //  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___|  ___| __ )
  // | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \ |_  |  _ \
  // | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/  _| | |_) |
  //  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|_|   |____/
  //                         |___/                         |___/

  type ConfigChangeFB =

    [<Emit("$0.Type()")>]
    abstract Type: ConfigChangeTypeFB

    [<Emit("$0.Node()")>]
    abstract Node: NodeFB

  type ConfigChangeFBConstructor =
    abstract prototype: ConfigChangeFB with get, set

    [<Emit("Iris.Serialization.Raft.ConfigChangeFB.startConfigChangeFB($1)")>]
    abstract StartConfigChangeFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ConfigChangeFB.addType($1, $2)")>]
    abstract AddType: builder: FlatBufferBuilder * tipe: ConfigChangeTypeFB -> unit

    [<Emit("Iris.Serialization.Raft.ConfigChangeFB.addNode($1, $2)")>]
    abstract AddNode: builder: FlatBufferBuilder * node: Offset<NodeFB> -> unit

    [<Emit("Iris.Serialization.Raft.ConfigChangeFB.endConfigChangeFB($1)")>]
    abstract EndConfigChangeFB: builder: FlatBufferBuilder -> Offset<ConfigChangeFB>

    [<Emit("Iris.Serialization.Raft.ConfigChangeFB.getRootAsConfigChangeFB($1)")>]
    abstract GetRootAsConfigChangeFB: bytes: ByteBuffer -> ConfigChangeFB

    [<Emit("new Iris.Serialization.Raft.ConfigChangeFB()")>]
    abstract Create: unit -> ConfigChangeFB

  let ConfigChangeFB: ConfigChangeFBConstructor = failwith "JS only"

  //  ____  _        _       _____ ____
  // / ___|| |_ __ _| |_ ___|  ___| __ )
  // \___ \| __/ _` | __/ _ \ |_  |  _ \
  //  ___) | || (_| | ||  __/  _| | |_) |
  // |____/ \__\__,_|\__\___|_|   |____/

  type StateFB =
    [<Emit("$0.Patches($1)")>]
    abstract Patches: int -> PatchFB

    [<Emit("$0.PatchesLength()")>]
    abstract PatchesLength: int

    [<Emit("$0.IOBoxes($1)")>]
    abstract IOBoxes: int -> IOBoxFB

    [<Emit("$0.IOBoxesLength()")>]
    abstract IOBoxesLength: int

    [<Emit("$0.Cues($1)")>]
    abstract Cues: int -> CueFB

    [<Emit("$0.CuesLength()")>]
    abstract CuesLength: int

    [<Emit("$0.CueLists($1)")>]
    abstract CueLists: int -> CueListFB

    [<Emit("$0.CueListsLength()")>]
    abstract CueListsLength: int

    [<Emit("$0.Nodes($1)")>]
    abstract Nodes: int -> NodeFB

    [<Emit("$0.NodesLength()")>]
    abstract NodesLength: int

    [<Emit("$0.Sessions($1)")>]
    abstract Sessions: int -> SessionFB

    [<Emit("$0.SessionsLength()")>]
    abstract SessionsLength: int

    [<Emit("$0.Users($1)")>]
    abstract Users: int -> UserFB

    [<Emit("$0.UsersLength()")>]
    abstract UsersLength: int

  type StateFBConstructor =
    abstract prototype: StateFB with get, set

    [<Emit("Iris.Serialization.Raft.StateFB.startStateFB($1)")>]
    abstract StartStateFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.StateFB.createPatchesVector($1, $2)")>]
    abstract CreatePatchesVector: builder: FlatBufferBuilder * patches: Offset<PatchFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StateFB.addPatches($1, $2)")>]
    abstract AddPatches: builder: FlatBufferBuilder * patches: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StateFB.createIOBoxesVector($1, $2)")>]
    abstract CreateIOBoxesVector: builder: FlatBufferBuilder * patches: Offset<IOBoxFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StateFB.addIOBoxes($1, $2)")>]
    abstract AddIOBoxes: builder: FlatBufferBuilder * patches: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StateFB.createCuesVector($1, $2)")>]
    abstract CreateCuesVector: builder: FlatBufferBuilder * patches: Offset<CueFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StateFB.addCues($1, $2)")>]
    abstract AddCues: builder: FlatBufferBuilder * patches: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StateFB.createCueListsVector($1, $2)")>]
    abstract CreateCueListsVector: builder: FlatBufferBuilder * patches: Offset<CueListFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StateFB.addCueLists($1, $2)")>]
    abstract AddCueLists: builder: FlatBufferBuilder * patches: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StateFB.createNodesVector($1, $2)")>]
    abstract CreateNodesVector: builder: FlatBufferBuilder * patches: Offset<NodeFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StateFB.addNodes($1, $2)")>]
    abstract AddNodes: builder: FlatBufferBuilder * patches: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StateFB.createSessionsVector($1, $2)")>]
    abstract CreateSessionsVector: builder: FlatBufferBuilder * patches: Offset<SessionFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StateFB.addSessions($1, $2)")>]
    abstract AddSessions: builder: FlatBufferBuilder * patches: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StateFB.createUsersVector($1, $2)")>]
    abstract CreateUsersVector: builder: FlatBufferBuilder * patches: Offset<UserFB> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StateFB.addUsers($1, $2)")>]
    abstract AddUsers: builder: FlatBufferBuilder * patches: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.StateFB.endStateFB($1)")>]
    abstract EndStateFB: builder: FlatBufferBuilder -> Offset<StateFB>

    [<Emit("Iris.Serialization.Raft.StateFB.getRootAsStateFB($1)")>]
    abstract GetRootAsStateFB: bytes: ByteBuffer -> StateFB

  let StateFB: StateFBConstructor = failwith "JS only"

  //  _                __  __           _____ ____
  // | |    ___   __ _|  \/  |___  __ _|  ___| __ )
  // | |   / _ \ / _` | |\/| / __|/ _` | |_  |  _ \
  // | |__| (_) | (_| | |  | \__ \ (_| |  _| | |_) |
  // |_____\___/ \__, |_|  |_|___/\__, |_|   |____/
  //             |___/            |___/

  type LogMsgFB =
    [<Emit("$0.LogLevel()")>]
    abstract LogLevel: string

    [<Emit("$0.Msg()")>]
    abstract Msg: string

  type LogMsgFBConstructor =
    abstract prototype: LogMsgFB with get, set

    [<Emit("Iris.Serialization.Raft.LogMsgFB.startLogMsgFB($1)")>]
    abstract StartLogMsgFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.LogMsgFB.addLogLevel($1, $2)")>]
    abstract AddLogLevel: builder: FlatBufferBuilder * level: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.LogMsgFB.addMsg($1, $2)")>]
    abstract AddMsg: builder: FlatBufferBuilder * msg: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.LogMsgFB.endLogMsgFB($1)")>]
    abstract EndLogMsgFB: builder: FlatBufferBuilder -> Offset<LogMsgFB>

    [<Emit("Iris.Serialization.Raft.LogMsgFB.getRootAsLogMsgFB($1)")>]
    abstract GetRootAsLogMsgFB: bytes: ByteBuffer -> LogMsgFB

    [<Emit("new Iris.Serialization.Raft.LogMsgFB()")>]
    abstract Create: unit -> LogMsgFB

  let LogMsgFB: LogMsgFBConstructor = failwith "JS only"

  //     _          _    _        _   _
  //    / \   _ __ (_)  / \   ___| |_(_) ___  _ __
  //   / _ \ | '_ \| | / _ \ / __| __| |/ _ \| '_ \
  //  / ___ \| |_) | |/ ___ \ (__| |_| | (_) | | | |
  // /_/   \_\ .__/|_/_/   \_\___|\__|_|\___/|_| |_|
  //         |_|

  type ActionTypeFB = int

  type ActionTypeFBConstructor =

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.AddFB")>]
    abstract AddFB: ActionTypeFB

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.UpdateFB")>]
    abstract UpdateFB: ActionTypeFB

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.RemoveFB")>]
    abstract RemoveFB: ActionTypeFB

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.LogMsgFB")>]
    abstract LogMsgFB: ActionTypeFB

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.UndoFB")>]
    abstract UndoFB: ActionTypeFB

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.RedoFB")>]
    abstract RedoFB: ActionTypeFB

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.ResetFB")>]
    abstract ResetFB: ActionTypeFB

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.SaveProjectFB")>]
    abstract SaveProjectFB: ActionTypeFB

    [<Emit("Iris.Serialization.Raft.ActionTypeFB.DataSnapshotFB")>]
    abstract DataSnapshotFB: ActionTypeFB

  let ActionTypeFB: ActionTypeFBConstructor = failwith "JS only"

  type PayloadFB = int

  type PayloadFBConstructor =
    [<Emit("Iris.Serialization.Raft.PayloadFB.NONE")>]
    abstract NONE: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.CueFB")>]
    abstract CueFB: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.CueListFB")>]
    abstract CueListFB: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.IOBoxFB")>]
    abstract IOBoxFB: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.PatchFB")>]
    abstract PatchFB: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.NodeFB")>]
    abstract NodeFB: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.UserFB")>]
    abstract UserFB: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.SessionFB")>]
    abstract SessionFB: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.LogMsgFB")>]
    abstract LogMsgFB: PayloadFB

    [<Emit("Iris.Serialization.Raft.PayloadFB.StateFB")>]
    abstract StateFB: PayloadFB

  let PayloadFB: PayloadFBConstructor = failwith "JS only"

  type ApiActionFB =

    [<Emit("$0.Action()")>]
    abstract Action: ActionTypeFB

    [<Emit("$0.PayloadType()")>]
    abstract PayloadType: PayloadFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.CueFB())")>]
    abstract CueFB: CueFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.CueListFB())")>]
    abstract CueListFB: CueListFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.IOBoxFB())")>]
    abstract IOBoxFB: IOBoxFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.PatchFB())")>]
    abstract PatchFB: PatchFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.NodeFB())")>]
    abstract NodeFB: NodeFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.UserFB())")>]
    abstract UserFB: UserFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.SessionFB())")>]
    abstract SessionFB: SessionFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.LogMsgFB())")>]
    abstract LogMsgFB: LogMsgFB

    [<Emit("$0.Payload(new Iris.Serialization.Raft.StateFB())")>]
    abstract StateFB: StateFB

    [<Emit("$0.Payload($1)")>]
    abstract Payload: 'a -> 'a

  type ApiActionFBConstructor =
    abstract prototype: ApiActionFB with get, set

    [<Emit("Iris.Serialization.Raft.ApiActionFB.startApiActionFB($1)")>]
    abstract StartApiActionFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ApiActionFB.addAction($1,$2)")>]
    abstract AddAction: builder: FlatBufferBuilder * tipe: ActionTypeFB -> unit

    [<Emit("Iris.Serialization.Raft.ApiActionFB.addPayloadType($1,$2)")>]
    abstract AddPayloadType: builder: FlatBufferBuilder * tipe: PayloadFB -> unit

    [<Emit("Iris.Serialization.Raft.ApiActionFB.addPayload($1,$2)")>]
    abstract AddPayload: builder: FlatBufferBuilder * payload: Offset<'a> -> unit

    [<Emit("Iris.Serialization.Raft.ApiActionFB.endApiActionFB($1)")>]
    abstract EndApiActionFB: builder: FlatBufferBuilder -> Offset<ApiActionFB>

    [<Emit("Iris.Serialization.Raft.ApiActionFB.getRootAsApiActionFB($1)")>]
    abstract GetRootAsApiActionFB: bytes: ByteBuffer -> ApiActionFB

  let ApiActionFB: ApiActionFBConstructor = failwith "JS only"

  //  _____                     _____ ____
  // | ____|_ __ _ __ ___  _ __|  ___| __ )
  // |  _| | '__| '__/ _ \| '__| |_  |  _ \
  // | |___| |  | | | (_) | |  |  _| | |_) |
  // |_____|_|  |_|  \___/|_|  |_|   |____/

  type ErrorTypeFB = int

  type ErrorTypeFBConstructor =

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.OKFB")>]
    abstract OKFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.BranchNotFoundFB")>]
    abstract BranchNotFoundFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.BranchDetailsNotFoundFB")>]
    abstract BranchDetailsNotFoundFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.RepositoryNotFoundFB")>]
    abstract RepositoryNotFoundFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.RepositoryInitFailedFB")>]
    abstract RepositoryInitFailedFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.CommitErrorFB")>]
    abstract CommitErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.GitErrorFB")>]
    abstract GitErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.ProjectNotFoundFB")>]
    abstract ProjectNotFoundFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.ProjectParseErrorFB")>]
    abstract ProjectParseErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.ProjectPathErrorFB")>]
    abstract ProjectPathErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.ProjectSaveErrorFB")>]
    abstract ProjectSaveErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.ProjectInitErrorFB")>]
    abstract ProjectInitErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.MetaDataNotFoundFB")>]
    abstract MetaDataNotFoundFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.MissingStartupDirFB")>]
    abstract MissingStartupDirFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.CliParseErrorFB")>]
    abstract CliParseErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.MissingNodeIdFB")>]
    abstract MissingNodeIdFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.MissingNodeFB")>]
    abstract MissingNodeFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.AssetSaveErrorFB")>]
    abstract AssetSaveErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.AssetDeleteErrorFB")>]
    abstract AssetDeleteErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.ParseErrorFB")>]
    abstract ParseErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.SocketErrorFB")>]
    abstract ParseErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.OtherFB")>]
    abstract OtherFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.AlreadyVotedFB")>]
    abstract AlreadyVotedFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.AppendEntryFailedFB")>]
    abstract AppendEntryFailedFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.CandidateUnknownFB")>]
    abstract CandidateUnknownFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.EntryInvalidatedFB")>]
    abstract EntryInvalidatedFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.InvalidCurrentIndexFB")>]
    abstract InvalidCurrentIndexFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.InvalidLastLogFB")>]
    abstract InvalidLastLogFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.InvalidLastLogTermFB")>]
    abstract InvalidLastLogTermFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.InvalidTermFB")>]
    abstract InvalidTermFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.LogFormatErrorFB")>]
    abstract LogFormatErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.LogIncompleteFB")>]
    abstract LogIncompleteFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.NoErrorFB")>]
    abstract NoErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.NoNodeFB")>]
    abstract NoNodeFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.NotCandidateFB")>]
    abstract NotCandidateFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.NotLeaderFB")>]
    abstract NotLeaderFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.NotVotingStateFB")>]
    abstract NotVotingStateFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.ResponseTimeoutFB")>]
    abstract ResponseTimeoutFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.SnapshotFormatErrorFB")>]
    abstract SnapshotFormatErrorFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.StaleResponseFB")>]
    abstract StaleResponseFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.UnexpectedVotingChangeFB")>]
    abstract UnexpectedVotingChangeFB: ErrorTypeFB

    [<Emit("Iris.Serialization.Raft.ErrorTypeFB.VoteTermMismatchFB")>]
    abstract VoteTermMismatchFB: ErrorTypeFB

  let ErrorTypeFB: ErrorTypeFBConstructor = failwith "JS only"

  type ErrorFB =
    [<Emit("$0.Type()")>]
    abstract Type: ErrorTypeFB

    [<Emit("$0.Message()")>]
    abstract Message: string

  type ErrorFBConstructor =
    abstract prototype: ErrorFB with get, set

    [<Emit("Iris.Serialization.Raft.ErrorFB.startErrorFB($1)")>]
    abstract StartErrorFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ErrorFB.addType($1, $2)")>]
    abstract AddType: builder: FlatBufferBuilder * tipe: ErrorTypeFB -> unit

    [<Emit("Iris.Serialization.Raft.ErrorFB.addMessage($1, $2)")>]
    abstract AddMessage: builder: FlatBufferBuilder * msg: Offset<string> -> unit

    [<Emit("Iris.Serialization.Raft.ErrorFB.endErrorFB($1)")>]
    abstract EndErrorFB: builder: FlatBufferBuilder -> Offset<ErrorFB>

    [<Emit("Iris.Serialization.Raft.ErrorFB.getRootAsErrorFB($1)")>]
    abstract GetRootAsErrorFB: bytes: ByteBuffer -> ErrorFB

    [<Emit("new Iris.Serialization.Raft.ErrorFB()")>]
    abstract Create: unit -> ErrorFB

  let ErrorFB: ErrorFBConstructor = failwith "JS only"
