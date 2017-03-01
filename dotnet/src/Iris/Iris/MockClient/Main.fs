namespace Iris.MockClient

open Argu
open Iris.Core
open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open Iris.Raft
open Iris.Client
open Iris.Service
open Iris.Service.Interfaces

[<AutoOpen>]
module Main =

  type OptionBuilder() =
    member this.Return x = Some x
    member this.Bind(m, f) = Option.bind f m

  let maybe = OptionBuilder()

  // * TODO
  // - add, update, remove, list commands
  // - exit/quit

  let private (|Exit|_|) str =
    match str with
    | "exit" | "quit" -> Some ()
    | _ -> None

  [<Literal>]
  let private help = @"
 ___      _      ____ _ _            _
|_ _|_ __(_)___ / ___| (_) ___ _ __ | |_
 | || '__| / __| |   | | |/ _ \ '_ \| __|
 | || |  | \__ \ |___| | |  __/ | | | |_
|___|_|  |_|___/\____|_|_|\___|_| |_|\__| © Nsynk, 2017

type :=
  ""string"" /
  ""int""    /
  ""float""  /
  ""double"" /
  ""bool""   /
  ""byte""   /
  ""enum""   /
  ""color""

attr :=
  ""name""       /
  ""patch""      /
  ""tags""       /
  ""type""       /
  ""max""        /
  ""min""        /
  ""behavior""   /
  ""unit""       /
  ""vecsize""    /
  ""precision""  /
  ""properties"" /
  ""values""

addpin := ""addpin"" type attr
  "

  let private (|Help|_|) str =
    match str with
    | "help" | "h" -> Some help
    | _ -> None

  let private restOf (attr: string) (str: string) =
    str.Substring(attr.Length + 1, str.Length - attr.Length - 1)

  let private parseAttr (attr: string) (str: string) =
    match str.StartsWith(attr) with
    | true -> restOf attr str |> Some
    | false -> None

  let private (|Add|_|) (str: string) =
    parseAttr "add" str

  let private (|Update|_|) (str: string) =
    parseAttr "update" str

  let private (|Remove|_|) (str: string) =
    parseAttr "remove" str

  let private (|Get|_|) (str: string) =
    parseAttr "get" str

  let private (|Toggle|_|) (str: string) =
    parseAttr "toggle" str

  let private (|Bang|_|) (str: string) =
    parseAttr "bang" str

  let private (|String|_|) (str: string) =
    parseAttr "string" str

  let private (|Multiline|_|) (str: string) =
    parseAttr "multiline" str

  let private (|File|_|) (str: string) =
    parseAttr "file" str

  let private (|Dir|_|) (str: string) =
    parseAttr "dir" str

  let private (|Url|_|) (str: string) =
    parseAttr "url" str

  let private (|IP|_|) (str: string) =
    parseAttr "ip" str

  let private (|Float|_|) (str: string) =
    parseAttr "float" str

  let private (|Double|_|) (str: string) =
    parseAttr "double" str

  let private (|Bytes|_|) (str: string) =
    parseAttr "bytes" str

  let private (|Color|_|) (str: string) =
    parseAttr "color" str

  let private (|Enum|_|) (str: string) =
    parseAttr "enum" str

  let private (|AddPin|_|) (str: string) =
    match str with
    | Add rest ->
      match rest with
      | Toggle name ->
        Pin.Toggle(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = false } |])
        |> Some

      | Bang name ->
        Pin.Bang(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = false } |])
        |> Some

      | String name ->
        Pin.String(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = "" } |])
        |> Some

      | Multiline name ->
        Pin.MultiLine(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = "" } |])
        |> Some

      | File name ->
        Pin.FileName(Id.Create(),name,Id.Create(), [| |], "", [| { Index = 0u; Value = "" } |])
        |> Some

      | Dir name ->
        Pin.Directory(Id.Create(),name,Id.Create(), [| |], "", [| { Index = 0u; Value = "" } |])
        |> Some

      | Url name ->
        Pin.Url(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = "" } |])
        |> Some

      | IP name ->
        Pin.IP(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = "" } |])
        |> Some

      | Float name ->
        Pin.Float(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = 0.0 } |])
        |> Some

      | Double name ->
        Pin.Double(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = 0.0 } |])
        |> Some

      | Bytes name ->
        Pin.Bytes(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = [| |] } |])
        |> Some

      | Color name ->
        let color = RGBA { Red = 0uy; Green = 0uy; Blue = 0uy; Alpha = 0uy }
        Pin.Color(Id.Create(),name,Id.Create(), [| |], [| { Index = 0u; Value = color } |])
        |> Some

      | Enum name ->
        let prop = { Key = ""; Value = "" }
        Pin.Enum(Id.Create(),name,Id.Create(), [| |], [| prop |], [| { Index = 0u; Value = prop } |])
        |> Some
      | _ -> None
    | _ -> None

  let private (|Value|_|) (str: string) =
    parseAttr "value" str

  let private (|Properties|_|) (str: string) =
    parseAttr "properties" str

  let private (|ListPins|_|) (str: string) =
    match str with
    | "listpins" | "lp" -> Some ()
    | _ -> None

  let private listPins (pins: Map<Id,Pin>) =
    printfn ""
    Map.iter
      (fun _ (pin: Pin) ->
        printfn "    id: %s name: %s type: %s" (string pin.Id) pin.Name pin.Type)
      pins
    printfn ""

  let private parseLine (line: string) : Pin option =
    match line with
    | AddPin pin -> Some pin
    | _ -> None

  let private tryLoad (path: FilePath) =
    let lines = try File.ReadAllLines(path) with | _ -> [| |]
    Array.fold
      (fun (pins: Map<Id,Pin>) line ->
        match parseLine line with
        | Some pin -> Map.add pin.Id pin pins
        | _ -> pins)
      Map.empty
      lines

  let private parseUpdate (str: string) =
    let spc = str.IndexOf(' ')
    let id = str.Substring(0, spc)
    let rest = str.Substring(spc + 1, str.Length - spc - 1)
    id, rest

  let private parseColor (str: string) =
    match str.Split(' ') with
    | [| r; g; b; a; |] ->
      try
        RGBA { Red = uint8 r; Green = uint8 g; Blue = uint8 b; Alpha = uint8 a }
        |> Some
      with
        | _ -> None
    | _ -> None

  let private parseProperties (str: string) =
    let pattern = @"(\w+)=(\w+)"
    let matches = Regex.Matches(str, pattern)

    if matches.Count > 0 then
      let out = Array.zeroCreate matches.Count
      let mutable i = 0
      for m in matches do
        out.[i] <- { Key = m.Groups.[1].Value; Value = m.Groups.[2].Value }
        i <- i + 1
      out
    else
      [| { Key = ""; Value = "" } |]

  [<Literal>]
  let private PS1 = "λ: "

  [<EntryPoint>]
  let main args =
    let patch = Id.Create()

    let mutable run = true
    let mutable pins =
      if Array.length args = 0 then
        Map.empty
      else
        tryLoad args.[0]

    while run do
      Console.Write(PS1)
      match Console.ReadLine() with
      | Exit _ ->
        printfn "Bye."
        run <- false

      | Help txt -> printfn "%s" txt

      | AddPin pin ->
        pins <- Map.add pin.Id pin pins

      | Get id ->
        Map.iter
          (fun _ (pin: Pin) ->
            if pin.Id = Id (id.Trim()) then
              printfn "%A" pin)
          pins

      | Update rest ->
        let id, rest = parseUpdate rest

        match rest with
        | Value value ->
          match Map.tryFind (Id (id.Trim())) pins with
          | Some (StringPin data) ->
            let updated = StringPin { data with Slices = [| { Index = 1u; Value = value } |] }
            pins <- Map.add updated.Id updated pins

          | Some (IntPin data) ->
            try
              let intval = int value
              let updated = IntPin { data with Slices = [| { Index = 1u; Value = intval } |] }
              pins <- Map.add updated.Id updated pins
            with | exn -> printfn "error: %s" exn.Message

          | Some (FloatPin data) ->
            try
              let floatval = float value
              let updated = FloatPin { data with Slices = [| { Index = 1u; Value = floatval } |] }
              pins <- Map.add updated.Id updated pins
            with | exn -> printfn "error: %s" exn.Message

          | Some (DoublePin data) ->
            try
              let doubleval = double value
              let updated = DoublePin { data with Slices = [| { Index = 1u; Value = doubleval } |] }
              pins <- Map.add updated.Id updated pins
            with | exn -> printfn "error: %s" exn.Message

          | Some (BoolPin data) ->
            try
              let boolval = Boolean.Parse value
              let updated = BoolPin { data with Slices = [| { Index = 1u; Value = boolval } |] }
              pins <- Map.add updated.Id updated pins
            with | exn -> printfn "error: %s" exn.Message

          | Some (BytePin data) ->
            let byteval = Encoding.UTF8.GetBytes value
            let updated = BytePin { data with Slices = [| { Index = 1u; Value = byteval } |] }
            pins <- Map.add updated.Id updated pins

          | Some (EnumPin data) ->
            match Array.tryFind (fun (prop:Property) -> prop.Value = value) data.Properties with
            | Some property ->
              let updated = EnumPin { data with Slices = [| { Index = 1u; Value = property } |] }
              pins <- Map.add updated.Id updated pins
            | _ -> printfn "no property found with value: %s" value

          | Some (ColorPin data) ->
            match parseColor value with
            | Some color ->
              let updated = ColorPin { data with Slices = [| { Index = 1u; Value = color } |] }
              pins <- Map.add updated.Id updated pins
            | _ -> printfn "error: could not parse color"

          | _ -> printfn "could not find pin with id %A" id

        | Properties value ->
          match Map.tryFind (Id (id.Trim())) pins with
          | Some (EnumPin data) ->
            let properties = parseProperties value
            let updated = EnumPin { data with Properties = properties }
            pins <- Map.add updated.Id updated pins
          | _ -> ()
        | other -> printfn "error: unknown subcommand %A" other

      | ListPins -> listPins pins

      | str ->
        printfn "unknown command: %A" str

    exit 0
