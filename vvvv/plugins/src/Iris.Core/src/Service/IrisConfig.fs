namespace Iris.Service

open Iris.Core.Types
open FSharp.Configuration

[<AutoOpen>]
module IrisConfig =
  
  [<Literal>]
  let private config = """
Project:
  Metadata: 
    Year: 2016
    Copyright: NSYNK
    Author: Karsten Gebbert
    Name: Template Project
    LastSaved: 

  VVVV: 
    Executables: 
      - Path: ..\..\vvvv\vvvv.exe
        Version: 33.7x_86
        Required: true
      - Path: ..\..\vvvv\vvvv.exe
        Version: 33.7x_64
        Required: true
    Plugins:
      - Name: iris
        Path: ..\..\Iris\Nodes

  Ports:
    WebSocket:
    IrisService:
    UDPCues:

  Engine:
    AesKey: JlUx4rYQUklINytFjzDre/4GoLA8xwjYnj6cOPgfcaeh8pabcdgb+NP1....
    DefaultTimeout: default
    DontCompress: false
    FastEthernet: false
    GracefulShutdown: true
    Hosts: localhost
    IgnorePartitions: true
    IgnoreSmallPartitions: true
    InfiniBand: false
    Large: false
    LogDir: Logs
    Logged: true
    MCMDReportRate: -1
    MCRangeHigh: default
    MCRangeLow: default
    MaxAsyncMTotal: -1
    MaxIPMCAddrs: -1
    MaxMsgLen: -1
    Mute: false
    Netmask: default
    NetworkInterfaces:
      - lo
      - enp5s0
    OOBViaTCP: true
    Port: -1
    PortP2P: -1
    RateLim: false
    Sigs: false
    SkipFirstInterface: false
    Subnet: default
    TTL: -1
    TokenDelay: -1
    UDPChkSum: false
    UnicastOnly: false
    UseIPv4: true
    UseIPv6: false
    UserDMA: false

  Timing:
    Framebase: 50
    Input: Iris Freerun
    Servers:
      - 192.168.2.2
      - 192.168.2.3
      - 192.168.2.4
    UDPPort: 8090
    TCPPort: 8091

  Audio:
    SampleRate: 48000

  ViewPorts:
    - ViewPort:
        Id: 0
        Name: ViewPort 1
        Position:
          X: 0
          Y: 0
        Size:
          X: 1200
          Y: 800
        OutputPosition:
          X: 200
          Y: 200
        OutputSize:
          X: 1000
          Y: 300
        Overlap:
          X: 10
          Y: 5
        Description: A nice ViewPort indeed.

  Displays:
    - Display:
      Id: 1
      Name: DP1
      Size:
        X: 5760
        Y: 1080
      Signals:
        - Signal:
            Size:
              X: 1920
              Y: 1080
            Position:
              X: 0
              Y: 0
      RegionMap:
        SrcViewportId: 0 
        Regions:
          - Region:
            Id: 0
            Name: eins
            SrcPosition:
              X: 0
              Y: 0
            SrcSize:
              X: 400
              Y: 100
            OutputPosition:
              X: 0
              Y: 0
            OutputSize:
              X: 0
              Y: 0
  Tasks:
    - Task:
      Id: TEC
      Description: Tec L
      Render:
        DisplayId: 1
      AudioStream: ""
      Arguments:
        - Argument:
            Key: Wall
            Value: 1

  Cluster:
    Name:  MainStage
    Nodes:
      - Node:
          Id: 0
          HostName: CO-10-TEC-L
          Ip: 192.168.100.10
          Task: TEC

    Groups:
      - Group:
          Name: Main
          Members:
            - Member:
                NodeId: 0

      - Group:
          Name: TEC
          Members:
            - Member:
                NodeId: 0
            - Member:
                NodeId: 1
"""

  type private ConfigFile = YamlConfig<"",false,config>

  let private ConfigHandler = ConfigFile()

  type IrisConfig =
    { Project   : Project
    ; Vvvv      : VvvvConfig
    ; Engine    : VsyncConfig
    ; Timing    : TimingConfig
    ; Port      : PortConfig
    ; ViewPorts : ViewPort list
    ; Displays  : Display list
    ; Tasks     : Task list
    ; Cluster   : Cluster
    }
