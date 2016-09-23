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
