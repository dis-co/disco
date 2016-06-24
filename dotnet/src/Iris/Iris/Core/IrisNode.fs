namespace Iris.Core

open System.Net


type IrisNode =
  { HostName : string
  ; IpAddr   : IPAddress
  ; Port     : int
  }

  static member create name host port =
    { HostName = name
    ; IpAddr = IPAddress.Parse host
    ; Port = port
    }

  override self.ToString() =
    sprintf "[hostname: %s] [Ip: %A] [port: %A]"
      self.HostName
      self.IpAddr
      self.Port

