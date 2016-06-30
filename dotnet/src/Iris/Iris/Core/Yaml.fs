namespace Iris.Core

open FSharp.Configuration

[<AutoOpen>]
module Yaml =

  [<Literal>]
  let private Config = """
Project:
  Metadata:
    Id:
    Year: 2016
    Copyright:
    Author:
    Name:
    LastSaved:

  VVVV:
    Executables:
      - Path:
        Version:
        Required: true
    Plugins:
      - Name:
        Path:

  Ports:
    WebSocket:   -1
    IrisService: -1
    UDPCues:     -1

  Engine:
    AesKey:
    DefaultTimeout:
    DontCompress: false
    FastEthernet: false
    GracefulShutdown: true
    Hosts:
      -
    IgnorePartitions: true
    IgnoreSmallPartitions: true
    InfiniBand: false
    Large: false
    LogDir:
    Logged: true
    MCMDReportRate: -1
    MCRangeHigh:
    MCRangeLow:
    MaxAsyncMTotal: -1
    MaxIPMCAddrs: -1
    MaxMsgLen: -1
    Mute: false
    Netmask:
    NetworkInterfaces:
      -
    OOBViaTCP: true
    Port: -1
    PortP2P: -1
    RateLim: -1
    Sigs: false
    SkipFirstInterface: false
    Subnet:
    TTL: -1
    TokenDelay: -1
    UDPChkSum: false
    UnicastOnly: false
    UseIPv4: true
    UseIPv6: false
    UserDMA: false

  Timing:
    Framebase: 50
    Input:
    Servers:
      -
    UDPPort: 8090
    TCPPort: 8091

  Audio:
    SampleRate: 48000

  ViewPorts:
    - Id:
      Name:
      Position:
      Size:
      OutputPosition:
      OutputSize:
      Overlap:
      Description:

  Displays:
    - Id:
      Name:
      Size:
      Signals:
        - Size:
          Position:
      RegionMap:
        SrcViewportId:
        Regions:
          - Id:
            Name:
            SrcPosition:
            SrcSize:
            OutputPosition:
            OutputSize:
  Tasks:
    - Id:
      Description:
      DisplayId:
      AudioStream:
      Arguments:
        - Key:
          Value:

  Cluster:
    Name:
    Nodes:
      - Id:
        HostName:
        Ip:
        Task:

    Groups:
      - Name:
        Members:
          -
"""

  type ConfigFile = YamlConfig<"",false,Config>

  let IrisConfig = ConfigFile()
