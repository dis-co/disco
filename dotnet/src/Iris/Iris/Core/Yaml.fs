namespace Iris.Core

open FSharp.Configuration

[<AutoOpen>]
module Yaml =

  [<Literal>]
  let private Config = """
Project:
  Metadata:
    Id:
    CreatedOn:
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
    Http:        -1
    Raft:        -1
    WebSocket:   -1
    UDPCues:     -1

  Engine:
    LogLevel:
    DataDir:
    BindAddress:
    RequestTimeout:   -1
    ElectionTimeout:  -1
    MaxLogDepth:      -1
    MaxRetries:       -1
    PeriodicInterval: -1

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
        Port: -1
        Status:
        TaskId:

    Groups:
      - Name:
        Members:
          -
"""

  type ConfigFile = YamlConfig<"",false,Config>

  let IrisConfig = ConfigFile()
