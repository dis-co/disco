namespace Iris.Core

open System.Net

type IpAddress =
  | IPv4Address of string
  | IPv6Address of string
