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
open Iris.Service.Interfaces

module Http =
  let private tag (str: string) = "HttpServer." + str

  module private Actions =
    open System.Text

    let private getString rawForm =
      System.Text.Encoding.UTF8.GetString(rawForm)

    let respond ctx status (txt: string) =
      { ctx with response = { ctx.response with status = status; content = Encoding.UTF8.GetBytes txt |> Bytes }}
      |> Some |> async.Return

    let getWsport (iris: IIrisServer ref) (config: IrisMachine) (ctx: HttpContext) =
      either {
        let! cfg = iris.Value.Config
        let! mem = Config.selfMember cfg
        return mem.WsPort
      }
      |> Either.unwrap (fun err -> Logger.err config.MachineId (tag "getWsport") (string err); 0us)
      |> string |> respond ctx HTTP_200 

    let loadProject (postCommand: string->unit) (ctx: HttpContext) =
      ctx.request.rawForm |> getString |> (+) "load " |> postCommand
      respond ctx HTTP_200 ""

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
  let private app (iris: IIrisServer ref) (config: IrisMachine) postCommand indexHtml =
    choose [
      Filters.GET >=>
        (choose [
          Filters.path WS_PORT_ENDPOINT >=> Actions.getWsport iris config
          Filters.path "/" >=> (Files.file indexHtml)
          Files.browseHome ])
      Filters.POST >=>
        (choose [
          Filters.path LOAD_PROJECT_ENDPOINT >=> Actions.loadProject postCommand
        ])
      RequestErrors.NOT_FOUND "Page not found."
    ]

  let private mkConfig (config: IrisMachine)
                       (basePath: string)
                       (cts: CancellationTokenSource) :
                       Either<IrisError,SuaveConfig> =
    either {
      try
        let logger =
          { new Logger with
              member self.Log level nextLine =
                let line = nextLine ()
                match line.level with
                | Suave.Logging.LogLevel.Verbose -> ()
                | _ ->
                  line.message
                  |> Logger.debug config.MachineId (tag "logger") }

        let addr = IPAddress.Parse Constants.DEFAULT_IP
        let port = Sockets.Port.Parse (string Constants.DEFAULT_WEB_PORT)

        sprintf "Suave Web Server ready to start on: %A:%A" addr port
        |> Logger.info config.MachineId (tag "mkConfig")

        return
          { defaultConfig with
              logger            = logger
              cancellationToken = cts.Token
              homeFolder        = Some basePath
              bindings          = [ HttpBinding.mk HTTP addr port ]
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

    /// - basePath: Directory from where static files will be served
    let create (iris: IIrisServer ref) (config: IrisMachine) (postCommand: string->unit) (basePath: string) =
      either {
        let cts = new CancellationTokenSource()
        let! webConfig = mkConfig config basePath cts

        return
          { new IHttpServer with
              member self.Start () =
                try
                  let _, server =
                    Path.Combine(basePath, "index.html")
                    |> app iris config postCommand
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
