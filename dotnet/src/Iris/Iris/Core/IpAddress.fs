namespace Iris.Core

open System.Net

type IpAddress =
  | IPv4Address of string
  | IPv6Address of string

  with
    override self.ToString () =
      match self with
        | IPv4Address str -> str
        | IPv6Address str -> str

#if JAVASCRIPT
#else
      //    _   _ _____ _____
      //   | \ | | ____|_   _|
      //   |  \| |  _|   | |
      //  _| |\  | |___  | |
      // (_)_| \_|_____| |_|

    static member Parse (str: string) =
      let ip = IPAddress.Parse str
      match ip.AddressFamily with
        | Sockets.AddressFamily.InterNetwork   -> IPv4Address str
        | Sockets.AddressFamily.InterNetworkV6 -> IPv6Address str
        | _ -> failwith "Addressfamily not supportet"

    static member TryParse (str: string) =
      let mutable ip = new IPAddress([||])
      match IPAddress.TryParse(str, &ip) with
        | true ->
          match ip.AddressFamily with
            | Sockets.AddressFamily.InterNetwork   -> IPv4Address str |> Some
            | Sockets.AddressFamily.InterNetworkV6 -> IPv6Address str |> Some
            | _ -> None
        | _ -> None

#endif
