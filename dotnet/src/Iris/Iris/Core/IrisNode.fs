namespace Iris.Core

open System
open FlatBuffers
open Iris.Serialization.Raft

type IrisNode =
  { MemberId : Guid
  ; HostName : string
  ; IpAddr   : IpAddress
  ; Port     : int }

  static member Create name host port =
    { MemberId = Guid.NewGuid()
    ; HostName = name
    ; IpAddr   = IPv4Address host
    ; Port     = port }

  override self.ToString() =
    sprintf "[id: %A] [hostname: %s] [Ip: %A] [port: %A]"
      self.MemberId
      self.HostName
      self.IpAddr
      self.Port

  member self.ToOffset (builder: FlatBufferBuilder) =
    let id = string self.MemberId |> builder.CreateString
    let ip = string self.IpAddr   |> builder.CreateString
    let hn = self.HostName        |> builder.CreateString

    IrisNodeFB.StartIrisNodeFB (builder)
    IrisNodeFB.AddMemberId     (builder, id)
    IrisNodeFB.AddHostName     (builder, hn)
    IrisNodeFB.AddIpAddr       (builder, ip)
    IrisNodeFB.AddPort         (builder, self.Port)
    IrisNodeFB.EndIrisNodeFB   (builder)


  static member FromFB (fb: IrisNodeFB) =
    { MemberId = Guid.Parse fb.MemberId
    ; HostName = fb.HostName
    ; IpAddr   = IPv4Address fb.IpAddr
    ; Port     = fb.Port }
