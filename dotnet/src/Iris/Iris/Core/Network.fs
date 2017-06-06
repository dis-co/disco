namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import

#else

open System
open System.Net
open System.Net.NetworkInformation

#endif

(*

type NetworkInterfaceStatus =
  | Up
  | Down
  | Unknown

type NetworkInterfaceType =
  | Ethernet
  | Wireless
  | Loopback
  | Unknown

type NetworkInterface =
  { Name: string
    Type: NetworkInterfaceType
    Status: NetworkInterfaceStatus
    Speed: int64
    SupportsMulticast: bool
    IpAddress: IpAddress }

*)

// * NetworkInterfaceStatus

type NetworkInterfaceStatus =
  | Up
  | Down
  | Unknown of string

// * NetworkInterfaceType

type NetworkInterfaceType =
  | Ethernet
  | Wireless
  | Loopback
  | Unknown of string

// * NetworkInterface

type NetworkInterface =
  { Name: string
    Type: NetworkInterfaceType
    Status: NetworkInterfaceStatus
    SupportsMulticast: bool
    Speed: int64
    IpAddresses: IpAddress list }

// * Network

[<RequireQualifiedAccess>]
module Network =

  // ** parseInterfaceType

  let private parseInterfaceType (iface: NetworkInformation.NetworkInterface) =
    match iface.NetworkInterfaceType with
    | NetworkInformation.NetworkInterfaceType.Ethernet -> Ethernet
    | NetworkInformation.NetworkInterfaceType.Wireless80211 -> Wireless
    | NetworkInformation.NetworkInterfaceType.Loopback -> Loopback
    | other -> other |> string |> NetworkInterfaceType.Unknown

  // ** parseInterfaceStatus

  let private parseInterfaceStatus (iface: NetworkInformation.NetworkInterface) =
    match iface.OperationalStatus with
    | OperationalStatus.Up -> Up
    | OperationalStatus.Down -> Down
    | other -> other |> string |> NetworkInterfaceStatus.Unknown

  // ** parseAddresses

  let private parseAddresses (iface: NetworkInformation.NetworkInterface) =
    iface.GetIPProperties()
    |> fun (props: IPInterfaceProperties) -> props.UnicastAddresses
    |> Seq.fold
        (fun m (info: UnicastIPAddressInformation) ->
          if info.Address.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork then
            try
              let ip = IpAddress.Parse (string info.Address)
              ip :: m
            with | _ -> m
          else m)
        []

  // ** getInterfaces

  let getInterfaces () =
    NetworkInterface.GetAllNetworkInterfaces()
    |> Seq.fold
      (fun lst (iface: NetworkInformation.NetworkInterface) ->
        if ( iface.NetworkInterfaceType = NetworkInformation.NetworkInterfaceType.Ethernet
           || iface.NetworkInterfaceType = NetworkInformation.NetworkInterfaceType.Wireless80211 )
           && iface.OperationalStatus    = OperationalStatus.Up
        then
          let parsed =
            { Name = iface.Id
              Type = parseInterfaceType iface
              Status = parseInterfaceStatus iface
              Speed = iface.Speed
              SupportsMulticast = iface.SupportsMulticast
              IpAddresses = parseAddresses iface }
          in parsed :: lst
        else lst)
      []


  // ** getHostName

  /// ## Get the current machine's host name
  ///
  /// Get the current machine's host name.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: string
  let getHostName () =
  #if FABLE_COMPILER
    Browser.window.location.hostname
  #else
    try
      Dns.GetHostName()
    with
      | _ -> Environment.MachineName
  #endif

  #if !FABLE_COMPILER

  // ** getIpAddress

  /// ## getIpAddress
  ///
  /// Find and return the IP address of the current machine.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: IPAddress option
  let getIpAddress (_ : unit) : IPAddress option =
    let mutable outip : IPAddress option = None
    for iface in NetworkInterface.GetAllNetworkInterfaces() do
      if iface.NetworkInterfaceType = NetworkInterfaceType.Wireless80211 ||
        iface.NetworkInterfaceType = NetworkInformation.NetworkInterfaceType.Ethernet
      then
        for ip in iface.GetIPProperties().UnicastAddresses do
          if ip.Address.AddressFamily = Sockets.AddressFamily.InterNetwork
          then outip <- Some(ip.Address)
    outip

  #endif
