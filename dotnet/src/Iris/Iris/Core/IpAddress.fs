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
