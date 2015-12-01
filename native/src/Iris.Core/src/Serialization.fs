namespace Iris.Core

open System.IO
open System.Text
open WebSharper

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
