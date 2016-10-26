namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Fable.Import.JS
open System.Text.RegularExpressions

#else

open System.Net

#endif

type IpAddress =
  | IPv4Address of string
  | IPv6Address of string

  override self.ToString () =
    match self with
      | IPv4Address str -> str
      | IPv6Address str -> str

  static member Parse (str: string) =
#if JAVASCRIPT
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

  static member TryParse (str: string) =
#if JAVASCRIPT
    try
      IpAddress.Parse str |> Some          // :D
    with
      | _ -> None
#else
    try
      let ip = IPAddress.Parse(str)
      match ip.AddressFamily with
      | Sockets.AddressFamily.InterNetwork   -> IPv4Address str |> Some
      | Sockets.AddressFamily.InterNetworkV6 -> IPv6Address str |> Some
      | _ -> None
    with
      | exn ->
        printfn "Error: %s\nValue:%s" exn.Message str
        None
#endif
