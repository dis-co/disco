[<RequireQualifiedAccess>]
module Disco.Unity

open System
open System.Collections.Generic

type IDiscoClient =
  inherit IDisposable
  abstract member Guid: Guid
  abstract member RegisterGameObject: groupName: string * pinName: string * values: IDictionary<string, double> * callback: Action<double[]> -> unit

[<CompiledName("GetDiscoClient")>]
val getDiscoClient: clientId: Guid * serverIp: string * serverPort: uint16 * clientIp: string * clientPort: uint16 * print: Action<string> -> IDiscoClient
