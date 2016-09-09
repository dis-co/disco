namespace Iris.Core

open System.Net
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type IpAddress =
  | IPv4Address of string
  | IPv6Address of string

  static member Type
    with get () = "Iris.Core.IpAddress"

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
    json.Add("$type", new JValue(IpAddress.Type))

    match self with
    | IPv4Address str ->
      json.Add("Case", new JValue("IPv4Address"))
      json.Add("Fields", new JArray([| new JValue(str) |]))
    | IPv6Address str ->
      json.Add("Case", new JValue("IPv6Address"))
      json.Add("Fields", new JArray([| new JValue(str) |]))

    json :> JToken

  member self.ToJson()  =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : IpAddress option =
    try
      let fields = token.["Fields"] :?> JArray
      match string token.["Case"] with
      | "IPv4Address" -> IPv4Address (string fields.[0]) |> Some
      | "IPv6Address" -> IPv6Address (string fields.[1]) |> Some
      | _             -> None
    with
      | exn ->
        printfn "Could not deserialize json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : IpAddress option =
    JObject.Parse(str) |> IpAddress.FromJToken

#endif
