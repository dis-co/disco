namespace Iris.Service.Types

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

type AssetServer(config: Config) =
  let cts = new CancellationTokenSource()

  let noCache =
    setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
    >=> setHeader "Need-Help" "k@ioct.it"
    >=> setHeader "Pragma" "no-cache"
    >=> setHeader "Expires" "0"

  let locate dir str =
    noCache >=> file (dir </> str)

  let basepath = Path.GetFullPath(".") </> "assets"

  // Add more mime-types here if necessary
  // the following are for fonts, source maps etc.
  let mimeTypes = defaultMimeTypesMap

  // our application only needs to serve files off the disk
  // but we do need to specify what to do in the base case, i.e. "/"
  let app =
    choose [
      GET >=> choose [
        path "/" >=> locate basepath "index.html"
        browseHome
      ]
    ]

  let appConfig =
    let token : CancellationToken = cts.Token

    match getNodeId () |> tryFindNode config with
    | Some node ->
      let addr = IPAddress.Parse (string node.IpAddr)
      let port = Sockets.Port.Parse (string node.WebPort)

      printfn "Starting WebSocket Server on: %A:%A" addr port

      { defaultConfig with
          logger            = ConsoleWindowLogger(Suave.Logging.LogLevel.Info)
          cancellationToken = token
          homeFolder        = Some(basepath)
          bindings          = [ HttpBinding.mk HTTP addr port ]
          mimeTypesMap      = mimeTypes }
    | _ ->
      printfn "Unable to find node in config. Aborting."
      exitWith ExitCode.MissingNode

  let thread = new Thread(new ThreadStart(fun _ ->
    try startWebServer appConfig app
    with
      | :? System.OperationCanceledException -> ()
      | ex -> printfn "Exception: %s" ex.Message))


  member this.Start() : unit =
    thread.Start ()

  interface System.IDisposable with
    member this.Dispose() : unit =
      cts.Cancel ()
      cts.Dispose ()
