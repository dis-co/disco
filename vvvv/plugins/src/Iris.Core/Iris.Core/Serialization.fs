namespace Iris.Core

open System.IO
open System.Text
open WebSharper

/// provides JSON serialization for communication 
module Serialization = 
  let toString = Encoding.ASCII.GetString

  let serialize (value : 'U) : string =
    let JsonProvider = Core.Json.Provider.Create()
    let encoder = JsonProvider.GetEncoder<'U>()
    use writer = new StringWriter()

    value
    |> encoder.Encode
    |> JsonProvider.Pack
    |> WebSharper.Core.Json.Write writer

    writer.ToString()

  let serializeBytes (value : 'U) : byte[] =
    let JsonProvider = Core.Json.Provider.Create()
    let encoder = JsonProvider.GetEncoder<'U>()
    use stream = new MemoryStream()
    use writer = new StreamWriter(stream)

    value
    |> encoder.Encode
    |> JsonProvider.Pack
    |> WebSharper.Core.Json.Write writer

    writer.Flush()
    stream.Flush()
    stream.ToArray()

  let unserializeBytes (bytes : byte[]) : 'U =
    let JsonProvider = Core.Json.Provider.Create()
    let decoder = JsonProvider.GetDecoder<'U>()

    use stream = new MemoryStream(bytes)
    use reader = new StreamReader(stream)

    stream.Seek(0L, SeekOrigin.Begin) |> ignore

    WebSharper.Core.Json.Read reader
    |> decoder.Decode
