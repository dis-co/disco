[<RequireQualifiedAccess>]
module Iris.Unity
 
open System

type IIrisClient =
  inherit IDisposable
  abstract member RegisterGameObject: objectId: int * callback: Action<double> -> unit

[<CompiledName("GetIrisClient")>]
val getIrisClient: serverIp: string * serverPort: uint16 -> IIrisClient
