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

// * Network

[<RequireQualifiedAccess>]
module Network =

  // *** getHostName

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

  // *** getIpAddress

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
        iface.NetworkInterfaceType = NetworkInterfaceType.Ethernet
      then
        for ip in iface.GetIPProperties().UnicastAddresses do
          if ip.Address.AddressFamily = Sockets.AddressFamily.InterNetwork
          then outip <- Some(ip.Address)
    outip

  #endif
