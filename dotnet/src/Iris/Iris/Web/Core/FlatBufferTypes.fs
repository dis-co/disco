namespace Iris.Web.Core

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers

[<Import("Iris", from="buffers")>]
module FlatBufferTypes =

  //  _   _               _____ ____
  // | | | |___  ___ _ __|  ___| __ )
  // | | | / __|/ _ \ '__| |_  |  _ \
  // | |_| \__ \  __/ |  |  _| | |_) |
  //  \___/|___/\___|_|  |_|   |____/

  type UserFB =
    [<Emit("$0.id()")>]
    abstract Id: string

    [<Emit("$0.userName()")>]
    abstract UserName: string

    [<Emit("$0.firstName()")>]
    abstract FirstName: string

    [<Emit("$0.lastName()")>]
    abstract LastName: string

    [<Emit("$0.email()")>]
    abstract Email: string

    [<Emit("$0.joined()")>]
    abstract Joined: string

    [<Emit("$0.created()")>]
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
    [<Emit("Iris.Serialization.Raft.ColorSpaceTypeFB.NONE")>]
    abstract NONE : int

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

    [<Emit("$0.Value($1))")>]
    abstract Value: 'a -> 'a

  type ColorSpaceFBConstructor =
    abstract prototype: ColorSpaceFB with get, set

    [<Emit("Iris.Serialization.Raft.ColorSpaceFB.startColorSpaceFB($1)")>]
    abstract StartColorSpaceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ColorSpaceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * offset: int -> unit

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
    abstract SimpleFB: int

    [<Emit("Iris.Serialization.Raft.StringTypeFB.MultiLineFB")>]
    abstract MultiLineFB: int

    [<Emit("Iris.Serialization.Raft.StringTypeFB.FileNameFB")>]
    abstract FileNameFB: int

    [<Emit("Iris.Serialization.Raft.StringTypeFB.DirectoryFB")>]
    abstract DirectoryFB: int

    [<Emit("Iris.Serialization.Raft.StringTypeFB.UrlFB")>]
    abstract UrlFB: int

    [<Emit("Iris.Serialization.Raft.StringTypeFB.IPFB")>]
    abstract IPFB: int

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
    abstract StringBoxFB: int

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.IntBoxFB")>]
    abstract IntBoxFB: int

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.FloatBoxFB")>]
    abstract FloatBoxFB: int

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.DoubleBoxFB")>]
    abstract DoubleBoxFB: int

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.BoolBoxFB")>]
    abstract BoolBoxFB: int

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.ByteBoxFB")>]
    abstract ByteBoxFB: int

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.EnumBoxFB")>]
    abstract EnumBoxFB: int

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.ColorBoxFB")>]
    abstract ColorBoxFB: int

    [<Emit("Iris.Serialization.Raft.IOBoxTypeFB.CompoundBoxFB")>]
    abstract CompoundBoxFB: int

  let IOBoxTypeFB: IOBoxTypeFBConstructor = failwith "JS only"

  //  ___ ___  ____            _____ ____
  // |_ _/ _ \| __ )  _____  _|  ___| __ )
  //  | | | | |  _ \ / _ \ \/ / |_  |  _ \
  //  | | |_| | |_) | (_) >  <|  _| | |_) |
  // |___\___/|____/ \___/_/\_\_|   |____/

  type IOBoxFB =

    [<Emit("$0.Value($1))")>]
    abstract IOBox: 'a -> 'a

    [<Emit("$0.ValueType()")>]
    abstract IOBoxType: int

  //  ____              _ ____  _ _          _____ ____
  // | __ )  ___   ___ | / ___|| (_) ___ ___|  ___| __ )
  // |  _ \ / _ \ / _ \| \___ \| | |/ __/ _ \ |_  |  _ \
  // | |_) | (_) | (_) | |___) | | | (_|  __/  _| | |_) |
  // |____/ \___/ \___/|_|____/|_|_|\___\___|_|   |____/

  type BoolSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type BoolSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.BoolSliceFB.startBoolSliceFB($1)")>]
    abstract StartBoolSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.BoolSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.BoolSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.BoolSliceFB.endBoolSliceFB($1)")>]
    abstract EndBoolSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.BoolSliceFB.getRootAsBoolSliceFB($1)")>]
    abstract GetRootAsBoolSliceFB: buffer: ByteBuffer -> BoolSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: BoolSliceFB array

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
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.endBoolBoxFB($1)")>]
    abstract EndBoolBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.getRootAsBoolBoxFB($1)")>]
    abstract GetRootAsBoolBoxFB: buffer: ByteBuffer -> BoolBoxFB

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.BoolBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let BoolBoxFB : BoolBoxFBConstructor = failwith "JS only"

  //  ___       _   ____  _ _          _____ ____
  // |_ _|_ __ | |_/ ___|| (_) ___ ___|  ___| __ )
  //  | || '_ \| __\___ \| | |/ __/ _ \ |_  |  _ \
  //  | || | | | |_ ___) | | | (_|  __/  _| | |_) |
  // |___|_| |_|\__|____/|_|_|\___\___|_|   |____/

  type IntSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type IntSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.IntSliceFB.startIntSliceFB($1)")>]
    abstract StartIntSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.IntSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.IntSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.IntSliceFB.endIntSliceFB($1)")>]
    abstract EndIntSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.IntSliceFB.getRootAsIntSliceFB($1)")>]
    abstract GetRootAsIntSliceFB: buffer: ByteBuffer -> IntSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: IntSliceFB array

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

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.IntBoxFB.endIntBoxFB($1)")>]
    abstract EndIntBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.IntBoxFB.getRootAsIntBoxFB($1)")>]
    abstract GetRootAsIntBoxFB: buffer: ByteBuffer -> IntBoxFB

    [<Emit("Iris.Serialization.Raft.IntBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.IntBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let IntBoxFB : IntBoxFBConstructor = failwith "JS only"

  //  _____ _             _   ____  _ _          _____ ____
  // |  ___| | ___   __ _| |_/ ___|| (_) ___ ___|  ___| __ )
  // | |_  | |/ _ \ / _` | __\___ \| | |/ __/ _ \ |_  |  _ \
  // |  _| | | (_) | (_| | |_ ___) | | | (_|  __/  _| | |_) |
  // |_|   |_|\___/ \__,_|\__|____/|_|_|\___\___|_|   |____/

  type FloatSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type FloatSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.FloatSliceFB.startFloatSliceFB($1)")>]
    abstract StartFloatSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.FloatSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.FloatSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.FloatSliceFB.endFloatSliceFB($1)")>]
    abstract EndFloatSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.FloatSliceFB.getRootAsFloatSliceFB($1)")>]
    abstract GetRootAsFloatSliceFB: buffer: ByteBuffer -> FloatSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: FloatSliceFB array

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
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.endFloatBoxFB($1)")>]
    abstract EndFloatBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.getRootAsFloatBoxFB($1)")>]
    abstract GetRootAsFloatBoxFB: buffer: ByteBuffer -> FloatBoxFB

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.FloatBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let FloatBoxFB : FloatBoxFBConstructor = failwith "JS only"

  //  ____              _     _      ____  _ _          _____ ____
  // |  _ \  ___  _   _| |__ | | ___/ ___|| (_) ___ ___|  ___| __ )
  // | | | |/ _ \| | | | '_ \| |/ _ \___ \| | |/ __/ _ \ |_  |  _ \
  // | |_| | (_) | |_| | |_) | |  __/___) | | | (_|  __/  _| | |_) |
  // |____/ \___/ \__,_|_.__/|_|\___|____/|_|_|\___\___|_|   |____/

  type DoubleSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type DoubleSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.startDoubleSliceFB($1)")>]
    abstract StartDoubleSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.endDoubleSliceFB($1)")>]
    abstract EndDoubleSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.DoubleSliceFB.getRootAsDoubleSliceFB($1)")>]
    abstract GetRootAsDoubleSliceFB: buffer: ByteBuffer -> DoubleSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: DoubleSliceFB array

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

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.endDoubleBoxFB($1)")>]
    abstract EndDoubleBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.getRootAsDoubleBoxFB($1)")>]
    abstract GetRootAsDoubleBoxFB: buffer: ByteBuffer -> DoubleBoxFB

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.DoubleBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let DoubleBoxFB : DoubleBoxFBConstructor = failwith "JS only"

  //  ____        _       ____  _ _          _____ ____
  // | __ ) _   _| |_ ___/ ___|| (_) ___ ___|  ___| __ )
  // |  _ \| | | | __/ _ \___ \| | |/ __/ _ \ |_  |  _ \
  // | |_) | |_| | ||  __/___) | | | (_|  __/  _| | |_) |
  // |____/ \__, |\__\___|____/|_|_|\___\___|_|   |____/
  //        |___/

  type ByteSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type ByteSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.ByteSliceFB.startByteSliceFB($1)")>]
    abstract StartByteSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.endByteSliceFB($1)")>]
    abstract EndByteSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ByteSliceFB.getRootAsByteSliceFB($1)")>]
    abstract GetRootAsByteSliceFB: buffer: ByteBuffer -> ByteSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: ByteSliceFB array

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
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.endByteBoxFB($1)")>]
    abstract EndByteBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.getRootAsByteBoxFB($1)")>]
    abstract GetRootAsByteBoxFB: buffer: ByteBuffer -> ByteBoxFB

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.ByteBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let ByteBoxFB : ByteBoxFBConstructor = failwith "JS only"

  //  _____                       ____  _ _          _____ ____
  // | ____|_ __  _   _ _ __ ___ / ___|| (_) ___ ___|  ___| __ )
  // |  _| | '_ \| | | | '_ ` _ \\___ \| | |/ __/ _ \ |_  |  _ \
  // | |___| | | | |_| | | | | | |___) | | | (_|  __/  _| | |_) |
  // |_____|_| |_|\__,_|_| |_| |_|____/|_|_|\___\___|_|   |____/

  type EnumSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type EnumSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.EnumSliceFB.startEnumSliceFB($1)")>]
    abstract StartEnumSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.endEnumSliceFB($1)")>]
    abstract EndEnumSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.EnumSliceFB.getRootAsEnumSliceFB($1)")>]
    abstract GetRootAsEnumSliceFB: buffer: ByteBuffer -> EnumSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: EnumSliceFB array

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
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.endEnumBoxFB($1)")>]
    abstract EndEnumBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.getRootAsEnumBoxFB($1)")>]
    abstract GetRootAsEnumBoxFB: buffer: ByteBuffer -> EnumBoxFB

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.EnumBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let EnumBoxFB : EnumBoxFBConstructor = failwith "JS only"

  //   ____      _            ____  _ _          _____ ____
  //  / ___|___ | | ___  _ __/ ___|| (_) ___ ___|  ___| __ )
  // | |   / _ \| |/ _ \| '__\___ \| | |/ __/ _ \ |_  |  _ \
  // | |__| (_) | | (_) | |   ___) | | | (_|  __/  _| | |_) |
  //  \____\___/|_|\___/|_|  |____/|_|_|\___\___|_|   |____/

  type ColorSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type ColorSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.ColorSliceFB.startColorSliceFB($1)")>]
    abstract StartColorSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.endColorSliceFB($1)")>]
    abstract EndColorSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.ColorSliceFB.getRootAsColorSliceFB($1)")>]
    abstract GetRootAsColorSliceFB: buffer: ByteBuffer -> ColorSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: ColorSliceFB array

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
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.endColorBoxFB($1)")>]
    abstract EndColorBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.getRootAsColorBoxFB($1)")>]
    abstract GetRootAsColorBoxFB: buffer: ByteBuffer -> ColorBoxFB

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.ColorBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let ColorBoxFB : ColorBoxFBConstructor = failwith "JS only"

  //  ____  _        _             ____  _ _          _____ ____
  // / ___|| |_ _ __(_)_ __   __ _/ ___|| (_) ___ ___|  ___| __ )
  // \___ \| __| '__| | '_ \ / _` \___ \| | |/ __/ _ \ |_  |  _ \
  //  ___) | |_| |  | | | | | (_| |___) | | | (_|  __/  _| | |_) |
  // |____/ \__|_|  |_|_| |_|\__, |____/|_|_|\___\___|_|   |____/
  //                         |___/

  type StringSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type StringSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.StringSliceFB.startStringSliceFB($1)")>]
    abstract StartStringSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.StringSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.StringSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.StringSliceFB.endStringSliceFB($1)")>]
    abstract EndStringSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.StringSliceFB.getRootAsStringSliceFB($1)")>]
    abstract GetRootAsStringSliceFB: buffer: ByteBuffer -> StringSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: StringSliceFB array

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

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addTags($1, $2)")>]
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.StringBoxFB.endStringBoxFB($1)")>]
    abstract EndStringBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StringBoxFB.getRootAsStringBoxFB($1)")>]
    abstract GetRootAsStringBoxFB: buffer: ByteBuffer -> StringBoxFB

    [<Emit("Iris.Serialization.Raft.StringBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.StringBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let StringBoxFB : StringBoxFBConstructor = failwith "JS only"

  //   ____                                            _ ____  _ _          _____
  //  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| / ___|| (_) ___ ___|  ___|
  // | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` \___ \| | |/ __/ _ \ |_
  // | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| |___) | | | (_|  __/  _|
  //  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/|_|_|\___\___|_| B
  //                      |_|

  type CompoundSliceFB =
    [<Emit("$0.Index()")>]
    abstract Index: string

    [<Emit("$0.Value()")>]
    abstract Value: bool

  type CompoundSliceFBConstructor =
    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.startCompoundSliceFB($1)")>]
    abstract StartCompoundSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.addIndex($1, $2)")>]
    abstract AddIndex: builder: FlatBufferBuilder * index: uint64 -> unit

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.addValue($1, $2)")>]
    abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.endCompoundSliceFB($1)")>]
    abstract EndCompoundSliceFB: builder: FlatBufferBuilder -> unit

    [<Emit("Iris.Serialization.Raft.CompoundSliceFB.getRootAsCompoundSliceFB($1)")>]
    abstract GetRootAsCompoundSliceFB: buffer: ByteBuffer -> CompoundSliceFB

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

    [<Emit("$0.Tags()")>]
    abstract Tags: string array

    [<Emit("$0.Behavior()")>]
    abstract Behavior: BehaviorFB

    [<Emit("$0.Slices()")>]
    abstract Slices: CompoundSliceFB array

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
    abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.addBehavior($1, $2)")>]
    abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.addSlices($1, $2)")>]
    abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> array -> unit

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.endCompoundBoxFB($1)")>]
    abstract EndCompoundBoxFB: builder: FlatBufferBuilder -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.getRootAsCompoundBoxFB($1)")>]
    abstract GetRootAsCompoundBoxFB: buffer: ByteBuffer -> CompoundBoxFB

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.createTagsVector($1, $2)")>]
    abstract CreateTagsVector: builder: FlatBufferBuilder -> Offset<string> array -> Offset<'a>

    [<Emit("Iris.Serialization.Raft.CompoundBoxFB.createSlicesVector($1, $2)")>]
    abstract CreateSlicesVector: builder: FlatBufferBuilder -> Offset<'a> array -> Offset<'a>

  let CompoundBoxFB : CompoundBoxFBConstructor = failwith "JS only"

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
    abstract CreateIOBoxesVector: builder: FlatBufferBuilder -> Offset<IOBoxFB> array -> Offset<'a>

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
    abstract CreateIOBoxesVector: builder: FlatBufferBuilder -> Offset<IOBoxFB> array -> Offset<'a>

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
    abstract CreateCuesVector: builder: FlatBufferBuilder -> Offset<CueFB> array -> Offset<'a>

  let CueListFB : CueListFBConstructor = failwith "JS only"
