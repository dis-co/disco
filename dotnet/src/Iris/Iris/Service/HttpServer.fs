namespace Iris.Service

open Suave
open Suave.Http;
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers
open Suave.Logging
open Suave.Logging.Loggers
open Suave.Web
open System.Threading
open System.IO
open System.Net
open System.Net.Sockets
open System.Diagnostics
open Iris.Core

module Http =

  let private tag (str: string) = sprintf "HttpServer.%s" str

  [<Literal>]
  let private defaultIP = "127.0.0.1"

  [<Literal>]
  let private defaultPort = "7000"

  let private noCache =
    setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
    >=> setHeader "Need-Help" "k@ioct.it"
    >=> setHeader "Pragma" "no-cache"
    >=> setHeader "Expires" "0"

  let private locate dir str =
    noCache >=> file (dir </> str)

  let getDefaultBasePath() =
  #if INTERACTIVE
    Path.GetFullPath(".") </> "assets" </> "frontend"
  #else
    let asm = System.Reflection.Assembly.GetExecutingAssembly()
    let dir = Path.GetDirectoryName(asm.Location)
    dir </> "assets"
  #endif

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
  let private app indexHtml =
    choose [
      Filters.GET >=>
        (choose [
          Filters.path "/" >=> (Files.file indexHtml)
          Files.browseHome ])
      RequestErrors.NOT_FOUND "Page not found."
    ]

  let private mkConfig (options: IrisConfig)
                       (basePath: string)
                       (cts: CancellationTokenSource) :
                       Either<IrisError,SuaveConfig> =
    either {
      try
        let! mem = Config.selfMember options

        let logger =
          { new Logger with
              member self.Log level nextLine =
                let line = nextLine ()
                match line.level with
                | Suave.Logging.LogLevel.Verbose -> ()
                | _ ->
                  line.message
                  |> Logger.debug options.Machine.MachineId (tag "logger") }

        let addr, port = string mem.IpAddr, string mem.WebPort

        let addr = IPAddress.Parse addr
        let port = Sockets.Port.Parse port

        sprintf "Suave Web Server ready to start on: %A:%A" addr port
        |> Logger.info options.Machine.MachineId (tag "mkConfig")

        return
          { defaultConfig with
              logger            = logger
              cancellationToken = cts.Token
              homeFolder        = Some(basePath)
              bindings          = [ HttpBinding.mk HTTP addr port ]
              mimeTypesMap      = mimeTypes }
      with
        | exn ->
          return!
            exn.Message
            |> Error.asSocketError (tag "mkConfig")
            |> Error.exitWith
    }

  // ** IHttpServer

  type IHttpServer =
    inherit System.IDisposable
    abstract Start: unit -> Either<IrisError,unit>

  // ** HttpServer

  [<RequireQualifiedAccess>]
  module HttpServer =

    // *** create

    let create (options: IrisConfig, basePath: string) =
      either {
        let cts = new CancellationTokenSource()
        let! config = mkConfig options basePath cts

        return
          { new IHttpServer with
              member self.Start () =
                try
                  let indexHtml = Path.Combine(basePath, "index.html")
                  let listening, server = startWebServerAsync config (app indexHtml)
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
