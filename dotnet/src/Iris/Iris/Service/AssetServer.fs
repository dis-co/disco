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

type FileName = string

type AssetServer(?config: IrisConfig) =
  let [<Literal>] defaultIP = "127.0.0.1"
  let [<Literal>] defaultPort = "7000"
  let cts = new CancellationTokenSource()

  let noCache =
    setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
    >=> setHeader "Need-Help" "k@ioct.it"
    >=> setHeader "Pragma" "no-cache"
    >=> setHeader "Expires" "0"

  let locate dir str =
    noCache >=> file (dir </> str)

  let basePath =
  #if INTERACTIVE
    Path.GetFullPath(".") </> "assets" </> "frontend"
  #else
    let asm = System.Reflection.Assembly.GetExecutingAssembly()
    let dir = Path.GetDirectoryName(asm.Location)
    dir </> "assets" </> "frontend"
  #endif

  let widgetPath = basePath </> "widgets"

  let listFiles (path: FilePath) : FileName list =
    DirectoryInfo(widgetPath).EnumerateFiles()
    |> Seq.map (fun file -> file.Name)
    |> Seq.toList

  let importStmt (name: FileName) =
    sprintf """<link rel="import" href="widgets/%s" />""" name

  let indexHtml () =
    listFiles widgetPath
    |> List.map importStmt
    |> List.fold (+) ""
    |> sprintf "%s"

  // Add more mime-types here if necessary
  // the following are for fonts, source maps etc.
  let mimeTypes = defaultMimeTypesMap

  // our application only needs to serve files off the disk
  // but we do need to specify what to do in the base case, i.e. "/"
  let app =
    choose [
      Filters.GET >=>
        (choose [
          Filters.path "/" >=> (Files.file <| Path.Combine(basePath, "index.html"))
          Files.browseHome ])
      RequestErrors.NOT_FOUND "Page not found."
    ]

  let appConfig: SuaveConfig =
    either {
      try
        let! addr, port =
          match config with
          | Some config ->
            either {
              let! nid = Config.getNodeId ()
              let! node = Config.findNode config nid
              return string node.IpAddr, string node.WebPort
            }
          | None ->
            either { return defaultIP, defaultPort }

        let addr = IPAddress.Parse addr
        let port = Sockets.Port.Parse port

        printfn "Suave Web Server ready to start on: %A:%A" addr port

        return
          { defaultConfig with
              logger            = ConsoleWindowLogger(Suave.Logging.LogLevel.Info)
              cancellationToken = cts.Token
              homeFolder        = Some(basePath)
              bindings          = [ HttpBinding.mk HTTP addr port ]
              mimeTypesMap      = mimeTypes }
      with
        | exn ->
          return!
            exn.Message
            |> Other
            |> Error.exitWith
    }
    |> Error.orExit id

  let thread = new Thread(new ThreadStart(fun _ ->
    try
      printfn "Starting asset server..."
      startWebServer appConfig app
    with
      | :? System.OperationCanceledException ->
        printfn "Asset server cancelled"
      | ex -> printfn "Asset server Exception: %s" ex.Message))

  member this.Start() : unit =
    thread.Start ()

  interface System.IDisposable with
    member this.Dispose() : unit =
      cts.Cancel ()
      cts.Dispose ()
