namespace Iris.Core

open System.Net
open Newtonsoft.Json
open Newtonsoft.Json.Linq

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

    //      _
    //     | |___  ___  _ __
    //  _  | / __|/ _ \| '_ \
    // | |_| \__ \ (_) | | | |
    //  \___/|___/\___/|_| |_|

    member self.ToJToken() : JToken =
      let json = new JObject()
      json.Add("$type", new JValue("Iris.Core.IpAddress"))

      match self with
      | IPv4Address str ->
        json.Add("Case", new JValue("IPv4Address"))
        json.Add("Fields", new JArray([| new JValue(str) |]))
      | IPv6Address str ->
        json.Add("Case", new JValue("IPv6Address"))
        json.Add("Fields", new JArray([| new JValue(str) |]))

      json :> JToken

#endif
