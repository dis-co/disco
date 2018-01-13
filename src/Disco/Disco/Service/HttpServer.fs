(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service

// * Imports

open Suave
open Suave.Http
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers
open Suave.Logging
open Suave.Logging.Log
open Suave.CORS
open Suave.Web
open System.Threading
open System.IO
open System.Net
open System.Net.Sockets
open System.Diagnostics
open System.Text
open System.Text.RegularExpressions
open Disco.Core
open Disco.Core.Commands
open Disco.Service.Interfaces

// * HttpServer

module HttpServer =

  // ** tag

  let private tag (str: string) = "HttpServer." + str

  // ** Actions

  module private Actions =

    // *** deserializeJson

    let deserializeJson<'T> =
      let converter = Fable.JsonConverter()
      fun json -> Newtonsoft.Json.JsonConvert.DeserializeObject<'T>(json, converter)

    // *** getString

    let getString rawForm =
      System.Text.Encoding.UTF8.GetString(rawForm)

    // *** mapJsonWith

    let mapJsonWith<'T> (f: 'T->string) =
      request(fun r ->
        f (r.rawForm |> getString |> deserializeJson<'T>)
        |> Encoding.UTF8.GetBytes
        |> Successful.ok
        >=> Writers.setMimeType "text/plain")

    // *** respondWithCors

    let respondWithCors ctx status (txt: string) =
      let res =
        { ctx.response with
            status = status
            headers = ["Access-Control-Allow-Origin", "*"
                       //"Access-Control-Allow-Headers", "content-type"
                       "Content-Type", "text/plain"]
            content = Encoding.UTF8.GetBytes txt |> Bytes }
      Some { ctx with response = res }

  // ** noCache

  let private noCache =
    setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
    >=> setHeader "Need-Help" "k@ioct.it"
    >=> setHeader "Pragma" "no-cache"
    >=> setHeader "Expires" "0"

  // ** locate

  let private locate dir str =
    noCache >=> (dir </> filepath str |> unwrap |> file)

  // ** getDefaultBasePath

  let private getDefaultBasePath() =
    let asm = System.Reflection.Assembly.GetExecutingAssembly()
    Path.Combine(Path.GetDirectoryName(asm.Location), "www")

  // ** pathWithArgs

  let private pathWithArgs (pattern: string) (f: Map<string,string>->WebPart) =
    let prefix = pattern.Substring(0, pattern.IndexOf(":"))
    let patternParts = pattern.Split('/')
    Filters.pathStarts prefix >=> (fun ctx ->
      let args =
        ctx.request.path.Split('/')
        |> Seq.zip patternParts
        |> Seq.choose (fun (k,v) ->
          if k.[0] = ':'
          then Some(k.Substring(0), v)
          else None)
        |> Map
      f args ctx)

  // ** mimeTypes

  // Add more mime-types here if necessary
  // the following are for fonts, source maps etc.
  let private mimeTypes = defaultMimeTypesMap

  // ** app

  // our application only needs to serve files off the disk
  // but we do need to specify what to do in the base case, i.e. "/"
  let private app (postCommand: CommandAgent) indexHtml =
    let postCommand (ctx: HttpContext) = async {
        let! res =
          ctx.request.rawForm
          |> Actions.getString
          |> Actions.deserializeJson
          |> postCommand
        return
          match res with
          | Left err ->
            Error.toMessage err |> Actions.respondWithCors ctx HTTP_500.status
          | Right msg ->
            msg |> Actions.respondWithCors ctx HTTP_200.status
      }
    choose [
      Filters.GET >=>
        (choose [
          Filters.path "/" >=> cors defaultCORSConfig >=> noCache >=> (indexHtml |> unwrap |> Files.file)
          Files.browseHome >=> cors defaultCORSConfig >=> noCache ])
      Filters.POST >=>
        (choose [
          // Cannot use `cors defaultCORSConfig` here, postCommand adds its own headers
          Filters.path Constants.WEP_API_COMMAND >=> postCommand
        ])
      Filters.OPTIONS >=>
        // defaultCORSConfig already contains: Access-Control-Allow-Methods: <ALL>
        Successful.OK "CORS approved" >=> cors defaultCORSConfig

      RequestErrors.NOT_FOUND "Page not found."
    ]

  // ** makeConfig

  let private makeConfig machine (basePath: FilePath) (cts: CancellationTokenSource) =
    either {
      try
        let logger =
          let reg = Regex("\{(\w+)(?:\:(.*?))?\}")
          { new Logger with
              member x.log(level: Suave.Logging.LogLevel) (nextLine: Suave.Logging.LogLevel -> Message): Async<unit> =
                match level with
                | Suave.Logging.LogLevel.Verbose -> ()
                | level ->
                  let line = nextLine level
                  match line.value with
                  | Event template ->
                    reg.Replace(template, fun m ->
                      let value = line.fields.[m.Groups.[1].Value]
                      if m.Groups.Count = 3
                      then System.String.Format("{0:" + m.Groups.[2].Value + "}", value)
                      else string value)
                    |> Logger.debug (tag "logger")
                  | Gauge _ -> ()
                async.Return ()
              member x.logWithAck(arg1: Suave.Logging.LogLevel) (arg2: Suave.Logging.LogLevel -> Message): Async<unit> =
//                failwith "Not implemented yet"
                async.Return ()
              member x.name: string [] =
                [|"disco"|] }

        do! Network.ensureIpAddress machine.BindAddress
        do! Network.ensureAvailability machine.BindAddress machine.WebPort

        let addr = machine.BindAddress |> string |> IPAddress.Parse
        let port = Sockets.Port.Parse (string machine.WebPort)

        port
        |> sprintf "Suave Web Server ready to start on: %A:%A" addr
        |> Logger.info (tag "makeConfig")

        basePath
        |> sprintf "Suave will serve static files from %O"
        |> Logger.info (tag "makeConfig")

        return
          { defaultConfig with
              logger            = logger
              cancellationToken = cts.Token
              homeFolder        = basePath |> unwrap |> Some
              bindings          = [ HttpBinding.create HTTP addr port ]
              mimeTypesMap      = mimeTypes }
      with
        | exn ->
          return!
            exn.Message
            |> Error.asSocketError (tag "makeConfig")
            |> Error.exitWith
    }

  // ** create

  let create (machine: DiscoMachine) (frontend: FilePath option) (postCommand: CommandAgent) =
    either {
      let status = ref ServiceStatus.Stopped

      let basePath =
        Option.defaultWith (getDefaultBasePath >> filepath) frontend
        |> Path.getFullPath

      let cts = new CancellationTokenSource()
      let! webConfig = makeConfig machine basePath cts

      return
        { new IHttpServer with
            member self.Start () = either {
                try
                  let _, server =
                    basePath </> filepath "index.html"
                    |> app postCommand
                    |> startWebServerAsync webConfig
                  Async.Start server
                  status := ServiceStatus.Running
                  return ()
                with
                  | exn ->
                    return!
                      exn.Message
                      |> Error.asSocketError (tag "create")
                      |> Either.fail
              }

            member self.Dispose () =
              if Service.isRunning !status then
                try
                  cts.Cancel ()
                  cts.Dispose ()
                  status := ServiceStatus.Disposed
                with | _ -> ()
          } }
