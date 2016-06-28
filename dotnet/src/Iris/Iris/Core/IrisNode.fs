namespace Iris.Core

open System.Net
open FlatBuffers
open Iris.Serialization.Raft

type IrisNode =
  { HostName : string
  ; IpAddr   : IPAddress
  ; Port     : int
  }

  static member Create name host port =
    { HostName = name
    ; IpAddr = IPAddress.Parse host
    ; Port = port
    }

  override self.ToString() =
    sprintf "[hostname: %s] [Ip: %A] [port: %A]"
      self.HostName
      self.IpAddr
      self.Port

  member self.ToOffset (builder: FlatBufferBuilder) =
    let hostname = self.HostName |> builder.CreateString
    let ip = self.IpAddr.ToString() |> builder.CreateString
    IrisNodeFB.StartIrisNodeFB(builder)
    IrisNodeFB.AddHostName(builder, hostname)
    IrisNodeFB.AddIpAddr(builder, ip)
    IrisNodeFB.AddPort(builder, uint32 self.Port)
    IrisNodeFB.EndIrisNodeFB(builder)
