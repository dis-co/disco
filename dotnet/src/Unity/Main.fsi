[<RequireQualifiedAccess>]
module Iris.Unity
 
open System

type IIrisClient =
  inherit IDisposable
  abstract member RegisterGameObject: objectId: string * callback: Action<double> -> unit

[<CompiledName("GetIrisClient")>]
val getIrisClient: serverIp: string * serverPort: uint16 * print: Action<string> -> IIrisClient
