namespace Iris.Service

open Suave
open Suave.Http;
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
open Iris.Core
open Iris.Core.Commands
open Iris.Service.Interfaces

module Http =
  let private tag (str: string) = "HttpServer." + str

  module private Actions =
    let deserializeJson<'T> =
      let converter = Fable.JsonConverter()
      fun json -> Newtonsoft.Json.JsonConvert.DeserializeObject<'T>(json, converter)

    let getString rawForm =
      System.Text.Encoding.UTF8.GetString(rawForm)

    let mapJsonWith<'T> (f: 'T->string) =
      request(fun r ->
        f (r.rawForm |> getString |> deserializeJson<'T>)
        |> Encoding.UTF8.GetBytes
        |> Successful.ok
        >=> Writers.setMimeType "text/plain")

    let respondWithCors ctx status (txt: string) =      
      let res =
        { ctx.response with
            status = status
            headers = ["Access-Control-Allow-Origin", "*"
                       //"Access-Control-Allow-Headers", "content-type"
                       "Content-Type", "text/plain"]
            content = Encoding.UTF8.GetBytes txt |> Bytes }
      Some { ctx with response = res }

  let private noCache =
    setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
    >=> setHeader "Need-Help" "k@ioct.it"
    >=> setHeader "Pragma" "no-cache"
    >=> setHeader "Expires" "0"

  let private locate dir str =
    noCache >=> (dir </> filepath str |> unwrap |> file)

  let getDefaultBasePath() =
  #if INTERACTIVE
    Path.GetFullPath(".") </> "assets" </> "frontend"
  #else
    let asm = System.Reflection.Assembly.GetExecutingAssembly()
    let dir = Path.GetDirectoryName(asm.Location)
    dir <.> "assets"
  #endif

  let pathWithArgs (pattern: string) (f: Map<string,string>->WebPart) =
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

//  let private widgetPath = basePath </> "widgets"
//
//  let private listFiles (path: FilePath) : FileName list =
//    DirectoryInfo(widgetPath).EnumerateFiles()
//    |> Seq.map (fun file -> file.Name)
//    |> Seq.toList
//
//  let private importStmt (name: FileName) =
//    sprintf """<link rel="import" href="widgets/%s" />""" name
//
//  let private indexHtml () =
//    listFiles widgetPath
//    |> List.map importStmt
//    |> List.fold (+) ""
//    |> sprintf "%s"

  // Add more mime-types here if necessary
  // the following are for fonts, source maps etc.
  let private mimeTypes = defaultMimeTypesMap

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

  let private mkConfig (config: IrisMachine)
                       (basePath: FilePath)
                       (cts: CancellationTokenSource) :
                       Either<IrisError,SuaveConfig> =
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
                [|"iris"|] }

        let machine = MachineConfig.get()
        let addr = IPAddress.Parse machine.WebIP
        let port = Sockets.Port.Parse (string machine.WebPort)

        sprintf "Suave Web Server ready to start on: %A:%A\nSuave will serve static files from %O" addr port basePath
        |> Logger.info (tag "mkConfig")

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
            |> Error.asSocketError (tag "mkConfig")
            |> Error.exitWith
    }

  // ** HttpServer

  [<RequireQualifiedAccess>]
  module HttpServer =

    // *** create

    let create (config: IrisMachine) (frontend: FilePath option) (postCommand: CommandAgent) =
      either {
        let basePath = defaultArg frontend <| getDefaultBasePath() |> Path.getFullPath
        let cts = new CancellationTokenSource()
        let! webConfig = mkConfig config basePath cts

        return
          { new IHttpServer with
              member self.Start () =
                try
                  let _, server =
                    basePath </> filepath "index.html"
                    |> app postCommand
                    |> startWebServerAsync webConfig
                  Async.Start server
                  |> Either.succeed
                with
                  | exn ->
                    exn.Message
                    |> Error.asSocketError (tag "create")
                    |> Either.fail

              member self.Dispose () =
                try
                  cts.Cancel ()
                  cts.Dispose ()
                with
                  | _ -> () }
      }
