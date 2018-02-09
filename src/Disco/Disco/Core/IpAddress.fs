(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import.JS
open System.Text.RegularExpressions

#else

open System.Net

#endif

// * IpAddress

type IpAddress =
  | IPv4Address of string
  | IPv6Address of string

  // ** ToString

  override self.ToString () =
    match self with
      | IPv4Address str -> str
      | IPv6Address str -> str

  // ** Parse

  static member Parse (str: string) =
    #if FABLE_COMPILER
    let regex = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")
    match regex.IsMatch str with
    | true -> IPv4Address str
    | _    -> IPv6Address str
    #else
    let ip = IPAddress.Parse str
    match ip.AddressFamily with
      | Sockets.AddressFamily.InterNetwork   -> IPv4Address str
      | Sockets.AddressFamily.InterNetworkV6 -> IPv6Address str
      | _ -> failwith "Addressfamily not supportet"
    #endif

  // ** TryParse

  static member TryParse (str: string) =
    let str = if System.String.IsNullOrWhiteSpace(str) then "0.0.0.0" else str
    #if FABLE_COMPILER
    try
      IpAddress.Parse str
      |> Result.succeed
    with
      | exn ->
        sprintf "Unable to parse IP: %s Cause: %s" str exn.Message
        |> Disco.Core.Error.asParseError "IpAddress.Parse"
        |> Result.fail
    #else
    try
      let ip = IPAddress.Parse(str)
      match ip.AddressFamily with
      | Sockets.AddressFamily.InterNetwork   -> IPv4Address str |> Ok
      | Sockets.AddressFamily.InterNetworkV6 -> IPv6Address str |> Ok
      | fam ->
        sprintf "Unable to parse IP: %s Unsupported AddressFamily: %A" str fam
        |> Error.asParseError "IpAddress.Parse"
        |> Result.fail

    with
      | exn ->
        sprintf "Unable to parse IP: %s Cause: %s" str exn.Message
        |> Error.asParseError "IpAddress.Parse"
        |> Result.fail
    #endif

  // ** ofIPAddress

  #if !FABLE_COMPILER

  static member ofIPAddress (ip: IPAddress) =
    match ip.AddressFamily with
    | Sockets.AddressFamily.InterNetwork   -> IPv4Address (string ip)
    | Sockets.AddressFamily.InterNetworkV6 -> IPv6Address (string ip)
    | _ -> IPv4Address "0.0.0.0"

  #endif

  // ** toIPAddress

  #if !FABLE_COMPILER

  member self.toIPAddress () =
    self |> string |> IPAddress.Parse

  #endif

  // ** Localhost

  static member Localhost
    with get () = IPv4Address "127.0.0.1"
