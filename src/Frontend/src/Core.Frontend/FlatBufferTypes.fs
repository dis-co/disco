module rec Disco.Web.Core.FlatBufferTypes

open Fable.Core
open Fable.Import
open Disco.Core.FlatBuffers

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
  abstract ProjectId: int -> byte
  abstract ProjectIdLength: int
  abstract ProjectName: string

type MachineStatusFBConstructor =
  abstract prototype: MachineStatusFB with get, set
  abstract StartMachineStatusFB: builder: FlatBufferBuilder -> unit
  abstract AddStatus: builder: FlatBufferBuilder * key: MachineStatusEnumFB -> unit
  abstract AddProjectId: builder: FlatBufferBuilder * value: VectorOffset -> unit
  abstract AddProjectName: builder: FlatBufferBuilder * value: Offset<string> -> unit
  abstract EndMachineStatusFB: builder: FlatBufferBuilder -> Offset<MachineStatusFB>
  abstract GetRootAsMachineStatusFB: buffer: ByteBuffer -> MachineStatusFB
  abstract CreateProjectIdVector: builder: FlatBufferBuilder * value:byte[] -> VectorOffset
  abstract Create: unit -> MachineStatusFB

let MachineStatusFB: MachineStatusFBConstructor = failwith "JS only"

//  ___      _     __  __            _     _
// |_ _|_ __(_)___|  \/  | __ _  ___| |__ (_)_ __   ___
//  | || '__| / __| |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
//  | || |  | \__ \ |  | | (_| | (__| | | | | | | |  __/
// |___|_|  |_|___/_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

type DiscoMachineFB =
  abstract MachineId: int -> byte
  abstract MachineIdLength: int -> byte
  abstract HostName: string
  abstract WorkSpace: string
  abstract LogDirectory: string
  abstract CollectMetrics: bool
  abstract MetricsHost: string
  abstract MetricsPort: uint16
  abstract MetricsDb: string
  abstract AssetDirectory: string
  abstract AssetFilter: string
  abstract BindAddress: string
  abstract MulticastAddress: string
  abstract MulticastPort: uint16
  abstract WebPort: uint16
  abstract RaftPort: uint16
  abstract WsPort: uint16
  abstract GitPort: uint16
  abstract ApiPort: uint16
  abstract Version: string

type DiscoMachineFBConstructor =
  abstract prototype: DiscoMachineFB with get, set
  abstract StartDiscoMachineFB: builder: FlatBufferBuilder -> unit
  abstract AddMachineId: builder: FlatBufferBuilder * key: VectorOffset -> unit
  abstract AddHostName: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddWorkSpace: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddLogDirectory: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddCollectMetrics: builder: FlatBufferBuilder * on: bool -> unit
  abstract AddMetricsHost: builder: FlatBufferBuilder * host: Offset<string> -> unit
  abstract AddMetricsPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddMetricsDb: builder: FlatBufferBuilder * db: Offset<string> -> unit
  abstract AddAssetDirectory: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddAssetFilter: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddBindAddress: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract AddMulticastAddress: builder: FlatBufferBuilder * addr: Offset<string> -> unit
  abstract AddMulticastPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddWebPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddRaftPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddWsPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddGitPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddApiPort: builder: FlatBufferBuilder * key: uint16 -> unit
  abstract AddVersion: builder: FlatBufferBuilder * key: Offset<string> -> unit
  abstract EndDiscoMachineFB: builder: FlatBufferBuilder -> Offset<DiscoMachineFB>
  abstract GetRootAsDiscoMachineFB: buffer: ByteBuffer -> DiscoMachineFB
  abstract CreateMachineIdVector: builder: FlatBufferBuilder * key:byte[] -> VectorOffset
  abstract Create: unit -> DiscoMachineFB

let DiscoMachineFB: DiscoMachineFBConstructor = failwith "JS only"

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

///  ____  _       _    __
/// |  _ \| | __ _| |_ / _| ___  _ __ _ __ ___
/// | |_) | |/ _` | __| |_ / _ \| '__| '_ ` _ \
/// |  __/| | (_| | |_|  _| (_) | |  | | | | | |
/// |_|   |_|\__,_|\__|_|  \___/|_|  |_| |_| |_|

type PlatformFB = int

type PlatformFBConstructor =
  abstract WindowsFB: PlatformFB
  abstract UnixFB: PlatformFB

let PlatformFB: PlatformFBConstructor = failwith "JS only"

///  _____   _____
/// |  ___|_|_   _| __ ___  ___
/// | |_ / __|| || '__/ _ \/ _ \
/// |  _|\__ \| || | |  __/  __/
/// |_|  |___/|_||_|  \___|\___|

type FsPathFB =
  abstract Drive: uint16
  abstract Platform: PlatformFB
  abstract ElementsLength: int
  abstract Elements: int -> string

type FsPathFBConstructor =
  abstract prototype: FsPathFB with get, set
  abstract StartFsPathFB: builder: FlatBufferBuilder -> unit
  abstract AddDrive: builder: FlatBufferBuilder * drive: uint16 -> unit
  abstract AddPlatform: builder: FlatBufferBuilder * platform: PlatformFB -> unit
  abstract AddElements: builder: FlatBufferBuilder * VectorOffset -> unit
  abstract CreateElementsVector: builder:FlatBufferBuilder * Offset<string>[] -> VectorOffset
  abstract EndFsPathFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsFsPathFB: buffer: ByteBuffer -> FsPathFB

let FsPathFB : FsPathFBConstructor = failwith "JS only"

type FsEntryTypeFB = int

type FsEntryTypeFBConstructor =
  abstract FileFB: FsEntryTypeFB
  abstract DirectoryFB: FsEntryTypeFB

let FsEntryTypeFB: FsEntryTypeFBConstructor = failwith "JS only"

type FsInfoFB =
  abstract Type: FsEntryTypeFB
  abstract Name: string
  abstract MimeType: string
  abstract Path: FsPathFB
  abstract Size: uint32
  abstract Filtered: uint32

type FsInfoFBConstructor =
  abstract prototype: FsInfoFB with get, set
  abstract StartFsInfoFB: builder: FlatBufferBuilder -> unit
  abstract AddType: builder: FlatBufferBuilder * tipe: FsEntryTypeFB -> unit
  abstract AddPath: builder: FlatBufferBuilder * path: Offset<FsPathFB> -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddMimeType: builder: FlatBufferBuilder * mime: Offset<string> -> unit
  abstract AddSize: builder: FlatBufferBuilder * size: uint32 -> unit
  abstract AddFiltered: builder: FlatBufferBuilder * filtered: uint32 -> unit
  abstract EndFsInfoFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsFsInfoFB: buffer: ByteBuffer -> FsInfoFB

let FsInfoFB : FsInfoFBConstructor = failwith "JS only"

type FsEntryFB =
  abstract Root: FsInfoFB
  abstract Children: int -> FsInfoFB
  abstract ChildrenLength: int

type FsEntryFBConstructor =
  abstract prototype: FsEntryFB with get, set
  abstract StartFsEntryFB: builder: FlatBufferBuilder -> unit
  abstract AddRoot: builder: FlatBufferBuilder * info: Offset<FsInfoFB> -> unit
  abstract AddChildren: builder: FlatBufferBuilder * children: VectorOffset -> unit
  abstract CreateChildrenVector: builder: FlatBufferBuilder * children: Offset<FsInfoFB> array -> VectorOffset
  abstract EndFsEntryFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsFsEntryFB: buffer: ByteBuffer -> FsEntryFB

let FsEntryFB : FsEntryFBConstructor = failwith "JS only"

type FsTreeFB =
  abstract HostId: int -> byte
  abstract HostIdLength: int
  abstract Root: FsInfoFB
  abstract Filters: string
  abstract Children: int -> FsInfoFB
  abstract ChildrenLength: int

type FsTreeFBConstructor =
  abstract prototype: FsTreeFB with get, set
  abstract StartFsTreeFB: builder: FlatBufferBuilder -> unit
  abstract AddHostId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddRoot: builder: FlatBufferBuilder * info: Offset<FsInfoFB> -> unit
  abstract AddFilters: builder: FlatBufferBuilder * filters: Offset<string> -> unit
  abstract AddChildren: builder: FlatBufferBuilder * children: VectorOffset -> unit
  abstract CreateChildrenVector: builder: FlatBufferBuilder * children: Offset<FsInfoFB> array -> VectorOffset
  abstract CreateHostIdVector: builder: FlatBufferBuilder * bytes: byte array -> VectorOffset
  abstract EndFsTreeFB: builder: FlatBufferBuilder -> Offset<FsTreeFB>
  abstract GetRootAsFsTreeFB: buffer: ByteBuffer -> FsTreeFB

let FsTreeFB : FsTreeFBConstructor = failwith "JS only"

type FsEntryUpdateFB =
  abstract Entry: FsEntryFB
  abstract Path: FsPathFB
  abstract HostId: int -> byte
  abstract HostIdLength: int

type FsEntryUpdateFBConstructor =
  abstract prototype: FsEntryUpdateFB with get, set
  abstract StartFsEntryUpdateFB: builder: FlatBufferBuilder -> unit
  abstract AddEntry: builder: FlatBufferBuilder * info: Offset<FsEntryFB> -> unit
  abstract AddPath: builder: FlatBufferBuilder * info: Offset<FsPathFB> -> unit
  abstract AddHostId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract CreateHostIdVector: builder: FlatBufferBuilder * children: byte array -> VectorOffset
  abstract EndFsEntryUpdateFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsFsEntryUpdateFB: buffer: ByteBuffer -> FsEntryUpdateFB

let FsEntryUpdateFB : FsEntryUpdateFBConstructor = failwith "JS only"

type FsTreeUpdateFB =
  abstract Tree: FsTreeFB
  abstract HostId: int -> byte
  abstract HostIdLength: int

type FsTreeUpdateFBConstructor =
  abstract prototype: FsTreeUpdateFB with get, set
  abstract StartFsTreeUpdateFB: builder: FlatBufferBuilder -> unit
  abstract AddTree: builder: FlatBufferBuilder * info: Offset<FsTreeFB> -> unit
  abstract AddHostId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract CreateHostIdVector: builder: FlatBufferBuilder * children: byte array -> VectorOffset
  abstract EndFsTreeUpdateFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsFsTreeUpdateFB: buffer: ByteBuffer -> FsTreeUpdateFB

let FsTreeUpdateFB : FsTreeUpdateFBConstructor = failwith "JS only"

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

type DiscoClientFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract Role: RoleFB
  abstract ServiceId: int -> byte
  abstract ServiceIdLength: int -> byte
  abstract Status: ServiceStatusFB
  abstract IpAddress: string
  abstract Port: uint16

type DiscoClientFBConstructor =
  abstract prototype: DiscoClientFB with get, set
  abstract StartDiscoClientFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddServiceId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddRole: builder: FlatBufferBuilder * role: RoleFB -> unit
  abstract AddStatus: builder: FlatBufferBuilder * status: Offset<ServiceStatusFB> -> unit
  abstract AddIpAddress: builder: FlatBufferBuilder * ip: Offset<string> -> unit
  abstract AddPort: builder: FlatBufferBuilder * port:uint16 -> unit
  abstract EndDiscoClientFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract CreateIdVector: FlatBufferBuilder * byte[] -> VectorOffset
  abstract CreateServiceIdVector: builder: FlatBufferBuilder * id:byte[] -> VectorOffset
  abstract GetRootAsDiscoClientFB: buffer: ByteBuffer -> DiscoClientFB

let DiscoClientFB : DiscoClientFBConstructor = failwith "JS only"

//  _   _               _____ ____
// | | | |___  ___ _ __|  ___| __ )
// | | | / __|/ _ \ '__| |_  |  _ \
// | |_| \__ \  __/ |  |  _| | |_) |
//  \___/|___/\___|_|  |_|   |____/

type UserFB =
  abstract Id: int -> byte
  abstract IdLength: int -> byte
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
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
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
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte[] -> VectorOffset

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
  abstract Id: int -> byte
  abstract IdLength: int -> byte
  abstract IpAddress: string
  abstract UserAgent: string

type SessionFBConstructor =
  abstract prototype: SessionFB with get, set
  abstract StartSessionFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddStatus: builder: FlatBufferBuilder * status: Offset<SessionStatusFB> -> unit
  abstract AddIpAddress: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddUserAgent: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract EndSessionFB: builder: FlatBufferBuilder -> Offset<SessionFB>
  abstract GetRootAsSessionFB: buffer: ByteBuffer -> SessionFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte[] -> VectorOffset
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

type PinConfigurationFB = int

type PinConfigurationFBConstructor =
  abstract SinkFB: PinConfigurationFB
  abstract SourceFB: PinConfigurationFB
  abstract PresetFB: PinConfigurationFB

let PinConfigurationFB: PinConfigurationFBConstructor = failwith "JS only"

//  ____              _ ____  _       _____ ____
// | __ )  ___   ___ | |  _ \(_)_ __ |  ___| __ )
// |  _ \ / _ \ / _ \| | |_) | | '_ \| |_  |  _ \
// | |_) | (_) | (_) | |  __/| | | | |  _| | |_) |
// |____/ \___/ \___/|_|_|   |_|_| |_|_|   |____/

type BoolPinFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract PinGroupId: int -> byte
  abstract PinGroupIdLength: int
  abstract ClientId: int -> byte
  abstract ClientIdLength: int
  abstract IsTrigger: bool
  abstract VecSize: VecSizeFB
  abstract Persisted: bool
  abstract Dirty: bool
  abstract Online: bool
  abstract PinConfiguration: PinConfigurationFB
  abstract Tags: int -> KeyValueFB
  abstract TagsLength: int
  abstract Labels: index: int -> string
  abstract LabelsLength: int
  abstract Values: index: int -> bool
  abstract ValuesLength: int

type BoolPinFBConstructor =
  abstract prototype: BoolPinFB with get, set
  abstract StartBoolPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroupId: builder: FlatBufferBuilder * group: VectorOffset -> unit
  abstract AddClientId: builder: FlatBufferBuilder * client: VectorOffset -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddDirty: builder: FlatBufferBuilder * dirty: bool -> unit
  abstract AddOnline: builder: FlatBufferBuilder * online: bool -> unit
  abstract AddIsTrigger: builder: FlatBufferBuilder * trigger: bool -> unit
  abstract AddPinConfiguration: builder: FlatBufferBuilder * PinConfigurationFB -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * labels: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndBoolPinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsBoolPinFB: buffer: ByteBuffer -> BoolPinFB
  abstract CreateIdVector: builder: FlatBufferBuilder * byte[] -> VectorOffset
  abstract CreatePinGroupIdVector: builder: FlatBufferBuilder * byte[] -> VectorOffset
  abstract CreateClientIdVector: builder: FlatBufferBuilder * byte[] -> VectorOffset
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> Offset<'a>
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
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract PinGroupId: int -> byte
  abstract PinGroupIdLength: int
  abstract ClientId: int -> byte
  abstract ClientIdLength: int
  abstract Min: int
  abstract Max: int
  abstract Unit: string
  abstract Precision: uint32
  abstract Persisted: bool
  abstract Dirty: bool
  abstract Online: bool
  abstract VecSize: VecSizeFB
  abstract PinConfiguration: PinConfigurationFB
  abstract Tags: int -> KeyValueFB
  abstract TagsLength: int
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> double
  abstract ValuesLength: int

type NumberPinFBConstructor =
  abstract prototype: NumberPinFB with get, set
  abstract StartNumberPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroupId: builder: FlatBufferBuilder * group: VectorOffset -> unit
  abstract AddClientId: builder: FlatBufferBuilder * client: VectorOffset -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * vecsize: uint32 -> unit
  abstract AddMin: builder: FlatBufferBuilder * min: int -> unit
  abstract AddMax: builder: FlatBufferBuilder * max: int -> unit
  abstract AddUnit: builder: FlatBufferBuilder * unit: Offset<string> -> unit
  abstract AddPrecision: builder: FlatBufferBuilder * precision: uint32 -> unit
  abstract AddOnline: builder: FlatBufferBuilder * online: bool -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddDirty: builder: FlatBufferBuilder * dirty: bool -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddPinConfiguration: builder: FlatBufferBuilder * PinConfigurationFB -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndNumberPinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsNumberPinFB: buffer: ByteBuffer -> NumberPinFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte[] -> VectorOffset
  abstract CreatePinGroupIdVector: builder: FlatBufferBuilder * id:byte[] -> VectorOffset
  abstract CreateClientIdVector: builder: FlatBufferBuilder * id:byte[] -> VectorOffset
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> Offset<'a>
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
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract PinGroupId: int -> byte
  abstract PinGroupIdLength: int
  abstract ClientId: int -> byte
  abstract ClientIdLength: int
  abstract VecSize: VecSizeFB
  abstract PinConfiguration: PinConfigurationFB
  abstract Persisted: bool
  abstract Dirty: bool
  abstract Online: bool
  abstract TagsLength: int
  abstract Tags: int -> KeyValueFB
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> string
  abstract ValuesLength: int

type BytePinFBConstructor =
  abstract prototype: BytePinFB with get, set
  abstract StartBytePinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroupId: builder: FlatBufferBuilder * group: VectorOffset -> unit
  abstract AddClientId: builder: FlatBufferBuilder * client: VectorOffset -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddDirty: builder: FlatBufferBuilder * dirty: bool -> unit
  abstract AddOnline: builder: FlatBufferBuilder * online: bool -> unit
  abstract AddPinConfiguration: builder: FlatBufferBuilder * PinConfigurationFB -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndBytePinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsBytePinFB: buffer: ByteBuffer -> BytePinFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreatePinGroupIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateClientIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> Offset<'a>
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
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract PinGroupId: int -> byte
  abstract PinGroupIdLength: int
  abstract ClientId: int -> byte
  abstract ClientIdLength: int
  abstract VecSize: VecSizeFB
  abstract PinConfiguration: PinConfigurationFB
  abstract Tags: int -> KeyValueFB
  abstract Persisted: bool
  abstract Dirty: bool
  abstract Online: bool
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
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroupId: builder: FlatBufferBuilder * name: VectorOffset -> unit
  abstract AddClientId: builder: FlatBufferBuilder * name: VectorOffset -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddPinConfiguration: builder: FlatBufferBuilder * PinConfigurationFB -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddDirty: builder: FlatBufferBuilder * dirty: bool -> unit
  abstract AddOnline: builder: FlatBufferBuilder * online: bool -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddProperties: builder: FlatBufferBuilder * properties: Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndEnumPinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsEnumPinFB: buffer: ByteBuffer -> EnumPinFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreatePinGroupIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateClientIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> Offset<'a>
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
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract PinGroupId: int -> byte
  abstract PinGroupIdLength: int
  abstract ClientId: int -> byte
  abstract ClientIdLength: int
  abstract VecSize: VecSizeFB
  abstract PinConfiguration: PinConfigurationFB
  abstract TagsLength: int
  abstract Tags: int -> KeyValueFB
  abstract Persisted: bool
  abstract Dirty: bool
  abstract Online: bool
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> ColorSpaceFB
  abstract ValuesLength: int

type ColorPinFBConstructor =
  abstract prototype: ColorPinFB with get, set
  abstract StartColorPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroupId: builder: FlatBufferBuilder * name: VectorOffset -> unit
  abstract AddClientId: builder: FlatBufferBuilder * name: VectorOffset -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddPinConfiguration: builder: FlatBufferBuilder * PinConfigurationFB -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddDirty: builder: FlatBufferBuilder * dirty: bool -> unit
  abstract AddOnline: builder: FlatBufferBuilder * online: bool -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndColorPinFB: builder: FlatBufferBuilder -> Offset<ColorPinFB>
  abstract GetRootAsColorPinFB: buffer: ByteBuffer -> ColorPinFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreatePinGroupIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateClientIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> Offset<'a>
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
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract PinGroupId: int -> byte
  abstract PinGroupIdLength: int
  abstract ClientId: int -> byte
  abstract ClientIdLength: int
  abstract Behavior: BehaviorFB
  abstract VecSize: VecSizeFB
  abstract PinConfiguration: PinConfigurationFB
  abstract MaxChars: int
  abstract Persisted: bool
  abstract Dirty: bool
  abstract Online: bool
  abstract Tags: int -> KeyValueFB
  abstract TagsLength: int
  abstract Labels: int -> string
  abstract LabelsLength: int
  abstract Values: int -> string
  abstract ValuesLength: int

type StringPinFBConstructor =
  abstract prototype: StringPinFB with get, set
  abstract StartStringPinFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPinGroupId: builder: FlatBufferBuilder * name: VectorOffset -> unit
  abstract AddClientId: builder: FlatBufferBuilder * name: VectorOffset -> unit
  abstract AddPersisted: builder: FlatBufferBuilder * persisted: bool -> unit
  abstract AddDirty: builder: FlatBufferBuilder * dirty: bool -> unit
  abstract AddOnline: builder: FlatBufferBuilder * online: bool -> unit
  abstract AddBehavior: builder: FlatBufferBuilder * tipe: BehaviorFB -> unit
  abstract AddMaxChars: builder: FlatBufferBuilder * max: int -> unit
  abstract AddPinConfiguration: builder: FlatBufferBuilder * PinConfigurationFB -> unit
  abstract AddVecSize: builder: FlatBufferBuilder * Offset<'a> -> unit
  abstract AddTags: builder: FlatBufferBuilder * tags: Offset<'a> -> unit
  abstract AddLabels: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract AddValues: builder: FlatBufferBuilder * values: Offset<'a> -> unit
  abstract EndStringPinFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreatePinGroupIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateClientIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateTagsVector: builder: FlatBufferBuilder * Offset<KeyValueFB> array -> Offset<'a>
  abstract CreateLabelsVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract CreateValuesVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract GetRootAsStringPinFB: buffer: ByteBuffer -> StringPinFB
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
  abstract Trigger: bool
  abstract ValuesLength: int
  abstract Values: int -> bool

type BoolsFBConstructor =
  abstract StartBoolsFB: builder: FlatBufferBuilder -> unit
  abstract CreateValuesVector: builder: FlatBufferBuilder * bool array -> VectorOffset
  abstract AddTrigger: builder: FlatBufferBuilder * trig:bool -> unit
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
  abstract PinId: int -> byte
  abstract PinIdLength: int
  abstract ClientId: int -> byte
  abstract ClientIdLength: int
  abstract Slices: 'a -> 'a
  abstract SlicesType: SliceTypeFB

type SlicesFBConstructor =
  abstract prototype: SlicesFB with get, set
  abstract StartSlicesFB: builder: FlatBufferBuilder -> unit
  abstract AddPinId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddClientId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddSlicesType: builder: FlatBufferBuilder * tipe: SlicesTypeFB -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndSlicesFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsSlicesFB: buffer: ByteBuffer -> SlicesFB
  abstract CreatePinIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateClientIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<SliceFB> array -> Offset<'a>
  abstract Create: unit -> SlicesFB

let SlicesFB : SlicesFBConstructor = failwith "JS only"

//   ____           _____ ____
//  / ___|   _  ___|  ___| __ )
// | |  | | | |/ _ \ |_  |  _ \
// | |__| |_| |  __/  _| | |_) |
//  \____\__,_|\___|_|   |____/

type CueFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract SlicesLength: int
  abstract Slices: int -> SlicesFB

type CueFBConstructor =
  abstract prototype: CueFB with get, set
  abstract StartCueFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddSlices: builder: FlatBufferBuilder * slices: Offset<'a> -> unit
  abstract EndCueFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueFB: buffer: ByteBuffer -> CueFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<SlicesFB> array -> Offset<'a>
  abstract Create: unit -> CueFB

let CueFB : CueFBConstructor = failwith "JS only"

type CueReferenceFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract CueId: int -> byte
  abstract CueIdLength: int -> byte
  abstract AutoFollow: bool
  abstract Duration: int
  abstract Prewait: int

type CueReferenceFBConstructor =
  abstract prototype: CueReferenceFB with get, set
  abstract StartCueReferenceFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddCueId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddAutoFollow: builder: FlatBufferBuilder * value:bool -> unit
  abstract AddDuration: builder: FlatBufferBuilder * value: int -> unit
  abstract AddPrewait: builder: FlatBufferBuilder * value: int -> unit
  abstract EndCueReferenceFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateCueIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract GetRootAsCueReferenceFB: buffer: ByteBuffer -> CueReferenceFB
  abstract Create: unit -> CueReferenceFB

let CueReferenceFB : CueReferenceFBConstructor = failwith "JS only"

type CueGroupFB =
  abstract Id: int -> byte
  abstract IdLength: int -> byte
  abstract Name: string
  abstract AutoFollow: bool
  abstract CueRefs: int -> CueReferenceFB
  abstract CueRefsLength: int

type CueGroupFBConstructor =
  abstract prototype: CueGroupFB with get, set
  abstract StartCueGroupFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddAutoFollow: builder: FlatBufferBuilder * flag: bool -> unit
  abstract AddCueRefs: builder: FlatBufferBuilder * cues: Offset<'a> -> unit
  abstract EndCueGroupFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueGroupFB: buffer: ByteBuffer -> CueGroupFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateCueRefsVector: builder: FlatBufferBuilder * Offset<CueReferenceFB> array -> Offset<'a>
  abstract Create: unit -> CueGroupFB

let CueGroupFB : CueGroupFBConstructor = failwith "JS only"

///  ____       __                                  ___     __    _
/// |  _ \ ___ / _| ___ _ __ ___ _ __   ___ ___  __| \ \   / /_ _| |_   _  ___
/// | |_) / _ \ |_ / _ \ '__/ _ \ '_ \ / __/ _ \/ _` |\ \ / / _` | | | | |/ _ \
/// |  _ <  __/  _|  __/ | |  __/ | | | (_|  __/ (_| | \ V / (_| | | |_| |  __/
/// |_| \_\___|_|  \___|_|  \___|_| |_|\___\___|\__,_|  \_/ \__,_|_|\__,_|\___|

type ReferencedValueTypeFB = int

type ReferencedValueTypeFBConstructor =
  abstract PlayerFB: ReferencedValueTypeFB
  abstract WidgetFB: ReferencedValueTypeFB

let ReferencedValueTypeFB: ReferencedValueTypeFBConstructor = failwith "JS only"

[<AllowNullLiteral>]
type ReferencedValueFB =
  abstract Id: int -> byte
  abstract IdLength: int -> byte
  abstract Type: ReferencedValueTypeFB

type ReferencedValueFBConstructor =
  abstract prototype: ReferencedValueFB with get, set
  abstract StartReferencedValueFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddType: builder: FlatBufferBuilder * tipe: ReferencedValueTypeFB -> unit
  abstract EndReferencedValueFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsReferencedValueFB: buffer: ByteBuffer -> ReferencedValueFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset

let ReferencedValueFB : ReferencedValueFBConstructor = failwith "JS only"

///  ____  _        ____
/// |  _ \(_)_ __  / ___|_ __ ___  _   _ _ __
/// | |_) | | '_ \| |  _| '__/ _ \| | | | '_ \
/// |  __/| | | | | |_| | | | (_) | |_| | |_) |
/// |_|   |_|_| |_|\____|_|  \___/ \__,_| .__/
///                                     |_|

type PinGroupFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract ClientId: int -> byte
  abstract ClientIdLength: int
  abstract Path: string
  abstract RefersTo: ReferencedValueFB
  abstract Pins: int -> PinFB
  abstract PinsLength: int

type PinGroupFBConstructor =
  abstract prototype: PinGroupFB with get, set
  abstract StartPinGroupFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddPath: builder: FlatBufferBuilder * path: Offset<string> -> unit
  abstract AddRefersTo: builder: FlatBufferBuilder * path: Offset<ReferencedValueFB> -> unit
  abstract AddClientId: builder: FlatBufferBuilder * client: VectorOffset -> unit
  abstract AddPins: builder: FlatBufferBuilder * pins: Offset<'a> -> unit
  abstract EndPinGroupFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsPinGroupFB: buffer: ByteBuffer -> PinGroupFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateClientIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreatePinsVector: builder: FlatBufferBuilder * Offset<PinFB> array -> Offset<'a>

let PinGroupFB : PinGroupFBConstructor = failwith "JS only"

///  ____  _        ____                       __  __
/// |  _ \(_)_ __  / ___|_ __ ___  _   _ _ __ |  \/  | __ _ _ __
/// | |_) | | '_ \| |  _| '__/ _ \| | | | '_ \| |\/| |/ _` | '_ \
/// |  __/| | | | | |_| | | | (_) | |_| | |_) | |  | | (_| | |_) |
/// |_|   |_|_| |_|\____|_|  \___/ \__,_| .__/|_|  |_|\__,_| .__/
///                                     |_|                |_|

type PinGroupMapFB =
  abstract GroupsLength: int
  abstract Groups: int -> PinGroupFB

type PinGroupMapFBConstructor =
  abstract prototype: PinGroupMapFB with get, set
  abstract StartPinGroupMapFB: builder: FlatBufferBuilder -> unit
  abstract AddGroups: builder: FlatBufferBuilder * groups: Offset<'a> -> unit
  abstract EndPinGroupMapFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsPinGroupMapFB: buffer: ByteBuffer -> PinGroupMapFB
  abstract CreateGroupsVector: builder: FlatBufferBuilder * Offset<PinGroupFB> array -> Offset<'a>

let PinGroupMapFB : PinGroupMapFBConstructor = failwith "JS only"

//   ____           _     _     _   _____ ____
//  / ___|   _  ___| |   (_)___| |_|  ___| __ )
// | |  | | | |/ _ \ |   | / __| __| |_  |  _ \
// | |__| |_| |  __/ |___| \__ \ |_|  _| | |_) |
//  \____\__,_|\___|_____|_|___/\__|_|   |____/

type CueListFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract ItemsLength: int
  abstract Items: int -> CueGroupFB

type CueListFBConstructor =
  abstract prototype: CueListFB with get, set
  abstract StartCueListFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddItems: builder: FlatBufferBuilder * items: Offset<'a> -> unit
  abstract EndCueListFB: builder: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsCueListFB: buffer: ByteBuffer -> CueListFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateItemsVector: builder: FlatBufferBuilder * Offset<CueGroupFB> array -> Offset<'a>

let CueListFB : CueListFBConstructor = failwith "JS only"

///  __  __                _               ____  _        _
/// |  \/  | ___ _ __ ___ | |__   ___ _ __/ ___|| |_ __ _| |_ _   _ ___
/// | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__\___ \| __/ _` | __| | | / __|
/// | |  | |  __/ | | | | | |_) |  __/ |   ___) | || (_| | |_| |_| \__ \
/// |_|  |_|\___|_| |_| |_|_.__/ \___|_|  |____/ \__\__,_|\__|\__,_|___/

type MemberStatusFB = int

type MemberStatusFBConstructor =
  abstract JoiningFB: MemberStatusFB
  abstract RunningFB: MemberStatusFB
  abstract FailedFB: MemberStatusFB

let MemberStatusFB: MemberStatusFBConstructor = failwith "JS only"

///  __  __                _               ____  _        _       _____ ____
/// |  \/  | ___ _ __ ___ | |__   ___ _ __/ ___|| |_ __ _| |_ ___|  ___| __ )
/// | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__\___ \| __/ _` | __/ _ \ |_  |  _ \
/// | |  | |  __/ | | | | | |_) |  __/ |   ___) | || (_| | ||  __/  _| | |_) |
/// |_|  |_|\___|_| |_| |_|_.__/ \___|_|  |____/ \__\__,_|\__\___|_|   |____/

type MemberStateFB = int

type MemberStateFBConstructor =
  abstract FollowerFB: MemberStateFB
  abstract LeaderFB: MemberStateFB
  abstract CandidateFB: MemberStateFB

let MemberStateFB: MemberStateFBConstructor = failwith "JS only"

///  ____        __ _   __  __                _
/// |  _ \ __ _ / _| |_|  \/  | ___ _ __ ___ | |__   ___ _ __
/// | |_) / _` | |_| __| |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
/// |  _ < (_| |  _| |_| |  | |  __/ | | | | | |_) |  __/ |
/// |_| \_\__,_|_|  \__|_|  |_|\___|_| |_| |_|_.__/ \___|_|

type RaftMemberFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract HostName: string
  abstract IpAddress: string
  abstract MulticastAddress: string
  abstract MulticastPort: uint16
  abstract RaftPort: uint16
  abstract HttpPort: uint16
  abstract WsPort: uint16
  abstract GitPort: uint16
  abstract ApiPort: uint16
  abstract Voting: bool
  abstract VotedForMe: bool
  abstract State: MemberStateFB
  abstract Status: MemberStatusFB
  abstract NextIndex: int
  abstract MatchIndex: int

type RaftMemberFBConstructor =
  abstract prototype: RaftMemberFB with get, set
  abstract StartRaftMemberFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddHostName: builder: FlatBufferBuilder * hostname: Offset<string> -> unit
  abstract AddIpAddress: builder: FlatBufferBuilder * ip: Offset<string> -> unit
  abstract AddMulticastAddress: builder: FlatBufferBuilder * addr: Offset<string> -> unit
  abstract AddMulticastPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddRaftPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddHttpPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddWsPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddGitPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddApiPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddVoting: builder: FlatBufferBuilder * voting: bool -> unit
  abstract AddVotedForMe: builder: FlatBufferBuilder * votedforme: bool -> unit
  abstract AddState: builder: FlatBufferBuilder * state: MemberStateFB -> unit
  abstract AddStatus: builder: FlatBufferBuilder * state: MemberStatusFB -> unit
  abstract AddNextIndex: builder: FlatBufferBuilder * idx: int -> unit
  abstract AddMatchIndex: builder: FlatBufferBuilder * idx: int -> unit
  abstract EndRaftMemberFB: builder: FlatBufferBuilder -> Offset<RaftMemberFB>
  abstract GetRootAsRaftMemberFB: bytes: ByteBuffer -> RaftMemberFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
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

// Client Executable

type ClientExecutableFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Executable: string
  abstract Version:  string
  abstract Required: bool

type ClientExecutableFBConstructor =
  abstract prototype: ClientExecutableFB with get, set
  abstract StartClientExecutableFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id:VectorOffset -> unit
  abstract AddExecutable: builder: FlatBufferBuilder * exe:Offset<string> -> unit
  abstract AddVersion: builder: FlatBufferBuilder * version:Offset<string> -> unit
  abstract AddRequired: builder: FlatBufferBuilder * required:bool -> unit
  abstract EndClientExecutableFB: builder: FlatBufferBuilder -> Offset<ClientExecutableFB>
  abstract GetRootAsClientExecutableFB: bytes: ByteBuffer -> ClientExecutableFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract Create: unit -> ClientExecutableFB

let ClientExecutableFB: ClientExecutableFBConstructor = failwith "JS only"

// Client CONFIG

type ClientConfigFB =
  abstract Executables: int -> ClientExecutableFB
  abstract ExecutablesLength: int

type ClientConfigFBConstructor =
  abstract prototype: ClientConfigFB with get, set
  abstract StartClientConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddExecutables: builder: FlatBufferBuilder * exes:Offset<'a> -> unit
  abstract CreateExecutablesVector: FlatBufferBuilder * Offset<ClientExecutableFB> array -> Offset<'a>
  abstract EndClientConfigFB: builder: FlatBufferBuilder -> Offset<ClientConfigFB>
  abstract GetRootAsClientConfigFB: bytes: ByteBuffer -> ClientConfigFB
  abstract Create: unit -> ClientConfigFB

let ClientConfigFB: ClientConfigFBConstructor = failwith "JS only"

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

// CLUSTER MEMBER

type ClusterMemberFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract HostName: string
  abstract IpAddress: string
  abstract MulticastAddress: string
  abstract MulticastPort: uint16
  abstract RaftPort: uint16
  abstract HttpPort: uint16
  abstract WsPort: uint16
  abstract GitPort: uint16
  abstract ApiPort: uint16
  abstract State: MemberStateFB
  abstract Status: MemberStatusFB

type ClusterMemberFBConstructor =
  abstract prototype: ClusterMemberFB with get, set
  abstract StartClusterMemberFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddHostName: builder: FlatBufferBuilder * hostname: Offset<string> -> unit
  abstract AddIpAddress: builder: FlatBufferBuilder * ip: Offset<string> -> unit
  abstract AddMulticastAddress: builder: FlatBufferBuilder * addr: Offset<string> -> unit
  abstract AddMulticastPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddRaftPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddHttpPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddWsPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddGitPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddApiPort: builder: FlatBufferBuilder * port: uint16 -> unit
  abstract AddState: builder: FlatBufferBuilder * state: MemberStateFB -> unit
  abstract AddStatus: builder: FlatBufferBuilder * state: MemberStatusFB -> unit
  abstract EndClusterMemberFB: builder: FlatBufferBuilder -> Offset<ClusterMemberFB>
  abstract GetRootAsClusterMemberFB: bytes: ByteBuffer -> ClusterMemberFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract Create: unit -> ClusterMemberFB

let ClusterMemberFB: ClusterMemberFBConstructor = failwith "JS only"

// CLUSTER CONFIG

type ClusterConfigFB =
  abstract Id:            int -> byte
  abstract IdLength:      int
  abstract Name:          string
  abstract Members:       int -> ClusterMemberFB
  abstract MembersLength: int
  abstract Groups:        int -> HostGroupFB
  abstract GroupsLength:  int

type ClusterConfigFBConstructor =
  abstract prototype: ClusterConfigFB with get, set
  abstract StartClusterConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id:VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name:Offset<string> -> unit
  abstract AddMembers: builder: FlatBufferBuilder * mems:Offset<'a> -> unit
  abstract CreateMembersVector: FlatBufferBuilder * Offset<ClusterMemberFB> array -> Offset<'a>
  abstract AddGroups: builder: FlatBufferBuilder * mems:Offset<'a> -> unit
  abstract CreateIdVector: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateGroupsVector: FlatBufferBuilder * Offset<HostGroupFB> array -> Offset<'a>
  abstract EndClusterConfigFB: builder: FlatBufferBuilder -> Offset<ClusterConfigFB>
  abstract GetRootAsClusterConfigFB: bytes: ByteBuffer -> ClusterConfigFB
  abstract Create: unit -> ClusterConfigFB

let ClusterConfigFB: ClusterConfigFBConstructor = failwith "JS only"

// CONFIG

type ConfigFB =
  abstract Version: string
  abstract Machine: DiscoMachineFB
  abstract ActiveSite: int -> byte
  abstract ActiveSiteLength: int
  abstract AudioConfig: AudioConfigFB
  abstract ClientConfig: ClientConfigFB
  abstract RaftConfig: RaftConfigFB
  abstract TimingConfig: TimingConfigFB
  abstract Sites: int -> ClusterConfigFB
  abstract SitesLength: int

type ConfigFBConstructor =
  abstract prototype: ConfigFB with get, set
  abstract StartConfigFB: builder: FlatBufferBuilder -> unit
  abstract AddVersion: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddMachine: builder: FlatBufferBuilder * v:Offset<DiscoMachineFB> -> unit
  abstract AddActiveSite: builder: FlatBufferBuilder * v:VectorOffset -> unit
  abstract AddAudioConfig: builder: FlatBufferBuilder * v:Offset<AudioConfigFB> -> unit
  abstract AddClientConfig: builder: FlatBufferBuilder * v:Offset<ClientConfigFB> -> unit
  abstract AddRaftConfig: builder: FlatBufferBuilder * v:Offset<RaftConfigFB> -> unit
  abstract AddTimingConfig: builder: FlatBufferBuilder * v:Offset<TimingConfigFB> -> unit
  abstract AddSites: builder: FlatBufferBuilder * v:Offset<'a> -> unit
  abstract CreateActiveSiteVector: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateSitesVector: FlatBufferBuilder * Offset<ClusterConfigFB> array -> Offset<'a>
  abstract EndConfigFB: FlatBufferBuilder -> Offset<'a>
  abstract GetRootAsConfigFB: bytes: ByteBuffer -> ConfigFB
  abstract Create: unit -> ConfigFB

let ConfigFB: ConfigFBConstructor = failwith "JS only"

// PROJECT

type ProjectFB =
  abstract Id: int -> byte
  abstract IdLength: int
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
  abstract AddId: builder: FlatBufferBuilder * v:VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddPath: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddCreatedOn: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddLastSaved: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddCopyright: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddAuthor: builder: FlatBufferBuilder * v:Offset<string> -> unit
  abstract AddConfig: builder: FlatBufferBuilder * v:Offset<ConfigFB> -> unit
  abstract EndProjectFB: builder: FlatBufferBuilder -> Offset<ProjectFB>
  abstract CreateIdVector: FlatBufferBuilder * id:byte array -> VectorOffset
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
  abstract Id: int -> byte
  abstract IdLength: int
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
  abstract AddId: builder: FlatBufferBuilder * VectorOffset -> unit
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
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
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
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract Locked: bool
  abstract Active: bool
  abstract Selected: int
  abstract RemainingWait: int
  abstract CueListId: int -> byte
  abstract CueListIdLength: int
  abstract CallId: int -> byte
  abstract CallIdLength: int
  abstract NextId: int -> byte
  abstract NextIdLength: int
  abstract PreviousId: int -> byte
  abstract PreviousIdLength: int
  abstract LastCallerId: int -> byte
  abstract LastCallerIdLength: int
  abstract LastCalledId: int -> byte
  abstract LastCalledIdLength: int

type CuePlayerFBConstructor =
  abstract prototype: CuePlayerFB with get, set
  abstract StartCuePlayerFB: builder: FlatBufferBuilder -> unit
  abstract AddId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddName: builder: FlatBufferBuilder * name: Offset<string> -> unit
  abstract AddLocked: builder: FlatBufferBuilder * locked:bool -> unit
  abstract AddActive: builder: FlatBufferBuilder * active:bool -> unit
  abstract AddSelected: builder: FlatBufferBuilder * int -> unit
  abstract AddRemainingWait: builder: FlatBufferBuilder * int -> unit
  abstract AddCueListId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddCallId: builder: FlatBufferBuilder * call: VectorOffset -> unit
  abstract AddNextId: builder: FlatBufferBuilder * next: VectorOffset -> unit
  abstract AddPreviousId: builder: FlatBufferBuilder * previous: VectorOffset -> unit
  abstract AddLastCallerId: builder: FlatBufferBuilder * lastcaller: VectorOffset -> unit
  abstract AddLastCalledId: builder: FlatBufferBuilder * lastcalled: VectorOffset -> unit
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateCueListIdVector: builder: FlatBufferBuilder * offset:byte array -> VectorOffset
  abstract CreateCallIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateNextIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreatePreviousIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateLastCallerIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateLastCalledIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
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
  abstract PinGroups: PinGroupMapFB
  abstract PinMappings: int -> PinMappingFB
  abstract PinMappingsLength: int
  abstract PinWidgets: int -> PinWidgetFB
  abstract PinWidgetsLength: int
  abstract FsTrees: int -> FsTreeFB
  abstract FsTreesLength: int
  abstract Cues: int -> CueFB
  abstract CuesLength: int
  abstract CueLists: int -> CueListFB
  abstract CueListsLength: int
  abstract Sessions: int -> SessionFB
  abstract SessionsLength: int
  abstract Users: int -> UserFB
  abstract UsersLength: int
  abstract Clients: int -> DiscoClientFB
  abstract ClientsLength: int
  abstract CuePlayers: int -> CuePlayerFB
  abstract CuePlayersLength: int
  abstract DiscoveredServices: int -> DiscoveredServiceFB
  abstract DiscoveredServicesLength: int

type StateFBConstructor =
  abstract prototype: StateFB with get, set
  abstract StartStateFB: builder: FlatBufferBuilder -> unit
  abstract AddProject: builder: FlatBufferBuilder * project: Offset<ProjectFB> -> unit
  abstract AddPinGroups: builder: FlatBufferBuilder * groups: Offset<PinGroupMapFB> -> unit
  abstract AddPinMappings: builder: FlatBufferBuilder * mappings: VectorOffset -> unit
  abstract AddPinWidgets: builder: FlatBufferBuilder * widgets: VectorOffset -> unit
  abstract AddFsTrees: builder: FlatBufferBuilder * fsTrees: VectorOffset -> unit
  abstract AddCues: builder: FlatBufferBuilder * cues: VectorOffset -> unit
  abstract AddCueLists: builder: FlatBufferBuilder * cuelists: VectorOffset -> unit
  abstract AddSessions: builder: FlatBufferBuilder * sessions: VectorOffset -> unit
  abstract AddUsers: builder: FlatBufferBuilder * users: VectorOffset -> unit
  abstract AddClients: builder: FlatBufferBuilder * clients: VectorOffset -> unit
  abstract AddCuePlayers: builder: FlatBufferBuilder * cueplayers: VectorOffset -> unit
  abstract AddDiscoveredServices: builder: FlatBufferBuilder * services: VectorOffset -> unit
  abstract CreateCuesVector: builder: FlatBufferBuilder * cues: Offset<CueFB> array -> VectorOffset
  abstract CreateFsTreesVector: builder: FlatBufferBuilder * fsTrees: Offset<FsTreeFB> array -> VectorOffset
  abstract CreateSessionsVector: builder: FlatBufferBuilder * groups: Offset<SessionFB> array -> VectorOffset
  abstract CreatePinMappingsVector: builder: FlatBufferBuilder * mappings: Offset<PinMappingFB> array -> VectorOffset
  abstract CreatePinWidgetsVector: builder: FlatBufferBuilder * widgets: Offset<PinWidgetFB> array -> VectorOffset
  abstract CreateCueListsVector: builder: FlatBufferBuilder * groups: Offset<CueListFB> array -> VectorOffset
  abstract CreateCuePlayersVector: builder: FlatBufferBuilder * groups: Offset<CuePlayerFB> array -> VectorOffset
  abstract CreateUsersVector: builder: FlatBufferBuilder * groups: Offset<UserFB> array -> VectorOffset
  abstract CreateClientsVector: builder: FlatBufferBuilder * groups: Offset<DiscoClientFB> array -> VectorOffset
  abstract CreateDiscoveredServicesVector: builder: FlatBufferBuilder * groups: Offset<DiscoveredServiceFB> array -> VectorOffset
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
  abstract MachineId: int -> byte
  abstract MachineIdLength: int
  abstract Time: uint32
  abstract Thread: int
  abstract Tier: string
  abstract Tag: string
  abstract LogLevel: string
  abstract Message: string

type LogEventFBConstructor =
  abstract prototype: LogEventFB with get, set
  abstract StartLogEventFB: builder: FlatBufferBuilder -> unit
  abstract AddTime: builder: FlatBufferBuilder * time: uint32 -> unit
  abstract AddThread: builder: FlatBufferBuilder * thread: int -> unit
  abstract AddTier: builder: FlatBufferBuilder * tier: Offset<string> -> unit
  abstract AddMachineId: builder: FlatBufferBuilder * id: VectorOffset -> unit
  abstract AddTag: builder: FlatBufferBuilder * tag: Offset<string> -> unit
  abstract AddLogLevel: builder: FlatBufferBuilder * level: Offset<string> -> unit
  abstract AddMessage: builder: FlatBufferBuilder * msg: Offset<string> -> unit
  abstract CreateMachineIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
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
  abstract Trigger: bool
  abstract Value: bool

type BoolFBConstructor =
  abstract prototype: BoolFB with get, set
  abstract StartBoolFB: builder: FlatBufferBuilder -> unit
  abstract AddTrigger: builder: FlatBufferBuilder * trigger:bool -> unit
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

type TransactionFB =
  abstract Commands: int -> StateMachineFB
  abstract CommandsLength: int

type TransactionFBConstructor =
  abstract prototype: TransactionFB with get, set
  abstract StartTransactionFB: builder:FlatBufferBuilder -> unit
  abstract AddCommands: builder:FlatBufferBuilder * commands:Offset<StateMachineFB> -> unit
  abstract EndTransactionFB: builder:FlatBufferBuilder -> Offset<TransactionFB>
  abstract GetRootAsTransactionFB: bytes:ByteBuffer -> TransactionFB
  abstract CreateCommandsVector: builder: FlatBufferBuilder * Offset<StateMachineFB> array -> Offset<'a>
  abstract Create: unit -> TransactionFB

let TransactionFB: TransactionFBConstructor = failwith "JS only"

//  ____  _ _               __  __
// / ___|| (_) ___ ___  ___|  \/  | __ _ _ __
// \___ \| | |/ __/ _ \/ __| |\/| |/ _` | '_ \
//  ___) | | | (_|  __/\__ \ |  | | (_| | |_) |
// |____/|_|_|\___\___||___/_|  |_|\__,_| .__/
//                                      |_|

type SlicesMapFB =
  abstract Slices: int -> SlicesFB
  abstract SlicesLength: int

type SlicesMapFBConstructor =
  abstract prototype: SlicesMapFB with get, set
  abstract StartSlicesMapFB: builder:FlatBufferBuilder -> unit
  abstract AddSlices: builder:FlatBufferBuilder * slices:Offset<SlicesFB> -> unit
  abstract EndSlicesMapFB: builder:FlatBufferBuilder -> Offset<SlicesMapFB>
  abstract GetRootAsSlicesMapFB: bytes:ByteBuffer -> SlicesMapFB
  abstract CreateSlicesVector: builder: FlatBufferBuilder * Offset<SlicesFB> array -> Offset<'a>
  abstract Create: unit -> SlicesMapFB

let SlicesMapFB: SlicesMapFBConstructor = failwith "JS only"

//  ____  _       __  __                   _
// |  _ \(_)_ __ |  \/  | __ _ _ __  _ __ (_)_ __   __ _
// | |_) | | '_ \| |\/| |/ _` | '_ \| '_ \| | '_ \ / _` |
// |  __/| | | | | |  | | (_| | |_) | |_) | | | | | (_| |
// |_|   |_|_| |_|_|  |_|\__,_| .__/| .__/|_|_| |_|\__, |
//                            |_|   |_|            |___/

type PinMappingFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Source: int -> byte
  abstract SourceLength: int
  abstract Sinks: int -> string
  abstract SinksLength: int

type PinMappingFBConstructor =
  abstract prototype: PinMappingFB with get, set
  abstract StartPinMappingFB: builder:FlatBufferBuilder -> unit
  abstract AddId: builder:FlatBufferBuilder * id:VectorOffset -> unit
  abstract AddSource: builder:FlatBufferBuilder * source:VectorOffset -> unit
  abstract AddSinks: builder:FlatBufferBuilder * source:Offset<'a> -> unit
  abstract EndPinMappingFB: builder:FlatBufferBuilder -> Offset<PinMappingFB>
  abstract GetRootAsPinMappingFB: bytes:ByteBuffer -> PinMappingFB
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateSourceVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateSinksVector: builder: FlatBufferBuilder * Offset<string> array -> Offset<'a>
  abstract Create: unit -> PinMappingFB

let PinMappingFB: PinMappingFBConstructor = failwith "JS only"

//  ____  _    __        ___     _            _
// |  _ \(_)_ _\ \      / (_) __| | __ _  ___| |_
// | |_) | | '_ \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
// |  __/| | | | \ V  V / | | (_| | (_| |  __/ |_
// |_|   |_|_| |_|\_/\_/  |_|\__,_|\__, |\___|\__|
//                                 |___/

type PinWidgetFB =
  abstract Id: int -> byte
  abstract IdLength: int
  abstract Name: string
  abstract WidgetType: int -> byte
  abstract WidgetTypeLength: int

type PinWidgetFBConstructor =
  abstract prototype: PinWidgetFB with get, set
  abstract StartPinWidgetFB: builder:FlatBufferBuilder -> unit
  abstract AddId: builder:FlatBufferBuilder * id:VectorOffset -> unit
  abstract AddName: builder:FlatBufferBuilder * source:Offset<string> -> unit
  abstract AddWidgetType: builder:FlatBufferBuilder * source:VectorOffset -> unit
  abstract CreateIdVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract CreateWidgetTypeVector: builder: FlatBufferBuilder * id:byte array -> VectorOffset
  abstract EndPinWidgetFB: builder:FlatBufferBuilder -> Offset<PinWidgetFB>
  abstract GetRootAsPinWidgetFB: bytes:ByteBuffer -> PinWidgetFB
  abstract Create: unit -> PinWidgetFB

let PinWidgetFB: PinWidgetFBConstructor = failwith "JS only"

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
  abstract SaveFB: StateMachineActionFB
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
  abstract PinMappingFB: StateMachinePayloadFB
  abstract PinWidgetFB: StateMachinePayloadFB
  abstract RaftMemberFB: StateMachinePayloadFB
  abstract UserFB: StateMachinePayloadFB
  abstract SessionFB: StateMachinePayloadFB
  abstract LogEventFB: StateMachinePayloadFB
  abstract StateFB: StateMachinePayloadFB
  abstract StringFB: StateMachinePayloadFB
  abstract ProjectFB: StateMachinePayloadFB
  abstract SlicesMapFB: StateMachinePayloadFB
  abstract DiscoClientFB: StateMachinePayloadFB
  abstract CuePlayerFB: StateMachinePayloadFB
  abstract DiscoveredServiceFB: StateMachinePayloadFB
  abstract ClockFB: StateMachinePayloadFB
  abstract TransactionFB: StateMachinePayloadFB
  abstract FsEntryUpdateFB: StateMachinePayloadFB
  abstract FsTreeUpdateFB: StateMachinePayloadFB

let StateMachinePayloadFB: StateMachinePayloadFBConstructor = failwith "JS only"

type StateMachineFB =
  abstract Action: StateMachineActionFB
  abstract PayloadType: StateMachinePayloadFB
  abstract DiscoveredServiceFB: DiscoveredServiceFB
  abstract CueFB: CueFB
  abstract CueListFB: CueListFB
  abstract PinFB: PinFB
  abstract PinGroupFB: PinGroupFB
  abstract PinMappingFB: PinMappingFB
  abstract PinWidgetFB: PinWidgetFB
  abstract RaftMemberFB: RaftMemberFB
  abstract UserFB: UserFB
  abstract SessionFB: SessionFB
  abstract LogEventFB: LogEventFB
  abstract StateFB: StateFB
  abstract StringFB: StringFB
  abstract ProjectFB: ProjectFB
  abstract DiscoClientFB: DiscoClientFB
  abstract CuePlayerFB: CuePlayerFB
  abstract ClockFB: ClockFB
  abstract SlicesMapFB: SlicesMapFB
  abstract TransactionFB: TransactionFB
  abstract FsEntryUpdateFB: FsEntryUpdateFB
  abstract FsTreeUpdateFB: FsTreeUpdateFB
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
