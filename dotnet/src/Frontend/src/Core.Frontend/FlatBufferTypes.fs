module rec Iris.Web.Core.FlatBufferTypes

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers

//  _____                     _____ ____
// | ____|_ __ _ __ ___  _ __|  ___| __ )
// |  _| | '__| '__/ _ \| '__| |_  |  _ \
// | |___| |  | | | (_) | |  |  _| | |_) |
// |_____|_|  |_|  \___/|_|  |_|   |____/

type ErrorTypeFB = int

type ErrorTypeFBConstructor =
  abstract OKFB: ErrorTypeFB
  abstract GitErrorFB: ErrorTypeFB
  abstract ProjectErrorFB: ErrorTypeFB
  abstract AssetErrorFB: ErrorTypeFB
  abstract ParseErrorFB: ErrorTypeFB
  abstract SocketErrorFB: ErrorTypeFB
  abstract ClientErrorFB: ErrorTypeFB
  abstract IOErrorFB: ErrorTypeFB
  abstract OtherFB: ErrorTypeFB
  abstract RaftErrorFB: ErrorTypeFB

let ErrorTypeFB: ErrorTypeFBConstructor = failwith "JS only"

type ErrorFB =
  abstract Type: ErrorTypeFB
  abstract Location: string
  abstract Message: string

type ErrorFBConstructor =
  abstract prototype: ErrorFB with get, set
  abstract StartErrorFB: builder: FlatBufferBuilder -> unit
  abstract AddType: builder: FlatBufferBuilder * tipe: ErrorTypeFB -> unit
  abstract AddLocation: builder: FlatBufferBuilder * location: Offset<string> -> unit
  abstract AddMessage: builder: FlatBufferBuilder * msg: Offset<string> -> unit
  abstract EndErrorFB: builder: FlatBufferBuilder -> Offset<ErrorFB>
  abstract GetRootAsErrorFB: bytes: ByteBuffer -> ErrorFB
  abstract Create: unit -> ErrorFB

let ErrorFB: ErrorFBConstructor = failwith "JS only"

//  _  __        __     __    _            _____ ____
// | |/ /___ _   \ \   / /_ _| |_   _  ___|  ___| __ )
// | ' // _ \ | | \ \ / / _` | | | | |/ _ \ |_  |  _ \
// | . \  __/ |_| |\ V / (_| | | |_| |  __/  _| | |_) |
// |_|\_\___|\__, | \_/ \__,_|_|\__,_|\___|_|   |____/
//           |___/

type KeyValueFB =
  abstract Key: string
  abstract Value: string

type KeyValueFBConstructor =
  abstract prototype: KeyValueFB with get, set
  abstract StartKeyValueFB: builder: FlatBufferBuilder -> unit
  abstract AddKey: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract EndKeyValueFB: builder: FlatBufferBuilder -> Offset<KeyValueFB>
  abstract GetRootAsKeyValueFB: buffer: ByteBuffer -> KeyValueFB
  abstract Create: unit -> KeyValueFB

let KeyValueFB: KeyValueFBConstructor = failwith "JS only"

//  __  __            _     _            ____  _        _
// |  \/  | __ _  ___| |__ (_)_ __   ___/ ___|| |_ __ _| |_ _   _ ___
// | |\/| |/ _` |/ __| '_ \| | '_ \ / _ \___ \| __/ _` | __| | | / __|
// | |  | | (_| | (__| | | | | | | |  __/___) | || (_| | |_| |_| \__ \
// |_|  |_|\__,_|\___|_| |_|_|_| |_|\___|____/ \__\__,_|\__|\__,_|___/

type MachineStatusEnumFB = int

type MachineStatusEnumFBConstructor =
  abstract IdleFB: MachineStatusEnumFB
  abstract BusyFB: MachineStatusEnumFB

let MachineStatusEnumFB: MachineStatusEnumFBConstructor = failwith "JS only"

type MachineStatusFB =
  abstract Status: MachineStatusEnumFB
  abstract ProjectId: string
  abstract ProjectName: string

type MachineStatusFBConstructor =
  abstract prototype: MachineStatusFB with get, set
  abstract StartMachineStatusFB: builder: FlatBufferBuilder -> unit
  abstract AddStatus: builder: FlatBufferBuilder * key: MachineStatusEnumFB -> unit
  abstract AddProjectId: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract AddProjectName: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract EndMachineStatusFB: builder: FlatBufferBuilder -> Offset<MachineStatusFB>
  abstract GetRootAsMachineStatusFB: buffer: ByteBuffer -> MachineStatusFB
  abstract Create: unit -> MachineStatusFB

let MachineStatusFB: MachineStatusFBConstructor = failwith "JS only"

//  ___      _     __  __            _     _
// |_ _|_ __(_)___|  \/  | __ _  ___| |__ (_)_ __   ___
//  | || '__| / __| |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  | || |  | \__ \ |  | | (_| | (__| | | | | | | |  __/
// |___|_|  |_|___/_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

type IrisMachineFB =
  abstract MachineId: string
  abstract HostName: string
  abstract WorkSpace: string
  abstract LogDirectory: string
  abstract BindAddress: string
  abstract WebPort: uint16
  abstract RaftPort: uint16
  abstract WsPort: uint16
  abstract GitPort: uint16
  abstract ApiPort: uint16
  abstract Version: string

type IrisMachineFBConstructor =
  abstract prototype: IrisMachineFB with get, set
  abstract StartIrisMachineFB: builder: FlatBufferBuilder -> unit
  abstract AddMachineId: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddHostName: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddWorkSpace: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddLogDirectory: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddBindAddress: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddWebPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddRaftPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddWsPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddGitPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddApiPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddVersion: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract EndIrisMachineFB: builder: FlatBufferBuilder -> Offset<IrisMachineFB>
  abstract GetRootAsIrisMachineFB: buffer: ByteBuffer -> IrisMachineFB
  abstract Create: unit -> IrisMachineFB

let IrisMachineFB: IrisMachineFBConstructor = failwith "JS only"

//  ____       _
// |  _ \ ___ | | ___
// | |_) / _ \| |/ _ \
// |  _ < (_) | |  __/
// |_| \_\___/|_|\___|

type RoleFB = int

type RoleFBConstructor =
  abstract RendererFB: RoleFB

let RoleFB: RoleFBConstructor = failwith "JS only"

//  ____                  _          ____  _        _
// / ___|  ___ _ ____   _(_) ___ ___/ ___|| |_ __ _| |_ _   _ ___
// \___ \ / _ \ '__\ \ / / |/ __/ _ \___ \| __/ _` | __| | | / __|
//  ___) |  __/ |   \ V /| | (_|  __/___) | || (_| | |_| |_| \__ \
// |____/ \___|_|    \_/ |_|\___\___|____/ \__\__,_|\__|\__,_|___/

type ServiceStatusTypeFB = int

type ServiceStatusTypeFBConstructor =
  abstract StartingFB: ServiceStatusTypeFB
  abstract RunningFB: ServiceStatusTypeFB
  abstract StoppingFB: ServiceStatusTypeFB
  abstract StoppedFB: ServiceStatusTypeFB
  abstract DegradedFB: ServiceStatusTypeFB
  abstract FailedFB: ServiceStatusTypeFB
  abstract DisposedFB: ServiceStatusTypeFB

let ServiceStatusTypeFB: ServiceStatusTypeFBConstructor = failwith "JS only"


type ServiceStatusFB =
  abstract Type: ServiceStatusTypeFB
  abstract Error: ErrorFB

type ServiceStatusFBConstructor =
  abstract prototype: ServiceStatusFB with get, set
  abstract StartServiceStatusFB: builder: FlatBufferBuilder -> unit
  abstract AddType: builder: FlatBufferBuilder * tipe: ServiceStatusTypeFB -> unit
  abstract AddError: builder: FlatBufferBuilder * error: Offset<ErrorFB> -> unit
  abstract EndServiceStatusFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsServiceStatusFB: buffer: ByteBuffer -> ServiceStatusFB

let ServiceStatusFB : ServiceStatusFBConstructor = failwith "JS only"

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

type IrisClientFB =
  abstract Id: string
  abstract Name: string
  abstract Role: RoleFB
  abstract ServiceId: string
  abstract Status: ServiceStatusFB
  abstract IpAddress: string
  abstract Port: uint16

type IrisClientFBConstructor =
  abstract prototype: IrisClientFB with get, set
  abstract StartIrisClientFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddServiceId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddRole: builder: FlatBufferBuilder * role: RoleFB -> unit
  abstract AddStatus: builder: FlatBufferBuilder * status: Offset<ServiceStatusFB> -> unit
  abstract AddIpAddress: builder: FlatBufferBuilder * ip: Offset<string> -> unit
  abstract AddPort: builder: FlatBufferBuilder * port:uint16 -> unit
  abstract EndIrisClientFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsIrisClientFB: buffer: ByteBuffer -> IrisClientFB

let IrisClientFB : IrisClientFBConstructor = failwith "JS only"

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
  abstract Password: string
  abstract Salt: string
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
  abstract AddPassword: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddSalt: builder: FlatBufferBuilder * salt: Offset<string> -> unit
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

type SessionStatusTypeFB = int

type SessionStatusTypeFBConstructor =
  abstract LoginFB: SessionStatusTypeFB
  abstract UnauthorizedFB: SessionStatusTypeFB
  abstract AuthorizedFB: SessionStatusTypeFB

type SessionStatusFB =
  abstract StatusType: SessionStatusTypeFB
  abstract Payload: string

type SessionStatusFBConstructor =
  abstract prototype: SessionStatusFB with get, set
  abstract StartSessionStatusFB: builder: FlatBufferBuilder -> unit
  abstract AddStatusType: builder: FlatBufferBuilder * typ: SessionStatusTypeFB -> unit
  abstract AddPayload: builder: FlatBufferBuilder * payload: Offset<string> -> unit
  abstract EndSessionStatusFB: builder: FlatBufferBuilder -> Offset<SessionStatusFB>
  abstract GetRootAsSessionStatusFB: buffer: ByteBuffer -> SessionStatusFB
  abstract Create: unit -> SessionStatusFB

type SessionFB =
  abstract Status: SessionStatusFB
  abstract Id: string
  abstract IpAddress: string
  abstract UserAgent: string

type SessionFBConstructor =
  abstract prototype: SessionFB with get, set
  abstract StartSessionFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddStatus: builder: FlatBufferBuilder * status: Offset<SessionStatusFB> -> unit
  abstract AddIpAddress: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddUserAgent: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract EndSessionFB: builder: FlatBufferBuilder -> Offset<SessionFB>
  abstract GetRootAsSessionFB: buffer: ByteBuffer -> SessionFB
  abstract Create: unit -> SessionStatusFB

let SessionStatusTypeFB : SessionStatusTypeFBConstructor = failwith "JS only"
let SessionStatusFB : SessionStatusFBConstructor = failwith "JS only"
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
  abstract Create: unit -> ColorSpaceFB

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
  abstract SimpleFB: BehaviorFB
  abstract MultiLineFB: BehaviorFB
  abstract FileNameFB: BehaviorFB
  abstract DirectoryFB: BehaviorFB
  abstract UrlFB: BehaviorFB
  abstract IPFB: BehaviorFB

let BehaviorFB: BehaviorFBConstructor = failwith "JS only"

//  ____  _     _____                 _____ ____
// |  _ \(_)_ _|_   _|   _ _ __   ___|  ___| __ )
// | |_) | | '_ \| || | | | '_ \ / _ \ |_  |  _ \
// |  __/| | | | | || |_| | |_) |  __/  _| | |_) |
// |_|   |_|_| |_|_| \__, | .__/ \___|_|   |____/
//                   |___/|_|

type PinTypeFB = int

type PinTypeFBConstructor =
  abstract StringPinFB: PinTypeFB
  abstract NumberPinFB: PinTypeFB
  abstract BoolPinFB: PinTypeFB
  abstract BytePinFB: PinTypeFB
  abstract EnumPinFB: PinTypeFB
  abstract ColorPinFB: PinTypeFB

let PinTypeFB: PinTypeFBConstructor = failwith "JS only"

//  ____  _       _____ ____
// |  _ \(_)_ __ |  ___| __ )
// | |_) | | '_ \| |_  |  _ \
// |  __/| | | | |  _| | |_) |
// |_|   |_|_| |_|_|   |____/

type PinFB =
  abstract Pin: 'a -> 'a
  abstract PinType: int

type PinFBConstructor =
  abstract StartPinFB: builder: FlatBufferBuilder -> unit
  abstract AddPin: builder: FlatBufferBuilder * pin: Offset<'a> -> unit
  abstract AddPinType: builder: FlatBufferBuilder * tipe: PinTypeFB -> unit
  abstract EndPinFB: builder: FlatBufferBuilder -> Offset<PinFB>
  abstract GetRootAsPinFB: buffer: ByteBuffer -> PinFB

let PinFB: PinFBConstructor = failwith "JS only"

// __     __        ____  _         _____ ____
// \ \   / /__  ___/ ___|(_)_______|  ___| __ )
//  \ \ / / _ \/ __\___ \| |_  / _ \ |_  |  _ \
//   \ V /  __/ (__ ___) | |/ /  __/  _| | |_) |
//    \_/ \___|\___|____/|_/___\___|_|   |____/

type VecSizeTypeFB = int

type VecSizeTypeFBConstructor =
  abstract DynamicFB: VecSizeTypeFB
  abstract FixedFB: VecSizeTypeFB

let VecSizeTypeFB: VecSizeTypeFBConstructor = failwith "JS only"

type VecSizeFB =
  abstract Type: int
  abstract Size: uint16

type VecSizeFBConstructor =
  abstract StartVecSizeFB: builder: FlatBufferBuilder -> unit
  abstract AddType: builder:FlatBufferBuilder * tipe:VecSizeTypeFB -> unit
  abstract AddSize: builder:FlatBufferBuilder * size:uint16 -> unit
  abstract EndVecSizeFB: builder:FlatBufferBuilder -> Offset<VecSizeFB>
  abstract GetRootAsVecSizeFB: buffer: ByteBuffer -> VecSizeFB

let VecSizeFB: VecSizeFBConstructor = failwith "JS only"

//  ____  _               _   _             _____ ____
// |  _ \(_)_ __ ___  ___| |_(_) ___  _ __ |  ___| __ )
// | | | | | '__/ _ \/ __| __| |/ _ \| '_ \| |_  |  _ \
// | |_| | | | |  __/ (__| |_| | (_) | | | |  _| | |_) |
// |____/|_|_|  \___|\___|\__|_|\___/|_| |_|_|   |____/

type ConnectionDirectionFB = int

type ConnectionDirectionFBConstructor =
  abstract InputFB: ConnectionDirectionFB
  abstract OutputFB: ConnectionDirectionFB

let ConnectionDirectionFB: ConnectionDirectionFBConstructor = failwith "JS only"

//  ____              _ ____  _       _____ ____
// | __ )  ___   ___ | |  _ \(_)_ __ |  ___| __ )
// |  _ \ / _ \ / _ \| | |_) | | '_ \| |_  |  _ \
// | |_) | (_) | (_) | |  __/| | | | |  _| | |_) |
// |____/ \___/ \___/|_|_|   |_|_| |_|_|   |____/

type BoolPinFB =
  abstract Id: string
  abstract Name: string
  abstract PinGroup: string
  abstract IsTrigger: bool
  abstract VecSize: VecSizeFB
  abstract Persisted: bool
  abstract Direction: ConnectionDirectionFB
  abstract Tags: int -> string
  abstract TagsLength: int
  abstract Labels: index: int -> string
  abstract LabelsLength: int
  abstract Values: index: int -> bool
  abstract ValuesLength: int

type BoolPinFBConstructor =
  abstract prototype: BoolPinFB with get, set
  abstract StartBoolPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroup: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddIsTrigger: builder: FlatBufferBuilder * trigger: bool -> unit
  abstract AddDirection: builder: FlatBufferBuilder * ConnectionDirectionFB -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * labels: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndBoolPinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsBoolPinFB: buffer: ByteBuffer -> BoolPinFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateLabelsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateValuesVector: builder: FlatBufferBuilder * slices: bool array -> Offset<'a>
  abstract Create: unit -> BoolPinFB

let BoolPinFB : BoolPinFBConstructor = failwith "JS only"

//  _   _                 _               ____  _       _____ ____
// | \ | |_   _ _ __ ___ | |__   ___ _ __|  _ \(_)_ __ |  ___| __ )
// |  \| | | | | '_ ` _ \| '_ \ / _ \ '__| |_) | | '_ \| |_  |  _ \
// | |\  | |_| | | | | | | |_) |  __/ |  |  __/| | | | |  _| | |_) |
// |_| \_|\__,_|_| |_| |_|_.__/ \___|_|  |_|   |_|_| |_|_|   |____/

type NumberPinFB =
  abstract Id: string
  abstract Name: string
  abstract PinGroup: string
  abstract Min: int
  abstract Max: int
  abstract Unit: string
  abstract Precision: uint32
  abstract Persisted: bool
  abstract VecSize: VecSizeFB
  abstract Direction: ConnectionDirectionFB
  abstract Tags: int -> string
  abstract TagsLength: int
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> double
  abstract ValuesLength: int

type NumberPinFBConstructor =
  abstract prototype: NumberPinFB with get, set
  abstract StartNumberPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroup: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * vecsize: uint32 -> unit
  abstract AddMin: builder: FlatBufferBuilder * min: int -> unit
  abstract AddMax: builder: FlatBufferBuilder * max: int -> unit
  abstract AddUnit: builder: FlatBufferBuilder * unit: Offset<string> -> unit
  abstract AddPrecision: builder: FlatBufferBuilder * precision: uint32 -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddDirection: builder: FlatBufferBuilder * ConnectionDirectionFB -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndNumberPinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsNumberPinFB: buffer: ByteBuffer -> NumberPinFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateLabelsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateValuesVector: builder: FlatBufferBuilder * double array -> Offset<'a>
  abstract Create: unit -> NumberPinFB

let NumberPinFB : NumberPinFBConstructor = failwith "JS only"

//  ____        _       ____  _       _____ ____
// | __ ) _   _| |_ ___|  _ \(_)_ __ |  ___| __ )
// |  _ \| | | | __/ _ \ |_) | | '_ \| |_  |  _ \
// | |_) | |_| | ||  __/  __/| | | | |  _| | |_) |
// |____/ \__, |\__\___|_|   |_|_| |_|_|   |____/
//        |___/

type BytePinFB =
  abstract Id: string
  abstract Name: string
  abstract PinGroup: string
  abstract VecSize: VecSizeFB
  abstract Direction: ConnectionDirectionFB
  abstract Persisted: bool
  abstract TagsLength: int
  abstract Tags: int -> string
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> string
  abstract ValuesLength: int

type BytePinFBConstructor =
  abstract prototype: BytePinFB with get, set
  abstract StartBytePinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroup: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddDirection: builder: FlatBufferBuilder * ConnectionDirectionFB -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndBytePinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsBytePinFB: buffer: ByteBuffer -> BytePinFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateLabelsVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> BytePinFB

let BytePinFB : BytePinFBConstructor = failwith "JS only"

//  _____                       ____            _____ ____
// | ____|_ __  _   _ _ __ ___ | __ )  _____  _|  ___| __ )
// |  _| | '_ \| | | | '_ ` _ \|  _ \ / _ \ \/ / |_  |  _ \
// | |___| | | | |_| | | | | | | |_) | (_) >  <|  _| | |_) |
// |_____|_| |_|\__,_|_| |_| |_|____/ \___/_/\_\_|   |____/

type EnumPinFB =
  abstract Id: string
  abstract Name: string
  abstract PinGroup: string
  abstract VecSize: VecSizeFB
  abstract Direction: ConnectionDirectionFB
  abstract Tags: int -> string
  abstract Persisted: bool
  abstract TagsLength: int
  abstract Properties: int -> KeyValueFB
  abstract PropertiesLength: int
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> KeyValueFB
  abstract ValuesLength: int

type EnumPinFBConstructor =
  abstract prototype: EnumPinFB with get, set
  abstract StartEnumPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroup: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddDirection: builder: FlatBufferBuilder * ConnectionDirectionFB -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddProperties: builder: FlatBufferBuilder * properties: Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndEnumPinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsEnumPinFB: buffer: ByteBuffer -> EnumPinFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreatePropertiesVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> Offset<'a>
  abstract CreateLabelsVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> EnumPinFB

let EnumPinFB : EnumPinFBConstructor = failwith "JS only"

//   ____      _            ____            _____ ____
//  / ___|___ | | ___  _ __| __ )  _____  _|  ___| __ )
// | |   / _ \| |/ _ \| '__|  _ \ / _ \ \/ / |_  |  _ \
// | |__| (_) | | (_) | |  | |_) | (_) >  <|  _| | |_) |
//  \____\___/|_|\___/|_|  |____/ \___/_/\_\_|   |____/

type ColorPinFB =
  abstract Id: string
  abstract Name: string
  abstract PinGroup: string
  abstract VecSize: VecSizeFB
  abstract Direction: ConnectionDirectionFB
  abstract TagsLength: int
  abstract Tags: int -> string
  abstract Persisted: bool
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> ColorSpaceFB
  abstract ValuesLength: int

type ColorPinFBConstructor =
  abstract prototype: ColorPinFB with get, set
  abstract StartColorPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroup: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddDirection: builder: FlatBufferBuilder * ConnectionDirectionFB -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndColorPinFB: builder: FlatBufferBuilder -> Offset<ColorPinFB>
  abstract GetRootAsColorPinFB: buffer: ByteBuffer -> ColorPinFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateLabelsVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<'a> array -> Offset<'a>
  abstract Create: unit -> ColorPinFB

let ColorPinFB : ColorPinFBConstructor = failwith "JS only"

//  ____  _        _             ____            _____ ____
// / ___|| |_ _ __(_)_ __   __ _| __ )  _____  _|  ___| __ )
// \___ \| __| '__| | '_ \ / _` |  _ \ / _ \ \/ / |_  |  _ \
//  ___) | |_| |  | | | | | (_| | |_) | (_) >  <|  _| | |_) |
// |____/ \__|_|  |_|_| |_|\__, |____/ \___/_/\_\_|   |____/
//                         |___/

type StringPinFB =
  abstract Id: string
  abstract Name: string
  abstract PinGroup: string
  abstract Behavior: BehaviorFB
  abstract VecSize: VecSizeFB
  abstract Direction: ConnectionDirectionFB
  abstract MaxChars: int
  abstract Persisted: bool
  abstract Tags: int -> string
  abstract TagsLength: int
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> string
  abstract ValuesLength: int

type StringPinFBConstructor =
  abstract prototype: StringPinFB with get, set
  abstract StartStringPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddPinGroup: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddBehavior: builder: FlatBufferBuilder * tipe: BehaviorFB -> unit
  abstract AddMaxChars: builder: FlatBufferBuilder * max: int -> unit
  abstract AddDirection: builder: FlatBufferBuilder * ConnectionDirectionFB -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndStringPinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsStringPinFB: buffer: ByteBuffer -> StringPinFB
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateLabelsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract Create: unit -> StringPinFB

let StringPinFB : StringPinFBConstructor = failwith "JS only"

//  ____  _ _         _____                 _____ ____
// / ___|| (_) ___ __|_   _|   _ _ __   ___|  ___| __ )
// \___ \| | |/ __/ _ \| || | | | '_ \ / _ \ |_  |  _ \
//  ___) | | | (_|  __/| || |_| | |_) |  __/  _| | |_) |
// |____/|_|_|\___\___||_| \__, | .__/ \___|_|   |____/
//                         |___/|_|

type SliceTypeFB = int

type SliceTypeFBConstructor =
  abstract StringFB : SliceTypeFB
  abstract DoubleFB : SliceTypeFB
  abstract BoolFB : SliceTypeFB
  abstract ByteFB : SliceTypeFB
  abstract KeyValueFB : SliceTypeFB
  abstract ColorSpaceFB : SliceTypeFB

let SliceTypeFB: SliceTypeFBConstructor = failwith "JS only"

//  ____  _ _          _____ ____
// / ___|| (_) ___ ___|  ___| __ )
// \___ \| | |/ __/ _ \ |_  |  _ \
//  ___) | | | (_|  __/  _| | |_) |
// |____/|_|_|\___\___|_|   |____/

type SliceFB =
  abstract Index: int
  abstract Slice: 'a -> 'a
  abstract SliceType: int

type SliceFBConstructor =
  abstract StartSliceFB: builder: FlatBufferBuilder -> unit
  abstract AddIndex: builder: FlatBufferBuilder * index: int -> unit
  abstract AddSlice: builder: FlatBufferBuilder * offset: Offset<'a> -> unit
  abstract AddSliceType: builder: FlatBufferBuilder * tipe: SliceTypeFB -> unit
  abstract EndSliceFB: builder: FlatBufferBuilder -> Offset<SliceFB>
  abstract GetRootAsSliceFB: bytes: ByteBuffer -> SliceFB

let SliceFB: SliceFBConstructor = failwith "JS only"

//  ____  _ _               _____ ____
// / ___|| (_) ___ ___  ___|  ___| __ )
// \___ \| | |/ __/ _ \/ __| |_  |  _ \
//  ___) | | | (_|  __/\__ \  _| | |_) |
// |____/|_|_|\___\___||___/_|   |____/

type StringsFB =
  abstract ValuesLength: int
  abstract Values: int -> string

type StringsFBConstructor =
  abstract StartStringsFB: builder: FlatBufferBuilder -> unit
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<'a> array -> VectorOffset
  abstract AddValues: builder: FlatBufferBuilder * VectorOffset -> unit
  abstract EndStringsFB: builder: FlatBufferBuilder -> Offset<StringsFB>
  abstract GetRootAsStringsFB: bytes: ByteBuffer -> StringsFB
  abstract Create: unit -> StringsFB

let StringsFB: StringsFBConstructor = failwith "JS only"

type DoublesFB =
  abstract ValuesLength: int
  abstract Values: int -> double

type DoublesFBConstructor =
  abstract StartDoublesFB: builder: FlatBufferBuilder -> unit
  abstract CreateValuesVector: builder: FlatBufferBuilder * double array -> VectorOffset
  abstract AddValues: builder: FlatBufferBuilder * VectorOffset -> unit
  abstract EndDoublesFB: builder: FlatBufferBuilder -> Offset<DoublesFB>
  abstract GetRootAsDoublesFB: bytes: ByteBuffer -> DoublesFB
  abstract Create: unit -> DoublesFB

let DoublesFB: DoublesFBConstructor = failwith "JS only"

type BoolsFB =
  abstract ValuesLength: int
  abstract Values: int -> bool

type BoolsFBConstructor =
  abstract StartBoolsFB: builder: FlatBufferBuilder -> unit
  abstract CreateValuesVector: builder: FlatBufferBuilder * bool array -> VectorOffset
  abstract AddValues: builder: FlatBufferBuilder * VectorOffset -> unit
  abstract EndBoolsFB: builder: FlatBufferBuilder -> Offset<BoolsFB>
  abstract GetRootAsBoolsFB: bytes: ByteBuffer -> BoolsFB
  abstract Create: unit -> BoolsFB

let BoolsFB: BoolsFBConstructor = failwith "JS only"

type BytesFB =
  abstract ValuesLength: int
  abstract Values: int -> string

type BytesFBConstructor =
  abstract StartBytesFB: builder: FlatBufferBuilder -> unit
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<'a> array -> VectorOffset
  abstract AddValues: builder: FlatBufferBuilder * VectorOffset -> unit
  abstract EndBytesFB: builder: FlatBufferBuilder -> Offset<BytesFB>
  abstract GetRootAsBytesFB: bytes: ByteBuffer -> BytesFB
  abstract Create: unit -> BytesFB

let BytesFB: BytesFBConstructor = failwith "JS only"

type KeyValuesFB =
  abstract ValuesLength: int
  abstract Values: int -> KeyValueFB

type KeyValuesFBConstructor =
  abstract StartKeyValuesFB: builder: FlatBufferBuilder -> unit
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> VectorOffset
  abstract AddValues: builder: FlatBufferBuilder * VectorOffset -> unit
  abstract EndKeyValuesFB: builder: FlatBufferBuilder -> Offset<KeyValuesFB>
  abstract GetRootAsKeyValuesFB: keyValues: ByteBuffer -> KeyValuesFB
  abstract Create: unit -> KeyValuesFB

let KeyValuesFB: KeyValuesFBConstructor = failwith "JS only"

type ColorSpacesFB =
  abstract ValuesLength: int
  abstract Values: int -> ColorSpaceFB

type ColorSpacesFBConstructor =
  abstract StartColorSpacesFB: builder: FlatBufferBuilder -> unit
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<ColorSpaceFB> array -> VectorOffset
  abstract AddValues: builder: FlatBufferBuilder * VectorOffset -> unit
  abstract EndColorSpacesFB: builder: FlatBufferBuilder -> Offset<ColorSpacesFB>
  abstract GetRootAsColorSpacesFB: colorSpaces: ByteBuffer -> ColorSpacesFB
  abstract Create: unit -> ColorSpacesFB

let ColorSpacesFB: ColorSpacesFBConstructor = failwith "JS only"

type SlicesTypeFB = int

type SlicesTypeFBConstructor =
  abstract StringsFB : SlicesTypeFB
  abstract DoublesFB : SlicesTypeFB
  abstract BoolsFB : SlicesTypeFB
  abstract BytesFB : SlicesTypeFB
  abstract KeyValuesFB : SlicesTypeFB
  abstract ColorSpacesFB : SlicesTypeFB

let SlicesTypeFB: SlicesTypeFBConstructor = failwith "JS only"


type SlicesFB =
  abstract Id: string
  abstract Slices: 'a -> 'a
  abstract SlicesType: SliceTypeFB

type SlicesFBConstructor =
  abstract prototype: SlicesFB with get, set
  abstract StartSlicesFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddSlicesType: builder: FlatBufferBuilder * tipe: SlicesTypeFB -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndSlicesFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsSlicesFB: buffer: ByteBuffer -> SlicesFB
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<SliceFB> array -> Offset<'a>
  abstract Create: unit -> SlicesFB

let SlicesFB : SlicesFBConstructor = failwith "JS only"

//   ____           _____ ____
//  / ___|   _  ___|  ___| __ )
// | |  | | | |/ _ \ |_  |  _ \
// | |__| |_| |  __/  _| | |_) |
//  \____\__,_|\___|_|   |____/

type CueFB =
  abstract Id: string
  abstract Name: string
  abstract SlicesLength: int
  abstract Slices: int -> SlicesFB

type CueFBConstructor =
  abstract prototype: CueFB with get, set
  abstract StartCueFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndCueFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueFB: buffer: ByteBuffer -> CueFB
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<SlicesFB> array -> Offset<'a>
  abstract Create: unit -> CueFB

let CueFB : CueFBConstructor = failwith "JS only"

type CueReferenceFB =
  abstract Id: string
  abstract CueId: string
  abstract AutoFollow: int
  abstract Duration: int
  abstract Prewait: int

type CueReferenceFBConstructor =
  abstract prototype: CueReferenceFB with get, set
  abstract StartCueReferenceFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddCueId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddAutoFollow: builder: FlatBufferBuilder * value: int -> unit
  abstract AddDuration: builder: FlatBufferBuilder * value: int -> unit
  abstract AddPrewait: builder: FlatBufferBuilder * value: int -> unit
  abstract EndCueReferenceFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueReferenceFB: buffer: ByteBuffer -> CueReferenceFB
  abstract Create: unit -> CueReferenceFB

let CueReferenceFB : CueReferenceFBConstructor = failwith "JS only"

type CueGroupFB =
  abstract Id: string
  abstract Name: string
  abstract CueRefsLength: int
  abstract CueRefs: int -> CueReferenceFB

type CueGroupFBConstructor =
  abstract prototype: CueGroupFB with get, set
  abstract StartCueGroupFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddCueRefs: builder: FlatBufferBuilder * cues: Offset<'a> -> unit
  abstract EndCueGroupFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueGroupFB: buffer: ByteBuffer -> CueGroupFB
  abstract CreateCueRefsVector: builder: FlatBufferBuilder * Offset<CueReferenceFB> array -> Offset<'a>

let CueGroupFB : CueGroupFBConstructor = failwith "JS only"

//  ____       _       _     _____ ____
// |  _ \ __ _| |_ ___| |__ |  ___| __ )
// | |_) / _` | __/ __| '_ \| |_  |  _ \
// |  __/ (_| | || (__| | | |  _| | |_) |
// |_|   \__,_|\__\___|_| |_|_|   |____/

type PinGroupFB =
  abstract Id: string
  abstract Name: string
  abstract Client: string
  abstract PinsLength: int
  abstract Pins: int -> PinFB

type PinGroupFBConstructor =
  abstract prototype: PinGroupFB with get, set
  abstract StartPinGroupFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddClient: builder: FlatBufferBuilder * client: Offset<string> -> unit
  abstract AddPins: builder: FlatBufferBuilder * pins: Offset<'a> -> unit
  abstract EndPinGroupFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsPinGroupFB: buffer: ByteBuffer -> PinGroupFB
  abstract CreatePinsVector: builder: FlatBufferBuilder * Offset<PinFB> array -> Offset<'a>

let PinGroupFB : PinGroupFBConstructor = failwith "JS only"

//   ____           _     _     _   _____ ____
//  / ___|   _  ___| |   (_)___| |_|  ___| __ )
// | |  | | | |/ _ \ |   | / __| __| |_  |  _ \
// | |__| |_| |  __/ |___| \__ \ |_|  _| | |_) |
//  \____\__,_|\___|_____|_|___/\__|_|   |____/

type CueListFB =
  abstract Id: string
  abstract Name: string
  abstract GroupsLength: int
  abstract Groups: int -> CueGroupFB

type CueListFBConstructor =
  abstract prototype: CueListFB with get, set
  abstract StartCueListFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddGroups: builder: FlatBufferBuilder * groups: Offset<'a> -> unit
  abstract EndCueListFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueListFB: buffer: ByteBuffer -> CueListFB
  abstract CreateGroupsVector: builder: FlatBufferBuilder * Offset<CueGroupFB> array -> Offset<'a>

let CueListFB : CueListFBConstructor = failwith "JS only"

//  _   _           _      ____  _        _       _____ ____
// | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___|  ___| __ )
// |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \ |_  |  _ \
// | |\  | (_) | (_| |  __/___) | || (_| | ||  __/  _| | |_) |
// |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|_|   |____/

type RaftMemberStateFB = int

type RaftMemberStateFBConstructor =
  abstract JoiningFB: RaftMemberStateFB
  abstract RunningFB: RaftMemberStateFB
  abstract FailedFB: RaftMemberStateFB

let RaftMemberStateFB: RaftMemberStateFBConstructor = failwith "JS only"

//  _   _           _
// | \ | | ___   __| | ___
// |  \| |/ _ \ / _` |/ _ \
// | |\  | (_) | (_| |  __/
// |_| \_|\___/ \__,_|\___|

type RaftMemberFB =
  abstract Id: string
  abstract HostName: string
  abstract IpAddr: string
  abstract Port: uint16
  abstract WebPort: uint16
  abstract WsPort: uint16
  abstract GitPort: uint16
  abstract ApiPort: uint16
  abstract Voting: bool
  abstract VotedForMe: bool
  abstract State: RaftMemberStateFB
  abstract NextIndex: int
  abstract MatchIndex: int

type RaftMemberFBConstructor =
  abstract prototype: RaftMemberFB with get, set
  abstract StartRaftMemberFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddHostName: builder: FlatBufferBuilder * hostname: Offset<string> -> unit
  abstract AddIpAddr: builder: FlatBufferBuilder * ip: Offset<string> -> unit
  abstract AddPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddWebPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddWsPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddGitPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddApiPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddVoting: builder: FlatBufferBuilder * voting: bool -> unit
  abstract AddVotedForMe: builder: FlatBufferBuilder * votedforme: bool -> unit
  abstract AddState: builder: FlatBufferBuilder * state: RaftMemberStateFB -> unit
  abstract AddNextIndex: builder: FlatBufferBuilder * idx: int -> unit
  abstract AddMatchIndex: builder: FlatBufferBuilder * idx: int -> unit
  abstract EndRaftMemberFB: builder: FlatBufferBuilder -> Offset<RaftMemberFB>
  abstract GetRootAsRaftMemberFB: bytes: ByteBuffer -> RaftMemberFB
  abstract Create: unit -> RaftMemberFB

let RaftMemberFB: RaftMemberFBConstructor = failwith "JS only"

//   ____ _                           _____
//  / ___| |__   __ _ _ __   __ _  __|_   _|   _ _ __   ___
// | |   | '_ \ / _` | '_ \ / _` |/ _ \| || | | | '_ \ / _ \
// | |___| | | | (_| | | | | (_| |  __/| || |_| | |_) |  __/
//  \____|_| |_|\__,_|_| |_|\__, |\___||_| \__, | .__/ \___|
//                          |___/          |___/|_|

type ConfigChangeTypeFB = int

type ConfigChangeTypeFBConstructor =
  abstract MemberAdded: ConfigChangeTypeFB
  abstract MemberRemoved: ConfigChangeTypeFB

let ConfigChangeTypeFB: ConfigChangeTypeFBConstructor = failwith "JS only"

//   ____             __ _        ____ _                            _____ ____
//  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___|  ___| __ )
// | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \ |_  |  _ \
// | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/  _| | |_) |
//  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|_|   |____/
//                         |___/                         |___/

type ConfigChangeFB =
  abstract Type: ConfigChangeTypeFB
  abstract Member: RaftMemberFB

type ConfigChangeFBConstructor =
  abstract prototype: ConfigChangeFB with get, set
  abstract StartConfigChangeFB: builder: FlatBufferBuilder -> unit
  abstract AddType: builder: FlatBufferBuilder * tipe: ConfigChangeTypeFB -> unit
  abstract AddMember: builder: FlatBufferBuilder * mem: Offset<RaftMemberFB> -> unit
  abstract EndConfigChangeFB: builder: FlatBufferBuilder -> Offset<ConfigChangeFB>
  abstract GetRootAsConfigChangeFB: bytes: ByteBuffer -> ConfigChangeFB
  abstract Create: unit -> ConfigChangeFB

let ConfigChangeFB: ConfigChangeFBConstructor = failwith "JS only"

//   ____             __ _
//  / ___|___  _ __  / _(_) __ _
// | |   / _ \| '_ \| |_| |/ _` |
// | |__| (_) | | | |  _| | (_| |
//  \____\___/|_| |_|_| |_|\__, |
//                         |___/

// AUDIO

type AudioConfigFB =
  abstract SampleRate: uint32

type AudioConfigFBConstructor =
  abstract prototype: AudioConfigFB with get, set
  abstract StartAudioConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddSampleRate: builder: FlatBufferBuilder * sr: uint32 -> unit
  abstract EndAudioConfigFB: builder: FlatBufferBuilder -> Offset<AudioConfigFB>
  abstract GetRootAsAudioConfigFB: bytes: ByteBuffer -> AudioConfigFB
  abstract Create: unit -> AudioConfigFB

let AudioConfigFB: AudioConfigFBConstructor = failwith "JS only"

// VVVV EXE

type VvvvExeFB =
  abstract Executable: string
  abstract Version:  string
  abstract Required: bool

type VvvvExeFBConstructor =
  abstract prototype: VvvvExeFB with get, set
  abstract StartVvvvExeFB: builder: FlatBufferBuilder -> unit
  abstract AddExecutable: builder: FlatBufferBuilder * exe:Offset<string> -> unit
  abstract AddVersion: builder: FlatBufferBuilder * version:Offset<string> -> unit
  abstract AddRequired: builder: FlatBufferBuilder * required:bool -> unit
  abstract EndVvvvExeFB: builder: FlatBufferBuilder -> Offset<VvvvExeFB>
  abstract GetRootAsVvvvExeFB: bytes: ByteBuffer -> VvvvExeFB
  abstract Create: unit -> VvvvExeFB

let VvvvExeFB: VvvvExeFBConstructor = failwith "JS only"

// VVVV PLUGIN

type VvvvPluginFB =
  abstract Name: string
  abstract Path: string

type VvvvPluginFBConstructor =
  abstract prototype: VvvvPluginFB with get, set
  abstract StartVvvvPluginFB: builder: FlatBufferBuilder -> unit
  abstract AddName: builder: FlatBufferBuilder * name:Offset<string> -> unit
  abstract AddPath: builder: FlatBufferBuilder * path:Offset<string> -> unit
  abstract EndVvvvPluginFB: builder: FlatBufferBuilder -> Offset<VvvvPluginFB>
  abstract GetRootAsVvvvPluginFB: bytes: ByteBuffer -> VvvvPluginFB
  abstract Create: unit -> VvvvPluginFB

let VvvvPluginFB: VvvvPluginFBConstructor = failwith "JS only"

// VVVV CONFIG

type VvvvConfigFB =
  abstract Executables: int -> VvvvExeFB
  abstract ExecutablesLength: int
  abstract Plugins: int -> VvvvPluginFB
  abstract PluginsLength: int

type VvvvConfigFBConstructor =
  abstract prototype: VvvvConfigFB with get, set
  abstract StartVvvvConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddExecutables: builder: FlatBufferBuilder * exes:Offset<'a> -> unit
  abstract AddPlugins: builder: FlatBufferBuilder * path:Offset<'a> -> unit
  abstract CreateExecutablesVector: FlatBufferBuilder * Offset<VvvvExeFB> array -> Offset<'a>
  abstract CreatePluginsVector: FlatBufferBuilder * Offset<VvvvPluginFB> array -> Offset<'a>
  abstract EndVvvvConfigFB: builder: FlatBufferBuilder -> Offset<VvvvConfigFB>
  abstract GetRootAsVvvvConfigFB: bytes: ByteBuffer -> VvvvConfigFB
  abstract Create: unit -> VvvvConfigFB

let VvvvConfigFB: VvvvConfigFBConstructor = failwith "JS only"

// RAFT CONFIG

type RaftConfigFB =
  abstract RequestTimeout:   int
  abstract ElectionTimeout:  int
  abstract MaxLogDepth:      int
  abstract LogLevel:         string
  abstract DataDir:          string
  abstract MaxRetries:       int
  abstract PeriodicInterval: int

type RaftConfigFBConstructor =
  abstract prototype: RaftConfigFB with get, set
  abstract StartRaftConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddRequestTimeout: builder: FlatBufferBuilder * rto:int -> unit
  abstract AddElectionTimeout: builder: FlatBufferBuilder * eto:int -> unit
  abstract AddMaxLogDepth: builder: FlatBufferBuilder * mld:int -> unit
  abstract AddLogLevel: builder: FlatBufferBuilder * lvl:Offset<string> -> unit
  abstract AddDataDir: builder: FlatBufferBuilder * dir:Offset<string> -> unit
  abstract AddMaxRetries: builder: FlatBufferBuilder * rtr:int -> unit
  abstract AddPeriodicInterval: builder: FlatBufferBuilder * pi:int -> unit
  abstract EndRaftConfigFB: builder: FlatBufferBuilder -> Offset<RaftConfigFB>
  abstract GetRootAsRaftConfigFB: bytes: ByteBuffer -> RaftConfigFB
  abstract Create: unit -> RaftConfigFB

let RaftConfigFB: RaftConfigFBConstructor = failwith "JS only"

// TIMING CONFIG

type TimingConfigFB =
  abstract UDPPort:       uint32
  abstract TCPPort:       uint32
  abstract Framebase:     uint32
  abstract Input:         string
  abstract Servers:       int -> string
  abstract ServersLength: int

type TimingConfigFBConstructor =
  abstract prototype: TimingConfigFB with get, set
  abstract StartTimingConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddFramebase: builder: FlatBufferBuilder * b:uint32 -> unit
  abstract AddUDPPort: builder: FlatBufferBuilder * port:uint32 -> unit
  abstract AddTCPPort: builder: FlatBufferBuilder * port:uint32 -> unit
  abstract AddInput: builder: FlatBufferBuilder * input:Offset<string> -> unit
  abstract AddServers: builder: FlatBufferBuilder * srvs:Offset<'a> -> unit
  abstract CreateServersVector: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract EndTimingConfigFB: builder: FlatBufferBuilder -> Offset<TimingConfigFB>
  abstract GetRootAsTimingConfigFB: bytes: ByteBuffer -> TimingConfigFB
  abstract Create: unit -> TimingConfigFB

let TimingConfigFB: TimingConfigFBConstructor = failwith "JS only"

// HOSTGROUP

type HostGroupFB =
  abstract Name:          string
  abstract Members:       int -> string
  abstract MembersLength: int

type HostGroupFBConstructor =
  abstract prototype: HostGroupFB with get, set
  abstract StartHostGroupFB: builder: FlatBufferBuilder -> unit
  abstract AddName: builder: FlatBufferBuilder * name:Offset<string> -> unit
  abstract AddMembers: builder: FlatBufferBuilder * mems:Offset<'a> -> unit
  abstract CreateMembersVector: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract EndHostGroupFB: builder: FlatBufferBuilder -> Offset<HostGroupFB>
  abstract GetRootAsHostGroupFB: bytes: ByteBuffer -> HostGroupFB
  abstract Create: unit -> HostGroupFB

let HostGroupFB: HostGroupFBConstructor = failwith "JS only"

// CLUSTER CONFIG

type ClusterConfigFB =
  abstract Id:            string
  abstract Name:          string
  abstract Members:       int -> RaftMemberFB
  abstract MembersLength: int
  abstract Groups:        int -> HostGroupFB
  abstract GroupsLength:  int

type ClusterConfigFBConstructor =
  abstract prototype: ClusterConfigFB with get, set
  abstract StartClusterConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * name:Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name:Offset<string> -> unit
  abstract AddMembers: builder: FlatBufferBuilder * mems:Offset<'a> -> unit
  abstract CreateMembersVector: FlatBufferBuilder * Offset<RaftMemberFB> array -> Offset<'a>
  abstract AddGroups: builder: FlatBufferBuilder * mems:Offset<'a> -> unit
  abstract CreateGroupsVector: FlatBufferBuilder * Offset<HostGroupFB> array -> Offset<'a>
  abstract EndClusterConfigFB: builder: FlatBufferBuilder -> Offset<ClusterConfigFB>
  abstract GetRootAsClusterConfigFB: bytes: ByteBuffer -> ClusterConfigFB
  abstract Create: unit -> ClusterConfigFB

let ClusterConfigFB: ClusterConfigFBConstructor = failwith "JS only"

// VIEWPORT CONFIG

type ViewPortFB =
  abstract Id:              string
  abstract Name:            string
  abstract Description:     string
  abstract PositionX:       int
  abstract PositionY:       int
  abstract SizeX:           int
  abstract SizeY:           int
  abstract OutputPositionX: int
  abstract OutputPositionY: int
  abstract OutputSizeX:     int
  abstract OutputSizeY:     int
  abstract OverlapX:        int
  abstract OverlapY:        int

type ViewPortFBConstructor =
  abstract prototype: ViewPortFB with get, set
  abstract StartViewPortFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id:Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name:Offset<string> -> unit
  abstract AddDescription: builder: FlatBufferBuilder * desc:Offset<string> -> unit
  abstract AddMembers: builder: FlatBufferBuilder * mems:Offset<'a> -> unit
  abstract AddPositionX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddPositionY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddSizeX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddSizeY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOutputPositionX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOutputPositionY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOutputSizeX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOutputSizeY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOverlapX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOverlapY: builder: FlatBufferBuilder * v:int -> unit
  abstract EndViewPortFB: builder: FlatBufferBuilder -> Offset<ViewPortFB>
  abstract GetRootAsViewPortFB: bytes: ByteBuffer -> ViewPortFB
  abstract Create: unit -> ViewPortFB

let ViewPortFB: ViewPortFBConstructor = failwith "JS only"

// TASK

type TaskFB =
  abstract Id:              string
  abstract DisplayId:       string
  abstract Description:     string
  abstract AudioStream:     string
  abstract PositionX:       int
  abstract Arguments:       int -> string
  abstract ArgumentsLength: int

type TaskFBConstructor =
  abstract prototype: TaskFB with get, set
  abstract StartTaskFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id:Offset<string> -> unit
  abstract AddDisplayId: builder: FlatBufferBuilder * id:Offset<string> -> unit
  abstract AddDescription: builder: FlatBufferBuilder * desc:Offset<string> -> unit
  abstract AddAudioStream: builder: FlatBufferBuilder * audio:Offset<string> -> unit
  abstract AddArguments: builder: FlatBufferBuilder * audio:Offset<'a> -> unit
  abstract CreateArgumentsVector: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract EndTaskFB: builder: FlatBufferBuilder -> Offset<TaskFB>
  abstract GetRootAsTaskFB: bytes: ByteBuffer -> TaskFB
  abstract Create: unit -> TaskFB

let TaskFB: TaskFBConstructor = failwith "JS only"

// SIGNAL

type SignalFB =
  abstract SizeX:     int
  abstract SizeY:     int
  abstract PositionX: int
  abstract PositionY: int

type SignalFBConstructor =
  abstract prototype: SignalFB with get, set
  abstract StartSignalFB: builder: FlatBufferBuilder -> unit
  abstract AddSizeX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddSizeY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddPositionX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddPositionY: builder: FlatBufferBuilder * v:int -> unit
  abstract EndSignalFB: builder: FlatBufferBuilder -> Offset<SignalFB>
  abstract GetRootAsSignalFB: bytes: ByteBuffer -> SignalFB
  abstract Create: unit -> SignalFB

let SignalFB: SignalFBConstructor = failwith "JS only"

// REGION

type RegionFB =
  abstract Id:              string
  abstract Name:            string
  abstract SrcSizeX:        int
  abstract SrcSizeY:        int
  abstract SrcPositionX:    int
  abstract SrcPositionY:    int
  abstract OutputSizeX:     int
  abstract OutputSizeY:     int
  abstract OutputPositionX: int
  abstract OutputPositionY: int

type RegionFBConstructor =
  abstract prototype: RegionFB with get, set
  abstract StartRegionFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddSrcSizeX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddSrcSizeY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddSrcPositionX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddSrcPositionY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOutputSizeX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOutputSizeY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOutputPositionX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddOutputPositionY: builder: FlatBufferBuilder * v:int -> unit
  abstract EndRegionFB: builder: FlatBufferBuilder -> Offset<RegionFB>
  abstract GetRootAsRegionFB: bytes: ByteBuffer -> RegionFB
  abstract Create: unit -> RegionFB

let RegionFB: RegionFBConstructor = failwith "JS only"

// REGION MAP

type RegionMapFB =
  abstract SrcViewportId: string
  abstract Regions: int -> RegionFB
  abstract RegionsLength: int

type RegionMapFBConstructor =
  abstract prototype: RegionMapFB with get, set
  abstract StartRegionMapFB: builder: FlatBufferBuilder -> unit
  abstract AddSrcViewportId: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddRegions: builder: FlatBufferBuilder * v:Offset<'a> -> unit
  abstract CreateRegionsVector: builder: FlatBufferBuilder * v:Offset<RegionFB> array -> Offset<'a>
  abstract EndRegionMapFB: builder: FlatBufferBuilder -> Offset<RegionMapFB>
  abstract GetRootAsRegionMapFB: bytes: ByteBuffer -> RegionMapFB
  abstract Create: unit -> RegionMapFB

let RegionMapFB: RegionMapFBConstructor = failwith "JS only"

// DISPLAY

type DisplayFB =
  abstract Id: string
  abstract Name: string
  abstract SizeX: int
  abstract SizeY: int
  abstract Signals: int -> SignalFB
  abstract SignalsLength: int
  abstract RegionMap: RegionMapFB

type DisplayFBConstructor =
  abstract prototype: DisplayFB with get, set
  abstract StartDisplayFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddSizeX: builder: FlatBufferBuilder * v:int -> unit
  abstract AddSizeY: builder: FlatBufferBuilder * v:int -> unit
  abstract AddSignals: builder: FlatBufferBuilder * v:Offset<'a> -> unit
  abstract CreateSignalsVector: builder: FlatBufferBuilder * v:Offset<SignalFB> array -> Offset<'a>
  abstract AddRegionMap: builder: FlatBufferBuilder * v:Offset<RegionMapFB> -> unit
  abstract EndDisplayFB: builder: FlatBufferBuilder -> Offset<DisplayFB>
  abstract GetRootAsDisplayFB: bytes: ByteBuffer -> DisplayFB
  abstract Create: unit -> DisplayFB

let DisplayFB: DisplayFBConstructor = failwith "JS only"

// CONFIG

type ConfigFB =
  abstract Version: string
  abstract Machine: IrisMachineFB
  abstract ActiveSite: string
  abstract AudioConfig: AudioConfigFB
  abstract VvvvConfig: VvvvConfigFB
  abstract RaftConfig: RaftConfigFB
  abstract TimingConfig: TimingConfigFB
  abstract Sites: int -> ClusterConfigFB
  abstract SitesLength: int
  abstract ViewPorts: int -> ViewPortFB
  abstract ViewPortsLength: int
  abstract Displays: int -> DisplayFB
  abstract DisplaysLength: int
  abstract Tasks: int -> TaskFB
  abstract TasksLength: int

type ConfigFBConstructor =
  abstract prototype: ConfigFB with get, set
  abstract StartConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddVersion: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddMachine: builder: FlatBufferBuilder * v:Offset<IrisMachineFB> -> unit
  abstract AddActiveSite: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddAudioConfig: builder: FlatBufferBuilder * v:Offset<AudioConfigFB> -> unit
  abstract AddVvvvConfig: builder: FlatBufferBuilder * v:Offset<VvvvConfigFB> -> unit
  abstract AddRaftConfig: builder: FlatBufferBuilder * v:Offset<RaftConfigFB> -> unit
  abstract AddTimingConfig: builder: FlatBufferBuilder * v:Offset<TimingConfigFB> -> unit
  abstract AddSites: builder: FlatBufferBuilder * v:Offset<'a> -> unit
  abstract CreateSitesVector: FlatBufferBuilder * Offset<ClusterConfigFB> array -> Offset<'a>
  abstract AddViewPorts: builder: FlatBufferBuilder * v:Offset<'a> -> unit
  abstract CreateViewPortsVector: FlatBufferBuilder * Offset<ViewPortFB> array -> Offset<'a>
  abstract AddDisplays: builder: FlatBufferBuilder * v:Offset<'a> -> unit
  abstract CreateDisplaysVector: FlatBufferBuilder * Offset<DisplayFB> array -> Offset<'a>
  abstract AddTasks: builder: FlatBufferBuilder * v:Offset<'a> -> unit
  abstract CreateTasksVector: FlatBufferBuilder * Offset<TaskFB> array -> Offset<'a>
  abstract EndConfigFB: builder: FlatBufferBuilder -> Offset<ConfigFB>
  abstract GetRootAsConfigFB: bytes: ByteBuffer -> ConfigFB
  abstract Create: unit -> ConfigFB

let ConfigFB: ConfigFBConstructor = failwith "JS only"


// PROJECT

type ProjectFB =
  abstract Id: string
  abstract Name: string
  abstract Path: string
  abstract CreatedOn: string
  abstract LastSaved: string
  abstract Copyright: string
  abstract Author: string
  abstract Config: ConfigFB

type ProjectFBConstructor =
  abstract prototype: ProjectFB with get, set
  abstract StartProjectFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddPath: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddCreatedOn: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddLastSaved: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddCopyright: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddAuthor: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddConfig: builder: FlatBufferBuilder * v:Offset<ConfigFB> -> unit
  abstract EndProjectFB: builder: FlatBufferBuilder -> Offset<ProjectFB>
  abstract GetRootAsProjectFB: bytes: ByteBuffer -> ProjectFB
  abstract Create: unit -> ProjectFB

let ProjectFB: ProjectFBConstructor = failwith "JS only"

//  ____  _
// |  _ \(_)___  ___ _____   _____ _ __ _   _
// | | | | / __|/ __/ _ \ \ / / _ \ '__| | | |
// | |_| | \__ \ (_| (_) \ V /  __/ |  | |_| |
// |____/|_|___/\___\___/ \_/ \___|_|   \__, |
//                                      |___/

type ExposedServiceTypeFB = int

type ExposedServiceTypeFBConstructor =
  abstract RendererFB: RoleFB
  abstract GitFB: ExposedServiceTypeFB
  abstract RaftFB: ExposedServiceTypeFB
  abstract HttpFB: ExposedServiceTypeFB
  abstract WebSocketFB: ExposedServiceTypeFB
  abstract ApiFB: ExposedServiceTypeFB

let ExposedServiceTypeFB: ExposedServiceTypeFBConstructor = failwith "JS only"

type ExposedServiceFB =
  abstract Type: ExposedServiceTypeFB
  abstract Port: uint16

type ExposedServiceFBConstructor =
  abstract prototype: ExposedServiceFB with get, set
  abstract StartExposedServiceFB: builder: FlatBufferBuilder -> unit
  abstract AddType: builder: FlatBufferBuilder * ExposedServiceTypeFB -> unit
  abstract AddPort: builder: FlatBufferBuilder * uint16 -> unit
  abstract EndExposedServiceFB: builder: FlatBufferBuilder -> Offset<ExposedServiceFB>
  abstract GetRootAsExposedServiceFB: bytes: ByteBuffer -> ExposedServiceFB

let ExposedServiceFB: ExposedServiceFBConstructor = failwith "JS only"

type DiscoveredServiceFB =
  abstract Id: string
  abstract Name: string
  abstract FullName: string
  abstract HostName: string
  abstract HostTarget: string
  abstract AliasesLength: int
  abstract Aliases: int -> string
  abstract Protocol: string
  abstract AddressListLength: int
  abstract AddressList: int -> string
  abstract ServicesLength: int
  abstract Services: int -> ExposedServiceFB
  abstract Status: MachineStatusFB
  abstract ExtraMetadataLength: int
  abstract ExtraMetadata: int -> KeyValueFB

type DiscoveredServiceFBConstructor =
  abstract prototype: DiscoveredServiceFB with get, set
  abstract StartDiscoveredServiceFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * Offset<string> -> unit
  abstract AddFullName: builder: FlatBufferBuilder * Offset<string> -> unit
  abstract AddType: builder: FlatBufferBuilder * Offset<string> -> unit
  abstract AddHostName: builder: FlatBufferBuilder * Offset<string> -> unit
  abstract AddHostTarget: builder: FlatBufferBuilder * Offset<string> -> unit
  abstract AddAliases: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddProtocol: builder: FlatBufferBuilder * Offset<string> -> unit
  abstract AddAddressList: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddStatus: builder: FlatBufferBuilder * Offset<MachineStatusFB> -> unit
  abstract AddServices: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddExtraMetadata: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract EndDiscoveredServiceFB: builder: FlatBufferBuilder -> Offset<DiscoveredServiceFB>
  abstract GetRootAsDiscoveredServiceFB: bytes: ByteBuffer -> DiscoveredServiceFB
  abstract CreateAliasesVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateAddressListVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateServicesVector: builder: FlatBufferBuilder * Offset<ExposedServiceFB> array -> Offset<'a>
  abstract CreateExtraMetadataVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> Offset<'a>

let DiscoveredServiceFB: DiscoveredServiceFBConstructor = failwith "JS only"

//   ____           ____  _                       _____ ____
//  / ___|   _  ___|  _ \| | __ _ _   _  ___ _ __|  ___| __ )
// | |  | | | |/ _ \ |_) | |/ _` | | | |/ _ \ '__| |_  |  _ \
// | |__| |_| |  __/  __/| | (_| | |_| |  __/ |  |  _| | |_) |
//  \____\__,_|\___|_|   |_|\__,_|\__, |\___|_|  |_|   |____/
//                                |___/

type CuePlayerFB =
  abstract Id: string
  abstract Name: string
  abstract CueList: string
  abstract Selected: int
  abstract Call: PinFB
  abstract Next: PinFB
  abstract Previous: PinFB
  abstract RemainingWait: int
  abstract LastCaller: string
  abstract LastCalled: string

type CuePlayerFBConstructor =
  abstract prototype: CuePlayerFB with get, set
  abstract StartCuePlayerFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: Offset<string> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddCueList: builder: FlatBufferBuilder * cuelist: Offset<string> -> unit
  abstract AddSelected: builder: FlatBufferBuilder * int -> unit
  abstract AddCall: builder: FlatBufferBuilder * call: Offset<PinFB> -> unit
  abstract AddNext: builder: FlatBufferBuilder * next: Offset<PinFB> -> unit
  abstract AddPrevious: builder: FlatBufferBuilder * previous: Offset<PinFB> -> unit
  abstract AddRemainingWait: builder: FlatBufferBuilder * int -> unit
  abstract AddLastCaller: builder: FlatBufferBuilder * lastcaller: Offset<string> -> unit
  abstract AddLastCalled: builder: FlatBufferBuilder * lastcalled: Offset<string> -> unit
  abstract EndCuePlayerFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCuePlayerFB: buffer: ByteBuffer -> CuePlayerFB

let CuePlayerFB : CuePlayerFBConstructor = failwith "JS only"

//  ____  _        _       _____ ____
// / ___|| |_ __ _| |_ ___|  ___| __ )
// \___ \| __/ _` | __/ _ \ |_  |  _ \
//  ___) | || (_| | ||  __/  _| | |_) |
// |____/ \__\__,_|\__\___|_|   |____/

type StateFB =
  abstract Project: ProjectFB
  abstract PinGroups: int -> PinGroupFB
  abstract PinGroupsLength: int
  abstract Cues: int -> CueFB
  abstract CuesLength: int
  abstract CueLists: int -> CueListFB
  abstract CueListsLength: int
  abstract Sessions: int -> SessionFB
  abstract SessionsLength: int
  abstract Users: int -> UserFB
  abstract UsersLength: int
  abstract Clients: int -> IrisClientFB
  abstract ClientsLength: int
  abstract CuePlayers: int -> CuePlayerFB
  abstract CuePlayersLength: int
  abstract DiscoveredServices: int -> DiscoveredServiceFB
  abstract DiscoveredServicesLength: int

type StateFBConstructor =
  abstract prototype: StateFB with get, set
  abstract StartStateFB: builder: FlatBufferBuilder -> unit
  abstract AddProject: builder: FlatBufferBuilder * project: Offset<ProjectFB> -> unit
  abstract AddPinGroups: builder: FlatBufferBuilder * groups: Offset<'a> -> unit
  abstract AddCues: builder: FlatBufferBuilder * cues: Offset<'a> -> unit
  abstract AddCueLists: builder: FlatBufferBuilder * cuelists: Offset<'a> -> unit
  abstract AddSessions: builder: FlatBufferBuilder * sessions: Offset<'a> -> unit
  abstract AddUsers: builder: FlatBufferBuilder * users: Offset<'a> -> unit
  abstract AddClients: builder: FlatBufferBuilder * clients: Offset<'a> -> unit
  abstract AddCuePlayers: builder: FlatBufferBuilder * cueplayers: Offset<'a> -> unit
  abstract AddDiscoveredServices: builder: FlatBufferBuilder * services: Offset<'a> -> unit
  abstract CreateCuesVector: builder: FlatBufferBuilder * cues: Offset<CueFB> array -> Offset<'a>
  abstract CreateSessionsVector: builder: FlatBufferBuilder * groups: Offset<SessionFB> array -> Offset<'a>
  abstract CreatePinGroupsVector: builder: FlatBufferBuilder * groups: Offset<PinGroupFB> array -> Offset<'a>
  abstract CreateCueListsVector: builder: FlatBufferBuilder * groups: Offset<CueListFB> array -> Offset<'a>
  abstract CreateCuePlayersVector: builder: FlatBufferBuilder * groups: Offset<CuePlayerFB> array -> Offset<'a>
  abstract CreateUsersVector: builder: FlatBufferBuilder * groups: Offset<UserFB> array -> Offset<'a>
  abstract CreateClientsVector: builder: FlatBufferBuilder * groups: Offset<IrisClientFB> array -> Offset<'a>
  abstract CreateDiscoveredServicesVector: builder: FlatBufferBuilder * groups: Offset<DiscoveredServiceFB> array -> Offset<'a>
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
  abstract CreateStringFB: builder:FlatBufferBuilder * Offset<string> -> Offset<StringFB>
  abstract Create: unit -> StringFB

let StringFB: StringFBConstructor = failwith "JS only"

//  ____              _     _      _____ ____
// |  _ \  ___  _   _| |__ | | ___|  ___| __ )
// | | | |/ _ \| | | | '_ \| |/ _ \ |_  |  _ \
// | |_| | (_) | |_| | |_) | |  __/  _| | |_) |
// |____/ \___/ \__,_|_.__/|_|\___|_|   |____/

type DoubleFB =
  abstract Value: double

type DoubleFBConstructor =
  abstract prototype: DoubleFB with get, set
  abstract StartDoubleFB: builder: FlatBufferBuilder -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: double -> unit
  abstract EndDoubleFB: builder: FlatBufferBuilder -> Offset<DoubleFB>
  abstract GetRootAsDoubleFB: bytes: ByteBuffer -> DoubleFB
  abstract CreateDoubleFB: builder:FlatBufferBuilder * double -> Offset<DoubleFB>
  abstract Create: unit -> DoubleFB

let DoubleFB: DoubleFBConstructor = failwith "JS only"

type ClockFB =
  abstract Value: uint32

type ClockFBConstructor =
  abstract prototype: ClockFB with get, set
  abstract StartClockFB: builder: FlatBufferBuilder -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: uint32 -> unit
  abstract EndClockFB: builder: FlatBufferBuilder -> Offset<ClockFB>
  abstract GetRootAsClockFB: bytes: ByteBuffer -> ClockFB
  abstract CreateClockFB: builder:FlatBufferBuilder * uint32 -> Offset<ClockFB>
  abstract Create: unit -> ClockFB

let ClockFB: ClockFBConstructor = failwith "JS only"

//  ____              _ _____ ____
// | __ )  ___   ___ | |  ___| __ )
// |  _ \ / _ \ / _ \| | |_  |  _ \
// | |_) | (_) | (_) | |  _| | |_) |
// |____/ \___/ \___/|_|_|   |____/

type BoolFB =
  abstract Value: bool

type BoolFBConstructor =
  abstract prototype: BoolFB with get, set
  abstract StartBoolFB: builder: FlatBufferBuilder -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: bool -> unit
  abstract EndBoolFB: builder: FlatBufferBuilder -> Offset<BoolFB>
  abstract GetRootAsBoolFB: bytes: ByteBuffer -> BoolFB
  abstract CreateBoolFB: builder:FlatBufferBuilder * bool -> Offset<BoolFB>
  abstract Create: unit -> BoolFB

let BoolFB: BoolFBConstructor = failwith "JS only"

//  ____        _       _____ ____
// | __ ) _   _| |_ ___|  ___| __ )
// |  _ \| | | | __/ _ \ |_  |  _ \
// | |_) | |_| | ||  __/  _| | |_) |
// |____/ \__, |\__\___|_|   |____/
//        |___/

type ByteFB =
  abstract Value: string

type ByteFBConstructor =
  abstract prototype: ByteFB with get, set
  abstract StartByteFB: builder: FlatBufferBuilder -> unit
  abstract AddValue: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract EndByteFB: builder: FlatBufferBuilder -> Offset<ByteFB>
  abstract GetRootAsByteFB: bytes: ByteBuffer -> ByteFB
  abstract CreateByteFB: builder:FlatBufferBuilder * Offset<string> -> Offset<ByteFB>
  abstract Create: unit -> ByteFB

let ByteFB: ByteFBConstructor = failwith "JS only"

//   ____                                          _ ____        _       _
//  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| | __ )  __ _| |_ ___| |__
// | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |  _ \ / _` | __/ __| '_ \
// | |__| (_) | | | | | | | | | | | (_| | | | | (_| | |_) | (_| | || (__| | | |
//  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|____/ \__,_|\__\___|_| |_|

type CommandBatchFB =
  abstract Commands: int -> StateMachineFB
  abstract CommandsLength: int

type CommandBatchFBConstructor =
  abstract prototype: CommandBatchFB with get, set
  abstract StartCommandBatchFB: builder:FlatBufferBuilder -> unit
  abstract AddCommands: builder:FlatBufferBuilder * commands:Offset<StateMachineFB> -> unit
  abstract EndCommandBatchFB: builder:FlatBufferBuilder -> Offset<CommandBatchFB>
  abstract GetRootAsCommandBatchFB: bytes:ByteBuffer -> CommandBatchFB
  abstract CreateCommandsVector: builder: FlatBufferBuilder * Offset<StateMachineFB> array -> Offset<'a>
  abstract Create: unit -> CommandBatchFB

let CommandBatchFB: CommandBatchFBConstructor = failwith "JS only"

//  ____  _        _       __  __            _     _
// / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
// \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
// |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

type StateMachineActionFB = int

type StateMachineActionFBConstructor =
  abstract AddFB: StateMachineActionFB
  abstract UpdateFB: StateMachineActionFB
  abstract RemoveFB: StateMachineActionFB
  abstract LogEventFB: StateMachineActionFB
  abstract UndoFB: StateMachineActionFB
  abstract RedoFB: StateMachineActionFB
  abstract ResetFB: StateMachineActionFB
  abstract CallFB: StateMachineActionFB
  abstract BatchFB: StateMachineActionFB
  abstract SaveProjectFB: StateMachineActionFB
  abstract DataSnapshotFB: StateMachineActionFB
  abstract SetLogLevelFB: StateMachineActionFB

let StateMachineActionFB: StateMachineActionFBConstructor = failwith "JS only"

type StateMachinePayloadFB = int

type StateMachinePayloadFBConstructor =
  abstract NONE: StateMachinePayloadFB
  abstract CueFB: StateMachinePayloadFB
  abstract CueListFB: StateMachinePayloadFB
  abstract PinFB: StateMachinePayloadFB
  abstract PinGroupFB: StateMachinePayloadFB
  abstract RaftMemberFB: StateMachinePayloadFB
  abstract UserFB: StateMachinePayloadFB
  abstract SessionFB: StateMachinePayloadFB
  abstract LogEventFB: StateMachinePayloadFB
  abstract StateFB: StateMachinePayloadFB
  abstract StringFB: StateMachinePayloadFB
  abstract ProjectFB: StateMachinePayloadFB
  abstract SlicesFB: StateMachinePayloadFB
  abstract IrisClientFB: StateMachinePayloadFB
  abstract CuePlayerFB: StateMachinePayloadFB
  abstract DiscoveredServiceFB: StateMachinePayloadFB
  abstract ClockFB: StateMachinePayloadFB
  abstract CommandBatchFB: StateMachinePayloadFB

let StateMachinePayloadFB: StateMachinePayloadFBConstructor = failwith "JS only"

type StateMachineFB =
  abstract Action: StateMachineActionFB
  abstract PayloadType: StateMachinePayloadFB
  abstract DiscoveredServiceFB: DiscoveredServiceFB
  abstract CueFB: CueFB
  abstract CueListFB: CueListFB
  abstract PinFB: PinFB
  abstract PinGroupFB: PinGroupFB
  abstract RaftMemberFB: RaftMemberFB
  abstract UserFB: UserFB
  abstract SessionFB: SessionFB
  abstract LogEventFB: LogEventFB
  abstract StateFB: StateFB
  abstract StringFB: StringFB
  abstract ProjectFB: ProjectFB
  abstract IrisClientFB: IrisClientFB
  abstract CuePlayerFB: CuePlayerFB
  abstract ClockFB: ClockFB
  abstract CommandBatchFB: CommandBatchFB
  abstract Payload: 'a -> 'a

type StateMachineFBConstructor =
  abstract prototype: StateMachineFB with get, set
  abstract StartStateMachineFB: builder: FlatBufferBuilder -> unit
  abstract AddAction: builder: FlatBufferBuilder * tipe: StateMachineActionFB -> unit
  abstract AddPayloadType: builder: FlatBufferBuilder * tipe: StateMachinePayloadFB -> unit
  abstract AddPayload: builder: FlatBufferBuilder * payload: Offset<'a> -> unit
  abstract EndStateMachineFB: builder: FlatBufferBuilder -> Offset<StateMachineFB>
  abstract GetRootAsStateMachineFB: bytes: ByteBuffer -> StateMachineFB

let StateMachineFB: StateMachineFBConstructor = failwith "JS only"
