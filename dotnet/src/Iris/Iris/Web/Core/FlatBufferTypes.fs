module Iris.Web.Core.FlatBufferTypes

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers

//  _   _               _____ ____
// | | | |___  ___ _ __|  ___| __ )
// | | | / __|/ _ \ '__| |_  |  _ \
// | |_| \__ \  __/ |  |  _| | |_) |
//  \___/|___/\___|_|  |_|   |____/

type UserFB =
  abstract Id: string
  abstract UserName: string
  abstract FirstName: string
  abstract LastName: string
  abstract Email: string
  abstract Joined: string
  abstract Created: string

type UserFBConstructor =
  abstract prototype: UserFB with get, set
  abstract StartUserFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddUserName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddFirstName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddLastName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddEmail: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddJoined: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddCreated: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract EndUserFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsUserFB: buffer: ByteBuffer -> UserFB

let UserFB : UserFBConstructor = failwith "JS only"

//  ____                _             _____ ____
// / ___|  ___  ___ ___(_) ___  _ __ |  ___| __ )
// \___ \ / _ \/ __/ __| |/ _ \| '_ \| |_  |  _ \
//  ___) |  __/\__ \__ \ | (_) | | | |  _| | |_) |
// |____/ \___||___/___/_|\___/|_| |_|_|   |____/

type SessionFB =
  abstract Id: string
  abstract UserName: string
  abstract IpAddress: string
  abstract UserAgent: string

type SessionFBConstructor =
  abstract prototype: SessionFB with get, set
  abstract StartSessionFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddUserName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddIpAddress: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddUserAgent: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract EndSessionFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsSessionFB: buffer: ByteBuffer -> SessionFB

let SessionFB : SessionFBConstructor = failwith "JS only"

//  ____   ____ ____    ___     __    _
// |  _ \ / ___| __ )  / \ \   / /_ _| |_   _  ___
// | |_) | |  _|  _ \ / _ \ \ / / _` | | | | |/ _ \
// |  _ <| |_| | |_) / ___ \ V / (_| | | |_| |  __/
// |_| \_\\____|____/_/   \_\_/ \__,_|_|\__,_|\___|

type RGBAValueFB =
  abstract Red: uint8
  abstract Green: uint8
  abstract Blue: uint8
  abstract Alpha: uint8

type RGBAValueFBConstructor =
  abstract prototype: RGBAValueFB with get, set
  abstract StartRGBAValueFB: builder: FlatBufferBuilder -> unit
  abstract AddRed: builder: FlatBufferBuilder * id: uint8 -> unit
  abstract AddGreen: builder: FlatBufferBuilder * id: uint8 -> unit
  abstract AddBlue: builder: FlatBufferBuilder * id: uint8 -> unit
  abstract AddAlpha: builder: FlatBufferBuilder * id: uint8 -> unit
  abstract EndRGBAValueFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsRGBAValueFB: buffer: ByteBuffer -> RGBAValueFB
  abstract Create: unit -> RGBAValueFB

//  _   _ ____  _        ___     __    _
// | | | / ___|| |      / \ \   / /_ _| |_   _  ___
// | |_| \___ \| |     / _ \ \ / / _` | | | | |/ _ \
// |  _  |___) | |___ / ___ \ V / (_| | | |_| |  __/
// |_| |_|____/|_____/_/   \_\_/ \__,_|_|\__,_|\___|

type HSLAValueFB =
  abstract Hue: uint8
  abstract Saturation: uint8
  abstract Lightness: uint8
  abstract Alpha: uint8

type HSLAValueFBConstructor =
  abstract prototype: HSLAValueFB with get, set
  abstract StartHSLAValueFB: builder: FlatBufferBuilder -> unit
  abstract AddHue: builder: FlatBufferBuilder * id: uint8 -> unit
  abstract AddSaturation: builder: FlatBufferBuilder * id: uint8 -> unit
  abstract AddLightness: builder: FlatBufferBuilder * id: uint8 -> unit
  abstract AddAlpha: builder: FlatBufferBuilder * id: uint8 -> unit
  abstract EndHSLAValueFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsHSLAValueFB: buffer: ByteBuffer -> HSLAValueFB
  abstract Create: unit -> HSLAValueFB

//  _____
// |_   _|   _ _ __   ___
//   | || | | | '_ \ / _ \
//   | || |_| | |_) |  __/
//   |_| \__, | .__/ \___|
//       |___/|_|

type ColorSpaceTypeEnumFB =
  abstract RGBAValueFB : int
  abstract HSLAValueFB : int

//   ____      _            ____
//  / ___|___ | | ___  _ __/ ___| _ __   __ _  ___ ___
// | |   / _ \| |/ _ \| '__\___ \| '_ \ / _` |/ __/ _ \
// | |__| (_) | | (_) | |   ___) | |_) | (_| | (_|  __/
//  \____\___/|_|\___/|_|  |____/| .__/ \__,_|\___\___|
//                               |_|

type ColorSpaceFB =
  abstract ValueType: int
  abstract Value: 'a -> 'a

type ColorSpaceFBConstructor =
  abstract prototype: ColorSpaceFB with get, set
  abstract StartColorSpaceFB: builder: FlatBufferBuilder -> unit
  abstract AddValue: builder: FlatBufferBuilder * offset: Offset<'a> -> unit
  abstract AddValueType: builder: FlatBufferBuilder * tipe: int -> unit
  abstract EndColorSpaceFB: builder: FlatBufferBuilder -> Offset<'a>
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
  abstract ToggleFB: int
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
  abstract SimpleFB: StringTypeFB
  abstract MultiLineFB: StringTypeFB
  abstract FileNameFB: StringTypeFB
  abstract DirectoryFB: StringTypeFB
  abstract UrlFB: StringTypeFB
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
  abstract StringBoxFB: IOBoxTypeFB
  abstract IntBoxFB: IOBoxTypeFB
  abstract FloatBoxFB: IOBoxTypeFB
  abstract DoubleBoxFB: IOBoxTypeFB
  abstract BoolBoxFB: IOBoxTypeFB
  abstract ByteBoxFB: IOBoxTypeFB
  abstract EnumBoxFB: IOBoxTypeFB
  abstract ColorBoxFB: IOBoxTypeFB
  abstract CompoundBoxFB: IOBoxTypeFB

let IOBoxTypeFB: IOBoxTypeFBConstructor = failwith "JS only"

//  ___ ___  ____            _____ ____
// |_ _/ _ \| __ )  _____  _|  ___| __ )
//  | | | | |  _ \ / _ \ \/ / |_  |  _ \
//  | | |_| | |_) | (_) >  <|  _| | |_) |
// |___\___/|____/ \___/_/\_\_|   |____/

type IOBoxFB =
  abstract IOBox: 'a -> 'a
  abstract IOBoxType: int

type IOBoxFBConstructor =
  abstract StartIOBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddIOBox: builder: FlatBufferBuilder * iobox: Offset<'a> -> unit
  abstract AddIOBoxType: builder: FlatBufferBuilder * tipe: IOBoxTypeFB -> unit
  abstract EndIOBoxFB: builder: FlatBufferBuilder -> Offset<IOBoxFB>
  abstract GetRootAsIOBoxFB: buffer: ByteBuffer -> IOBoxFB

let IOBoxFB: IOBoxFBConstructor = failwith "JS only"

//  ____              _ ____  _ _          _____ ____
// | __ )  ___   ___ | / ___|| (_) ___ ___|  ___| __ )
// |  _ \ / _ \ / _ \| \___ \| | |/ __/ _ \ |_  |  _ \
// | |_) | (_) | (_) | |___) | | | (_|  __/  _| | |_) |
// |____/ \___/ \___/|_|____/|_|_|\___\___|_|   |____/

type BoolSliceFB =
  abstract Index: uint32
  abstract Value: bool

type BoolSliceFBConstructor =
  abstract StartBoolSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit
  abstract EndBoolSliceFB: builder: FlatBufferBuilder -> Offset<BoolSliceFB>
  abstract GetRootAsBoolSliceFB: buffer: ByteBuffer -> BoolSliceFB
  abstract Create: unit -> BoolSliceFB

let BoolSliceFB: BoolSliceFBConstructor = failwith "JS only"

//  ____              _ ____            _____ ____
// | __ )  ___   ___ | | __ )  _____  _|  ___| __ )
// |  _ \ / _ \ / _ \| |  _ \ / _ \ \/ / |_  |  _ \
// | |_) | (_) | (_) | | |_) | (_) >  <|  _| | |_) |
// |____/ \___/ \___/|_|____/ \___/_/\_\_|   |____/

type BoolBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract Behavior: BehaviorFB
  abstract TagsLength: int
  abstract SlicesLength: int
  abstract Tags: int -> string
  abstract Slices: index: int -> BoolSliceFB

type BoolBoxFBConstructor =
  abstract prototype: BoolBoxFB with get, set
  abstract StartBoolBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndBoolBoxFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsBoolBoxFB: buffer: ByteBuffer -> BoolBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * slices: Offset<BoolSliceFB> array -> Offset<'a>
  abstract Create: unit -> BoolBoxFB

let BoolBoxFB : BoolBoxFBConstructor = failwith "JS only"

//  ___       _   ____  _ _          _____ ____
// |_ _|_ __ | |_/ ___|| (_) ___ ___|  ___| __ )
//  | || '_ \| __\___ \| | |/ __/ _ \ |_  |  _ \
//  | || | | | |_ ___) | | | (_|  __/  _| | |_) |
// |___|_| |_|\__|____/|_|_|\___\___|_|   |____/

type IntSliceFB =
  abstract Index: uint32
  abstract Value: int

type IntSliceFBConstructor =
  abstract StartIntSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: int -> unit
  abstract EndIntSliceFB: builder: FlatBufferBuilder -> Offset<IntSliceFB>
  abstract GetRootAsIntSliceFB: buffer: ByteBuffer -> IntSliceFB
  abstract Create: unit -> IntSliceFB

let IntSliceFB: IntSliceFBConstructor = failwith "JS only"

//  ___       _   ____            _____ ____
// |_ _|_ __ | |_| __ )  _____  _|  ___| __ )
//  | || '_ \| __|  _ \ / _ \ \/ / |_  |  _ \
//  | || | | | |_| |_) | (_) >  <|  _| | |_) |
// |___|_| |_|\__|____/ \___/_/\_\_|   |____/

type IntBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract VecSize: uint32
  abstract Min: int
  abstract Max: int
  abstract Unit: string
  abstract TagsLength: int
  abstract Tags: int -> string
  abstract SlicesLength: int
  abstract Slices: int -> IntSliceFB

type IntBoxFBConstructor =
  abstract prototype: IntBoxFB with get, set
  abstract StartIntBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * vecsize: uint32 -> unit
  abstract AddMin: builder: FlatBufferBuilder * min: int -> unit
  abstract AddMax: builder: FlatBufferBuilder * max: int -> unit
  abstract AddUnit: builder: FlatBufferBuilder * unit: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndIntBoxFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsIntBoxFB: buffer: ByteBuffer -> IntBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> IntBoxFB

let IntBoxFB : IntBoxFBConstructor = failwith "JS only"

//  _____ _             _   ____  _ _          _____ ____
// |  ___| | ___   __ _| |_/ ___|| (_) ___ ___|  ___| __ )
// | |_  | |/ _ \ / _` | __\___ \| | |/ __/ _ \ |_  |  _ \
// |  _| | | (_) | (_| | |_ ___) | | | (_|  __/  _| | |_) |
// |_|   |_|\___/ \__,_|\__|____/|_|_|\___\___|_|   |____/

type FloatSliceFB =
  abstract Index: uint32
  abstract Value: float

type FloatSliceFBConstructor =
  abstract StartFloatSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: float32 -> unit
  abstract EndFloatSliceFB: builder: FlatBufferBuilder -> Offset<FloatSliceFB>
  abstract GetRootAsFloatSliceFB: buffer: ByteBuffer -> FloatSliceFB
  abstract Create: unit -> FloatSliceFB

let FloatSliceFB: FloatSliceFBConstructor = failwith "JS only"

//  _____ _             _   ____            _____ ____
// |  ___| | ___   __ _| |_| __ )  _____  _|  ___| __ )
// | |_  | |/ _ \ / _` | __|  _ \ / _ \ \/ / |_  |  _ \
// |  _| | | (_) | (_| | |_| |_) | (_) >  <|  _| | |_) |
// |_|   |_|\___/ \__,_|\__|____/ \___/_/\_\_|   |____/

type FloatBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract VecSize: uint32
  abstract Min: int
  abstract Max: int
  abstract Unit: string
  abstract Precision: uint32
  abstract TagsLength: int
  abstract Tags: int -> string
  abstract Behavior: BehaviorFB
  abstract SlicesLength: int
  abstract Slices: int -> FloatSliceFB

type FloatBoxFBConstructor =
  abstract prototype: FloatBoxFB with get, set
  abstract StartFloatBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * vecsize: uint32 -> unit
  abstract AddMin: builder: FlatBufferBuilder * min: int -> unit
  abstract AddMax: builder: FlatBufferBuilder * max: int -> unit
  abstract AddUnit: builder: FlatBufferBuilder * unit: Offset<string> -> unit
  abstract AddPrecision: builder: FlatBufferBuilder * precision: uint32 -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndFloatBoxFB: builder: FlatBufferBuilder -> Offset<FloatBoxFB>
  abstract GetRootAsFloatBoxFB: buffer: ByteBuffer -> FloatBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> FloatBoxFB

let FloatBoxFB : FloatBoxFBConstructor = failwith "JS only"

//  ____              _     _      ____  _ _          _____ ____
// |  _ \  ___  _   _| |__ | | ___/ ___|| (_) ___ ___|  ___| __ )
// | | | |/ _ \| | | | '_ \| |/ _ \___ \| | |/ __/ _ \ |_  |  _ \
// | |_| | (_) | |_| | |_) | |  __/___) | | | (_|  __/  _| | |_) |
// |____/ \___/ \__,_|_.__/|_|\___|____/|_|_|\___\___|_|   |____/

type DoubleSliceFB =
  abstract Index: uint32
  abstract Value: double

type DoubleSliceFBConstructor =
  abstract prototype: DoubleSliceFB with get, set
  abstract StartDoubleSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: double -> unit
  abstract EndDoubleSliceFB: builder: FlatBufferBuilder -> Offset<DoubleSliceFB>
  abstract GetRootAsDoubleSliceFB: buffer: ByteBuffer -> DoubleSliceFB
  abstract Create: unit -> DoubleSliceFB

let DoubleSliceFB: DoubleSliceFBConstructor = failwith "JS only"

//  ____              _     _      ____            _____ ____
// |  _ \  ___  _   _| |__ | | ___| __ )  _____  _|  ___| __ )
// | | | |/ _ \| | | | '_ \| |/ _ \  _ \ / _ \ \/ / |_  |  _ \
// | |_| | (_) | |_| | |_) | |  __/ |_) | (_) >  <|  _| | |_) |
// |____/ \___/ \__,_|_.__/|_|\___|____/ \___/_/\_\_|   |____/

type DoubleBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract VecSize: uint32
  abstract Min: int
  abstract Max: int
  abstract Unit: string
  abstract Precision: uint32
  abstract Tags: int -> string
  abstract TagsLength: int
  abstract Slices: int -> DoubleSliceFB
  abstract SlicesLength: int

type DoubleBoxFBConstructor =
  abstract prototype: DoubleBoxFB with get, set
  abstract StartDoubleBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * vecsize: uint32 -> unit
  abstract AddMin: builder: FlatBufferBuilder * min: int -> unit
  abstract AddMax: builder: FlatBufferBuilder * max: int -> unit
  abstract AddUnit: builder: FlatBufferBuilder * unit: Offset<string> -> unit
  abstract AddPrecision: builder: FlatBufferBuilder * precision: uint32 -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndDoubleBoxFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsDoubleBoxFB: buffer: ByteBuffer -> DoubleBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> DoubleBoxFB

let DoubleBoxFB : DoubleBoxFBConstructor = failwith "JS only"

//  ____        _       ____  _ _          _____ ____
// | __ ) _   _| |_ ___/ ___|| (_) ___ ___|  ___| __ )
// |  _ \| | | | __/ _ \___ \| | |/ __/ _ \ |_  |  _ \
// | |_) | |_| | ||  __/___) | | | (_|  __/  _| | |_) |
// |____/ \__, |\__\___|____/|_|_|\___\___|_|   |____/
//        |___/

type ByteSliceFB =
  abstract Index: uint32
  abstract Value: string

type ByteSliceFBConstructor =
  abstract prototype: ByteSliceFB with get, set
  abstract StartByteSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract EndByteSliceFB: builder: FlatBufferBuilder -> Offset<ByteSliceFB>
  abstract GetRootAsByteSliceFB: buffer: ByteBuffer -> ByteSliceFB
  abstract Create: unit -> ByteSliceFB

let ByteSliceFB: ByteSliceFBConstructor = failwith "JS only"

//  ____        _       ____            _____ ____
// | __ ) _   _| |_ ___| __ )  _____  _|  ___| __ )
// |  _ \| | | | __/ _ \  _ \ / _ \ \/ / |_  |  _ \
// | |_) | |_| | ||  __/ |_) | (_) >  <|  _| | |_) |
// |____/ \__, |\__\___|____/ \___/_/\_\_|   |____/
//        |___/

type ByteBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract TagsLength: int
  abstract Tags: int -> string
  abstract SlicesLength: int
  abstract Slices: int -> ByteSliceFB

type ByteBoxFBConstructor =
  abstract prototype: ByteBoxFB with get, set
  abstract StartByteBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndByteBoxFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsByteBoxFB: buffer: ByteBuffer -> ByteBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> ByteBoxFB

let ByteBoxFB : ByteBoxFBConstructor = failwith "JS only"

//  _____                       ____                  _____ ____
// | ____|_ __  _   _ _ __ ___ |  _ \ _ __ ___  _ __ |  ___| __ )
// |  _| | '_ \| | | | '_ ` _ \| |_) | '__/ _ \| '_ \| |_  |  _ \
// | |___| | | | |_| | | | | | |  __/| | | (_) | |_) |  _| | |_) |
// |_____|_| |_|\__,_|_| |_| |_|_|   |_|  \___/| .__/|_|   |____/
//                                             |_|

type EnumPropertyFB =
  abstract Key: string
  abstract Value: string

type EnumPropertyFBConstructor =
  abstract prototype: EnumPropertyFB with get, set
  abstract StartEnumPropertyFB: builder: FlatBufferBuilder -> unit
  abstract AddKey: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract EndEnumPropertyFB: builder: FlatBufferBuilder -> Offset<EnumPropertyFB>
  abstract GetRootAsEnumPropertyFB: buffer: ByteBuffer -> EnumPropertyFB
  abstract Create: unit -> EnumPropertyFB

let EnumPropertyFB: EnumPropertyFBConstructor = failwith "JS only"

//  _____                       ____  _ _          _____ ____
// | ____|_ __  _   _ _ __ ___ / ___|| (_) ___ ___|  ___| __ )
// |  _| | '_ \| | | | '_ ` _ \\___ \| | |/ __/ _ \ |_  |  _ \
// | |___| | | | |_| | | | | | |___) | | | (_|  __/  _| | |_) |
// |_____|_| |_|\__,_|_| |_| |_|____/|_|_|\___\___|_|   |____/

type EnumSliceFB =
  abstract Index: uint32
  abstract Value: EnumPropertyFB

type EnumSliceFBConstructor =
  abstract prototype: EnumSliceFB with get, set
  abstract StartEnumSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<EnumPropertyFB> -> unit
  abstract EndEnumSliceFB: builder: FlatBufferBuilder -> Offset<EnumSliceFB>
  abstract GetRootAsEnumSliceFB: buffer: ByteBuffer -> EnumSliceFB
  abstract Create: unit -> EnumSliceFB

let EnumSliceFB: EnumSliceFBConstructor = failwith "JS only"

//  _____                       ____            _____ ____
// | ____|_ __  _   _ _ __ ___ | __ )  _____  _|  ___| __ )
// |  _| | '_ \| | | | '_ ` _ \|  _ \ / _ \ \/ / |_  |  _ \
// | |___| | | | |_| | | | | | | |_) | (_) >  <|  _| | |_) |
// |_____|_| |_|\__,_|_| |_| |_|____/ \___/_/\_\_|   |____/

type EnumBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract Tags: int -> string
  abstract TagsLength: int
  abstract Properties: int -> EnumPropertyFB
  abstract PropertiesLength: int
  abstract Slices: int -> EnumSliceFB
  abstract SlicesLength: int

type EnumBoxFBConstructor =
  abstract prototype: EnumBoxFB with get, set
  abstract StartEnumBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddProperties: builder: FlatBufferBuilder * properties: Offset<'a> -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndEnumBoxFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsEnumBoxFB: buffer: ByteBuffer -> EnumBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreatePropertiesVector: builder: FlatBufferBuilder * Offset<EnumPropertyFB> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> EnumBoxFB

let EnumBoxFB : EnumBoxFBConstructor = failwith "JS only"

//   ____      _            ____  _ _          _____ ____
//  / ___|___ | | ___  _ __/ ___|| (_) ___ ___|  ___| __ )
// | |   / _ \| |/ _ \| '__\___ \| | |/ __/ _ \ |_  |  _ \
// | |__| (_) | | (_) | |   ___) | | | (_|  __/  _| | |_) |
//  \____\___/|_|\___/|_|  |____/|_|_|\___\___|_|   |____/

type ColorSliceFB =
  abstract Index: uint32
  abstract Value: ColorSpaceFB

type ColorSliceFBConstructor =
  abstract prototype: ColorSliceFB with get, set
  abstract StartColorSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<ColorSpaceFB> -> unit
  abstract EndColorSliceFB: builder: FlatBufferBuilder -> Offset<ColorSliceFB>
  abstract GetRootAsColorSliceFB: buffer: ByteBuffer -> ColorSliceFB
  abstract Create: unit -> ColorSliceFB

let ColorSliceFB: ColorSliceFBConstructor = failwith "JS only"

//   ____      _            ____            _____ ____
//  / ___|___ | | ___  _ __| __ )  _____  _|  ___| __ )
// | |   / _ \| |/ _ \| '__|  _ \ / _ \ \/ / |_  |  _ \
// | |__| (_) | | (_) | |  | |_) | (_) >  <|  _| | |_) |
//  \____\___/|_|\___/|_|  |____/ \___/_/\_\_|   |____/

type ColorBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract TagsLength: int
  abstract Tags: int -> string
  abstract Slices: int -> ColorSliceFB
  abstract SlicesLength: int

type ColorBoxFBConstructor =
  abstract prototype: ColorBoxFB with get, set
  abstract StartColorBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndColorBoxFB: builder: FlatBufferBuilder -> Offset<ColorBoxFB>
  abstract GetRootAsColorBoxFB: buffer: ByteBuffer -> ColorBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> ColorBoxFB

let ColorBoxFB : ColorBoxFBConstructor = failwith "JS only"

//  ____  _        _             ____  _ _          _____ ____
// / ___|| |_ _ __(_)_ __   __ _/ ___|| (_) ___ ___|  ___| __ )
// \___ \| __| '__| | '_ \ / _` \___ \| | |/ __/ _ \ |_  |  _ \
//  ___) | |_| |  | | | | | (_| |___) | | | (_|  __/  _| | |_) |
// |____/ \__|_|  |_|_| |_|\__, |____/|_|_|\___\___|_|   |____/
//                         |___/

type StringSliceFB =
  abstract Index: uint32
  abstract Value: string

type StringSliceFBConstructor =
  abstract prototype: StringSliceFB with get, set
  abstract StartStringSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract EndStringSliceFB: builder: FlatBufferBuilder -> Offset<StringSliceFB>
  abstract GetRootAsStringSliceFB: buffer: ByteBuffer -> StringSliceFB
  abstract Create: unit -> StringSliceFB

let StringSliceFB: StringSliceFBConstructor = failwith "JS only"

//  ____  _        _             ____            _____ ____
// / ___|| |_ _ __(_)_ __   __ _| __ )  _____  _|  ___| __ )
// \___ \| __| '__| | '_ \ / _` |  _ \ / _ \ \/ / |_  |  _ \
//  ___) | |_| |  | | | | | (_| | |_) | (_) >  <|  _| | |_) |
// |____/ \__|_|  |_|_| |_|\__, |____/ \___/_/\_\_|   |____/
//                         |___/

type StringBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract StringType: StringTypeFB
  abstract FileMask: string
  abstract MaxChars: int
  abstract TagsLength: int
  abstract Tags: int -> string
  abstract Slices: int -> StringSliceFB
  abstract SlicesLength: int

type StringBoxFBConstructor =
  abstract prototype: StringBoxFB with get, set
  abstract StartStringBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddStringType: builder: FlatBufferBuilder * tipe: StringTypeFB -> unit
  abstract AddFileMask: builder: FlatBufferBuilder * mask: Offset<string> -> unit
  abstract AddMaxChars: builder: FlatBufferBuilder * max: int -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddBehavior: builder: FlatBufferBuilder * behavior: BehaviorFB -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndStringBoxFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsStringBoxFB: buffer: ByteBuffer -> StringBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<StringSliceFB> array -> Offset<'a>
  abstract Create: unit -> StringBoxFB

let StringBoxFB : StringBoxFBConstructor = failwith "JS only"

//   ____                                            _ ____  _ _          _____
//  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| / ___|| (_) ___ ___|  ___|
// | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` \___ \| | |/ __/ _ \ |_
// | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| |___) | | | (_|  __/  _|
//  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/|_|_|\___\___|_| B
//                      |_|

type CompoundSliceFB =
  abstract Index: uint32
  abstract Value: int -> IOBoxFB
  abstract ValueLength: int

type CompoundSliceFBConstructor =
  abstract prototype: CompoundSliceFB with get, set
  abstract StartCompoundSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: uint32 -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<'a> -> unit
  abstract CreateValueVector: builder: FlatBufferBuilder * value: Offset<IOBoxFB> array -> Offset<'a>
  abstract EndCompoundSliceFB: builder: FlatBufferBuilder -> Offset<CompoundSliceFB>
  abstract GetRootAsCompoundSliceFB: buffer: ByteBuffer -> CompoundSliceFB
  abstract Create: unit -> CompoundSliceFB

let CompoundSliceFB: CompoundSliceFBConstructor = failwith "JS only"

//   ____                                            _ ____
//  / ___|___  _ __ ___  _ __   ___  _   _ _ __   __| | __ )  _____  __
// | |   / _ \| '_ ` _ \| '_ \ / _ \| | | | '_ \ / _` |  _ \ / _ \ \/ /
// | |__| (_) | | | | | | |_) | (_) | |_| | | | | (_| | |_) | (_) >  <
//  \____\___/|_| |_| |_| .__/ \___/ \__,_|_| |_|\__,_|____/ \___/_/\_\
//                      |_|

type CompoundBoxFB =
  abstract Id: string
  abstract Name: string
  abstract Patch: string
  abstract Tags: int -> string
  abstract TagsLength: int
  abstract Slices: int -> CompoundSliceFB
  abstract SlicesLength: int

type CompoundBoxFBConstructor =
  abstract prototype: CompoundBoxFB with get, set
  abstract StartCompoundBoxFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPatch: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndCompoundBoxFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCompoundBoxFB: buffer: ByteBuffer -> CompoundBoxFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
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
  abstract StringSliceFB : SliceTypeFB
  abstract IntSliceFB : SliceTypeFB
  abstract FloatSliceFB : SliceTypeFB
  abstract DoubleSliceFB : SliceTypeFB
  abstract BoolSliceFB : SliceTypeFB
  abstract ByteSliceFB : SliceTypeFB
  abstract EnumSliceFB : SliceTypeFB
  abstract ColorSliceFB : SliceTypeFB
  abstract CompoundSliceFB : SliceTypeFB

let SliceTypeFB: SliceTypeFBConstructor = failwith "JS only"

//  ____  _ _          _____ ____
// / ___|| (_) ___ ___|  ___| __ )
// \___ \| | |/ __/ _ \ |_  |  _ \
//  ___) | | | (_|  __/  _| | |_) |
// |____/|_|_|\___\___|_|   |____/

type SliceFB =
  abstract Slice: 'a -> 'a
  abstract SliceType: int

type SliceFBConstructor =
  abstract StartSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddSlice: builder: FlatBufferBuilder * offset: Offset<'a> -> unit
  abstract AddSliceType: builder: FlatBufferBuilder * tipe: SliceTypeFB -> unit
  abstract EndSliceFB: builder: FlatBufferBuilder -> Offset<SliceFB>
  abstract GetRootAsSliceFB: bytes: ByteBuffer -> SliceFB

let SliceFB: SliceFBConstructor = failwith "JS only"

//   ____           _____ ____
//  / ___|   _  ___|  ___| __ )
// | |  | | | |/ _ \ |_  |  _ \
// | |__| |_| |  __/  _| | |_) |
//  \____\__,_|\___|_|   |____/

type CueFB =
  abstract Id: string
  abstract Name: string
  abstract IOBoxesLength: int
  abstract IOBoxes: int -> IOBoxFB

type CueFBConstructor =
  abstract prototype: CueFB with get, set
  abstract StartCueFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddIOBoxes: builder: FlatBufferBuilder * ioboxes: Offset<'a> -> unit
  abstract EndCueFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueFB: buffer: ByteBuffer -> CueFB
  abstract CreateIOBoxesVector: builder: FlatBufferBuilder * Offset<IOBoxFB> array -> Offset<'a>
  abstract Create: unit -> CueFB

let CueFB : CueFBConstructor = failwith "JS only"

//  ____       _       _     _____ ____
// |  _ \ __ _| |_ ___| |__ |  ___| __ )
// | |_) / _` | __/ __| '_ \| |_  |  _ \
// |  __/ (_| | || (__| | | |  _| | |_) |
// |_|   \__,_|\__\___|_| |_|_|   |____/

type PatchFB =
  abstract Id: string
  abstract Name: string
  abstract IOBoxesLength: int
  abstract IOBoxes: int -> IOBoxFB

type PatchFBConstructor =
  abstract prototype: PatchFB with get, set
  abstract StartPatchFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddIOBoxes: builder: FlatBufferBuilder * ioboxes: Offset<'a> -> unit
  abstract EndPatchFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsPatchFB: buffer: ByteBuffer -> PatchFB
  abstract CreateIOBoxesVector: builder: FlatBufferBuilder * Offset<IOBoxFB> array -> Offset<'a>

let PatchFB : PatchFBConstructor = failwith "JS only"

//   ____           _     _     _   _____ ____
//  / ___|   _  ___| |   (_)___| |_|  ___| __ )
// | |  | | | |/ _ \ |   | / __| __| |_  |  _ \
// | |__| |_| |  __/ |___| \__ \ |_|  _| | |_) |
//  \____\__,_|\___|_____|_|___/\__|_|   |____/

type CueListFB =
  abstract Id: string
  abstract Name: string
  abstract CuesLength: int
  abstract Cues: int -> CueFB

type CueListFBConstructor =
  abstract prototype: CueListFB with get, set
  abstract StartCueListFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddCues: builder: FlatBufferBuilder * cues: Offset<'a> -> unit
  abstract EndCueListFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueListFB: buffer: ByteBuffer -> CueListFB
  abstract CreateCuesVector: builder: FlatBufferBuilder * Offset<CueFB> array -> Offset<'a>

let CueListFB : CueListFBConstructor = failwith "JS only"

//  _   _           _      ____  _        _       _____ ____
// | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___|  ___| __ )
// |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \ |_  |  _ \
// | |\  | (_) | (_| |  __/___) | || (_| | ||  __/  _| | |_) |
// |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|_|   |____/

type NodeStateFB = int

type NodeStateFBConstructor =
  abstract JoiningFB: NodeStateFB
  abstract RunningFB: NodeStateFB
  abstract FailedFB: NodeStateFB

let NodeStateFB: NodeStateFBConstructor = failwith "JS only"

//  _   _           _
// | \ | | ___   __| | ___
// |  \| |/ _ \ / _` |/ _ \
// | |\  | (_) | (_| |  __/
// |_| \_|\___/ \__,_|\___|

type NodeFB =
  abstract Id: string
  abstract HostName: string
  abstract IpAddr: string
  abstract Port: uint16
  abstract WebPort: uint16
  abstract WsPort: uint16
  abstract GitPort: uint16
  abstract Voting: bool
  abstract VotedForMe: bool
  abstract State: NodeStateFB
  abstract NextIndex: uint32
  abstract MatchIndex: uint32

type NodeFBConstructor =
  abstract prototype: NodeFB with get, set
  abstract StartNodeFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddHostName: builder: FlatBufferBuilder * hostname: Offset<string> -> unit
  abstract AddIpAddr: builder: FlatBufferBuilder * ip: Offset<string> -> unit
  abstract AddPort: builder: FlatBufferBuilder * port: int -> unit
  abstract AddWebPort: builder: FlatBufferBuilder * port: int -> unit
  abstract AddWsPort: builder: FlatBufferBuilder * port: int -> unit
  abstract AddGitPort: builder: FlatBufferBuilder * port: int -> unit
  abstract AddVoting: builder: FlatBufferBuilder * voting: bool -> unit
  abstract AddVotedForMe: builder: FlatBufferBuilder * votedforme: bool -> unit
  abstract AddState: builder: FlatBufferBuilder * state: NodeStateFB -> unit
  abstract AddNextIndex: builder: FlatBufferBuilder * idx: uint32 -> unit
  abstract AddMatchIndex: builder: FlatBufferBuilder * idx: uint32 -> unit
  abstract EndNodeFB: builder: FlatBufferBuilder -> Offset<NodeFB>
  abstract GetRootAsNodeFB: bytes: ByteBuffer -> NodeFB
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
  abstract NodeAdded: ConfigChangeTypeFB
  abstract NodeRemoved: ConfigChangeTypeFB

let ConfigChangeTypeFB: ConfigChangeTypeFBConstructor = failwith "JS only"

//   ____             __ _        ____ _                            _____ ____
//  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___|  ___| __ )
// | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \ |_  |  _ \
// | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/  _| | |_) |
//  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|_|   |____/
//                         |___/                         |___/

type ConfigChangeFB =
  abstract Type: ConfigChangeTypeFB
  abstract Node: NodeFB

type ConfigChangeFBConstructor =
  abstract prototype: ConfigChangeFB with get, set
  abstract StartConfigChangeFB: builder: FlatBufferBuilder -> unit
  abstract AddType: builder: FlatBufferBuilder * tipe: ConfigChangeTypeFB -> unit
  abstract AddNode: builder: FlatBufferBuilder * node: Offset<NodeFB> -> unit
  abstract EndConfigChangeFB: builder: FlatBufferBuilder -> Offset<ConfigChangeFB>
  abstract GetRootAsConfigChangeFB: bytes: ByteBuffer -> ConfigChangeFB
  abstract Create: unit -> ConfigChangeFB

let ConfigChangeFB: ConfigChangeFBConstructor = failwith "JS only"

//  ____  _        _       _____ ____
// / ___|| |_ __ _| |_ ___|  ___| __ )
// \___ \| __/ _` | __/ _ \ |_  |  _ \
//  ___) | || (_| | ||  __/  _| | |_) |
// |____/ \__\__,_|\__\___|_|   |____/

type StateFB =
  abstract Patches: int -> PatchFB
  abstract PatchesLength: int
  abstract IOBoxes: int -> IOBoxFB
  abstract IOBoxesLength: int
  abstract Cues: int -> CueFB
  abstract CuesLength: int
  abstract CueLists: int -> CueListFB
  abstract CueListsLength: int
  abstract Nodes: int -> NodeFB
  abstract NodesLength: int
  abstract Sessions: int -> SessionFB
  abstract SessionsLength: int
  abstract Users: int -> UserFB
  abstract UsersLength: int

type StateFBConstructor =
  abstract prototype: StateFB with get, set
  abstract StartStateFB: builder: FlatBufferBuilder -> unit
  abstract CreatePatchesVector: builder: FlatBufferBuilder * patches: Offset<PatchFB> array -> Offset<'a>
  abstract AddPatches: builder: FlatBufferBuilder * patches: Offset<'a> -> unit
  abstract CreateIOBoxesVector: builder: FlatBufferBuilder * patches: Offset<IOBoxFB> array -> Offset<'a>
  abstract AddIOBoxes: builder: FlatBufferBuilder * patches: Offset<'a> -> unit
  abstract CreateCuesVector: builder: FlatBufferBuilder * patches: Offset<CueFB> array -> Offset<'a>
  abstract AddCues: builder: FlatBufferBuilder * patches: Offset<'a> -> unit
  abstract CreateCueListsVector: builder: FlatBufferBuilder * patches: Offset<CueListFB> array -> Offset<'a>
  abstract AddCueLists: builder: FlatBufferBuilder * patches: Offset<'a> -> unit
  abstract CreateNodesVector: builder: FlatBufferBuilder * patches: Offset<NodeFB> array -> Offset<'a>
  abstract AddNodes: builder: FlatBufferBuilder * patches: Offset<'a> -> unit
  abstract CreateSessionsVector: builder: FlatBufferBuilder * patches: Offset<SessionFB> array -> Offset<'a>
  abstract AddSessions: builder: FlatBufferBuilder * patches: Offset<'a> -> unit
  abstract CreateUsersVector: builder: FlatBufferBuilder * patches: Offset<UserFB> array -> Offset<'a>
  abstract AddUsers: builder: FlatBufferBuilder * patches: Offset<'a> -> unit
  abstract EndStateFB: builder: FlatBufferBuilder -> Offset<StateFB>
  abstract GetRootAsStateFB: bytes: ByteBuffer -> StateFB

let StateFB: StateFBConstructor = failwith "JS only"

//  _                _____                 _   _____ ____
// | |    ___   __ _| ____|_   _____ _ __ | |_|  ___| __ )
// | |   / _ \ / _` |  _| \ \ / / _ \ '_ \| __| |_  |  _ \
// | |__| (_) | (_| | |___ \ V /  __/ | | | |_|  _| | |_) |
// |_____\___/ \__, |_____| \_/ \___|_| |_|\__|_|   |____/
//             |___/

type LogEventFB =
  abstract Time: uint32
  abstract Thread: int
  abstract Tier: string
  abstract Id: string
  abstract Tag: string
  abstract LogLevel: string
  abstract Message: string

type LogEventFBConstructor =
  abstract prototype: LogEventFB with get, set
  abstract StartLogEventFB: builder: FlatBufferBuilder -> unit
  abstract AddTime: builder: FlatBufferBuilder * time: uint32 -> unit
  abstract AddThread: builder: FlatBufferBuilder * thread: int -> unit
  abstract AddTier: builder: FlatBufferBuilder * tier: Offset<string> -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddTag: builder: FlatBufferBuilder * tag: Offset<string> -> unit
  abstract AddLogLevel: builder: FlatBufferBuilder * level: Offset<string> -> unit
  abstract AddMessage: builder: FlatBufferBuilder * msg: Offset<string> -> unit
  abstract EndLogEventFB: builder: FlatBufferBuilder -> Offset<LogEventFB>
  abstract GetRootAsLogEventFB: bytes: ByteBuffer -> LogEventFB
  abstract Create: unit -> LogEventFB

let LogEventFB: LogEventFBConstructor = failwith "JS only"

//  ____  _        _             _____ ____
// / ___|| |_ _ __(_)_ __   __ _|  ___| __ )
// \___ \| __| '__| | '_ \ / _` | |_  |  _ \
//  ___) | |_| |  | | | | | (_| |  _| | |_) |
// |____/ \__|_|  |_|_| |_|\__, |_|   |____/
//                         |___/

type StringFB =
  abstract Value: string

type StringFBConstructor =
  abstract prototype: StringFB with get, set
  abstract StartStringFB: builder: FlatBufferBuilder -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract EndStringFB: builder: FlatBufferBuilder -> Offset<string>
  abstract GetRootAsStringFB: bytes: ByteBuffer -> StringFB
  abstract Create: unit -> StringFB

let StringFB: StringFBConstructor = failwith "JS only"

//     _          _    _        _   _
//    / \   _ __ (_)  / \   ___| |_(_) ___  _ __
//   / _ \ | '_ \| | / _ \ / __| __| |/ _ \| '_ \
//  / ___ \| |_) | |/ ___ \ (__| |_| | (_) | | | |
// /_/   \_\ .__/|_/_/   \_\___|\__|_|\___/|_| |_|
//         |_|

type ActionTypeFB = int

type ActionTypeFBConstructor =
  abstract AddFB: ActionTypeFB
  abstract UpdateFB: ActionTypeFB
  abstract RemoveFB: ActionTypeFB
  abstract LogEventFB: ActionTypeFB
  abstract UndoFB: ActionTypeFB
  abstract RedoFB: ActionTypeFB
  abstract ResetFB: ActionTypeFB
  abstract SaveProjectFB: ActionTypeFB
  abstract DataSnapshotFB: ActionTypeFB
  abstract SetLogLevelFB: ActionTypeFB

let ActionTypeFB: ActionTypeFBConstructor = failwith "JS only"

type PayloadFB = int

type PayloadFBConstructor =
  abstract NONE: PayloadFB
  abstract CueFB: PayloadFB
  abstract CueListFB: PayloadFB
  abstract IOBoxFB: PayloadFB
  abstract PatchFB: PayloadFB
  abstract NodeFB: PayloadFB
  abstract UserFB: PayloadFB
  abstract SessionFB: PayloadFB
  abstract LogEventFB: PayloadFB
  abstract StateFB: PayloadFB
  abstract StringFB: PayloadFB

let PayloadFB: PayloadFBConstructor = failwith "JS only"

type ApiActionFB =
  abstract Action: ActionTypeFB
  abstract PayloadType: PayloadFB
  abstract CueFB: CueFB
  abstract CueListFB: CueListFB
  abstract IOBoxFB: IOBoxFB
  abstract PatchFB: PatchFB
  abstract NodeFB: NodeFB
  abstract UserFB: UserFB
  abstract SessionFB: SessionFB
  abstract LogEventFB: LogEventFB
  abstract StateFB: StateFB
  abstract StringFB: StringFB
  abstract Payload: 'a -> 'a

type ApiActionFBConstructor =
  abstract prototype: ApiActionFB with get, set
  abstract StartApiActionFB: builder: FlatBufferBuilder -> unit
  abstract AddAction: builder: FlatBufferBuilder * tipe: ActionTypeFB -> unit
  abstract AddPayloadType: builder: FlatBufferBuilder * tipe: PayloadFB -> unit
  abstract AddPayload: builder: FlatBufferBuilder * payload: Offset<'a> -> unit
  abstract EndApiActionFB: builder: FlatBufferBuilder -> Offset<ApiActionFB>
  abstract GetRootAsApiActionFB: bytes: ByteBuffer -> ApiActionFB

let ApiActionFB: ApiActionFBConstructor = failwith "JS only"

//  _____                     _____ ____
// | ____|_ __ _ __ ___  _ __|  ___| __ )
// |  _| | '__| '__/ _ \| '__| |_  |  _ \
// | |___| |  | | | (_) | |  |  _| | |_) |
// |_____|_|  |_|  \___/|_|  |_|   |____/

type ErrorTypeFB = int

type ErrorTypeFBConstructor =
  abstract OKFB: ErrorTypeFB
  abstract BranchNotFoundFB: ErrorTypeFB
  abstract BranchDetailsNotFoundFB: ErrorTypeFB
  abstract RepositoryNotFoundFB: ErrorTypeFB
  abstract RepositoryInitFailedFB: ErrorTypeFB
  abstract CommitErrorFB: ErrorTypeFB
  abstract GitErrorFB: ErrorTypeFB
  abstract ProjectNotFoundFB: ErrorTypeFB
  abstract ProjectParseErrorFB: ErrorTypeFB
  abstract ProjectPathErrorFB: ErrorTypeFB
  abstract ProjectSaveErrorFB: ErrorTypeFB
  abstract ProjectInitErrorFB: ErrorTypeFB
  abstract MetaDataNotFoundFB: ErrorTypeFB
  abstract MissingStartupDirFB: ErrorTypeFB
  abstract CliParseErrorFB: ErrorTypeFB
  abstract MissingNodeIdFB: ErrorTypeFB
  abstract MissingNodeFB: ErrorTypeFB
  abstract AssetSaveErrorFB: ErrorTypeFB
  abstract AssetLoadErrorFB: ErrorTypeFB
  abstract AssetNotFoundErrorFB: ErrorTypeFB
  abstract AssetDeleteErrorFB: ErrorTypeFB
  abstract ParseErrorFB: ErrorTypeFB
  abstract SocketErrorFB: ErrorTypeFB
  abstract IOErrorFB: ErrorTypeFB
  abstract OtherFB: ErrorTypeFB
  abstract AlreadyVotedFB: ErrorTypeFB
  abstract AppendEntryFailedFB: ErrorTypeFB
  abstract CandidateUnknownFB: ErrorTypeFB
  abstract EntryInvalidatedFB: ErrorTypeFB
  abstract InvalidCurrentIndexFB: ErrorTypeFB
  abstract InvalidLastLogFB: ErrorTypeFB
  abstract InvalidLastLogTermFB: ErrorTypeFB
  abstract InvalidTermFB: ErrorTypeFB
  abstract LogFormatErrorFB: ErrorTypeFB
  abstract LogIncompleteFB: ErrorTypeFB
  abstract NoErrorFB: ErrorTypeFB
  abstract NoNodeFB: ErrorTypeFB
  abstract NotCandidateFB: ErrorTypeFB
  abstract NotLeaderFB: ErrorTypeFB
  abstract NotVotingStateFB: ErrorTypeFB
  abstract ResponseTimeoutFB: ErrorTypeFB
  abstract SnapshotFormatErrorFB: ErrorTypeFB
  abstract StaleResponseFB: ErrorTypeFB
  abstract UnexpectedVotingChangeFB: ErrorTypeFB
  abstract VoteTermMismatchFB: ErrorTypeFB

let ErrorTypeFB: ErrorTypeFBConstructor = failwith "JS only"

type ErrorFB =
  abstract Type: ErrorTypeFB
  abstract Message: string

type ErrorFBConstructor =
  abstract prototype: ErrorFB with get, set
  abstract StartErrorFB: builder: FlatBufferBuilder -> unit
  abstract AddType: builder: FlatBufferBuilder * tipe: ErrorTypeFB -> unit
  abstract AddMessage: builder: FlatBufferBuilder * msg: Offset<string> -> unit
  abstract EndErrorFB: builder: FlatBufferBuilder -> Offset<ErrorFB>
  abstract GetRootAsErrorFB: bytes: ByteBuffer -> ErrorFB
  abstract Create: unit -> ErrorFB

let ErrorFB: ErrorFBConstructor = failwith "JS only"
