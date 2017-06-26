[<RequireQualifiedAccess>]
module Iris.Unity

open System
open System.Collections.Generic

type IIrisClient =
  inherit IDisposable
  abstract member Guid: Guid
  abstract member RegisterGameObject: groupName: string * pinName: string * values: IDictionary<string, double> * callback: Action<double[]> -> unit

[<CompiledName("GetIrisClient")>]
val getIrisClient: clientId: Guid * serverIp: string * serverPort: uint16 * clientIp: string * clientPort: uint16 * print: Action<string> -> IIrisClient
