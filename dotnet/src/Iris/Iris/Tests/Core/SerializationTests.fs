namespace Iris.Tests

open Expecto
open Expecto.Helpers
open FsCheck
open FsCheck.GenBuilder
open Iris.Core
open Iris.Raft
open Iris.Service
open Iris.Serialization
open Iris.Service.Utilities
open Iris.Service.Persistence
open System
open System.Net
open FlatBuffers
open FSharpx.Functional

module Generators =
  open System.Net

  //  ___         _       _     _
  // |_ _|_ __   / \   __| | __| |_ __ ___  ___ ___
  //  | || '_ \ / _ \ / _` |/ _` | '__/ _ \/ __/ __|
  //  | || |_) / ___ \ (_| | (_| | | |  __/\__ \__ \
  // |___| .__/_/   \_\__,_|\__,_|_|  \___||___/___/
  //     |_|
  let ipv4 = gen {
      let! bts = Gen.listOfLength 4 Arb.generate<byte>
      let addr =
        bts
        |> List.map (int >> string)
        |> fun parts -> String.Join(".", parts)
      return addr
    }

  let ipv6 = gen {
      let! bts = Gen.listOfLength 16 Arb.generate<byte>
      let addr =
        bts
        |> List.map (fun bte -> String.Format("{0:X2}",bte))
        |> List.chunkBySize 2
        |> List.fold
          (fun m lst ->
            match lst with
            | [ fst; snd ] -> fst + snd :: m
            | _ -> m)
          []
        |> fun lst -> String.Join(":", lst)
      return addr
    }

  let ipGen =
    Gen.oneof [ Gen.map IPv4Address ipv4
                Gen.map IPv6Address ipv6 ]

  let stringGen = Arb.generate<string>
  let intGen = Arb.generate<int>
  let uint8Gen = Arb.generate<uint8>
  let uint16Gen = Arb.generate<uint16>
  let uint32Gen = Arb.generate<uint32>

  let indexGen = Gen.map index intGen
  let termGen = Gen.map term intGen
  let nameGen = Gen.map name stringGen
  let emailGen = Gen.map email stringGen
  let hashGen = Gen.map checksum stringGen
  let pathGen = Gen.map filepath stringGen
  let portGen = Gen.map port uint16Gen
  let versionGen = Gen.map version stringGen
  let tsGen = Arb.generate<TimeStamp>
  let tagGen = Gen.map astag stringGen
  let timeoutGen = Gen.map ((*) 1<ms>) intGen

  let inline maybeGen g = Gen.oneof [ Gen.constant None
                                      Gen.map Some g ]

  let inline mapGen g = Gen.arrayOf g |> Gen.map (Array.map toPair >> Map.ofArray)

  //  ___    _
  // |_ _|__| |
  //  | |/ _` |
  //  | | (_| |
  // |___\__,_|

  let idGen = gen {
      let! value = Arb.generate<Guid>
      return Id (string value)
    }

  //  __  __            _     _
  // |  \/  | __ _  ___| |__ (_)_ __   ___
  // | |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  // | |  | | (_| | (__| | | | | | | |  __/
  // |_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let machineGen = gen {
      let! id = idGen
      let! hn = nameGen
      let! wrksp = pathGen
      let! ba = ipGen
      let! wp = portGen
      let! rp = portGen
      let! wsp = portGen
      let! gp = portGen
      let! ap = portGen
      let! vs = versionGen
      return
        { MachineId = id
          HostName = hn
          WorkSpace = wrksp
          BindAddress = ba
          WebPort = wp
          RaftPort = rp
          WsPort = wsp
          GitPort = gp
          ApiPort = ap
          Version = vs }
    }

  //  ____        __ _   ____  _        _
  // |  _ \ __ _ / _| |_/ ___|| |_ __ _| |_ ___
  // | |_) / _` | |_| __\___ \| __/ _` | __/ _ \
  // |  _ < (_| |  _| |_ ___) | || (_| | ||  __/
  // |_| \_\__,_|_|  \__|____/ \__\__,_|\__\___|


  let raftStateGen = Gen.oneof [ Gen.constant Joining
                                 Gen.constant Running
                                 Gen.constant Failed ]

  //  ____        __ _   __  __                _
  // |  _ \ __ _ / _| |_|  \/  | ___ _ __ ___ | |__   ___ _ __
  // | |_) / _` | |_| __| |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
  // |  _ < (_| |  _| |_| |  | |  __/ | | | | | |_) |  __/ |
  // |_| \_\__,_|_|  \__|_|  |_|\___|_| |_| |_|_.__/ \___|_|

  let raftMem = gen {
      let! id = idGen
      let! n = nameGen
      let! ip = ipGen
      let! p = portGen
      let! wp = portGen
      let! ap = portGen
      let! gp = portGen
      let! voting = Arb.generate<bool>
      let! vfm = Arb.generate<bool>
      let! state = raftStateGen
      let! nidx = indexGen
      let! midx = indexGen
      return
        { Id         = id
          HostName   = n
          IpAddr     = ip
          Port       = p
          WsPort     = wp
          GitPort    = gp
          ApiPort    = ap
          Voting     = voting
          VotedForMe = vfm
          State      = state
          NextIndex  = nidx
          MatchIndex = midx }
    }

  let raftMemArr = Gen.arrayOf raftMem

  //  _                _                   _
  // | |    ___   __ _| |    _____   _____| |
  // | |   / _ \ / _` | |   / _ \ \ / / _ \ |
  // | |__| (_) | (_| | |__|  __/\ V /  __/ |
  // |_____\___/ \__, |_____\___| \_/ \___|_|
  //             |___/

  let logLevelGen =
    [ LogLevel.Debug
      LogLevel.Info
      LogLevel.Warn
      LogLevel.Err
      LogLevel.Trace ]
    |> List.map Gen.constant
    |> Gen.oneof

  //  ___      _      ____             __ _
  // |_ _|_ __(_)___ / ___|___  _ __  / _(_) __ _
  //  | || '__| / __| |   / _ \| '_ \| |_| |/ _` |
  //  | || |  | \__ \ |__| (_) | | | |  _| | (_| |
  // |___|_|  |_|___/\____\___/|_| |_|_| |_|\__, |
  //                                        |___/

  let audioGen = gen {
      let! sr = Arb.generate<uint32>
      return { SampleRate = sr }
    }

  let exeGen = gen {
      let! pth = pathGen
      let! vs = versionGen
      let! req = Arb.generate<bool>
      return { Executable = pth
               Version = vs
               Required = req }
    }

  let plugGen = gen {
      let! nm = nameGen
      let! pth = pathGen
      return { Name = nm; Path = pth }
    }

  let vvvvGen = gen {
      let! exes = Gen.arrayOf exeGen
      let! plugs = Gen.arrayOf plugGen
      return { Executables = exes; Plugins = plugs }
    }

  let raftConfigGen = gen {
      let! reqto = timeoutGen
      let! elto = timeoutGen
      let! mld = intGen
      let! level = logLevelGen
      let! dd = pathGen
      let! mrtr = intGen
      let! prdc = timeoutGen
      return
        { RequestTimeout = reqto
          ElectionTimeout = elto
          MaxLogDepth = mld
          LogLevel = level
          DataDir = dd
          MaxRetries = mrtr
          PeriodicInterval = prdc }
    }

  let timingGen = gen {
      let! fb = Arb.generate<uint32>
      let! input = stringGen
      let! srvs = Gen.arrayOf ipGen
      let! udp = Arb.generate<uint32>
      let! tcp = Arb.generate<uint32>
      return
        { Framebase = fb
          Input = input
          Servers = srvs
          UDPPort = udp
          TCPPort = tcp }
    }

  let hostgroupGen = gen {
      let! nm = nameGen
      let! mems = Gen.arrayOf idGen
      return { Name = nm; Members = mems }
    }

  let clusterGen = gen {
      let! id = idGen
      let! nm = nameGen
      let! mems = mapGen raftMem
      let! grps = Gen.arrayOf hostgroupGen
      return
        { Id = id
          Name = nm
          Members = mems
          Groups = grps }
    }

  let rectGen = gen {
      let! w = intGen
      let! h = intGen
      return Rect (w, h)
    }

  let coordinateGen = gen {
      let! x = intGen
      let! y = intGen
      return Coordinate (x, y)
    }

  let signalGen = gen {
      let! sz = rectGen
      let! pos = coordinateGen
      return { Size = sz; Position = pos }
    }

  let regionGen = gen {
      let! id = idGen
      let! nm = nameGen
      let! srcpos = coordinateGen
      let! srcsz = rectGen
      let! outpos = coordinateGen
      let! outsz = rectGen
      return
        { Id = id
          Name = nm
          SrcPosition = srcpos
          SrcSize = srcsz
          OutputPosition = outpos
          OutputSize = outsz }
    }

  let regionmapGen = gen {
      let! id = idGen
      let! regs = Gen.arrayOf regionGen
      return { SrcViewportId = id; Regions = regs }
    }

  let displayGen = gen {
      let! id = idGen
      let! nm = nameGen
      let! sz = rectGen
      let! sigs = Gen.arrayOf signalGen
      let! rm = regionmapGen
      return
        { Id = id
          Name = nm
          Size = sz
          Signals = sigs
          RegionMap = rm }
    }

  let viewportGen = gen {
      let! id = idGen
      let! nm = nameGen
      let! pos = coordinateGen
      let! sz = rectGen
      let! outpos = coordinateGen
      let! outsz = rectGen
      let! ovlp = rectGen
      let! desc = stringGen
      return
        { Id = id
          Name = nm
          Position = pos
          Size = sz
          OutputPosition = outpos
          OutputSize = outsz
          Overlap = ovlp
          Description = desc }
    }

  let taskGen = gen {
      let! id = idGen
      let! desc = stringGen
      let! did = idGen
      let! aus = stringGen
      let! args = Gen.arrayOf (Gen.zip stringGen
                                       stringGen)
      return
        { Id = id
          Description = desc
          DisplayId = did
          AudioStream = aus
          Arguments = args }
    }

  let configGen = gen {
      let! machine = machineGen
      let! site = maybeGen idGen
      let! vs = stringGen
      let! ac = audioGen
      let! vvvv = vvvvGen
      let! raft = raftConfigGen
      let! timing = timingGen
      let! sites = Gen.arrayOf clusterGen
      let! vps = Gen.arrayOf viewportGen
      let! disps = Gen.arrayOf displayGen
      let! tks = Gen.arrayOf taskGen
      return
        { Machine = machine
          ActiveSite = site
          Version = vs
          Audio = ac
          Vvvv = vvvv
          Raft = raft
          Timing = timing
          Sites = sites
          ViewPorts = vps
          Displays = disps
          Tasks = tks }
    }

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/

  let projectGen = gen {
      let! id  = idGen
      let! nme = nameGen
      let! pth = pathGen
      let! crt = tsGen
      let! lst = maybeGen tsGen
      let! cr  = maybeGen stringGen
      let! ath = maybeGen stringGen
      let! cfg = configGen
      return
        { Id = id
          Name = nme
          Path = pth
          CreatedOn = crt
          LastSaved = lst
          Copyright = cr
          Author = ath
          Config = cfg }
    }

  //  ___      _     _____
  // |_ _|_ __(_)___| ____|_ __ _ __ ___  _ __
  //  | || '__| / __|  _| | '__| '__/ _ \| '__|
  //  | || |  | \__ \ |___| |  | | | (_) | |
  // |___|_|  |_|___/_____|_|  |_|  \___/|_|

  let errGen =
    [ Gen.constant OK
      Gen.map GitError     (Gen.two stringGen)
      Gen.map ProjectError (Gen.two stringGen)
      Gen.map SocketError  (Gen.two stringGen)
      Gen.map ParseError   (Gen.two stringGen)
      Gen.map IOError      (Gen.two stringGen)
      Gen.map AssetError   (Gen.two stringGen)
      Gen.map RaftError    (Gen.two stringGen)
      Gen.map ClientError  (Gen.two stringGen)
      Gen.map Other        (Gen.two stringGen) ]
    |> Gen.oneof

  //  ____                  _          ____  _        _
  // / ___|  ___ _ ____   _(_) ___ ___/ ___|| |_ __ _| |_ _   _ ___
  // \___ \ / _ \ '__\ \ / / |/ __/ _ \___ \| __/ _` | __| | | / __|
  //  ___) |  __/ |   \ V /| | (_|  __/___) | || (_| | |_| |_| \__ \
  // |____/ \___|_|    \_/ |_|\___\___|____/ \__\__,_|\__|\__,_|___/

  let servicestatusGen =
    [ Gen.constant ServiceStatus.Starting
      Gen.constant ServiceStatus.Running
      Gen.constant ServiceStatus.Stopping
      Gen.constant ServiceStatus.Stopped
      Gen.map ServiceStatus.Degraded errGen
      Gen.map ServiceStatus.Failed errGen
      Gen.constant ServiceStatus.Disposed ]
    |> Gen.oneof

  //  ___      _      ____ _ _            _
  // |_ _|_ __(_)___ / ___| (_) ___ _ __ | |_
  //  | || '__| / __| |   | | |/ _ \ '_ \| __|
  //  | || |  | \__ \ |___| | |  __/ | | | |_
  // |___|_|  |_|___/\____|_|_|\___|_| |_|\__|


  let clientGen = gen {
      let! id = idGen
      let! nm = stringGen
      let! sts = servicestatusGen
      let! ip = ipGen
      let! prt = portGen
      return
        { Id = id
          Role = Role.Renderer
          Name = nm
          Status = sts
          IpAddress = ip
          Port = prt }
    }

  //  ____  _
  // |  _ \(_)_ __
  // | |_) | | '_ \
  // |  __/| | | | |
  // |_|   |_|_| |_|

  let directionGen =
    [ ConnectionDirection.Input
      ConnectionDirection.Output ]
    |> List.map Gen.constant
    |> Gen.oneof

  let behaviorGen =
    [ Behavior.Simple
      Behavior.MultiLine
      Behavior.FileName
      Behavior.Directory
      Behavior.Url
      Behavior.IP ]
    |> List.map Gen.constant
    |> Gen.oneof

  let vecsizeGen =
    [ Gen.constant VecSize.Dynamic
      Gen.map VecSize.Fixed uint16Gen ]
    |> Gen.oneof

  let stringpinGen = gen {
      let! id = idGen
      let! nm = stringGen
      let! grp = idGen
      let! tgs = Gen.arrayOf tagGen
      let! dir = directionGen
      let! bh = behaviorGen
      let! mx = Gen.map ((*) 1<chars>) intGen
      let! vs = vecsizeGen
      let! lbs = Gen.arrayOf stringGen
      let! vls = Gen.arrayOf stringGen
      return
        { Id = id
          Name = nm
          PinGroup = grp
          Tags = tgs
          Direction = dir
          Behavior = bh
          MaxChars = mx
          VecSize = vs
          Labels = lbs
          Values = vls }
    }

  let numberpinGen = gen {
      let! id = idGen
      let! nm = stringGen
      let! grp = idGen
      let! tgs = Gen.arrayOf tagGen
      let! dir = directionGen
      let! vs = vecsizeGen
      let! min = intGen
      let! max = intGen
      let! unit = stringGen
      let! prec = Arb.generate<uint32>
      let! lbs = Gen.arrayOf stringGen
      let! vls = Arb.generate<double[]>
      return
        { Id = id
          Name = nm
          PinGroup = grp
          Tags = tgs
          Min = min
          Max = max
          Unit = unit
          Direction = dir
          Precision = prec
          VecSize = vs
          Labels = lbs
          Values = vls }
    }

  let boolpinGen = gen {
      let! id = idGen
      let! nm = stringGen
      let! grp = idGen
      let! tgs = Gen.arrayOf tagGen
      let! dir = directionGen
      let! vs = vecsizeGen
      let! trig = Arb.generate<bool>
      let! lbs = Gen.arrayOf stringGen
      let! vls = Arb.generate<bool[]>
      return
        { Id = id
          Name = nm
          PinGroup = grp
          Tags = tgs
          IsTrigger = trig
          Direction = dir
          VecSize = vs
          Labels = lbs
          Values = vls }
    }

  let bytepinGen : Gen<BytePinD> = gen {
      let! id = idGen
      let! nm = stringGen
      let! grp = idGen
      let! tgs = Gen.arrayOf tagGen
      let! dir = directionGen
      let! vs = vecsizeGen
      let! lbs = Gen.arrayOf stringGen
      let! vls = Gen.arrayOf Arb.generate<byte[]>
      return
        { Id = id
          Name = nm
          PinGroup = grp
          Tags = tgs
          VecSize = vs
          Direction = dir
          Labels = lbs
          Values = vls }
    }

  let propertyGen = gen {
      let! key = stringGen
      let! value = stringGen
      return { Key = key; Value = value }
    }

  let enumpinGen = gen {
      let! id = idGen
      let! nm = stringGen
      let! grp = idGen
      let! vs = vecsizeGen
      let! tgs = Gen.arrayOf tagGen
      let! dir = directionGen
      let! lbs = Gen.arrayOf stringGen
      let! vls = Gen.arrayOf propertyGen
      let! props = Gen.arrayOf propertyGen
      return
        { Id = id
          Name = nm
          PinGroup = grp
          Tags = tgs
          VecSize = vs
          Direction = dir
          Properties = props
          Labels = lbs
          Values = vls }
    }

  let rgbaGen = gen {
      let! red = Arb.generate<uint8>
      let! green = Arb.generate<uint8>
      let! blue = Arb.generate<uint8>
      let! alpha = Arb.generate<uint8>
      return
        { Red = red
          Green = green
          Blue = blue
          Alpha = alpha }
    }

  let hslaGen = gen {
      let! hue = Arb.generate<uint8>
      let! saturation = Arb.generate<uint8>
      let! lightness = Arb.generate<uint8>
      let! alpha = Arb.generate<uint8>
      return
        { Hue = hue
          Saturation = saturation
          Lightness = lightness
          Alpha = alpha }
    }

  let colorGen =
    Gen.oneof [ Gen.map RGBA rgbaGen
                Gen.map HSLA hslaGen ]

  let colorpinGen = gen {
      let! id = idGen
      let! nm = stringGen
      let! grp = idGen
      let! vs = vecsizeGen
      let! tgs = Gen.arrayOf tagGen
      let! dir = directionGen
      let! lbs = Gen.arrayOf stringGen
      let! vls = Gen.arrayOf colorGen
      return
        { Id = id
          Name = nm
          PinGroup = grp
          Direction = dir
          Tags = tgs
          VecSize = vs
          Labels = lbs
          Values = vls }
    }

  let pinGen =
    [ Gen.map StringPin stringpinGen
      Gen.map NumberPin numberpinGen
      Gen.map BoolPin   boolpinGen
      Gen.map BytePin   bytepinGen
      Gen.map EnumPin   enumpinGen
      Gen.map ColorPin  colorpinGen ]
    |> Gen.oneof

  let slicesGen =
    [ Gen.map StringSlices (Gen.zip idGen Arb.generate<string[]>)
      Gen.map NumberSlices (Gen.zip idGen Arb.generate<double[]>)
      Gen.map BoolSlices   (Gen.zip idGen Arb.generate<bool[]>)
      Gen.map ByteSlices   (Gen.zip idGen Arb.generate<byte[][]>)
      Gen.map EnumSlices   (Gen.zip idGen (Gen.arrayOf propertyGen))
      Gen.map ColorSlices  (Gen.zip idGen (Gen.arrayOf colorGen)) ]
    |> Gen.oneof

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let cueGen = gen {
      let! id = idGen
      let! nm = stringGen
      let! slcs = Gen.arrayOf slicesGen
      return
        { Id = id
          Name = nm
          Slices = slcs }
    }

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_
  // | |  | | | |/ _ \ |   | / __| __|
  // | |__| |_| |  __/ |___| \__ \ |_
  //  \____\__,_|\___|_____|_|___/\__|

  let cuelistGen = gen {
      let! id = idGen
      let! nm = nameGen
      let! cues = Gen.arrayOf cueGen
      return
        { Id = id
          Name = nm
          Cues = cues }
    }

  //   ____           ____  _
  //  / ___|   _  ___|  _ \| | __ _ _   _  ___ _ __
  // | |  | | | |/ _ \ |_) | |/ _` | | | |/ _ \ '__|
  // | |__| |_| |  __/  __/| | (_| | |_| |  __/ |
  //  \____\__,_|\___|_|   |_|\__,_|\__, |\___|_|
  //                                |___/

  let cueplayerGen = gen {
      let! id = idGen
      let! nm = nameGen
      let! cl = maybeGen idGen
      let! sel = indexGen
      let! call = pinGen
      let! next = pinGen
      let! prev = pinGen
      let! rmw = intGen
      let! lcd = maybeGen idGen
      let! lcr = maybeGen idGen
      return
        { Id = id
          Name = nm
          CueList = cl
          Selected = sel
          Call = call
          Next = next
          Previous = prev
          RemainingWait = rmw
          LastCalled = lcd
          LastCaller = lcr }
    }

  //  ____  _        ____
  // |  _ \(_)_ __  / ___|_ __ ___  _   _ _ __
  // | |_) | | '_ \| |  _| '__/ _ \| | | | '_ \
  // |  __/| | | | | |_| | | | (_) | |_| | |_) |
  // |_|   |_|_| |_|\____|_|  \___/ \__,_| .__/
  //                                     |_|

  let pingroupGen = gen {
      let! id = idGen
      let! nm = nameGen
      let! clnt = idGen
      let! pins = mapGen pinGen
      return
        { Id = id
          Name = nm
          Client = clnt
          Pins = pins }
    }

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  let userGen = gen {
      let! id = idGen
      let! un = nameGen
      let! fn = nameGen
      let! ln = nameGen
      let! em = emailGen
      let! pw = hashGen
      let! slt = hashGen
      let! jnd = Arb.generate<DateTime>
      let! crt = Arb.generate<DateTime>
      return
        { Id = id
          UserName = un
          FirstName = fn
          LastName = ln
          Email = em
          Password = pw
          Salt = slt
          Joined = jnd
          Created = crt }
    }

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  let sessionGen = gen {
      let! id = idGen
      let! ip = ipGen
      let! ua = stringGen
      return
        { Id = id
          IpAddress = ip
          UserAgent = ua }
    }

  //  ____  _                                     _
  // |  _ \(_)___  ___ _____   _____ _ __ ___  __| |
  // | | | | / __|/ __/ _ \ \ / / _ \ '__/ _ \/ _` |
  // | |_| | \__ \ (_| (_) \ V /  __/ | |  __/ (_| |
  // |____/|_|___/\___\___/ \_/ \___|_|  \___|\__,_|

  let machinestatusGen =
    [ Gen.constant Idle
      Gen.map Busy (Gen.zip idGen nameGen) ]
    |> Gen.oneof

  let protocolGen =
    [ IPv4; IPv6 ]
    |> List.map Gen.constant
    |> Gen.oneof

  let servicetypeGen =
    [ ServiceType.Git
      ServiceType.Raft
      ServiceType.Http
      ServiceType.WebSocket
      ServiceType.Api ]
    |> List.map Gen.constant
    |> Gen.oneof

  let exposedserviceGen = gen {
      let! tpe = servicetypeGen
      let! prt = portGen
      return { ServiceType = tpe; Port = prt }
    }

  let discoveredGen = gen {
      let! id = idGen
      let! nm = stringGen
      let! fn = stringGen
      let! hn = stringGen
      let! ht = stringGen
      let! mst = machinestatusGen
      let! als = Gen.arrayOf stringGen
      let! prt = protocolGen
      let! addrs = Gen.arrayOf ipGen
      let! srvcs = Gen.arrayOf exposedserviceGen
      let! meta = Gen.arrayOf propertyGen
      return
        { Id = id
          Name = nm
          FullName = fn
          HostName = hn
          HostTarget = ht
          Status = mst
          Aliases = als
          Protocol = prt
          AddressList = addrs
          Services = srvcs
          ExtraMetadata = meta }
    }

  //  _                _____                 _
  // | |    ___   __ _| ____|_   _____ _ __ | |_
  // | |   / _ \ / _` |  _| \ \ / / _ \ '_ \| __|
  // | |__| (_) | (_| | |___ \ V /  __/ | | | |_
  // |_____\___/ \__, |_____| \_/ \___|_| |_|\__|
  //             |___/

  let tierGen =
    [ Tier.FrontEnd
      Tier.Client
      Tier.Service ]
    |> List.map Gen.constant
    |> Gen.oneof

  let logeventGen = gen {
      let! time = Arb.generate<uint32>
      let! thread = intGen
      let! tier = tierGen
      let! id = idGen
      let! tag = stringGen
      let! level = logLevelGen
      let! msg = stringGen
      return
        { Time = time
          Thread = thread
          Tier = tier
          Id = id
          Tag = tag
          LogLevel = level
          Message = msg }
    }

  //     _                 ____                                          _
  //    / \   _ __  _ __  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| |
  //   / _ \ | '_ \| '_ \| |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |
  //  / ___ \| |_) | |_) | |__| (_) | | | | | | | | | | | (_| | | | | (_| |
  // /_/   \_\ .__/| .__/ \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|
  //         |_|   |_|

  let appcommandGen =
    [ AppCommand.Undo
      AppCommand.Redo
      AppCommand.Reset ]
    |> List.map Gen.constant
    |> Gen.oneof

  //  ____  _        _
  // / ___|| |_ __ _| |_ ___
  // \___ \| __/ _` | __/ _ \
  //  ___) | || (_| | ||  __/
  // |____/ \__\__,_|\__\___|

  let stateGen = gen {
    let! project = projectGen
    let! groups = mapGen pingroupGen
    let! cues = mapGen cueGen
    let! cuelists = mapGen cuelistGen
    let! sessions = mapGen sessionGen
    let! users = mapGen userGen
    let! clients = mapGen clientGen
    let! players = mapGen cueplayerGen
    let! discovered = mapGen discoveredGen
    return
      { Project            = project
        PinGroups          = groups
        Cues               = cues
        CueLists           = cuelists
        Sessions           = sessions
        Users              = users
        Clients            = clients
        CuePlayers         = players
        DiscoveredServices = discovered }
    }

  //  ____  _        _       __  __            _     _
  // / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
  // \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  //  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
  // |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let statemachineGen =
    [ Gen.map UpdateProject           projectGen
      Gen.constant UnloadProject
      Gen.map AddMember               raftMem
      Gen.map UpdateMember            raftMem
      Gen.map RemoveMember            raftMem
      Gen.map AddClient               clientGen
      Gen.map UpdateClient            clientGen
      Gen.map RemoveClient            clientGen
      Gen.map AddPinGroup             pingroupGen
      Gen.map UpdatePinGroup          pingroupGen
      Gen.map RemovePinGroup          pingroupGen
      Gen.map AddPin                  pinGen
      Gen.map UpdatePin               pinGen
      Gen.map RemovePin               pinGen
      Gen.map UpdateSlices            slicesGen
      Gen.map AddCue                  cueGen
      Gen.map UpdateCue               cueGen
      Gen.map RemoveCue               cueGen
      Gen.map CallCue                 cueGen
      Gen.map AddCueList              cuelistGen
      Gen.map UpdateCueList           cuelistGen
      Gen.map RemoveCueList           cuelistGen
      Gen.map AddCuePlayer            cueplayerGen
      Gen.map UpdateCuePlayer         cueplayerGen
      Gen.map RemoveCuePlayer         cueplayerGen
      Gen.map AddUser                 userGen
      Gen.map UpdateUser              userGen
      Gen.map RemoveUser              userGen
      Gen.map AddSession              sessionGen
      Gen.map UpdateSession           sessionGen
      Gen.map RemoveSession           sessionGen
      Gen.map AddDiscoveredService    discoveredGen
      Gen.map UpdateDiscoveredService discoveredGen
      Gen.map RemoveDiscoveredService discoveredGen
      Gen.map UpdateClock             uint32Gen
      Gen.map Command                 appcommandGen
      Gen.map DataSnapshot            stateGen
      Gen.map SetLogLevel             logLevelGen
      Gen.map LogMsg                  logeventGen ]
    |> Gen.oneof

  //   ____             __ _        ____ _
  //  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
  // | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
  // | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
  //  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
  //                         |___/                         |___/

  let changesGen = Gen.oneof [ Gen.map MemberAdded raftMem
                               Gen.map MemberRemoved raftMem ]

  let changeGen = Arb.fromGen changesGen
  let changeArr = Gen.arrayOf changesGen

  //  _                _____       _
  // | |    ___   __ _| ____|_ __ | |_ _ __ _   _
  // | |   / _ \ / _` |  _| | '_ \| __| '__| | | |
  // | |__| (_) | (_| | |___| | | | |_| |  | |_| |
  // |_____\___/ \__, |_____|_| |_|\__|_|   \__, |
  //             |___/                      |___/

  let rec logGen = gen {
      let prev = Gen.oneof [ Gen.constant None; Gen.map Some logGen ]

      let! id = idGen
      let! idx = indexGen
      let! trm = termGen
      let! lidx = indexGen
      let! ltrm = termGen
      let! entry = Gen.oneof [ Gen.map (fun (mems, prev) -> Configuration(id, idx, trm, mems, prev))
                                       (Gen.zip raftMemArr prev)
                               Gen.map (fun (chs, prev) -> JointConsensus(id, idx, trm, chs, prev))
                                       (Gen.zip changeArr prev)
                               Gen.map (fun (data, prev) -> LogEntry(id, idx, trm, data, prev))
                                       (Gen.zip statemachineGen prev)
                               Gen.map (fun (mems, data) -> Snapshot(id, idx, trm, lidx, ltrm, mems, data))
                                       (Gen.zip raftMemArr statemachineGen) ]
      return entry
    }

  // __     __    _       ____                            _
  // \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __|
  //   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_
  //    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__|
  //                                   |_|

  let voteReqGen = gen {
      let! mem = raftMem
      let! trm = termGen
      let! lidx = indexGen
      let! ltrm = termGen
      return
        { Term = trm
          Candidate = mem
          LastLogIndex = lidx
          LastLogTerm = ltrm }
    }

  //     _                               _ _____       _        _
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
  //         |_|   |_|

  let appendReqGen = gen {
      let! trm = termGen
      let! plidx = indexGen
      let! pltrm = termGen
      let! lcmt = indexGen
      let! entries = Gen.oneof [ Gen.map Some logGen
                                 Gen.constant None ]
      return
        { Term         = trm
          PrevLogIdx   = plidx
          PrevLogTerm  = pltrm
          LeaderCommit = lcmt
          Entries      = entries }
    }


[<AutoOpen>]
module SerializationTests =
  // __     __    _       ____
  // \ \   / /__ | |_ ___|  _ \ ___  ___ _ __   ___  _ __  ___  ___
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
  //   \ V / (_) | ||  __/  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
  //    \_/ \___/ \__\___|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
  //                                    |_|

  let test_validate_requestvote_response_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let vr : VoteResponse =
        { Term = term 8
        ; Granted = false
        ; Reason = Some (RaftError("test","error")) }

      RequestVoteResponse(Id.Create(), vr)
      |> binaryEncDec

  //     _                               _ _____       _        _
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
  //         |_|   |_|

  let test_validate_appendentries_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> mkState

        let mem1 = Member.create (Id.Create())
        let mem2 = Member.create (Id.Create())

        let changes = [| MemberRemoved mem2 |]
        let mems = [| mem1; mem2 |]

        let log =
          Some <| LogEntry(Id.Create(), index 7, term 1, DataSnapshot            (state),
            Some <| LogEntry(Id.Create(), index 6, term 1, DataSnapshot            (state),
              Some <| Configuration(Id.Create(), index 5, term 1, [| mem1 |],
                Some <| JointConsensus(Id.Create(), index 4, term 1, changes,
                  Some <| Snapshot(Id.Create(), index 3, term 1, index 2, term 1, mems, DataSnapshot            (state))))))

        let ae : AppendEntries =
          { Term = term 8
          ; PrevLogIdx = index 192
          ; PrevLogTerm = term 87
          ; LeaderCommit = index 182
          ; Entries = log }

        AppendEntries(Id.Create(), ae)
        |> binaryEncDec

        AppendEntries(Id.Create(), { ae with Entries = None })
        |> binaryEncDec
      }
      |> noError

  //     _                               _ ____
  //    / \   _ __  _ __   ___ _ __   __| |  _ \ ___  ___ _ __   ___  _ __  ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
  //  / ___ \| |_) | |_) |  __/ | | | (_| |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
  //         |_|   |_|                                   |_|

  let test_validate_appendentries_response_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let response : AppendResponse =
        { Term         = term 38
        ; Success      = true
        ; CurrentIndex = index 1234
        ; FirstIndex   = index 8942
        }

      AppendEntriesResponse(Id.Create(), response)
      |> binaryEncDec

  //  ____                        _           _
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  //                   |_|

  let test_validate_installsnapshot_serialization =
    testCase "Validate InstallSnapshot Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> mkState

        let mem1 = [| Member.create (Id.Create()) |]

        let is : InstallSnapshot =
          { Term = term 2134
          ; LeaderId = Id.Create()
          ; LastIndex = index 242
          ; LastTerm = term 124242
          ; Data = Snapshot(Id.Create(), index 12, term 3414, index 241, term 422, mem1, DataSnapshot            (state))
          }

        InstallSnapshot(Id.Create(), is)
        |> binaryEncDec
      }
      |> noError

  //  _   _                 _ ____  _           _
  // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
  // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
  // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
  // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

  let test_validate_handshake_serialization =
    // testCase "Validate HandShake Serialization" <| fun _ ->
    //   HandShake(Member.create (Id.Create()))
    //   |> binaryEncDec
      pending "test_validate_handshake_serialization"

  //  _   _                 ___        __    _
  // | | | | __ _ _ __   __| \ \      / /_ _(_)_   _____
  // | |_| |/ _` | '_ \ / _` |\ \ /\ / / _` | \ \ / / _ \
  // |  _  | (_| | | | | (_| | \ V  V / (_| | |\ V /  __/
  // |_| |_|\__,_|_| |_|\__,_|  \_/\_/ \__,_|_| \_/ \___|

  let test_validate_handwaive_serialization =
    // testCase "Validate HandWaive Serialization" <| fun _ ->
    //   HandWaive(Member.create (Id.Create()))
    //   |> binaryEncDec
      pending "test_validate_handwaive_serialization"

  //  ____          _ _               _
  // |  _ \ ___  __| (_)_ __ ___  ___| |_
  // | |_) / _ \/ _` | | '__/ _ \/ __| __|
  // |  _ <  __/ (_| | | | |  __/ (__| |_
  // |_| \_\___|\__,_|_|_|  \___|\___|\__|

  let test_validate_redirect_serialization =
    testCase "Validate Redirect Serialization" <| fun _ ->
      Redirect(Member.create (Id.Create()))
      |> binaryEncDec

  // __        __   _
  // \ \      / /__| | ___ ___  _ __ ___   ___
  //  \ \ /\ / / _ \ |/ __/ _ \| '_ ` _ \ / _ \
  //   \ V  V /  __/ | (_| (_) | | | | | |  __/
  //    \_/\_/ \___|_|\___\___/|_| |_| |_|\___|

  let test_validate_welcome_serialization =
    // testCase "Validate Welcome Serialization" <| fun _ ->
    //   Welcome(Member.create (Id.Create()))
    //   |> binaryEncDec
      pending "test_validate_welcome_serialization"

  //     _              _               _               _
  //    / \   _ __ _ __(_)_   _____  __| | ___ _ __ ___(_)
  //   / _ \ | '__| '__| \ \ / / _ \/ _` |/ _ \ '__/ __| |
  //  / ___ \| |  | |  | |\ V /  __/ (_| |  __/ | | (__| |
  // /_/   \_\_|  |_|  |_| \_/ \___|\__,_|\___|_|  \___|_|

  let test_validate_arrivederci_serialization =
    // testCase "Validate Arrivederci Serialization" <| fun _ ->
    //   Arrivederci |> binaryEncDec
      pending "test_validate_arrivederci_serialization"

  //  _____
  // | ____|_ __ _ __ ___  _ __
  // |  _| | '__| '__/ _ \| '__|
  // | |___| |  | | | (_) | |
  // |_____|_|  |_|  \___/|_|

  let test_validate_errorresponse_serialization =
    testCase "Validate ErrorResponse Serialization" <| fun _ ->
      let id = Id.Create()
      let combine a b = (a,b)
      List.iter (combine id >> ErrorResponse >> binaryEncDec) [
        OK
        GitError ("one","two")
        ProjectError ("one","two")
        ParseError ("one","two")
        SocketError ("one","two")
        IOError ("one","two")
        AssetError ("one","two")
        RaftError ("one","two")
        Other  ("one","two")
      ]

  //   ____             __ _        ____ _
  //  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
  // | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
  // | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
  //  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
  //                         |___/                         |___/

  let test_config_change =
    testCase "ConfigChange serialization should work" <| fun _ ->
      let prop (ch: ConfigChange) =
        let rech = ch |> Binary.encode |> Binary.decode |> Either.get
        ch = rech
      Check.QuickThrowOnFailure (Prop.forAll Generators.changeGen prop)

  //  ____        __ _   ____                            _
  // |  _ \ __ _ / _| |_|  _ \ ___  __ _ _   _  ___  ___| |_
  // | |_) / _` | |_| __| |_) / _ \/ _` | | | |/ _ \/ __| __|
  // |  _ < (_| |  _| |_|  _ <  __/ (_| | |_| |  __/\__ \ |_
  // |_| \_\__,_|_|  \__|_| \_\___|\__, |\__,_|\___||___/\__|
  //                                  |_|

  let test_validate_raftrequest_serialization =
    testProperty "Validate RaftRequest Serialization" <| fun _ ->
      let prop (req: RaftRequest) =
        let rereq = req |> Binary.encode |> Binary.decode |> Either.get
        req = rereq
      Check.QuickThrowOnFailure (Prop.forAll Generators.raftReqGen prop)

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  let test_save_restore_raft_value_correctly =
    testCase "save/restore raft value correctly" <| fun _ ->
      either {
        let machine = MachineConfig.create "127.0.0.1" None

        let self =
          machine.MachineId
          |> Member.create

        let mem1 =
          Id.Create()
          |> Member.create

        let mem2 =
          Id.Create()
          |> Member.create

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (self.Id,self)
                                       (mem1.Id, mem1)
                                       (mem2.Id, mem2) |] }

        let config =
          Config.create "default" machine
          |> Config.addSiteAndSetActive site

        let trm = term 666

        let! raft =
          createRaft config
          |> Either.map (Raft.setTerm trm)

        saveRaft config raft
        |> Either.mapError Error.throw
        |> ignore

        let! loaded = loadRaft config

        expect "Member should be correct" self Raft.self loaded
        expect "Term should be correct" trm Raft.currentTerm loaded
      }
      |> noError

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/

  let test_validate_project_binary_serialization =
    testCase "Validate IrisProject Binary Serializaton" <| fun _ ->
      either {
        let! project = mkTmpDir () |>  mkProject
        let! reproject = project |> Binary.encode |> Binary.decode
        expect "Project should be the same" project id reproject
      }
      |> noError

  let test_validate_project_yaml_serialization =
    testCase "Validate IrisProject Yaml Serializaton" <| fun _ ->
      either {
        let! project = mkTmpDir () |>  mkProject
        let reproject : IrisProject = project |> Yaml.encode |> Yaml.decode |> Either.get
        let reconfig = { reproject.Config with Machine = project.Config.Machine }

        // not all properties can be the same (timestampts for instance, so we check basics)
        expect "Project Id should be the same" project.Id id reproject.Id
        expect "Project Name should be the same" project.Name id reproject.Name
        expect "Project Config should be the same" project.Config id reconfig
      }
      |> noError

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let test_validate_cue_binary_serialization =
    testCase "Validate Cue Binary Serialization" <| fun _ ->
      mkCue () |> binaryEncDec

  let test_validate_cue_yaml_serialization =
    testCase "Validate Cue Yaml Serialization" <| fun _ ->
      mkCue () |> yamlEncDec

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_
  // | |  | | | |/ _ \ |   | / __| __|
  // | |__| |_| |  __/ |___| \__ \ |_
  //  \____\__,_|\___|_____|_|___/\__|

  let test_validate_cuelist_binary_serialization =
    testCase "Validate CueList Binary Serialization" <| fun _ ->
      mkCueList () |> binaryEncDec

  let test_validate_cuelist_yaml_serialization =
    testCase "Validate CueList Yaml Serialization" <| fun _ ->
      mkCueList () |> yamlEncDec

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  let test_validate_group_binary_serialization =
    testCase "Validate PinGroup Binary Serialization" <| fun _ ->
      mkPinGroup () |> binaryEncDec

  let test_validate_group_yaml_serialization =
    testCase "Validate PinGroup Yaml Serialization" <| fun _ ->
      mkPinGroup () |> yamlEncDec

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  let test_validate_session_binary_serialization =
    testCase "Validate Session Binary Serialization" <| fun _ ->
      mkSession () |> binaryEncDec

  let test_validate_session_yaml_serialization =
    testCase "Validate Session Yaml Serialization" <| fun _ ->
      mkSession () |> yamlEncDec

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  let test_validate_user_binary_serialization =
    testCase "Validate User Binary Serialization" <| fun _ ->
      mkUser () |> binaryEncDec

  let test_validate_user_yaml_serialization =
    testCase "Validate User Yaml Serialization" <| fun _ ->
      mkUser () |> yamlEncDec

  //  ____  _ _
  // / ___|| (_) ___ ___
  // \___ \| | |/ __/ _ \
  //  ___) | | | (_|  __/
  // |____/|_|_|\___\___|

  let test_validate_slice_binary_serialization =
    testCase "Validate Slice Binary Serialization" <| fun _ ->
      [| BoolSlice   (index 0, true)
      ;  StringSlice (index 0, "hello")
      ;  NumberSlice (index 0, 1234.0)
      ;  ByteSlice   (index 0, [| 0uy; 4uy; 9uy; 233uy |])
      ;  EnumSlice   (index 0, { Key = "one"; Value = "two" })
      ;  ColorSlice  (index 0, RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy })
      ;  ColorSlice  (index 0, HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy })
      |]
      |> Array.iter binaryEncDec

  let test_validate_slice_yaml_serialization =
    testCase "Validate Slice Yaml Serialization" <| fun _ ->
      [| BoolSlice    (index 0, true    )
      ;  StringSlice  (index 0, "hello" )
      ;  NumberSlice  (index 0, 1234.0  )
      ;  ByteSlice    (index 0, [| 0uy; 4uy; 9uy; 233uy |] )
      ;  EnumSlice    (index 0, { Key = "one"; Value = "two" })
      ;  ColorSlice   (index 0, RGBA { Red = 255uy; Blue = 2uy; Green = 255uy; Alpha = 33uy } )
      ;  ColorSlice   (index 0, HSLA { Hue = 255uy; Saturation = 25uy; Lightness = 255uy; Alpha = 55uy } )
      |]
      |> Array.iter yamlEncDec

  //  ____  _ _
  // / ___|| (_) ___ ___  ___
  // \___ \| | |/ __/ _ \/ __|
  //  ___) | | | (_|  __/\__ \
  // |____/|_|_|\___\___||___/

  let test_validate_slices_binary_serialization =
    testCase "Validate Slices Binary Serialization" <| fun _ ->
      mkSlices() |> Array.iter binaryEncDec

  let test_validate_slices_yaml_serialization =
    testCase "Validate Slices Yaml Serialization" <| fun _ ->
      mkSlices() |> Array.iter yamlEncDec

  //  ____  _
  // |  _ \(_)_ __
  // | |_) | | '_ \
  // |  __/| | | | |
  // |_|   |_|_| |_|

  let test_validate_pin_binary_serialization =
    testCase "Validate Pin Binary Serialization" <| fun _ ->
      mkPins () |> Array.iter binaryEncDec

  let test_validate_pin_yaml_serialization =
    testCase "Validate Pin Yaml Serialization" <| fun _ ->
      mkPins () |> Array.iter yamlEncDec

  //   ____ _ _            _
  //  / ___| (_) ___ _ __ | |_
  // | |   | | |/ _ \ '_ \| __|
  // | |___| | |  __/ | | | |_
  //  \____|_|_|\___|_| |_|\__|

  let test_validate_client_binary_serialization =
    testCase "Validate Client Binary Serialization" <| fun _ ->
      mkClient () |> binaryEncDec

  //  ____  _        _
  // / ___|| |_ __ _| |_ ___
  // \___ \| __/ _` | __/ _ \
  //  ___) | || (_| | ||  __/
  // |____/ \__\__,_|\__\___|

  let test_validate_state_binary_serialization =
    testCase "Validate State Binary Serialization" <| fun _ ->
      mkTmpDir() |> mkState |> Either.map binaryEncDec |> noError

  //  ____  _        _       __  __            _     _
  // / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
  // \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  //  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
  // |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let test_validate_state_machine_binary_serialization =
    testCase "Validate StateMachine Binary Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> mkState

        [ AddCue                  <| mkCue ()
          UpdateCue               <| mkCue ()
          RemoveCue               <| mkCue ()
          AddCueList              <| mkCueList ()
          UpdateCueList           <| mkCueList ()
          RemoveCueList           <| mkCueList ()
          AddSession              <| mkSession ()
          UpdateSession           <| mkSession ()
          RemoveSession           <| mkSession ()
          AddUser                 <| mkUser ()
          UpdateUser              <| mkUser ()
          RemoveUser              <| mkUser ()
          AddPinGroup             <| mkPinGroup ()
          UpdatePinGroup          <| mkPinGroup ()
          RemovePinGroup          <| mkPinGroup ()
          AddPin                  <| mkPin ()
          UpdatePin               <| mkPin ()
          RemovePin               <| mkPin ()
          UpdateSlices            <| mkSlice ()
          AddClient               <| mkClient ()
          UpdateClient            <| mkClient ()
          RemoveClient            <| mkClient ()
          AddMember               <| Member.create (Id.Create())
          UpdateMember            <| Member.create (Id.Create())
          RemoveMember            <| Member.create (Id.Create())
          AddDiscoveredService    <| mkDiscoveredService ()
          UpdateDiscoveredService <| mkDiscoveredService ()
          RemoveDiscoveredService <| mkDiscoveredService ()
          DataSnapshot            <| state
          UpdateClock 1234u
          Command AppCommand.Undo
          LogMsg(Logger.create Debug "bla" "oohhhh")
          SetLogLevel Warn
        ] |> List.iter binaryEncDec
      }
      |> noError

  let test_validate_discovered_service_binary_serialization =
    testCase "Validate DiscoveredService Binary Serialization" <| fun _ ->
      mkDiscoveredService()
      |> binaryEncDec

  let test_validate_client_api_request_binary_serialization =
    testCase "Validate ClientApiRequest Binary Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> mkState
        [ AddCue                  <| mkCue ()
          UpdateCue               <| mkCue ()
          RemoveCue               <| mkCue ()
          AddCueList              <| mkCueList ()
          UpdateCueList           <| mkCueList ()
          RemoveCueList           <| mkCueList ()
          AddCuePlayer            <| mkCuePlayer ()
          UpdateCuePlayer         <| mkCuePlayer ()
          RemoveCuePlayer         <| mkCuePlayer ()
          AddSession              <| mkSession ()
          UpdateSession           <| mkSession ()
          RemoveSession           <| mkSession ()
          AddUser                 <| mkUser ()
          UpdateUser              <| mkUser ()
          RemoveUser              <| mkUser ()
          AddPinGroup             <| mkPinGroup ()
          UpdatePinGroup          <| mkPinGroup ()
          RemovePinGroup          <| mkPinGroup ()
          AddPin                  <| mkPin ()
          UpdatePin               <| mkPin ()
          RemovePin               <| mkPin ()
          UpdateSlices            <| mkSlice ()
          AddClient               <| mkClient ()
          UpdateClient            <| mkClient ()
          RemoveClient            <| mkClient ()
          AddDiscoveredService    <| mkDiscoveredService ()
          UpdateDiscoveredService <| mkDiscoveredService ()
          RemoveDiscoveredService <| mkDiscoveredService ()
          AddMember               <| Member.create (Id.Create())
          UpdateMember            <| Member.create (Id.Create())
          RemoveMember            <| Member.create (Id.Create())
          DataSnapshot            <| state
        ] |> List.iter binaryEncDec
      }
      |> noError

  let test_validate_cueplayer_binary_serialization =
    testCase "Validate CuePlayer Binary Serialization" <| fun _ ->
      mkCuePlayer() |> binaryEncDec

  let test_validate_cueplayer_yaml_serialization =
    testCase "Validate CuePlayer Yaml Serialization" <| fun _ ->
      mkCuePlayer() |> yamlEncDec

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/

  let serializationTests =
    ftestList "Serialization Tests" [
      // test_validate_discovered_service_binary_serialization
      // test_validate_requestvote_response_serialization
      // test_validate_appendentries_serialization
      // test_validate_appendentries_response_serialization
      // test_validate_installsnapshot_serialization
      // test_validate_handshake_serialization
      // test_validate_handwaive_serialization
      // test_validate_redirect_serialization
      // test_validate_welcome_serialization
      // test_validate_arrivederci_serialization
      // test_validate_errorresponse_serialization
      // test_save_restore_raft_value_correctly
      // test_validate_project_binary_serialization
      // test_validate_project_yaml_serialization
      // test_validate_cue_binary_serialization
      // test_validate_cue_yaml_serialization
      // test_validate_cuelist_binary_serialization
      // test_validate_cuelist_yaml_serialization
      // test_validate_group_binary_serialization
      // test_validate_group_yaml_serialization
      // test_validate_session_binary_serialization
      // test_validate_session_yaml_serialization
      // test_validate_user_binary_serialization
      // test_validate_user_yaml_serialization
      // test_validate_slice_binary_serialization
      // test_validate_slices_binary_serialization
      // test_validate_pin_binary_serialization
      // test_validate_pin_yaml_serialization
      // test_validate_client_binary_serialization
      // test_validate_state_binary_serialization
      // test_validate_state_machine_binary_serialization
      // test_validate_client_api_request_binary_serialization
      // test_validate_cueplayer_binary_serialization
      // test_validate_cueplayer_yaml_serialization

      test_config_change
      test_validate_raftrequest_serialization
    ]
